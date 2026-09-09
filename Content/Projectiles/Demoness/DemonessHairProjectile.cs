using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Projectiles.Demoness
{
    public class DemonessHairProjectile : ModProjectile
    {
        // --- 核心修复：借用原版“暗影光束”的贴图，防止报错 ---
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.ShadowBeamFriendly;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;

            // 把弹幕本身设为隐形，因为我们主要靠粒子(Dust)来表现头发
            // 这样就不会看到一个奇怪的紫色光束在飞，而是只看到黑色粒子
            Projectile.alpha = 255;
        }

        public override void AI()
        {
            // 1. 视觉效果：黑色粒子拖尾，模拟发丝
            // 既然本体隐形了，粒子就要密集一点
            for (int i = 0; i < 2; i++)
            {
                int dustId = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Demonite, 0, 0, 100, default, 1.2f);
                Main.dust[dustId].noGravity = true;
                Main.dust[dustId].velocity *= 0.1f; // 粒子几乎不动，形成连贯的线条
            }

            // 2. 自动追踪最近的敌人
            float maxDetectRadius = 400f;
            NPC closestNPC = null;
            float closestDist = maxDetectRadius;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.CanBeChasedBy() && !npc.friendly)
                {
                    float dist = Projectile.Distance(npc.Center);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestNPC = npc;
                    }
                }
            }

            if (closestNPC != null)
            {
                Vector2 direction = (closestNPC.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                float speed = 12f;
                float inertia = 15f; // 稍微减小惯性，让它转弯更灵活，像鞭子

                Projectile.velocity = (Projectile.velocity * (inertia - 1) + direction * speed) / inertia;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.ShadowFlame, 180);
        }
    }
}