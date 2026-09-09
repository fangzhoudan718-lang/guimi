using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content; // 引用 LotMPlayer

namespace zhashi.Content.Items.Accessories
{
    public class AbyssCard : BlasphemyCardBase
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
            mp.isAbyssCardEquipped = true;

            // 2. 基础效果：恶魔 / 连环杀手

            // [恶魔之体]：适应极端环境
            player.lavaImmune = true;          // 免疫岩浆伤害
            player.fireWalk = true;            // 免疫燃烧块伤害
            player.buffImmune[BuffID.OnFire] = true;        // 免疫普通着火
            player.buffImmune[BuffID.CursedInferno] = true; // 免疫诅咒地狱火

            // [钢铁皮肤]：恶魔的高防御
            player.endurance += 0.15f;         // 受到伤害减少 15%
            player.statLifeMax2 += 40;         // 生命值提升

            // [污秽攻击]：攻击附加火焰 (类似岩浆石)
            player.magmaStone = true;
            player.GetDamage(DamageClass.Generic) += 0.15f; // 全伤害提升

            // [危险感知]：罪犯途径的核心能力，对恶意极其敏感
            player.dangerSense = true;

            // 3. 预留接口：深渊途径专属加成
            // 未来可实现：恶魔变身、岩浆喷射、甚至引爆敌人的情绪
            /*
            if (mp.baseAbyssSequence <= 4)
            {
                // TODO: 真正的“恶魔形态”
            }
            */
        }
    }
}