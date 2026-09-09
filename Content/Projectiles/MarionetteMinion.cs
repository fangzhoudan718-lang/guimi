using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using System;

namespace zhashi.Content.Projectiles
{
    public class MarionetteMinion : ModProjectile
    {
        // 指向你的新图片路径
        public override string Texture => "zhashi/Content/Projectiles/MarionetteMinion";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1; // 如果后续想加动画，记得改这里的帧数

            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 50;

            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18000;
            Projectile.DamageType = DamageClass.Summon;

            // 使用原版史莱姆 AI
            Projectile.aiStyle = 26;
            AIType = ProjectileID.BabySlime;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead) { Projectile.Kill(); return; }

            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.rotation = 0f;

            // 处理朝向
            if (Projectile.velocity.X > 0.1f) Projectile.spriteDirection = -1;
            else if (Projectile.velocity.X < -0.1f) Projectile.spriteDirection = 1;

            // 粒子特效
            if (Main.rand.NextBool(20))
            {
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.SilverCoin, 0, 0, 0, default, 0.8f);
                d.noGravity = true;
            }

            // 防卡住
            if (Projectile.Distance(player.Center) > 2000f)
            {
                Projectile.Center = player.Center;
                Projectile.velocity = Vector2.Zero;
            }

            CheckContactDamage(player);
            Projectile.frame = 0;
        }

        private void CheckContactDamage(Player player)
        {
            Rectangle myRect = Projectile.getRect();
            foreach (NPC target in Main.ActiveNPCs)
            {
                if (!target.friendly && !target.dontTakeDamage && target.lifeMax > 5 && myRect.Intersects(target.getRect()))
                {
                    if (target.immune[Projectile.owner] == 0 && Projectile.localNPCImmunity[target.whoAmI] == 0)
                    {
                        int damage = Projectile.damage;
                        target.SimpleStrikeNPC(damage, 0, Main.rand.NextBool(4), 0, DamageClass.Summon, true, player.luck);
                        target.immune[Projectile.owner] = 20;
                        Projectile.localNPCImmunity[target.whoAmI] = 20;

                        Projectile.velocity.X *= -0.5f;
                        Projectile.velocity.Y = -4f;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10, target.Center);
                    }
                }
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.velocity.X != oldVelocity.X)
            {
                Projectile.velocity.X = -oldVelocity.X * 0.2f;
            }
            return false;
        }

        // ========================================================
        // 【新增】绘制函数：将新贴图染成银白色
        // ========================================================
        public override bool PreDraw(ref Color lightColor)
        {
            // 1. 获取贴图资源
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

            // 2. 获取“光亮银染料”的着色器 ID (Bright Silver Dye)
            // 如果你觉得太亮了，可以改成 ItemID.SilverDye (普通银染料)
            int shaderId = GameShaders.Armor.GetShaderIdFromItemId(ItemID.BrightSilverDye);

            // 3. 计算绘制区域 (处理动画帧，虽然目前只有1帧)
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            int startY = frameHeight * Projectile.frame;
            Rectangle sourceRect = new Rectangle(0, startY, texture.Width, frameHeight);

            // 4. 计算绘制中心点
            Vector2 origin = sourceRect.Size() / 2f;

            // 5. 计算绘制位置 (修正屏幕坐标)
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            drawPos.Y += Projectile.gfxOffY; // 加上这个可以让贴图在斜坡上移动更平滑

            // 6. 确定翻转方向
            SpriteEffects effects = Projectile.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            // 7. 开启特殊的绘制模式 (Shader模式)
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);

            // 8. 应用银色 Shader
            GameShaders.Armor.GetSecondaryShader(shaderId, Main.LocalPlayer).Apply(null);

            // 9. 绘制本体 (注意：这里颜色传 Color.White 以保证最大亮度，或者传 lightColor 让它受环境光影响)
            Main.EntitySpriteDraw(texture, drawPos, sourceRect, Color.White, Projectile.rotation, origin, Projectile.scale, effects, 0);

            // 10. 结束 Shader 绘制，恢复默认绘制模式 (非常重要，否则后续UI会乱掉)
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.ZoomMatrix);

            // 返回 false 告诉系统：“我已经画完了，你不要再用默认方式画一遍了”
            return false;
        }
    }
}