using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace zhashi.Content.Pathways.BlackEmperor
{
    /// <summary>
    /// 称号进度：采到手里的东西。黑皇帝途径的「名义」要靠做成一件事来换，
    /// 这里负责把「采蘑菇」「钓鱼」「挖土」三件事记在人头上。
    /// </summary>
    public class BlackEmperorTitleProgressItem : GlobalItem
    {
        public override bool OnPickup(Item item, Player player)
        {
            // 拾取发生在玩家那一侧：联机时由本人记账再上报，单机与主机就地记账。
            if (Main.netMode == NetmodeID.MultiplayerClient && player.whoAmI != Main.myPlayer) return true;

            BlackEmperorPlayer be = player.GetModPlayer<BlackEmperorPlayer>();
            if (!be.TitleSystemActive) return true;

            switch (item.type)
            {
                case ItemID.Mushroom:
                case ItemID.GlowingMushroom:
                    be.CountMushroomCollected(item.stack);
                    break;
                default:
                    if (ItemID.Sets.IsFishingCrate[item.type]) be.CountFishCaught(3 * item.stack);
                    else if (IsFishName(item.type)) be.CountFishCaught(item.stack);
                    break;
            }
            return true;
        }

        /// <summary>
        /// 原版没有「这是鱼」的标记，所以退一步看名字：中文名里带鱼部的大多就是鱼。
        /// 宝匣另算（一次顶三条），外语客户端也不会完全卡住。
        /// </summary>
        private static bool IsFishName(int type)
        {
            string name = Lang.GetItemNameValue(type);
            if (string.IsNullOrEmpty(name)) return false;
            return name.Contains("鱼") || name.Contains("鲤") || name.Contains("鲷") ||
                   name.Contains("鲈") || name.Contains("鳟") || name.Contains("鲑") ||
                   name.Contains("Bass") || name.Contains("Trout") || name.Contains("Salmon");
        }
    }

    /// <summary>称号进度：挖穿的土石。</summary>
    public class BlackEmperorTitleProgressTile : GlobalTile
    {
        public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || fail || effectOnly) return;

            // 这个钩子拿不到「谁挖的」，所以退一步：离这块土最近的那个人就是动手的人。
            Vector2 tileCenter = new Vector2(i * 16f + 8f, j * 16f + 8f);
            Player miner = null;
            float best = 40f * 16f;
            foreach (Player candidate in Main.ActivePlayers)
            {
                if (candidate.dead || candidate.ghost) continue;
                float distance = Vector2.Distance(candidate.Center, tileCenter);
                if (distance < best) { best = distance; miner = candidate; }
            }
            if (miner == null) return;

            miner.GetModPlayer<BlackEmperorPlayer>().CountTileMined();
        }
    }
}
