using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace zhashi.Content.Projectiles
{
    public class HandOfGodCharge : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = -1;
            Projectile.hide = false;
            Projectile.scale = 1f;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            // 1. 鼠标保活
            bool isHoldingRightMouse = false;
            if (Main.myPlayer == Projectile.owner)
            {
                if (Main.mouseRight) isHoldingRightMouse = true;
                Projectile.netUpdate = true;
            }
            else
            {
                isHoldingRightMouse = true;
            }

            if (!player.active || player.dead || !isHoldingRightMouse)
            {
                Projectile.Kill();
                return;
            }

            // 2. 续命
            Projectile.timeLeft = 2;
            player.itemTime = 2;
            player.itemAnimation = 2;

            // 3. 计算位置
            Vector2 toMouse = Main.MouseWorld - player.Center;
            Projectile.rotation = toMouse.ToRotation();

            // 手腕距离身体 45 像素
            Vector2 direction = toMouse.SafeNormalize(Vector2.UnitX);
            Vector2 offset = direction * 45f;
            offset.Y += 6f;

            Projectile.Center = player.Center + offset;

            // 4. 方向标记
            Projectile.spriteDirection = 1;

            // 5. 蓄力倍率
            Projectile.ai[0]++;
            float visualFactor = 1f + (Projectile.ai[0] / 60f) * 0.5f;
            if (visualFactor > 2.5f) visualFactor = 2.5f;
            Projectile.scale = visualFactor;

            // 6. 抖动
            float shake = 0f;
            if (Projectile.ai[0] > 60) shake = 1f * visualFactor;
            if (Projectile.ai[0] > 180) shake = 2f * visualFactor;

            if (shake > 0)
            {
                Projectile.Center += Main.rand.NextVector2Circular(shake, shake);
            }

            // 同步玩家
            player.ChangeDir(toMouse.X > 0 ? 1 : -1);
            Projectile.direction = player.direction;
            float armRot = toMouse.ToRotation();
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRot - MathHelper.PiOver2);

            // 7. 粒子特效
            int dustChance = 3;
            if (Projectile.ai[0] > 60) dustChance = 2;
            if (Projectile.ai[0] > 180) dustChance = 1;

            int loopCount = 1;
            if (Projectile.ai[0] > 300) loopCount = 2;

            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            float realHandLength = texture.Width;

            for (int k = 0; k < loopCount; k++)
            {
                if (Main.rand.NextBool(dustChance))
                {
                    Vector2 tipPos = Projectile.Center + Projectile.rotation.ToRotationVector2() * (realHandLength * visualFactor);

                    int dustID = (Projectile.ai[0] > 180 && Main.rand.NextBool(3)) ? DustID.RedTorch : DustID.GoldCoin;
                    float spawnRadius = 30f * visualFactor;
                    float dustScale = 1.2f * visualFactor;

                    Dust d = Dust.NewDustPerfect(tipPos + Main.rand.NextVector2Circular(spawnRadius, spawnRadius), dustID, Vector2.Zero, 0, default, dustScale);
                    d.velocity = (tipPos - d.position) * (0.1f * visualFactor);
                    d.noGravity = true;
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            Player player = Main.player[Projectile.owner];
            if (Main.myPlayer != Projectile.owner) return;

            float chargeTime = Projectile.ai[0];
            if (chargeTime < 5) return;

            float damageMultiplier = 1f + (chargeTime / 60f);
            int finalDamage = (int)(Projectile.damage * damageMultiplier);

            Vector2 velocity = (Main.MouseWorld - player.Center).SafeNormalize(Vector2.UnitX) * 24f;

            // =========================================================
            // 【★联机同步修复：在发射端计算镐力★】
            // 这样能确保发射出的弹幕携带正确的镐力数值传给服务器
            // =========================================================
            int maxPickPower = 0;
            // 遍历背包
            for (int i = 0; i < 58; i++)
            {
                Item item = player.inventory[i];
                if (!item.IsAir && item.pick > maxPickPower)
                {
                    maxPickPower = item.pick;
                }
            }

            // 发射弹幕：
            // ai[0] = 蓄力时间
            // ai[1] = 玩家的最大镐力 (关键修复)
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                velocity,
                ModContent.ProjectileType<HandOfGodAirCannon>(),
                finalDamage,
                Projectile.knockBack * 3,
                player.whoAmI,
                chargeTime,
                maxPickPower // 传入镐力
            );

            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

            float blastScale = 1f + (chargeTime / 60f) * 0.5f;
            if (blastScale > 2.5f) blastScale = 2.5f;

            for (int i = 0; i < 30; i++)
            {
                Dust.NewDustPerfect(Projectile.Center, DustID.GoldCoin, velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.5f, 2f), 0, default, 2f * blastScale).noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int texW = texture.Width;
            int texH = texture.Height;

            SpriteEffects effects = SpriteEffects.None;
            if (Math.Abs(Projectile.rotation) > MathHelper.PiOver2)
            {
                effects = SpriteEffects.FlipVertically;
            }

            Vector2 rotVector = Projectile.rotation.ToRotationVector2();
            float fadeDistance = 100f * Projectile.scale;
            int step = 1;

            for (int i = 0; i < texW; i += step)
            {
                int currentStep = step;
                if (i + currentStep > texW) currentStep = texW - i;

                float distFromWrist = i;
                float alpha = 1f;
                if (distFromWrist < fadeDistance)
                {
                    alpha = distFromWrist / fadeDistance;
                    alpha = alpha * alpha * alpha;
                }

                if (alpha <= 0.01f) continue;

                Rectangle sourceRect = new Rectangle(i, 0, currentStep, texH);
                Vector2 sliceOrigin = new Vector2(0, texH / 2f);
                Vector2 sliceWorldPos = Projectile.Center + rotVector * (i * Projectile.scale);
                Vector2 sliceDrawPos = sliceWorldPos - Main.screenPosition;

                Main.EntitySpriteDraw(texture, sliceDrawPos, sourceRect, lightColor * alpha, Projectile.rotation, sliceOrigin, Projectile.scale, effects, 0);
            }

            return false;
        }
    }
}