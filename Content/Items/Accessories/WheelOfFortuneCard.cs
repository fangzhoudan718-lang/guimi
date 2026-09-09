using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content; // 引用 LotMPlayer

namespace zhashi.Content.Items.Accessories
{
    public class WheelOfFortuneCard : BlasphemyCardBase
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
            mp.isWheelOfFortuneCardEquipped = true;


            player.luck += 0.9f;

            // [命运闪避]：大概率规避伤害
            player.blackBelt = true;           // 获得忍者装备的闪避效果 (约10%)
            player.brainOfConfusionItem = Item; // 获得混乱之脑的闪避效果 (约16%)
            // 叠加后生存能力极强，因为“命运不让你死”

            // [概率重击]：暴击率提升
            player.GetCritChance(DamageClass.Generic) += 15f;

            // [水银流动]：移动速度与灵活性
            player.moveSpeed += 0.3f;
            player.accRunSpeed += 2f;

            // 3. 预留接口：命运之轮途径专属加成
            // 未来可实现：重置状态(Reboot)、强制敌人大失败、必定暴击
            /*
            if (mp.baseWheelSequence <= 4)
            {
                // TODO: 真正的“重启”与“概率操控”
            }
            */
        }
    }
}