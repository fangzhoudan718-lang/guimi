using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using zhashi.Content.Configs;

namespace zhashi.Content.Systems
{
    public class BalanceSystem : ModSystem
    {
        // 核心方法：获取当前世界的战力倍率
        // 0.1 (开局) -> 1.0 (月总) -> 5.0+ (模组终局)
        public static float GetWorldTierMultiplier()
        {
            // 如果玩家关闭了动态平衡，直接返回配置的倍率
            if (!ModContent.GetInstance<LotMConfig>().DynamicProgression)
                return ModContent.GetInstance<LotMConfig>().GlobalPowerMultiplier;

            float tier = 0.1f; // 初始倍率

            // ==========================================
            // 1. 原版进度 (Vanilla) - 上限 1.0
            // ==========================================
            if (NPC.downedSlimeKing) tier += 0.05f;
            if (NPC.downedBoss1) tier += 0.05f; // 克眼
            if (NPC.downedBoss2) tier += 0.1f;  // 世吞/克脑
            if (NPC.downedBoss3) tier += 0.1f;  // 骷髅王
            if (Main.hardMode) tier += 0.2f;    // 肉山后质变
            if (NPC.downedMechBossAny) tier += 0.1f;
            if (NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3) tier += 0.1f;
            if (NPC.downedPlantBoss) tier += 0.1f; // 花后
            if (NPC.downedGolemBoss) tier += 0.05f;
            if (NPC.downedFishron) tier += 0.05f;
            if (NPC.downedMoonlord) tier += 0.2f; // 月后，此时约为 1.1

            // ==========================================
            // 2. 模组进度适配 (Modded) - 上限 突破天际
            // ==========================================

            // --- Calamity (灾厄) ---
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 灾厄肉后/花后补充
                if ((bool)calamity.Call("GetBossDowned", "cryogen")) tier += 0.05f;
                if ((bool)calamity.Call("GetBossDowned", "plaguebringergoliath")) tier += 0.05f;

                // 灾厄月后 (神长直)
                if ((bool)calamity.Call("GetBossDowned", "providence")) tier += 0.5f;   // 亵渎天神
                if ((bool)calamity.Call("GetBossDowned", "polterghast")) tier += 0.3f;  // 幽花
                if ((bool)calamity.Call("GetBossDowned", "devourerofgods")) tier += 0.8f; // 神吞 (质变)
                if ((bool)calamity.Call("GetBossDowned", "yharon")) tier += 1.0f;       // 丛林龙
                if ((bool)calamity.Call("GetBossDowned", "supremecalamitas")) tier += 1.5f; // 终灾
            }

            // --- Thorium (瑟银) ---
            if (ModLoader.TryGetMod("ThoriumMod", out Mod thorium))
            {
                if ((bool)thorium.Call("GetDownedBoss", "ThePrimordials")) tier += 0.8f; // 始生灾灵 (终局)
            }

            // --- Fargo's Souls (法狗) ---
            if (ModLoader.TryGetMod("FargowiltasSouls", out Mod fargo))
            {
                if ((bool)fargo.Call("DownedEridanus")) tier += 0.5f; // 宇宙勇士
                if ((bool)fargo.Call("DownedAbom")) tier += 1.0f;     // 憎恶
                if ((bool)fargo.Call("DownedMutant")) tier += 2.0f;   // 突变体 (极难)
            }

            // --- Mod of Redemption (救赎) ---
            if (ModLoader.TryGetMod("Redemption", out Mod redemption))
            {
                if ((bool)redemption.Call("DownedNebuleus")) tier += 1.0f; // 星云女神
            }

            // --- Spooky Mod (惊悚) ---
            if (ModLoader.TryGetMod("Spooky", out Mod spooky))
            {
                // Spooky 的 Boss 多在花后月前，补充中期强度
                if (NPC.downedPlantBoss) tier += 0.1f;
            }

            // 应用全局倍率设置
            tier *= ModContent.GetInstance<LotMConfig>().GlobalPowerMultiplier;

            return tier;
        }
    }
}