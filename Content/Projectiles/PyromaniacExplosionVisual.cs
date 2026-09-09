using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Projectiles
{
    public class PyromaniacExplosionVisual : ModProjectile
    {
        // 指向你的新图片
        public override string Texture => "zhashi/Content/Projectiles/PyromaniacExplosionVisual";

        public override void SetDefaults()
        {
            Projectile.width = 41;  // 你的图片宽度
            Projectile.height = 60; // 你的图片高度
            Projectile.friendly = false; // 纯视觉，不造成伤害
            Projectile.hostile = false;
            Projectile.tileCollide = false; // 穿墙
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 20; // 持续 20 帧 (约0.3秒)
            Projectile.alpha = 0;     // 初始完全不透明
            Projectile.scale = 1.0f;  // 初始大小
        }

        public override void AI()
        {
            // 视觉效果逻辑：
            // 1. 迅速变大 (爆炸扩散感)
            Projectile.scale += 0.1f;

            // 2. 迅速变透明 (渐隐)
            Projectile.alpha += 12;
            if (Projectile.alpha >= 255)
            {
                Projectile.Kill();
            }

            // 3. 缓慢旋转 (增加动感)
            Projectile.rotation += 0.1f;

            // 4. 添加光照
            Lighting.AddLight(Projectile.Center, 1.0f, 0.5f, 0.0f);
        }

        // 使用加法混合绘制，让爆炸看起来像在发光
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() / 2f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(
                texture,
                drawPos,
                null,
                Color.White * ((255 - Projectile.alpha) / 255f), // 根据透明度变暗
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}