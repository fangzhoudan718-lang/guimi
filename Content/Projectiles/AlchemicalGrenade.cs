using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace zhashi.Content.Projectiles
{
    public class AlchemicalGrenade : ModProjectile
    {
        // 借用原版“毒气瓶”的贴图
        public override string Texture => "Terraria/Images/Item_" + ItemID.ToxicFlask;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic; // 算作魔法伤害
            Projectile.penetrate = 1;    // 撞到就炸
            Projectile.timeLeft = 600;   // 10秒后消失
            Projectile.ignoreWater = false;
            Projectile.tileCollide = true;
        }

        public override void AI()
        {
            // 【核心修改】重力控制
            // ai[1] == 0: 玩家使用，受重力影响 (抛物线)
            // ai[1] == 1: 狗使用，不受重力影响 (直线)
            if (Projectile.ai[1] == 0)
            {
                Projectile.velocity.Y += 0.2f;
            }

            // 旋转效果
            Projectile.rotation += 0.1f * (float)Projectile.direction;

            // 飞行时的粒子
            if (Main.rand.NextBool(3))
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GreenFairy, 0, 0, 150, default, 1.0f);
            }
        }

        public override void OnKill(int timeLeft)
        {
            // 播放玻璃破碎声
            SoundEngine.PlaySound(SoundID.Shatter, Projectile.position);

            // 1. 爆炸视觉效果
            for (int i = 0; i < 30; i++)
            {
                int dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GreenFairy, 0f, 0f, 100, default, 2f);
                Main.dust[dustIndex].velocity *= 1.5f;
            }

            // 2. 范围伤害与Debuff
            // 遍历所有 NPC
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC target = Main.npc[i];
                if (target.active && !target.friendly && target.Distance(Projectile.Center) < 150f) // 150像素半径爆炸
                {
                    // 造成伤害
                    target.SimpleStrikeNPC(Projectile.damage, 0, false, 0f, DamageClass.Magic);

                    // --- 随机施加炼金 Debuff ---
                    int rand = Main.rand.Next(5);
                    int duration = 600; // 10秒

                    switch (rand)
                    {
                        case 0: target.AddBuff(BuffID.Ichor, duration); break;       // 灵液 (降防)
                        case 1: target.AddBuff(BuffID.CursedInferno, duration); break; // 咒火 (高伤)
                        case 2: target.AddBuff(BuffID.ShadowFlame, duration); break;   // 暗影焰
                        case 3: target.AddBuff(BuffID.Venom, duration); break;         // 剧毒
                        case 4: target.AddBuff(BuffID.Frostburn, duration); break;     // 霜火
                    }
                }
            }
        }
    }
}