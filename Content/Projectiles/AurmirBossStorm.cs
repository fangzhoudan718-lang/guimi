using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.ID;

namespace zhashi.Content.Projectiles
{
    // 这是 Boss 专用的版本，敌对，不伤自己
    public class AurmirBossStorm : ModProjectile
    {
        // 直接借用玩家技能的贴图，不需要新图
        public override string Texture => "zhashi/Content/Projectiles/TwilightStormProjectile";

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;

            // 【关键区别】
            Projectile.friendly = false; // 不打怪
            Projectile.hostile = true;   // 打玩家

            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 180;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            // 旋转
            Projectile.rotation += 0.02f;

            // 粒子特效 (和玩家版一样)
            float damageRadius = 250f;
            if (Main.rand.NextBool(2))
            {
                Vector2 spawnPos = Projectile.Center + Main.rand.NextVector2Circular(damageRadius, damageRadius);
                int dust = Dust.NewDust(spawnPos, 0, 0, DustID.OrangeTorch, 0, 0, 100, default, 2f);
                Main.dust[dust].noGravity = true;

                Vector2 toCenter = Projectile.Center - spawnPos;
                toCenter.Normalize();
                Vector2 tangent = toCenter.RotatedBy(MathHelper.ToRadians(90));
                Main.dust[dust].velocity = tangent * 10f + toCenter * 2f;
            }
            Lighting.AddLight(Projectile.Center, 2.0f, 1.0f, 0.5f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() / 2f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(texture, drawPos, null, Color.White, Projectile.rotation, origin, 1.5f, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        // 伤害判定
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionRadius = 450f;
            if (targetHitbox.Distance(Projectile.Center) < collisionRadius) return true;
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Daybreak, 300);
            target.AddBuff(BuffID.BetsysCurse, 300);
            target.AddBuff(BuffID.MoonLeech, 300);
        }
    }
}