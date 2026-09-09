using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用红祭司牌

namespace zhashi.Content.Items.Potions.Hunter
{
    public class PyromaniacPotion : LotMItem
    {
        // 1. 定义途径和前置序列
        public override string Pathway => "Hunter";
        public override int RequiredSequence => 8; // 需要序列8 (挑衅者)

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
            Item.rare = ItemRarityID.Orange; // 序列7 橙色
            Item.value = Item.buyPrice(gold: 3);

            // 副作用：短暂燃烧，符合纵火家特征
            Item.buffType = BuffID.OnFire;
            Item.buffTime = 300;
        }

        // 2. 晋升逻辑
        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // LotMItem 基类已确保玩家是序列8
            modPlayer.baseHunterSequence = 7;

            // 播放火焰音效
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74, player.position);

            Main.NewText("你的血液仿佛沸腾了...", 255, 100, 0); // 烈火红
            Main.NewText("晋升成功：序列7 纵火家！", 255, 100, 0);
            Main.NewText("能力：【火焰亲和】(免火) | 【火球术】(按F键)", 255, 255, 255);

            return true;
        }

        // 3. 双配方支持
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<RedPriestCard>(), // 核心：红祭司牌
                (ItemID.BottledWater, 1),
                (ItemID.HellstoneBar, 5),      // 狱岩锭 (火的核心)
                (ItemID.Fireblossom, 3),       // 火焰花 (火的灵性)
                (ItemID.ExplosivePowder, 10)   // 爆炸粉 (破坏力)
            );
        }
    }
}