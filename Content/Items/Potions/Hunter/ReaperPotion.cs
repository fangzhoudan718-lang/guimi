using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用红祭司牌

namespace zhashi.Content.Items.Potions.Hunter
{
    public class ReaperPotion : LotMItem
    {
        // 1. 定义途径和前置序列
        public override string Pathway => "Hunter";
        public override int RequiredSequence => 6; // 需要序列6 (阴谋家)

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
            Item.rare = ItemRarityID.Pink; // 序列5 粉色 (机械三王后)
            Item.value = Item.buyPrice(gold: 15);
        }

        // 2. 晋升逻辑
        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // LotMItem 基类已确保玩家是序列6
            modPlayer.baseHunterSequence = 5;

            // 音效与文本
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position);

            Main.NewText("你看到了万物的死线...", 255, 50, 50); // 血红色
            Main.NewText("晋升成功：序列5 收割者！", 255, 50, 50);
            Main.NewText("能力：【弱点攻击】(暴击伤害提升) | 【致命攻击】(斩杀低血量) | 【屠杀】(按J键)", 255, 255, 255);

            return true;
        }

        // 3. 双配方支持
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<RedPriestCard>(), // 核心：红祭司牌
                (ItemID.BottledWater, 1),
                (ItemID.HallowedBar, 10),   // 神圣锭 (圣者/神性)
                (ItemID.SoulofFright, 5),   // 恐惧之魂 (杀戮/恐惧)
                (ItemID.SoulofNight, 10),   // 暗影之魂
                (ItemID.SharkFin, 5)        // 鲨鱼鳍 (猎杀者/血腥)
            );
        }
    }
}