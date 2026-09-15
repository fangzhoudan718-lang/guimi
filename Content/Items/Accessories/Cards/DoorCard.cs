using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content; // 引用 LotMPlayer
using zhashi.Content.Pathways.Door;

namespace zhashi.Content.Items.Accessories
{
    public class DoorCard : BlasphemyCardBase
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
            // 1. 核心标记
            mp.isDoorCardEquipped = true;

            // 2. 旅行家体魄与空间亲和
            player.moveSpeed += 0.5f;          // 移动速度 +50%
            player.accRunSpeed += 3f;          // 跑步加速度大幅提升
            player.pickSpeed -= 0.25f;         // 挖掘速度提升 (象征“开门”打通阻碍)
            player.GetDamage(DamageClass.Magic) += 0.15f; // 门途径通常偏向法术/戏法

            // 3. 门途径专属：所有序列4-1空间权柄技能灵性消耗降低20%。
            player.GetModPlayer<DoorPathwayPlayer>().cardSpatialEfficiency = true;
            if (mp.currentDoorSequence <= 4)
            {
                player.endurance += 0.05f;
                player.GetAttackSpeed(DamageClass.Generic) += 0.10f;
            }
        }
    }
}
