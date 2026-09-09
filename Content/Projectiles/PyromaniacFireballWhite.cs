using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Projectiles
{
    public class PyromaniacFireballWhite : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.BallofFire;

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.light = 2.0f; // 极亮
            Projectile.tileCollide = true;
        }

        public override void AI()
        {
            // --- 飞行特效：等离子核心 ---
            // 1. 核心白光
            int d1 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.WhiteTorch, 0, 0, 100, default, 2f);
            Main.dust[d1].noGravity = true;
            Main.dust[d1].velocity *= 0.2f;

            // 2. 外围蓝色电弧
            if (Main.rand.NextBool(2))
            {
                int d2 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Electric, 0, 0, 100, default, 1f);
                Main.dust[d2].noGravity = true;
                Main.dust[d2].velocity *= 0.5f;
            }

            // 飞行逻辑
            Projectile.ai[0]++;
            if (Projectile.ai[0] > 30) Projectile.velocity.Y += 0.1f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 600);
            target.AddBuff(BuffID.Frostburn, 600);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
            SoundEngine.PlaySound(SoundID.Item74, Projectile.position); // 额外的火焰音效

            Vector2 center = Projectile.Center;

            // === 爆炸特效：超新星爆发 ===

            // 1. 核心坍缩 (高密度白光)
            for (int i = 0; i < 50; i++)
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.WhiteTorch, 0f, 0f, 100, default, 4f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 2f;
            }

            // 2. 能量喷射 (高速射线)
            for (int i = 0; i < 30; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(10f, 10f);
                int d = Dust.NewDust(center, 0, 0, DustID.Electric, velocity.X, velocity.Y, 0, default, 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity = velocity * 2f; // 射得非常远
            }

            // 3. 扩散的蓝色余烬 (慢速)
            for (int i = 0; i < 40; i++)
            {
                int d = Dust.NewDust(center, 0, 0, DustID.DungeonSpirit, 0, 0, 100, default, 2.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity = Main.rand.NextVector2Circular(5f, 5f);
            }

            // 范围伤害
            if (Projectile.owner == Main.myPlayer)
            {
                int explosionRadius = 180;
                Rectangle explosionRect = new Rectangle((int)center.X - explosionRadius, (int)center.Y - explosionRadius, explosionRadius * 2, explosionRadius * 2);

                foreach (NPC npc in Main.npc)
                {
                    if (npc.active && !npc.friendly && !npc.dontTakeDamage && npc.getRect().Intersects(explosionRect))
                    {
                        int damage = (int)(Projectile.damage * 0.8f);
                        npc.SimpleStrikeNPC(damage, 0);
                        npc.AddBuff(BuffID.OnFire3, 600); // 强力Debuff
                    }
                }
            }
        }
    }
}