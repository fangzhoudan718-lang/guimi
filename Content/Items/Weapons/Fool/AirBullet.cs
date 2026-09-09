using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content;
using zhashi.Content.Projectiles.Weapons.Fool;

namespace zhashi.Content.Items.Weapons.Fool
{
    public class AirBullet : LotMItem
    {
        public override string Pathway => "Fool";
        public override int RequiredSequence => 7;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("空气弹");
        }

        public override void SetDefaults()
        {
            Item.damage = 60; // 基础伤害
            Item.DamageType = DamageClass.Magic;
            Item.width = 20;
            Item.height = 20;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Thrust;
            Item.noMelee = true;
            Item.noUseGraphic = true; // 不显示手持物品
            Item.knockBack = 6;
            Item.value = 5000;
            Item.rare = ItemRarityID.Orange;
            Item.UseSound = SoundID.Item50;
            Item.autoReuse = true;

            Item.shoot = ModContent.ProjectileType<AirBulletProjectile>();
            Item.shootSpeed = 15f;
        }

        public override bool CanUseItem(Player player)
        {
            LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();
            // 消耗 5 点灵性
            return base.CanUseItem(player) && modPlayer.TryConsumeSpirituality(5.0f);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Cloud, 10)
                .AddTile(TileID.WorkBenches)
                .Register();
        }

        // =========================================================
        // 【核心修复】唯一的伤害判定方法 (包含了序列6、5、4的逻辑)
        // =========================================================
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

            // 序列3：岸防炮 (10倍伤害)
            if (modPlayer.currentFoolSequence <= 3)
            {
                damage += 9.0f; // +900%
            }
            // 序列4 (诡法师)：炮弹层次 (5倍伤害)
            if (modPlayer.currentFoolSequence <= 4)
            {
                damage += 4.0f; // +400%
            }
            // 序列5 (秘偶大师)：蒸汽步枪 (3倍伤害)
            else if (modPlayer.currentFoolSequence <= 5)
            {
                damage += 2.0f; // +200%
            }
            // 序列6 (无面人)：威力翻倍 (2倍伤害)
            else if (modPlayer.currentFoolSequence <= 6)
            {
                damage += 1.0f; // +100%
            }
        }
    }
}