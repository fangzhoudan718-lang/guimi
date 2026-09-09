using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.DamageClasses;

namespace zhashi.Content.Projectiles.Ritual
{
    public class RitualFallingStar : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // 开启拖尾记录，用于绘制残影
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        // 使用原版星怒弹幕的贴图
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Starfury;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = ModContent.GetInstance<RitualDamage>();
            Projectile.tileCollide = false; // 初始不碰撞，由 AI 控制何时开启
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.scale = 1f;
            Projectile.light = 0.8f;

            // 【修复点】直接使用数字 5，代表坠落星星/星怒的 AI 风格
            // 5 = Falling Star AI (旋转 + 加速下落)
            Projectile.aiStyle = 5;
        }

        public override void AI()
        {
            // 目标Y坐标存在 ai[1] 中 (由画笔传入)
            float targetY = Projectile.ai[1];

            // 既然使用了 aiStyle = 5，原版 AI 会自动处理旋转和重力加速
            // 我们只需要额外控制何时开启碰撞：当接近目标高度时开启
            // 否则它可能在穿墙过程中撞到天花板
            if (Projectile.Center.Y > targetY - 32f)
            {
                Projectile.tileCollide = true;
            }

            // 视觉特效：粉紫色拖尾粒子
            if (Main.rand.NextBool(2))
            {
                int dustType = Main.rand.NextBool() ? DustID.PinkTorch : DustID.PurpleCrystalShard;
                Dust d = Dust.NewDustPerfect(Projectile.Center, dustType, -Projectile.velocity * 0.2f);
                d.noGravity = true;
                d.scale = Main.rand.NextFloat(0.8f, 1.2f);
            }
        }

        public override void OnKill(int timeLeft)
        {
            // 爆炸音效
            SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);

            // 爆炸视觉特效：粉紫色能量爆发
            for (int i = 0; i < 20; i++)
            {
                int dustType = Main.rand.NextBool() ? DustID.PinkTorch : DustID.PurpleCrystalShard;
                Vector2 speed = Main.rand.NextVector2Circular(4f, 4f);
                Dust d = Dust.NewDustPerfect(Projectile.Center, dustType, speed);
                d.noGravity = true;
                d.scale = 1.5f;
            }
            // 扩散星芒
            for (int i = 0; i < 10; i++)
            {
                Vector2 speed = Main.rand.NextVector2CircularEdge(6f, 6f);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GemAmethyst, speed);
                d.noGravity = true;
                d.scale = 1.2f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            // 绘制残影
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                Vector2 drawPos = Projectile.oldPos[i] - Main.screenPosition + Projectile.Size / 2f;
                Color color = new Color(255, 100, 255) * ((float)(Projectile.oldPos.Length - i) / Projectile.oldPos.Length);
                Main.spriteBatch.Draw(tex, drawPos, null, color * 0.5f, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return true;
        }
    }
}