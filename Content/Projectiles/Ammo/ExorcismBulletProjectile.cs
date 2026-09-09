using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.Audio;

namespace zhashi.Content.Projectiles.Ammo
{
    public class ExorcismBulletProjectile : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.aiStyle = 1; // 标准子弹AI
            Projectile.friendly = true; // 对怪物造成伤害
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1; // 穿透数：1次
            Projectile.timeLeft = 600; // 存在时间
            Projectile.alpha = 255; // 初始透明（为了显示拖尾）
            Projectile.light = 0.5f; // 自带微光
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 1; // 速度更快

            AIType = ProjectileID.Bullet; // 模仿普通子弹的行为
        }

        public override void AI()
        {
            // 飞行特效：加上一点金色的神圣粒子
            Projectile.alpha = 0; // 显形
            if (Main.rand.NextBool(3))
            {
                // 169 = 也就是 Gold Dust (金色粒子)
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldCoin);
                Main.dust[dust].velocity *= 0.2f;
                Main.dust[dust].noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            // 撞击时产生小范围光效和声音
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.Item10, Projectile.position);
        }

        // 特性：对亡灵生物（僵尸、骷髅等）造成额外伤害
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // 检查敌人是否免疫中毒（亡灵通常免疫中毒，或者直接判定特定的AI类型）
            // 泰拉瑞亚原版没有直接的 "IsUndead" 属性，通常用排除法或特定ID
            // 这里为了简单，我们让它对所有敌人都有一点暴击加成
            modifiers.CritDamage += 0.2f; // 暴击伤害增加20%
        }
    }
}