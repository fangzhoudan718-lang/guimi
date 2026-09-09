using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Globals
{
    /// <summary>
    /// 序列1 巨蛇 仪式: 在序列2状态下击败月亮领主.
    /// </summary>
    public class SerpentRitualGlobalNPC : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            if (npc.type != NPCID.MoonLordCore) return;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            // 给所有在线且序列2的玩家记仪式
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.active) continue;
                var mp = p.GetModPlayer<LotMPlayer>();
                if (mp.baseWheelSequence == 2 && !mp.serpentRitualBeatMoonLord)
                {
                    mp.serpentRitualBeatMoonLord = true;
                    if (p.whoAmI == Main.myPlayer)
                    {
                        Main.NewText("【序列1·巨蛇仪式】 你已在先知形态下见证月之主的陨落...", 200, 200, 255);
                    }
                }
            }
        }
    }
}
