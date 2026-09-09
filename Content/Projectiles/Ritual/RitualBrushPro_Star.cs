using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Utils;
using zhashi.Content.DamageClasses;

namespace zhashi.Content.Projectiles.Ritual
{
    public class RitualBrushPro_Star : ModProjectile
    {
        private class StarLine
        {
            public List<Vector2> Points;
            public float Opacity;
            public float WidthMult;

            public StarLine(List<Vector2> points)
            {
                Points = new List<Vector2>(points);
                Opacity = 1f;
                WidthMult = 1f;
            }
        }

        private List<Vector2> currentLine = new List<Vector2>();
        private List<StarLine> fadingLines = new List<StarLine>();

        private float time = 0f;
        private Vector2 lastMousePos;
        private bool wasChanneling = false;

        // 🎨 配色方案：星怒粉紫 (Starfury Pink/Purple)
        private Color colorCore = new Color(255, 220, 255); // 核心：亮粉白
        private Color colorMid = new Color(255, 50, 150);  // 中层：热粉色
        private Color colorEdge = new Color(120, 0, 220);   // 边缘：深紫色

        public override string Texture => "Terraria/Images/Projectile_0";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = ModContent.GetInstance<RitualDamage>();
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 10;
            Projectile.scale = 1f;
            Projectile.hide = false;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

            if (player.dead || !player.active || player.HeldItem.ModItem is not Items.Weapons.Ritual.StarfallEtcher)
            {
                Projectile.Kill();
                return;
            }
            Projectile.timeLeft = 10;
            time += 0.05f;

            if (Main.myPlayer == Projectile.owner)
            {
                if (lastMousePos == Vector2.Zero) lastMousePos = Main.MouseWorld;
                Projectile.Center = Main.MouseWorld;
                Projectile.netUpdate = true;
            }
            Vector2 mouseVelocity = Projectile.Center - lastMousePos;
            lastMousePos = Projectile.Center;

            bool isChanneling = player.channel && !player.noItems && !player.CCed;

            // 1. 指针特效：粉紫星尘
            SpawnStarParticles(mouseVelocity);

            // 2. 画线逻辑
            if (isChanneling)
            {
                if (currentLine.Count == 0 || Vector2.Distance(currentLine[currentLine.Count - 1], Projectile.Center) > 4f)
                {
                    if (currentLine.Count == 0 || modPlayer.ConsumePixels(2))
                    {
                        currentLine.Add(Projectile.Center);
                        CheckCircle(currentLine);
                    }
                }
            }

            // 3. 结算逻辑
            if (wasChanneling && !isChanneling)
            {
                if (currentLine.Count > 0)
                {
                    TriggerStarfallAttack(currentLine);
                    fadingLines.Add(new StarLine(currentLine));
                    currentLine.Clear();
                }
            }
            wasChanneling = isChanneling;

            // 4. 渐隐逻辑：星光消逝
            for (int i = fadingLines.Count - 1; i >= 0; i--)
            {
                fadingLines[i].Opacity -= 0.04f;
                fadingLines[i].WidthMult -= 0.02f; // 慢慢变细消失

                if (fadingLines[i].Opacity <= 0f || fadingLines[i].WidthMult <= 0f)
                {
                    fadingLines.RemoveAt(i);
                }
            }
        }

        private void SpawnStarParticles(Vector2 velocity)
        {
            if (velocity.Length() > 1f || Main.rand.NextBool(5))
            {
                Vector2 spawnPos = Projectile.Center + Main.rand.NextVector2Circular(4, 4);
                // 粉色和紫色粒子
                int type = Main.rand.NextBool() ? DustID.PinkTorch : DustID.PurpleCrystalShard;
                Dust d = Dust.NewDustPerfect(spawnPos, type, Vector2.Zero);
                d.noGravity = true;
                d.velocity = -velocity * 0.2f;
                d.scale = Main.rand.NextFloat(0.6f, 1.0f);
            }
        }

        // === 🌟 核心攻击逻辑：召唤坠星 ===
        private void TriggerStarfallAttack(List<Vector2> line)
        {
            if (line.Count == 0) return;

            // 点射补偿：如果点太少，强制在中心召唤一颗
            if (line.Count < 3)
            {
                SpawnFallingStar(Projectile.Center);
                return;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9, Projectile.Center); // 播放星星音效

            // 沿着线条每隔一段距离召唤一颗星星
            // 步长设大一点 (e.g., 15)，避免星星太密集
            for (int i = 0; i < line.Count; i += 15)
            {
                SpawnFallingStar(line[i]);
            }
        }

        private void SpawnFallingStar(Vector2 targetPos)
        {
            Player player = Main.player[Projectile.owner];

            // 在目标点上方高空生成
            float spawnHeight = 600f;
            // 稍微加一点水平随机偏移，让它们不是死板地排成一条线
            Vector2 spawnPos = targetPos + new Vector2(Main.rand.NextFloat(-50, 50), -spawnHeight);

            // 计算落向目标点的速度
            Vector2 velocity = Vector2.Normalize(targetPos - spawnPos) * 18f; // 速度快一点

            int pIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPos,
                velocity,
                ModContent.ProjectileType<RitualFallingStar>(),
                Projectile.damage, // 使用武器面板伤害
                Projectile.knockBack,
                Projectile.owner,
                0f, // ai[0] 未使用
                targetPos.Y // ai[1] 传入目标Y坐标，告诉星星什么时候开启碰撞
            );
        }

        private void CheckCircle(List<Vector2> line)
        {
            if (line.Count < 20) return;
            Vector2 head = line[line.Count - 1];
            for (int i = 0; i < line.Count - 15; i++)
            {
                if (Vector2.Distance(head, line[i]) < 25f)
                {
                    List<Vector2> circlePoints = line.GetRange(i, line.Count - i);
                    TriggerCircleStarfall(circlePoints);
                    fadingLines.Add(new StarLine(circlePoints));
                    line.Clear();
                    break;
                }
            }
        }

        private void TriggerCircleStarfall(List<Vector2> points)
        {
            if (points.Count < 10) return;
            Vector2 center = Vector2.Zero;
            foreach (var p in points) center += p;
            center /= points.Count;

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9, center);

            // 闭环时，在圆圈区域内随机轰炸多颗星星
            int starCount = Math.Min(points.Count / 3, 15); // 根据圆圈大小决定星星数量，最多15颗
            for (int i = 0; i < starCount; i++)
            {
                // 在圆圈包围盒内随机取点作为目标
                Vector2 randomTarget = points[Main.rand.Next(points.Count)] + Main.rand.NextVector2Circular(20, 20);
                SpawnFallingStar(randomTarget);
            }

            // 地面生成一圈法阵粒子提示
            for (int i = 0; i < 36; i++)
            {
                Vector2 dir = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 36f);
                Dust d = Dust.NewDustPerfect(center, DustID.PinkTorch, dir * 6f);
                d.noGravity = true; d.scale = 1.5f;
            }
        }

        // ==========================================================
        // === 🎨 核心绘制：星怒粉紫流光 ===
        // ==========================================================
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            // 使用 Additive 发光混合
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D glowTex = TextureAssets.Extra[98].Value;

            foreach (var line in fadingLines)
            {
                DrawStarLine(line.Points, glowTex, line.Opacity, line.WidthMult);
            }

            if (currentLine.Count > 1)
            {
                DrawStarLine(currentLine, glowTex, 1f, 1f);
            }

            DrawStarCursor();

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        private void DrawStarLine(List<Vector2> points, Texture2D tex, float opacity, float widthMult)
        {
            if (points.Count < 2) return;
            List<Vector2> smooth = GetSmoothedPoints(points);

            float baseWidth = 25f * widthMult;
            float flicker = (float)Math.Sin(time * 20f) * 2f; // 高频闪烁

            // === 第一层：深紫边缘 (最宽) ===
            EasyTrail.Draw(smooth, -Main.screenPosition,
                (p) => (baseWidth + flicker) * 1.8f,
                (p) => colorEdge * opacity * 0.6f,
                tex);

            // === 第二层：热粉主体 ===
            EasyTrail.Draw(smooth, -Main.screenPosition,
                (p) => (baseWidth + flicker) * 1.0f,
                (p) => colorMid * opacity * 0.8f,
                tex);

            // === 第三层：亮白核心 ===
            EasyTrail.Draw(smooth, -Main.screenPosition,
                (p) => baseWidth * 0.4f,
                (p) => colorCore * opacity,
                tex);
        }

        private void DrawStarCursor()
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float rot = time * 3f; // 快速旋转
            float scale = 1f + (float)Math.Sin(time * 10f) * 0.1f;

            // 使用星芒贴图
            Texture2D starTex = TextureAssets.Extra[58].Value;

            // 外圈紫色星芒
            Main.spriteBatch.Draw(starTex, pos, null, colorEdge * 0.8f, rot, starTex.Size() / 2, scale * 1.2f, SpriteEffects.None, 0f);
            // 内圈粉色星芒 (反向旋转)
            Main.spriteBatch.Draw(starTex, pos, null, colorMid, -rot * 1.5f, starTex.Size() / 2, scale * 0.8f, SpriteEffects.None, 0f);
            // 核心亮点
            Texture2D orbTex = TextureAssets.Extra[89].Value;
            Main.spriteBatch.Draw(orbTex, pos, null, colorCore, 0f, orbTex.Size() / 2, 0.5f, SpriteEffects.None, 0f);
        }

        private List<Vector2> GetSmoothedPoints(List<Vector2> originalPoints)
        {
            if (originalPoints.Count < 3) return originalPoints;
            List<Vector2> smoothed = new List<Vector2>();
            smoothed.Add(originalPoints[0]);
            for (int i = 0; i < originalPoints.Count - 1; i++)
            {
                Vector2 p0 = originalPoints[i];
                Vector2 p1 = originalPoints[i + 1];
                float dist = Vector2.Distance(p0, p1);
                int segments = (int)(dist / 4f);
                if (segments < 1) segments = 1;
                if (segments > 5) segments = 5;
                for (int j = 1; j <= segments; j++)
                {
                    float t = (float)j / segments;
                    smoothed.Add(Vector2.Lerp(p0, p1, t));
                }
            }
            return smoothed;
        }
    }
}