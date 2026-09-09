using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using Terraria.UI;
using System.Collections.Generic;
using System.Linq;
using System;
using zhashi.Content.UI;

namespace zhashi.Content.UI.Profiles
{
    public class AnglerProfile : GalgameProfile
    {
        // --- 视觉风格：海边顽童 ---
        public override float OffsetX => 0f;
        public override float OffsetY => 0f;

        public override Color BackgroundColor => new Color(0, 105, 148) * 0.98f;
        public override Color BorderColor => new Color(244, 164, 96) * 0.8f;
        public override Color NameColor => Color.Gold;

        public override string HeadTexture => "zhashi/Content/UI/Portraits/Angler_Head";
        public override string StandingTexture => "zhashi/Content/UI/Portraits/Angler_Standing";

        private string RandomLine(params string[] lines) => lines[Main.rand.Next(lines.Length)];

        // --- 核心对话逻辑 ---
        public override string GetDialogue(NPC npc)
        {
            Player player = Main.LocalPlayer;
            int favor = GetFavor(npc.type);
            bool isQuestFinished = Main.anglerQuestFinished;

            // 1. 任务完成后的驱逐
            if (isQuestFinished)
            {
                return RandomLine(
                    "你还赖在这里干什么？今天的赏赐已经发完了！\n快滚，别挡着我看海。",
                    "别用那种看小孩的眼神看我！我杀过的海怪比你吃过的饭还多！\n现在，消失！",
                    "去睡你的觉去！我不需要你在旁边呼吸我的空气。"
                );
            }

            // 2. 特殊环境
            if (Main.bloodMoon)
                return "这种红色的月亮...让我想起了那晚的大海。\n那时候我还很高大...该死，快去把那些僵尸清理掉！";

            if (Main.raining)
                return "雨水会让我想起我不该想起的事情。\n喂，跑腿的，给我撑伞！别让一滴雨落在本大爷身上！";

            // 3. 好感度判定
            if (favor >= 100)
                return RandomLine(
                    "啧...是你啊。虽然你长得丑，脑子笨，还有一股鱼腥味...\n但只有你稍微能听懂我说的话。\n感到荣幸吧，你是我的头号大副...虽然现在没有船。",
                    "喂，别去给别人跑腿了。你只能给我跑腿，听懂了吗？\n这是命令！来自船长的命令！",
                    "把手伸过来...哼，既然你这么想要我的关注，我就勉强允许你摸摸我的头。\n敢弄乱发型你就死定了！我可是很在意形象的。"
                );

            if (favor < 0)
                return "呕...看到你的脸我就想吐。你是从哪个垃圾堆里爬出来的？\n离我至少十米远！";

            // 4. 默认对话 (包含诅咒伏笔)
            return RandomLine(
                "我看你不顺眼。你的衣服没品位，发型像鸟窝。\n只有给我抓鱼才能稍微弥补你的无能。",
                "我是这里的老大，懂吗？别以为我个子小就好欺负。\n我的真实年龄说出来能吓死你！",
                "你是猴子请来的救兵吗？怎么看起来呆头呆脑的。\n希望你钓鱼的技术比你的智商高那么一点点。",
                "如果你看到戴着**单片眼镜**的家伙...算了，你也打不过他。\n总之，别把那种人引到我这里来！"
            );
        }

        // --- 按钮逻辑 ---
        public override void SetupButtons(NPC npc, GalgameDialogueUI ui)
        {
            int favor = GetFavor(npc.type);
            Player player = Main.LocalPlayer;

            // 1. 任务系统
            int questFishID = Main.anglerQuestItemNetIDs[Main.anglerQuest];
            bool hasQuestFish = player.CountItem(questFishID) > 0;
            bool isFinished = Main.anglerQuestFinished;

            if (!isFinished)
            {
                if (hasQuestFish)
                    ui.AddButton("【上交任务鱼】", () => TurnInQuest(ui, npc, questFishID));
                else
                    ui.AddButton("我要抓什么？", () => ExplainQuest(ui));
            }
            else
            {
                ui.AddButton("任务已完成", () => ui.SetDialogueText("你是金鱼脑子吗？我说了今天结束了！\n明天早上再来，如果你还没死在野外的话。"));
            }

            // 2. 闲聊 (包含诅咒剧情)
            ui.AddButton("闲聊", () => SetupChatMenu(npc, ui));

            // 3. 送礼
            if (FavorabilitySystem.CanGiftToday(NPCID.Angler))
                ui.AddButton("进贡礼物", () => HandleGiftGiving(ui, npc));
            else
                ui.AddButton("进贡礼物 (已满)", () => ui.SetDialogueText("拿着你的垃圾滚。我的口袋已经装不下更多废品了。"));

            // 4. 欢愉
            if (favor >= 100)
            {
                ui.AddButton("【欢愉】", () => HandlePleasure(ui, npc));
            }

            ui.AddButton("滚开", () => {
                Main.LocalPlayer.SetTalkNPC(-1);
                ModContent.GetInstance<GalgameUISystem>().CloseUI();
            });
        }

        // ==========================================
        // ★ 核心玩法：提交任务 ★
        // ==========================================
        private void TurnInQuest(GalgameDialogueUI ui, NPC npc, int fishID)
        {
            Player player = Main.LocalPlayer;

            if (player.CountItem(fishID) <= 0 || Main.anglerQuestFinished)
            {
                ui.SetDialogueText("鱼呢？你敢耍我？信不信我把你当鱼饵挂在钩子上？");
                return;
            }

            int index = player.FindItem(fishID);
            if (index != -1)
            {
                player.inventory[index].stack--;
                if (player.inventory[index].stack <= 0)
                    player.inventory[index].TurnToAir();
            }

            player.GetAnglerReward(npc, fishID);
            Main.anglerQuestFinished = true;

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4);
            CombatText.NewText(npc.getRect(), Color.Gold, "任务完成！");

            FavorabilitySystem.IncreaseFavorability(NPCID.Angler, 2);
            FavorabilitySystem.RecordInteraction(npc.type);

            ui.ClearButtons();
            SetupButtons(npc, ui);

            string reply = RandomLine(
                "哼，终于拿来了。虽然这条鱼看起来有点蠢，就像你一样...但勉强合格吧。\n拿着你的赏赐快滚！",
                "这就完了？我还以为你会死在半路上呢。\n东西放下，奖励拿走。别指望我会说谢谢。",
                "干得不错...我是指对于你这种低等生物来说。\n这是给你的一点小费，拿去买糖吃吧...啧，我才不吃糖。"
            );
            ui.SetDialogueText(reply);
        }

        // ==========================================
        // ★ 核心玩法：解释任务 ★
        // ==========================================
        private void ExplainQuest(GalgameDialogueUI ui)
        {
            string questText = Lang.AnglerQuestChat(false);
            string insult = RandomLine(
                "你连这个都不知道？还要我解释一遍？听好了，笨蛋！\n\n",
                "把你的耳朵竖起来！我只说一次！\n\n",
                "你是文盲吗？好吧，本大爷就大发慈悲告诉你：\n\n"
            );
            ui.SetDialogueText(insult + questText);
        }

        // ==========================================
        // ★ 欢愉系统 (主仆/被诅咒者的慰藉) ★
        // ==========================================
        private void HandlePleasure(GalgameDialogueUI ui, NPC npc)
        {
            Player player = Main.LocalPlayer;
            player.statLife = player.statLifeMax2; player.HealEffect(player.statLifeMax2);
            npc.life = npc.lifeMax; npc.HealEffect(npc.lifeMax);

            for (int i = 0; i < 50; i++)
            {
                Vector2 speed = Main.rand.NextVector2Circular(5f, 5f);
                Dust.NewDust(player.position, player.width, player.height, DustID.GoldCoin, speed.X, speed.Y);
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);

            string text = RandomLine(
                "（他把你当成靠垫一样靠在身上，声音变得低沉而沙哑，不像个孩子）\n...只有这个时候，我才感觉那个‘窃取者’留下的空洞被填满了。\n不准动！让我作为‘大人’休息一会儿。\n...谢谢，我的大副。",
                "（他紧紧抓着你的衣角，眼神里闪过一丝极度的恐惧后的依赖）\n...别误会！我只是怕你跑了没人给我抓鱼！\n那只‘时之虫’...如果它再出现，你就挡在我前面！\n你是我的专属仆人，这辈子都是！"
            );

            ui.SetDialogueText(text);
            FavorabilitySystem.RecordInteraction(npc.type);
        }

        // ==========================================
        // ★ 送礼系统 ★
        // ==========================================
        private void HandleGiftGiving(GalgameDialogueUI ui, NPC npc)
        {
            Player player = Main.LocalPlayer;
            Item item = player.HeldItem;

            if (item.IsAir)
            {
                ui.SetDialogueText("两手空空？你是来向我展示你的贫穷吗？\n滚！");
                return;
            }

            string reply = "";
            int favorChange = 0;
            bool success = false;

            // 1. 真爱之心
            if (item.type == ModContent.ItemType<Items.TrueLoveHeart>())
            {
                FavorabilitySystem.SetFavorability(NPCID.Angler, 100);
                item.stack--; if (item.stack <= 0) item.TurnToAir();
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4);
                FavorabilitySystem.RecordGift(NPCID.Angler);

                ui.ClearButtons(); SetupButtons(npc, ui);
                ui.SetDialogueText("（他看着那颗心，眼神瞬间变得复杂，仿佛透过它看到了过去的岁月）\n你...你把这种东西给我？给一个被时间诅咒的怪物？\n...哼！既然你这么求我收下，那我就勉为其难地保管着！\n这不是因为感动！绝对不是！笨蛋！");
                return;
            }

            // 2. 喜欢
            if (item.type == ItemID.GoldenCarp || item.type == ItemID.FishingPotion || item.type == ItemID.MasterBait)
            {
                favorChange = 2;
                reply = "这还差不多。虽然品质一般般，但勉强能入我的眼。\n下次记得找个更亮一点的！";
                success = true;
            }
            // 3. 讨厌
            else if (item.type == ItemID.OldShoe || item.type == ItemID.TinCan || item.type == ItemID.FishingSeaweed)
            {
                favorChange = -5;
                reply = "（他把你送的垃圾狠狠砸在你脸上）\n这就是你钓上来的东西？还是说这就是你的晚餐？\n拿走！别弄脏我的地板！";
                success = false;
            }
            // 4. 普通
            else
            {
                favorChange = 0;
                reply = "这什么破烂？我不感兴趣。\n既然你这么想送，就放在那边的角落里吧，也许哪天我会用来垫桌脚。";
                success = true;
            }

            if (favorChange != 0 || success)
            {
                if (success)
                {
                    item.stack--; if (item.stack <= 0) item.TurnToAir();
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4);
                    FavorabilitySystem.RecordGift(NPCID.Angler);
                }
                if (favorChange != 0) FavorabilitySystem.IncreaseFavorability(NPCID.Angler, favorChange);
                ui.ClearButtons(); SetupButtons(npc, ui);
            }
            ui.SetDialogueText(reply);
        }

        // ==========================================
        // ★ 闲聊系统 (含剧情伏笔) ★
        // ==========================================
        private void SetupChatMenu(NPC npc, GalgameDialogueUI ui)
        {
            ui.ClearButtons();
            var chatOptions = new List<KeyValuePair<string, string>>();
            chatOptions.Add(new KeyValuePair<string, string>("Fish", "聊聊鱼"));
            chatOptions.Add(new KeyValuePair<string, string>("Parents", "询问父母")); // 雷区
            chatOptions.Add(new KeyValuePair<string, string>("Curse", "询问诅咒")); // ★ 核心剧情选项
            chatOptions.Add(new KeyValuePair<string, string>("Insult", "对骂")); // 作死

            foreach (var option in chatOptions)
                ui.AddButton(option.Value, () => HandleChatResponse(option.Key, ui, npc));

            ui.AddButton("<< 闭嘴离开", () => {
                ui.ClearButtons();
                SetupButtons(npc, ui);
                ui.SetDialogueText("终于安静了。");
            });
            ui.SetDialogueText("有话快说，有屁快放。本大爷很忙的。");
        }

        private void HandleChatResponse(string type, GalgameDialogueUI ui, NPC npc)
        {
            string reply = "";
            int favorChange = 0;

            switch (type)
            {
                case "Fish":
                    reply = "你也配聊鱼？你知道怎么区别彩虹鱼和普通的热带鱼吗？\n不知道就闭嘴听我说！首先，你要看鳞片的光泽...";
                    favorChange = 1;
                    break;
                case "Parents":
                    reply = "（他的眼神瞬间变得阴沉，仿佛看到了深渊）\n...父母？我没有父母。我是被大海吐出来的。\n再提这个话题，我就让你后悔出生在这个世上！";
                    favorChange = -5;
                    break;
                case "Curse":
                    reply = "（他下意识地摸了摸右眼的位置，那里空无一物）\n哼，既然你这么想知道...我就告诉你，让你做噩梦！\n我曾经是个船长，统领着七海...直到我遇到了那个戴单片眼镜的家伙。\n他是个‘小偷’。他偷走了我的时间，偷走了我的成长。\n他把我变成这副模样，还把我的船员变成了...变成了...闭嘴！我不想说了！";
                    favorChange = 2; // 触及灵魂，反而加好感（视为倾诉）
                    break;
                case "Insult":
                    reply = "哈？你想跟我比嘴臭？你还嫩了点！\n你就是个没毛的哥布林！长着腿的史莱姆！\n你的出生就是个错误，连僵尸都嫌你肉酸！";
                    favorChange = -2;
                    break;
            }

            if (favorChange > 0)
            {
                if (!FavorabilitySystem.CanChatRewardToday(NPCID.Angler)) favorChange = 0;
                else FavorabilitySystem.RecordChatReward(NPCID.Angler);
            }

            if (favorChange != 0) FavorabilitySystem.IncreaseFavorability(NPCID.Angler, favorChange);
            ui.SetDialogueText(reply);
        }
    }
}