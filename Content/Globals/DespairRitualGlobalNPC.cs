using Terraria;
using Terraria.ModLoader;
using zhashi.Content; // 引用 Player 所在的命名空间

namespace zhashi.Content.Globals
{
    public class DespairRitualGlobalNPC : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            // 检查是否是日食 (Eclipse)
            if (Main.eclipse)
            {
                // 遍历所有玩家（处理联机情况，或者只给击杀者加）
                // 这里简化为：只要场上有符合条件的玩家，就给他加进度
                foreach (Player player in Main.ActivePlayers)
                {
                    if (player.dead || !player.active) continue;

                    LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

                    // 只有序列5需要做这个仪式，且没做完的时候才加
                    if (modPlayer.baseDemonessSequence == 5 && modPlayer.despairRitualCount < 50)
                    {
                        modPlayer.despairRitualCount++;

                        // 每杀10个提示一次
                        if (modPlayer.despairRitualCount % 10 == 0)
                        {
                            CombatText.NewText(player.getRect(), new Microsoft.Xna.Framework.Color(150, 0, 150),
                                $"绝望仪式: {modPlayer.despairRitualCount}/50", true);
                        }

                        // 完成提示
                        if (modPlayer.despairRitualCount == 50)
                        {
                            Main.NewText("灾难的养分已足够，绝望魔药正在沸腾...", 255, 0, 255);
                            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.NPCDeath10, player.Center);
                        }
                    }
                }
            }
        }
    }
}