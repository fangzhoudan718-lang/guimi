using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用愚者牌

namespace zhashi.Content.Items.Potions.Fool
{
    public class ClownPotion : LotMItem
    {
        // 设定途径和前置序列 (需要序列9 占卜家)
        public override string Pathway => "Fool";
        public override int RequiredSequence => 9;

        public override void SetStaticDefaults()
        {
            // 名字和描述在HJSON中设置
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
            Item.rare = ItemRarityID.Green; // 序列8稍微稀有一点
            Item.value = Item.sellPrice(gold: 1);
            Item.buffType = BuffID.Tipsy; // 喝完有点晕（小丑的癫狂感）
            Item.buffTime = 600;
        }

        public override bool CanUseItem(Player player)
        {
            // 1. 调用基类以获取基础检查和Tooltip逻辑
            if (!base.CanUseItem(player)) return false;

            LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

            // 2. 严格检查：只有当前是序列9（占卜家）才能晋升序列8
            // 防止已经是小丑或更高序列的人误服
            return modPlayer.baseFoolSequence == 9;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();
                modPlayer.baseFoolSequence = 8; // 晋升为序列8

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, player.position);
                Main.NewText("你的嘴角不自觉地上扬，世界在你眼中变得滑稽而清晰...", 255, 165, 0);
                Main.NewText("晋升成功！序列8：小丑！", 50, 255, 50);
            }
            return true;
        }

        public override void AddRecipes()
        {
            // 使用智能配方生成器
            CreateDualRecipe(
                ModContent.ItemType<FoolCard>(), // 愚者牌

                (ItemID.BottledWater, 1),
                (ItemID.Stinger, 1),     // 毒刺
                (ItemID.JungleRose, 1),  // 丛林玫瑰
                (ItemID.Blinkroot, 1),   // 闪耀根
                (ItemID.Sunflower, 1),   // 向日葵
                (ItemID.Moonglow, 1),    // 月光草
                (ItemID.Deathweed, 1)    // 死亡草
            );
        }
    }
}