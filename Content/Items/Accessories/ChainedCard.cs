using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content; // 引用 LotMPlayer

namespace zhashi.Content.Items.Accessories
{
    public class ChainedCard : BlasphemyCardBase
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
            mp.isChainedCardEquipped = true;

            // 2. 基础效果：异种 / 怨魂

            // [异种钢皮]：像僵尸一样抗揍
            player.statDefense += 25;          // 极高的防御加成
            player.endurance += 0.15f;         // 伤害减免+15%
            player.noKnockback = true;         // 免疫击退

            // [狼人自愈]：强大的生命恢复
            player.lifeRegen += 4;
            player.shinyStone = true;          // 站立不动时极速回血 (神话饰品效果)

            // [挣脱束缚]：免疫限制类Debuff
            player.buffImmune[BuffID.Slow] = true;     // 免疫减速
            player.buffImmune[BuffID.Webbed] = true;   // 免疫蛛网
            player.buffImmune[BuffID.Stoned] = true;   // 免疫石化
            player.buffImmune[BuffID.Silenced] = true; // 免疫沉默

            // [怨魂嘶叫]：全伤害提升
            player.GetDamage(DamageClass.Generic) += 0.15f;

            // 3. 预留接口：被缚者途径专属加成
            // 未来可实现：怨魂附身、制造诅咒木偶、虚化穿墙
            /*
            if (mp.baseChainedSequence <= 4)
            {
                // TODO: 真正的“提线木偶”与“怨魂附体”
            }
            */
        }
    }
}