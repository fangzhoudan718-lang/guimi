using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria.ModLoader;
using Terraria.ID; // 引用ID库
using zhashi;

namespace zhashi.Content.UI
{
    public class PathwayUI : UIState
    {
        private UIImage icon;
        private UIPanel mainPanel;

        public override void OnInitialize()
        {
            mainPanel = new UIPanel();
            mainPanel.SetPadding(0);

            mainPanel.Left.Set(20f, 0f);
            mainPanel.Top.Set(260f, 0f);

            mainPanel.Width.Set(50f, 0f);
            mainPanel.Height.Set(50f, 0f);
            mainPanel.BackgroundColor = new Color(0, 0, 0, 0); // 透明背景
            mainPanel.BorderColor = new Color(0, 0, 0, 0);     // 透明边框

            Texture2D texture = null;
            if (ModContent.RequestIfExists<Texture2D>("zhashi/Content/Items/Materials/ExtraordinaryBlood", out var asset))
            {
                texture = asset.Value;
            }
            else
            {
                texture = Main.Assets.Request<Texture2D>("Images/Item_" + ItemID.ManaCrystal).Value;
            }

            icon = new UIImage(texture);
            icon.Width.Set(50, 0f);
            icon.Height.Set(50, 0f);
            icon.ImageScale = 1.0f; // 不缩放
            mainPanel.Append(icon);

            Append(mainPanel);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            if (mainPanel.IsMouseHovering)
            {
                Player player = Main.LocalPlayer;
                LotMPlayer modPlayer = player.GetModPlayer<Content.LotMPlayer>();
                string text = GetSequenceText(modPlayer);

                // 绘制文字提示
                Main.instance.MouseText(text);
            }
        }

        private string GetSequenceText(Content.LotMPlayer modPlayer)
        {
            if (modPlayer.currentHunterSequence < 10)
            {
                return $"【猎人途径】\n当前序列: {modPlayer.currentHunterSequence}\n灵性: {(int)modPlayer.spiritualityCurrent} / {modPlayer.spiritualityMax}";
            }
            else if (modPlayer.currentSequence < 10)
            {
                return $"【巨人途径】\n当前序列: {modPlayer.currentSequence}\n灵性: {(int)modPlayer.spiritualityCurrent} / {modPlayer.spiritualityMax}";
            }
            return "【凡人】\n未服用魔药";
        }
    }
}