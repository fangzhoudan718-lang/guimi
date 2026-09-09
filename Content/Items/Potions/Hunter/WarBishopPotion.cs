using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用红祭司牌

namespace zhashi.Content.Items.Potions.Hunter
{
    public class WarBishopPotion : LotMItem
    {
        // 1. 定义途径和前置序列
        public override string Pathway => "Hunter";
        public override int RequiredSequence => 4; // 需要序列4 (铁血骑士)

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
            Item.rare = ItemRarityID.Cyan; // 序列3 圣者 (青色)
            Item.value = Item.buyPrice(platinum: 2);
        }

        // 2. 显示仪式进度 (击杀拜月)
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            var modPlayer = Main.LocalPlayer.GetModPlayer<LotMPlayer>();

            // 只有序列4才显示仪式
            if (modPlayer.baseHunterSequence == 4)
            {
                string statusColor = NPC.downedAncientCultist ? "00FF00" : "FF0000";

                tooltips.Add(new TooltipLine(Mod, "RitualDesc", "晋升仪式: 击败拜月教邪教徒，开启最终的战争。"));
                tooltips.Add(new TooltipLine(Mod, "RitualProgress", $"[c/{statusColor}:目标状态: {(NPC.downedAncientCultist ? "已击杀" : "存活")}]"));
            }
        }

        // 3. 使用条件检查
        public override bool CanUseItem(Player player)
        {
            // 基类检查是否是序列4
            if (!base.CanUseItem(player)) return false;

            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 检查拜月是否击杀
            if (modPlayer.baseHunterSequence == 4 && !NPC.downedAncientCultist)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    Main.NewText("战争的序幕还未拉开... (需击败拜月教邪教徒)", 255, 50, 50);
                }
                return false;
            }

            return true;
        }

        // 4. 晋升逻辑
        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 晋升
            modPlayer.baseHunterSequence = 3;

            // 音效与文本
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position);

            Main.NewText("你的意志连接了整个战场！", 255, 69, 0); // 战争红
            Main.NewText("晋升成功：序列3 战争主教 (圣者)！", 255, 69, 0);
            Main.NewText("能力：【心灵网络】(属性共享) | 【战争兵器】(哨兵强化) | 【L键集众强化】", 255, 255, 255);

            return true;
        }

        // 5. 双配方支持
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<RedPriestCard>(), // 核心：红祭司牌
                (ItemID.BottledWater, 1),
                (ItemID.SpectreBar, 10),      // 幽灵锭 (灵性/主教)
                (ItemID.ShroomiteBar, 10),    // 蘑菇矿 (科技/战争)
                (ItemID.LihzahrdPowerCell, 1) // 蜥蜴电池 (古代能源/兵器)
            );
        }
    }
}