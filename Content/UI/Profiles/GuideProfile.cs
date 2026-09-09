using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using System.Collections.Generic;
using System.Linq;
using System;

namespace zhashi.Content.UI.Profiles
{
    public class GuideProfile : GalgameProfile
    {
        // --- 视觉风格 ---
        public override float OffsetX => -50f;
        public override float OffsetY => 120f;
        public override Color BackgroundColor => new Color(20, 20, 25) * 0.98f;
        public override Color BorderColor => new Color(80, 80, 80) * 0.8f;
        public override Color NameColor => Color.LightGray;

        public override string HeadTexture => "zhashi/Content/UI/Portraits/Guide_Head";
        public override string StandingTexture => "zhashi/Content/UI/Portraits/Guide_Standing";

        private string RandomLine(params string[] lines) => lines[Main.rand.Next(lines.Length)];

        // --- 核心对话逻辑 ---
        public override string GetDialogue(NPC npc)
        {
            Player player = Main.LocalPlayer;
            int favor = GetFavor(npc.type);

            // 1. 彩蛋：持有巫毒娃娃
            if (player.HasItem(ItemID.GuideVoodooDoll))
            {
                return RandomLine(
                    "那股硫磺味...还有那个娃娃。你是来给我送行的吗？\n快点，别犹豫。我已经准备好燃烧了。",
                    "盯着那个娃娃看什么？把它扔进岩浆里，我也许能睡个好觉。\n虽然我知道我还是会醒过来...",
                    "终于...你终于要把我献祭了吗？谢谢你，真的。"
                );
            }

            // 2. 环境判定
            if (Main.bloodMoon) return "门锁好了吗？如果你被僵尸吃了，没人会给你收尸的。\n我也不会，我正忙着发呆。";
            if (Main.eclipse) return "看那太阳...就像一部烂透了的恐怖片。";
            if (player.ZoneGraveyard) return "这里真安静。比那个吵闹的小镇好多了。\n如果我躺在这里不动，系统会判定我死亡吗？";

            // 3. 好感度判定 (补充了高好感对话)
            if (favor >= 100)
                return RandomLine(
                    "你又来了...即使我这么无趣，你还是愿意来找我吗？\n...不，我不讨厌这样。",
                    "在这个疯狂的世界里，你是唯一让我觉得“真实”的存在。",
                    "别走。再待一会儿。只有你在的时候，我才听不到那些代码运行的噪音。"
                );

            if (favor < 0) return "离我远点。你的存在让空气都变得浑浊了。";

            return RandomLine(
                "（他深深吸了一口烟，并没有点燃）\n活着真累...你有什么事？快说，说完快滚。",
                "我又看见你死了一次。我都懒得数是第几次了。",
                "你以为你是这个世界的主角？别傻了，我也曾以为我是。",
                "这杯咖啡已经凉了。就像我的心一样。"
            );
        }

        // --- 按钮逻辑 (已修复：加入了欢愉按钮) ---
        public override void SetupButtons(NPC npc, GalgameDialogueUI ui)
        {
            int favor = GetFavor(npc.type);

            ui.AddButton("配方查询...", () => {
                Main.playerInventory = true; // 打开背包
                Main.npcChatText = "";       // 清空原版对话框文本
                Main.LocalPlayer.SetTalkNPC(npc.whoAmI); // 锁定对话NPC

                Main.InGuideCraftMenu = true;

                ModContent.GetInstance<GalgameUISystem>().CloseUI();
            });

            // 2. 帮助
            ui.AddButton("寻求指引", () => {
                string helpText = GetRandomHelpText();
                ui.SetDialogueText(helpText);
                // 注意：这里没有调用 ui.ClearButtons()，所以按钮会保留，可以一直点
            });

            // 3. 闲聊
            ui.AddButton("闲聊", () => SetupChatMenu(npc, ui));

            // 4. 送礼
            if (FavorabilitySystem.CanGiftToday(NPCID.Guide))
                ui.AddButton("赠送礼物", () => HandleGiftGiving(ui, npc));
            else
                ui.AddButton("赠送礼物 (上限)", () => ui.SetDialogueText("（他摆了摆手）\n哪怕你把金山搬来，我也没地方放了。省省吧。"));

            // ★★★ 5. 欢愉系统 (核心修复点) ★★★
            // 只有好感度达到 100 才会显示这个按钮
            if (favor >= 100)
            {
                ui.AddButton("【欢愉】", () => HandlePleasure(ui, npc));
            }

            // 6. 离开
            ui.AddButton("别丧了，再见", () => {
                Main.LocalPlayer.SetTalkNPC(-1);
                ModContent.GetInstance<GalgameUISystem>().CloseUI();
            });
        }

        private string GetRandomHelpText()
        {
            if (!Main.hardMode)
            {
                return RandomLine(
                    "往左走是死，往右走也是死。\n我的建议是：找个坑把自己埋了，那样最安全。",
                    "如果你看见一个紫色或红色的大坑，千万别跳下去。\n除非你想变成怪物的饲料，或者变成和我一样的死人。",
                    "晚上会有僵尸敲门。如果你不开门，它们就会一直敲。\n如果你开了门，你的脑子就会被吃掉。真是两难的选择，对吧？",
                    "据说地底下有发光的蘑菇。\n如果你吃了它们，也许能看到幻觉，忘掉这个糟糕的世界。",
                    "想变强？去挖矿吧。\n像个地鼠一样在黑暗里挖一整天，然后被落石砸死。这就是冒险者的宿命。",

                    "想获得力量？去找亵渎石板碎片。\n但记住，魔药不是饮料。乱喝的下场就是变成外面那些只会嘶吼的怪物。",
                    "如果你还有什么不懂的，不妨去作者的B站账号上看看。",
                    "众所周知，泰拉瑞亚是有wiki百科的。",
                    "如果你感觉到耳边有呓语，或者看到不存在的阴影...\n恭喜你，你的san值太低了，或者你快失控了。去歇一会吧。",
                    "不要直视那些不可名状的存在...哦，我忘了，在这个像素世界里，你也看不清它们的脸。"
                );
            }
            else if (!NPC.downedMechBossAny)
            {
                return RandomLine(
                    "你把那个肉墙杀死了？恭喜，你把世界搞得更糟了。\n现在光之女皇和腐化怪物正在争夺地盘，我们只是夹缝中的蝼蚁。",
                    "听到了吗？金属碰撞的声音。\n如果你不想被激光射成筛子，最好在晚上躲进地窖里。",
                    "现在连天空都不安全了。飞龙会把你撕成碎片的。\n所以，还是回地下当你的地鼠吧。",
                    "如果你手里拿着什么奇怪的机械零件，千万别在晚上拿出来。\n除非你想体验一下被几十吨钢铁碾压的快感。",

                    "世界随着血肉之墙的倒塌变得更加疯狂了。\n这空气中弥漫着高序列强者的气息...小心，别被他们的神性污染。",
                    "你现在的序列应该不低了吧？\n小心失控。力量越强，离疯狂越近。如果你觉得自己变成了某种动物或者一堆触手，记得离我远点。",
                    "某些强大的封印物虽然好用，但都有可怕的负面效果。\n比如那把剑可能会让你想自杀，那件衣服可能会吸干你的血...就像资本家一样。",
                    "想要晋升半神？那你需要准备好仪式。\n没有仪式的辅助，凡人的躯体是无法容纳神性的。别为了省事而送命。"
                );
            }
            else if (!NPC.downedPlantBoss)
            {
                return RandomLine(
                    "丛林里有一朵巨大的花。\n如果你把它吵醒了，它会把你当成肥料。\n不过对于这个世界来说，那也许是最好的结局。",
                    "去丛林吧。那里的黄蜂比你的头还大。\n如果你能活着回来，我就...算了，我也没什么能给你的。",
                    "世界变得越来越疯狂了。我有时候真希望那个巫毒娃娃早点掉进岩浆里。",

                    "你身上的人性正在减少...我能感觉到。\n你现在看我的眼神，像是在看一只蚂蚁。这就是神性的代价吗？",
                    "在这个阶段，普通的魔药已经不够了。\n你需要狩猎那些传说中的生物，夺取它们的特性。这是一场弱肉强食的残酷游戏。",
                    "小心‘星空’的注视。\n虽然泰拉瑞亚没有真正的星空，但那种来自宇宙深处的恶意...依然存在。",
                    "如果你在地下看到了古老的神庙，别乱碰里面的东西。\n那是属于旧日神灵的领域，不是你这种半神能随意亵渎的。"
                );
            }
            else
            {
                return RandomLine(
                    "你已经战胜了神明，甚至战胜了月亮。\n但你依然无法战胜空虚，对吗？\n这就是为什么你还会来找我这个无用的向导说话。",
                    "你已经是这个世界的主宰了。你想干什么都行。\n杀了我？毁灭世界？随便你。反正我已经麻木了。",
                    "在这个阶段，活着本身就是一种奇迹。\n或者是某种残酷的诅咒。",

                    "你现在是什么？天使？还是神？\n对我来说都一样。只要别在我不小心的时候把我抹杀掉就行。",
                    "你需要‘锚’。\n如果不想被原初的意志同化，你需要这个世界的人记住你。去吧，去当个英雄，或者魔王。",
                    "所有的非凡特性必将回归...这是聚合定律。\n你杀得越多，你就越接近那个源头。你确定这真的是你想要的吗？",
                    "小心阿蒙...哦，对不起，串戏了。\n不过如果你发现你的单片眼镜在右眼，或者我有两个单片眼镜...快跑！"
                );
            }
        }
        // --- ★ 向导专属的欢愉逻辑 ★ ---
        private void HandlePleasure(GalgameDialogueUI ui, NPC npc)
        {
            Player player = Main.LocalPlayer;

            // 1. 回复生命 (双向)
            player.statLife = player.statLifeMax2; player.HealEffect(player.statLifeMax2);
            npc.life = npc.lifeMax; npc.HealEffect(npc.lifeMax);

            // 2. 特效 (向导的特效可以稍微暗淡一点，或者保持心形)
            for (int i = 0; i < 50; i++)
            {
                Vector2 speed = Main.rand.NextVector2Circular(5f, 5f);
                Dust.NewDust(player.position, player.width, player.height, DustID.HeartCrystal, speed.X, speed.Y);
            }

            // 3. 联机同步
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);

            string text = RandomLine(
                "（他紧紧抓住了你的肩膀，仿佛溺水的人抓住了唯一的浮木）\n...好温暖。\n在这短暂的一刻，我感觉那个该死的血肉之墙似乎不存在了。\n别停下...让我觉得自己还活着。",
                "（他把头埋在你的颈窝里，身体微微颤抖）\n这就是所谓的“幸福”吗？\n如果能一直这样...我也许就不想死了。",
                "（事后，他看着天花板，眼神中少了一丝死寂）\n...谢谢。刚才那几分钟，是我这辈子唯一清醒的时候。"
            );

            ui.SetDialogueText(text);
            FavorabilitySystem.RecordInteraction(npc.type);
        }

        // --- 闲聊子菜单 ---
        private void SetupChatMenu(NPC npc, GalgameDialogueUI ui)
        {
            ui.ClearButtons();
            var chatOptions = new List<KeyValuePair<string, string>>();

            chatOptions.Add(new KeyValuePair<string, string>("Life", "询问人生"));
            chatOptions.Add(new KeyValuePair<string, string>("World", "询问世界本质"));
            chatOptions.Add(new KeyValuePair<string, string>("Wall", "提起那堵墙"));

            if (Main.rand.NextBool()) chatOptions.Add(new KeyValuePair<string, string>("Nurse", "提起护士"));

            foreach (var option in chatOptions)
                ui.AddButton(option.Value, () => HandleChatResponse(option.Key, ui, npc));

            ui.AddButton("<< 返回", () => {
                ui.ClearButtons();
                SetupButtons(npc, ui);
                ui.SetDialogueText("（他叹了口气，继续盯着虚空发呆）");
            });

            ui.SetDialogueText("（他抬起沉重的眼皮）\n你想聊什么？如果是关于梦想之类的废话，就免了。");
        }

        private void HandleChatResponse(string type, GalgameDialogueUI ui, NPC npc)
        {
            string reply = "";
            int favorChange = 0;

            switch (type)
            {
                case "Life":
                    if (FavorabilitySystem.CanChatRewardToday(NPCID.Guide))
                    {
                        reply = "人生？NPC的人生就是等待刷新...（略）";
                        favorChange = 1;
                        // ★ 记录今天已奖励
                        FavorabilitySystem.RecordChatReward(NPCID.Guide);
                    }
                    else
                    {
                        reply = "人生？我都说过了，NPC的人生就是...别让我重复废话。";
                        favorChange = 0; // 不再加分
                    }
                    break;
                case "World":
                    reply = "这个世界...不过是一堆像素方块堆砌起来的监狱。\n你以为你在冒险？不，你只是在程序设定的轨道上跑圈。";
                    break;
                case "Wall":
                    reply = "（他浑身颤抖了一下）\n那堵血肉之墙...它是我的宿命，也是我的诅咒。\n每次我想起它，我就感觉身体在燃烧...那是解脱的味道。";
                    break;
                case "Nurse":
                    reply = "那个总是拿着针筒的疯女人？\n我有一次受伤去找她，她看着我的眼神像是在看一块鲜肉。\n我宁愿流血流死也不想再进她的诊所。";
                    break;
            }

            if (favorChange != 0) FavorabilitySystem.IncreaseFavorability(NPCID.Guide, favorChange);
            ui.SetDialogueText(reply);
            // 聊完不清空按钮，保留在子菜单方便继续聊
        }

        // --- 送礼系统 ---
        private void HandleGiftGiving(GalgameDialogueUI ui, NPC npc)
        {
            Player player = Main.LocalPlayer;
            Item item = player.HeldItem;

            if (item.IsAir)
            {
                ui.SetDialogueText("手是空的？你是想送我“虚无”吗？\n...其实这倒挺符合我心意的。");
                return;
            }

            string reply = "";
            int favorChange = 0;
            bool success = false;

            if (item.type == ModContent.ItemType<Items.TrueLoveHeart>())
            {
                FavorabilitySystem.SetFavorability(NPCID.Guide, 100);

                item.stack--;
                if (item.stack <= 0) item.TurnToAir();
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4);
                FavorabilitySystem.RecordGift(NPCID.Guide);

                ui.ClearButtons();
                SetupButtons(npc, ui);

                ui.SetDialogueText("（他愣住了，手中的那颗心散发着温暖的光芒，驱散了他眼中的阴霾）\n为了我？浪费这么珍贵的东西？\n...也许这个世界并不全是绝望。至少此时此刻，我觉得活着...真好。");
                return;
            }

            // 1. 酒类 (最爱)
            if (item.type == ItemID.Ale || item.type == ItemID.Sake)
            {
                favorChange = 10;
                reply = "（他一把抢过酒瓶，猛灌了一口）\n哈...只有这个能让我短暂地忘记这一切。\n为了这瓶酒，我愿意多活几分钟。";
                success = true;
            }
            // 2. 巫毒娃娃 (特殊)
            else if (item.type == ItemID.GuideVoodooDoll)
            {
                favorChange = 5;
                reply = "你把它送给我？\n傻瓜...这东西在我手里没用。你得把它扔进地狱的岩浆里。\n不过...谢谢你让我看看我的归宿。";
                success = true;
            }
            // 3. 书籍/纸张 (还行)
            else if (item.type == ItemID.Book)
            {
                favorChange = 2;
                reply = "书？好吧，打发时间用的。虽然里面的故事都是假的，但总比现实好。";
                success = true;
            }
            // 4. 垃圾
            else if (item.type == ItemID.DirtBlock || item.type == ItemID.MudBlock)
            {
                favorChange = -5;
                reply = "泥土。真棒。就像我的未来一样肮脏。\n拿着它滚远点。";
                success = false;
            }
            else
            {
                favorChange = 1;
                reply = "这是什么？算了，不管是什么，只要不是炸弹就行。\n...其实炸弹也不错。";
                success = true;
            }

            if (favorChange != 0)
            {
                if (success)
                {
                    item.stack--;
                    if (item.stack <= 0) item.TurnToAir();
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4);
                    FavorabilitySystem.RecordGift(NPCID.Guide);
                }
                FavorabilitySystem.IncreaseFavorability(NPCID.Guide, favorChange);

                ui.ClearButtons();
                SetupButtons(npc, ui);
            }
            ui.SetDialogueText(reply);
        }
    }
}