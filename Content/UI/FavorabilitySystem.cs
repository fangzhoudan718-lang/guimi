using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System.Collections.Generic;
using System;
using System.IO;
using Terraria.ID;

namespace zhashi.Content.UI
{
    public class FavorabilitySystem : ModSystem
    {
        // ==========================================
        // ★ 1. 数据存储字典 ★
        // ==========================================
        public static Dictionary<int, int> favorabilityScores = new Dictionary<int, int>();
        public static Dictionary<int, bool> giftedToday = new Dictionary<int, bool>();
        public static Dictionary<int, bool> chatRewardToday = new Dictionary<int, bool>();
        public static Dictionary<int, bool> healedRecently = new Dictionary<int, bool>();

        // ==========================================
        // ★ 2. 存读档系统 ★
        // ==========================================
        public override void SaveWorldData(TagCompound tag)
        {
            tag["favorKeys"] = new List<int>(favorabilityScores.Keys);
            tag["favorValues"] = new List<int>(favorabilityScores.Values);
        }

        public override void LoadWorldData(TagCompound tag)
        {
            favorabilityScores.Clear();
            if (tag.ContainsKey("favorKeys") && tag.ContainsKey("favorValues"))
            {
                var keys = tag.GetList<int>("favorKeys");
                var values = tag.GetList<int>("favorValues");
                for (int i = 0; i < keys.Count; i++)
                {
                    favorabilityScores[keys[i]] = values[i];
                }
            }
        }

        // ==========================================
        // ★ 3. 联机同步系统 ★
        // ==========================================

        // 进服全量同步
        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(favorabilityScores.Count);
            foreach (var kvp in favorabilityScores)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }
        }

        public override void NetReceive(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            favorabilityScores.Clear();
            for (int i = 0; i < count; i++)
            {
                int key = reader.ReadInt32();
                int val = reader.ReadInt32();
                favorabilityScores[key] = val;
            }
        }

        // 发送好感度变更包 (客户端 -> 服务器)
        public static void SendFavorabilityChangePacket(int npcID, int newValue)
        {
            ModPacket packet = ModContent.GetInstance<zhashi>().GetPacket();

            // ★★★ 核心修复：使用统一的枚举 ID，防止冲突 ★★★
            packet.Write((byte)LotMNetMsg.SyncFavorability); // 去掉 zhashi.

            // 数据内容：NPC ID + 新分数
            packet.Write(npcID);
            packet.Write(newValue);
            packet.Send();
        }

        // ★★★ 注意：原来的 HandlePacket 方法已被删除，逻辑移至 zhashi.cs ★★★

        // ==========================================
        // ★ 4. 每日重置逻辑 ★
        // ==========================================
        public override void PostUpdateWorld()
        {
            if (Main.dayTime && Main.time == 0)
            {
                giftedToday.Clear();
                chatRewardToday.Clear();
                healedRecently.Clear();
            }
        }

        // ==========================================
        // ★ 5. 公共操作方法 (API) ★
        // ==========================================
        public static int GetFavorability(int npcType)
        {
            if (favorabilityScores.ContainsKey(npcType)) return favorabilityScores[npcType];
            return 0;
        }

        public static void IncreaseFavorability(int npcType, int amount)
        {
            if (!favorabilityScores.ContainsKey(npcType)) favorabilityScores[npcType] = 0;
            favorabilityScores[npcType] += amount;
            favorabilityScores[npcType] = Math.Clamp(favorabilityScores[npcType], 0, 100);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                SendFavorabilityChangePacket(npcType, favorabilityScores[npcType]);
            }
        }

        public static void SetFavorability(int npcType, int value)
        {
            if (!favorabilityScores.ContainsKey(npcType)) favorabilityScores[npcType] = 0;
            favorabilityScores[npcType] = Math.Clamp(value, 0, 100);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                SendFavorabilityChangePacket(npcType, favorabilityScores[npcType]);
            }
        }

        // 其他辅助方法
        public static void RecordInteraction(int npcType) { }
        public static bool CanGiftToday(int npcType) { return !giftedToday.ContainsKey(npcType) || !giftedToday[npcType]; }
        public static void RecordGift(int npcType) => giftedToday[npcType] = true;
        public static bool CanChatRewardToday(int npcType) { return !chatRewardToday.ContainsKey(npcType) || !chatRewardToday[npcType]; }
        public static void RecordChatReward(int npcType) => chatRewardToday[npcType] = true;
        public static void RecordHeal(int npcType) => healedRecently[npcType] = true;
        public static bool HasHealedRecently(int npcType) => healedRecently.ContainsKey(npcType) && healedRecently[npcType];
    }
}