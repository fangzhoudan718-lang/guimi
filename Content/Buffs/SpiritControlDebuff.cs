using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace zhashi.Content.Buffs
{
    public class SpiritControlDebuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            // 身体僵硬：大幅减速
            npc.velocity *= 0.1f;

            // 视觉效果：变成深紫色/黑色
            npc.color = Color.Purple;

            // 冒出灵性黑烟
            if (Main.rand.NextBool(3))
            {
                Dust.NewDust(npc.position, npc.width, npc.height,
                    Terraria.ID.DustID.DemonTorch, 0, 0, 100, default, 1.5f);
            }
        }
    }
}