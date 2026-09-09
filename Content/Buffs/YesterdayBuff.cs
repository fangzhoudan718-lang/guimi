using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace zhashi.Content.Buffs
{
    public class YesterdayBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = false;
            // DisplayName.SetDefault("昨日重现");
            // Description.SetDefault("借来的力量充盈全身...");
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // 全属性爆炸提升
            player.GetDamage(DamageClass.Generic) += 1.0f; // 伤害翻倍
            player.statDefense += 100;                     // 防御+100
            player.lifeRegen += 50;                        // 回血极快
            player.moveSpeed += 1.0f;                      // 移速翻倍
            player.endurance += 0.3f;                      // 30% 免伤
            player.GetCritChance(DamageClass.Generic) += 20;

            // 特效
            if (Main.rand.NextBool(5))
            {
                Dust.NewDust(player.position, player.width, player.height, DustID.Smoke, 0, 0, 100, Microsoft.Xna.Framework.Color.Gray, 1.5f);
            }
        }
    }
}