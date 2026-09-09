using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用恋人牌

namespace zhashi.Content.Items.Potions.Marauder
{
    public class PrometheusPotion : LotMItem
    {
        public override string Pathway => "Marauder";

        // 序列要求：需要序列7 (解密学者)
        public override int RequiredSequence => 7;

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
            Item.rare = ItemRarityID.Pink; // 序列6 粉色稀有度
            Item.value = Item.sellPrice(gold: 5);

            // 保留你设计的副作用：服用时获得“着火了！”Debuff，象征盗取火种的代价
            Item.buffType = BuffID.OnFire;
            Item.buffTime = 300; // 5秒燃烧
        }

        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 晋升逻辑：LotMItem 基类已确保玩家是序列7
            modPlayer.baseMarauderSequence = 6;

            // 视觉与文本反馈
            Main.NewText("你感觉到体内的灵性燃烧起来，双手仿佛能探入虚空窃取万物...", 255, 100, 0); // 橙红色文本
            Main.NewText("晋升成功：序列6 盗火人！", 255, 100, 0);
            Main.NewText("获得能力：【盗火】(攻击窃取物品) 与 【属性窃取】(削弱敌人强化自身)", 255, 140, 0);

            // 播放音效
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45, player.position); // Item45 是火焰相关的音效

            return true;
        }

        public override void AddRecipes()
        {
            // 使用双配方：支持 石板 或 恋人牌
            CreateDualRecipe(
                ModContent.ItemType<LoversCard>(), // 核心：恋人牌
                (ItemID.BottledWater, 1),
                (ItemID.CrystalShard, 1),    // 水晶线虫 -> 水晶碎块
                (ItemID.Ale, 1),             // 美酒 -> 麦酒
                (ItemID.Topaz, 10),          // 灵性材料 -> 黄玉
                (ItemID.LivingFireBlock, 1)  // 普罗米修斯之火 -> 活火块
            );
        }
    }
}