using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Projectiles
{
    public class HandOfGodProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
        }

        public override void SetDefaults()
        {
            Projectile.width = 120;
            Projectile.height = 120;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ownerHitCheck = false;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            if (!player.active || player.dead || player.itemAnimation <= 0)
            {
                Projectile.Kill();
                return;
            }

            if (Main.myPlayer == Projectile.owner)
            {
                player.heldProj = Projectile.whoAmI;
            }

            // 初始化
            if (Projectile.localAI[0] == 0)
            {
                Projectile.timeLeft = player.itemAnimationMax;
                Projectile.localAI[1] = player.itemAnimationMax;

                UpdateHandPosition(player, 0f); // 立即计算第一帧
                ResetTrails(); // 填充历史数据

                Projectile.localAI[0] = 1;
            }

            float duration = Projectile.localAI[1] > 0 ? Projectile.localAI[1] : 1;
            float progress = 1f - (Projectile.timeLeft / duration);

            UpdateHandPosition(player, progress);
        }

        private void UpdateHandPosition(Player player, float progress)
        {
            float swingProgress = SmootherStep(progress);

            // 体型变化：0.1 -> 2.5倍
            Projectile.scale = MathHelper.Lerp(0.1f, 2.5f, swingProgress);

            // 转向逻辑
            if (Math.Abs(Main.MouseWorld.X - player.Center.X) > 10f)
            {
                player.ChangeDir(Main.MouseWorld.X > player.Center.X ? 1 : -1);
            }
            Projectile.direction = player.direction;
            Projectile.spriteDirection = Projectile.direction;

            // 握持偏移
            Projectile.Center = player.Center + new Vector2(0, 25f * Projectile.scale);

            // 角度计算
            float targetAngle = GetSwingRotation(player, swingProgress);

            if (player.direction == 1)
            {
                Projectile.rotation = targetAngle;
            }
            else
            {
                Projectile.rotation = MathHelper.Pi - targetAngle;
            }

            Projectile.velocity = Projectile.rotation.ToRotationVector2();
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
        }

        // --- 核心角度逻辑 ---
        private float GetSwingRotation(Player player, float swingProgress)
        {
            Vector2 toMouse = Main.MouseWorld - player.Center;
            if (player.direction == -1) toMouse.X *= -1;
            float mouseAngle = toMouse.ToRotation();

            // 限制角度，防止反关节
            if (mouseAngle < -2.2f) mouseAngle = -2.2f;
            if (mouseAngle > 2.0f) mouseAngle = 2.0f;

            float startAngle = -2.0f;
            float totalSwing = mouseAngle - startAngle;
            if (totalSwing < 0.2f) totalSwing = 0.2f;

            return startAngle + (totalSwing * swingProgress);
        }

        private void ResetTrails()
        {
            for (int i = 0; i < Projectile.oldRot.Length; i++)
            {
                Projectile.oldRot[i] = Projectile.rotation;
                Projectile.oldPos[i] = Projectile.position;
            }
        }

        private float SmootherStep(float x)
        {
            return x * x * x * (x * (x * 6 - 15) + 10);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float handLength = 260f * Projectile.scale;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.rotation.ToRotationVector2() * handLength;
            float collisionPoint = 0f;
            float collisionWidth = 180f * Projectile.scale;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, collisionWidth, ref collisionPoint);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage *= Projectile.scale;
        }

        // ========================================================================
        // 【核心修改区：绘制部分】
        // ========================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.localAI[0] == 0) return false;

            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            // 原始原点
            Vector2 originalOrigin = new Vector2(texture.Width / 2f, texture.Height - 5f);

            Player player = Main.player[Projectile.owner];
            float duration = Projectile.localAI[1] > 0 ? Projectile.localAI[1] : 1;
            float currentProgress = 1f - (Projectile.timeLeft / duration);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            SpriteEffects effects = Projectile.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            // 1. 绘制渐变残影
            for (int k = 0; k < Projectile.oldPos.Length; k++)
            {
                float lagAmount = k * (1f / duration) * 1.5f;
                float oldProgress = currentProgress - lagAmount;
                if (oldProgress < 0f) continue;

                float oldSwingProgress = SmootherStep(oldProgress);
                float oldBaseAngle = GetSwingRotation(player, oldSwingProgress);

                float oldDrawRot;
                if (Projectile.direction == 1)
                    oldDrawRot = oldBaseAngle + MathHelper.PiOver2;
                else
                    oldDrawRot = -(oldBaseAngle + MathHelper.PiOver2);

                Color color = Color.MediumPurple * (0.4f - k * 0.05f);
                float oldScale = MathHelper.Lerp(0.1f, 2.5f, oldSwingProgress);
                oldScale *= (1f - k * 0.05f);

                // 调用我们的切片绘制函数
                DrawTextureWithGradient(texture, drawPos, oldDrawRot, oldScale, color, effects, originalOrigin);
            }

            // 2. 绘制渐变本体
            float swingProgress = SmootherStep(currentProgress);
            float baseAngle = GetSwingRotation(player, swingProgress);

            float finalDrawRotation;
            if (Projectile.direction == 1)
                finalDrawRotation = baseAngle + MathHelper.PiOver2;
            else
                finalDrawRotation = -(baseAngle + MathHelper.PiOver2);

            // 本体发光层
            DrawTextureWithGradient(texture, drawPos, finalDrawRotation, Projectile.scale * 1.1f, Color.Gold * 0.3f, effects, originalOrigin);
            // 本体实体层
            DrawTextureWithGradient(texture, drawPos, finalDrawRotation, Projectile.scale, lightColor, effects, originalOrigin);

            return false;
        }

        // ========================================================================
        // 【新增辅助函数：切片渐变绘制】
        // 原理：将图片横向切成无数个2像素高的细条，越底部的细条透明度越低
        // ========================================================================
        private void DrawTextureWithGradient(Texture2D texture, Vector2 position, float rotation, float scale, Color baseColor, SpriteEffects effects, Vector2 originalOrigin)
        {
            // 步长：每次画2个像素的高度（性能与效果的平衡）
            int step = 2;

            // 渐变范围：底部 180 像素的区域进行渐变 (你可以调整这个数值)
            // 也就是说，从手腕往上数180像素，会慢慢变实；再往上就全是实心的
            float fadeHeight = 180f;

            for (int i = 0; i < texture.Height; i += step)
            {
                // 计算当前切片到底部的距离
                // texture.Height 是底部(手腕)，0 是顶部(指尖)
                float distFromWrist = texture.Height - i;

                // 计算透明度 (Alpha)
                float alpha = 1f;
                if (distFromWrist < fadeHeight)
                {
                    // 越接近手腕(底部)，distFromWrist越小，alpha越接近0
                    alpha = distFromWrist / fadeHeight;
                    // 使用三次方插值，让渐变更柔和自然，而不是生硬的线性变化
                    alpha = alpha * alpha * alpha;
                }

                // 如果完全透明，就跳过不画，节省性能
                if (alpha <= 0.01f) continue;

                // 计算这一条的颜色
                Color sliceColor = baseColor * alpha;

                // 定义切片区域
                // 每次只从图片里切出 2像素高 的一条
                Rectangle sourceRect = new Rectangle(0, i, texture.Width, step);

                // 核心数学：计算切片的原点
                // 既然我们只画这一条，那么原点必须相应地向上移动，
                // 才能保证这一条画在它本来应该在的位置。
                Vector2 sliceOrigin = new Vector2(originalOrigin.X, originalOrigin.Y - i);

                // 绘制这一条
                Main.EntitySpriteDraw(texture, position, sourceRect, sliceColor, rotation, sliceOrigin, scale, effects, 0);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Buffs.ConquerorWill>(), 600);
            for (int i = 0; i < 20; i++)
            {
                int dustId = (i % 2 == 0) ? DustID.GoldCoin : DustID.Shadowflame;
                Dust.NewDust(target.position, target.width, target.height, dustId, hit.HitDirection * 3, 0, 0, default, 1.5f * Projectile.scale);
            }
            Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(Projectile.Center, Main.rand.NextVector2Circular(1, 1), 10f * Projectile.scale, 6f, 10, 1000f));
        }
    }
}