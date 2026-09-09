using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using zhashi; // 引用命名空间

namespace zhashi.Content.UI
{
    public class FavorabilityPlayer : ModPlayer
    {
        public Dictionary<int, int> favorScores = new Dictionary<int, int>();
        public Dictionary<int, bool> hasGiftedToday = new Dictionary<int, bool>();
        public Dictionary<int, bool> hasChattedToday = new Dictionary<int, bool>();
        public Dictionary<int, bool> hasHealedToday = new Dictionary<int, bool>();

        public int GetScore(int npcId)
        {
            if (favorScores.ContainsKey(npcId)) return favorScores[npcId];
            return 60;
        }

        public void ChangeScore(int npcId, int amount)
        {
            if (!favorScores.ContainsKey(npcId)) favorScores[npcId] = 60;
            favorScores[npcId] += amount;
            if (favorScores[npcId] > 100) favorScores[npcId] = 100;
            if (favorScores[npcId] < 0) favorScores[npcId] = 0;
            SyncFavorChange(npcId);
        }

        // --- 同步逻辑 ---
        public void SyncFavorChange(int npcId)
        {
            if (Main.netMode == Terraria.ID.NetmodeID.SinglePlayer) return;

            ModPacket packet = Mod.GetPacket();

            // ★★★ 修复点：直接使用 LotMNetMsg，去掉 zhashi. 前缀 ★★★
            packet.Write((byte)LotMNetMsg.SyncFavorability);

            packet.Write((byte)Player.whoAmI);
            packet.Write(npcId);
            packet.Write(favorScores[npcId]);
            packet.Send();
        }

        // --- 每日重置 ---
        public void ResetDailyFlags()
        {
            hasGiftedToday.Clear();
            hasChattedToday.Clear();
            hasHealedToday.Clear();
        }

        // --- 存读档 ---
        public override void SaveData(TagCompound tag)
        {
            var ids = new List<int>(favorScores.Keys);
            var vals = new List<int>(favorScores.Values);
            tag["Fav_IDs"] = ids;
            tag["Fav_Vals"] = vals;

            tag["Gift_IDs"] = new List<int>(hasGiftedToday.Keys);
            tag["Chat_IDs"] = new List<int>(hasChattedToday.Keys);
            tag["Heal_IDs"] = new List<int>(hasHealedToday.Keys);
        }

        public override void LoadData(TagCompound tag)
        {
            favorScores.Clear();
            hasGiftedToday.Clear();
            hasChattedToday.Clear();
            hasHealedToday.Clear();

            if (tag.ContainsKey("Fav_IDs"))
            {
                var ids = tag.GetList<int>("Fav_IDs");
                var vals = tag.GetList<int>("Fav_Vals");
                for (int i = 0; i < ids.Count; i++)
                {
                    if (i < vals.Count) favorScores[ids[i]] = vals[i];
                }
            }

            if (tag.ContainsKey("Gift_IDs"))
                foreach (int id in tag.GetList<int>("Gift_IDs")) hasGiftedToday[id] = true;

            if (tag.ContainsKey("Chat_IDs"))
                foreach (int id in tag.GetList<int>("Chat_IDs")) hasChattedToday[id] = true;

            if (tag.ContainsKey("Heal_IDs"))
                foreach (int id in tag.GetList<int>("Heal_IDs")) hasHealedToday[id] = true;
        }
    }
}