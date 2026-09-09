using Terraria;
using Terraria.ModLoader;

namespace zhashi.Content.Buffs.Curse
{
    public class AfflictionCurseBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true; // 这是个负面效果
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            // DisplayName.SetDefault("诅咒病");
            // Description.SetDefault("你的身体正在由内而外地腐烂...");
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            // 1. 降低防御 (身体腐烂变软)
            npc.defense = (int)(npc.defense * 0.8f);

            // 2. 持续扣血 (类似毒，但更狠)
            if (npc.lifeRegen > 0) npc.lifeRegen = 0;
            npc.lifeRegen -= 60; // 每秒扣血量约为这个数值的一半

            // 3. 视觉特效：黑色病气
            if (Main.rand.NextBool(5))
            {
                Dust d = Dust.NewDustDirect(npc.position, npc.width, npc.height, 14, 0, 0, 100, default, 1.5f); // 14是腐化粒子
                d.noGravity = true;
                d.velocity *= 0.5f;
            }
        }
    }
}