using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用红祭司牌

namespace zhashi.Content.Items.Potions.Hunter
{
    public class ProvokerPotion : LotMItem
    {
        // 1. 定义途径和前置序列
        public override string Pathway => "Hunter";
        public override int RequiredSequence => 9; // 需要序列9 (猎人)

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.UseSound = SoundID.Item3;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.maxStack = 99;
            Item.consumable = true;
            Item.rare = ItemRarityID.Blue; // 序列8 蓝色
            Item.value = Item.buyPrice(gold: 1);
        }

        // 2. 晋升逻辑
        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var modPlayer = player.GetModPlayer<LotMPlayer>();

                // 防止高序列误服 (序列8及以上)
                if (modPlayer.baseHunterSequence <= 8)
                {
                    Main.NewText("你已经是更高序列的强者了，无需再次服用。", 200, 200, 200);
                    return false;
                }

                // 晋升逻辑：LotMItem 基类已确保玩家至少是序列9
                modPlayer.baseHunterSequence = 8;

                // 音效与文本
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position);

                Main.NewText("你感觉喉咙里像吞了一块火炭，想要大声嘲笑这个世界...", 255, 150, 50); // 挑衅者的橙色
                Main.NewText("晋升成功：序列8 挑衅者！", 255, 150, 50);
                Main.NewText("获得能力：【挑衅】(大幅吸引仇恨) | 【格斗体魄】", 255, 255, 255);
            }
            return true;
        }

        // 3. 双配方支持
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<RedPriestCard>(), // 核心：红祭司牌
                (ItemID.BottledWater, 1),
                (ItemID.Ale, 1),        // 麦芽酒 (酒精/挑衅)
                (ItemID.Vine, 2),       // 藤蔓 (纠缠)
                (ItemID.Obsidian, 5),   // 黑曜石 (坚固/火)
                (ItemID.Waterleaf, 2)   // 水叶草
            );
        }
    }
}