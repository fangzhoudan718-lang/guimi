using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Projectiles
{
    // === 序列 1：时之钟 (终极版：视觉稳定 + 伤害史诗级增强) ===
    public class DogTimeClock : ModProjectile
    {
        public override string Texture => "zhashi/Content/Projectiles/TimeClockVisual";

        public override void SetDefaults()
        {
            Projectile.width = 160;
            Projectile.height = 160;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 2;
            Projectile.alpha = 0;
            Projectile.scale = 0.6f;
            Projectile.penetrate = -1;
            Projectile.hide = true; // 保持背景绘制，防止闪烁
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }

        public override void AI()
        {
            int dogIndex = (int)Projectile.ai[0];
            if (dogIndex < 0 || dogIndex >= Main.maxNPCs || !Main.npc[dogIndex].active || Main.npc[dogIndex].type != ModContent.NPCType<NPCs.DogNPC>())
            {
                Projectile.Kill();
                return;
            }

            NPC dog = Main.npc[dogIndex];

            // 1. 位置与光照 (保持之前的完美视觉)
            Projectile.timeLeft = 2;
            Projectile.Center = dog.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation += 0.02f;
            Lighting.AddLight(Projectile.Center, 1.5f, 1.5f, 1.5f); // 保持常亮

            // 2. 领域逻辑 (伤害增强版)
            float range = 600f;
            foreach (NPC target in Main.ActiveNPCs)
            {
                if (target.whoAmI != dogIndex && !target.friendly && !target.dontTakeDamage && target.Distance(Projectile.Center) < range)
                {
                    // === A. 强力控制：时间几乎静止 ===
                    // 速度削减至 10% (原为 50%)
                    target.velocity *= 0.1f;

                    target.AddBuff(BuffID.Slow, 10);
                    target.AddBuff(BuffID.WitheredArmor, 10); // 减防
                    target.AddBuff(BuffID.WitheredWeapon, 10); // 减攻
                    target.AddBuff(BuffID.ShadowFlame, 10);    // 增加视觉特效

                    // === B. 伤害计算 (每 20 帧触发一次 = 每秒 3 次) ===
                    if (Main.GameUpdateCount % 20 == 0)
                    {
                        int decay = 0;

                        if (target.boss)
                        {
                            // --- Boss 伤害逻辑 ---
                            // 基础：2% 当前生命 + 500 固定伤害 (原为 0.1% + 20)
                            // 上限：单次最高 10000 (每秒 3万 DPS)
                            decay = (int)(target.life * 0.02f) + 500;
                            if (decay > 10000) decay = 10000;
                        }
                        else
                        {
                            // --- 小怪 伤害逻辑 ---
                            // 基础：10% 当前生命 + 2000 固定伤害 (原为 1% + 50)
                            // 效果：普通怪进圈基本上 2-3 秒内必死，体现“时间流逝”的恐怖
                            decay = (int)(target.life * 0.10f) + 2000;

                            // 稍微限制一下对地牢守卫这种特殊怪的伤害
                            if (decay > 50000) decay = 50000;
                        }

                        // 执行伤害
                        if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers)
                        {
                            // 参数 true 表示暴击，增加打击感
                            Main.player[Projectile.owner].ApplyDamageToNPC(target, decay, 0, 0, true);
                        }
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 保持之前的 NonPremultiplied 绘制，确保不闪烁、去白边、高亮
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() / 2f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.NonPremultiplied,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                RasterizerState.CullCounterClockwise,
                null,
                Main.GameViewMatrix.TransformationMatrix
            );

            Color color = Color.White * 0.8f;

            Main.EntitySpriteDraw(
                texture,
                drawPos,
                null,
                color,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                RasterizerState.CullCounterClockwise,
                null,
                Main.GameViewMatrix.TransformationMatrix
            );

            return false;
        }
    }
}