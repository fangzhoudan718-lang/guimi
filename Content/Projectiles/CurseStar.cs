using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Projectiles
{
    /// <summary>
    /// 命运诅咒 - 砸玩家的星星 (敌方弹幕).
    /// 视觉上模仿原版坠落星, 但是 hostile + 不可捡 + 撞地消失.
    /// </summary>
    public class CurseStar : ModProjectile
    {
        // 复用原版坠落星贴图
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.FallingStar;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 600;
            Projectile.alpha = 0;
            Projectile.scale = 1.2f;
        }

        public override void AI()
        {
            // 旋转跟随速度方向
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // 加速度: 自由落体感
            if (Projectile.velocity.Y < 24f)
                Projectile.velocity.Y += 0.35f;

            // 星光拖尾
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center,
                    DustID.YellowTorch, Vector2.Zero, 0, default, 1.3f);
                d.noGravity = true;
                d.fadeIn = 1.2f;
            }
            if (Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center,
                    DustID.GoldCoin, Vector2.Zero, 0, default, 1.0f);
                d.noGravity = true;
            }

            // 持续金色光照
            Lighting.AddLight(Projectile.Center, 0.7f, 0.6f, 0.3f);
        }

        public override void OnKill(int timeLeft)
        {
            // 落地爆炸
            for (int i = 0; i < 12; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center,
                    DustID.YellowTorch, Main.rand.NextVector2Circular(4f, 4f), 0, default, 1.4f);
                d.noGravity = true;
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10, Projectile.position);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 使用更亮的金色绘制(覆盖原版颜色)
            Texture2D tex = TextureAssets.Projectile[ProjectileID.FallingStar].Value;
            Main.spriteBatch.Draw(tex,
                Projectile.Center - Main.screenPosition,
                null, new Color(255, 220, 100),
                Projectile.rotation,
                tex.Size() / 2f,
                Projectile.scale,
                SpriteEffects.None, 0f);
            return false;
        }
    }
}
