using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content;

namespace zhashi.Content.Items.Accessories
{
    public class MotherCard : BlasphemyCardBase
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
            mp.isMotherCardEquipped = true;

            // [生命源泉]
            player.statLifeMax2 += 100;
            player.lifeRegen += 6;

            // [自然恩赐]
            player.honey = true;

            // 【核心修复】直接删除 player.palaBuff = true; 
            // 因为我们在下面已经加了 endurance (伤害减免)，效果是一样的

            // 药水冷却减少
            player.potionDelayTime = (int)(player.potionDelayTime * 0.75f);

            // [大地守护]
            player.statDefense += 15;
            player.endurance += 0.10f; // 这里的 10% 减伤足以替代帕拉丁效果

            // [丰收之力]
            player.maxMinions += 1;
            player.GetDamage(DamageClass.Summon) += 0.10f;
        }
    }
}