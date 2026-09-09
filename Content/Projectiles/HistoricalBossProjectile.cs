using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Projectiles
{
    public class HistoricalBossProjectile : ModProjectile
    {
        // 占位符
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.EnchantedBeam;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.minionSlots = 0f;
            Projectile.penetrate = -1;

            // 【修改1】持续时间改为 5分钟 (60帧 * 60秒 * 5)
            Projectile.timeLeft = 18000;

            Projectile.DamageType = DamageClass.Summon;
            Projectile.aiStyle = -1;

            // 独立无敌帧设置
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10; // 每秒攻击6次
        }

        public int TargetBossID
        {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        private bool hasResized = false;

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead) { Projectile.Kill(); return; }

            // =================================================
            // 1. 动态调整大小 (修复碰撞箱跑偏问题)
            // =================================================
            if (!hasResized && TargetBossID > 0)
            {
                Main.instance.LoadNPC(TargetBossID);
                Texture2D texture = Terraria.GameContent.TextureAssets.Npc[TargetBossID].Value;
                int frames = Main.npcFrameCount[TargetBossID];
                if (frames <= 0) frames = 1;

                int newWidth = texture.Width;
                int newHeight = texture.Height / frames;

                // 【核心修复】记录旧中心点，Resize后还原，防止碰撞箱瞬移
                Vector2 oldCenter = Projectile.Center;
                Projectile.Resize(newWidth, newHeight);
                Projectile.Center = oldCenter;

                hasResized = true;
            }

            // =================================================
            // 2. 属性重置
            // =================================================
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.minion = true;
            Projectile.rotation = 0f; // 禁止旋转

            // =================================================
            // 3. 索敌与移动 (幽灵冲撞)
            // =================================================
            NPC target = null;
            float maxRange = 3000f;

            if (player.HasMinionAttackTargetNPC)
            {
                NPC npc = Main.npc[player.MinionAttackTargetNPC];
                if (npc.CanBeChasedBy() && Projectile.Distance(npc.Center) < maxRange * 1.5f)
                    target = npc;
            }

            if (target == null)
            {
                float nearestDist = maxRange;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (npc.CanBeChasedBy() && Projectile.Distance(npc.Center) < nearestDist)
                    {
                        nearestDist = Projectile.Distance(npc.Center);
                        target = npc;
                    }
                }
            }

            Vector2 targetPos;
            float speed = 18f;
            float inertia = 30f;

            if (target != null)
            {
                targetPos = target.Center;
            }
            else
            {
                targetPos = player.Center + new Vector2(0, -100);
                speed = 10f;
            }

            Vector2 direction = (targetPos - Projectile.Center).SafeNormalize(Vector2.Zero);
            Projectile.velocity = (Projectile.velocity * (inertia - 1) + direction * speed) / inertia;

            if (Projectile.velocity.X > 0.5f) Projectile.spriteDirection = 1;
            else if (Projectile.velocity.X < -0.5f) Projectile.spriteDirection = -1;

            // =================================================
            // 4. 【核心修复】手动强制伤害检测
            // 只要重叠，直接扣血，不依赖原版判定
            // =================================================
            ForceContactDamage(player);

            // 5. 特效
            if (Main.rand.NextBool(5))
            {
                Vector2 randomPos = Projectile.position + new Vector2(Main.rand.Next(Projectile.width), Main.rand.Next(Projectile.height));
                Dust d = Dust.NewDustDirect(randomPos, 0, 0, DustID.Smoke, 0, 0, 150, Color.LightGray, 2f);
                d.noGravity = true;
                d.velocity *= 0.2f;
            }

            if (Projectile.Distance(player.Center) > 4000f)
            {
                Projectile.Center = player.Center;
            }
        }

        // 暴力伤害函数
        private void ForceContactDamage(Player player)
        {
            Rectangle myRect = Projectile.getRect();
            // 稍微缩小一点判定框，避免看起来没碰到却打到了
            myRect.Inflate(-10, -10);

            foreach (NPC target in Main.ActiveNPCs)
            {
                // 是敌人，且不是无敌的
                if (!target.friendly && !target.dontTakeDamage && target.lifeMax > 5)
                {
                    // 检测碰撞
                    if (myRect.Intersects(target.getRect()))
                    {
                        // 检查无敌帧 (防止每帧都打，秒伤过高)
                        if (target.immune[Projectile.owner] == 0 && Projectile.localNPCImmunity[target.whoAmI] == 0)
                        {
                            // 计算最终伤害
                            int damage = Projectile.damage;

                            // 应用伤害 (直接扣血)
                            target.SimpleStrikeNPC(damage, 0, Main.rand.NextBool(4), 0, DamageClass.Summon, true, player.luck);

                            // 设置无敌帧 (10帧 = 0.16秒)
                            target.immune[Projectile.owner] = 10;
                            Projectile.localNPCImmunity[target.whoAmI] = 10;

                            // 视觉/听觉反馈
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103, target.Center); // 幽灵撞击声
                        }
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int npcID = TargetBossID;
            if (npcID <= 0) return false;

            Main.instance.LoadNPC(npcID);
            Texture2D texture = Terraria.GameContent.TextureAssets.Npc[npcID].Value;

            int frames = Main.npcFrameCount[npcID];
            if (frames <= 0) frames = 1;
            int frameHeight = texture.Height / frames;
            Rectangle sourceRect = new Rectangle(0, 0, texture.Width, frameHeight);

            // 保持原尺寸
            float scale = 1f;
            if (texture.Width > 600) scale = 0.8f;

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 origin = sourceRect.Size() / 2f;

            // 灰白半透明
            Color ghostColor = Color.White * 0.8f;
            ghostColor.R = 200; ghostColor.G = 200; ghostColor.B = 200; ghostColor.A = 150;

            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            Main.EntitySpriteDraw(texture, drawPos, sourceRect, ghostColor, Projectile.rotation, origin, scale, effects, 0);

            return false;
        }
    }
}