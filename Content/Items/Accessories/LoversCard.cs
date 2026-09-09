using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content; // 确保引用了 LotMPlayer
using zhashi.Content.Items;

namespace zhashi.Content.Items.Accessories
{
    public class LoversCard : BlasphemyCardBase
    {
        public override void SafeSetDefaults()
        {
            Item.width = 28;
            Item.height = 34;
            Item.accessory = true;
            Item.rare = ItemRarityID.Red; // 红色稀有度
            Item.value = Item.sellPrice(platinum: 5); // 价值
            Item.maxStack = 1;
        }

        public override void SafeUpdateAccessory(Player player, LotMPlayer mp, bool hideVisual)
        {
            mp.isLoversCardEquipped = true;

            player.buffImmune[BuffID.Confused] = true;
            player.aggro -= 400;

            if (mp.baseMarauderSequence <= 9)
            {
                player.GetArmorPenetration(DamageClass.Generic) += 25;
                player.GetCritChance(DamageClass.Generic) += 12;

                player.manaMagnet = true;
                player.goldRing = true;



                player.blackBelt = true;
                player.endurance += 0.10f;
                player.moveSpeed += 0.25f;
                player.accRunSpeed += 3f;
                player.lifeRegen += 4;
                player.manaRegenDelayBonus += 1;
            }
        }
    }
}