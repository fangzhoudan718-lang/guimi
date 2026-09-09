using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace zhashi.Content.Buffs.Debuffs
{
    public class TimeStagnationBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            // DisplayName.SetDefault("时间凝固");
            // Description.SetDefault("你的时间被盗取了...");
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            // 视觉效果：变成黑白色/灰色，象征时间静止
            npc.color = Color.Gray;

            // 如果你希望稍微扣点血（时间侵蚀），可以加一行：
            // npc.lifeRegen -= 20; 
        }
    }
}