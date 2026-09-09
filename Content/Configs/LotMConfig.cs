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

        [Header("超凡生物模式")]
        [Label("生物超凡化")]
        [Tooltip("开启后:全游戏所有生成的生物有几率成为某途径某序列的超凡者\n序列越低=几率越小=强度越高\n序列9=10%, 序列8=5%, 序列7=1%, ..., 序列0=0.0001%\n超凡生物拥有强化数值+自动技能+Boss血条+丰厚掉落\n[序列0生物可以秒杀玩家,慎开]")]
        [DefaultValue(false)]
        public bool EnableBeyonderCreatures;

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