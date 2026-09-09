using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace zhashi.Content.Buffs
{
    public class MarionetteTownNPCBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true; // 算作 Debuff
            Main.pvpBuff[Type] = false;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            // 视觉效果：像秘偶一样变成深紫色/黑色
            npc.color = Color.Purple;

            // 冒出灵性黑烟
            if (Main.rand.NextBool(10))
            {
                Dust.NewDust(npc.position, npc.width, npc.height,
                    Terraria.ID.DustID.DemonTorch, 0, 0, 100, default, 1.0f);
            }

            // 让 NPC 动作变慢，表现出木偶的僵硬感
            npc.velocity.X *= 0.9f;
        }
    }
}