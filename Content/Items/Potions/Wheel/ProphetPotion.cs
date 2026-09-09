using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using zhashi.Content.Items.Accessories;

namespace zhashi.Content.Items.Potions.Wheel
{
    /// <summary>
    /// 命运途径 序列2：先知 (Prophet)
    /// 前置序列：3 怪人
    /// 晋升仪式：在序列3状态下，"不可定数"成功触发5次
    /// </summary>
    public class ProphetPotion : LotMItem
    {
        public override string Pathway => "Wheel";
        public override int RequiredSequence => 3; // 需要序列3(怪人)

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
            Item.rare = ItemRarityID.Cyan; // 序列2 半神 (青色,与其它途径对齐)
            Item.value = Item.sellPrice(gold: 50);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            var modPlayer = Main.LocalPlayer.GetModPlayer<LotMPlayer>();

            if (modPlayer.baseWheelSequence == 3)
            {
                string color = modPlayer.prophetRitualComplete ? "00FF00" : "FF0000";
                string statusText = modPlayer.prophetRitualComplete ? "已完成" : "未完成";

                tooltips.Add(new TooltipLine(Mod, "RitualDesc",
                    "晋升仪式：在序列3状态下，让\"不可定数\"成功触发5次。"));
                tooltips.Add(new TooltipLine(Mod, "RitualProgress",
                    $"[c/{color}:仪式进度: {modPlayer.prophetRitualProgress} / {LotMPlayer.PROPHET_RITUAL_TARGET} ({statusText})]"));
            }
        }

        public override bool CanUseItem(Player player)
        {
            if (!base.CanUseItem(player)) return false;

            var modPlayer = player.GetModPlayer<LotMPlayer>();

            if (!modPlayer.prophetRitualComplete)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    int left = LotMPlayer.PROPHET_RITUAL_TARGET - modPlayer.prophetRitualProgress;
                    Main.NewText($"仪式未完成：还需 {left} 次\"命运拒绝死亡\"...", 255, 50, 50);
                }
                return false;
            }
            return true;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer) return true;

            var modPlayer = player.GetModPlayer<LotMPlayer>();
            modPlayer.baseWheelSequence = 2;
            modPlayer.currentWheelSequence = 2;

            SoundEngine.PlaySound(SoundID.Roar, player.position);
            Main.NewText("命运的长河在你眼前舒展开来，你看到了所有可能的未来...", 200, 150, 255);
            Main.NewText("晋升成功：序列2 先知！", 50, 255, 50);
            Main.NewText("能力：【福祸之言】| 【命运启示】| 【预言术】| 【水银之躯】", 255, 255, 255);

            return true;
        }

        public override void AddRecipes()
        {
            // 序列2用料: 月光锭/暗影鳞片/无尽材料等
            CreateDualRecipe(
                ModContent.ItemType<WheelOfFortuneCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.LunarBar, 10),          // 月光锭(月亮领主后)
                (ItemID.FragmentNebula, 10),    // 星云碎片(预言/星象)
                (ItemID.FragmentVortex, 10),    // 漩涡碎片(命运的旋涡)
                (ItemID.Ectoplasm, 15),         // 灵气
                (ItemID.LifeFruit, 3),          // 生命果
                (ItemID.SoulofLight, 10),       // 光明之魂(预知光明)
                (ItemID.SoulofNight, 10)        // 暗影之魂(预知黑暗)
            );
        }
    }
}
