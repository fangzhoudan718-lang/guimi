using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content; // 引用 LotMPlayer

namespace zhashi.Content.Items.Accessories
{
    public class JusticiarCard : BlasphemyCardBase
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
            mp.isJusticiarCardEquipped = true;

            // 2. 基础效果：仲裁人 / 处刑者

            // [秩序壁垒]：如钢铁般坚硬
            player.statDefense += 20;          // 高额防御
            player.noKnockback = true;         // 免疫击退 (秩序不可撼动)

            // [精神抗性]：规则守护者不会被迷惑
            player.buffImmune[BuffID.Confused] = true; // 免疫混乱
            player.buffImmune[BuffID.Slow] = true;     // 免疫减速

            // [裁决之力]：处决敌人
            player.GetDamage(DamageClass.Generic) += 0.15f;    // 全伤害提升
            player.GetCritChance(DamageClass.Generic) += 10f;  // 暴击率提升 (代表“审判”)

            // 3. 预留接口：审判者途径专属加成
            // 未来可实现：区域禁锢、强制削弱敌人、精神刺穿
            /*
            if (mp.baseArbiterSequence <= 4)
            {
                // TODO: 真正的“此地禁止飞行”
            }
            */
        }
    }
}