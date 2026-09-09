using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Weapons;

namespace zhashi.Content.Globals
{
    /// <summary>
    /// 序列6 记录官: 击杀有"非凡能力"(可拟态)的敌人时,有概率获得该敌人的"非凡符号".
    /// 借用蠕动饥饿的能力库(IsSupportedNPC_Static): 只有该敌人在能力库里,才能被记录.
    /// </summary>
    public class RecorderGlobalNPC : GlobalNPC
    {
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
                    // 神性级 5%
                    if (Main.rand.NextBool(20) && mp.recorderDivineList.Count < mp.GetRecorderDivineMax())
                    {
                        if (!mp.recorderDivineList.Contains(npc.type))
                        {
                            mp.recorderDivineList.Add(npc.type);
                            if (p.whoAmI == Main.myPlayer)
                            {
                                Main.NewText($"◈ 神性记录成功! 你刻下了 [{npc.GivenOrTypeName}] 的非凡神性", 220, 180, 255);
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item104, p.position);
                            }
                        }
                    }
                }
                else if (npc.lifeMax > 200)
                {
                    // 半神级 30%
                    if (Main.rand.NextBool(3, 10) && mp.recorderNormalList.Count < LotMPlayer.RECORDER_NORMAL_MAX)
                    {
                        if (!mp.recorderNormalList.Contains(npc.type))
                        {
                            mp.recorderNormalList.Add(npc.type);
                            if (p.whoAmI == Main.myPlayer)
                                Main.NewText($"◆ 记录成功! [{npc.GivenOrTypeName}] ({mp.recorderNormalList.Count}/{LotMPlayer.RECORDER_NORMAL_MAX})", 220, 200, 120);
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
                        if (p.whoAmI == Main.myPlayer)
                            Main.NewText($"◇ 记录: [{npc.GivenOrTypeName}] ({mp.recorderNormalList.Count}/{LotMPlayer.RECORDER_NORMAL_MAX})", 200, 200, 150);
                    }
                }
            }
        }
    }
}
