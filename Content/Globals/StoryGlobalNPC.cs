using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content; // 引用 LotMStoryPlayer 所在的命名空间

namespace zhashi.Content.Globals
{
    public class StoryGlobalNPC : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            // 监听史莱姆王死亡
            if (npc.type == NPCID.KingSlime)
            {
                // 遍历所有玩家（处理联机）
                foreach (var player in Main.ActivePlayers)
                {
                    // === 关键：获取 LotMStoryPlayer ===
                    LotMStoryPlayer story = player.GetModPlayer<LotMStoryPlayer>();

                    // 如果剧情还处在阶段0，就推进到1
                    if (story.StoryStage == 0)
                    {
                        story.StoryStage = 1;
                        Main.NewText("你展现了非凡的实力，邓恩·史密斯对你刮目相看。", 100, 200, 255);
                    }
                }
            }
        }
    }
}