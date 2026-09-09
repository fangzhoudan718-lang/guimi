using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Events; // 引用天气事件
using zhashi.Content.Systems;

namespace zhashi.Content.Projectiles.Demoness
{
    public class CatastropheController : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.ShadowBeamFriendly;

        ref float Timer => ref Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 180; // 持续3秒
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            Projectile.Center = owner.Center;

            // --- 第1帧：启动天灾环境 ---
            if (Timer == 0)
            {
                // 音效：雷鸣 + 咆哮
                SoundEngine.PlaySound(SoundID.Roar, owner.Center);
                SoundEngine.PlaySound(SoundID.Item122, owner.Center);

                // 屏幕剧烈震动
                OwnerScreenShake(15f);

                // 强制改变全图天气 (仅服务器/单人执行)
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // 狂风
                    Main.windSpeedCurrent = Main.windSpeedTarget = (Main.rand.NextBool() ? 1f : -1f);

                    // 暴雨
                    Main.raining = true;
                    Main.rainTime = 3600 * 5;
                    Main.maxRaining = 1f;

                    // 沙尘暴 (如果在沙漠)
                    if (owner.ZoneDesert)
                    {
                        Sandstorm.Happening = true;
                        Sandstorm.TimeLeft = 3600 * 5;
                        Sandstorm.Severity = 1f;
                    }

                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.WorldData);
                    }
                }
            }

            // --- 持续期间：随机播放雷声 ---
            if (Timer % 45 == 0)
            {
                SoundEngine.PlaySound(SoundID.Thunder, owner.Center);
                OwnerScreenShake(3f); // 轻微余震
            }

            // --- 伤害逻辑：每5帧造成一次判定 ---
            if (Timer % 5 == 0)
            {
                DealGlobalDamage(owner);
            }

            Timer++;
        }

        private void DealGlobalDamage(Player owner)
        {
            float range = 3000f; // 3000像素范围内全屏攻击

            foreach (NPC target in Main.ActiveNPCs)
            {
                if (target.active && !target.friendly && !target.dontTakeDamage && target.Distance(owner.Center) < range)
                {
                    // 造成伤害
                    owner.ApplyDamageToNPC(target, Projectile.damage, 0f, 0, false);

                    // 施加 "天灾套餐" Debuff
                    target.AddBuff(BuffID.Frostburn2, 300); // 冻伤
                    target.AddBuff(BuffID.Venom, 300);      // 剧毒
                    target.AddBuff(BuffID.BetsysCurse, 300);// 破甲

                    // 生成受击特效 (替代龙卷风的视觉反馈)
                    // 在敌人身上炸开 冰霜 和 诅咒火 粒子
                    for (int i = 0; i < 3; i++)
                    {
                        Dust d = Dust.NewDustDirect(target.position, target.width, target.height, DustID.IceTorch, 0, 0, 0, default, 1.5f);
                        d.velocity *= 3f;
                        d.noGravity = true;

                        Dust d2 = Dust.NewDustDirect(target.position, target.width, target.height, DustID.CursedTorch, 0, 0, 0, default, 1.5f);
                        d2.velocity *= 3f;
                        d2.noGravity = true;
                    }
                }
            }
        }

        private void OwnerScreenShake(float intensity)
        {
            if (Main.netMode != NetmodeID.Server && Main.myPlayer == Projectile.owner)
            {
                Main.LocalPlayer.GetModPlayer<LotMPlayer>().screenShakeMagnitude = intensity;
            }
        }
    }
}