using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content;

namespace zhashi.Content.Items.Accessories
{
    public class DemonessCard : BlasphemyCardBase
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
            mp.isDemonessCardEquipped = true;

            // [致命一击]
            player.GetCritChance(DamageClass.Generic) += 20f;

            // [魔女毒素/寒冰]
            player.frostBurn = true;
            player.GetDamage(DamageClass.Generic) += 0.10f;

            // [魅惑舞步]
            player.moveSpeed += 0.4f;
            player.accRunSpeed += 2f;

            // [痛苦之刺]
            player.starCloakItem = Item;

            // [魅惑] - 混乱之脑
            player.brainOfConfusionItem = Item;

            // ======================================================
            // [新增功能] 女巫扫帚权限
            // ======================================================
            if (mp.currentDemonessSequence <= 9)
            {
                // 只负责开启权限，具体按键逻辑交给 LotMPlayer.PostUpdate 处理
                // 这样能完美解决“按键无效”或“无限上下马”的问题
                mp.canUseWitchBroom = true;
            }
        }
    }
}