using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using zhashi.Content.Items.Accessories;

namespace zhashi.Content.Items.Potions.Wheel
{
    public class MisfortuneMagePotion : LotMItem
    {
        public override string Pathway => "Wheel";
        public override int RequiredSequence => 5; // 前置：赢家

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 30;
            Item.consumable = true;
            Item.rare = ItemRarityID.Cyan; // 半神级
            Item.value = Item.buyPrice(platinum: 1);
        }

        public override bool CanUseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();
            if (!modPlayer.misfortuneMageRitualComplete)
            {
                if (player.whoAmI == Main.myPlayer)
                    Main.NewText("仪式未完成：你需要佩戴【灰色四叶草】击败机械骷髅王！", 255, 50, 50);
                return false;
            }
            return base.CanUseItem(player);
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var modPlayer = player.GetModPlayer<LotMPlayer>();
                modPlayer.baseWheelSequence = 4;
                modPlayer.currentWheelSequence = 4;

                SoundEngine.PlaySound(SoundID.Roar, player.position); // 半神的怒吼
                Main.NewText("你看到了命运的长河，你成为了厄运的主宰...", 147, 112, 219);
                Main.NewText("晋升成功：序列4 厄运法师！神话生物形态初显！", 255, 215, 0);
            }
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);
            var modPlayer = Main.LocalPlayer.GetModPlayer<LotMPlayer>();
            string status = modPlayer.misfortuneMageRitualComplete ? "[c/00FF00:已完成]" : "[c/FF0000:未完成]";
            tooltips.Add(new TooltipLine(Mod, "RitualReq", $"[c/FF5555:晋升仪式: 佩戴【厄运之骰】战胜机械骷髅王] {status}"));
        }

        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<WheelOfFortuneCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.HallowedBar, 10),     // 神圣锭
                (ItemID.SoulofFright, 5),     // 恐惧之魂 (机械骷髅王) - 代表厄运与恐惧
                (ItemID.SoulofSight, 5),      // 视域之魂 (双子魔眼) - 代表绝对灵感
                (ItemID.SoulofMight, 5),      // 力量之魂 (毁灭者) - 代表水银之躯的肉体质变
                (ItemID.Bone, 30)             // 骨头 (呼应骷髅王)
            );
        }
    }
}