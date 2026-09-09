using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Buffs;

namespace zhashi.Content.Projectiles
{
    public class AbyssShackleProjectile : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;

            // 【核心修复】1.4.4+ 版本必须这样设置伤害类型
            Projectile.DamageType = DamageClass.Magic;

            Projectile.penetrate = 1; // 穿透数：1次
            Projectile.timeLeft = 300; // 持续时间
            Projectile.extraUpdates = 1; // 额外更新一次（速度更快）
        }

        public override void AI()
        {
            // 简单的黑色粒子拖尾
            for (int i = 0; i < 2; i++)
            {
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, 0, 0, 100, default, 1.2f);
                d.noGravity = true;
                d.velocity *= 0.3f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 施加深渊枷锁 Debuff，持续 3 秒 (180帧)
            target.AddBuff(ModContent.BuffType<AbyssShacklesDebuff>(), 180);
        }
    }
}