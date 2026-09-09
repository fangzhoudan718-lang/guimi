using Terraria;
using Terraria.ModLoader;
using SubworldLibrary;

namespace zhashi.Content.Dimensions
{
    public class OriginCastleGlobalNPC : GlobalNPC
    {
        // 在源堡维度禁用 NPC 生成
        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (SubworldSystem.IsActive<OriginCastle>())
            {
                spawnRate = 0;
                maxSpawns = 0;
            }
        }

        // 强制禁止任何 NPC 在此更新 (如果它们不知为何还是出现了)
        // 这能有效防止 AI_007 崩溃，因为如果 NPC 不活动，就不会运行 AI
        public override bool PreAI(NPC npc)
        {
            if (SubworldSystem.IsActive<OriginCastle>())
            {
                npc.active = false; // 直接清除
                return false;
            }
            return true;
        }
    }
}