using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Projectiles.Demoness
{
    public class DemonessSpiderSilk : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WebSpit;

        public override void SetDefaults()
        {
            // 【修改1】增大碰撞箱，更容易打中
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            // 【修改2】特效增强：频率提高，尺寸变大
            // 每一帧都产生粒子，而不是只有一半几率
            int dustType = Main.rand.NextBool() ? DustID.Web : DustID.PinkSlime;

            // Scale 从 0.8f 改为 1.5f (变大近一倍)
            Dust d = Dust.NewDustPerfect(Projectile.Center, dustType, Vector2.Zero, 100, default, 1.5f);
            d.noGravity = true;
            d.velocity *= 0.5f; // 让粒子稍微飘散一点
        }

        private NPC FindClosestNPC(float maxDetectDistance)
        {
            NPC closestNPC = null;
            float sqrMaxDetectDistance = maxDetectDistance * maxDetectDistance;
            for (int k = 0; k < Main.maxNPCs; k++)
            {
                NPC target = Main.npc[k];
                if (target.CanBeChasedBy())
                {
                    float sqrDistanceToTarget = Vector2.DistanceSquared(target.Center, Projectile.Center);
                    if (sqrDistanceToTarget < sqrMaxDetectDistance)
                    {
                        sqrMaxDetectDistance = sqrDistanceToTarget;
                        closestNPC = target;
                    }
                }
            }
            return closestNPC;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 【修改3】补全剧毒和虚弱效果
            target.AddBuff(BuffID.Poisoned, 300); // 剧毒 5秒
            target.AddBuff(BuffID.Weak, 300);     // 虚弱 5秒

            // 原有的蛛丝控制
            if (!target.boss) target.AddBuff(BuffID.Webbed, 120);
            else target.AddBuff(BuffID.Slow, 180);

            // 【修改4】命中特效：数量翻倍，尺寸变大
            for (int i = 0; i < 20; i++) // 10 -> 20
            {
                // Scale 1.2f -> 2.0f
                Dust.NewDust(target.position, target.width, target.height, DustID.Web, 0, 0, 0, default, 2.0f);
            }
        }
    }
}