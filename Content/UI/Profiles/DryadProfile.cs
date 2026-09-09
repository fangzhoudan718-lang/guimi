using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using System.Collections.Generic;
using System.Linq;
using System;
using zhashi.Content.UI;

namespace zhashi.Content.UI.Profiles
{
    public class DryadProfile : GalgameProfile
    {
        // --- 视觉风格：自然与生机 ---
        public override float OffsetX => -110f; // 略微靠左
        public override float OffsetY => 120f;   // 稍微沉一点，贴合地面

        public override Color BackgroundColor => new Color(20, 40, 20) * 0.98f; // 深森林绿
        public override Color BorderColor => new Color(80, 200, 80) * 0.8f;     // 嫩叶绿
        public override Color NameColor => Color.LimeGreen;

        public override string HeadTexture => "zhashi/Content/UI/Portraits/Dryad_Head";
        public override string StandingTexture => "zhashi/Content/UI/Portraits/Dryad_Standing";

        private string RandomLine(params string[] lines) => lines[Main.rand.Next(lines.Length)];

        // --- 核心对话逻辑：感知世界状态 ---
        public override string GetDialogue(NPC npc)
        {
            Player player = Main.LocalPlayer;
            int favor = GetFavor(npc.type);

            int evilTiles = WorldGen.tEvil + WorldGen.tBlood;
            int totalTiles = Main.maxTilesX * Main.maxTilesY;
            float pollutionRatio = (float)evilTiles / (totalTiles * 0.2f); // 0.2是一个经验系数，让百分比更敏感

            // 1. 特殊环境
            if (Main.bloodMoon)
                return "今晚...大自然在愤怒地咆哮。你能听到根须在地下撕裂的声音吗？保护好自己，自然的勇士。";

            if (Main.eclipse)
                return "阳光被遮蔽了...植物们在恐惧中瑟瑟发抖。这种黑暗不仅来自天空，也来自大地深处。";

            if (player.ZoneCorrupt || player.ZoneCrimson)
                return "（她脸色苍白，捂着胸口）\n这里...好痛。大地在流脓。求求你，带我离开这里，或者...净化它。";

            if (player.ZoneHallow)
                return "虽然神圣之地也是一种极端的平衡，但这里的彩虹和独角兽...至少比腐烂的气味让人安心，对吗？";

            // 2. 污染程度判定对话
            // 严重污染
            if (pollutionRatio > 0.2f)
                return RandomLine(
                    "（她看起来很虚弱，眼神中充满悲伤）\n世界正在被吞噬...我能感觉到每一寸土壤的哀嚎。\n我们是不是...已经来不及了？",
                    "太多了...腐败蔓延得太快了。我的力量正在衰退。勇士，请帮帮这个世界。"
                );

            // 中度污染
            if (pollutionRatio > 0.05f)
                return RandomLine(
                    "我们还有很多工作要做。那些邪恶的根须依然在试图刺穿地心。\n但我相信你，你不会放弃我们的家园，对吗？",
                    "只要还有一颗种子发芽，希望就还在。让我们继续战斗吧，为了恢复平衡。"
                );

            // 3. 好感度与纯净判定
            if (favor >= 100)
                return RandomLine(
                    "（她温柔地注视着你，周围似乎有花朵绽放）\n看着这充满生机的世界...这一切都是因为你。\n你不仅拯救了泰拉瑞亚，也拯救了我的心。",
                    "只要你在身边，我就感觉像是沐浴在春日的暖阳里。\n你是大自然的奇迹，亲爱的。",
                    "累了吗？来我的怀里休息一会儿吧。这里只有风的声音和草的清香。"
                );

            return RandomLine(
                "大自然总是能找到出路...但有时候，它需要一点帮助。\n也就是你和我。",
                "你能感觉到风的流动吗？它在诉说着世界各地的故事。",
                "请善待这个世界，它也会善待你。"
            );
        }

        // --- 按钮逻辑 ---
        public override void SetupButtons(NPC npc, GalgameDialogueUI ui)
        {
            int favor = GetFavor(npc.type);

            ui.AddButton("商店", () => {
                // 1. 打开背包
                Main.playerInventory = true;

                // 2. 锁定 NPC
                Main.LocalPlayer.SetTalkNPC(npc.whoAmI);

                // 3. ★★★ 关键修复：给原版 UI 塞一句话，防止对话框塌陷 ★★★
                Main.npcChatText = "看看你需要些什么？";

                // 4. 设置商店索引
                Main.SetNPCShopIndex(1);

                // 5. 关闭模组 UI，切回原版
                ModContent.GetInstance<GalgameUISystem>().CloseUI();
            });

            // 1. 核心机制：汇报净化进度 (刷好感的主要途径)
            ui.AddButton("汇报净化进度", () => CheckWorldStatus(ui, npc));

            // 2. 自然赐福 (Buff)
            ui.AddButton("请求自然赐福", () => GiveNatureBlessing(ui, favor));

            // 3. 闲聊
            ui.AddButton("闲聊", () => SetupChatMenu(npc, ui));

            // 4. 送礼
            if (FavorabilitySystem.CanGiftToday(NPCID.Dryad))
                ui.AddButton("赠送礼物", () => HandleGiftGiving(ui, npc));
            else
                ui.AddButton("赠送礼物 (上限)", () => ui.SetDialogueText("（她微笑着接过你手中的花瓣）\n谢谢你的心意，但我今天已经收到了太多的爱。\n把这些留给需要它的土地吧。"));

            // 5. 欢愉 (需要 100 好感度)
            if (favor >= 100)
            {
                ui.AddButton("【欢愉】", () => HandlePleasure(ui, npc));
            }

            ui.AddButton("离开", () => {
                Main.LocalPlayer.SetTalkNPC(-1);
                ModContent.GetInstance<GalgameUISystem>().CloseUI();
            });
        }

        // ==========================================
        // ★ 核心玩法：净化检测 ★
        // ==========================================
        private void CheckWorldStatus(GalgameDialogueUI ui, NPC npc)
        {
            // 获取世界腐化/猩红方块数量
            // 注意：WorldGen.tEvil 等变量在联机模式下可能不是实时更新的，这里做简单处理
            int badTiles = WorldGen.tEvil + WorldGen.tBlood;

            // 简单的评分逻辑
            // 假设 20000 格以下算比较干净，5000 格以下算非常干净
            // 实际游戏中这个数值可能很大，需要根据地图大小调整，这里仅作演示逻辑

            string statusText = "";
            int bonusFavor = 0;

            if (badTiles == 0)
            {
                statusText = "（她难以置信地看着你，眼含热泪）\n真的吗？一点邪恶的气息都没有了？\n这简直是奇迹...你做到了，你真的净化了这个世界。\n（她激动地抱住了你）";
                bonusFavor = 10; // 巨额加分
            }
            else if (badTiles < 8000) // 非常干净
            {
                statusText = "做得太好了！虽然还有零星的污染，但大自然已经重新夺回了主导权。\n我也感觉好多了，谢谢你为这个世界做的一切。";
                bonusFavor = 3;
            }
            else if (badTiles < 30000) // 一般
            {
                statusText = "我们取得了一些进展，但我依然能听到远处土地的哀鸣。\n不能松懈，勇士，邪恶随时可能卷土重来。";
                bonusFavor = 1;
            }
            else // 很脏
            {
                statusText = "（她痛苦地摇了摇头）\n不行...还不够。污染依然在蔓延。\n如果你不加快速度，我们都会被吞噬的。";
                bonusFavor = 0;
            }

            // 每天只能通过汇报加一次分，防止无限刷
            if (bonusFavor > 0)
            {
                if (FavorabilitySystem.CanChatRewardToday(NPCID.Dryad)) // 复用聊天奖励的每日限制
                {
                    FavorabilitySystem.IncreaseFavorability(NPCID.Dryad, bonusFavor);
                    FavorabilitySystem.RecordChatReward(NPCID.Dryad);
                    statusText += $"\n[好感度 +{bonusFavor}]";
                }
                else
                {
                    statusText += "\n（你今天已经汇报过了，明天再来吧）";
                }
            }

            // 刷新界面显示好感度变化
            ui.ClearButtons();
            SetupButtons(npc, ui);
            ui.SetDialogueText(statusText);
        }

        // ==========================================
        // ★ 自然赐福 (Buff) ★
        // ==========================================
        private void GiveNatureBlessing(GalgameDialogueUI ui, int favor)
        {
            Player player = Main.LocalPlayer;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29); // 魔法声音

            // 给予树妖祝福 Buff (防御 + 反伤 + 回血)
            player.AddBuff(BuffID.DryadsWard, 36000); // 10分钟

            for (int i = 0; i < 30; i++)
            {
                Dust.NewDust(player.position, player.width, player.height, DustID.DryadsWard, 0, 0, 0, default, 1.5f);
            }

            string text = "";
            if (favor >= 80)
                text = "（她轻吻了你的额头，一股暖流涌遍全身）\n愿大自然的灵气护佑你。无论走到哪里，我的祝福都与你同在。";
            else
                text = "自然会保护它的守护者。带上这个祝福，去战斗吧。";

            ui.SetDialogueText(text);
        }

        // ==========================================
        // ★ 欢愉系统 (天人合一) ★
        // ==========================================
        private void HandlePleasure(GalgameDialogueUI ui, NPC npc)
        {
            Player player = Main.LocalPlayer;

            // 恢复状态
            player.statLife = player.statLifeMax2; player.HealEffect(player.statLifeMax2);
            npc.life = npc.lifeMax; npc.HealEffect(npc.lifeMax);

            // 独特的自然特效：树叶与光点
            for (int i = 0; i < 60; i++)
            {
                Vector2 speed = Main.rand.NextVector2Circular(6f, 6f);
                Dust d = Dust.NewDustPerfect(player.Center, DustID.DryadsWard, speed, 0, default, 2f);
                d.noGravity = true;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);

            // 欢愉文本：强调精神与自然的融合
            string text = RandomLine(
                "（藤蔓轻轻缠绕着你们，周围的花朵在一瞬间全部盛开）\n闭上眼睛...感受到了吗？我们的灵魂正在与大地的脉搏共振。\n此刻，你就是自然，我就是你...我们是永恒的。",
                "（她身上散发出迷人的草木清香，将你紧紧拥入怀中）\n不需要言语...让生命力在我们之间流淌。\n这就对了...这种合二为一的感觉，比任何魔法都要美妙。"
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
                ui.SetDialogueText("（她温柔地握住你的空手）\n只要是你来的，哪怕只是带着风，也是最好的礼物。");
                return;
            }

            string reply = "";
            int favorChange = 0;
            bool success = false;

            // 1. 真爱之心 (秒满)
            if (item.type == ModContent.ItemType<Items.TrueLoveHeart>())
            {
                FavorabilitySystem.SetFavorability(NPCID.Dryad, 100);
                item.stack--; if (item.stack <= 0) item.TurnToAir();
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4);
                FavorabilitySystem.RecordGift(NPCID.Dryad);

                ui.ClearButtons(); SetupButtons(npc, ui);
                ui.SetDialogueText("（这颗心在她手中化作了无数飞舞的萤火虫，环绕在你们身边）\n我听到了...这是爱的回响。\n既然你把心交给了我，那我也将我的灵魂，以及这个世界所有的温柔，都许诺给你。");
                return;
            }

            // 2. 喜欢：种子、植物、自然相关物品
            if (item.type == ItemID.Daybloom || item.type == ItemID.Moonglow || item.type == ItemID.GrassSeeds || item.type == ItemID.Sunflower)
            {
                favorChange = 3;
                reply = $"啊，多么可爱的{item.Name}！\n我会把它种在最肥沃的土壤里，让它见证我们的友谊。";
                success = true;
            }
            // 3. 讨厌：腐化/猩红物品、火把、爆炸物
            else if (item.type == ItemID.RottenChunk || item.type == ItemID.Vertebrae || item.type == ItemID.Dynamite)
            {
                favorChange = -5;
                reply = "（她厌恶地退后了一步，周围的植物也枯萎了）\n快拿走！你怎么能把这种充满了死亡和毁灭气息的东西带给我？\n这让我感到恶心...";
                success = false; // 不收垃圾
            }
            // 4. 普通物品
            else
            {
                favorChange = 1;
                reply = "谢谢你。虽然这对我来说没什么用，但我感受到了你的善意。";
                success = true;
            }

            if (favorChange != 0)
            {
                if (success)
                {
                    item.stack--; if (item.stack <= 0) item.TurnToAir();
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4);
                    FavorabilitySystem.RecordGift(NPCID.Dryad);
                }
                FavorabilitySystem.IncreaseFavorability(NPCID.Dryad, favorChange);
                ui.ClearButtons(); SetupButtons(npc, ui);
            }
            ui.SetDialogueText(reply);
        }

        // ==========================================
        // ★ 闲聊系统 ★
        // ==========================================
        private void SetupChatMenu(NPC npc, GalgameDialogueUI ui)
        {
            ui.ClearButtons();
            var chatOptions = new List<KeyValuePair<string, string>>();
            chatOptions.Add(new KeyValuePair<string, string>("Nature", "询问自然"));
            chatOptions.Add(new KeyValuePair<string, string>("Corruption", "询问腐化"));
            chatOptions.Add(new KeyValuePair<string, string>("Age", "询问年龄")); // 作死选项

            foreach (var option in chatOptions)
                ui.AddButton(option.Value, () => HandleChatResponse(option.Key, ui, npc));

            ui.AddButton("<< 返回", () => {
                ui.ClearButtons();
                SetupButtons(npc, ui);
                ui.SetDialogueText("（她正在修剪一株盆栽的枝叶）");
            });
            ui.SetDialogueText("你想了解关于这个世界的什么秘密？");
        }

        private void HandleChatResponse(string type, GalgameDialogueUI ui, NPC npc)
        {
            string reply = "";
            int favorChange = 0;

            switch (type)
            {
                case "Nature":
                    reply = "每一棵树都有灵魂，每一朵花都在歌唱。\n如果你静下心来倾听，你会发现世界并不孤独。";
                    favorChange = 1;
                    break;
                case "Corruption":
                    reply = "那是大地的癌症。它不仅吞噬土地，也吞噬灵魂。\n我们必须时刻保持警惕，否则一切美好都会化为乌有。";
                    favorChange = 1;
                    break;
                case "Age":
                    reply = "（她温和的笑容瞬间凝固了）\n...这是一个非常、非常不礼貌的问题。\n勇士，有些秘密还是让它像树木的年轮一样深埋起来比较好。";
                    favorChange = -2;
                    break;
            }

            // 简单加分限制逻辑：闲聊每天第一次给分，后面不给
            if (favorChange > 0)
            {
                if (!FavorabilitySystem.CanChatRewardToday(NPCID.Dryad)) favorChange = 0;
                else FavorabilitySystem.RecordChatReward(NPCID.Dryad);
            }

            if (favorChange != 0) FavorabilitySystem.IncreaseFavorability(NPCID.Dryad, favorChange);

            ui.SetDialogueText(reply);
        }
    }
}