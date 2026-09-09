using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.ID;

namespace zhashi.Content.UI
{
    public class LotMMenu : ModMenu
    {

        public override string DisplayName => "诡秘之主 (Lord of the Mysteries)";


        public override Asset<Texture2D> Logo => ModContent.Request<Texture2D>("zhashi/Assets/Textures/MenuLogo");


        public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Music/MenuTheme");


        public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoDrawCenter, ref float logoRotation, ref float logoScale, ref Color drawColor)
        {

            Texture2D bgTexture = ModContent.Request<Texture2D>("zhashi/Assets/Textures/MenuBackground").Value;


            Vector2 screenCenter = new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f);
            Vector2 textureCenter = new Vector2(bgTexture.Width / 2f, bgTexture.Height / 2f);

            spriteBatch.Draw(
                bgTexture,
                new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                Color.White
            );
            return true;
        }
    }
}