using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent;
using System; // 需要引用这个来使用 MathHelper

namespace zhashi.Content.Projectiles
{
    public class ConquerorSwordProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = 200;
            Projectile.height = 200;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.light = 1.2f;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.scale = 1f;
        }

        public override void AI()
        {
            // 【核心修复：调整旋转方向】
            // 请根据你实际的游戏效果，选择下面其中一行取消注释：

            // 方案 A：如果你的贴图是【头朝右】画的 (这是最标准的做法)
            Projectile.rotation = Projectile.velocity.ToRotation();

            // 方案 B：如果你的贴图是【头朝上】画的 (很多素材是这样的)
            // Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // 方案 C：如果你的贴图是【头朝右上 45度】画的 (标准剑类素材)
            // Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;


            // 2. 播放动画
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }
            }

            // 3. 添加粒子特效
            if (Main.rand.NextBool(2))
            {
                Vector2 dustPos = Projectile.Center + Main.rand.NextVector2Circular(100, 100);
                Dust d = Dust.NewDustPerfect(dustPos, DustID.Shadowflame, -Projectile.velocity * 0.2f, 100, default, 2f);
                d.noGravity = true;
            }
        }

        // 绘制逻辑不变...
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle sourceRect = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = new Vector2(texture.Width / 2f, frameHeight / 2f);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                Vector2 oldDrawPos = Projectile.oldPos[i] + new Vector2(Projectile.width / 2f, Projectile.height / 2f) - Main.screenPosition;
                Color color = Color.Purple * 0.3f * ((float)(Projectile.oldPos.Length - i) / Projectile.oldPos.Length);
                Main.EntitySpriteDraw(texture, oldDrawPos, sourceRect, color, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(texture, drawPos, sourceRect, Color.Purple * 0.5f, Projectile.rotation, origin, Projectile.scale * 1.1f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, drawPos, sourceRect, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Buffs.ConquerorWill>(), 600);
            for (int i = 0; i < 15; i++)
            {
                Dust.NewDust(target.position, target.width, target.height, DustID.Shadowflame, 0, 0, 0, default, 2.5f);
            }
        }
    }
}