using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.GameContent;

namespace zhashi.Content.Projectiles.Sun
{
    public class PureWhiteLight : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.SolarWhipSwordExplosion; // 借用个图，实际上不画

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 60; // 持续1秒
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            // 1. 屏幕闪白 (纯白之光)
            if (Projectile.timeLeft > 40)
            {
                // 让整个屏幕变白，模拟强光
                Lighting.AddLight(Projectile.Center, 20f, 20f, 20f);
            }

            // 2. 只有第一帧造成伤害 (瞬间毁灭)
            if (Projectile.ai[0] == 0)
            {
                Projectile.ai[0] = 1;

                // 音效：空灵的神圣之声 + 爆炸
                SoundEngine.PlaySound(SoundID.Item29, Projectile.Center);
                SoundEngine.PlaySound(SoundID.Item122, Projectile.Center);

                // 全屏震动
                Main.screenPosition += Main.rand.NextVector2Circular(30, 30);

                // 核心逻辑：遍历全屏敌人进行净化
                float radius = 3000f; // 极大范围
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.friendly && !npc.dontTakeDamage && npc.Distance(Projectile.Center) < radius)
                    {
                        // 基础伤害由 Player 调用时传入
                        int dmg = Projectile.damage;

                        // 纯白特性：对非Boss单位造成 10倍 伤害 (几乎秒杀)
                        if (!npc.boss) dmg *= 10;

                        // 对不死/邪恶生物再翻倍
                        bool isEvil = NPCID.Sets.Zombies[npc.type] || NPCID.Sets.Skeletons[npc.type] || npc.aiStyle == 22;
                        if (isEvil) dmg *= 2;

                        // 造成真实伤害
                        Player owner = Main.player[Projectile.owner];
                        owner.ApplyDamageToNPC(npc, dmg, 0, 0, true);

                        // 强力Debuff：直接清除所有增益并附加神圣火焰
                        npc.AddBuff(BuffID.Daybreak, 1200);
                        npc.AddBuff(BuffID.Confused, 600);

                        // 视觉：敌人身上爆出白光
                        for (int k = 0; k < 10; k++)
                        {
                            Dust d = Dust.NewDustPerfect(npc.Center, DustID.WhiteTorch, Main.rand.NextVector2Circular(5, 5), 0, default, 2f);
                            d.noGravity = true;
                        }
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false; // 不绘制本体，全靠光效
        }
    }
}