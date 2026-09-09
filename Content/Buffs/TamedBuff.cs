using Terraria;
using Terraria.ModLoader;

namespace zhashi.Content.Buffs
{
    public class TamedBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {

            Main.debuff[Type] = false;

            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            // 视觉特效：被驯服的怪物冒爱心
            if (Main.rand.NextBool(20))
            {
                int d = Dust.NewDust(npc.position, npc.width, npc.height, Terraria.ID.DustID.HeartCrystal);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity.Y -= 1f;
            }
        }
    }
}