using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Projectiles.Door;

namespace zhashi.Content.Dimensions
{
    /// <summary>
    /// “放逐”的落点：把主世界的目标丢进灵界。
    ///
    /// SubworldLibrary 同一时间只加载一个世界，NPC 数组无法直接跨世界搬运，所以分两步：
    /// 1) 服务器在主世界把目标收进门里，记录种族与当前血量；
    /// 2) 灵界载入时，按记录把同种生物重新投放出来。
    /// 玩家目标走的是原版灵界钥匙同一条路：服务器通知该客户端调用 SubworldSystem.Enter。
    /// </summary>
    public class SpiritBanishSystem : ModSystem
    {
        /// <summary>待投放队列上限，防止长时间只放逐不清场时无限堆积。</summary>
        private const int MaxPending = 64;

        private struct BanishedCreature
        {
            public int Type;
            public int Life;
            public int LifeMax;
            public int Owner;
        }

        private static readonly List<BanishedCreature> pending = new();
        private static int homeWorldId = -1;

        /// <summary>只有站在主世界时才能往灵界里丢东西。</summary>
        public static bool CanBanishFromHere => !SubworldSystem.IsActive<SpiritWorld>();

        /// <summary>把目标收进隐形门并排入灵界投放队列，返回是否真的送走了。</summary>
        public static bool Banish(NPC npc, Player owner)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || npc == null) return false;
            if (!npc.active || !CanBanishFromHere) return false;

            // 换了主世界存档就丢掉上一档留下的队列，避免幽灵生物串档。
            if (homeWorldId != Main.worldID) { pending.Clear(); homeWorldId = Main.worldID; }

            NPC root = BanishRoot(npc);
            if (!root.active) return false;

            if (pending.Count >= MaxPending) pending.RemoveAt(0);
            pending.Add(new BanishedCreature
            {
                Type = root.type,
                Life = System.Math.Max(1, root.life),
                LifeMax = System.Math.Max(1, root.lifeMax),
                Owner = owner != null && owner.active ? owner.whoAmI : -1
            });

            DespawnBody(root);
            return true;
        }

        /// <summary>蠕虫类 Boss 的目标其实在头部，统一换算成头部再处理。</summary>
        public static NPC BanishRoot(NPC npc)
        {
            if (npc.realLife >= 0 && npc.realLife < Main.maxNPCs && Main.npc[npc.realLife].active)
                return Main.npc[npc.realLife];
            return npc;
        }

        /// <summary>让本体与它的所有体节一起消失，不触发掉落。</summary>
        private static void DespawnBody(NPC root)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active) continue;
                if (npc != root && npc.realLife != root.whoAmI) continue;
                npc.active = false;
                npc.netUpdate = true;
            }
        }

        public override void PostUpdateWorld()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            if (!SubworldSystem.IsActive<SpiritWorld>())
            {
                // 回到主世界时对齐存档编号：换了世界就作废旧队列。
                if (SubworldSystem.Current == null && homeWorldId != Main.worldID)
                {
                    pending.Clear();
                    homeWorldId = Main.worldID;
                }
                return;
            }

            if (pending.Count == 0) return;
            foreach (BanishedCreature record in pending) Release(record);
            pending.Clear();
        }

        /// <summary>在灵界出生点附近找一块能站人的地方把生物放出来。</summary>
        private static void Release(BanishedCreature record)
        {
            Vector2 position = FindReleasePoint();
            int index = NPC.NewNPC(new EntitySource_Misc("zhashi:SpiritBanish"),
                (int)position.X, (int)position.Y, record.Type);
            if (index < 0 || index >= Main.maxNPCs) return;

            NPC npc = Main.npc[index];
            npc.lifeMax = record.LifeMax;
            npc.life = System.Math.Clamp(record.Life, 1, record.LifeMax);
            npc.netUpdate = true;

            // 灵界那一侧同样开门再关门：被丢进来的东西是“从门里掉出来”的。
            int owner = record.Owner >= 0 && record.Owner < Main.maxPlayers ? record.Owner : Main.myPlayer;
            SpawnDoor(position, 2, 0f, owner);
            SpawnDoor(position, 3, 26f, owner);
        }

        private static void SpawnDoor(Vector2 position, int style, float delay, int owner)
        {
            Projectile.NewProjectile(new EntitySource_Misc("zhashi:SpiritBanish"), position, Vector2.Zero,
                ModContent.ProjectileType<DoorAuthorityVisual>(), 0, 0f, owner, style, delay);
        }

        private static Vector2 FindReleasePoint()
        {
            for (int attempt = 0; attempt < 60; attempt++)
            {
                int tileX = Main.spawnTileX + Main.rand.Next(-220, 221);
                int tileY = Main.spawnTileY + Main.rand.Next(-60, 61);
                if (!WorldGen.InWorld(tileX, tileY, 12)) continue;

                Vector2 world = new Vector2(tileX * 16f, tileY * 16f);
                if (Collision.SolidCollision(world, 32, 48)) continue;
                // 脚下要有落脚点，避免直接掉进虚空。
                if (!Collision.SolidCollision(world + new Vector2(0f, 48f), 32, 16)) continue;
                return world;
            }
            return new Vector2(Main.spawnTileX * 16f, Main.spawnTileY * 16f);
        }

        /// <summary>服务器把某个玩家送进灵界：客户端收到后自己走进门。</summary>
        public static void SendPlayerToSpiritWorld(int playerIndex)
        {
            if (Main.netMode != NetmodeID.Server) return;
            if (playerIndex < 0 || playerIndex >= Main.maxPlayers) return;
            ModPacket packet = ModContent.GetInstance<zhashi>().GetPacket();
            packet.Write((byte)LotMNetMsg.SpiritBanishPlayer);
            packet.Write((byte)playerIndex);
            packet.Send(playerIndex);
        }

        public static void ReceivePlayerBanish(BinaryReader reader, int whoAmI)
        {
            byte playerIndex = reader.ReadByte();
            if (Main.netMode != NetmodeID.MultiplayerClient) return;
            if (playerIndex != Main.myPlayer || !Main.player[playerIndex].active || Main.player[playerIndex].dead) return;
            if (SubworldSystem.IsActive<SpiritWorld>()) return;
            SubworldSystem.Enter<SpiritWorld>();
        }
    }
}
