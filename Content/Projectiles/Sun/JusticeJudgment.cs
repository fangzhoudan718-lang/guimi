using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.GameContent;
using zhashi.Content; // 【关键】引用 Content 命名空间以访问 LotMPlayer

namespace zhashi.Content.Projectiles.Sun
{
    public class JusticeJudgment : ModProjectile
    {
        // 自动读取 JusticeJudgment.png

        public override void SetDefaults()
        {
            // 碰撞箱 (如果你的贴图很大，可以适当改大这里的数值，比如 40, 40)
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 0;

            // 【修改1】变大！从 1.2 -> 1.6
            Projectile.scale = 1.6f;
        }

        public override void AI()
        {
            // 1. 出生那一帧的特效 (只执行一次)
            if (Projectile.ai[0] == 0)
            {
                Projectile.ai[0] = 1;
                SoundEngine.PlaySound(SoundID.Item122, Projectile.Center);

                // 【修改2】释放时的屏幕震动
                Player owner = Main.player[Projectile.owner];
                if (owner.active && !owner.dead)
                {
                    LotMPlayer modPlayer = owner.GetModPlayer<LotMPlayer>();
                    modPlayer.shakeTime = 5;  // 震动5帧
                    modPlayer.shakePower = 3f; // 轻微震动
                }

                // 【修改3】出生视觉反馈：爆发一圈光环
                for (int i = 0; i < 20; i++)
                {
                    Vector2 speed = Main.rand.NextVector2Circular(6, 6);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, speed, 0, default, 2.5f);
                    d.noGravity = true;
                }
            }

            // 2. 加速下落
            Projectile.velocity.Y += 1.5f;

            // 3. 旋转 (保持剑尖朝下)
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // 4. 飞行拖尾 (加密度)
            // 现在每一帧都生成，而不是随机生成，让拖尾更连贯
            Vector2 tailPos = Projectile.Center - Projectile.velocity * 0.5f;
            Dust trail = Dust.NewDustPerfect(tailPos, DustID.GoldFlame, Vector2.Zero, 0, default, 1.5f);
            trail.noGravity = true;
            trail.velocity *= 0.1f;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

            // 【修改4】命中时的剧烈屏幕震动
            Player owner = Main.player[Projectile.owner];
            if (owner.active && !owner.dead)
            {
                LotMPlayer modPlayer = owner.GetModPlayer<LotMPlayer>();
                modPlayer.shakeTime = 15; // 震动15帧 (0.25秒)
                modPlayer.shakePower = 10f; // 强力震动！
            }

            // 【修改5】爆炸特效升级
            // 1. 核心爆炸
            for (int i = 0; i < 30; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.SolarFlare, Main.rand.NextVector2Circular(10, 10), 0, default, 3f);
                d.noGravity = true;
            }

            // 2. 十字架光爆 (范围更大)
            for (int i = 0; i < 60; i++)
            {
                // 横向
                Vector2 speedX = new Vector2(Main.rand.NextFloat(-15, 15), 0);
                Dust d1 = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, speedX, 0, default, 2.5f);
                d1.noGravity = true;

                // 纵向
                Vector2 speedY = new Vector2(0, Main.rand.NextFloat(-20, 10)); // 偏向上喷发
                Dust d2 = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, speedY, 0, default, 2.5f);
                d2.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() / 2f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.ZoomMatrix);

            // 绘制残影
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                Vector2 drawPos = Projectile.oldPos[i] - Main.screenPosition + Projectile.Size / 2f;
                // 颜色稍微加深一点，让残影更明显
                Color color = new Color(255, 215, 0, 100) * 0.5f;
                Main.spriteBatch.Draw(texture, drawPos, null, color, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }

            // 绘制本体 (高亮)
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.ZoomMatrix);

            return false;
        }
    }
}