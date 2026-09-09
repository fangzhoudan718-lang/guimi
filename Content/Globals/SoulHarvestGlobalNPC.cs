using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Weapons;
using Microsoft.Xna.Framework;

namespace zhashi.Content.Globals
{
    public class SoulHarvestGlobalNPC : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {

            if (Main.netMode == NetmodeID.Server) return; // 仅客户端处理

            Player player = Main.LocalPlayer;

            // 检查玩家是否持有（或背包里有）蠕动的饥饿
            Item gloveItem = null;
            if (player.HeldItem.type == ModContent.ItemType<CreepingHunger>())
                gloveItem = player.HeldItem;
            else
            {
                foreach (var item in player.inventory)
                {
                    if (item.type == ModContent.ItemType<CreepingHunger>())
                    {
                        gloveItem = item;
                        break;
                    }
                }
            }

            if (gloveItem != null && gloveItem.ModItem is CreepingHunger glove)
            {
                // === 1. 喂食逻辑 ===
                // 只有“人形”或“Boss”或“高血量怪”才能满足它的胃口
                // 这里简单判定：如果是 Boss 或者生命值大于 50 的怪
                if (!glove.isFed && (npc.boss || npc.lifeMax > 200))
                {
                    glove.Feed();
                }

                // === 2. 收集灵魂逻辑 ===
                if (glove.IsSupportedNPC(npc.type))
                {
                    glove.AddSoul(npc.type);
                }
            }
        }
    }
}