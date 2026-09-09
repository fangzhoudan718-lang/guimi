using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content; // 引用 LotMPlayer
using zhashi.Content.Items;

namespace zhashi.Content.Items.Accessories
{
    public class RedPriestCard : BlasphemyCardBase
    {
        public override void SafeSetDefaults()
        {
            Item.width = 28;
            Item.height = 34;
            Item.accessory = true;
            Item.rare = ItemRarityID.Red; // 红色稀有度
            Item.value = Item.sellPrice(platinum: 5);
            Item.maxStack = 1;
        }

        public override void SafeUpdateAccessory(Player player, LotMPlayer mp, bool hideVisual)
        {
            // 1. 核心标记
            mp.isRedPriestCardEquipped = true;

            // 2. 基础效果：挑衅与耐热
            player.aggro += 1000; // 仇恨拉满，红祭司是战场的中心
            player.buffImmune[BuffID.OnFire] = true;
            player.buffImmune[BuffID.Burning] = true;
            player.buffImmune[BuffID.OnFire3] = true;

            // 3. 途径专属加成 (需猎人途径序列5及以上)
            if (mp.baseHunterSequence <= 9)
            {
                // === [战争之红：真·炼狱光环] ===
                player.inferno = true; // 保留原版视觉圈（仅作特效）

                if (player.whoAmI == Main.myPlayer)
                {
                    float range = 350f; // 光环半径 (约22格)
                    int baseDmg = (int)(50 * player.GetDamage(DamageClass.Generic).Multiplicative); // 基础伤害50 * 玩家伤害加成

                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC target = Main.npc[i];
                        if (target.active && !target.friendly && !target.dontTakeDamage && target.lifeMax > 5)
                        {
                            float distance = player.Distance(target.Center);
                            if (distance < range)
                            {
                                // A. 施加极强的混合燃烧效果 (持续1秒，离开光环即止)
                                target.AddBuff(BuffID.OnFire3, 60);    // 地狱火
                                target.AddBuff(BuffID.Oiled, 60);      // 涂油 (易燃)
                                target.AddBuff(BuffID.ShadowFlame, 60);// 暗影焰
                                target.AddBuff(BuffID.Daybreak, 60);   // 破晓 (堆叠Debuff)

                                // B. 简单的频率限制伤害 (利用 enemy 的无敌帧机制)
                                // 如果敌人不在无敌状态，强制造成一次伤害
                                // 注意：AddBuff 已经造成了很高的DoT伤害，这里补充直接打击感
                                if (Main.GameUpdateCount % 30 == 0)
                                {
                                    player.ApplyDamageToNPC(target, baseDmg, 0f, 0, false);
                                }
                            }
                        }
                    }
                }

                // === [铁血骑士] ===
                player.GetDamage(DamageClass.Generic) += 0.25f; // 伤害提升
                player.GetCritChance(DamageClass.Generic) += 15; // 暴击提升

                // === [弱点侦察] ===
                player.detectCreature = true;
                player.dangerSense = true;

                // === [绝境战神] ===
                if (player.statLife < player.statLifeMax2 * 0.5f)
                {
                    player.endurance += 0.20f; // 20% 减伤
                    player.lifeRegen += 10;    // 极高回血
                    // 绝境下的额外吸血或爆发可以写在这里
                }
            }
        }
    }
}