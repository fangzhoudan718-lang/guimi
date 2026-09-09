using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用太阳牌

namespace zhashi.Content.Items.Potions.Sun
{
    public class LightSupplicantPotion : LotMItem
    {
        // 1. 定义途径和前置序列
        public override string Pathway => "Sun";
        public override int RequiredSequence => 9; // 需要序列9 (歌颂者)

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.useTurn = true;
            Item.maxStack = 99;
            Item.consumable = true;
            Item.rare = ItemRarityID.Green; // 序列8 绿色
            Item.value = Item.buyPrice(gold: 1);
            Item.buffType = BuffID.WellFed;
            Item.buffTime = 3600;
        }

        // 2. 动态提示
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            var modPlayer = Main.LocalPlayer.GetModPlayer<LotMPlayer>();

            // 检查序列 (LotMItem 基类虽然有做，但这里显式显示更好看)
            bool seqReady = modPlayer.baseSunSequence == 9;
            string seqColor = seqReady ? "00FF00" : "FF0000";
            string statusText = seqReady ? "已满足" : "未满足";

            // 如果当前不是序列9，提示会更显眼
            if (modPlayer.baseSunSequence != 9)
            {
                tooltips.Add(new TooltipLine(Mod, "SeqReqCustom", $"[c/{seqColor}:条件: 需序列9 歌颂者 ({statusText})]"));
            }
        }

        // 3. 晋升逻辑
        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var p = player.GetModPlayer<LotMPlayer>();

                // 基类 LotMItem 已经处理了 CanUseItem 的检查
                // 这里直接执行晋升逻辑
                p.baseSunSequence = 8;
                p.currentSunSequence = 8; // 同步

                Main.NewText("你感觉到炽热的光辉在体内流淌，你成为了祈光人！", 255, 215, 0); // 金色

                // 音效
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item3, player.position);

                // 晋升特效
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDust(player.position, player.width, player.height, DustID.GoldFlame, 0, 0, 100, default, 1.5f);
                }
            }
            return true;
        }

        // 4. 双配方支持
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<SunCard>(), // 核心：太阳牌
                (ItemID.BottledWater, 1),
                (ItemID.FallenStar, 3),    // 坠落之星 (光)
                (ItemID.Fireblossom, 1),   // 火焰花 (热)
                (ItemID.MagmaStone, 1),    // 岩浆石 (能量)
                (ItemID.Ale, 1),           // 麦酒
                (ItemID.Sunflower, 1),     // 向日葵
                (ItemID.Waterleaf, 1),     // 水叶草
                (ItemID.GoldBar, 1)        // 金锭
            );
        }
    }
}