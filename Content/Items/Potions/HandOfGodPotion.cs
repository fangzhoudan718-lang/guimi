using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Materials;
using zhashi.Content.Items.Accessories; // 【新增】引用力量牌

namespace zhashi.Content.Items.Potions
{
    // 1. 改为继承 LotMItem
    public class HandOfGodPotion : LotMItem
    {
        // 2. 设定途径属性 (巨人途径，需要序列2才能晋升)
        public override string Pathway => "Giant";
        public override int RequiredSequence => 2;

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 9999;
            Item.useTime = 45;
            Item.useAnimation = 45;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.consumable = true;
            Item.rare = ItemRarityID.Red;
            Item.UseSound = SoundID.Item3;
            Item.value = Item.sellPrice(gold: 50);
        }

        public override bool CanUseItem(Player player)
        {
            // 先调用基类检查
            if (!base.CanUseItem(player)) return false;

            // 严格检查：必须是序列2 荣耀战神 才能晋升序列1
            return player.GetModPlayer<LotMPlayer>().baseSequence == 2;
        }

        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 确保是喝下去的瞬间触发
            if (player.itemAnimation > 0 && player.itemTime == 0)
            {
                // 1. 晋升逻辑
                modPlayer.baseSequence = 1;

                // 2. 给予序列1 武器
                player.QuickSpawnItem(player.GetSource_ItemUse(Item), ModContent.ItemType<Weapons.HandOfGod>());

                // 3. 文本与特效
                Main.NewText("你已晋升为序列1：神明之手！", 255, 100, 0);
                Main.NewText("你获得了神罚的权柄（武器已放入背包）。", 200, 200, 200);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position);

                return true;
            }
            return false;
        }

        // ==========================================
        // 配方升级：支持力量牌免石板
        // ==========================================
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<StrengthCard>(), // 力量牌

                // 材料列表
                (ModContent.ItemType<HandOfGodCharacteristic>(), 1), // 非凡特性
                (ItemID.BottledWater, 9),
                (ItemID.LifeFruit, 5),
                (ItemID.Ectoplasm, 20)
            );
        }
    }
}