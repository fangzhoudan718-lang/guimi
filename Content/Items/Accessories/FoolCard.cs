using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content; // 引用你的命名空间
using zhashi.Content.Items;

namespace zhashi.Content.Items.Accessories
{
    public class FoolCard : BlasphemyCardBase
    {
        public override void SafeSetDefaults()
        {
            Item.width = 28;
            Item.height = 34;
            Item.accessory = true; // 它是饰品
            Item.rare = ItemRarityID.Red; // 红色稀有度
            Item.value = Item.sellPrice(platinum: 1); // 非常值钱
            Item.maxStack = 1; // 只能堆叠1个
        }

        // 当饰品装备时触发
        public override void SafeUpdateAccessory(Player player, LotMPlayer mp, bool hideVisual)
        {
            // === 这里只写愚者牌独有的效果 ===

            // 标记装备了愚者牌 (用于 LotMPlayer 里的火焰跳跃判断)
            mp.isFoolCardEquipped = true;

            // 增加属性 (例如)
            player.statManaMax2 += 100;

            // 只有愚者途径玩家才能享受的高级加成
            if (mp.currentFoolSequence <= 9)
            {
                // 比如增加灵性回复速度
                mp.spiritualityRegenTimer += 2;
            }
        }
    }
}