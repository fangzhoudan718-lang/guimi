using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Buffs.Debuffs; 

namespace zhashi.Content.Systems
{
    public class MisfortuneMageRitual : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            // 判定：击杀机械骷髅王 (Skeletron Prime)
            if (npc.type == NPCID.SkeletronPrime)
            {
                // 遍历所有玩家
                foreach (Player player in Main.player)
                {
                    if (player.active)
                    {
                        var modPlayer = player.GetModPlayer<LotMPlayer>();

                        // 条件：命运途径序列5赢家 + 身上有厄运缠身Buff (说明佩戴了厄运之骰)
                        if (modPlayer.currentWheelSequence == 5 && player.HasBuff(ModContent.BuffType<ExtremeBadLuckBuff>()))
                        {
                            modPlayer.misfortuneMageRitualComplete = true; // 仪式完成！

                            if (player.whoAmI == Main.myPlayer)
                            {
                                Main.NewText("你在机械的毁灭与极度的厄运中，抓住了那一丝命运的生机！", 255, 215, 0);
                                Main.NewText("【序列4：厄运法师】晋升仪式已完成！", 255, 50, 255);
                            }
                        }
                    }
                }
            }
        }
    }
}