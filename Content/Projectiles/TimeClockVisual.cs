using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content;

namespace zhashi.Content.Projectiles
{
    public class TimeClockVisual : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // 注册为拖尾类型，防止某些视觉冲突
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
            Projectile.alpha = 0;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];

            if (!owner.active || owner.dead || owner.ghost)
            {
                Projectile.Kill();
                return;
            }

            LotMPlayer modPlayer = owner.GetModPlayer<LotMPlayer>();
            if (modPlayer.isTimeClockActive)
            {
                Projectile.timeLeft = 2;
            }
            else
            {
                Projectile.Kill();
                return;
            }

            // 锁定在玩家位置
            Projectile.Center = owner.Center;

            // 持续旋转
            Projectile.rotation += 0.02f;

            // 哒哒哒声音 (每20帧响一次)
            Projectile.ai[0]++;
            if (Projectile.ai[0] >= 20)
            {
                // 使用 MenuTick 模拟机械钟表声，并将音量稍微调低一点防止太吵
                SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.5f }, Projectile.Center);
                Projectile.ai[0] = 0;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle sourceRect = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 origin = sourceRect.Size() / 2f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            // 1. 结束默认绘制
            Main.spriteBatch.End();

            // 2. 开启 Additive (加法混合) 模式 -> 完美去白边 + 发光效果
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                RasterizerState.CullCounterClockwise,
                null,
                Main.GameViewMatrix.TransformationMatrix
            );

            // 颜色设置：灰色 * 0.8，避免发光过曝
            Color color = new Color(150, 150, 150, 255) * 0.8f;

            Main.EntitySpriteDraw(
                texture,
                drawPos,
                sourceRect,
                color,
                Projectile.rotation,
                origin,
                // 【修改这里】倍率改为 2.5f，变得巨大！
                Projectile.scale * 2.0f,
                SpriteEffects.None,
                0
            );

            // 3. 恢复默认绘制
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                RasterizerState.CullCounterClockwise,
                null,
                Main.GameViewMatrix.TransformationMatrix
            );

            return false;
        }
    }
}