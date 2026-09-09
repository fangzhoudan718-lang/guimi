using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content; // 引用 LotMPlayer

namespace zhashi.Content.Items.Accessories
{
    public class DarknessCard : BlasphemyCardBase
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
            mp.isDarknessCardEquipped = true;

            // 2. 基础效果：守夜人 / 隐秘之仆

            // [不眠之眼]：黑暗中的视觉
            player.nightVision = true;         // 夜视
            player.dangerSense = true;         // 危险感知
            player.buffImmune[BuffID.Darkness] = true; // 免疫黑暗
            player.buffImmune[BuffID.Blackout] = true; // 免疫黑视
            player.buffImmune[BuffID.Silenced] = true; // 隐秘亦代表寂静

            // [黑夜主宰]：夜晚属性翻倍
            if (!Main.dayTime)
            {
                player.GetDamage(DamageClass.Generic) += 0.20f; // 夜晚伤害+20%
                player.GetCritChance(DamageClass.Generic) += 10f; // 夜晚暴击+10%
                player.statDefense += 15;                       // 夜晚防御+15
                player.moveSpeed += 0.3f;                       // 夜晚移动速度+30%
            }
            else
            {
                // 白天只有微弱加成
                player.GetDamage(DamageClass.Generic) += 0.05f;
            }

            // [隐秘权柄]：难以被发现
            player.aggro -= 400;               // 降低敌人仇恨
            player.shroomiteStealth = true;    // 站立不动时进入隐身状态 (类似蘑菇矿套)

            // 3. 预留接口：黑暗途径专属加成
            // 未来可实现：强制入梦、厄运诅咒、黑夜领域
            /*
            if (mp.baseDarknessSequence <= 4)
            {
                // TODO: 真正的“绯红之月”与“永恒安眠”
            }
            */
        }
    }
}