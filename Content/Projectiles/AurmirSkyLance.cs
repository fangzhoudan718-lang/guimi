using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Audio;

namespace zhashi.Content.Projectiles
{
    public class AurmirSkyLance : ModProjectile
    {
        // 借用光之女皇的剑贴图，你可以换成自己的
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.FairyQueenLance;

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 100;
            Projectile.hostile = true; // 敌对
            Projectile.friendly = false;
            Projectile.tileCollide = false; // 穿墙
            Projectile.timeLeft = 300;
            Projectile.alpha = 255; // 初始隐形
            Projectile.light = 1.0f;
        }

        public override void AI()
        {
            // 渐变显示
            if (Projectile.alpha > 0) Projectile.alpha -= 25;

            // 保持旋转角度与速度方向一致
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // 生成金色粒子拖尾
            if (Main.rand.NextBool(2))
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, 0, 0, 0, default, 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 0.5f;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 180); // 附带燃烧
        }
    }
}
