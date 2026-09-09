using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Projectiles.Ritual;
using zhashi.Content.DamageClasses;
using Microsoft.Xna.Framework;

namespace zhashi.Content.Items.Weapons.Ritual
{
    public class StarfallEtcher : ModItem
    {
        public override void SetDefaults()
        {
            // 伤害比毛笔稍高
            Item.damage = 14;
            Item.DamageType = ModContent.GetInstance<RitualDamage>();
            Item.width = 34;
            Item.height = 34;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.shoot = ProjectileID.None;
            Item.autoReuse = true;
            Item.value = Item.sellPrice(0, 1, 0, 0); // 1金
            Item.rare = ItemRarityID.Green; // 绿色品质
        }

        public override void HoldItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                // 指向新的星光画笔弹幕
                int type = ModContent.ProjectileType<RitualBrushPro_Star>();
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
            // 配方：仪式刻录刀 + 星怒 + 5个坠落之星 + 3个铁/铅锭
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<RitualKnife>())
                .AddIngredient(ItemID.Starfury) // 【新增】加入星怒作为材料
                .AddIngredient(ItemID.FallenStar, 20)
                .AddRecipeGroup(RecipeGroupID.IronBar, 3)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}