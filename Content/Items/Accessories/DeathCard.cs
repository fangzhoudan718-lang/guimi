using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content; // 引用 LotMPlayer

namespace zhashi.Content.Items.Accessories
{
    public class DeathCard : BlasphemyCardBase
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
            mp.isDeathCardEquipped = true;

            // 2. 基础效果：冥界主宰 / 死亡执政官

            // [亡灵大军]：擅长驱使灵体
            player.maxMinions += 2;            // 增加召唤上限
            player.GetDamage(DamageClass.Summon) += 0.20f; // 增加召唤伤害

            // [死亡凋零]：让敌人腐朽 (护甲穿透)
            player.GetArmorPenetration(DamageClass.Generic) += 15;

            // [寒冷体质]：死神途径的高序列自带冥界寒气
            player.buffImmune[BuffID.Chilled] = true; // 免疫寒冷
            player.buffImmune[BuffID.Frozen] = true;  // 免疫冻结
            player.buffImmune[BuffID.Frostburn] = true; // 免疫霜火

            // 3. 预留接口：死神途径专属加成
            // 未来可实现：召唤冥界之门、复活尸体、即死攻击
            /*
            if (mp.baseDeathSequence <= 4)
            {
                // TODO: 真正的“苍白之死”
            }
            */
        }
    }
}