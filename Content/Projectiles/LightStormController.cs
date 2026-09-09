using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Projectiles
{
    public class LightStormController : ModProjectile
    {
        public override string Texture => "zhashi/Content/Projectiles/MagicCircle";

        private const int FRAME_COUNT = 24;
        private float scalePulse = 0f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = FRAME_COUNT;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 0;
            Projectile.hide = false;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Player owner = Main.player[Projectile.owner];

            scalePulse += 0.05f;

            // 帧动画播放
            if (++Projectile.frameCounter >= 2)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame >= FRAME_COUNT)
                {
                    Projectile.frame = 0;
                }
            }

            // 音效
            if (Projectile.timeLeft == 359)
            {
                SoundEngine.PlaySound(SoundID.Item122, Projectile.Center);
                SoundEngine.PlaySound(SoundID.DD2_EtherianPortalOpen, Projectile.Center);
            }

            // 生成剑雨逻辑
            float outerRadius = 280f;
            if (Projectile.timeLeft % 6 == 0 && Projectile.owner == Main.myPlayer && Projectile.timeLeft < 340)
            {
                if (Projectile.timeLeft < 340)
                {
                    Vector2 spawnPos = Projectile.Center + Main.rand.NextVector2Circular(outerRadius, outerRadius * 0.6f);
                    spawnPos.Y -= 700f;
                    Vector2 velocity = new Vector2(0, 14f);

                    // 【修复点】确保 knockBack 属性的大写 B 是正确的
                    // Projectile.knockBack 是当前控制器投射物的击退力属性
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPos, velocity, ModContent.ProjectileType<DawnBladeProjectile>(), Projectile.damage, Projectile.knockBack, owner.whoAmI);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;

            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle sourceRect = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = sourceRect.Size() / 2f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            float animScale = 1f;
            if (Projectile.timeLeft > 340) animScale = MathHelper.SmoothStep(0f, 1f, (360 - Projectile.timeLeft) / 20f);
            else if (Projectile.timeLeft < 20) animScale = MathHelper.SmoothStep(0f, 1f, Projectile.timeLeft / 20f);

            float pulse = 1f + (float)Math.Sin(scalePulse) * 0.05f;
            float finalScale = 2.5f * pulse * animScale;

            if (animScale < 0.01f) return false;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            Color drawColor = Color.White;
            drawColor *= 0.9f * animScale;

            Main.EntitySpriteDraw(
                texture,
                drawPos,
                sourceRect,
                drawColor,
                0f,
                origin,
                finalScale,
                SpriteEffects.None,
                0
            );

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}