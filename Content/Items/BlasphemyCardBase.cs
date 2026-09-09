using Terraria;
using Terraria.ModLoader;
using zhashi.Content; // 确保引用了 LotMPlayer 所在的命名空间

namespace zhashi.Content.Items
{
    // abstract 表示这是一个基类，不能直接做成物品，必须被继承
    public abstract class BlasphemyCardBase : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true; // 确认为饰品
            Item.rare = Terraria.ID.ItemRarityID.Red; // 红色稀有度
            Item.value = Item.sellPrice(0, 10, 0, 0);

            // 调用子类的独特设置
            SafeSetDefaults();
        }

        // 让子类去实现具体的贴图、大小等设置
        public virtual void SafeSetDefaults() { }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            var mp = player.GetModPlayer<LotMPlayer>();

            mp.isAntiDivinationActive = true;

            // 2. 通用效果：每秒扣除 1% 最大灵性
            if (mp.spiritualityCurrent > 0)
            {
                float drainAmount = mp.spiritualityMax * 0.03f / 60f;
                mp.spiritualityCurrent -= drainAmount;
            }
            mp.blasphemyCardEquippedCount++;

            SafeUpdateAccessory(player, mp, hideVisual);
        }

        // 子类在这个方法里写独有的效果
        public virtual void SafeUpdateAccessory(Player player, LotMPlayer mp, bool hideVisual) { }

        // 尝试防止玩家装备第二张 (UI层面限制)
        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            if (player.GetModPlayer<LotMPlayer>().blasphemyCardEquippedCount > 0)
            {
                return false;
            }
            return true;
        }
    }
}