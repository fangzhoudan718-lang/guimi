using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content; // 引用 Player

namespace zhashi.Content.Items.Weapons.Fool
{
    public class PaperCard : LotMItem
    {
        public override string Pathway => "Fool";
        public override int RequiredSequence => 8; // 序列8可用

        public override void SetStaticDefaults()
        {
            // 名字和描述
        }

        public override void SetDefaults()
        {
            Item.damage = 35;
            Item.DamageType = DamageClass.Generic;
            Item.width = 20;
            Item.height = 20;
            Item.useTime = 12;
            Item.useAnimation = 12;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.knockBack = 2;
            Item.value = 1000;
            Item.rare = ItemRarityID.Green;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;

            // 引用弹幕 (因为现在大家都在同一个 namespace 下，所以直接写名字就行)
            Item.shoot = ModContent.ProjectileType<PaperCardProjectile>();
            Item.shootSpeed = 16f;
        }

        public override bool CanUseItem(Player player)
        {
            LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();
            // 消耗 2 点灵性
            return base.CanUseItem(player) && modPlayer.TryConsumeSpirituality(2.0f);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 10)
                .AddIngredient(ItemID.Book, 1)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}