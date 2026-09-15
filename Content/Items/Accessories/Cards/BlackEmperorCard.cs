using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content; // 引用 LotMPlayer

namespace zhashi.Content.Items.Accessories
{
    public class BlackEmperorCard : BlasphemyCardBase
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
            // 黑皇帝之牌记录的是本途径的神性权柄。其他途径即使强行佩戴，
            // 也只能承受亵渎之牌的通用灵性负担，无法调用牌内能力。
            if (mp.currentBlackEmperorSequence > 9)
                return;

            // 核心标记：同时驱动外观、技能消耗折扣与「秩序威压」。
            mp.isBlackEmperorCardEquipped = true;

            // 2. 基础效果：律法主宰

            // [扭曲]：无视规则
            player.GetArmorPenetration(DamageClass.Generic) += 30; // 极高的破甲，象征利用规则漏洞

            // [威严]：秩序护盾
            player.statDefense += 20; // 高额防御加成

            // [阴影]：躲避
            // 给予 10% 的几率完全免疫伤害 (类似黑腰带)
            player.blackBelt = true;

            // [贿赂/扭曲]：全伤害提升
            player.GetDamage(DamageClass.Generic) += 0.15f;
        }
    }
}
