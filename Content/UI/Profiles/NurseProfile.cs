using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using System.Linq;
using System;

namespace zhashi.Content.UI.Profiles
{
    public class NurseProfile : GalgameProfile
    {
        // --- 视觉风格 ---
        public override float OffsetX => 0f;

        public override float OffsetY => 40f;
        public override Color BackgroundColor => new Color(30, 10, 15) * 0.98f; // 深红背景
        public override Color BorderColor => Color.Crimson * 0.8f; // 鲜红边框
        public override Color NameColor => Color.Crimson;

        public override string HeadTexture => "zhashi/Content/UI/Portraits/Nurse_Head";
        public override string StandingTexture => "zhashi/Content/UI/Portraits/Nurse_Standing";

        // 辅助随机数生成器
        private string RandomLine(params string[] lines) => lines[Main.rand.Next(lines.Length)];

        // --- 核心：动态对话生成系统 ---
        public override string GetDialogue(NPC npc)
        {
            Player player = Main.LocalPlayer;
            int favor = GetFavor(npc.type);
            int missingHealth = player.statLifeMax2 - player.statLife;
            float healthPct = (float)player.statLife / player.statLifeMax2;

            // 1. 特殊环境/状态判定 (优先级最高)

            // 拿着武器
            if (player.HeldItem.damage > 0 && Main.rand.NextBool(4))
                return RandomLine(
                    $"那把{player.HeldItem.Name}...上面沾着血吗？让我闻闻...",
                    "你又要去制造伤口了？还是去制造尸体？无论哪个我都喜欢。"
                );

            // 树妖在场 (吃醋)
            if (player.HasBuff(BuffID.DryadsWard))
                return "（她皱起眉头，拿出一瓶除臭剂对着你喷了几下）\n全是那个女人的土腥味...别动，我要给你彻底消毒。";

            // 血月 (兴奋)
            if (Main.bloodMoon)
                return RandomLine(
                    "听到了吗？外面的惨叫声...简直像乐章一样悦耳。",
                    "今晚的月亮真美。如果不小心把动脉割破，血液喷溅的高度会比平时更高哦。",
                    "别出去。留在这里。如果你死了，谁来当我的实验...我是说，病人？"
                );

            // 墓地 (占有欲)
            if (player.ZoneGraveyard)
                return "这里很冷，对吧？如果你躺进棺材里，我就可以永远守着你了...开玩笑的，你还没死透呢。";

            // 2. 玩家健康状态判定

            // 濒死 (血量 < 20%)
            if (healthPct < 0.2f)
                return RandomLine(
                    "啊...太美了。这种濒临破碎的脆弱感...",
                    "别动。再动一下你的肠子就要流出来了。让我好好“欣赏”一下。",
                    "嘘...把你的命交给我。现在，我是你的神。"
                );

            // 受伤 (血量 < 100%)
            if (missingHealth > 0)
                return RandomLine(
                    "又受伤了？你是不是故意想见我？",
                    "痛吗？痛就对了。痛觉证明你还属于我。",
                    "过来，让我把这块肉缝回去。"
                );

            // 3. 好感度判定 (默认状态)

            if (favor < 40) // 厌恶/冷淡
                return RandomLine(
                    "没病就滚。我的手术刀不长眼睛。",
                    "别挡着光。我在擦洗地上的血迹。",
                    "你很闲吗？去跳进岩浆里，那样我就有事做了。"
                );

            if (favor < 80) // 逐渐接受/病娇初显
                return RandomLine(
                    "你最近来的次数变多了。想我了？还是想我的药？",
                    "把衣服脱...我是说，把袖子挽起来。例行检查。",
                    "我在研究一种新药。想试试吗？副作用可能包括...爱上我？呵呵。"
                );

            // 极高好感 (完全病娇化)
            return RandomLine(
                "亲爱的...我已经把手术台清理干净了。你可以随时躺上来，永远不下去。",
                "你的心跳声...咚、咚、咚...这是我最喜欢的摇篮曲。",
                "我在你的药水里加了一点...我的血。这样我们就融为一体了。",
                "不要看别人。如果你敢看那个电工妹一眼，我就把你的眼球挖出来泡在福尔马林里，让它只看着我。"
            );
        }

        // --- 按钮配置 ---
        public override void SetupButtons(NPC npc, GalgameDialogueUI ui)
        {
            int favor = GetFavor(npc.type);

            // 治疗
            string healText = favor >= 100 ? "爱的治疗 (免费)" : (Main.LocalPlayer.statLife < Main.LocalPlayer.statLifeMax2 ? "请求治疗" : "例行检查");
            ui.AddButton(healText, () => HealNurse(ui));

            // 闲聊
            ui.AddButton("闲聊", () => SetupChatMenu(npc, ui));

            // 送礼
            if (FavorabilitySystem.CanGiftToday(NPCID.Nurse))
                ui.AddButton("赠送礼物", () => HandleGiftGiving(ui, npc));
            else
                ui.AddButton("赠送礼物 (上限)", () => ui.SetDialogueText("（她指了指堆满礼物的柜子）\n够了。虽然我喜欢你的奉献，但今天已经塞不下了。"));

            // 高级功能
            if (favor >= 80)
                ui.AddButton("★ 特殊护理 ★", () => HandleSpecialCare(ui, favor));

            if (favor >= 100)
                ui.AddButton("【欢愉】", () => HandlePleasure(ui, npc));

            ui.AddButton("离开", () => {
                Main.LocalPlayer.SetTalkNPC(-1);
                ModContent.GetInstance<GalgameUISystem>().CloseUI();
            });
        }

        // ==========================================
        // ★ 核心功能实现 ★
        // ==========================================

        private void HealNurse(GalgameDialogueUI ui)
        {
            Player player = Main.LocalPlayer;
            int favor = GetFavor(NPCID.Nurse);
            int missingHealth = player.statLifeMax2 - player.statLife;

            // 满血时的对话
            if (missingHealth <= 0)
            {
                string[] healthyLines = {
                    "身体很健康...甚至有点太健康了。想让我切开看看里面吗？",
                    "没有伤口？真无聊。你是来专门看我的吗？",
                    "心率正常，血压正常，体温...稍微有点高。你在紧张什么？"
                };
                ui.SetDialogueText(RandomLine(healthyLines));
                return;
            }

            // 计算价格
            int baseCost = missingHealth * 100;
            if (Main.expertMode) baseCost *= 2;
            float discount = favor >= 100 ? 0f : (favor >= 70 ? 0.9f : 1.0f);
            if (favor < 50) discount = 1.5f; // 好感低加价

            int finalCost = (int)(baseCost * discount);

            if (player.BuyItem(finalCost))
            {
                player.statLife += missingHealth;
                player.HealEffect(missingHealth);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4);
                FavorabilitySystem.IncreaseFavorability(NPCID.Nurse, 1);
                FavorabilitySystem.RecordHeal(NPCID.Nurse);

                // 治疗后的随机文本
                string reply = "";
                if (favor >= 100)
                {
                    reply = RandomLine(
                        "哪怕你只剩下一个头，我也会把你拼回去的...因为你是我的。",
                        "好了。下次如果你再带着别人的气味回来，我就不给你打麻药了。",
                        "这种肌肤愈合的过程...真是百看不厌。"
                    );
                }
                else
                {
                    reply = RandomLine(
                        "别乱动，缝歪了就丑了。",
                        "好了。下次小心点，我不希望这么快又见到你的内脏。",
                        "收工。记得按时吃药。"
                    );
                }

                ui.SetDialogueText(reply);

                // 特效
                if (favor >= 90)
                    for (int i = 0; i < 20; i++) Dust.NewDust(player.position, player.width, player.height, DustID.LifeDrain);

                // 刷新界面
                ui.ClearButtons();
                SetupButtons(Main.npc[Main.LocalPlayer.talkNPC], ui);
            }
            else
            {
                ui.SetDialogueText($"没钱？你需要 {finalCost} 铜币。\n（她冷冷地看着你流血的伤口）\n没有钱，我就不能买新的手术刀。这就是规矩。");
            }
        }

        private void HandleGiftGiving(GalgameDialogueUI ui, NPC npc)
        {
            Player player = Main.LocalPlayer;
            Item item = player.HeldItem;

            if (item.IsAir)
            {
                ui.SetDialogueText("（她抓住你的手腕，检查了一番）\n手是空的？你想把你的体温送给我吗？");
                return;
            }

            string reply = "";
            int favorChange = 0;
            bool success = false;

            // --- 礼物判定逻辑 ---
            if (item.type == ModContent.ItemType<Items.TrueLoveHeart>())
            {
                // 直接设为 100 (或者是 999 突破上限，看你设定)
                FavorabilitySystem.SetFavorability(NPCID.Nurse, 100);

                // 消耗物品
                item.stack--;
                if (item.stack <= 0) item.TurnToAir();
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4); // 吃东西声音
                FavorabilitySystem.RecordGift(NPCID.Nurse);

                // 刷新界面
                ui.ClearButtons();
                SetupButtons(npc, ui);

                ui.SetDialogueText("（她颤抖着接过那颗跳动的心脏，眼神从疯狂转为彻底的沦陷）\n这...这是给我的？\n不需要再做实验了，也不需要再用药剂了...\n我现在确信，我们的灵魂已经缝合在了一起，永远不会分开。");
                return; // 直接结束，不跑后面的逻辑
            }
            if (item.type == ItemID.LifeCrystal || item.type == ItemID.LifeFruit)
            {
                favorChange = 5;
                reply = $"（她的瞳孔猛地收缩，死死盯着那颗{item.Name}）\n这是...纯粹的生命力？\n太美妙了...我会把它缝进我的枕头里，每晚都听着它的脉动入睡。";
                success = true;
            }
            else if (item.type == ItemID.LovePotion) // 特殊彩蛋：爱情药水
            {
                favorChange = 10;
                reply = "（她闻了闻药水，露出了意味深长的笑容）\n呵呵...不需要这种东西，我也已经为你疯狂了。\n不过，既然你想玩点刺激的...我收下了。";
                success = true;
            }
            else if (item.type == ItemID.Daybloom || item.type == ItemID.Ruby)
            {
                favorChange = 2;
                reply = "哼，还算稍微有点品味。我会把它做成标本，就像我想对你做的一样。";
                success = true;
            }
            else if (item.type == ItemID.DirtBlock || item.type == ItemID.MudBlock)
            {
                favorChange = -3;
                reply = "（她把你手里的脏东西打落在地）\n你想把细菌带进我的无菌室吗？捡起来，吞下去。";
                success = false;
            }
            else
            {
                favorChange = 1;
                reply = "奇怪的东西...算了，只要不是那个树妖送的垃圾就行。";
                success = true;
            }

            if (favorChange != 0)
            {
                if (success)
                {
                    item.stack--;
                    if (item.stack <= 0) item.TurnToAir();
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4);
                    FavorabilitySystem.RecordGift(NPCID.Nurse);
                }
                FavorabilitySystem.IncreaseFavorability(NPCID.Nurse, favorChange);

                ui.ClearButtons();
                SetupButtons(npc, ui);
            }
            ui.SetDialogueText(reply);
        }

        private void SetupChatMenu(NPC npc, GalgameDialogueUI ui)
        {
            ui.ClearButtons();

            var chatOptions = new List<KeyValuePair<string, string>>();
            chatOptions.Add(new KeyValuePair<string, string>("Praise", "称赞技术"));
            chatOptions.Add(new KeyValuePair<string, string>("Fear", "表示恐惧"));
            chatOptions.Add(new KeyValuePair<string, string>("Obsession", "表达依赖")); // 新选项
            chatOptions.Add(new KeyValuePair<string, string>("Others", "询问其他人")); // 作死选项

            // 随机取3个显示
            var randomSelection = chatOptions.OrderBy(x => Guid.NewGuid()).Take(3).ToList();

            foreach (var option in randomSelection)
                ui.AddButton(option.Value, () => HandleChatResponse(option.Key, ui, npc));

            ui.AddButton("<< 返回", () => {
                ui.ClearButtons();
                SetupButtons(npc, ui);
                ui.SetDialogueText("（她停下了擦拭手术刀的动作，静静地看着你）");
            });

            ui.SetDialogueText("（她用漆黑的眸子盯着你，仿佛想看穿你的皮肤）\n你想说什么？");
        }

        private void HandleChatResponse(string type, GalgameDialogueUI ui, NPC npc)
        {
            int favor = GetFavor(NPCID.Nurse);
            string reply = "";
            int favorChange = 0;

            switch (type)
            {
                case "Praise":
                    if (!FavorabilitySystem.HasHealedRecently(NPCID.Nurse))
                    {
                        favorChange = -1;
                        reply = "（她冷笑了一声）\n你今天根本没让我治疗过。我不喜欢虚伪的奉承。";
                    }
                    else
                    {
                        favorChange = 2;
                        reply = favor < 80 ? "哼，那是当然的。我是专业的。" : "喜欢吗？我可以把你的每一根神经都挑出来...那样你就能更清晰地感受我的技术了。";
                    }
                    break;
                case "Fear":
                    favorChange = -2;
                    reply = "胆小鬼。你在发抖？为什么要怕我？难道我对你还不够好吗？";
                    break;
                case "Obsession":
                    favorChange = 3;
                    reply = "（她脸红了，呼吸变得急促）\n这就对了...依赖我，离不开我。除了我，没人能治好你。";
                    break;
                case "Others":
                    favorChange = -5;
                    reply = "其他人？为什么要在我们独处的时候提那些无关紧要的垃圾？\n闭嘴，否则我就缝上你的嘴。";
                    break;
            }

            if (favorChange != 0) FavorabilitySystem.IncreaseFavorability(NPCID.Nurse, favorChange);
            ui.ClearButtons();
            SetupButtons(npc, ui);
            ui.SetDialogueText(reply);
        }

        private void HandleSpecialCare(GalgameDialogueUI ui, int favor)
        {
            Player player = Main.LocalPlayer;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item5);
            player.AddBuff(BuffID.Regeneration, 3600);
            player.AddBuff(BuffID.Ironskin, 3600);
            player.AddBuff(BuffID.Lifeforce, 3600);
            if (favor >= 100) player.AddBuff(BuffID.Endurance, 3600);

            string text = favor < 90 ? "躺好。这是我的特制药剂，别在外面死了。" : "（她温柔地把针头刺入你的血管）\n看着红色的液体流进你的身体...去吧，亲爱的，现在我流淌在你身体里了。";
            for (int i = 0; i < 20; i++) Dust.NewDust(player.position, player.width, player.height, DustID.HeartCrystal);
            ui.SetDialogueText(text);
        }

        private void HandlePleasure(GalgameDialogueUI ui, NPC npc)
        {
            Player player = Main.LocalPlayer;
            player.statLife = player.statLifeMax2; player.HealEffect(player.statLifeMax2);
            npc.life = npc.lifeMax; npc.HealEffect(npc.lifeMax);

            for (int i = 0; i < 50; i++)
            {
                Vector2 speed = Main.rand.NextVector2Circular(5f, 5f);
                Dust.NewDust(player.position, player.width, player.height, DustID.HeartCrystal, speed.X, speed.Y);
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);

            ui.SetDialogueText("（她紧紧拥抱了你，你能感受到她剧烈的心跳）\n啊...就是这种感觉。\n我们的生命连接在一起了。");
            FavorabilitySystem.RecordInteraction(npc.type);
        }
    }
}