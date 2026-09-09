using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Projectiles.Demoness
{
    public class DespairIceProjectile : ModProjectile
    {
        // 借用“寒霜九头蛇”的冰弹贴图，或者“暴雪法杖”的冰晶
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Blizzard;

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 3; // 穿透
            Projectile.timeLeft = 300;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            // 视觉特效：黑色火焰 + 冰霜
            if (Main.rand.NextBool(2))
            {
                // 黑火
                Dust d1 = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, 0, 0, 100, default, 1.5f);
                d1.noGravity = true;

                // 冰霜
                Dust d2 = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.IceTorch, 0, 0, 100, default, 1.5f);
                d2.noGravity = true;
                d2.velocity *= 0.5f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 双重Debuff：黑焰 + 冻伤
            target.AddBuff(BuffID.ShadowFlame, 300); // 5秒黑火
            target.AddBuff(BuffID.Frostburn2, 300);  // 5秒强力冻伤

            // 绝望效果：削弱防御
            target.AddBuff(BuffID.Ichor, 180); // 降低防御
        }

        public override void Kill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27, Projectile.position); // 冰碎声
            for (int i = 0; i < 20; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, 0, 0, 100, default, 1.5f);
            }
        }
    }
}