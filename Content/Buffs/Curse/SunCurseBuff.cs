using Terraria;
using Terraria.ModLoader;

namespace zhashi.Content.Buffs.Curse
{
    public class SunCurseBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true; // 这是一个负面状态
            Main.buffNoSave[Type] = true; // 退出不保存
            Main.buffNoTimeDisplay[Type] = true; // 不显示倒计时 (永久持续直到条件结束)
        }
    }
}