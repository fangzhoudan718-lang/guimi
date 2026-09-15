using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace zhashi.Content.Systems
{
    /// <summary>
    /// 记录本世界已经击败过的 Boss 类型，供“放逐”判断目标是否只是重复刷的 Boss。
    ///
    /// 原版 Boss 另外查询 NPC.downed* 旗标，所以在本模组装上去之前就打赢过的原版 Boss 一样算数；
    /// 模组 Boss 走的是这份运行期记录。
    /// </summary>
    public class BossDefeatRegistry : ModSystem
    {
        private static readonly HashSet<int> defeated = new();

        public static bool IsDefeated(int npcType)
        {
            if (npcType <= 0) return false;
            return defeated.Contains(npcType) || IsVanillaDowned(npcType);
        }

        public static void Record(int npcType)
        {
            if (npcType > 0) defeated.Add(npcType);
        }

        public override void ClearWorld() => defeated.Clear();

        public override void SaveWorldData(TagCompound tag)
        {
            if (defeated.Count > 0) tag["DoorDefeatedBosses"] = new List<int>(defeated);
        }

        public override void LoadWorldData(TagCompound tag)
        {
            defeated.Clear();
            if (!tag.ContainsKey("DoorDefeatedBosses")) return;
            foreach (int npcType in tag.GetList<int>("DoorDefeatedBosses"))
                if (npcType > 0) defeated.Add(npcType);
        }

        private static bool IsVanillaDowned(int type)
        {
            if (type == NPCID.KingSlime) return NPC.downedSlimeKing;
            if (type == NPCID.EyeofCthulhu) return NPC.downedBoss1;
            if (type == NPCID.EaterofWorldsHead || type == NPCID.EaterofWorldsBody ||
                type == NPCID.EaterofWorldsTail || type == NPCID.BrainofCthulhu) return NPC.downedBoss2;
            if (type == NPCID.SkeletronHead) return NPC.downedBoss3;
            if (type == NPCID.QueenBee) return NPC.downedQueenBee;
            if (type == NPCID.TheDestroyer || type == NPCID.TheDestroyerBody ||
                type == NPCID.TheDestroyerTail) return NPC.downedMechBoss1;
            if (type == NPCID.Retinazer || type == NPCID.Spazmatism) return NPC.downedMechBoss2;
            if (type == NPCID.SkeletronPrime) return NPC.downedMechBoss3;
            if (type == NPCID.Plantera) return NPC.downedPlantBoss;
            if (type == NPCID.Golem) return NPC.downedGolemBoss;
            if (type == NPCID.DukeFishron) return NPC.downedFishron;
            if (type == NPCID.CultistBoss) return NPC.downedAncientCultist;
            if (type == NPCID.MoonLordCore || type == NPCID.MoonLordHead ||
                type == NPCID.MoonLordHand) return NPC.downedMoonlord;
            if (type == NPCID.HallowBoss) return NPC.downedEmpressOfLight;
            if (type == NPCID.QueenSlimeBoss) return NPC.downedQueenSlime;
            if (type == NPCID.Deerclops) return NPC.downedDeerclops;
            return false;
        }
    }

    /// <summary>任何 Boss 被击杀时登记一次，之后“放逐”才会承认它是重复刷的目标。</summary>
    public class BossDefeatTracker : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type])
                BossDefeatRegistry.Record(npc.type);
        }
    }
}
