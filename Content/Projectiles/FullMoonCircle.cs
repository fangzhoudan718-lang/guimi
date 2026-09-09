using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using zhashi.Content;

namespace zhashi.Content.Projectiles
{
    public class FullMoonCircle : ModProjectile
    {
        public override string Texture => "zhashi/Content/Projectiles/FullMoonCircle";

        public override void SetDefaults()
        {
            Projectile.width = 130;
            Projectile.height = 54;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.scale = 0.1f;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            if (!player.active || player.dead)
            {
                Projectile.Kill();
                return;
            }

            LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

            if (!modPlayer.isFullMoonActive)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;

            // 吸附在玩家脚下 (微调 Y 轴让它贴地)
            Projectile.Center = player.Bottom + new Vector2(0, -2f);

            // 淡入
            if (Projectile.alpha > 50)
            {
                Projectile.alpha -= 15;
            }

            // 展开动画
            Projectile.scale = MathHelper.Lerp(Projectile.scale, 1.0f, 0.1f);

            // 不旋转，固定在脚下
            Projectile.rotation = 0f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() / 2f;

            // 【核心修改】
            // 1. 改为白色 (月光)
            Color color = Color.White * 0.9f;

            // 2. Alpha 设为 0
            // 这会触发加法混合 (Additive Blending)，让贴图变得像光一样透亮
            // 并且能完美消除图片边缘的锯齿和黑边
            color.A = 0;

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                color,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0
            );
            return false;
        }
    }
}