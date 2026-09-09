using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content;

namespace zhashi.Content.Items.Accessories
{
    public class TyrantCard : BlasphemyCardBase
    {
        public override void SafeSetDefaults()
        {
            Item.width = 28;
            Item.height = 34;
            Item.accessory = true;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(platinum: 5);
            Item.maxStack = 1;
        }

        public override void SafeUpdateAccessory(Player player, LotMPlayer mp, bool hideVisual)
        {
            mp.isTyrantCardEquipped = true;

            // [海洋眷顾]
            player.gills = true;
            player.ignoreWater = true;
            player.accFlipper = true;
            player.accDivingHelm = true;

            // [暴君之怒]
            player.GetDamage(DamageClass.Melee) += 0.25f;

            // 【核心修复】meleeSpeed 已过时，改用 GetAttackSpeed
            player.GetAttackSpeed(DamageClass.Melee) += 0.20f;

            player.kbBuff = true;

            // [神性体魄]
            player.noKnockback = true;
            player.statLifeMax2 += 50;
        }
    }
}