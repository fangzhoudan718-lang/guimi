using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization; // 必须引用这个
using Microsoft.Xna.Framework;
using zhashi.Content.Projectiles;
using zhashi.Content.Items.Materials; // 引用材料命名空间
using zhashi.Content; // 引用 Player 命名空间

namespace zhashi.Content.Items.Weapons
{
    public class ConquerorSword : LotMItem
    {
        // 条件：猎人途径 序列1
        public override string Pathway => "Hunter";
        public override int RequiredSequence => 1;

        public override void SetDefaults()
        {
            Item.damage = 3500;
            Item.DamageType = DamageClass.Melee;
            Item.width = 60;
            Item.height = 60;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.noUseGraphic = false;
            Item.noMelee = false;
            Item.knockBack = 600;

            // 价值改为无价
            Item.value = 0;

            Item.rare = ItemRarityID.Red;
            Item.UseSound = SoundID.Item71;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<ConquerorSwordProjectile>();
            Item.shootSpeed = 20f;
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            position += Vector2.Normalize(velocity) * 40f;
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-15f, -5f);
        }

        // 【核心修复】配方添加逻辑
        public override void AddRecipes()
        {
            CreateRecipe()
                // 使用 1 个 "ConquerorCharacteristic" (序列1特性)
                .AddIngredient(ModContent.ItemType<ConquerorCharacteristic>(), 1)
                // 【修复点】这里使用了正确的 Condition 写法
                // Language.GetOrRegister 会自动注册一个本地化键值，你可以在 hjson 里修改它的显示文本
                .AddCondition(new Condition(
                    Language.GetOrRegister("Mods.zhashi.Conditions.ConquerorOnly"),
                    () => Main.LocalPlayer.GetModPlayer<LotMPlayer>().currentHunterSequence <= 1
                ))
                .Register();
        }
    }
}