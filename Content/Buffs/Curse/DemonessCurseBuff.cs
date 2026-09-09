using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace zhashi.Content.Buffs.Curse
{
    public class DemonessCurseBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // 设定为减益效果 (红框)
            Main.debuff[Type] = true;
            // 退出游戏/死亡后不保存
            Main.buffNoSave[Type] = true;
            // 不显示持续时间 (因为是常驻被动，每帧刷新)
            Main.buffNoTimeDisplay[Type] = true;
            // 不可被护士移除 (因为它源自途径本身)
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;

            // 1.4.4 版本推荐在 .hjson 文件中设置名字和描述
            // DisplayName.SetDefault("原初的诅咒");
            // Description.SetDefault("魔女的宿命：痛苦与欢愉交织。\n防御力降低20%，受到的伤害增加10%，生命回复减少。\n身处火焰中时攻击力大幅降低。");
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // 提示：
            // 具体的数值削弱逻辑 (防御降低、易燃等) 
            // 已经在 LotMPlayer.cs 的 ResetEffects 方法中编写了 (见你上一段代码)。
            // 所以这里只需要留空，或者仅做一些纯视觉效果即可。

            // 这种设计是为了方便在 Player 类中统一管理所有途径的逻辑。
        }
    }
}