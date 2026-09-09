using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content;
using zhashi.Content.Items;

namespace zhashi.Content.Items.Accessories
{
    public class StrengthCard : BlasphemyCardBase
    {
        public override void SafeSetDefaults()
        {
            Item.width = 28;
            Item.height = 34;
            Item.accessory = true;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(platinum: 1);
        }

        public override void SafeUpdateAccessory(Player player, LotMPlayer mp, bool hideVisual)
        {
            // currentSequence 代表巨人途径
            if (mp.currentSequence <= 9)
            {
                // 1. 激活特效开关 (LotMPlayer 里的震荡破甲才会生效)
                mp.isStrengthCardEquipped = true;

                // 2. 数值翻倍 (简单粗暴)
                player.statDefense *= 2;                  // 防御翻倍
                player.GetDamage(DamageClass.Melee) *= 2f; // 近战伤害翻倍

                // 3. 伤势愈合 (泰坦之躯)
                player.lifeRegen += 10;      // 基础回复值 +10
                player.lifeRegenTime += 5;   // 受伤后极快开始回血
                player.shinyStone = true;    // 站立不动时获得极速回血 (闪耀石效果)

                // 4. 泰坦拳套效果
                player.kbGlove = true;          // 击退增强
                player.meleeScaleGlove = true;  // 近战武器变大 (近战范围增加)

                // 5. 霸体 (免疫击退)
                player.noKnockback = true;
            }
        }
    }
}