using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.ID;

namespace zhashi.Content.Projectiles
{
    public class GuardianShieldProjectile : ModProjectile
    {
        // 指向你的新图片
        public override string Texture => "zhashi/Content/Projectiles/GuardianShieldProjectile";

        public override void SetStaticDefaults()
        {
            // 【关键】告诉游戏这张图有 4 帧
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            // 设置单帧的大致尺寸 (1272 / 4 = 318)
            Projectile.width = 200;
            Projectile.height = 318;

            Projectile.friendly = true;
            Projectile.tileCollide = false; // 穿墙
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2; // 依靠 AI 续命
            Projectile.alpha = 0;    // 不透明 (我们会手动控制绘制)
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 1. 状态检查：如果没按住 X 键或者人死了，弹幕消失
            if (!modPlayer.isGuardianStance || player.dead || !player.active)
            {
                Projectile.Kill();
                return;
            }

            // 2. 续命
            Projectile.timeLeft = 2;

            // 3. 位置锁定：始终粘在玩家身前
            // player.direction: 1是向右，-1是向左
            Projectile.spriteDirection = player.direction;
            Vector2 offset = new Vector2(player.direction * 0, -10); // 向前20，稍微向上提一点
            Projectile.Center = player.Center + offset;

            // 4. 动画播放逻辑
            // frameCounter 是内部计时器
            Projectile.frameCounter++;

            // 每 5 帧切换一次图片 (数字越小播放越快)
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++; // 切到下一张

                // 如果超过第 3 张 (0,1,2,3)，回到第 0 张
                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }
            }

            // 5. 添加一点点光照
            Lighting.AddLight(Projectile.Center, 1.0f, 0.8f, 0.4f);
        }

        // === 绘制逻辑 (为了更好的视觉效果) ===
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;

            // 计算单帧高度
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];

            // 剪裁区域
            Rectangle sourceRect = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = sourceRect.Size() / 2f;

            // 计算位置
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            // 使用加法混合 (Additive)，让光盾看起来晶莹剔透、发光
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(
                texture,
                drawPos,
                sourceRect,
                Color.White * 0.8f, // 稍微带点透明度
                Projectile.rotation,
                origin,
                1.0f, // 缩放比例，如果觉得盾太大/太小可以在这里改
                Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, // 根据朝向翻转图片
                0
            );

            // 恢复正常绘制
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            return false; // 阻止系统自动绘制
        }
    }
}