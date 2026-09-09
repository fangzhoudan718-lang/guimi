using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace zhashi.Content.Buffs
{
    public class AbyssShacklesDebuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            // 强力减速效果
            npc.velocity *= 0.6f; // 速度变为原来的 60%

            // 视觉效果：黑色粒子缠绕
            if (Main.rand.NextBool(3))
            {
                int d = Dust.NewDust(npc.position, npc.width, npc.height, Terraria.ID.DustID.Shadowflame, 0, 0, 100, default, 1.5f);
                Main.dust[d].noGravity = true;
            }
        }
    }
}