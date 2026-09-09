using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;

namespace zhashi.Content.Projectiles.Sun
{
    public class SunFireTrap : ModProjectile
    {
        // 继续借用透明贴图，全靠代码画特效
        public override string Texture => "Terraria/Images/Projectile_0";

        public override void SetDefaults()
        {
            Projectile.width = 100; // 范围进一步扩大，更有压迫感
            Projectile.height = 100;
            Projectile.friendly = true;
            Projectile.penetrate = 50; // 几乎无限穿透
            Projectile.timeLeft = 1200; // 持续20秒
            Projectile.tileCollide = true;
            Projectile.alpha = 255;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20; // 伤害频率
        }

        public override void AI()
        {
            // 1. 物理逻辑：落地与吸附
            Projectile.velocity.X *= 0.9f;
            if (Projectile.velocity.Y < 10) Projectile.velocity.Y += 0.3f;

            // 只有落地后才开始展示“神迹”
            if (Projectile.velocity.Y == 0 || Projectile.velocity.Length() < 0.1f)
            {
                Vector2 center = Projectile.Center;
                float time = Main.GameUpdateCount * 0.05f; // 时间因子

                // =================================================
                // A. 地面法阵：旋转的日珥光环 (Solar Halo)
                // =================================================
                // 两个反向旋转的圈，制造复杂的魔法阵感
                float radius1 = 60f;
                float radius2 = 40f;

                for (int i = 0; i < 3; i++) // 每帧画3个点，利用视觉暂留形成圈
                {
                    // 外圈：顺时针
                    Vector2 offset1 = Vector2.UnitX.RotatedBy(time + i * 2.09f) * radius1; // 2.09 = 120度
                    Dust d1 = Dust.NewDustPerfect(center + offset1, DustID.GoldFlame, Vector2.Zero, 0, default, 1.5f);
                    d1.noGravity = true;

                    // 内圈：逆时针
                    Vector2 offset2 = Vector2.UnitX.RotatedBy(-time * 1.5f + i * 2.09f) * radius2;
                    Dust d2 = Dust.NewDustPerfect(center + offset2, DustID.SolarFlare, Vector2.Zero, 0, default, 1.2f);
                    d2.noGravity = true;
                }

                // =================================================
                // B. 核心爆发：日冕螺旋 (The Helix)
                // =================================================
                // 火焰不再是乱喷，而是双螺旋上升
                for (int k = 0; k < 2; k++)
                {
                    float phase = k * 3.14159f; // 相位差
                    float heightOffset = (float)Math.Sin(time * 3f + phase) * 20f;

                    // 螺旋上升的速度
                    Vector2 helixVel = new Vector2((float)Math.Cos(time * 5f + phase) * 2f, -6f - (float)Math.Abs(heightOffset) * 0.1f);

                    Dust d = Dust.NewDustPerfect(center + new Vector2(0, 10), DustID.UltraBrightTorch, helixVel, 0, default, 2.5f);
                    d.noGravity = true;
                    d.fadeIn = 1.2f;
                }

                // =================================================
                // C. 区域燃烧：熔岩地毯 (Lava Carpet)
                // =================================================
                // 在地面随机位置生成贴地流动的火
                for (int i = 0; i < 4; i++)
                {
                    Vector2 groundPos = center + new Vector2(Main.rand.NextFloat(-50, 50), 40);
                    // 检测是否在地面（防止画到虚空里）
                    if (WorldGen.SolidTile(groundPos.ToTileCoordinates())) continue;

                    Dust d = Dust.NewDustPerfect(groundPos, DustID.OrangeTorch, new Vector2(Main.rand.NextFloat(-2, 2), 0), 0, default, 1.8f);
                    d.noGravity = true;
                }

                // =================================================
                // D. 神圣氛围：升腾的符文与热浪 (Rising Runes)
                // =================================================
                if (Main.rand.NextBool(2)) // 高频率
                {
                    Vector2 randomPos = center + Main.rand.NextVector2Circular(50, 10);
                    Dust d = Dust.NewDustPerfect(randomPos, DustID.Enchanted_Gold, new Vector2(0, -1.5f), 0, default, 0.8f);
                    d.noGravity = true;
                    d.fadeIn = 1.5f; // 慢慢变亮再消失，像灰烬一样
                }

                // =================================================
                // E. 动态照明：像心脏一样跳动
                // =================================================
                float pulse = 1.2f + (float)Math.Sin(time * 2f) * 0.3f;
                Lighting.AddLight(center, 1.5f * pulse, 1.0f * pulse, 0.2f * pulse); // 强烈的金橙色光
            }
            else
            {
                // 空中轨迹：像流星一样划过，带有拖尾
                for (int i = 0; i < 3; i++)
                {
                    Vector2 offset = Main.rand.NextVector2Circular(10, 10);
                    Dust d = Dust.NewDustPerfect(Projectile.Center + offset, DustID.GoldFlame, Projectile.velocity * 0.2f, 0, default, 2.0f);
                    d.noGravity = true;
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 强力 Debuff 组合
            target.AddBuff(BuffID.OnFire3, 300); // 狱火
            target.AddBuff(BuffID.Daybreak, 180); // 破晓
            target.AddBuff(BuffID.Midas, 300);    // 金身
            target.AddBuff(BuffID.Confused, 120); // 混乱（被强光致盲）

            // 命中特效：受击点炸开太阳花纹
            for (int i = 0; i < 10; i++)
            {
                Vector2 vel = Vector2.UnitX.RotatedBy(i * 0.628f) * 4f; // 辐射状飞散
                Dust d = Dust.NewDustPerfect(target.Center, DustID.SolarFlare, vel, 0, default, 1.5f);
                d.noGravity = true;
            }
        }
    }
}