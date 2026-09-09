using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content; // 引用 LotMPlayer

namespace zhashi.Content.Items.Accessories
{
    public class BlackEmperorCard : BlasphemyCardBase
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
            mp.isBlackEmperorCardEquipped = true;

            // 2. 基础效果：律法主宰

            // [扭曲]：无视规则
            player.GetArmorPenetration(DamageClass.Generic) += 30; // 极高的破甲，象征利用规则漏洞

            // [威严]：秩序护盾
            player.statDefense += 20; // 高额防御加成

            // [阴影]：躲避
            // 给予 10% 的几率完全免疫伤害 (类似黑腰带)
            player.blackBelt = true;

            // [贿赂/扭曲]：全伤害提升
            player.GetDamage(DamageClass.Generic) += 0.15f;

            // 3. 预留接口：黑皇帝途径专属加成
            // 未来可实现：无限复活、放大/缩小敌人、制定规则禁止某种行为
            /*
            if (mp.baseLawyerSequence <= 0) // 黑皇帝序列0
            {
                // TODO: 真正的“皇帝归来”
            }
            */
        }
    }
}