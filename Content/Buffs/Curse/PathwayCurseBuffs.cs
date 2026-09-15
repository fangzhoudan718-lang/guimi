using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Buffs.Curse
{
    /// <summary>
    /// 门途径的神性副作用图标：空间在排斥静止不动的你。
    /// 图标借用原版，和门途径其它 buff 一样不额外占贴图。
    /// </summary>
    public class DoorCurseBuff : ModBuff
    {
        public override string Texture => $"Terraria/Images/Buff_{BuffID.Blackout}";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }
    }

    /// <summary>
    /// 黑皇帝途径的神性副作用图标：没有秩序托着的时候，身体会松掉。
    /// </summary>
    public class BlackEmperorCurseBuff : ModBuff
    {
        public override string Texture => $"Terraria/Images/Buff_{BuffID.WitheredArmor}";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }
    }
}
