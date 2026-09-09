using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用红祭司牌

namespace zhashi.Content.Items.Potions.Hunter
{
    public class ConspiratorPotion : LotMItem
    {
        // 1. 定义途径和前置序列
        public override string Pathway => "Hunter";
        public override int RequiredSequence => 7; // 需要序列7 (纵火家)

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
            Item.rare = ItemRarityID.LightRed; // 浅红色 (肉后初期)
            Item.value = Item.buyPrice(gold: 10);
        }

        // 2. 晋升逻辑
        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // LotMItem 基类已确保玩家是序列7
            modPlayer.baseHunterSequence = 6;

            // 音效与文本
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position);

            Main.NewText("你的思维变得冰冷而清晰...", 255, 100, 0); // 橙色
            Main.NewText("晋升成功：序列6 阴谋家！", 255, 100, 0);
            Main.NewText("能力：【火焰闪现】(按V键) | 【洞察】(暴击提升)", 255, 255, 255);

            return true;
        }

        // 3. 双配方支持
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<RedPriestCard>(), // 核心：红祭司牌
                (ItemID.BottledWater, 1),
                (ItemID.SpiderFang, 5),       // 蜘蛛牙 (编织阴谋)
                (ItemID.AntlionMandible, 5),  // 蚁狮上颚 (陷阱)
                (ItemID.Amber, 3),            // 琥珀 (保存/隐藏)
                (ItemID.Acorn, 5)             // 橡果 (森林/猎人)
            );
        }
    }
}