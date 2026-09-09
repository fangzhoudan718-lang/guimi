using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content; // 引用 LotMPlayer

namespace zhashi.Content.Items.Accessories
{
    public class WhiteTowerCard : BlasphemyCardBase
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
            mp.isWhiteTowerCardEquipped = true;

            // 2. 基础效果：全知之眼 (在没有序列逻辑前的通用效果)
            // 白塔途径擅长解析和模仿，所以给予全能型的法术加成

            player.statManaMax2 += 100;         // 知识渊博：大幅增加蓝量
            player.manaCost -= 0.20f;           // 精密计算：减少20%魔力消耗
            player.GetCritChance(DamageClass.Generic) += 15f; // 洞悉弱点：全伤害暴击率+15%

            // 侦测能力 (解析)
            player.detectCreature = true;       // 显示周围生物
            player.dangerSense = true;          // 危险感知

            // 3. 预留接口：白塔途径专属加成
            // 等你以后写了序列逻辑，可以在这里添加“模仿技能”、“解析光环”等
            /*
            if (mp.baseWhiteTowerSequence <= 4)
            {
                // TODO: 添加智者的高阶能力
            }
            */
        }
    }
}