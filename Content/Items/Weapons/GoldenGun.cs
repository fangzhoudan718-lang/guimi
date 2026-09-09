using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace zhashi.Content.Items.Weapons
{
    public class GoldenGun : ModItem
    {
        public override void SetDefaults()
        {
            // 基础属性
            Item.damage = 15;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 40;
            Item.height = 20;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 4;
            Item.value = 10000;
            Item.rare = ItemRarityID.Green;
            Item.UseSound = SoundID.Item11;
            Item.autoReuse = true;

            // 弹药设置
            Item.shoot = ProjectileID.PurificationPowder;
            Item.shootSpeed = 10f;
            Item.useAmmo = AmmoID.Bullet;
        }

        // ★★★ 位置微调 ★★★
        public override Vector2? HoldoutOffset()
        {
            // new Vector2(X, Y)
            // X (水平): 负数向左(向后)，正数向右(向前)
            // Y (垂直): 正数向下，负数向上

            // 这里设置为 (-2, 5)
            // -2: 让枪往身体靠一点点，看起来更自然
            //  5: 满足你要求的“下移5像素”
            return new Vector2(-2, 5);
        }
    }
}