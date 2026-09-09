using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Projectiles
{
    // 这是从天上掉下来的那把光剑
    public class DawnBladeProjectile : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 60;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.light = 1.2f; // 更亮
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            DrawOffsetX = 0;
            DrawOriginOffsetY = 0;
        }

        public override void AI()
        {
            // === 【核心修改】区分模式 ===
            // ai[1] == 1 代表是狗发射的（直线、变小）
            // ai[1] == 0 代表是玩家发射的（原版抛物线、原大小）

            if (Projectile.ai[1] == 1)
            {
                // 1. 狗模式：无重力 (直线飞行)
                // 不执行 velocity.Y 的增加逻辑

                // 2. 狗模式：变小 (只在第一帧执行一次)
                if (Projectile.localAI[0] == 0)
                {
                    Projectile.scale = 0.6f; // 视觉缩小
                    Projectile.Resize(20, 40); // 碰撞箱缩小 (原30x60 -> 20x40)
                    Projectile.localAI[0] = 1; // 标记已执行
                }
            }
            else
            {
                // 玩家模式：保留重力 (抛物线)
                Projectile.velocity.Y += 0.4f; // 加速下坠
                if (Projectile.velocity.Y > 20f) Projectile.velocity.Y = 20f;
            }

            // 统一的旋转逻辑：根据速度方向旋转
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // 更密集的拖尾
            for (int i = 0; i < 2; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5, 5), DustID.GoldFlame, -Projectile.velocity * 0.1f, 100, default, 1.8f * Projectile.scale);
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            // 更有力的爆炸声
            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
            SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, Projectile.position);

            // === 升级版华丽爆炸 ===
            float scaleMult = Projectile.scale; // 根据当前大小调整特效范围

            // 1. 向四周扩散的冲击波圈
            for (int i = 0; i < 30; i++)
            {
                Vector2 speed = Main.rand.NextVector2CircularEdge(10f, 10f);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Gold, speed, 0, default, 2.5f * scaleMult);
                d.noGravity = true;
                d.velocity *= 1.2f;
            }

            // 2. 向上喷发的能量柱
            for (int i = 0; i < 20; i++)
            {
                Vector2 speed = new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-5f, -12f));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, speed, 100, default, 3f * scaleMult);
                d.noGravity = true;
            }

            // 3. 屏幕震动 (轻微)
            if (Main.myPlayer == Projectile.owner)
            {
                Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(Projectile.Center, new Vector2(0, 1), 0.5f * scaleMult, 6f, 10, 1000f));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 240);
        }
    }
}