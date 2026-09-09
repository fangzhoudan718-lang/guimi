using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用红祭司牌

namespace zhashi.Content.Items.Potions.Hunter
{
    public class HunterPotion : LotMItem
    {
        // 1. 定义途径
        public override string Pathway => "Hunter";

        // 序列要求：0 代表无序列门槛 (凡人可服)
        public override int RequiredSequence => 0;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("猎人魔药");
        }

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
            Item.rare = ItemRarityID.Blue; // 序列9 蓝色
            Item.value = Item.sellPrice(silver: 50);
            Item.buffType = BuffID.WellFed;
            Item.buffTime = 300;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

                // 【核心逻辑】检查是否已是非凡者 (防止多重途径)
                if (modPlayer.IsBeyonder)
                {
                    Main.NewText("你的灵性已定型，无法开启第二条途径，强行服用只会导致失控！", 255, 50, 50);
                    return true; // 消耗掉作为惩罚
                }

                // 晋升逻辑
                modPlayer.baseHunterSequence = 9;

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, player.position);
                Main.NewText("你的感官瞬间变得敏锐，仿佛能嗅到空气中猎物的气息...", 200, 100, 50); // 猎人风格的土黄色/橙色
                Main.NewText("晋升成功！序列9：猎人！", 255, 100, 0);
            }
            return true;
        }

        public override void AddRecipes()
        {
            // 使用双配方：支持 石板 或 红祭司牌
            CreateDualRecipe(
                ModContent.ItemType<RedPriestCard>(), // 核心：红祭司牌
                (ItemID.BottledWater, 1),
                (ItemID.Ale, 1),       // 麦芽酒 (酒精/猎人)
                (ItemID.Daybloom, 1),  // 太阳花
                (ItemID.Gel, 5)        // 凝胶 (粘性/陷阱材料)
            );
        }
    }
}