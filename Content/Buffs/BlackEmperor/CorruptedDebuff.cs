using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Pathways.BlackEmperor;

namespace zhashi.Content.Buffs.BlackEmperor
{
    /// <summary>
    /// 序列六「腐化男爵」的腐蚀光环留在敌人身上的「阴暗」。
    ///
    /// 层数并不写在这条 buff 的时间里，而是记在 <see cref="BlackEmperorGlobalNPC"/> 上；
    /// 这条 buff 只是层数的一个可见影子——只要身上还有层数，图标就不会掉。
    /// 沿用愚者途径的做法复用原版图标，不额外占一张贴图。
    /// </summary>
    public class CorruptedDebuff : ModBuff
    {
        public override string Texture => $"Terraria/Images/Buff_{BuffID.ShadowFlame}";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            int stacks = npc.GetGlobalNPC<BlackEmperorGlobalNPC>().corruptionStacks;
            if (stacks <= 0) return;

            // 心里长了阴影，身体也跟着松掉：防御随层数一点点垮。
            npc.defense = Math.Max(0, npc.defense - 2 * stacks);

            // 视觉只留一点点暗色微尘，不做光污染。
            if (Main.rand.NextBool(18))
            {
                Dust dust = Dust.NewDustDirect(npc.position, npc.width, npc.height,
                    DustID.PurpleCrystalShard, 0f, 0f, 130, default, 0.85f);
                dust.noGravity = true;
                dust.velocity *= 0.35f;
            }
        }
    }
}
