using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Projectiles.Ritual;
using zhashi.Content.DamageClasses;
using Microsoft.Xna.Framework;

namespace zhashi.Content.Items.Weapons.Ritual
{
    public class BloodEtcher : ModItem
    {
        public override void SetDefaults()
        {
            // 血祭武器，伤害较高
            Item.damage = 15;
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
            Item.value = Item.sellPrice(0, 1, 50, 0);
            Item.rare = ItemRarityID.Blue;
        }

        public override void HoldItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                // 指向新的鲜血画笔弹幕
                int type = ModContent.ProjectileType<RitualBrushPro_Blood>();
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
            // 配方 1：猩红矿锭 (Crimtane)
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<RitualKnife>())
                .AddIngredient(ItemID.CrimtaneBar, 10)
                .AddTile(TileID.DemonAltar) // 恶魔/猩红祭坛
                .Register();

            // 配方 2：魔矿锭 (Demonite)
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<RitualKnife>())
                .AddIngredient(ItemID.DemoniteBar, 10)
                .AddTile(TileID.DemonAltar)
                .Register();
        }
    }
}