using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content;
using zhashi.Content.Items.Accessories; // 引用力量牌

namespace zhashi.Content.Items.Potions
{
    public class SilverKnightPotion : LotMItem
    {
        // 设定途径和前置序列 (序列4 猎魔者)
        public override string Pathway => "Giant";
        public override int RequiredSequence => 4;

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
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.buyPrice(platinum: 1);
        }

        // 1. 动态提示：显示霜月和南瓜月的击杀进度
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // 先显示基础序列要求
            base.ModifyTooltips(tooltips);

            var modPlayer = Main.LocalPlayer.GetModPlayer<LotMPlayer>();

            // 只有序列4才显示详细仪式进度
            if (modPlayer.baseSequence == 4)
            {
                bool frostMoonDone = NPC.downedChristmasTree && NPC.downedChristmasSantank && NPC.downedChristmasIceQueen;
                bool pumpkinMoonDone = NPC.downedHalloweenTree && NPC.downedHalloweenKing;

                string frostColor = frostMoonDone ? "00FF00" : "FF0000";
                string pumpkinColor = pumpkinMoonDone ? "00FF00" : "FF0000";

                tooltips.Add(new TooltipLine(Mod, "RitualDesc", "晋升仪式: 通关霜月与南瓜月事件，证明你的杀戮能力。"));
                tooltips.Add(new TooltipLine(Mod, "RitualStatus1", $"[c/{frostColor}:霜月 (常青树/坦克/冰女): {(frostMoonDone ? "已击杀" : "未完成")}]"));
                tooltips.Add(new TooltipLine(Mod, "RitualStatus2", $"[c/{pumpkinColor}:南瓜月 (哀木/南瓜王): {(pumpkinMoonDone ? "已击杀" : "未完成")}]"));
            }
        }

        // 2. 核心检查：能否使用
        public override bool CanUseItem(Player player)
        {
            // 基础检查
            if (!base.CanUseItem(player)) return false;

            var modPlayer = player.GetModPlayer<LotMPlayer>();
            if (modPlayer.baseSequence == 4)
            {
                bool frostMoonDone = NPC.downedChristmasTree && NPC.downedChristmasSantank && NPC.downedChristmasIceQueen;
                bool pumpkinMoonDone = NPC.downedHalloweenTree && NPC.downedHalloweenKing;

                if (!frostMoonDone || !pumpkinMoonDone)
                {
                    if (player.whoAmI == Main.myPlayer)
                        Main.NewText("祭品不足，神灵未投下注视... 需彻底征服霜月与南瓜月。", 255, 50, 50);
                    return false;
                }
            }
            return true;
        }

        // 3. 晋升效果
        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            if (modPlayer.baseSequence == 4)
            {
                modPlayer.baseSequence = 3;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position);

                Main.NewText("这是一场献给神明的血腥祭祀！", 255, 20, 20);
                Main.NewText("晋升成功：序列3 银骑士！", 192, 192, 192);
                Main.NewText("能力：【水银化】(按C键) | 【借光隐藏】 | 【空间斩杀】", 255, 255, 255);
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
                (ItemID.EverscreamTrophy, 1),   // 常青树纪念章
                (ItemID.SantaNK1Trophy, 1),      // 坦克纪念章
                (ItemID.IceQueenTrophy, 1),      // 冰女纪念章
                (ItemID.MourningWoodTrophy, 1),  // 哀木纪念章
                (ItemID.PumpkingTrophy, 1),      // 南瓜王纪念章
                (ItemID.SpookyWood, 50)          // 阴森木
            );
        }
    }
}