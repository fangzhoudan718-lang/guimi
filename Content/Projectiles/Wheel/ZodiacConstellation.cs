using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Projectiles
{
    /// <summary>
    /// 十二星座连星图特效.
    /// ai[0] = 星座index 0-11 (Aries..Pisces)
    /// 在玩家上方绘制一组真实的"星点+连线"星座图,持续3秒.
    /// 全部用 1×1 像素绘制十字光芒与连线, 无粒子.
    /// </summary>
    public class ZodiacConstellation : ModProjectile
    {
        private const int LIFE = 180; // 3 秒

        public override string Texture => "Terraria/Images/Misc/Perlin";

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = LIFE;
            Projectile.alpha = 0;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active) { Projectile.Kill(); return; }
            // 跟随玩家上方,缓慢上浮
            int frame = LIFE - Projectile.timeLeft;
            float floatUp = frame * 0.5f;
            Projectile.Center = owner.Center + new Vector2(0, -220 - floatUp);

            // 持续淡彩光照
            int sign = (int)Projectile.ai[0];
            Color c = GetZodiacColor(sign);
            Lighting.AddLight(Projectile.Center, c.R / 255f * 0.6f, c.G / 255f * 0.6f, c.B / 255f * 0.8f);
        }

        // 每个星座一种独特配色
        private static Color GetZodiacColor(int sign)
        {
            switch (sign)
            {
                case 0:  return new Color(255, 100, 80);   // 白羊 - 火红
                case 1:  return new Color(180, 150, 90);   // 金牛 - 大地
                case 2:  return new Color(255, 220, 100);  // 双子 - 双黄
                case 3:  return new Color(150, 200, 255);  // 巨蟹 - 月光蓝
                case 4:  return new Color(255, 170, 60);   // 狮子 - 金色
                case 5:  return new Color(200, 255, 180);  // 处女 - 嫩绿
                case 6:  return new Color(255, 200, 255);  // 天秤 - 粉紫
                case 7:  return new Color(180, 60, 180);   // 天蝎 - 暗紫
                case 8:  return new Color(160, 100, 255);  // 射手 - 紫蓝
                case 9:  return new Color(100, 100, 130);  // 摩羯 - 深灰蓝
                case 10: return new Color(100, 200, 255);  // 水瓶 - 冰蓝
                case 11: return new Color(180, 220, 255);  // 双鱼 - 海蓝
                default: return Color.White;
            }
        }

        // === 星座定义: 顶点(归一化-1..1) + 连线对(顶点索引对) ===
        // 每个星座 4-8 颗星, 形态尽量贴近真实星座轮廓简化版
        private static (Vector2[] stars, int[,] lines) GetZodiacShape(int sign)
        {
            switch (sign)
            {
                case 0: // 白羊 Aries - 弯角形 (4 颗)
                    return (new[]
                    {
                        new Vector2(-0.8f, 0.6f),
                        new Vector2(-0.3f, 0.0f),
                        new Vector2(0.3f, -0.4f),
                        new Vector2(0.8f, -0.2f),
                    }, new int[,] { {0,1},{1,2},{2,3} });

                case 1: // 金牛 Taurus - V字形角+脸 (7 颗)
                    return (new[]
                    {
                        new Vector2(-0.9f, -0.7f),  // 左角尖
                        new Vector2(-0.4f, -0.2f),  // 左角根
                        new Vector2(0f, 0.2f),      // 鼻
                        new Vector2(-0.2f, 0.6f),   // 左眼
                        new Vector2(0.3f, 0.6f),    // 右眼
                        new Vector2(0.4f, -0.2f),   // 右角根
                        new Vector2(0.9f, -0.7f),   // 右角尖
                    }, new int[,] { {0,1},{1,2},{2,3},{2,4},{4,5},{5,6},{1,5} });

                case 2: // 双子 Gemini - 两个人形 (8 颗)
                    return (new[]
                    {
                        new Vector2(-0.7f, -0.7f),  // 左头
                        new Vector2(-0.7f, -0.1f),  // 左肩
                        new Vector2(-0.7f, 0.5f),   // 左脚
                        new Vector2(-0.3f, 0.0f),   // 左手
                        new Vector2(0.3f, 0.0f),    // 右手
                        new Vector2(0.7f, -0.7f),   // 右头
                        new Vector2(0.7f, -0.1f),   // 右肩
                        new Vector2(0.7f, 0.5f),    // 右脚
                    }, new int[,] { {0,1},{1,2},{1,3},{3,4},{4,6},{5,6},{6,7} });

                case 3: // 巨蟹 Cancer - 螃蟹钳 (5 颗) Y字形
                    return (new[]
                    {
                        new Vector2(-0.6f, -0.6f),  // 左钳上
                        new Vector2(-0.3f, 0f),     // 左钳关节
                        new Vector2(0f, 0.5f),      // 身体
                        new Vector2(0.3f, 0f),      // 右钳关节
                        new Vector2(0.6f, -0.6f),   // 右钳上
                    }, new int[,] { {0,1},{1,2},{2,3},{3,4} });

                case 4: // 狮子 Leo - 镰刀形 (6 颗)
                    return (new[]
                    {
                        new Vector2(-0.8f, 0.6f),   // 尾
                        new Vector2(-0.4f, 0.5f),
                        new Vector2(0f, 0.3f),
                        new Vector2(0.4f, 0f),      // 身
                        new Vector2(0.7f, -0.4f),   // 头
                        new Vector2(0.3f, -0.7f),   // 鬃毛
                    }, new int[,] { {0,1},{1,2},{2,3},{3,4},{4,5},{5,3} });

                case 5: // 处女 Virgo - Y 字形 (5 颗)
                    return (new[]
                    {
                        new Vector2(-0.6f, -0.7f),  // 左肩
                        new Vector2(0f, -0.2f),     // 颈
                        new Vector2(0.6f, -0.7f),   // 右肩
                        new Vector2(-0.2f, 0.6f),   // 左脚
                        new Vector2(0.4f, 0.6f),    // 右脚
                    }, new int[,] { {0,1},{1,2},{1,3},{3,4} });

                case 6: // 天秤 Libra - 三角形+底 (5 颗)
                    return (new[]
                    {
                        new Vector2(0f, -0.7f),     // 顶
                        new Vector2(-0.7f, 0f),     // 左
                        new Vector2(0.7f, 0f),      // 右
                        new Vector2(-0.5f, 0.6f),   // 左盘
                        new Vector2(0.5f, 0.6f),    // 右盘
                    }, new int[,] { {0,1},{0,2},{1,3},{2,4},{1,2} });

                case 7: // 天蝎 Scorpio - S形带勾尾 (8 颗)
                    return (new[]
                    {
                        new Vector2(-0.9f, -0.5f),  // 头
                        new Vector2(-0.5f, -0.2f),
                        new Vector2(-0.1f, 0f),
                        new Vector2(0.3f, 0.2f),
                        new Vector2(0.6f, 0.5f),    // 尾根
                        new Vector2(0.9f, 0.2f),    // 勾上
                        new Vector2(0.8f, -0.3f),   // 勾尖
                        new Vector2(-0.6f, -0.7f),  // 左钳
                    }, new int[,] { {0,1},{1,2},{2,3},{3,4},{4,5},{5,6},{0,7} });

                case 8: // 射手 Sagittarius - 弓箭形 (6 颗)
                    return (new[]
                    {
                        new Vector2(-0.8f, -0.4f),  // 弓上
                        new Vector2(-0.7f, 0f),     // 弓中
                        new Vector2(-0.8f, 0.4f),   // 弓下
                        new Vector2(-0.3f, 0f),     // 箭尾
                        new Vector2(0.3f, 0f),      // 箭身
                        new Vector2(0.9f, 0f),      // 箭尖
                    }, new int[,] { {0,1},{1,2},{1,3},{3,4},{4,5} });

                case 9: // 摩羯 Capricorn - V 字带尾 (5 颗)
                    return (new[]
                    {
                        new Vector2(-0.8f, -0.5f),  // 左角
                        new Vector2(-0.2f, 0.2f),   // 中
                        new Vector2(0.4f, -0.3f),   // 右角
                        new Vector2(0.7f, 0.4f),    // 尾根
                        new Vector2(0.5f, 0.8f),    // 尾尖
                    }, new int[,] { {0,1},{1,2},{2,3},{3,4} });

                case 10: // 水瓶 Aquarius - W 形 (5 颗)
                    return (new[]
                    {
                        new Vector2(-0.9f, -0.3f),
                        new Vector2(-0.4f, 0.3f),
                        new Vector2(0f, -0.3f),
                        new Vector2(0.4f, 0.3f),
                        new Vector2(0.9f, -0.3f),
                    }, new int[,] { {0,1},{1,2},{2,3},{3,4} });

                case 11: // 双鱼 Pisces - V形带两鱼 (6 颗)
                    return (new[]
                    {
                        new Vector2(-0.9f, -0.5f),  // 左鱼头
                        new Vector2(-0.5f, -0.1f),
                        new Vector2(0f, 0.2f),      // 鱼线交点
                        new Vector2(0.5f, -0.1f),
                        new Vector2(0.9f, -0.5f),   // 右鱼头
                        new Vector2(0f, 0.7f),      // 下垂结
                    }, new int[,] { {0,1},{1,2},{2,3},{3,4},{2,5} });

                default:
                    return (new[] { Vector2.Zero }, new int[0, 0]);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int sign = (int)Projectile.ai[0];
            int frame = LIFE - Projectile.timeLeft;
            var shape = GetZodiacShape(sign);
            if (shape.stars.Length == 0) return false;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            SpriteBatch sb = Main.spriteBatch;
            Color zodColor = GetZodiacColor(sign);

            // 整体星座的中心位置 + 旋转角 (缓慢顺时针)
            Vector2 center = Projectile.Center - Main.screenPosition;
            float rot = frame * 0.005f; // 缓慢旋转
            float scale = 100f;          // 100 像素的"半径", 形成约 200×200 的星座

            // 全局透明度: 前 20 帧渐入, 最后 30 帧渐出
            float alpha;
            if (frame < 20) alpha = frame / 20f;
            else if (frame > LIFE - 30) alpha = (LIFE - frame) / 30f;
            else alpha = 1f;
            alpha = MathHelper.Clamp(alpha, 0f, 1f);

            Color baseStar = zodColor * alpha;
            Color baseLine = new Color(zodColor.R, zodColor.G, zodColor.B, 0) * (alpha * 0.6f);
            Color brightLine = zodColor * (alpha * 0.85f);

            // === 计算每颗星的屏幕位置(应用旋转) ===
            Vector2[] starPositions = new Vector2[shape.stars.Length];
            for (int i = 0; i < shape.stars.Length; i++)
            {
                Vector2 raw = shape.stars[i];
                // 应用旋转
                float c = (float)Math.Cos(rot);
                float s = (float)Math.Sin(rot);
                Vector2 rotated = new Vector2(raw.X * c - raw.Y * s, raw.X * s + raw.Y * c);
                starPositions[i] = center + rotated * scale;
            }

            // === [1] 绘制连线 (用线段, 从星到星渐进出现) ===
            int lineCount = shape.lines.GetLength(0);
            // 绘线进度: 第 10-90 帧之间逐渐"绘成"
            float drawProgress;
            if (frame < 10) drawProgress = 0f;
            else if (frame > 90) drawProgress = 1f;
            else drawProgress = (frame - 10) / 80f;

            for (int li = 0; li < lineCount; li++)
            {
                int a = shape.lines[li, 0];
                int b = shape.lines[li, 1];
                if (a < 0 || b < 0 || a >= starPositions.Length || b >= starPositions.Length) continue;

                Vector2 pA = starPositions[a];
                Vector2 pB = starPositions[b];
                Vector2 delta = pB - pA;
                float lineLen = delta.Length();
                if (lineLen < 0.01f) continue;

                // 每条线段独立的"绘成进度" (前面的线先绘成, 后面的后绘成)
                float lineStart = li / (float)lineCount * 0.7f; // 起始百分比
                float lineEnd = lineStart + 0.3f;               // 完成百分比
                float thisProgress = MathHelper.Clamp((drawProgress - lineStart) / (lineEnd - lineStart), 0f, 1f);

                float drawnLen = lineLen * thisProgress;
                if (drawnLen < 0.5f) continue;

                Vector2 dir = delta / lineLen;
                float angle = (float)Math.Atan2(delta.Y, delta.X);

                // 线段主体: 中等亮度
                sb.Draw(pixel, pA, null, brightLine, angle,
                    Vector2.Zero, new Vector2(drawnLen, 1f),
                    SpriteEffects.None, 0f);

                // 线段两侧虚光: 在线段附近撒淡光形成"星云感"
                // 偏移半像素, 让线条不那么生硬
                sb.Draw(pixel, pA + new Vector2(0, -1), null, brightLine * 0.4f, angle,
                    Vector2.Zero, new Vector2(drawnLen, 1f),
                    SpriteEffects.None, 0f);
                sb.Draw(pixel, pA + new Vector2(0, 1), null, brightLine * 0.4f, angle,
                    Vector2.Zero, new Vector2(drawnLen, 1f),
                    SpriteEffects.None, 0f);
            }

            // === [2] 绘制每颗星 - 十字光芒 + 中心圆 ===
            for (int i = 0; i < starPositions.Length; i++)
            {
                Vector2 sp = starPositions[i];

                // 星点亮度随时间脉动 (每颗星不同相位)
                float pulse = 0.7f + (float)Math.Sin(frame * 0.08f + i * 1.2f) * 0.3f;

                // 十字光芒 (4 个方向)
                float armLen = 10f * pulse;
                // 水平
                sb.Draw(pixel, sp, null, baseStar * pulse, 0f,
                    new Vector2(0.5f, 0.5f), new Vector2(armLen, 1.2f),
                    SpriteEffects.None, 0f);
                // 垂直
                sb.Draw(pixel, sp, null, baseStar * pulse, MathHelper.PiOver2,
                    new Vector2(0.5f, 0.5f), new Vector2(armLen, 1.2f),
                    SpriteEffects.None, 0f);
                // 45度
                sb.Draw(pixel, sp, null, baseStar * pulse * 0.7f, MathHelper.PiOver4,
                    new Vector2(0.5f, 0.5f), new Vector2(armLen * 0.7f, 0.9f),
                    SpriteEffects.None, 0f);
                // -45度
                sb.Draw(pixel, sp, null, baseStar * pulse * 0.7f, -MathHelper.PiOver4,
                    new Vector2(0.5f, 0.5f), new Vector2(armLen * 0.7f, 0.9f),
                    SpriteEffects.None, 0f);

                // 中心亮点
                sb.Draw(pixel, sp, null, Color.White * (alpha * pulse), 0f,
                    new Vector2(0.5f, 0.5f), 3f * pulse,
                    SpriteEffects.None, 0f);
                // 中心更亮内点
                sb.Draw(pixel, sp, null, Color.White * alpha, 0f,
                    new Vector2(0.5f, 0.5f), 1.5f,
                    SpriteEffects.None, 0f);
            }

            return false;
        }
    }
}
