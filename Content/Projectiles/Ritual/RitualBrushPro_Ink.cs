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
    public class RitualBrushPro_Ink : ModProjectile
    {
        private class InkLine
        {
            public List<Vector2> Points;
            public float Opacity;
            public float Spread; // 墨水扩散值

            public InkLine(List<Vector2> points)
            {
                Points = new List<Vector2>(points);
                Opacity = 1f;
                Spread = 0f;
            }
        }

        private List<Vector2> currentLine = new List<Vector2>();
        private List<InkLine> fadingLines = new List<InkLine>();

        private float time = 0f;
        private Vector2 lastMousePos;
        private bool wasChanneling = false;

        // 🎨 配色方案：纯黑
        private Color colorCore = new Color(0, 0, 0, 255);
        private Color colorMid = new Color(30, 30, 30, 200);
        private Color colorWash = new Color(60, 60, 60, 100);

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

            // 只要拿着武器，或者还有没消失的墨迹，弹幕就保持存活
            // 防止松开鼠标瞬间因为不画线了导致弹幕直接消失
            bool validWeapon = player.active && !player.dead && player.HeldItem.ModItem is Items.Weapons.Ritual.RitualBrush;
            if (!validWeapon)
            {
                Projectile.Kill();
                return;
            }
            Projectile.timeLeft = 10; // 只要满足条件，无限续命

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

            // 1. 指针特效
            SpawnInkParticles(mouseVelocity);

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

            // 3. 结算逻辑 (松开鼠标瞬间)
            if (wasChanneling && !isChanneling)
            {
                if (currentLine.Count > 0)
                {
                    TriggerSlashAttack(currentLine);
                    // 伤害打出去了，墨迹加入渐隐列表
                    fadingLines.Add(new InkLine(currentLine));
                    currentLine.Clear();
                }
            }
            wasChanneling = isChanneling;

            // === 4. 渐隐逻辑 (核心修改点) ===
            for (int i = fadingLines.Count - 1; i >= 0; i--)
            {
                // 【关键修改 1】极慢的消失速度
                // 0.005f 意味着 200帧 (约3.3秒) 才会完全消失
                // 这让墨迹能悬停在空中很久
                fadingLines[i].Opacity -= 0.005f;

                // 【关键修改 2】极慢的扩散速度
                // 因为时间变长了，扩散必须变慢，否则线条会变得巨大无比
                fadingLines[i].Spread += 0.02f;

                if (fadingLines[i].Opacity <= 0f)
                {
                    fadingLines.RemoveAt(i);
                }
            }
        }

        private void SpawnInkParticles(Vector2 velocity)
        {
            if (velocity.Length() > 1f || Main.rand.NextBool(5))
            {
                Vector2 spawnPos = Projectile.Center + Main.rand.NextVector2Circular(4, 4);
                int type = DustID.Smoke;
                Dust d = Dust.NewDustPerfect(spawnPos, type, Vector2.Zero);
                d.noGravity = true;
                d.color = Color.Black;
                d.velocity = -velocity * 0.3f + Main.rand.NextVector2Circular(0.8f, 0.8f);
                d.scale = Main.rand.NextFloat(1.2f, 2.0f);
                d.alpha = 20;
                d.fadeIn = 1.2f;
            }
        }

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
                    fadingLines.Add(new InkLine(circlePoints));
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
                Vector2 dir = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 36f);
                Dust d = Dust.NewDustPerfect(center, DustID.Smoke, dir * 6f);
                d.color = Color.Black; d.noGravity = true; d.scale = 2.5f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D glowTex = TextureAssets.Extra[98].Value;
            BlendState inkBlend = BlendState.NonPremultiplied;

            foreach (var line in fadingLines)
            {
                DrawInkLine(line.Points, glowTex, line.Opacity, line.Spread, inkBlend);
            }

            if (currentLine.Count > 1)
            {
                DrawInkLine(currentLine, glowTex, 1f, 0f, inkBlend);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, inkBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            DrawBrushTip(glowTex);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        private void DrawInkLine(List<Vector2> points, Texture2D tex, float opacity, float spread, BlendState blend)
        {
            if (points.Count < 2) return;
            List<Vector2> smooth = GetSmoothedPoints(points);

            float width = 12f + spread * 4f;

            Func<float, float> brushWidth = (p) => {
                float noise = (float)Math.Sin(p * 50f + time * 3f) * 4f + Main.rand.NextFloat(-1f, 1f);
                return width + noise;
            };

            // 使用平方根衰减，让墨迹在最后阶段也能保持一定的可见度，而不是线性消失
            float drawOpacity = (float)Math.Sqrt(opacity);

            EasyTrail.Draw(smooth, -Main.screenPosition,
                (p) => brushWidth(p) * 2.5f,
                (p) => colorWash * drawOpacity * 0.6f,
                tex, blend);

            EasyTrail.Draw(smooth, -Main.screenPosition,
                (p) => brushWidth(p) * 1.6f,
                (p) => colorMid * drawOpacity * 0.8f,
                tex, blend);

            EasyTrail.Draw(smooth, -Main.screenPosition,
                (p) => brushWidth(p) * 0.8f,
                (p) => colorCore * drawOpacity * 1.2f,
                tex, blend);
        }

        private void DrawBrushTip(Texture2D tex)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float scale = 1f + (float)Math.Sin(time * 10f) * 0.05f;

            Main.spriteBatch.Draw(tex, pos, null, colorCore, 0f, tex.Size() / 2, scale * 0.5f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(tex, pos, null, colorWash * 0.5f, 0f, tex.Size() / 2, scale * 1.2f, SpriteEffects.None, 0f);
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