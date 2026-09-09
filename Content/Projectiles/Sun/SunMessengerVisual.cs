using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent;

namespace zhashi.Content.Projectiles.Sun
{
    // 这是一个纯视觉弹幕，用于在变身时显示为一个“太阳”
    public class SunMessengerVisual : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.SolarWhipSwordExplosion;

        public override void SetDefaults()
        {
            Projectile.width = 100;
            Projectile.height = 100;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.timeLeft = 2; // 只要玩家保持状态，它就一直存在
            Projectile.scale = 1.0f;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead || !owner.GetModPlayer<LotMPlayer>().isSunMessenger)
            {
                Projectile.Kill();
                return;
            }

            // 1. 始终跟随玩家
            Projectile.Center = owner.Center;
            Projectile.timeLeft = 2; // 续命

            // 2. 旋转与呼吸效果
            Projectile.rotation += 0.05f;
            Projectile.scale = 1.5f + (float)System.Math.Sin(Main.GameUpdateCount * 0.1f) * 0.1f;

            // 3. 喷射日珥粒子
            if (Main.GameUpdateCount % 2 == 0)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(40 * Projectile.scale, 40 * Projectile.scale);
                Dust d = Dust.NewDustPerfect(Projectile.Center + offset, DustID.SolarFlare, offset.SafeNormalize(Vector2.Zero) * 4f, 0, default, 2f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() / 2f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.ZoomMatrix);

            // 绘制多层光晕
            Main.spriteBatch.Draw(texture, drawPos, null, Color.OrangeRed, -Projectile.rotation, origin, Projectile.scale * 1.8f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(texture, drawPos, null, Color.Gold, Projectile.rotation, origin, Projectile.scale * 1.2f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(texture, drawPos, null, Color.White, 0f, origin, Projectile.scale * 0.6f, SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.ZoomMatrix);

            return false;
        }
    }
}