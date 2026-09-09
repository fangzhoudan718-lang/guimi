using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content; // 引用 LotMPlayer

namespace zhashi.Content.Items.Accessories
{
    public class HermitCard : BlasphemyCardBase
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
            mp.isHermitCardEquipped = true;

            // 2. 基础效果：窥秘人 / 巫师

            // [神秘学识]：强大的法术驾驭能力
            player.GetDamage(DamageClass.Magic) += 0.25f; // 魔法伤害大幅提升
            player.manaCost -= 0.15f;                     // 魔力消耗减少

            // [窥秘之眼]：看穿弱点 (暴击)
            player.GetCritChance(DamageClass.Magic) += 15f;

            // [灵性充盈]：类似星云盔甲的回蓝效果
            player.manaRegenDelayBonus += 1;
            player.manaRegenBonus += 50;       // 极快的回蓝速度，象征隐匿贤者的知识灌注

            // [预言]：危险感知 (基础能力)
            player.detectCreature = true;      // 生物探测
            player.dangerSense = true;         // 危险感知

            // 3. 预留接口：隐者途径专属加成
            // 未来可实现：神秘学重现（模仿其他法术）、预知未来（闪避）、变成信息流
            /*
            if (mp.baseHermitSequence <= 4)
            {
                // TODO: 真正的“神秘再现”
            }
            */
        }
    }
}