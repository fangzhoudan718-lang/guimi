using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace zhashi.Content.Configs
{
    public class LotMConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        // 使用 $ 符号强制链接到 HJSON 中的 Headers.BaseSettings
        [Header("$Mods.zhashi.Configs.LotMConfig.Headers.BaseSettings")]

        [DefaultValue(true)]
        public bool DynamicProgression;

        [Range(0.1f, 10f)]
        [Increment(0.1f)]
        [DefaultValue(1.0f)]
        public float GlobalPowerMultiplier;

        [Header("$Mods.zhashi.Configs.LotMConfig.Headers.ChallengeSettings")]

        [DefaultValue(false)]
        public bool EnableSanitySystem;

        [DefaultValue(false)]
        public bool EnableWorldRestriction;

        [DefaultValue(false)]
        public bool EnableDivineCurse;

        [Header("$Mods.zhashi.Configs.LotMConfig.Headers.OtherSettings")]

        [DefaultValue(false)]
        public bool NerfDivineAbilities;


        [Header("平衡性设置")] // 添加一个标题

        [Label("灾厄适配模式")]
        [Tooltip("开启后：\n1. 玩家造成的伤害降低 60%，防御降低 40%\n2. 只有击败特定 Boss 后才能服用对应序列的魔药")]
        [DefaultValue(false)]
        public bool CalamityAdaptationMode;
    }
}
