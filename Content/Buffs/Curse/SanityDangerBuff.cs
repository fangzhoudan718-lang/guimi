using Terraria;
using Terraria.ModLoader;

namespace zhashi.Content.Buffs.Curse
{
    public class SanityDangerBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }
    }
}