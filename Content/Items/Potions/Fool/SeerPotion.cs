using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content; // 引用核心 Player 类
using zhashi.Content.Items.Accessories; // 引用愚者牌

namespace zhashi.Content.Items.Potions.Fool
{
    public class SeerPotion : LotMItem
    {
        // 设定途径为 "Fool" (愚者)
        // RequiredSequence = 10 代表需要是普通人才能服用
        public override string Pathway => "Fool";
        public override int RequiredSequence => 10;

        public override void SetStaticDefaults()
        {
            // 建议在 hjson 中设置 DisplayName 和 Tooltip
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
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 50);
            Item.buffType = BuffID.WellFed;
            Item.buffTime = 300;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

                // 【核心逻辑】检查是否已是非凡者
                // 如果已经是巨人途径或其他途径的非凡者，禁止开启愚者途径
                if (modPlayer.IsBeyonder)
                {
                    Main.NewText("你的灵性已定型，无法开启第二条途径，强行服用只会导致失控！", 255, 50, 50);
                    return true; // 消耗掉作为惩罚
                }

                // 晋升逻辑：成为序列9 占卜家
                modPlayer.baseFoolSequence = 9;

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, player.position);
                Main.NewText("你感觉到灵感在脑海中迸发，眼前的世界多了许多虚幻的丝线...", 100, 100, 255);
                Main.NewText("晋升成功！序列9：占卜家！", 50, 255, 50);
            }
            return true;
        }

        public override void AddRecipes()
        {
            // 使用智能配方生成器
            // 只要持有【愚者牌】，就不需要消耗【亵渎石板】
            CreateDualRecipe(
                ModContent.ItemType<FoolCard>(), // 愚者牌

                (ItemID.BottledWater, 1), // 纯水
                (ItemID.BlackInk, 1),     // 拉瓦章鱼血液 (黑墨水)
                (ItemID.FallenStar, 1),   // 星水晶 (坠落之星)
                (ItemID.Daybloom, 1),     // 夜香草 (太阳花)
                (ItemID.Blinkroot, 1),    // 金薄荷 (闪耀根)
                (ItemID.Deathweed, 1)     // 龙血草 (死亡草)
            );
        }
    }
}