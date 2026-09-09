using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace zhashi.Content.Projectiles
{
    public class AurmirTwilightOrb : ModProjectile
    {
        // 【关键修复】直接使用数字 101 (混沌球的图片ID)，避开 ProjectileID 报错
        public override string Texture => "Terraria/Images/Projectile_101";

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.hostile = true; // 敌对弹幕
            Projectile.friendly = false;
            Projectile.tileCollide = false; // 穿墙
            Projectile.timeLeft = 300;
            Projectile.alpha = 255; // 隐形，靠粒子显示
        }

        public override void AI()
        {
            // 生成橘红色粒子
            for (int i = 0; i < 2; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.OrangeTorch, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].scale = 2f;
            }

            // 追踪逻辑
            // 检查传入的 ai[0] 是否有效
            int targetIndex = (int)Projectile.ai[0];
            if (targetIndex >= 0 && targetIndex < Main.maxPlayers)
            {
                Player target = Main.player[targetIndex];
                if (target.active && !target.dead)
                {
                    Vector2 direction = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                    // 缓慢转向
                    Projectile.velocity = (Projectile.velocity * 20f + direction * 10f) / 21f;
                }
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 180);
            target.AddBuff(BuffID.Weak, 300);
        }
    }
}