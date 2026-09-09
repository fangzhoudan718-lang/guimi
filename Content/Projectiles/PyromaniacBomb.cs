using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Projectiles
{
    public class PyromaniacBomb : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Bomb;

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.tileCollide = true;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.2f;
            Projectile.velocity.X *= 0.97f;

            if (Main.rand.NextBool(3))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0, 0, 100, default, 1f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity = Vector2.Zero;
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);

            // === 【新增】生成爆炸视觉特效 ===
            if (Main.myPlayer == Projectile.owner)
            {
                // 生成特效弹幕，不造成伤害，只负责帅
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero, // 速度为0，原地播放
                    ModContent.ProjectileType<PyromaniacExplosionVisual>(),
                    0,
                    0,
                    Main.myPlayer
                );
            }

            // 原有的粒子特效 (保留一部分作为点缀)
            for (int i = 0; i < 30; i++)
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 2f);
                Main.dust[d].velocity *= 1.5f;
                Main.dust[d].noGravity = true;
            }

            // 范围伤害逻辑
            if (Projectile.owner == Main.myPlayer)
            {
                int explosionRadius = 150;
                Rectangle explosionRect = new Rectangle((int)Projectile.Center.X - explosionRadius, (int)Projectile.Center.Y - explosionRadius, explosionRadius * 2, explosionRadius * 2);

                foreach (NPC npc in Main.npc)
                {
                    if (npc.active && !npc.friendly && !npc.dontTakeDamage && npc.getRect().Intersects(explosionRect))
                    {
                        int damage = (int)(Projectile.damage * 1.5f);
                        npc.SimpleStrikeNPC(damage, 0);
                        npc.AddBuff(BuffID.OnFire3, 600);
                        npc.AddBuff(BuffID.Oiled, 600);
                    }
                }
            }
        }
    }
}