using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Projectiles
{
    public class HistoricalSceneProjectile : ModProjectile
    {
        // 借用星云球的贴图作为占位符，但实际上我们把它隐藏了，只显示特效
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.NebulaSphere;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;

            // 本体完全隐形，只靠粒子特效
            Projectile.alpha = 255;

            Projectile.timeLeft = 36000; // 10分钟
        }

        // 领域半径 (1200像素 = 约75格，足够覆盖屏幕)
        private const float RADIUS = 1200f;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            // 1. 始终保持静止 (定在召唤的位置)
            Projectile.velocity = Vector2.Zero;

            // 2. 【视觉特效核心】构建历史场景氛围
            // 每一帧生成多个粒子，营造浓厚的氛围
            for (int i = 0; i < 5; i++)
            {
                // A. 随机生成内部迷雾 (历史感)
                // 在半径范围内随机取点
                Vector2 fogPos = Projectile.Center + Main.rand.NextVector2Circular(RADIUS, RADIUS);

                // 距离中心越远，粒子越少/越透明，形成自然边缘
                float dist = Vector2.Distance(Projectile.Center, fogPos);
                if (Main.rand.NextFloat() < (1f - dist / RADIUS))
                {
                    // 生成银色/灰色烟雾
                    // DustID.SilverCoin 是一种好看的亮银色
                    Dust d = Dust.NewDustPerfect(fogPos, DustID.SilverCoin, Vector2.Zero, 150, Color.Gray, 1.0f);
                    d.noGravity = true; // 悬浮
                    d.velocity = Main.rand.NextVector2Circular(1f, 1f); // 缓慢飘动
                    d.fadeIn = 1.5f; // 淡入效果
                }

                // B. 随机生成金色光点 (奇迹感)
                if (Main.rand.NextBool(10))
                {
                    Vector2 lightPos = Projectile.Center + Main.rand.NextVector2Circular(RADIUS, RADIUS);
                    Dust d2 = Dust.NewDustPerfect(lightPos, DustID.Enchanted_Gold, Vector2.Zero, 150, default, 0.8f);
                    d2.noGravity = true;
                    d2.velocity *= 0.5f;
                }
            }

            // C. 边缘光圈 (可选，让玩家知道边界在哪，如果不想要可以删掉)
            if (Main.GameUpdateCount % 2 == 0)
            {
                Vector2 borderPos = Projectile.Center + Main.rand.NextVector2CircularEdge(RADIUS, RADIUS);
                Dust border = Dust.NewDustPerfect(borderPos, DustID.GoldFlame, Vector2.Zero, 200, default, 2f);
                border.noGravity = true;
                border.velocity *= 0.1f; // 几乎不动
            }

            // 3. 施加 Buff 逻辑
            foreach (Player p in Main.ActivePlayers)
            {
                if (p.Distance(Projectile.Center) < RADIUS)
                {
                    // 历史的庇护：全套生存Buff
                    p.AddBuff(BuffID.Campfire, 2);
                    p.AddBuff(BuffID.HeartLamp, 2);
                    p.AddBuff(BuffID.StarInBottle, 2);
                    p.AddBuff(BuffID.DryadsWard, 2); // 树妖祝福 (防御+反伤)
                    p.AddBuff(BuffID.Honey, 2);      // 蜂蜜回复

                    // 奇迹师本人额外加成
                    if (p.GetModPlayer<Content.LotMPlayer>().currentFoolSequence <= 2)
                    {
                        p.statDefense += 20;
                        p.endurance += 0.1f;
                    }
                }
            }

            // 4. 环境光照 (让区域内微微发亮)
            Lighting.AddLight(Projectile.Center, 0.6f, 0.6f, 0.7f);
        }
    }
}