using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Items.Potions.Spirituality
{
    /// <summary>
    /// 灵性恢复药水。四档与其它药水一样从小到大排：
    /// 弱效 → 普通 → 强效 → 超级，恢复比例与冷却逐档提高。
    ///
    /// 每一档都有两条配方：一条用墓碑（凡人用自己的死换来的东西），
    /// 一条用门途径超凡者陨落后析出的学徒途径非凡特性。两条路等价。
    /// </summary>
    public abstract class SpiritualityPotionBase : ModItem
    {
        /// <summary>恢复的固定灵性数值（不是百分比）。</summary>
        protected abstract int RestoreAmount { get; }
        /// <summary>喝完之后的灵性激荡时长（tick）。</summary>
        protected abstract int SicknessTicks { get; }
        protected abstract int TombstoneCount { get; }
        protected abstract int EssenceCount { get; }
        protected abstract int ItemWidth { get; }
        protected abstract int ItemHeight { get; }
        protected abstract int Rarity { get; }
        protected abstract int ValueCopper { get; }
        /// <summary>除了水瓶和主料之外的配料。</summary>
        protected abstract (int type, int count)[] Extras { get; }

        public override void SetDefaults()
        {
            Item.width = ItemWidth;
            Item.height = ItemHeight;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 30;
            Item.consumable = true;
            Item.rare = Rarity;
            Item.value = Item.sellPrice(copper: ValueCopper);
        }

        public override bool CanUseItem(Player player) => !player.HasBuff(BuffID.ManaSickness);

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer) return true;

            LotMPlayer lotm = player.GetModPlayer<LotMPlayer>();
            if (lotm.spiritualityCurrent >= lotm.spiritualityMax)
            {
                Main.NewText("灵性已满，喝下去只是浪费。", 180, 160, 220);
                return false;
            }

            int restore = RestoreAmount;
            lotm.spiritualityCurrent = System.Math.Min(lotm.spiritualityMax, lotm.spiritualityCurrent + restore);
            player.AddBuff(BuffID.ManaSickness, SicknessTicks);

            SoundEngine.PlaySound(SoundID.Item3, player.Center);
            Main.NewText($"灵性恢复了 {restore} 点。", 190, 150, 230);
            CombatText.NewText(player.getRect(), new Color(190, 150, 230), $"+{restore}", true);
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "SpiritualityPotionHint",
                $"[c/9A6AB0:恢复 {RestoreAmount} 点灵性（固定数值）]\n" +
                $"[c/AAAAAA:喝下后 {SicknessTicks / 60f:F0} 秒内不能再喝任何灵性药水]"));
        }

        public override void AddRecipes()
        {
            // 配方一：墓碑
            Recipe byGrave = CreateRecipe();
            byGrave.AddIngredient(ItemID.BottledWater, 1);
            byGrave.AddIngredient(ItemID.Tombstone, TombstoneCount);
            foreach (var extra in Extras) byGrave.AddIngredient(extra.type, extra.count);
            byGrave.AddTile(TileID.Bottles);
            byGrave.Register();

            // 配方二：门途径超凡者析出的非凡特性
            Recipe byEssence = CreateRecipe();
            byEssence.AddIngredient(ItemID.BottledWater, 1);
            byEssence.AddIngredient(ModContent.ItemType<Content.Items.Materials.SpiritEssence>(), EssenceCount);
            foreach (var extra in Extras) byEssence.AddIngredient(extra.type, extra.count);
            byEssence.AddTile(TileID.Bottles);
            byEssence.Register();
        }
    }

    /// <summary>弱效：入门那一档，一朵太阳花就能配出来。</summary>
    public class LesserSpiritualityPotion : SpiritualityPotionBase
    {
        protected override int RestoreAmount => 50;
        protected override int SicknessTicks => 15 * 60;
        protected override int TombstoneCount => 1;
        protected override int EssenceCount => 1;
        protected override int ItemWidth => 20;
        protected override int ItemHeight => 26;
        protected override int Rarity => ItemRarityID.Blue;
        protected override int ValueCopper => 5000;
        protected override (int, int)[] Extras => new[] { ((int)ItemID.Daybloom, 1), ((int)ItemID.Shiverthorn, 2) };
    }

    /// <summary>普通：死亡草打底，中期的主力。</summary>
    public class SpiritualityPotion : SpiritualityPotionBase
    {
        protected override int RestoreAmount => 150;
        protected override int SicknessTicks => 25 * 60;
        protected override int TombstoneCount => 2;
        protected override int EssenceCount => 1;
        protected override int ItemWidth => 20;
        protected override int ItemHeight => 26;
        protected override int Rarity => ItemRarityID.Green;
        protected override int ValueCopper => 15000;
        protected override (int, int)[] Extras => new[] { ((int)ItemID.Deathweed, 3), ((int)ItemID.Moonglow, 2) };
    }

    /// <summary>强效：掺进水晶碎块与坠落之星，肉后开始常备。</summary>
    public class GreaterSpiritualityPotion : SpiritualityPotionBase
    {
        protected override int RestoreAmount => 500;
        protected override int SicknessTicks => 40 * 60;
        protected override int TombstoneCount => 5;
        protected override int EssenceCount => 2;
        protected override int ItemWidth => 20;
        protected override int ItemHeight => 28;
        protected override int Rarity => ItemRarityID.LightRed;
        protected override int ValueCopper => 40000;
        protected override (int, int)[] Extras => new[]
        {
            ((int)ItemID.Moonglow, 5), ((int)ItemID.CrystalShard, 3), ((int)ItemID.FallenStar, 2)
        };
    }

    /// <summary>超级：掺进灵气，天使层面的续航。</summary>
    public class SuperSpiritualityPotion : SpiritualityPotionBase
    {
        protected override int RestoreAmount => 1500;
        protected override int SicknessTicks => 60 * 60;
        protected override int TombstoneCount => 10;
        protected override int EssenceCount => 3;
        protected override int ItemWidth => 20;
        protected override int ItemHeight => 30;
        protected override int Rarity => ItemRarityID.Purple;
        protected override int ValueCopper => 100000;
        protected override (int, int)[] Extras => new[]
        {
            ((int)ItemID.Moonglow, 5), ((int)ItemID.CrystalShard, 5), ((int)ItemID.Ectoplasm, 3)
        };
    }
}
