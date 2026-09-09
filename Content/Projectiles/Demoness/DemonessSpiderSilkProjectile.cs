using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Projectiles.Demoness
{
    public class DemonessSpiderSilkProjectile : ModProjectile
    {
        // --- 核心修复：借用原版“蜘蛛网”的贴图 ---
        // 14 是原版 Web (蜘蛛网) 弹幕的ID
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Web;

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.aiStyle = 0;
        }

        public override void AI()
        {
            // 重力效果
            Projectile.velocity.Y += 0.2f;
            Projectile.rotation += 0.1f;

            // 白色蛛丝粒子
            if (Main.rand.NextBool(3))
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Web, 0, 0, 100, default, 1f);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!target.boss)
            {
                target.AddBuff(BuffID.Webbed, 120);
            }
            else
            {
                target.AddBuff(BuffID.Slow, 300);
                target.AddBuff(BuffID.Poisoned, 300);
            }
        }

        public override void Kill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Web, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), 0, default, 1.5f);
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10, Projectile.position);
        }
    }
}