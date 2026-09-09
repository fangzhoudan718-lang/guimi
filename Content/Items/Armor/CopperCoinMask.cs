using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using Microsoft.Xna.Framework;
using zhashi.Content;
using zhashi.Content.DaqianLu; // 引用 DaqianLuPlayer

namespace zhashi.Content.Items.Armor
{
    [AutoloadEquip(EquipType.Face)]
    public class CopperCoinMask : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.value = Item.sellPrice(gold: 1);
            Item.rare = ItemRarityID.Orange;
            Item.defense = 5;
            Item.accessory = true; // 饰品属性
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            var lotmPlayer = player.GetModPlayer<LotMPlayer>();
            var dqPlayer = player.GetModPlayer<DaqianLuPlayer>();

            // 核心修改：激活“心素”状态，允许显示理智条
            dqPlayer.isWearingMask = true;

            float sanityPercent = lotmPlayer.sanityCurrent / lotmPlayer.sanityMax;

            // 基础效果
            player.GetCritChance(DamageClass.Generic) += 5f;

            // 动态机制
            if (sanityPercent > 0.7f)
            {
                player.statDefense += 10;
                player.lifeRegen += 2;
            }
            else if (sanityPercent < 0.3f)
            {
                player.statDefense -= 5;
                player.GetDamage(DamageClass.Generic) += 0.15f;
                player.endurance += 0.1f;

                if (Main.rand.NextBool(60))
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.CoinPickup.WithPitchOffset(-0.5f), player.position);
            }
        }

        public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips)
        {
            foreach (TooltipLine line in tooltips)
            {
                if (line.Name == "Tooltip0") line.OverrideColor = new Color(200, 50, 50);
                if (line.Name == "Tooltip1") line.OverrideColor = Color.Gold;
            }
        }
    }
}