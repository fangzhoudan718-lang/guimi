using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace zhashi.Content.Buffs
{
    public class ConquerorWill : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true; // 这是一个负面效果
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            // 征服效果：防御崩溃，持续扣血
            npc.defense -= 30;
            npc.lifeRegen -= 60; // 每秒扣很多血

            // 紫色火焰视觉效果
            if (Main.rand.NextBool(3))
            {
                int d = Dust.NewDust(npc.position, npc.width, npc.height, DustID.Shadowflame, 0, 0, 100, default, 2f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 2f;
            }
        }
    }
}