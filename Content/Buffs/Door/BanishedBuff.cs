using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Buffs.Door
{
    public class BanishedBuff : ModBuff
    {
        public override string Texture => $"Terraria/Images/Buff_{BuffID.ChaosState}";
        public override void SetStaticDefaults() => Main.debuff[Type] = true;
    }

    public class SpatialPrisonBuff : ModBuff
    {
        public override string Texture => $"Terraria/Images/Buff_{BuffID.Frozen}";
        public override void SetStaticDefaults() => Main.debuff[Type] = true;
    }

    public class MiniaturizedBuff : ModBuff
    {
        public override string Texture => $"Terraria/Images/Buff_{BuffID.Slow}";
        public override void SetStaticDefaults() => Main.debuff[Type] = true;
    }

    public class TimeSpaceMazeBuff : ModBuff
    {
        public override string Texture => $"Terraria/Images/Buff_{BuffID.Confused}";
        public override void SetStaticDefaults() => Main.debuff[Type] = true;
    }
}
