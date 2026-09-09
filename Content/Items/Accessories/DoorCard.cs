using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content; // 引用 LotMPlayer

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

            // 2. 基础效果：旅行家的体魄 (在没有途径逻辑前的临时/基础效果)
            player.moveSpeed += 0.5f;          // 移动速度 +50%
            player.accRunSpeed += 3f;          // 跑步加速度大幅提升
            player.pickSpeed -= 0.25f;         // 挖掘速度提升 (象征“开门”打通阻碍)
            player.GetDamage(DamageClass.Magic) += 0.15f; // 门途径通常偏向法术/戏法

            // 3. 预留接口：门途径专属加成
            // 等你以后写了门途径的逻辑，可以在这里取消注释并添加代码
            /*
            if (mp.baseDoorSequence <= 4)
            {
                // 比如：传送消耗减少、穿墙能力增强等
            }
            */
        }
    }
}