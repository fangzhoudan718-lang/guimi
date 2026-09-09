using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Items.Weapons.Fool
{
    public class PaperCardProjectile : ModProjectile
    {
        // 不需要 Texture，自动读取同名图片

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.aiStyle = 1;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 600;
            Projectile.light = 0.5f;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            AIType = ProjectileID.Shuriken;
        }

        public override void AI()
        {
            // 飞行特效
            if (Main.rand.NextBool(3))
            {
                // 这里用的是 Confetti，是正确的
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Confetti, Projectile.velocity.X * 0.1f, Projectile.velocity.Y * 0.1f, 150, default, 0.8f);
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item10, Projectile.position);

            for (int i = 0; i < 5; i++)
            {
                // 【核心修复】将 DustID.Paper 改为 DustID.Confetti
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Confetti, 0f, 0f, 0, default, 1f);
            }
        }
    }
}