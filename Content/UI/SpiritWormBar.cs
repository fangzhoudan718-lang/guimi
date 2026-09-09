using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using zhashi.Content;

namespace zhashi.Content.UI
{
    public class SpiritWormBar : UIState
    {
        private UIText text;
        private UIElement area;

        public override void OnInitialize()
        {
            // 1. 设定区域
            area = new UIElement();

            area.Left.Set(Main.screenWidth / 2 - 100, 0f);
            area.Top.Set(Main.screenHeight - 50, 0f);
            area.Width.Set(200, 0f);
            area.Height.Set(30, 0f);

            text = new UIText("0/0", 0.8f);
            text.Width.Set(200, 0f);
            text.Height.Set(30, 0f);
            text.Top.Set(0, 0f); // 直接紧贴区域顶部
            text.HAlign = 0.5f; // 居中

            area.Append(text);
            Append(area);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            var player = Main.LocalPlayer;
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            if (modPlayer.currentFoolSequence > 4 || player.dead || !player.active)
                return;

            int maxWorms = 50;
            if (modPlayer.currentFoolSequence <= 2) maxWorms = 1200;
            else if (modPlayer.currentFoolSequence <= 3) maxWorms = 600;

            int currentWorms = modPlayer.spiritWorms;

            text.SetText($"灵之虫: {currentWorms} / {maxWorms}");

            if (currentWorms < maxWorms * 0.2f)
                text.TextColor = Color.Red;
            else
                text.TextColor = Color.White;

            base.Draw(spriteBatch);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (area != null)
            {
                area.Left.Set(Main.screenWidth / 2 - 100, 0f);
                area.Top.Set(Main.screenHeight - 50, 0f);
            }
        }
    }
}