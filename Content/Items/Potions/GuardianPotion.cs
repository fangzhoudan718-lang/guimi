using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content;
using zhashi.Content.Items.Accessories; // 引用力量牌

namespace zhashi.Content.Items.Potions
{
    public class GuardianPotion : LotMItem
    {
        // 设定途径和前置序列 (序列6 黎明骑士)
        public override string Pathway => "Giant";
        public override int RequiredSequence => 6;

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
            Item.rare = ItemRarityID.Lime;
            Item.value = Item.buyPrice(gold: 15);
        }

        // 1. 动态修改物品说明：实时显示仪式进度
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // 先调用基类逻辑 (显示"需要序列6"的红绿字)
            base.ModifyTooltips(tooltips);

            var modPlayer = Main.LocalPlayer.GetModPlayer<LotMPlayer>();

            // 只有当玩家是序列6时，才额外显示守护者仪式进度
            if (modPlayer.baseSequence == 6)
            {
                string statusColor = (modPlayer.guardianRitualProgress >= LotMPlayer.GUARDIAN_RITUAL_TARGET) ? "00FF00" : "FF0000";
                string progressText = $"[c/{statusColor}:仪式进度: {modPlayer.guardianRitualProgress} / {LotMPlayer.GUARDIAN_RITUAL_TARGET}]";

                tooltips.Add(new TooltipLine(Mod, "RitualDesc", "晋升仪式: 需在城镇NPC附近(50格内)承受伤害以履行守护之责。"));
                tooltips.Add(new TooltipLine(Mod, "RitualProgress", progressText));
            }
        }

        // 2. 核心判断：能否使用物品？
        public override bool CanUseItem(Player player)
        {
            // 先调用基类检查是否满足序列6
            if (!base.CanUseItem(player)) return false;

            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 额外检查仪式进度
            if (modPlayer.baseSequence == 6)
            {
                if (modPlayer.guardianRitualProgress < LotMPlayer.GUARDIAN_RITUAL_TARGET)
                {
                    if (player.whoAmI == Main.myPlayer)
                    {
                        Main.NewText($"你还未理解守护的重量... (进度: {modPlayer.guardianRitualProgress}/{LotMPlayer.GUARDIAN_RITUAL_TARGET})", 255, 50, 50);
                        Main.NewText("提示：请在任何城镇NPC附近承受伤害来增加进度。", 200, 200, 200);
                    }
                    return false; // 禁止使用
                }
            }
            return true;
        }

        // 3. 物品使用后的效果 (晋升逻辑)
        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            if (modPlayer.baseSequence == 6)
            {
                modPlayer.baseSequence = 5;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position);

                Main.NewText("你的皮肤泛起金属的光泽，你发誓守护这一切...", 255, 215, 0);
                Main.NewText("晋升成功：序列5 守护者！", 255, 215, 0);
                Main.NewText("获得能力：【守护姿态】 (按住 X 键开启)", 255, 255, 255);
                Main.NewText("被动能力：免疫幻觉，攻击无视大量防御，并在受到攻击时保护队友。", 200, 200, 200);
                return true;
            }

            return true;
        }

        // ==========================================
        // 配方升级：支持力量牌免石板
        // ==========================================
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<StrengthCard>(), // 力量牌

                (ItemID.BottledWater, 1),
                (ItemID.ChlorophyteBar, 5),  // 叶绿锭
                (ItemID.LifeFruit, 3),       // 生命果
                (ItemID.IronskinPotion, 5)   // 铁皮药水
            );
        }
    }
}