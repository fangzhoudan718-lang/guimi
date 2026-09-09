using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using zhashi.Content.UI;

namespace zhashi.Content.Items
{
    public class ArrodesItem : ModItem
    {
        public const int ARRODES_ID = -100;

        public override void SetStaticDefaults()
        {
            // 留空，使用 zh-Hans.hjson
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.HoldUp; // 举过头顶
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.value = Item.sellPrice(gold: 10);
            Item.rare = ItemRarityID.Pink;
            Item.useTurn = true; // 允许转身

            // 确保没有音效
            Item.UseSound = null;
        }

        public override bool AltFunctionUse(Player player)
        {
            return true;
        }

        public override bool CanUseItem(Player player)
        {
            // 只有右键才允许使用
            if (player.altFunctionUse == 2)
            {
                return true;
            }
            return false;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                if (player.altFunctionUse == 2)
                {
                    GalgameUISystem.OpenArrodesUI();
                }
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.MagicMirror)
                .AddIngredient(ItemID.Lens, 5)
                .AddIngredient(ItemID.GoldBar, 10)
                .AddTile(TileID.Anvils)
                .Register();

            CreateRecipe()
                .AddIngredient(ItemID.IceMirror)
                .AddIngredient(ItemID.Lens, 5)
                .AddIngredient(ItemID.PlatinumBar, 10)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}