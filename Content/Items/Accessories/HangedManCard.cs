using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content; // 引用 LotMPlayer

namespace zhashi.Content.Items.Accessories
{
    public class HangedManCard : BlasphemyCardBase
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
            mp.isHangedManCardEquipped = true;

            // 2. 基础效果：牧羊人 / 堕落者

            // [血肉再生]：极强的生命恢复能力
            player.lifeRegen += 6;

            // [受难与报复]：荆棘效果 (反弹伤害)
            player.thorns += 1.0f;             // 受到伤害时全额反弹给敌人

            // [阴影皮肤]：伤害减免
            player.endurance += 0.12f;         // 受到的伤害减少 12%

            // [堕落之力]：提升全伤害
            player.GetDamage(DamageClass.Generic) += 0.15f;

            // 3. 预留接口：倒吊人途径专属加成
            // 未来可实现：吞噬灵魂、血肉炸弹、通过影子攻击等
            /*
            if (mp.baseHangedManSequence <= 4)
            {
                // TODO: 真正的“真实造物主”之力
            }
            */
        }
    }
}