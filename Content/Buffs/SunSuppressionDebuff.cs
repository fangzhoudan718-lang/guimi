using Terraria;
using Terraria.ModLoader;
using zhashi.Content;
using System;

namespace zhashi.Content.Buffs
{
    public class SunSuppressionDebuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true; // 允许 PVP
            Main.buffNoSave[Type] = false; // 退出重进还在
            Terraria.ID.BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

            // 逻辑：在当前基础上 +1 (变弱)，但不能超过 10 (凡人)
            // 因为 ResetEffects 每帧都会把它们重置为 Base，所以这里只是暂时 +1

            if (modPlayer.currentSequence < 10) modPlayer.currentSequence = Math.Min(10, modPlayer.currentSequence + 1);
            if (modPlayer.currentHunterSequence < 10) modPlayer.currentHunterSequence = Math.Min(10, modPlayer.currentHunterSequence + 1);
            if (modPlayer.currentMoonSequence < 10) modPlayer.currentMoonSequence = Math.Min(10, modPlayer.currentMoonSequence + 1);
            if (modPlayer.currentFoolSequence < 10) modPlayer.currentFoolSequence = Math.Min(10, modPlayer.currentFoolSequence + 1);
            if (modPlayer.currentMarauderSequence < 10) modPlayer.currentMarauderSequence = Math.Min(10, modPlayer.currentMarauderSequence + 1);
            if (modPlayer.currentSunSequence < 10) modPlayer.currentSunSequence = Math.Min(10, modPlayer.currentSunSequence + 1);
        }
    }
}