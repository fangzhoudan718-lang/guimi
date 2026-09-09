using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Projectiles
{
    public class ReaperSlashProjectile : ModProjectile
    {
        public override string Texture => "zhashi/Content/Projectiles/ReaperSlashProjectile";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 2; // 2帧动画
        }

        public override void SetDefaults()
        {
            // 单帧尺寸 (317 x 184)
            Projectile.width = 317;
            Projectile.height = 184;

            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 15;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;

            Projectile.scale = 1.5f;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile.Center = player.Center;
            Projectile.spriteDirection = player.direction;

            // 动画
            if (Projectile.timeLeft > 8) Projectile.frame = 0;
            else Projectile.frame = 1;

            // 特效 (仅第一帧)
            if (Projectile.timeLeft == 15)
            {
                SoundEngine.PlaySound(SoundID.Item71, Projectile.position);
                SoundEngine.PlaySound(SoundID.NPCDeath1, Projectile.position);

                for (int i = 0; i < 60; i++)
                {
                    Vector2 velocity = Vector2.UnitX.RotatedBy(MathHelper.ToRadians(i * 6)) * 15f;
                    int d = Dust.NewDust(player.Center, 0, 0, DustID.CrimsonTorch, velocity.X, velocity.Y, 0, default, 2.5f);
                    Main.dust[d].noGravity = true;
                }

                Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(player.Center, new Vector2(1, 0), 10f, 6f, 10, 1000f));
            }

            Lighting.AddLight(Projectile.Center, 1.0f, 0.1f, 0.1f);
        }

        // === 绘制逻辑 ===
        public override bool PreDraw(ref Color lightColor)
        {
            // 【修改1】直接使用原图，不再去白底
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;

            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle sourceRect = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = sourceRect.Size() / 2f;

            // 【修改2】位置微调：向左上方移动
            // X: -40 (向左), Y: -50 (向上)
            // 根据你的贴图情况，如果还不对，就改这两个数
            Vector2 drawOffset = new Vector2(3f, -30f);

            // 如果玩家朝左(spriteDirection == -1)，X偏移量可能需要反向
            // 这里简单处理为固定偏移，如果发现转身时位置不对，可以将 drawOffset.X 乘以 Projectile.spriteDirection
            if (Projectile.spriteDirection == -1)
            {
                drawOffset.X = 5f; // 向左看时，向右修正一点(因为图片翻转了)
            }

            Vector2 drawPos = Projectile.Center - Main.screenPosition + drawOffset;

            // 使用加法混合绘制 (让颜色更亮)
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(
                texture,
                drawPos,
                sourceRect,
                Color.White,
                Projectile.rotation,
                origin,
                Projectile.scale,
                Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                0
            );

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Bleeding, 600);
            target.AddBuff(BuffID.Ichor, 300);
        }
    }
}