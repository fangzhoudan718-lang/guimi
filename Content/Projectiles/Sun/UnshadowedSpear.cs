using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.GameContent;

namespace zhashi.Content.Projectiles.Sun
{
    public class UnshadowedSpear : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Daybreak;

        public override void SetDefaults()
        {
            // 【改动1】碰撞箱缩小到 14 (原40)，防止擦到地面
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 300;
            Projectile.light = 3f;

            // 初始不碰撞
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 3;
            Projectile.aiStyle = -1;
            Projectile.scale = 1.3f;
        }

        public override void AI()
        {
            // 【改动2】延长保护期到 10 帧 (约0.16秒)
            // 确保飞出玩家身体一段距离后才开启碰撞
            if (Projectile.ai[0] < 10)
            {
                Projectile.ai[0]++;
            }
            else
            {
                Projectile.tileCollide = true;
            }

            // 角度修正：+90度 (指向右方)
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // 纯白粒子特效
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.SilverFlame, -Projectile.velocity * 0.2f, 0, Color.White, 1.5f);
                d.noGravity = true;
                if (Main.rand.NextBool(3))
                {
                    Dust d2 = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Gold, Vector2.Zero, 0, Color.White, 1.0f);
                    d2.noGravity = true;
                }
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage *= 1.5f;
            modifiers.SetCrit();
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
            for (int i = 0; i < 50; i++)
            {
                Vector2 speed = Main.rand.NextVector2Circular(15, 15);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.WhiteTorch, speed, 0, default, 3f);
                d.noGravity = true;
                d.velocity *= 1.5f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() / 2f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.ZoomMatrix);

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                Vector2 drawPos = Projectile.oldPos[i] - Main.screenPosition + Projectile.Size / 2f;
                Main.spriteBatch.Draw(texture, drawPos, null, Color.White * 0.5f, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.ZoomMatrix);

            return false;
        }
    }
}