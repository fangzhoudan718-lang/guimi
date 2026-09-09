using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Audio;
using System;

namespace zhashi.Content.Projectiles
{
    public class TwilightSwordBeam : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 5; // 穿透5个敌人
            Projectile.timeLeft = 300;
            Projectile.light = 1.0f;
            Projectile.extraUpdates = 2;

            // 【关键修改】允许穿透物块
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            // 特效
            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.OrangeTorch, 0, 0, 100, default, 1.5f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.5f;
            }
            if (Main.rand.NextBool(5))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Ash, 0, 0, 100, default, 1.2f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Daybreak, 300);
            target.AddBuff(BuffID.ShadowFlame, 300);
        }

        // 注意：因为 tileCollide = false，OnKill 不会在撞墙时触发，只会在时间结束或穿透次数用尽时触发
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
            for (int i = 0; i < 20; i++)
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.OrangeTorch, 0f, 0f, 0, default, 2f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 2f;
            }
        }
    }
}