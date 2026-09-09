using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用力量牌

namespace zhashi.Content.Items.Potions
{
    public class WarriorPotion : LotMItem
    {
        // 设定途径和前置序列 (序列9 战士，基础要求是序列10普通人)
        public override string Pathway => "Giant";
        public override int RequiredSequence => 10;

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 30;
            Item.consumable = true;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 50);
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var modPlayer = player.GetModPlayer<LotMPlayer>();

                // 检查是否已经是非凡者
                if (modPlayer.IsBeyonder)
                {
                    Main.NewText("你的灵性已定型，无法开启第二条途径，强行服用只会导致失控！", 255, 50, 50);
                    return true; // 消耗掉作为惩罚
                }

                // 晋升逻辑
                modPlayer.baseSequence = 9;

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, player.position);
                Main.NewText("你服用了战士魔药，感觉身体充满了力量！", 255, 100, 100);
                Main.NewText("晋升成功！序列9：战士！", 255, 100, 0);
            }
            return true;
        }

        public override void AddRecipes()
        {
            // 使用智能配方生成器
            // 支持力量牌免石板
            CreateDualRecipe(
                ModContent.ItemType<StrengthCard>(), // 力量牌

                (ItemID.BottledWater, 1),
                (ItemID.Daybloom, 1),
                (ItemID.IronBar, 3) // 基础配方指定铁锭
            );

            CreateDualRecipe(
                ModContent.ItemType<StrengthCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.Daybloom, 1),
                (ItemID.LeadBar, 3)
            );
        }
    }
}