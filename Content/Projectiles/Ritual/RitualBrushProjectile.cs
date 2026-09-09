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
    public class FadingLine
    {
        public List<Vector2> Points;
        public float Opacity;
        public float Dissolve; // 消散进度

        public FadingLine(List<Vector2> points)
        {
            Points = new List<Vector2>(points);
            Opacity = 1f;
            Dissolve = 0f;
        }
    }

    public class RitualBrushProjectile : ModProjectile
    {
        private List<Vector2> currentLine = new List<Vector2>();
        private List<FadingLine> fadingLines = new List<FadingLine>();

        private float time = 0f;
        private Vector2 lastMousePos;
        private bool wasChanneling = false;

        // 🎨 配色方案：圣白流光 (Holy Starlight)
        private Color colorCore = new Color(255, 255, 255);   // 核心：极致纯白
        private Color colorGlow = new Color(220, 220, 220);   // 光晕：淡银白
        private Color colorFading = new Color(150, 150, 160); // 消散：灰银色

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

            if (player.dead || !player.active || player.HeldItem.ModItem is not Items.Weapons.Ritual.RitualKnife)
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

            // === 1. 指针特效：纯白星屑 ===
            SpawnStarlightParticles(mouseVelocity);

            // === 2. 画线逻辑 ===
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

            // === 3. 结算逻辑 ===
            if (wasChanneling && !isChanneling)
            {
                if (currentLine.Count > 0)
                {
                    TriggerSlashAttack(currentLine);
                    fadingLines.Add(new FadingLine(currentLine));
                    currentLine.Clear();
                }
            }
            wasChanneling = isChanneling;

            // === 4. 渐隐逻辑：光雾消散 ===
            for (int i = fadingLines.Count - 1; i >= 0; i--)
            {
                fadingLines[i].Opacity -= 0.04f; // 消失速度适中
                fadingLines[i].Dissolve += 0.5f; // 稍微扩散一点点

                if (fadingLines[i].Opacity <= 0f)
                {
                    fadingLines.RemoveAt(i);
                }
            }
        }

        // === ✨ 粒子特效：纯白星屑 ===
        private void SpawnStarlightParticles(Vector2 velocity)
        {
            // 移动时拖尾，静止时偶尔闪烁
            if (velocity.Length() > 1f || Main.rand.NextBool(10))
            {
                // 在笔尖稍微随机一点的位置
                Vector2 spawnPos = Projectile.Center + Main.rand.NextVector2Circular(4, 4);

                // 使用 SilverCoin (银色) 或 WhiteTorch (纯白)
                int type = Main.rand.NextBool() ? DustID.SilverCoin : DustID.WhiteTorch;

                Dust d = Dust.NewDustPerfect(spawnPos, type, Vector2.Zero);
                d.noGravity = true;

                // 粒子向后飘，模拟流星划过
                d.velocity = -velocity * 0.3f;
                d.scale = Main.rand.NextFloat(0.4f, 0.8f); // 粒子比较细腻小巧
                d.alpha = 50;
            }
        }

        // ==========================================================
        // === ⚔️ 伤害判定 (保持不变) ===
        // ==========================================================

        private void TriggerSlashAttack(List<Vector2> line)
        {
            if (line.Count < 2)
            {
                SpawnExplosion(Projectile.Center, Projectile.damage * 5);
                return;
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item71, Projectile.Center);
            int lineDamage = line.Count * Projectile.damage;
            if (lineDamage > 99999) lineDamage = 99999;
            for (int i = 0; i < line.Count; i += 5) SpawnExplosion(line[i], lineDamage);
        }

        private void SpawnExplosion(Vector2 pos, int damage)
        {
            int pIndex = Projectile.NewProjectile(Projectile.GetSource_FromThis(), pos, Vector2.Zero, ModContent.ProjectileType<RitualExplosion>(), damage, 0, Projectile.owner);
            Main.projectile[pIndex].scale = 1.3f; Main.projectile[pIndex].Resize((int)(40 * 1.3f), (int)(40 * 1.3f));
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
                    TriggerCircleExplosion(circlePoints);
                    fadingLines.Add(new FadingLine(circlePoints));
                    line.Clear();
                    break;
                }
            }
        }

        private void TriggerCircleExplosion(List<Vector2> points)
        {
            if (points.Count < 10) return;
            Vector2 center = Vector2.Zero;
            foreach (var p in points) center += p;
            center /= points.Count;

            float area = 0f;
            for (int i = 0; i < points.Count; i++) { Vector2 p1 = points[i]; Vector2 p2 = points[(i + 1) % points.Count]; area += (p1.X * p2.Y) - (p2.X * p1.Y); }
            area = Math.Abs(area) / 2f;
            int damage = (int)(area / 75f);
            if (damage < Projectile.damage * 1.5f) damage = (int)(Projectile.damage * 1.5f);
            if (damage > 600) damage = 600;
            float radius = (float)Math.Sqrt(area / Math.PI);
            float scale = radius / 16f;

            int pIndex = Projectile.NewProjectile(Projectile.GetSource_FromThis(), center, Vector2.Zero, ModContent.ProjectileType<RitualExplosion>(), damage, 6f, Projectile.owner);
            Main.projectile[pIndex].scale = scale; Main.projectile[pIndex].width = (int)(40 * scale); Main.projectile[pIndex].height = (int)(40 * scale); Main.projectile[pIndex].Center = center;

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item62, center);
            for (int i = 0; i < 36; i++)
            {
                // 纯白爆炸粒子
                Vector2 dir = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 36f);
                Dust d = Dust.NewDustPerfect(center, DustID.SilverCoin, dir * 6f); d.noGravity = true; d.scale = 2f;
                Dust d2 = Dust.NewDustPerfect(center, DustID.WhiteTorch, dir * 4f); d2.noGravity = true; d2.scale = 1.5f;
            }
        }

        // ==========================================================
        // === 🎨 核心绘制：圣白流光 ===
        // ==========================================================
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            // Additive 模式让白色发光更纯净
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D glowTex = TextureAssets.Extra[98].Value;

            // 1. 绘制消散的线条
            foreach (var line in fadingLines)
            {
                DrawHolyLine(line.Points, glowTex, line.Opacity, line.Dissolve, true);
            }

            // 2. 绘制当前线条
            if (currentLine.Count > 1)
            {
                DrawHolyLine(currentLine, glowTex, 1f, 0f, false);
            }

            // 3. 绘制星核 (回归简单的高亮十字星)
            DrawStarCore(glowTex);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        private void DrawHolyLine(List<Vector2> points, Texture2D tex, float opacity, float dissolve, bool isFading)
        {
            if (points.Count < 2) return;
            List<Vector2> smooth = GetSmoothedPoints(points);

            // 宽度逻辑：平滑、稳定
            // 消失时稍微变宽一点点 (光晕扩散)
            float baseWidth = 20f + dissolve * 5f;

            // 闪烁感：非常微弱的呼吸，保持纯净感
            float pulse = 1f + (float)Math.Sin(time * 15f) * 0.05f;

            // === 第一层：银白光晕 ===
            EasyTrail.Draw(smooth, -Main.screenPosition,
                (p) => baseWidth * 1.5f * pulse,
                (p) => colorGlow * opacity * 0.5f, // 半透明银白
                tex);

            // === 第二层：纯白核心 ===
            EasyTrail.Draw(smooth, -Main.screenPosition,
                (p) => baseWidth * 0.6f,
                (p) => colorCore * opacity, // 纯白，不透明
                tex);
        }

        private void DrawStarCore(Texture2D tex)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float rot = time * 2f;
            float scale = 1f + (float)Math.Sin(time * 10f) * 0.1f;

            // 简单的十字星芒，返璞归真
            Texture2D starTex = TextureAssets.Extra[58].Value;

            // 外圈柔光
            Main.spriteBatch.Draw(starTex, pos, null, colorGlow * 0.6f, rot * 0.5f, starTex.Size() / 2, scale * 1.0f, SpriteEffects.None, 0f);

            // 核心亮星
            Main.spriteBatch.Draw(starTex, pos, null, colorCore, -rot, starTex.Size() / 2, scale * 0.6f, SpriteEffects.None, 0f);

            // 中心小白点
            Texture2D orbTex = TextureAssets.Extra[89].Value;
            Main.spriteBatch.Draw(orbTex, pos, null, Color.White, 0f, orbTex.Size() / 2, 0.4f, SpriteEffects.None, 0f);
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