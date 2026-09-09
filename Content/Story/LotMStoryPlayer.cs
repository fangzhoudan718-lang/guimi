using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System.Collections.Generic;
using System.IO;
using zhashi.Content.Items;
using zhashi.Content.Items.Story;
using zhashi.Content.Items.Weapons;

namespace zhashi.Content
{
    public class LotMStoryPlayer : ModPlayer
    {
        public int StoryStage = 0;
        public int DaysSinceJoined = 0;
        public bool DayCheck = false;

        public bool HasDailyQuest = false;
        public bool QuestCompletedToday = false;
        public int QuestType = 0;
        public int QuestTargetID = 0;
        public int QuestRequiredAmount = 0;
        public int QuestCurrentAmount = 0;
        public double LastResetTime = 0;

        public bool HasReceivedStarterPotion = false;

        public override IEnumerable<Item> AddStartingItems(bool mediumCoreDeath)
        {
            List<Item> items = new List<Item>();
            items.Add(new Item(ModContent.ItemType<KleinsDiary>()));
            items.Add(new Item(ModContent.ItemType<GoldenGun>()));
            items.Add(new Item(ItemID.MusketBall, 10));
            items.Add(new Item(ModContent.ItemType<RoselleDiary>()));
            return items;
        }

        public override void SaveData(TagCompound tag)
        {
            tag["StoryStage"] = StoryStage;
            tag["DaysSinceJoined"] = DaysSinceJoined;
            tag["HasDailyQuest"] = HasDailyQuest;
            tag["QuestCompletedToday"] = QuestCompletedToday;
            tag["QuestType"] = QuestType;
            tag["QuestTargetID"] = QuestTargetID;
            tag["QuestRequiredAmount"] = QuestRequiredAmount;
            tag["QuestCurrentAmount"] = QuestCurrentAmount;
            tag["LastResetTime"] = LastResetTime;
            tag["HasReceivedStarterPotion"] = HasReceivedStarterPotion;
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.ContainsKey("StoryStage")) StoryStage = tag.GetInt("StoryStage");
            if (tag.ContainsKey("DaysSinceJoined")) DaysSinceJoined = tag.GetInt("DaysSinceJoined");
            if (tag.ContainsKey("HasDailyQuest")) HasDailyQuest = tag.GetBool("HasDailyQuest");
            if (tag.ContainsKey("QuestCompletedToday")) QuestCompletedToday = tag.GetBool("QuestCompletedToday");
            if (tag.ContainsKey("QuestType")) QuestType = tag.GetInt("QuestType");
            if (tag.ContainsKey("QuestTargetID")) QuestTargetID = tag.GetInt("QuestTargetID");
            if (tag.ContainsKey("QuestRequiredAmount")) QuestRequiredAmount = tag.GetInt("QuestRequiredAmount");
            if (tag.ContainsKey("QuestCurrentAmount")) QuestCurrentAmount = tag.GetInt("QuestCurrentAmount");
            if (tag.ContainsKey("LastResetTime")) LastResetTime = tag.GetDouble("LastResetTime");
            if (tag.ContainsKey("HasReceivedStarterPotion")) HasReceivedStarterPotion = tag.GetBool("HasReceivedStarterPotion");
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            ModPacket packet = Mod.GetPacket();
            // 直接使用 LotMNetMsg，不需要前缀
            packet.Write((byte)LotMNetMsg.StoryDataSync);
            packet.Write((byte)Player.whoAmI);

            packet.Write(StoryStage);
            packet.Write(DaysSinceJoined);
            packet.Write(HasDailyQuest);
            packet.Write(QuestCompletedToday);
            packet.Write(QuestType);
            packet.Write(QuestTargetID);
            packet.Write(QuestRequiredAmount);
            packet.Write(QuestCurrentAmount);
            packet.Write(HasReceivedStarterPotion);

            packet.Send(toWho, fromWho);
        }

        // ★★★ 修复点 1：直接强制转换，而不是调用 GetModPlayer ★★★
        public override void SendClientChanges(ModPlayer clientPlayer)
        {

            LotMStoryPlayer oldPlayer = (LotMStoryPlayer)clientPlayer;

            if (oldPlayer.StoryStage != StoryStage ||
                oldPlayer.DaysSinceJoined != DaysSinceJoined ||
                oldPlayer.HasDailyQuest != HasDailyQuest ||
                oldPlayer.QuestCompletedToday != QuestCompletedToday ||
                oldPlayer.QuestCurrentAmount != QuestCurrentAmount ||
                oldPlayer.QuestType != QuestType ||
                oldPlayer.HasReceivedStarterPotion != HasReceivedStarterPotion)
            {
                SyncPlayer(toWho: -1, fromWho: Main.myPlayer, newPlayer: false);
            }
        }

        // ★★★ 修复点 2：同上 ★★★
        public override void CopyClientState(ModPlayer targetCopy)
        {
            LotMStoryPlayer clone = (LotMStoryPlayer)targetCopy;
            clone.StoryStage = StoryStage;
            clone.DaysSinceJoined = DaysSinceJoined;
            clone.HasDailyQuest = HasDailyQuest;
            clone.QuestCompletedToday = QuestCompletedToday;
            clone.QuestType = QuestType;
            clone.QuestTargetID = QuestTargetID;
            clone.QuestRequiredAmount = QuestRequiredAmount;
            clone.QuestCurrentAmount = QuestCurrentAmount;
            clone.HasReceivedStarterPotion = HasReceivedStarterPotion;
        }

        public override void PostUpdate()
        {
            if (Player.whoAmI == Main.myPlayer)
            {
                if (Main.dayTime && Main.time < 1000 && LastResetTime != Main.time)
                {
                    if (System.Math.Abs(Main.time - LastResetTime) > 2000)
                    {
                        QuestCompletedToday = false;
                        LastResetTime = Main.time;
                    }
                }

                if (StoryStage == 2)
                {
                    if (Main.dayTime && !DayCheck)
                    {
                        DayCheck = true;
                        DaysSinceJoined++;

                        if (DaysSinceJoined >= 3)
                        {
                            StoryStage = 3;
                            Main.NewText("邓恩·史密斯似乎有紧急的事情找你...", 255, 50, 50);
                        }
                    }

                    if (!Main.dayTime)
                    {
                        DayCheck = false;
                    }
                }
            }
        }

        public void GenerateNewQuest()
        {
            HasDailyQuest = true;
            QuestCurrentAmount = 0;

            if (Main.rand.NextBool())
            {
                QuestType = 1;
                QuestRequiredAmount = Main.rand.Next(5, 11);
                int[] mobs = { NPCID.Zombie, NPCID.DemonEye, NPCID.GreenSlime, NPCID.BlueSlime };
                QuestTargetID = mobs[Main.rand.Next(mobs.Length)];
            }
            else
            {
                QuestType = 2;
                QuestRequiredAmount = Main.rand.Next(3, 8);
                int[] items = { ItemID.Gel, ItemID.Lens, ItemID.Mushroom, ItemID.Wood };
                QuestTargetID = items[Main.rand.Next(items.Length)];
            }
        }
    }
}