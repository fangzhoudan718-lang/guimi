using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Globals
{
    /// <summary>
    /// 命运启示 - 百分百掉落:
    /// 任何被启示玩家击杀的敌人, 强制掉落它所有"可能"掉落物.
    /// 实现: 扫描 NPC 的所有 IItemDropRule, 用反射提取 itemId 字段, 然后直接 NewItem 强制生成.
    /// </summary>
    public class RevelationFullDropGlobalNPC : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (npc.lastInteraction < 0 || npc.lastInteraction >= Main.maxPlayers) return;

            Player killer = Main.player[npc.lastInteraction];
            if (!killer.active || killer.dead) return;

            var mp = killer.GetModPlayer<LotMPlayer>();
            if (mp.revelationActiveTimer <= 0) return;

            try
            {
                // 收集该 NPC 的所有可能掉落 itemID
                HashSet<int> itemIds = new HashSet<int>();
                var rules = Main.ItemDropsDB.GetRulesForNPCID(npc.netID, false);
                foreach (var rule in rules)
                {
                    CollectItemIDs(rule, itemIds);
                }

                // 直接生成所有物品 (每种 1 个)
                int dropCount = 0;
                foreach (int id in itemIds)
                {
                    if (id <= 0 || id >= ItemLoader.ItemCount) continue;
                    Item.NewItem(npc.GetSource_Death("RevelationFullDrop"),
                        npc.position, npc.Size, id, 1, false, 0, false, false);
                    dropCount++;
                }

                if (dropCount > 0)
                {
                    // 视觉反馈
                    for (int k = 0; k < 30; k++)
                    {
                        Dust d = Dust.NewDustPerfect(npc.Center, DustID.GoldCoin,
                            Main.rand.NextVector2Circular(6, 6), 0, default, 1.6f);
                        d.noGravity = true;
                    }
                    CombatText.NewText(npc.getRect(), new Color(255, 215, 100), $"命运慷慨! ({dropCount}件)", true);
                }
            }
            catch { /* 任何异常都不应阻塞正常游戏流程 */ }
        }

        /// <summary>
        /// 递归扫描一个 DropRule, 用反射提取里面所有的 itemId 字段.
        /// 覆盖 vanilla 常见 rule 类型: CommonDrop, OneFromOptionsDropRule, ItemDropWithConditionRule 等.
        /// </summary>
        private static void CollectItemIDs(IItemDropRule rule, HashSet<int> bag)
        {
            if (rule == null) return;

            try
            {
                var type = rule.GetType();
                // 寻找 itemId / itemIds 字段
                foreach (var field in type.GetFields(System.Reflection.BindingFlags.Public
                                                   | System.Reflection.BindingFlags.NonPublic
                                                   | System.Reflection.BindingFlags.Instance))
                {
                    object val = field.GetValue(rule);
                    if (val == null) continue;

                    if (field.FieldType == typeof(int))
                    {
                        // 命名约定: 通常叫 itemId, dropIds 等
                        string nm = field.Name.ToLowerInvariant();
                        if (nm.Contains("itemid") || nm.Contains("itemtype") || nm.Contains("dropid"))
                        {
                            int v = (int)val;
                            if (v > 0 && v < ItemLoader.ItemCount) bag.Add(v);
                        }
                    }
                    else if (val is int[] arr)
                    {
                        foreach (int v in arr)
                            if (v > 0 && v < ItemLoader.ItemCount) bag.Add(v);
                    }
                }
            }
            catch { }

            // 递归子规则
            if (rule.ChainedRules != null)
            {
                foreach (var chain in rule.ChainedRules)
                {
                    if (chain != null && chain.RuleToChain != null)
                        CollectItemIDs(chain.RuleToChain, bag);
                }
            }
        }
    }
}

