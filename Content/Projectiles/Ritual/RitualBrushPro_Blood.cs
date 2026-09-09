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
    public class RitualBrushPro_Blood : ModProjectile
    {
        private class BloodLine
        {
            public List<Vector2> Points;
            public float Opacity;
            public float Spread;

            public BloodLine(List<Vector2> points)
            {
                Points = new List<Vector2>(points);
                Opacity = 1f;
                Spread = 0f;
            }
        }

        private List<Vector2> currentLine = new List<Vector2>();
        private List<BloodLine> fadingLines = new List<BloodLine>();

        private float time = 0f;
        private Vector2 lastMousePos;
        private bool wasChanneling = false;

        // 🎨 配色方案：凝固之血 (Coagulated Blood)
        // 使用 AlphaBlend/NonPremultiplied，所以 Alpha 通道很重要
        private Color colorCore = new Color(80, 0, 0, 255);      // 核心：深暗红
        private Color colorMid = new Color(180, 20, 20, 200);   // 中层：鲜血红
        private Color colorWash = new Color(100, 0, 0, 100);     // 边缘：干涸红

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

            // 武器检查
            if (player.dead || !player.active || player.HeldItem.ModItem is not Items.Weapons.Ritual.BloodEtcher)
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

            // 1. 指针特效：血液飞溅
            SpawnBloodParticles(mouseVelocity);

            // 2. 画线逻辑
            if (isChanneling)
            {
                if (currentLine.Count == 0 || Vector2.Distance(currentLine[currentLine.Count - 1], Projectile.Center) > 4f)
                {
                    // === 🩸 核心机制：消耗生命值 ===
                    // 检查生命值是否大于 1 (防止画死自己，或者你可以允许画死)
                    if (player.statLife > 1)
                    {
                        // 每次加点扣除 1 点生命值
                        // 为了防止扣血太快（每秒60帧），我们可以加个间隔，或者就让它这么硬核
                        // 这里设定为：每加一个点扣 1 血 (一条长线可能会扣几十血)
                        player.statLife -= 1;

                        // 可选：显示扣血数字（可能会刷屏，建议注释掉或仅在大量扣血时显示）
                        // CombatText.NewText(player.getRect(), Color.Red, "-1"); 

                        currentLine.Add(Projectile.Center);
                        CheckCircle(currentLine);
                    }
                    else
                    {
                        // 没血了，强制停止绘制
                        isChanneling = false;
                    }
                }
            }

            // 3. 结算逻辑
            if (wasChanneling && !isChanneling)
            {
                if (currentLine.Count > 0)
                {
                    TriggerSlashAttack(currentLine);
                    fadingLines.Add(new BloodLine(currentLine));
                    currentLine.Clear();
                }
            }
            wasChanneling = isChanneling;

            // 4. 渐隐逻辑
            for (int i = fadingLines.Count - 1; i >= 0; i--)
            {
                fadingLines[i].Opacity -= 0.02f; // 血迹干涸速度
                fadingLines[i].Spread += 0.1f;   // 扩散

                if (fadingLines[i].Opacity <= 0f)
                {
                    fadingLines.RemoveAt(i);
                }
            }
        }

        private void SpawnBloodParticles(Vector2 velocity)
        {
            if (velocity.Length() > 1f || Main.rand.NextBool(5))
            {
                Vector2 spawnPos = Projectile.Center + Main.rand.NextVector2Circular(4, 4);
                // 使用原版血液粒子
                int type = DustID.Blood;
                Dust d = Dust.NewDustPerfect(spawnPos, type, Vector2.Zero);
                d.noGravity = false; // 血液受重力影响，会滴落！
                d.velocity = -velocity * 0.2f + Main.rand.NextVector2Circular(0.5f, 0.5f);
                d.scale = Main.rand.NextFloat(1.0f, 1.8f);
                d.alpha = 20;
            }
        }

        private void TriggerSlashAttack(List<Vector2> line)
        {
            if (line.Count < 2)
            {
                SpawnExplosion(Projectile.Center, Projectile.damage * 5);
                return;
            }
            // 播放血肉音效 (Item17 是这一类，或者 NPCHit1)
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath1, Projectile.Center);

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
                    fadingLines.Add(new BloodLine(circlePoints));
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

            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath1, center);

            // 血液大爆发
            for (int i = 0; i < 36; i++)
            {
                Vector2 dir = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 36f);
                Dust d = Dust.NewDustPerfect(center, DustID.Blood, dir * 6f);
                d.noGravity = false; // 受重力
                d.scale = 2.0f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 使用 NonPremultiplied 混合模式，适合深红色液体
            BlendState bloodBlend = BlendState.NonPremultiplied;
            Texture2D glowTex = TextureAssets.Extra[98].Value;

            foreach (var line in fadingLines)
            {
                DrawBloodLine(line.Points, glowTex, line.Opacity, line.Spread, bloodBlend);
            }

            if (currentLine.Count > 1)
            {
                DrawBloodLine(currentLine, glowTex, 1f, 0f, bloodBlend);
            }

            // 笔尖
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, bloodBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            DrawBrushTip(glowTex);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        private void DrawBloodLine(List<Vector2> points, Texture2D tex, float opacity, float spread, BlendState blend)
        {
            if (points.Count < 2) return;
            List<Vector2> smooth = GetSmoothedPoints(points);

            // 血液比墨水稍微细一点，但更浓稠
            float width = 10f + spread * 3f;

            // 脉动效果：模拟血管跳动
            float pulse = 1f + (float)Math.Sin(time * 15f) * 0.1f;

            Func<float, float> brushWidth = (p) => {
                float noise = (float)Math.Sin(p * 40f + time * 2f) * 3f;
                return (width + noise) * pulse;
            };

            // === 第一层：干涸边缘 ===
            EasyTrail.Draw(smooth, -Main.screenPosition,
                (p) => brushWidth(p) * 2.0f,
                (p) => colorWash * opacity * 0.5f,
                tex, blend);

            // === 第二层：鲜血主体 ===
            EasyTrail.Draw(smooth, -Main.screenPosition,
                (p) => brushWidth(p) * 1.2f,
                (p) => colorMid * opacity * 0.8f,
                tex, blend);

            // === 第三层：凝固核心 (暗红) ===
            EasyTrail.Draw(smooth, -Main.screenPosition,
                (p) => brushWidth(p) * 0.6f,
                (p) => colorCore * opacity * 1.5f,
                tex, blend);
        }

        private void DrawBrushTip(Texture2D tex)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float scale = 1f + (float)Math.Sin(time * 10f) * 0.05f;

            // 暗红色的血珠
            Main.spriteBatch.Draw(tex, pos, null, colorCore, 0f, tex.Size() / 2, scale * 0.6f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(tex, pos, null, colorMid * 0.5f, 0f, tex.Size() / 2, scale * 1.2f, SpriteEffects.None, 0f);
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