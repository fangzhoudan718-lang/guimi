using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Projectiles
{
    public class WeatherStrikeLightning : ModProjectile
    {
        public override string Texture => "zhashi/Content/Projectiles/WeatherStrikeLightning";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 3; // 3帧动画
        }

        public override void SetDefaults()
        {
            Projectile.width = 103; // 你的贴图宽度
            Projectile.height = 100; // 初始高度 (会在AI里动态拉长)
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1; // 无限穿透
            Projectile.tileCollide = false; // 穿墙
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 20; // 存在时间很短 (0.3秒)
            Projectile.alpha = 255; // 初始隐藏，靠PreDraw绘制
        }

        public override void AI()
        {
            // === 1. 动态调整高度与位置 ===
            // 目标：让弹幕的底部位于鼠标点击处 (Spawn点)，顶部延伸到屏幕外

            // 记录生成时的底部位置 (只在第一帧记录)
            if (Projectile.localAI[0] == 0)
            {
                Projectile.localAI[0] = Projectile.Center.Y; // 记录底部Y坐标

                // 屏幕震动
                Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(Projectile.Center, new Vector2(0, 1), 12f, 8f, 10, 1000f));
            }

            float bottomY = Projectile.localAI[0];
            // 计算顶部Y (屏幕顶端再往上 200 像素，确保看不到头)
            float topY = Main.screenPosition.Y - 200f;

            // 计算总高度
            float totalHeight = bottomY - topY;
            if (totalHeight < 100) totalHeight = 100; // 防止负数

            // 更新弹幕高度 (改变碰撞箱)
            Projectile.height = (int)totalHeight;

            // 更新弹幕中心点 (使其底部保持在 bottomY)
            Projectile.position.Y = topY;
            // X轴保持不变 (稍微修正中心对齐)
            // Projectile.position.X 已经在生成时确定了

            // === 2. 动画播放 ===
            // 每 5 帧切一张图
            if (++Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= 3)
                {
                    Projectile.Kill();
                }
            }

            // === 3. 环境光 ===
            // 在雷击路径上打几个光点
            for (float y = topY; y < bottomY; y += 100f)
            {
                Lighting.AddLight(new Vector2(Projectile.Center.X, y), 0.6f, 0.8f, 1.0f);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // 简单的矩形碰撞即可，因为我们已经动态修改了 Projectile.height
            return projHitbox.Intersects(targetHitbox);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 300); // 感电
            target.AddBuff(BuffID.Frostburn2, 300);  // 冻伤 (模拟极寒天气)
        }

        // === 核心绘制：自适应拉伸 ===
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;

            // 单帧高度
            int frameHeight = texture.Height / Main.projFrames[Projectile.type]; // 约 333

            // 获取当前帧的剪裁区域
            Rectangle sourceRect = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);

            // 锚点设为【底部中心】
            Vector2 origin = new Vector2(texture.Width / 2f, frameHeight);

            // 绘制位置：弹幕的底部中心
            // 因为我们在 AI 里把 Projectile.height 拉长了，所以 Bottom 才是我们的基准点
            Vector2 drawPos = Projectile.Bottom - Main.screenPosition;

            // 计算拉伸比例
            // 我们希望贴图的高度 (frameHeight) 被拉伸到 弹幕的高度 (Projectile.height)
            // 注意：这里 Projectile.height 已经在 AI 里被动态修改为“从天到地”的距离了
            float scaleY = (float)Projectile.height / frameHeight;

            // 使用加法混合 (让雷电发光)
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(
                texture,
                drawPos,
                sourceRect,
                Color.White, // 最亮
                Projectile.rotation,
                origin,
                new Vector2(1.0f, scaleY), // X轴不变，Y轴拉伸
                SpriteEffects.None,
                0
            );

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}