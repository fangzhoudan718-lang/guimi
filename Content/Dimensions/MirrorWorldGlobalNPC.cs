using Terraria;
using Terraria.ModLoader;
using SubworldLibrary;
using zhashi.Content.Dimensions;

namespace zhashi.Content.Dimensions
{
    public class MirrorWorldGlobalNPC : GlobalNPC
    {
        public override bool PreAI(NPC npc)
        {
            // 检测：如果当前处于镜中世界
            if (SubworldSystem.IsActive<MirrorWorld>())
            {
                // 检测：如果是城镇NPC (包括向导、城镇宠物、或者你的 DogNPC 如果它也是城镇NPC的话)
                // 或者 AI 类型是 7 (城镇NPC的通用AI)
                if (npc.townNPC || npc.aiStyle == 7)
                {
                    // 强制使其消失，防止运行寻路代码导致崩溃
                    npc.active = false;
                    return false; // 阻止原版 AI 运行
                }
            }
            return true;
        }
    }
}