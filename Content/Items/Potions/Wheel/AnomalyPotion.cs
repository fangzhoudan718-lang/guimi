using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using zhashi.Content.Items.Accessories; // 引用命运之轮牌

namespace zhashi.Content.Items.Potions.Wheel
{
    /// <summary>
    /// 命运途径 序列3：怪人 (Anomaly)
    /// 前置序列：4 厄运法师
    /// 晋升仪式：在序列4状态下，3次于<10%血量时存活并恢复至60%+血量
    /// </summary>
    public class AnomalyPotion : LotMItem
    {
        public override string Pathway => "Wheel";
        public override int RequiredSequence => 4; // 需要序列4(厄运法师)

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
            Item.rare = ItemRarityID.Lime; // 序列3 圣者 (青柠色,与其它途径对齐)
            Item.value = Item.sellPrice(gold: 25);
        }

        // 显示仪式进度
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            var modPlayer = Main.LocalPlayer.GetModPlayer<LotMPlayer>();

            // 只在玩家处于序列4时显示仪式进度,避免干扰其他状态
            if (modPlayer.baseWheelSequence == 4)
            {
                string color = modPlayer.anomalyRitualComplete ? "00FF00" : "FF0000";
                string statusText = modPlayer.anomalyRitualComplete ? "已完成" : "未完成";

                tooltips.Add(new TooltipLine(Mod, "RitualDesc",
                    "晋升仪式：在序列4状态下，3次于<10%血量时存活并恢复至60%+生命。"));
                tooltips.Add(new TooltipLine(Mod, "RitualProgress",
                    $"[c/{color}:仪式进度: {modPlayer.anomalyRitualProgress} / {LotMPlayer.ANOMALY_RITUAL_TARGET} ({statusText})]"));
            }
        }

        // 服用条件检查
        public override bool CanUseItem(Player player)
        {
            // 父类检查:必须处于序列4
            if (!base.CanUseItem(player)) return false;

            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 仪式必须已完成
            if (!modPlayer.anomalyRitualComplete)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    int left = LotMPlayer.ANOMALY_RITUAL_TARGET - modPlayer.anomalyRitualProgress;
                    Main.NewText($"仪式未完成：还需 {left} 次绝境逆转，让命运为你倾斜...", 255, 50, 50);
                }
                return false;
            }
            return true;
        }

        // 晋升逻辑
        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer) return true;

            var modPlayer = player.GetModPlayer<LotMPlayer>();
            modPlayer.baseWheelSequence = 3;
            modPlayer.currentWheelSequence = 3;

            SoundEngine.PlaySound(SoundID.Roar, player.position);
            Main.NewText("你看清了命运齿轮的咬合，自己却成了不可被预测的那一环...", 147, 112, 219);
            Main.NewText("晋升成功：序列3 混乱行者！", 50, 255, 50);
            Main.NewText("能力：【怪人之眼】(暴击伤害+30%) | 【不可定数】(致命伤30%抹除) | 【M键命运骰子】", 255, 255, 255);

            return true;
        }

        public override void AddRecipes()
        {
            // 仿照其它途径序列3用料档次:神圣锭、灵气、Boss掉落 + 1张关键牌或亵渎石板
            CreateDualRecipe(
                ModContent.ItemType<WheelOfFortuneCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.HallowedBar, 15),       // 神圣锭(神圣等级)
                (ItemID.Ectoplasm, 10),         // 灵气(高阶超凡素材)
                (ItemID.SoulofFright, 5),       // 恐惧之魂(机械骷髅王 - 厄运)
                (ItemID.SoulofMight, 5),        // 力量之魂(肉体质变)
                (ItemID.SoulofSight, 5),        // 视域之魂(预见可能性)
                (ItemID.LifeFruit, 1)           // 生命果(肉体强化)
            );
        }
    }
}
