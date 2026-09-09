using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using zhashi.Content.Items.Accessories; // 引用愚者牌

namespace zhashi.Content.Items.Potions.Fool
{
    public class MiracleInvokerPotion : LotMItem
    {
        // 设定途径和前置序列 (需要序列3 古代学者)
        public override string Pathway => "Fool";
        public override int RequiredSequence => 3;

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
            Item.rare = ItemRarityID.Red; // 天使级 (红色)
            Item.value = Item.sellPrice(platinum: 20);
        }

        // 【核心检查】
        public override bool CanUseItem(Player player)
        {
            // 1. 先检查序列要求
            if (!base.CanUseItem(player)) return false;

            // 2. 仪式：在具有历史气息的地方 (地牢)
            if (!player.ZoneDungeon)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    Main.NewText("仪式未满足：需在历史迷雾中(地牢)服食...", 255, 50, 50);
                }
                return false;
            }
            return true;
        }

        // 【UI提示】
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            bool inDungeon = Main.LocalPlayer.ZoneDungeon;
            string c = inDungeon ? "00FF00" : "FF0000";
            string status = inDungeon ? "满足" : "未满足";

            tooltips.Add(new TooltipLine(Mod, "Ritual", $"[c/{c}:仪式要求：重现历史 (在地牢服食) ({status})]"));
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();
                modPlayer.baseFoolSequence = 2; // 晋升序列2

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, player.position);
                Main.NewText("你向命运许愿，命运回应了你...", 255, 255, 0);
                Main.NewText("晋升成功！序列2：奇迹师！(天使)", 255, 215, 0);
            }
            return true;
        }

        public override void AddRecipes()
        {
            // 使用智能配方生成器
            CreateDualRecipe(
                ModContent.ItemType<FoolCard>(), // 愚者牌

                (ItemID.BottledWater, 1),
                (ItemID.LunarOre, 20),      // 乌黯魔狼 (夜明矿)
                (ItemID.Ectoplasm, 20),     // 奇迹特性 (灵气)
                (ItemID.GoldWatch, 1),      // 时之虫 (金表)
                (ItemID.FallenStar, 10)     // 星之虫 (坠落之星)
            );
        }
    }
}