using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace zhashi.Content.UI.Profiles
{
    // 抽象基类：所有 NPC 的档案都继承自它
    public class GalgameProfile
    {
        // 1. 主题色配置 (背景色, 边框色, 名字色)
        public virtual Color BackgroundColor => new Color(10, 10, 30) * 0.98f; // 默认蓝
        public virtual Color BorderColor => Color.CornflowerBlue * 0.8f;
        public virtual Color NameColor => Color.White;

        // 2. 立绘路径 (默认用原版 ID 查找，特殊 NPC 可重写)
        public virtual string HeadTexture => null; // null 代表使用原版头像
        public virtual string StandingTexture => null; // null 代表无立绘

        public virtual float OffsetX => -100f; // 默认值
        public virtual float OffsetY => 50f;   // 默认值

        // 3. 生成对话文本
        public virtual string GetDialogue(NPC npc)
        {
            return Main.npcChatText; // 默认返回原版对话
        }

        public virtual void SetupButtons(NPC npc, GalgameDialogueUI ui)
        {
            // 默认按钮
            ui.AddButton("离开", () => {
                Main.LocalPlayer.SetTalkNPC(-1);
                ModContent.GetInstance<GalgameUISystem>().CloseUI();
            });
        }

        // 辅助：获取好感度 (方便子类调用)
        protected int GetFavor(int type) => FavorabilitySystem.GetFavorability(type);
    }
}