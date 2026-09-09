using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace zhashi.Content.Items.Weapons
{
    public class NighthawkPistol : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("值夜人制式手枪");
            // Tooltip.SetDefault("阿霍瓦郡警察厅特殊行动部的标准装备。\n‘恐惧来自于未知，而这把枪能带给你确定的安全感。’");
        }

        public override void SetDefaults()
        {
            // --- 修改点 1：高伤害 ---
            Item.damage = 48; // 原 18 -> 48，大幅提高单发伤害

            Item.DamageType = DamageClass.Ranged;
            Item.width = 40;
            Item.height = 20;

            // --- 修改点 2：慢攻速 ---
            // 泰拉瑞亚中 60 = 1秒。
            // 原 15 (0.25秒) -> 改为 45 (0.75秒)，射速变慢，更有节奏感
            Item.useTime = 45;
            Item.useAnimation = 45;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;

            // 稍微增加击退，体现大口径手枪的冲击力
            Item.knockBack = 6f; // 原 3 -> 6

            Item.value = 10000;
            Item.rare = ItemRarityID.Green;

            // 可以考虑换成更厚重的枪声，比如 Item40 (手炮) 或 Item38 (猎枪)
            // 这里暂时保持原样 Item11 (普通手枪)
            Item.UseSound = SoundID.Item11;

            Item.autoReuse = false; // 慢速手枪通常不自动连发

            // 弹药设置
            Item.shoot = ProjectileID.PurificationPowder;
            Item.shootSpeed = 14f; // 稍微提高弹速 (原 12 -> 14) 让子弹更准
            Item.useAmmo = AmmoID.Bullet;
        }

        // --- 修改点 3：贴图位置微调 ---
        public override Vector2? HoldoutOffset()
        {
            // new Vector2(X, Y)
            // X: 左右偏移 (负数向左/向身后)
            // Y: 上下偏移 (正数向下，负数向上)
            // 这里 Y 改为 3，即向下移动 3 像素
            return new Vector2(-2, 7);
        }
    }
}