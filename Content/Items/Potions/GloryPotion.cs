using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content;
using zhashi.Content.Items.Accessories; // 引用力量牌命名空间

namespace zhashi.Content.Items.Potions
{
    // 1. 改为继承 LotMItem 以使用智能配方功能
    public class GloryPotion : LotMItem
    {
        // 设定途径信息
        public override string Pathway => "Giant";
        public override int RequiredSequence => 3;

        public override void SetDefaults()
        {
            Item.width = 20; Item.height = 30; Item.useStyle = ItemUseStyleID.DrinkLiquid; Item.UseSound = SoundID.Item3;
            Item.useAnimation = 17; Item.useTime = 17; Item.useTurn = true; Item.maxStack = 99; Item.consumable = true;
            Item.rare = ItemRarityID.Red; Item.value = Item.buyPrice(platinum: 5);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // 保留你原有的月亮领主击杀检查提示
            if (Main.LocalPlayer.GetModPlayer<LotMPlayer>().baseSequence == 3)
            {
                string statusColor = NPC.downedMoonlord ? "00FF00" : "FF0000";
                tooltips.Add(new TooltipLine(Mod, "RitualDesc", "晋升仪式: 猎杀一位天使或同等强大的生物 (月亮领主)。"));
                tooltips.Add(new TooltipLine(Mod, "RitualProgress", $"[c/{statusColor}:目标状态: {(NPC.downedMoonlord ? "已陨落" : "存活")}]"));
            }
            // 基类的 ModifyTooltips 会自动添加序列要求提示，这里不需要额外写
            base.ModifyTooltips(tooltips);
        }

        public override bool CanUseItem(Player player)
        {
            // 先让基类检查是否满足序列3
            if (!base.CanUseItem(player)) return false;

            var modPlayer = player.GetModPlayer<LotMPlayer>();
            // 额外检查是否击杀了月亮领主
            if (modPlayer.baseSequence == 3 && !NPC.downedMoonlord)
            {
                if (player.whoAmI == Main.myPlayer) Main.NewText("你的战绩不足以承载神性的光辉...", 255, 50, 50);
                return false;
            }
            return true;
        }

        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();
            if (modPlayer.baseSequence == 3)
            {
                modPlayer.baseSequence = 2;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position);
                Main.NewText("黄昏的余晖在你身上凝固，你即是荣耀！", 255, 100, 0);
                Main.NewText("晋升成功：序列2 荣耀者 (天使)！", 255, 100, 0);
                Main.NewText("获得权柄：【黄昏重生】(抵挡一次死亡) | 【黄昏之笼】", 255, 255, 255);
                return true;
            }
            else if (modPlayer.baseSequence > 3) { Main.NewText("你还未成为银骑士。", 200, 50, 50); return true; }
            else { Main.NewText("你已是荣耀者。", 200, 200, 200); return true; }
        }

        // ==========================================
        // 配方升级：支持力量牌免石板
        // ==========================================
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<StrengthCard>(), // 力量牌 ID

                (ItemID.BottledWater, 1),
                (ItemID.LunarBar, 10),      // 夜明锭
                (ItemID.FragmentSolar, 10), // 日耀碎片
                (ItemID.FragmentNebula, 5)  // 星云碎片
            );
        }
    }
}