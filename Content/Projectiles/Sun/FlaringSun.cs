using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.GameContent;

namespace zhashi.Content.Projectiles.Sun
{
    public class FlaringSun : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.SolarWhipSwordExplosion;

        public override void SetDefaults()
        {
            // 初始碰撞箱设小一点，随 AI 变大
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.scale = 0.1f;
        }

        public override void AI()
        {
            // 1. 动态膨胀
            if (Projectile.timeLeft > 60)
            {
                if (Projectile.scale < 5.0f) Projectile.scale += 0.05f;
            }
            else
            {
                Projectile.scale -= 0.1f;
            }

            // 【核心改动】同步调整碰撞箱大小 (Resize)
            // 基础大小 80 * scale。例如 scale=5 时，范围=400
            int newSize = (int)(80f * Projectile.scale);

            // 只有当尺寸发生变化时才调整，避免不必要的计算
            if (Projectile.width != newSize)
            {
                // Resize 会改变左上角坐标，所以需要重新定位中心，防止偏离
                Vector2 oldCenter = Projectile.Center;
                Projectile.width = newSize;
                Projectile.height = newSize;
                Projectile.Center = oldCenter; // 强制把中心拉回来
            }

            // 屏幕震动
            if (Projectile.scale > 2.0f)
            {
                Main.screenPosition += Main.rand.NextVector2Circular(Projectile.scale, Projectile.scale);
            }

            // 3D粒子球体算法
            float baseRadius = 40f * Projectile.scale; // 粒子生成半径也随之变大
            int particlesPerFrame = (int)(20 * Projectile.scale); // 球越大，粒子越多，防止稀疏
            if (particlesPerFrame > 60) particlesPerFrame = 60; // 限制上限防卡

            float rotX = Main.GameUpdateCount * 0.02f;
            float rotY = Main.GameUpdateCount * 0.03f;

            for (int i = 0; i < particlesPerFrame; i++)
            {
                float z = Main.rand.NextFloat(-1f, 1f);
                float theta = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float r = (float)Math.Sqrt(1f - z * z);

                float x = r * (float)Math.Cos(theta);
                float y = r * (float)Math.Sin(theta);

                Vector3 pos3D = new Vector3(x, y, z);
                pos3D = Vector3.Transform(pos3D, Matrix.CreateRotationX(rotX) * Matrix.CreateRotationY(rotY));

                float depthScale = 1f + (pos3D.Z * 0.3f);
                Vector2 spawnPos = Projectile.Center + new Vector2(pos3D.X, pos3D.Y) * baseRadius;

                int dustType = Main.rand.NextBool(3) ? DustID.SolarFlare : DustID.GoldFlame;
                Dust d = Dust.NewDustPerfect(spawnPos, dustType, Vector2.Zero, 0, default, 2.5f * depthScale);
                d.noGravity = true;
                d.velocity = new Vector2(pos3D.X, pos3D.Y) * 1.5f;

                if (Main.rand.NextBool(20))
                {
                    Dust d2 = Dust.NewDustPerfect(Projectile.Center, DustID.WhiteTorch, Main.rand.NextVector2Circular(2, 2), 0, Color.White, 3f);
                    d2.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}