using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Projectiles.Ammo;

namespace zhashi.Content.Items.Ammo
{
    public class ExorcismBullet : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("除魔子弹");
            // Tooltip.SetDefault("附加了神圣力量的特殊子弹，对邪恶生物有奇效。");
        }

        public override void SetDefaults()
        {
            Item.damage = 12; // 比普通子弹(7)高
            Item.DamageType = DamageClass.Ranged;
            Item.width = 8;
            Item.height = 8;
            Item.maxStack = 9999; // 堆叠上限
            Item.consumable = true; // 是消耗品
            Item.knockBack = 4f;
            Item.value = 50;
            Item.rare = ItemRarityID.Green;

            // 关键属性
            Item.shoot = ModContent.ProjectileType<ExorcismBulletProjectile>(); // 发射刚才写的弹幕
            Item.shootSpeed = 5f; // 子弹速度补正
            Item.ammo = AmmoID.Bullet; // 归类为“子弹”，所有枪都能用
        }

        public override void AddRecipes()
        {
            CreateRecipe(50) // 一次做50个
                .AddIngredient(ItemID.MusketBall, 50) // 需要50个普通子弹
                .AddIngredient(ItemID.FallenStar, 25) // 需要1个星星
                .AddTile(TileID.Anvils) // 在铁砧制作
                .Register();
        }
    }
}