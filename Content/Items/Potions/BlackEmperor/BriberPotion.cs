using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories;
using zhashi.Content.Pathways.BlackEmperor;

namespace zhashi.Content.Items.Potions.BlackEmperor
{
    /// <summary>序列七 · 贿赂者。核心是「贿赂」：花真钱买通目标。</summary>
    public class BriberPotion : LotMItem
    {
        public override string Pathway => "BlackEmperor";
        public override int RequiredSequence => 8;
        // 同序列统一瓶型：引用愚者途径序列7的瓶型资源。
        public override string Texture => "zhashi/Content/Items/Potions/Fool/MagicianPotion";

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
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.buyPrice(gold: 3);
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer) return true;
            LotMPlayer lotm = player.GetModPlayer<LotMPlayer>();
            if (lotm.baseBlackEmperorSequence != 8) return false;

            lotm.baseBlackEmperorSequence = 7;
            lotm.currentBlackEmperorSequence = 7;
            if (Main.netMode == NetmodeID.MultiplayerClient) lotm.SyncPlayer(-1, -1, false);
            PromotionPulse.Raise(player, PromotionPulse.BlackEmperor);

            SoundEngine.PlaySound(SoundID.Item3, player.Center);
            Main.NewText("冒着气泡的黑液意外地清爽好喝，唇齿间留下钱币的味道——晋升成功：序列7 贿赂者！", 200, 160, 90);
            Main.NewText("能力：贿赂 [%bribe%]。Shift + 同键切换模式：削弱 / 魅惑 / 狂妄 / 关联。", 200, 170, 110);
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);
            tooltips.Add(new TooltipLine(Mod, "BlackEmperorIntro",
                "[c/B08850:黑皇帝途径 · 序列7 贿赂者]\n" +
                "核心能力「贿赂」分四种：削弱、魅惑、狂妄、关联。\n" +
                "花出去的**钱币面额决定强度**；对城镇 NPC 行贿则换取一段时间的庇护。"));
        }

        public override void AddRecipes()
        {
            // 原著主材料「哭泣婴儿花 / 怪脸大麻结晶」用游戏内等价物替代；
            // 贿赂主题直接体现在配方里的金币上。
            CreateDualRecipe(
                ModContent.ItemType<BlackEmperorCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.GoldCoin, 5),
                (ItemID.Topaz, 3),
                (ItemID.Ale, 5),
                (ItemID.Deathweed, 5));
        }
    }
}
