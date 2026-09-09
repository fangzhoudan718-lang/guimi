using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.ID;

namespace zhashi.Content.Projectiles
{
    public class TwilightStormProjectile : ModProjectile
    {
        public override string Texture => "zhashi/Content/Projectiles/TwilightStormProjectile";

        private bool hasInitialized = false;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;

            // 默认时间，会被初始化逻辑覆盖
            Projectile.timeLeft = 60;

            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override void AI()
        {
            // === 1. 初始化逻辑：计算持续时间 ===
            if (!hasInitialized)
            {
                float consumedSpirit = Projectile.ai[0];

                // 计算持续时间
                // 基准：1000 灵性 = 180帧 (3秒)
                int duration = (int)((consumedSpirit / 1000f) * 180f);

                // 【修改】允许极短时间
                // 哪怕只有1点灵性，也给5帧的时间闪一下
                if (duration < 5) duration = 5;

                // 也不要太长，上限设为 1分钟 (3600帧)
                if (duration > 3600) duration = 3600;

                Projectile.timeLeft = duration;
                hasInitialized = true;
            }

            // === 2. 旋转与特效 ===
            Projectile.rotation += 0.02f;

            // 粒子特效范围
            float visualRadius = 400f;

            if (Main.rand.NextBool(2))
            {
                Vector2 spawnPos = Projectile.Center + Main.rand.NextVector2Circular(visualRadius, visualRadius);
                int dust = Dust.NewDust(spawnPos, 0, 0, DustID.OrangeTorch, 0, 0, 100, default, 2f);
                Main.dust[dust].noGravity = true;

                Vector2 toCenter = Projectile.Center - spawnPos;
                toCenter.Normalize();
                Vector2 tangent = toCenter.RotatedBy(MathHelper.ToRadians(90));
                Main.dust[dust].velocity = tangent * 15f + toCenter * 3f;
            }
            Lighting.AddLight(Projectile.Center, 2.0f, 1.0f, 0.5f);
        }

        // === 3. 自定义圆形碰撞检测 ===
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // 伤害半径约 450 (匹配图片视觉大小)
            float collisionRadius = 450f;
            if (targetHitbox.Distance(Projectile.Center) < collisionRadius)
            {
                return true;
            }
            return false;
        }

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
                Color.White,
                Projectile.rotation,
                origin,
                1.5f, // 缩放比例
                SpriteEffects.None,
                0
            );

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Daybreak, 300);
            target.AddBuff(BuffID.BetsysCurse, 300);
            target.AddBuff(BuffID.MoonLeech, 300);
        }
    }
}