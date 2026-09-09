using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Projectiles
{
    public class PyromaniacFireball : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.BallofFire;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.light = 0.8f;
            Projectile.tileCollide = true;

            // 确保不使用原版受重力的 AI Style
            Projectile.aiStyle = -1;
        }

        public override void AI()
        {
            // --- 飞行特效：岩浆拖尾 ---
            for (int i = 0; i < 3; i++)
            {
                // 随机位置偏移，让拖尾更粗
                Vector2 dustPos = Projectile.Center + Main.rand.NextVector2Circular(4, 4);
                // 混合使用普通火(6)和日耀火(259)
                int dustType = Main.rand.NextBool() ? DustID.Torch : DustID.SolarFlare;

                int d = Dust.NewDust(dustPos, 0, 0, dustType, 0, 0, 100, default, 1.5f);
                Main.dust[d].velocity = -Projectile.velocity * 0.5f; // 向后喷射
                Main.dust[d].noGravity = true;
            }

            // 【已删除】重力逻辑
            // 原来的代码在这里加了 velocity.Y += 0.2f，导致它不是直线的
            // 现在删掉了，它就会一直按发射时的方向直线飞行

            // 稍微旋转一下贴图，增加动感 (可选)
            Projectile.rotation += 0.3f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 300);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);

            // === 爆炸特效：火焰冲击波 ===
            Vector2 center = Projectile.Center;

            // 1. 核心爆炸团
            for (int i = 0; i < 30; i++)
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0, 0, 100, default, 3f);
                Main.dust[d].velocity *= 3f;
                Main.dust[d].noGravity = true;
            }

            // 2. 扩散的火焰圆环 (华丽点)
            int dustCount = 40;
            for (int i = 0; i < dustCount; i++)
            {
                // 计算圆周上的速度向量
                Vector2 velocity = Vector2.UnitX.RotatedBy(MathHelper.ToRadians(360f / dustCount * i)) * 6f;

                // 生成无重力的高亮粒子
                int d = Dust.NewDust(center, 0, 0, DustID.SolarFlare, velocity.X, velocity.Y, 0, default, 2f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity = velocity; // 强制设置速度，形成完美的圆
            }

            // 3. 烟雾残留
            for (int i = 0; i < 20; i++)
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0, 0, 100, default, 1.5f);
                Main.dust[d].velocity *= 0.5f;
            }

            // 范围伤害
            if (Projectile.owner == Main.myPlayer)
            {
                int explosionRadius = 100;
                Rectangle explosionRect = new Rectangle((int)center.X - explosionRadius, (int)center.Y - explosionRadius, explosionRadius * 2, explosionRadius * 2);
                foreach (NPC npc in Main.npc)
                {
                    if (npc.active && !npc.friendly && !npc.dontTakeDamage && npc.getRect().Intersects(explosionRect))
                    {
                        int damage = (int)(Projectile.damage * 0.7f);
                        npc.SimpleStrikeNPC(damage, 0);
                        npc.AddBuff(BuffID.OnFire3, 300);
                    }
                }
            }
        }
    }
}