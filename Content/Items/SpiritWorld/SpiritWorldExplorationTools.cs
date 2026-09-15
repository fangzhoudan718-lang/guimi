using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using zhashi.Content.Dimensions;

namespace zhashi.Content.Items.SpiritTools
{
    public class SpiritCompass : ModItem
    {
        public override string Texture => $"Terraria/Images/Item_{ItemID.Compass}";

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.maxStack = 1;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 35;
            Item.useAnimation = 35;
            Item.UseSound = SoundID.Item4;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = Item.sellPrice(gold: 4);
        }

        public override bool CanUseItem(Player player) => SubworldSystem.IsActive<SpiritWorld>();

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer) return true;
            SpiritWorldExplorerPlayer explorer = player.GetModPlayer<SpiritWorldExplorerPlayer>();
            if (explorer.TryFindNearestUnexplored(out string arrow, out int distance))
                Main.NewText(Language.GetTextValue("Mods.zhashi.Messages.SpiritTools.CompassHint", arrow, distance), new Color(145, 220, 205));
            else
                Main.NewText(Language.GetTextValue("Mods.zhashi.Messages.SpiritTools.CompassComplete"), new Color(205, 190, 145));
            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Compass)
                .AddIngredient(ItemID.FallenStar, 8)
                .AddIngredient(ItemID.Ectoplasm, 5)
                .AddTile(TileID.CrystalBall)
                .Register();
        }
    }

}
