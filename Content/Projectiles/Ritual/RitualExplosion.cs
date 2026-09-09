using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using zhashi.Content.DamageClasses;
using Microsoft.Xna.Framework;

namespace zhashi.Content.Projectiles.Ritual
{
    public class RitualExplosion : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.DamageType = ModContent.GetInstance<RitualDamage>();
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            // 【关键】这一步非常重要！
            // 当我们在 BrushProjectile 里修改了 scale 之后，碰撞箱大小不会自动变。
            // 我们必须手动重置碰撞箱大小，并保持中心点不变。
            if (Projectile.scale != 1f && Projectile.width == 40) // 只有第一次需要调整
            {
                Projectile.Resize((int)(40 * Projectile.scale), (int)(40 * Projectile.scale));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 如果是大爆炸，产生更剧烈的特效
            if (Projectile.scale > 1.5f)
            {
                for (int i = 0; i < 3; i++)
                {
                    Dust.NewDust(target.position, target.width, target.height, DustID.SolarFlare, 0, 0, 100, default, 1.5f);
                }
            }
        }
    }
}