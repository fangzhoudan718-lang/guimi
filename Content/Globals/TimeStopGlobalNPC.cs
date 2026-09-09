using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID; // 需要引用 ID 命名空间
using zhashi.Content.Buffs.Debuffs;

namespace zhashi.Content.Globals
{
    public class TimeStopGlobalNPC : GlobalNPC
    {
        public override bool PreAI(NPC npc)
        {
            // 检查是否有“时间凝固”Buff
            if (npc.HasBuff(ModContent.BuffType<TimeStagnationBuff>()))
            {
                // ==== 智能 Boss 判定 ====
                // 1. 原版 boss 属性
                // 2. 原版判定为类 Boss (如天界柱)
                // 3. 多体生物 (realLife != -1，如蠕虫，必须同步)
                // 4. [兼容性修复] 灾厄等模组的大怪：如果血量超过 5000 且属于模组生物，强制按 Boss 处理（防止亵渎守卫等复杂 AI 崩溃）
                bool isBoss = npc.boss ||
                              NPCID.Sets.ShouldBeCountedAsBoss[npc.type] ||
                              npc.realLife != -1 ||
                              (npc.ModNPC != null && npc.lifeMax > 5000);

                // --- 1. 对于普通小怪：完全静止 (时间停止) ---
                if (!isBoss)
                {
                    npc.velocity = Vector2.Zero; // 速度归零
                    npc.position = npc.oldPosition; // 锁死位置
                    npc.frameCounter = 0; // 停止动画

                    // 对于普通小怪，直接跳过 AI 是安全的
                    return false;
                }

                // --- 2. 对于 Boss/大怪/灾厄生物：极度减速 (时间变缓) ---
                // 这种方式允许 AI 运行，但极慢，保证了绘制数据（如数组）能正常更新，不会崩溃
                else
                {
                    // 只有每 10 帧才执行一次 AI
                    if (Main.GameUpdateCount % 10 != 0)
                    {
                        // 抵消 90% 的移动，模拟慢动作
                        npc.position -= npc.velocity * 0.9f;
                        return false; // 跳过这次 AI 思考
                    }
                    // 每 10 帧放行一次，让它跑一次 AI，更新状态
                }
            }
            return true; // 没有 Buff 则正常行动
        }

        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (npc.HasBuff(ModContent.BuffType<TimeStagnationBuff>()))
            {
                drawColor = Color.Gray;
            }
        }
    }
}