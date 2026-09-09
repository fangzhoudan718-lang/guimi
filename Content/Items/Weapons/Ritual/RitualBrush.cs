using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Projectiles.Ritual;
using zhashi.Content.DamageClasses;
using Microsoft.Xna.Framework;

namespace zhashi.Content.Items.Weapons.Ritual
{
    public class RitualBrush : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 3;
            Item.DamageType = ModContent.GetInstance<RitualDamage>();
            Item.width = 32;
            Item.height = 32;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.shoot = ProjectileID.None;
            Item.autoReuse = true;
            Item.value = Item.sellPrice(0, 0, 80, 0);
            Item.rare = ItemRarityID.Blue;
        }

        public override void HoldItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                int type = ModContent.ProjectileType<RitualBrushPro_Ink>();
                if (player.ownedProjectileCounts[type] < 1)
                {
                    Projectile.NewProjectile(
                        player.GetSource_ItemUse(Item),
                        Main.MouseWorld,
                        Vector2.Zero,
                        type,
                        Item.damage,
                        Item.knockBack,
                        player.whoAmI
                    );
                }
            }
        }

        public override void AddRecipes()
        {
            // 配方：仪式刻录刀 + 原版黑墨水
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<RitualKnife>())
                .AddIngredient(ItemID.BlackInk) // 原版物品
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}