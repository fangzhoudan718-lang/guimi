using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic; // 必须引用这个，用于ModifyTooltips的List
using zhashi.Content.Items.Accessories; // 引用魔女牌所在的命名空间

namespace zhashi.Content.Items.Potions.Demoness
{
    public class DespairDemonessPotion : LotMItem
    {
        public override string Pathway => "Demoness";
        public override int RequiredSequence => 5; // 前置序列5

        public override void SetDefaults()
        {
            Item.width = 20; Item.height = 30;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 17; Item.useTime = 17; Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 30; Item.consumable = true;
            Item.rare = ItemRarityID.Yellow; // 序列4 黄色/花后品质
            Item.value = Item.sellPrice(gold: 20);
        }

        // --- 核心逻辑：检查是否能服用 (序列 + 仪式) ---
        public override bool CanUseItem(Player player)
        {
            // 1. 基类检查序列等级
            if (!base.CanUseItem(player)) return false;

            LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

            // 2. 仪式检查：在日食中击杀 50 个敌人
            if (modPlayer.despairRitualCount < 50)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    Main.NewText($"仪式未完成：需在日食(灾难)中收割生命 ({modPlayer.despairRitualCount}/50)", 255, 50, 50);
                }
                return false;
            }

            return true;
        }

        // --- UI提示：在物品说明里显示仪式进度 ---
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            Player player = Main.LocalPlayer;
            LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

            int count = modPlayer.despairRitualCount;
            bool complete = count >= 50;

            if (count > 50) count = 50; // 显示上限锁定为50

            string color = complete ? "00FF00" : "FF0000"; // 完成变绿，未完成变红
            string status = complete ? " (已完成)" : "";

            tooltips.Add(new TooltipLine(Mod, "Ritual", $"[c/{color}:仪式要求：日食杀敌数 ({count}/50){status}]"));
        }

        // --- 晋升逻辑 ---
        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

                modPlayer.baseDemonessSequence = 4;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item119, player.Center);
                Main.NewText("绝望如瘟疫般蔓延，你成为了半神！", 255, 0, 255);
                Main.NewText("晋升成功！序列4：绝望魔女！", 255, 20, 147);
            }
            return true;
        }

        // --- 配方注册 (使用你的智能双重配方) ---
        public override void AddRecipes()
        {

            CreateDualRecipe(
                ModContent.ItemType<DemonessCard>(), // 核心道具：魔女牌

                (ItemID.BottledWater, 1),
                (ItemID.VialofVenom, 1),   // 毒囊
                (ItemID.Ectoplasm, 3),     // 结晶 (灵气)
                (ItemID.CrystalShard, 5),  // 碎片 (水晶碎块)
                (ItemID.JungleSpores, 5),  // 瘟疫材料 (丛林孢子)
                (ItemID.RottenChunk, 5)    // 血液 (腐肉)
            );
        }
    }
}