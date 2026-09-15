using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using Terraria.Chat;
using Microsoft.Xna.Framework;
using zhashi.Content.Items.Weapons;
using zhashi.Content.Pathways.Door;

namespace zhashi.Content.Globals
{
    /// <summary>
    /// 序列6 记录官: 击杀有"非凡能力"(可拟态)的敌人时,有概率获得该敌人的"非凡符号".
    /// 借用蠕动饥饿的能力库(IsSupportedNPC_Static): 只有该敌人在能力库里,才能被记录.
    /// </summary>
    public class RecorderGlobalNPC : GlobalNPC
    {
        private static void Notify(Player player, string message, Color color)
        {
            if (Main.netMode == NetmodeID.Server)
                ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral(message), color, player.whoAmI);
            else if (player.whoAmI == Main.myPlayer)
                Main.NewText(message, color);
        }

        public override void OnKill(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (npc.friendly || npc.lifeMax <= 0) return;

            // 必须是能力库里有的NPC才能被记录
            if (!CreepingHunger.IsSupportedNPC_Static(npc.type)) return;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.active || p.dead) continue;
                var mp = p.GetModPlayer<LotMPlayer>();
                if (mp.currentDoorSequence > 6) continue;
                if (p.Distance(npc.Center) > 2400f) continue;

                if (npc.boss || npc.lifeMax > 5000)
                {
                    // 记录随位格质变：记录官5%，旅行家10%，秘法师20%，漫游者35%，旅法师60%，星之匙必定。
                    int divineChance = mp.currentDoorSequence <= 1 ? 100 : mp.currentDoorSequence <= 2 ? 60 :
                                        mp.currentDoorSequence <= 3 ? 35 : mp.currentDoorSequence <= 4 ? 20 :
                                        mp.currentDoorSequence <= 5 ? 10 : 5;
                    if (Main.rand.Next(100) < divineChance && mp.recorderDivineList.Count < mp.GetRecorderDivineMax())
                    {
                        if (!mp.recorderDivineList.Contains(npc.type))
                        {
                            mp.recorderDivineList.Add(npc.type);
                            p.GetModPlayer<DoorPathwayPlayer>().SyncState();
                            Notify(p, $"◈ 神性记录成功！你刻下了 [{npc.GivenOrTypeName}] 的非凡神性", new Color(220, 180, 255));
                        }
                    }
                }
                else if (npc.lifeMax > 200)
                {
                    int normalChance = mp.currentDoorSequence <= 3 ? 100 : mp.currentDoorSequence <= 4 ? 80 :
                                       mp.currentDoorSequence <= 5 ? 60 : 30;
                    if (Main.rand.Next(100) < normalChance && mp.recorderNormalList.Count < LotMPlayer.RECORDER_NORMAL_MAX)
                    {
                        if (!mp.recorderNormalList.Contains(npc.type))
                        {
                            mp.recorderNormalList.Add(npc.type);
                            p.GetModPlayer<DoorPathwayPlayer>().SyncState();
                            Notify(p, $"◆ 记录成功！[{npc.GivenOrTypeName}] ({mp.recorderNormalList.Count}/{LotMPlayer.RECORDER_NORMAL_MAX})", new Color(220, 200, 120));
                        }
                    }
                }
                else
                {
                    // 凡兽级 100% (但去重)
                    if (mp.recorderNormalList.Count < LotMPlayer.RECORDER_NORMAL_MAX
                        && !mp.recorderNormalList.Contains(npc.type))
                    {
                        mp.recorderNormalList.Add(npc.type);
                        p.GetModPlayer<DoorPathwayPlayer>().SyncState();
                        Notify(p, $"◇ 记录：[{npc.GivenOrTypeName}] ({mp.recorderNormalList.Count}/{LotMPlayer.RECORDER_NORMAL_MAX})", new Color(200, 200, 150));
                    }
                }
            }
        }
    }
}
