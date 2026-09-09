using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using zhashi.Content;

namespace zhashi.Content.Systems
{
    // 1. 负责修改商店价格
    public class StealGlobalNPC : GlobalNPC
    {
        public override void ModifyActiveShop(NPC npc, string shopName, Item[] items)
        {
            Player player = Main.LocalPlayer;
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 只有当玩家开启了窃取模式，且是序列9及以上时生效
            if (modPlayer.stealMode && modPlayer.currentMarauderSequence <= 9)
            {
                foreach (Item item in items)
                {
                    if (item != null && !item.IsAir)
                    {
                        item.shopCustomPrice = 0; // 0元购
                    }
                }
            }
        }
    }

    // 2. 负责购买后的风险判定
    public class StealGlobalItem : GlobalItem
    {
        public override void OnCreated(Item item, ItemCreationContext context)
        {
            Player player = Main.LocalPlayer;
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            bool isBought = context is BuyItemCreationContext;

            // 只有是购买行为，且开启了窃取模式时才判定
            if (isBought && modPlayer.stealMode)
            {
                // === [新增逻辑] 恋人牌判定：必定成功 ===
                if (modPlayer.isLoversCardEquipped)
                {
                    // 1. 视觉反馈：星旋粒子 (象征扭曲规则/Bug)
                    for (int i = 0; i < 15; i++)
                    {
                        Dust d = Dust.NewDustPerfect(player.Center, DustID.Vortex, Main.rand.NextVector2Circular(2f, 2f), 0, default, 1.5f);
                        d.noGravity = true;
                    }

                    // 2. 文字提示 (紫色)
                    CombatText.NewText(player.getRect(), new Color(178, 102, 255), "命运欺诈!", true);

                    // 3. 直接返回，跳过后续的失败判定
                    return;
                }
                // ========================================

                // 原有的概率计算
                float catchChance = 0.5f - (9 - modPlayer.currentMarauderSequence) * 0.05f;
                if (catchChance < 0.05f) catchChance = 0.05f;

                if (Main.rand.NextFloat() < catchChance)
                {
                    // === 失败：被发现 ===
                    Main.NewText($"偷窃被发现了！(概率: {catchChance:P0})", 255, 0, 0);

                    modPlayer.stealAggroTimer = 3600;
                    modPlayer.stealMode = false;

                    // 停止对话并关闭界面
                    player.SetTalkNPC(-1);
                    Main.playerInventory = false;

                    player.Hurt(PlayerDeathReason.ByCustomReason(player.name + " 被愤怒的店主当场抓获！"), 50, 0);
                    SoundEngine.PlaySound(SoundID.Roar, player.position);
                }
                else
                {
                    // === 成功 ===
                    CombatText.NewText(player.getRect(), Color.Gold, "窃取成功!", true);
                }
            }
        }
    }
}