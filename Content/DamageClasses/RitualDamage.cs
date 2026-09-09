using Terraria;
using Terraria.ModLoader;

namespace zhashi.Content.DamageClasses
{
    public class RitualDamage : DamageClass
    {
        public override void SetStaticDefaults()
        {
            // 【修复】删除了 ClassName.SetDefault(...) 
            // 现在显示名称会自动使用你的 Localization 文件，或者默认为 "Ritual Damage"
        }

        public override StatInheritanceData GetModifierInheritance(DamageClass damageClass)
        {
            if (damageClass == Generic)
                return StatInheritanceData.Full;
            return StatInheritanceData.None;
        }

        public override bool GetEffectInheritance(DamageClass damageClass)
        {
            if (damageClass == Generic)
                return true;
            return false;
        }

        public override void SetDefaultStats(Player player)
        {
            player.GetCritChance(this) += 4;
        }
    }
}