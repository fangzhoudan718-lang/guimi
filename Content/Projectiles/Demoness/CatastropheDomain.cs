using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content; // 引用你的主命名空间

namespace zhashi.Content.Projectiles.Demoness
{
    // 这是一个"控制器"弹幕，负责制造伤害和特效
    public class CatastropheDomain : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.ShadowBeamFriendly; // 随便用个隐形贴图，或者写 "zhashi/Assets/Invisible"

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 60; // 持续1秒（特效瞬间触发，或者你可以设长一点做持续伤害）
            Projectile.alpha = 255;   // 完全透明
            Projectile.penetrate = -1; // 无限穿透
        }

        public override void AI()
        {
            // 只在第一帧触发逻辑 (相当于 Instant Cast)
            if (Projectile.ai[0] == 0)
            {
                TriggerEffects();
                Projectile.ai[0] = 1; // 标记已触发
            }
        }

        private void TriggerEffects()
        {
            Player owner = Main.player[Projectile.owner];

            // 1. 音效
            SoundEngine.PlaySound(SoundID.Item122, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Roar, Projectile.Center);

            // 2. 改变天候 (视觉效果 - 仅客户端执行)
            if (Main.netMode != NetmodeID.Server)
            {
                Main.StartRain();
                Main.windSpeedCurrent = 1.0f;
            }
            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.WorldData); // 服务器同步天气

            // 3. 全屏毁灭打击
            Vector2 center = owner.Center;
            float range = 2000f;

            foreach (NPC target in Main.ActiveNPCs)
            {
                if (target.friendly || target.dontTakeDamage) continue;
                if (target.Distance(center) > range) continue;

                int finalDmg = Projectile.damage; // 伤害由玩家发射时传入

                // A. Debuff
                target.AddBuff(BuffID.Frozen, 120);
                target.AddBuff(BuffID.Frostburn2, 600);
                target.AddBuff(BuffID.Venom, 600);
                target.AddBuff(BuffID.BetsysCurse, 600);

                // B. 造成伤害 (直接应用伤害，或者生成子弹幕)
                // 注意：Projectile 可以直接用 OnHitNPC 逻辑，但这种全屏AOE手动判定更稳
                owner.ApplyDamageToNPC(target, finalDmg, 0, 0, false); // 冰霜
                owner.ApplyDamageToNPC(target, finalDmg, 0, 0, false); // 飓风

                // C. 粒子特效
                for (int i = 0; i < 10; i++)
                {
                    Dust.NewDust(target.position, target.width, target.height, DustID.IceTorch, 0, 0, 0, default, 2f);
                    Dust.NewDust(target.position, target.width, target.height, DustID.CursedTorch, 0, 0, 0, default, 2f);
                }
            }

            // 4. 召唤视觉弹幕 (冰雹/暴风雪)
            // 限制数量，防止卡顿
            for (int i = 0; i < 20; i++)
            {
                Vector2 spawnPos = center + Main.rand.NextVector2Circular(800, 600);
                Vector2 velocity = (Main.MouseWorld - spawnPos).SafeNormalize(Vector2.Zero) * 15f;

                // 生成暴风雪弹幕
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPos,
                    velocity,
                    ProjectileID.Blizzard,
                    (int)(Projectile.damage * 0.5f),
                    5f,
                    owner.whoAmI
                );
            }
        }
    }
}