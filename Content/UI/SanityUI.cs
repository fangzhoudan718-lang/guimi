using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using zhashi.Content; // 引用 LotMPlayer

namespace zhashi.Content.UI
{
    public class SanityUI : UIState
    {
        private UIText textDisplay;

        public override void OnInitialize()
        {
            // 创建一个纯文本控件
            // 参数：初始文本, 文本缩放比例 (1.2f 会稍微大一点，看得清)
            textDisplay = new UIText("理智: ???", 1.2f);

            // --- 设置位置 ---
            // Left/Top: 距离屏幕左上角的像素距离
            // 你可以根据需要调整 Top 的值，避免遮挡原版 UI
            textDisplay.Left.Set(20, 0f);
            textDisplay.Top.Set(120, 0f); // 大概在原有血条下方

            Append(textDisplay);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // 获取玩家数据
            var player = Main.LocalPlayer;
            if (player == null || !player.active) return;

            var lotmPlayer = player.GetModPlayer<LotMPlayer>();

            // 计算数值
            int current = (int)lotmPlayer.sanityCurrent;
            int max = (int)lotmPlayer.sanityMax;
            float ratio = lotmPlayer.sanityCurrent / lotmPlayer.sanityMax;

            // --- 极简风格文本 ---
            // 直接显示 "理智: 50 / 100"
            textDisplay.SetText($"San: {current} / {max}");

            // --- 动态变色 ---
            // 越疯狂（数值越低），颜色越红
            if (ratio < 0.2f)
                textDisplay.TextColor = Color.Red; // 极度危险
            else if (ratio < 0.5f)
                textDisplay.TextColor = Color.Orange; // 危险
            else
                textDisplay.TextColor = Color.LightGreen; // 安全
        }
    }
}