using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using zhashi.Content.Items.Accessories; // 引用愚者牌

namespace zhashi.Content.Items.Potions.Fool
{
    public class ScholarOfYorePotion : LotMItem
    {
        // 设定途径和前置序列 (需要序列4 诡法师)
        public override string Pathway => "Fool";
        public override int RequiredSequence => 4;

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
            Item.rare = ItemRarityID.Cyan; // 圣者级 (青色/彩虹)
            Item.value = Item.sellPrice(platinum: 5);
        }

        // 【核心检查】
        public override bool CanUseItem(Player player)
        {
            // 1. 先检查序列要求
            if (!base.CanUseItem(player)) return false;

            // 2. 仪式：脱离现实 (处于太空层 Space)
            if (!player.ZoneSkyHeight)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    Main.NewText("仪式未满足：需在脱离现实的高度(太空层)服食...", 255, 50, 50);
                }
                return false;
            }
            return true;
        }

        // 【UI提示】
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            bool inSpace = Main.LocalPlayer.ZoneSkyHeight;
            string c = inSpace ? "00FF00" : "FF0000";
            string status = inSpace ? "满足" : "未满足";

            tooltips.Add(new TooltipLine(Mod, "Ritual", $"[c/{c}:仪式要求：脱离现实 (在太空层服食) ({status})]"));
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();
                modPlayer.baseFoolSequence = 3; // 晋升序列3

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, player.position);
                Main.NewText("你仿佛成为了历史的一部分，时间在你眼中不再是直线...", 0, 255, 255);
                Main.NewText("晋升成功！序列3：古代学者！(圣者)", 255, 215, 0);
            }
            return true;
        }

        public override void AddRecipes()
        {
            // 使用智能配方生成器
            CreateDualRecipe(
                ModContent.ItemType<FoolCard>(), // 愚者牌

                (ItemID.BottledWater, 1),
                (ItemID.FragmentSolar, 10),  // 福根之犬 (日耀碎片)
                (ItemID.FragmentNebula, 10), // 雾之魔狼 (星云碎片)
                (ItemID.FrostCore, 3),       // 白霜结晶 (寒霜核)
                (ItemID.Ectoplasm, 10),      // 历史记录 (灵气)
                (ItemID.Book, 5)             // 历史书 (书)
            );
        }
    }
}