using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Projectiles.Ritual;
using zhashi.Content.DamageClasses;
using Microsoft.Xna.Framework;

namespace zhashi.Content.Items.Weapons.Ritual
{
    public class RitualKnife : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 1;
            Item.DamageType = ModContent.GetInstance<RitualDamage>();
            Item.width = 32;
            Item.height = 32;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noUseGraphic = true;
            Item.channel = true;

            // 我们在 HoldItem 里手动生成，所以这里设为 None 防止左键双重生成
            Item.shoot = ProjectileID.None;

            Item.autoReuse = true;
            Item.value = Item.sellPrice(0, 0, 50, 0);
            Item.rare = ItemRarityID.Blue;
        }

        // 只要拿着武器，每一帧都会调用这个方法
        public override void HoldItem(Player player)
        {
            // 如果玩家属于自己（防止联机bug）
            if (player.whoAmI == Main.myPlayer)
            {
                // 检查是否已经有画笔弹幕了
                int type = ModContent.ProjectileType<RitualBrushProjectile>();
                if (player.ownedProjectileCounts[type] < 1)
                {
                    // 如果没有，立刻生成一个“常驻画笔”
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
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 10)
                .AddIngredient(ItemID.StoneBlock, 10)
                .AddIngredient(ItemID.Amethyst, 1)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}