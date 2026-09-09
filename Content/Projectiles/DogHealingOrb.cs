using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Projectiles
{
    // === 自定义治疗球弹幕 ===
    public class DogHealingOrb : ModProjectile
    {
        // 复用原版吸血鬼飞刀弹幕的贴图 (虽然它主要是靠粒子特效)
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.VampireHeal;

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = true;
            Projectile.tileCollide = false; // 穿墙
            Projectile.timeLeft = 600;      // 10秒后消失
            Projectile.alpha = 255;         // 透明 (靠粒子显示)
            Projectile.extraUpdates = 2;    // 高速移动
        }

        public override void AI()
        {
            // 1. 视觉特效 (复刻原版吸血球的绿色粒子)
            for (int i = 0; i < 2; i++)
            {
                // 绿色/生命色粒子
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Terra, 0f, 0f, 100, default, 1.2f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 0.3f;
                Main.dust[d].velocity += Projectile.velocity * 0.5f;
            }

            // 2. 追踪主人
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            // 飞向主人的算法
            float speed = 15f;
            float inertia = 15f;
            Vector2 direction = (owner.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
            Projectile.velocity = (Projectile.velocity * (inertia - 1) + direction * speed) / inertia;

            // 3. 碰撞检测与治疗
            if (Projectile.getRect().Intersects(owner.getRect()))
            {
                int healAmount = Projectile.damage; // 取弹幕伤害作为治疗量

                // 执行治疗
                owner.statLife += healAmount;
                owner.HealEffect(healAmount);
                if (owner.statLife > owner.statLifeMax2) owner.statLife = owner.statLifeMax2;

                // 消失特效
                for (int i = 0; i < 15; i++)
                {
                    int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Terra, 0f, 0f, 50, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 1.5f;
                }

                Projectile.Kill();
            }
        }
    }
}