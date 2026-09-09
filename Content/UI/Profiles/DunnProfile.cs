using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Localization;
using System.Collections.Generic;
using zhashi.Content;
using zhashi.Content.NPCs.Town;

namespace zhashi.Content.UI.Profiles
{
    public class DunnProfile : GalgameProfile
    {
        public override string HeadTexture => "zhashi/Content/UI/Portraits/Dunn_Head";
        public override string StandingTexture => "zhashi/Content/UI/Portraits/Dunn_Standing";

        // 立绘位置微调
        public override float OffsetX => 0f;
        public override float OffsetY => 20f;

        // ==========================================================
        // 获取对话文本 (GetDialogue)
        // ==========================================================
        public override string GetDialogue(NPC npc)
        {
            var story = Main.LocalPlayer.GetModPlayer<LotMStoryPlayer>();
            Player player = Main.LocalPlayer;

            // 1. 优先检测：任务是否已满足完成条件
            // 实时检测，确保文本和按钮状态完全同步
            if (story.HasDailyQuest)
            {
                bool canComplete = false;
                if (story.QuestType == 1 && story.QuestCurrentAmount >= story.QuestRequiredAmount) canComplete = true;
                if (story.QuestType == 2 && player.CountItem(story.QuestTargetID) >= story.QuestRequiredAmount) canComplete = true;

                if (canComplete)
                {
                    return "干得漂亮。这种处理效率正是值夜者所需要的。\n这是你的报酬，拿去买点咖啡或者更实用的东西吧。";
                }
            }

            // 2. 任务进行中 (未完成)
            if (story.HasDailyQuest)
            {
                string targetName = "";
                if (story.QuestType == 1) targetName = Lang.GetNPCNameValue(story.QuestTargetID);
                else targetName = Lang.GetItemNameValue(story.QuestTargetID);

                return $"任务进行得怎么样了？\n我们需要处理掉 {story.QuestRequiredAmount} 个 {targetName}。\n这不仅是为了维护治安，也是为了防止灵性污染扩散。";
            }

            // 3. 今天任务已做完
            if (story.QuestCompletedToday)
            {
                return "今天的巡查任务已经结束了，你可以休息一下，或者去练习一下非凡能力。\n不要给自己太大压力，时刻保持理智。";
            }

            // ----------------------------------------------------
            // 剧情相关对话
            // ----------------------------------------------------

            // --- 阶段 0: 调查期 ---
            if (story.StoryStage == 0)
            {
                List<string> chats = new List<string>
                {
                    "我是邓恩·史密斯，正在调查廷根市的一起自杀案。\n线索指向了克莱恩·莫雷蒂...虽然你看起来很像他，但既然你没有失控的迹象，我会暂时保持观察。",
                    "不用紧张，我们是阿霍瓦郡警察厅的特殊行动部...好吧，这只是对外的说法。\n实际上，我们是黑夜女神教会的‘值夜者’。",
                    "你认识韦尔奇或者是娜娅吗？很遗憾，他们因为接触了某些危险的物品而不幸离世了。\n这就是神秘世界的残酷之处。",
                    "如果你感觉到耳边有奇怪的低语，或者看到不存在的阴影，请立刻告诉我。\n那是失控的前兆。"
                };
                return chats[Main.rand.Next(chats.Count)];
            }

            // --- 阶段 1: 邀请期 ---
            if (story.StoryStage == 1)
            {
                return "我看过你的战斗了，那种力量...你确实有成为非凡者的潜质。\n怎么样，有没有兴趣加入我们值夜者？\n我们需要你这样的人才来对抗隐秘的威胁，而且我们会提供相对安全的晋升途径。";
            }

            // --- 阶段 2: 队员期 (加入后) ---
            if (story.StoryStage == 2)
            {
                List<string> chats = new List<string>
                {
                    "最近廷根市的灵性波动有些异常，如果你准备好了，可以向我申请每日的巡查委托。", // 引导任务
                    "去和老尼尔聊聊吧，他很快就会搬过来。\n虽然他有时候为了报销单发愁，但在神秘学知识上，他是很好的导师。",
                    "你的薪水是周薪3镑，这在廷根市算是不错的收入了。\n记得每个周末可以轮休，但如果有紧急任务，必须随时待命。",
                    "如果你需要领取子弹或者申请经费，记得对罗珊客气一点。\n文职人员也是我们重要的伙伴。",
                    "这里有很好的咖啡豆，要来一杯吗？\n...哦，抱歉，我忘记磨了，这是速溶的。",
                    "关于瑞尔·比伯的下落，我们查到了一些线索。\n他带着安提格努斯家族的笔记躲到了码头区，一旦确认位置，我会通知你。",
                    "记住，我们是守护者，也是一群时刻对抗着疯狂的可怜虫。"
                };
                return chats[Main.rand.Next(chats.Count)];
            }

            // --- 阶段 3: 任务触发 ---
            if (story.StoryStage == 3)
            {
                return "紧急情况！我们锁定了瑞尔·比伯的位置。\n他携带的安提格努斯笔记已经让他失控了，周围出现了巨大的灵性波动。\n必须在他造成更大破坏前解决他！准备好战斗了吗？";
            }

            // --- 阶段 4及以后: 任务结束 ---
            if (story.StoryStage > 3)
            {
                return "干得好，克莱恩。\n瑞尔·比伯的事情已经解决了，你可以休息一段时间了。\n不过，只要非凡特性还存在，我们的工作就永远不会结束。";
            }

            return "愿女神庇佑你。";
        }

        // ==========================================================
        // 设置按钮 (SetupButtons)
        // ==========================================================
        public override void SetupButtons(NPC npc, GalgameDialogueUI ui)
        {
            var story = Main.LocalPlayer.GetModPlayer<LotMStoryPlayer>();
            Player player = Main.LocalPlayer;

            // ----------------------------------------------------
            // 每日委托系统 (StoryStage >= 2)
            // ----------------------------------------------------
            if (story.StoryStage >= 2)
            {
                if (story.HasDailyQuest)
                {
                    // 实时判断是否可提交
                    bool canComplete = false;
                    if (story.QuestType == 1 && story.QuestCurrentAmount >= story.QuestRequiredAmount) canComplete = true;
                    if (story.QuestType == 2 && player.CountItem(story.QuestTargetID) >= story.QuestRequiredAmount) canComplete = true;

                    // 分支 A: 可以提交 (不再显示进度按钮，直接显示提交)
                    if (canComplete)
                    {
                        ui.AddButton("【提交委托】", () => {
                            // 扣除物品逻辑 (搜集任务)
                            if (story.QuestType == 2)
                            {
                                int needed = story.QuestRequiredAmount;
                                // 从后往前遍历背包，这是修改背包最安全的方法
                                for (int i = 57; i >= 0; i--)
                                {
                                    if (needed <= 0) break;
                                    Item item = player.inventory[i];
                                    if (item.type == story.QuestTargetID)
                                    {
                                        if (item.stack > needed)
                                        {
                                            item.stack -= needed;
                                            needed = 0;
                                        }
                                        else
                                        {
                                            needed -= item.stack;
                                            item.TurnToAir();
                                        }
                                    }
                                }
                            }

                            // 发放奖励
                            player.QuickSpawnItem(npc.GetSource_GiftOrReward(), ItemID.SilverCoin, Main.rand.Next(20, 50)); // 银币
                            if (Main.rand.NextBool(3)) // 33% 概率给药水
                                player.QuickSpawnItem(npc.GetSource_GiftOrReward(), ItemID.HealingPotion, 2);

                            // 结算状态
                            story.HasDailyQuest = false;
                            story.QuestCompletedToday = true;
                            Main.NewText("任务完成！获得奖励。", 255, 215, 0);

                            // 刷新 UI
                            ui.SetDialogueText("做得好。这是你的经费报销。\n休息一下吧，明天还有新的工作。");
                            ui.ClearButtons();
                            SetupButtons(npc, ui); // 递归调用以刷新按钮
                        });
                    }
                    // 分支 B: 不能提交 (显示进度提示和放弃按钮)
                    else
                    {
                        string targetName = story.QuestType == 1 ? Lang.GetNPCNameValue(story.QuestTargetID) : Lang.GetItemNameValue(story.QuestTargetID);
                        string status = story.QuestType == 1
                            ? $"进度: {story.QuestCurrentAmount}/{story.QuestRequiredAmount}"
                            : $"背包持有: {player.CountItem(story.QuestTargetID)}/{story.QuestRequiredAmount}";

                        // 这是一个不可点击的提示按钮，或者点击刷新文本
                        ui.AddButton($"进行中: {targetName} ({status})", () => {
                            ui.SetDialogueText($"你需要{(story.QuestType == 1 ? "消灭" : "上交")} {story.QuestRequiredAmount} 个 {targetName}。\n目前的进度是: {status}。\n(若已凑齐请重新对话刷新)");
                        });

                        ui.AddButton("放弃任务", () => {
                            story.HasDailyQuest = false;
                            story.QuestCompletedToday = true; // 放弃也算今日已做
                            ui.SetDialogueText("好吧，也许这个任务对现在的你来说太勉强了。\n休息一下，明天再来领新的任务吧。");
                            ui.ClearButtons();
                            SetupButtons(npc, ui);
                        });
                    }
                }
                // 情况 C: 没有任务 & 今天还没做过
                else if (!story.QuestCompletedToday)
                {
                    ui.AddButton("【申请巡查委托】", () => {
                        story.GenerateNewQuest();

                        string tName = story.QuestType == 1 ? Lang.GetNPCNameValue(story.QuestTargetID) : Lang.GetItemNameValue(story.QuestTargetID);
                        string action = story.QuestType == 1 ? "消灭" : "收集";

                        ui.SetDialogueText($"好的，这里有一份积压的委托。\n我们需要你去{action} {story.QuestRequiredAmount} 个 {tName}。\n请务必小心。");

                        ui.ClearButtons();
                        SetupButtons(npc, ui);
                    });
                }
            }

            // ----------------------------------------------------
            // 剧情按钮
            // ----------------------------------------------------

            // 阶段 1：加入值夜者
            if (story.StoryStage == 1)
            {
                ui.AddButton("加入值夜者", () => {
                    story.StoryStage = 2;
                    story.DaysSinceJoined = 0;

                    // 1. 赠送 值夜人制式手枪
                    Main.LocalPlayer.QuickSpawnItem(npc.GetSource_GiftOrReward(), ModContent.ItemType<Items.Weapons.NighthawkPistol>());

                    // 2. 赠送 100发 除魔子弹
                    Main.LocalPlayer.QuickSpawnItem(npc.GetSource_GiftOrReward(), ModContent.ItemType<Items.Ammo.ExorcismBullet>(), 100);

                    // 3. 赠送一点钱
                    Main.LocalPlayer.QuickSpawnItem(npc.GetSource_GiftOrReward(), ItemID.SilverCoin, 50);

                    Main.NewText("你加入了值夜者小队！获得了制式装备。", 50, 255, 50);

                    ui.SetDialogueText("明智的选择，欢迎加入。\n这是你的配枪和特制的炼金子弹。\n面对超凡生物时，普通子弹的效果很差，记得省着点用。");

                    ui.ClearButtons();
                    // 加入完成后，给一个离开按钮
                    ui.AddButton("离开", () => { Main.LocalPlayer.SetTalkNPC(-1); ui.Close(); });
                });
            }
            // 阶段 3：Boss战
            else if (story.StoryStage == 3)
            {
                ui.AddButton("出发！(挑战瑞尔·比伯)", () => {
                    Main.LocalPlayer.SetTalkNPC(-1);
                    ui.Close();
                    Main.NewText("瑞尔·比伯在前方现身了！(BOSS战待实装)", 175, 75, 255);
                    // NPC.SpawnOnPlayer(Main.LocalPlayer.whoAmI, ModContent.NPCType<Content.NPCs.Bosses.RayBieber>());
                });
            }

            // ----------------------------------------------------
            // 通用离开按钮 (重要：必须包含 SetTalkNPC(-1))
            // ----------------------------------------------------
            ui.AddButton("离开", () => {
                Main.LocalPlayer.SetTalkNPC(-1);
                ui.Close();
            });
        }
    }
}