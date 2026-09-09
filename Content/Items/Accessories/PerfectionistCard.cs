using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content; // 引用 LotMPlayer

namespace zhashi.Content.Items.Accessories
{
    public class PerfectionistCard : BlasphemyCardBase
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
            mp.isPerfectionistCardEquipped = true;

            // 2. 基础效果：通识者 / 机械专家

            // [文明工匠]：强大的建设能力
            player.tileSpeed += 0.5f;          // 放置速度 +50%
            player.wallSpeed += 0.5f;          // 墙壁放置速度 +50%
            player.blockRange += 5;            // 放置/挖掘距离 +5
            player.pickSpeed -= 0.15f;         // 挖掘速度提升

            // [机械武装]：擅长使用枪械和炮台
            player.GetDamage(DamageClass.Ranged) += 0.20f; // 远程伤害提升 (枪炮)
            player.maxTurrets += 2;                        // 哨兵栏位 +2 (机械炮台)

            // [精密计算]：暴击率提升
            player.GetCritChance(DamageClass.Ranged) += 10f;

            // [完美装甲]：防御提升
            player.statDefense += 15;

            // 3. 预留接口：完美者途径专属加成
            // 未来可实现：炼金术、高达机甲、机械改造
            /*
            if (mp.baseParagonSequence <= 4)
            {
                // TODO: 真正的“机械飞升”
            }
            */
        }
    }
}