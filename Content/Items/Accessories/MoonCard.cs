using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content; // 引用 LotMPlayer

namespace zhashi.Content.Items.Accessories
{
    public class MoonCard : BlasphemyCardBase
    {
        public override void SafeSetDefaults()
        {
            Item.width = 28;
            Item.height = 34;
            Item.accessory = true;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(platinum: 5);
            Item.maxStack = 1;
        }

        public override void SafeUpdateAccessory(Player player, LotMPlayer mp, bool hideVisual)
        {
            // 1. 核心标记
            mp.isMoonCardEquipped = true;

            // 2. 基础效果：月之眼
            player.nightVision = true;      // 夜视
            player.gills = true;            // 水下呼吸 (潮汐权柄)
            player.ignoreWater = true;      // 水中行动自如

            // 3. 途径专属加成 (需月亮途径序列5及以上)
            // 假设变量名为 baseMoonSequence，如未定义请在 LotMPlayer 中添加
            if (mp.baseMoonSequence <= 9)
            {
                // === [暗夜眷属] ===
                // 月亮途径擅长召唤和控制
                player.maxMinions += 3;
                player.GetDamage(DamageClass.Summon) += 0.35f;
                player.whipRangeMultiplier += 0.5f;

                // === [深红领域] ===
                // 自动吸血光环
                if (player.whoAmI == Main.myPlayer && Main.GameUpdateCount % 40 == 0) // 每40帧(约0.6秒)触发一次
                {
                    float range = 400f;
                    int baseDmg = (int)(50 * player.GetDamage(DamageClass.Summon).Multiplicative);
                    bool healed = false; // 限制单次判定回血次数

                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC target = Main.npc[i];
                        if (target.active && !target.friendly && !target.dontTakeDamage && target.lifeMax > 5)
                        {
                            if (player.Distance(target.Center) < range)
                            {
                                // 造成伤害
                                player.ApplyDamageToNPC(target, baseDmg, 0f, 0, false);

                                // 施加 Debuff：暗影焰 + 困惑 (疯狂)
                                target.AddBuff(BuffID.ShadowFlame, 120);
                                target.AddBuff(BuffID.Confused, 60);

                                // 吸血逻辑：每次触发光环，如果有敌人受伤，玩家回血
                                if (!healed && player.statLife < player.statLifeMax2)
                                {
                                    int healAmount = 2; // 每次回2点，配合高频率还可以
                                    player.statLife += healAmount;
                                    player.HealEffect(healAmount);
                                    healed = true; // 每次光环跳动只回一次，防止怪多瞬间回满
                                }

                                // 特效：红色血雾
                                Dust.NewDust(target.position, target.width, target.height, DustID.VampireHeal, 0, 0, 0, default, 1.2f);
                            }
                        }
                    }
                }

                // === [月之庇护] ===
                // 夜晚大幅增强
                if (!Main.dayTime)
                {
                    player.statDefense += 15;
                    player.lifeRegen += 5;
                    player.GetCritChance(DamageClass.Generic) += 10;

                    // 月光漫步：移动速度
                    player.moveSpeed += 0.2f;
                    player.accRunSpeed += 2f;
                }
            }
        }
    }
}