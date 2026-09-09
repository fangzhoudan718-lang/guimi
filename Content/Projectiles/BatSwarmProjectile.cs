using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content;

namespace zhashi.Content.Projectiles
{
    public class BatSwarmProjectile : ModProjectile
    {
        public override string Texture => "zhashi/Content/Projectiles/BatSwarmProjectile";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4; // 确保图片高度能被4整除！
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.scale = Main.rand.NextFloat(0.7f, 1.1f);
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            // 【安全修复 1】检查 Owner 索引是否合法 (防止越界崩溃)
            if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers)
            {
                Projectile.Kill();
                return;
            }

            Player player = Main.player[Projectile.owner];

            // 【安全修复 2】确保 Player 有效且 ModPlayer 存在
            if (!player.active || player.dead)
            {
                Projectile.Kill();
                return;
            }

            // 使用 TryGetModPlayer 防止获取失败导致的空引用崩溃
            if (!player.TryGetModPlayer(out LotMPlayer modPlayer) || !modPlayer.isBatSwarm)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;

            // --- 1. 动画播放 ---
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            }

            // --- 2. 运动算法 ---
            float seed = Projectile.whoAmI * 1.3f;
            float time = (float)Main.timeForVisualEffects * 0.05f;

            float radiusX = 60f + (float)Math.Sin(time * 0.5f + seed) * 40f;
            float radiusY = 50f + (float)Math.Cos(time * 0.3f + seed) * 30f;

            Vector2 offset = new Vector2(
                (float)Math.Sin(time + seed) * radiusX,
                (float)Math.Cos(time * 0.8f + seed * 1.5f) * radiusY
            );

            Vector2 targetPos = player.Center + offset;

            // 【安全修复 3】防止 targetPos 为 NaN (导致绘制崩溃)
            if (float.IsNaN(targetPos.X) || float.IsNaN(targetPos.Y))
            {
                targetPos = player.Center; // 回滚到玩家中心
            }

            // --- 3. 跟随逻辑 ---
            if (Vector2.Distance(Projectile.Center, targetPos) > 500f)
            {
                Projectile.Center = targetPos;
            }
            else
            {
                Projectile.Center = Vector2.Lerp(Projectile.Center, targetPos, 0.2f);
            }

            // --- 4. 索敌 ---
            NPC target = FindClosestNPC(400f);
            if (target != null && target.active) // 再次检查 target.active
            {
                // SafeNormalize 防止除以0
                Vector2 attackDir = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                Projectile.Center += attackDir * 3f;
            }

            // --- 5. 朝向与旋转 ---
            Vector2 delta = Projectile.Center - Projectile.oldPosition;
            if (delta.X != 0) Projectile.spriteDirection = delta.X > 0 ? -1 : 1;

            // 【安全修复 4】防止 Rotation 变成 NaN (最常见的绘制崩溃原因)
            float newRot = delta.X * 0.1f;
            if (!float.IsNaN(newRot))
            {
                Projectile.rotation = newRot;
            }
            else
            {
                Projectile.rotation = 0f;
            }

            // --- 6. 粒子 ---
            if (Main.rand.NextBool(30))
            {
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, 0, 0, 150, default, 0.6f);
                d.noGravity = true;
            }
        }

        // 优化：改用 for 循环遍历，比 foreach ActiveNPCs 更稳定，防止集合修改异常
        private NPC FindClosestNPC(float maxDist)
        {
            NPC closest = null;
            float sqrMaxDist = maxDist * maxDist;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.friendly && !npc.dontTakeDamage)
                {
                    float sqrDist = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                    if (sqrDist < sqrMaxDist)
                    {
                        sqrMaxDist = sqrDist;
                        closest = npc;
                    }
                }
            }
            return closest;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ArmorPenetration += 20;
        }
    }
}