using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Projectiles;

namespace zhashi.Content.Items.SealedArtifacts
{
    /// <summary>
    /// 命运多面骰 (Polyhedron of Fate)
    /// 序列1巨蛇专属道具.
    /// 右键使用: 消耗800灵性, 随机投出4/6/8/10/12/20面骰之一,
    ///          不同面数有完全不同的效果池.
    /// </summary>
    public class PolyhedronOfFate : ModItem
    {
        // 可投出的骰子面数
        private static readonly int[] DieFaces = new int[] { 4, 6, 8, 10, 12, 20 };

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.useTurn = false;
            Item.UseSound = SoundID.Item29;
            Item.maxStack = 1;
            Item.consumable = false;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(gold: 200);
            Item.noUseGraphic = false;
        }

        public override bool CanUseItem(Player player)
        {
            var mp = player.GetModPlayer<LotMPlayer>();
            if (mp.currentWheelSequence > 1)
            {
                if (player.whoAmI == Main.myPlayer)
                    Main.NewText("只有命运之蛇方能掌握多面骰...", 200, 100, 100);
                return false;
            }
            if (mp.spiritualityCurrent < 800)
            {
                if (player.whoAmI == Main.myPlayer)
                    Main.NewText("灵性不足 (需 800 点)", 255, 50, 50);
                return false;
            }
            return true;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer) return true;
            var mp = player.GetModPlayer<LotMPlayer>();
            if (!mp.TryConsumeSpirituality(800)) return false;

            // 投出: 随机面数 + 随机点数
            int faces = DieFaces[Main.rand.Next(DieFaces.Length)];
            int result = Main.rand.Next(1, faces + 1);


            Main.NewText($"【命运多面骰】 投掷 D{faces} ...", 200, 200, 255);

            // 1秒后(60帧)执行效果 - 用 timer 字段在 LotMPlayer 里处理
            mp.QueuePolyhedronEffect(faces, result);

            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.LunarBar, 30)
                .AddIngredient(ItemID.FragmentNebula, 20)
                .AddIngredient(ItemID.FragmentVortex, 20)
                .AddIngredient(ItemID.FragmentSolar, 20)
                .AddIngredient(ItemID.FragmentStardust, 20)
                .AddIngredient(ItemID.LifeFruit, 10)
                .AddIngredient(ItemID.Ectoplasm, 30)
                .AddTile(TileID.LunarCraftingStation) // 古代操纵机
                .Register();
        }
    }
}
