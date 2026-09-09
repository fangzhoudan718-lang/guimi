using Terraria;
using Terraria.ModLoader;
using zhashi.Content; // 引用 Player

namespace zhashi.Content.Globals
{
    public class QuestGlobalNPC : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            // 获取击杀者的 Player 实例
            // 注意：如果是多人模式，可能需要判断 npc.lastInteraction 等，这里写最简单的单人/主机逻辑
            Player player = Main.LocalPlayer;

            if (player.active && !player.dead)
            {
                var story = player.GetModPlayer<LotMStoryPlayer>();

                // 如果玩家有任务 && 是杀怪任务 && 还没做完
                if (story.HasDailyQuest && story.QuestType == 1 && story.QuestCurrentAmount < story.QuestRequiredAmount)
                {
                    // 判断怪物种类 (处理史莱姆可能有多种ID的情况)
                    bool isTarget = false;
                    if (npc.type == story.QuestTargetID) isTarget = true;
                    // 特殊处理：如果任务是普通僵尸，打死别的种类僵尸也算（可选优化）

                    if (isTarget)
                    {
                        story.QuestCurrentAmount++;
                        if (story.QuestCurrentAmount >= story.QuestRequiredAmount)
                        {
                            Main.NewText("每日委托：目标已达成！回去找邓恩报告。", 50, 255, 50);
                        }
                        else
                        {
                            // 提示进度 (可选，不想刷屏可以注释掉)
                            // Main.NewText($"任务进度: {story.QuestCurrentAmount}/{story.QuestRequiredAmount}", 200, 200, 200);
                        }
                    }
                }
            }
        }
    }
}