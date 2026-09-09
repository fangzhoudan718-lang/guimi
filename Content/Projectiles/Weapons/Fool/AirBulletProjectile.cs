using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Projectiles.Weapons.Fool
{
    public class AirBulletProjectile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Bullet; // 随便用个原版图，反正我们要隐藏它

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.aiStyle = 1;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic; // 魔术师是法系
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.alpha = 255; // 【关键】完全透明，不可见
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 2; // 极快速度，肉眼难辨
            AIType = ProjectileID.Bullet;
        }

        public override void AI()
        {
            // 虽然本体看不见，但为了打击感，加一点点空气扭曲的粒子
            if (Main.rand.NextBool(5))
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Cloud, 0, 0, 150, default, 0.5f);
                Main.dust[d].velocity *= 0.1f;
                Main.dust[d].noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            // 命中时的空气爆破效果
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0, 0, 0, default, 1f);
            }
        }
    }
}