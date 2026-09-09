using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using System;

namespace zhashi.Content.Projectiles.Sun
{
    public class HolyLightBeam : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        public override void SetDefaults()
        {
            Projectile.width = 60; // 判定范围加宽
            Projectile.height = 1600; // 贯穿整个屏幕的高度
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90;  // 持续时间延长到 1.5秒，让特效充分展示
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
        }

        public override void AI()
        {
            // 1. 初始震撼音效
            if (Projectile.timeLeft == 90)
            {
                SoundEngine.PlaySound(SoundID.Item122, Projectile.Center);
                SoundEngine.PlaySound(SoundID.Item29, Projectile.Center); // 时空扭曲声
                SoundEngine.PlaySound(SoundID.DD2_EtherianPortalOpen, Projectile.Center); // 传送门开启声

                // 剧烈震动
                if (Main.myPlayer == Projectile.owner)
                {
                    Main.LocalPlayer.GetModPlayer<LotMPlayer>().shakeTime = 15;
                    Main.LocalPlayer.GetModPlayer<LotMPlayer>().shakePower = 8f;
                }
            }

            Vector2 center = Projectile.Center;
            float height = Projectile.height;

            // 2. 核心：极高密度的光流 (每一帧都刷新)
            for (int i = 0; i < 40; i++) // 粒子数量翻倍
            {
                // 随机分布在光柱高度上
                float yOffset = Main.rand.NextFloat(-height / 2, height / 2);
                Vector2 pos = center + new Vector2(Main.rand.NextFloat(-10, 10), yOffset);

                // A. 纯白核心 (模拟激光)
                Dust core = Dust.NewDustPerfect(pos, DustID.WhiteTorch, new Vector2(0, -8f), 0, default, 1.5f);
                core.noGravity = true;

                // B. 金色烈焰外壳
                Vector2 outerPos = center + new Vector2(Main.rand.NextFloat(-30, 30), yOffset);
                Dust gold = Dust.NewDustPerfect(outerPos, DustID.GoldFlame, new Vector2(0, -4f), 0, default, 2.0f);
                gold.noGravity = true;
            }

            // 3. 华丽特效：双重神圣螺旋
            // 利用时间变量 (time) 来计算正弦波
            float time = (90 - Projectile.timeLeft) * 0.3f;

            // 遍历整个高度，画出两条螺旋线
            for (float h = -height / 2; h < height / 2; h += 20)
            {
                // 螺旋半径
                float radius = 50f + (float)Math.Sin(time + h * 0.01f) * 10f; // 半径也会呼吸

                // 左螺旋
                float angle1 = time + h * 0.02f;
                Vector2 pos1 = center + new Vector2((float)Math.Cos(angle1) * radius, h);
                if (Main.rand.NextBool(3)) // 稍微稀疏一点，不用每点都画
                {
                    Dust d = Dust.NewDustPerfect(pos1, DustID.Enchanted_Gold, Vector2.Zero, 0, default, 1.2f);
                    d.noGravity = true;
                }

                // 右螺旋 (相位差 PI)
                float angle2 = time + h * 0.02f + 3.14159f;
                Vector2 pos2 = center + new Vector2((float)Math.Cos(angle2) * radius, h);
                if (Main.rand.NextBool(3))
                {
                    Dust d = Dust.NewDustPerfect(pos2, DustID.UltraBrightTorch, Vector2.Zero, 0, default, 1.2f);
                    d.noGravity = true;
                }
            }

            // 4. 地面冲击波 (不断扩散)
            Vector2 bottom = center + new Vector2(0, height / 2);
            // 每5帧生成一圈扩散波纹
            if (Projectile.timeLeft % 5 == 0)
            {
                for (int k = 0; k < 20; k++)
                {
                    Vector2 vel = Main.rand.NextVector2CircularEdge(15f, 2f); // 扁平扩散
                    Dust d = Dust.NewDustPerfect(bottom, DustID.SolarFlare, vel, 0, default, 2.5f);
                    d.noGravity = true;
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 300);
            target.AddBuff(BuffID.Daybreak, 120); // 破晓

            // 命中处炸开烟花
            for (int i = 0; i < 15; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(5f, 5f);
                Dust.NewDust(target.position, target.width, target.height, DustID.GoldFlame, vel.X, vel.Y, 0, default, 2f);
            }
        }
    }
}