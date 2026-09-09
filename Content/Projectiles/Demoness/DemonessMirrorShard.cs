using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace zhashi.Content.Projectiles.Demoness
{
    public class DemonessMirrorShard : ModProjectile
    {
        // ==========================================================
        // 核心修改：使用原版“水晶风暴”的弹幕贴图 (ID 90)
        // 这样它看起来就是一块块锐利的水晶/镜片，而不是原本的黑方块
        // ==========================================================
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CrystalShard;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("魔女之镜碎片");
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;  // 水晶碎片比原来的判定稍大一点
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 3;   // 穿透3个敌人
            Projectile.timeLeft = 180;  // 3秒后消失
            Projectile.aiStyle = -1;    // 保持自定义AI
            Projectile.tileCollide = true;
            Projectile.light = 0.5f;    // 添加一点发光效果
        }

        public override void AI()
        {
            // 1. 物理效果：重力与旋转
            Projectile.velocity.Y += 0.25f; // 重力稍大，更有“碎片”的沉重感
            Projectile.rotation += 0.4f * Projectile.direction; // 旋转更快

            // 2. 视觉特效：混合使用“玻璃”和“蓝色水晶”粒子
            // 这样既有镜子的破碎感，又有魔法的晶莹感
            if (Main.rand.NextBool(3))
            {
                // DustID.Glass = 玻璃碎屑
                // DustID.BlueCrystalShard = 蓝色水晶亮光 (更像镜面反光)
                int dustId = Main.rand.NextBool() ? DustID.Glass : DustID.BlueCrystalShard;

                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, dustId, 0, 0, 100, default, 1.1f);
                d.noGravity = true;
                d.velocity *= 0.6f; // 让粒子跟随碎片
            }

            // 3. 让碎片始终朝向运动方向（可选，如果你喜欢旋转就不需要这句）
            // Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 命中音效：清脆的水晶破碎声
            SoundEngine.PlaySound(SoundID.Item27, Projectile.position);

            // 命中Debuff：魔女的诅咒 (暗影焰 + 霜冻)
            target.AddBuff(BuffID.ShadowFlame, 180);
            target.AddBuff(BuffID.Frostburn, 180);

            // 命中爆炸特效
            for (int i = 0; i < 8; i++)
            {
                // 炸出一圈玻璃碴子
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Glass, 0, 0, 0, default, 1.5f);
                // 炸出一圈魔女紫光
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, 0, 0, 0, default, 1.2f);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.penetrate--;
            if (Projectile.penetrate <= 0)
            {
                // 彻底销毁时播放破碎音效
                SoundEngine.PlaySound(SoundID.Shatter, Projectile.position);
                return true;
            }

            // 撞墙反弹音效
            SoundEngine.PlaySound(SoundID.Dig, Projectile.position);

            // 反弹逻辑
            if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
                Projectile.velocity.X = -oldVelocity.X * 0.5f; // 弹性降低，反弹减速

            if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
                Projectile.velocity.Y = -oldVelocity.Y * 0.5f;

            return false;
        }
    }
}