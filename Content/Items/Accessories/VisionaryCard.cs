using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content; // 引用 LotMPlayer

namespace zhashi.Content.Items.Accessories
{
    public class VisionaryCard : BlasphemyCardBase
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
            mp.isVisionaryCardEquipped = true;

            // 2. 基础效果：幕后操控者
            // 空想家途径擅长躲在幕后，通过“想象”和“暗示”来战斗

            player.aggro -= 1000;              // 观众模式：极大幅度降低怪物仇恨
            player.maxMinions += 2;            // 空想具现：想象出的生物
            player.maxTurrets += 2;            // 空想具现：想象出的构造体
            player.GetDamage(DamageClass.Magic) += 0.20f; // 心灵风暴伤害

            // 3. 预留接口：空想家途径专属加成
            // 未来可实现：强制控制敌人、修改环境、凭空造物等
            /*
            if (mp.baseVisionarySequence <= 4)
            {
                // TODO: 真正的“想象即现实”
            }
            */
        }
    }
}