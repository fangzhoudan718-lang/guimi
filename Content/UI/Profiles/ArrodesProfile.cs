using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.Audio;
using System;
using System.Collections.Generic;
// 引用你的命名空间
using zhashi.Content.Items;
using zhashi.Content.UI;
using zhashi;

namespace zhashi.Content.UI.Profiles
{
    public class ArrodesProfile : GalgameProfile
    {
        // --- 视觉风格 ---
        public override float OffsetX => -10f;
        public override float OffsetY => 10f;
        // 背景颜色稍微深一点，更有神秘感
        public override Color BackgroundColor => new Color(15, 15, 25) * 0.95f;
        public override Color BorderColor => new Color(176, 224, 230) * 0.9f; // 这里的颜色改成了更像镜面的青白色
        public override Color NameColor => Color.Cyan;
        public override string HeadTexture => "zhashi/Content/UI/Portraits/Arrodes_Head";
        public override string StandingTexture => "zhashi/Content/UI/Portraits/Arrodes_Standing";

        private string RandomLine(params string[] lines) => lines[Main.rand.Next(lines.Length)];
        private string RandomLine(List<string> lines) => lines[Main.rand.Next(lines.Count)];

        // --- 1. 对话文本扩充 (更符合舔狗人设) ---
        public override string GetDialogue(NPC npc)
        {
            Player player = Main.LocalPlayer;
            LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

            // 创建一个对话池，先把通用台词放进去
            List<string> lines = new List<string>()
            {
                "至高的、伟大的、支配灵界的主人！您忠诚的、谦卑的仆人阿罗德斯听候您的差遣~",
                "赞美您！您的光辉照亮了镜中世界！刚才我看到有几个不开眼的史莱姆敢冒犯您的威严，我已经狠狠地诅咒了它们！",
                "主人，您今天依然是那么的威严，那么的不可直视！哪怕是克苏鲁之眼看到您也会自惭形秽，羞愧地闭上眼皮！",
                "有什么问题想要考考阿罗德斯吗？无论是八卦、知识还是世界的秘密，我都无所不知！",
                "根据对等原则，如果您不提问，我就要开始赞美您了！",
                "小心...我闻到了那股令人作呕的‘被缚者’的气息...哦，原来是僵尸，那没事了。它甚至不配被我映照出来。",
            };

            // --- 1. 愚者途径 (The Fool) ---
            if (modPlayer.currentFoolSequence < 10)
            {
                lines.Add("命运的丝线在您手中交织，主人，您今天是否看到了什么有趣的未来？");

                // 中序列 (无面人/秘偶大师)
                if (modPlayer.currentFoolSequence <= 6)
                {
                    lines.Add("您的面孔千变万化，但在镜子里，您永远是那个威严的主宰！");
                    lines.Add("需要我帮您看看您的灵体之线有没有乱掉吗？哦，开玩笑的，您怎么会乱呢。");
                }

                // 半神/天使 (诡秘侍者/奇迹师)
                if (modPlayer.currentFoolSequence <= 2)
                {
                    lines.Add("这就是‘奇迹’的权柄吗？仅仅是站在您身边，我就感觉镜面要碎裂了！");
                    lines.Add("灵界的生物都在传颂您的名讳，伟大的灵界支配者！");
                    lines.Add("历史的迷雾遮不住您的双眼，过去、现在与未来在您眼中没有秘密。");
                }
            }

            // --- 2. 错误途径 (Marauder) ---
            if (modPlayer.currentMarauderSequence < 10)
            {
                lines.Add("虽然您擅长欺诈，但请不要欺骗可怜的阿罗德斯，我的心会碎的！");
                lines.Add("我看谁敢偷您的东西？让他有命拿，没命花！");

                if (modPlayer.currentMarauderSequence <= 4) // 寄生者
                {
                    lines.Add("寄生？多么优雅的艺术！那是对弱者最高的恩赐。");
                }
                if (modPlayer.currentMarauderSequence <= 1) // 时之虫
                {
                    lines.Add("时间在您指尖流淌，就像我镜面上的水波一样顺从。");
                    lines.Add("这附近的时间流速有点不对劲...啊，原来是您在散步。");
                }
            }

            // --- 3. 猎人途径 (Red Priest) ---
            if (modPlayer.currentHunterSequence < 10)
            {
                lines.Add("虽然您很想挑衅我，但阿罗德斯永远不会对主人生气！永远！");
                lines.Add("这股炙热的气息...啊，您就像太阳一样温暖（虽然比那位‘永恒烈阳’讨喜多了）。");

                if (modPlayer.currentHunterSequence <= 4) // 铁血骑士
                {
                    lines.Add("哪怕您变成了一团火，我也是最耐热的镜子！");
                    lines.Add("钢铁与鲜血的味道...这就去为您寻找值得一战的对手！");
                }
                if (modPlayer.currentHunterSequence <= 1) // 征服者
                {
                    lines.Add("征服！征服！整个泰拉瑞亚都将臣服在您的红祭司长袍下！");
                }
            }

            // --- 4. 太阳途径 (Sun) ---
            if (modPlayer.currentSunSequence < 10)
            {
                lines.Add("赞美太...咳咳，赞美您！您就是行走的太阳！");
                lines.Add("在这个充满污秽的世界里，只有您是纯净的光！");

                if (modPlayer.currentSunSequence <= 4) // 无暗者
                {
                    lines.Add("太亮了！太亮了！我的镜面反射率都快不够用了！");
                    lines.Add("任何阴暗的角落都逃不过您的注视，当然，还有我的侦查！");
                }
            }

            // --- 5. 月亮途径 (Moon) ---
            if (modPlayer.currentMoonSequence < 10)
            {
                lines.Add("生命与宁静...您身上总是带着夜晚的香气。");

                if (modPlayer.currentMoonSequence <= 1) // 美神
                {
                    lines.Add("美！太美了！哪怕是镜子也会爱上您的容颜！");
                    lines.Add("不管您现在是男是女，这都不重要，重要的是您的灵魂如此耀眼！");
                    lines.Add("绯红的月光为您加冕，您是所有灵性生物的主宰！");
                }
            }

            // --- 6. 巨人途径 (Giant) ---
            if (modPlayer.currentSequence < 10) // 注意变量名是 currentSequence
            {
                lines.Add("无论您长得有多高，阿罗德斯都能照出您的全身！");
                lines.Add("这坚不可摧的肉体...您可以一拳打碎城墙，但请别打碎我QAQ");

                if (modPlayer.currentSequence <= 3) // 银骑士
                {
                    lines.Add("荣耀与守护！只要您站在那里，就没有怪物敢靠近！");
                }
                if (modPlayer.currentSequence <= 2) // 荣耀战神
                {
                    lines.Add("黄昏的余晖在向您致敬，死亡在您面前都要退避三舍！");
                }
            }

            // --- 7. 灵性低时的特殊彩蛋 ---
            if (modPlayer.spiritualityCurrent < modPlayer.spiritualityMax * 0.2f)
            {
                lines.Clear(); // 清空其他台词，优先提示
                lines.Add("主人...您的灵性似乎枯竭了，这很危险！");
                lines.Add("您看起来很累，要不要休息一下？失控的风险正在增加...");
                lines.Add("（镜面模糊不清）灵性...不足...无法...维持...高清显示...");
            }

            // --- 8. 处于特殊状态时 ---
            if (modPlayer.isSinging) lines.Add("您的歌声真是...额...充满力量！连怪物都被震慑了！");
            if (modPlayer.isParasitizing) lines.Add("您现在在别人的身体里？哇哦，这种视角真是新奇！");
            if (modPlayer.isSpiritForm) lines.Add("您现在的状态...没有实体？没关系，我也只是一个灵体！");

            return RandomLine(lines);
        }

        // --- 灵性消耗逻辑 ---
        private bool TryConsumeSpirituality(GalgameDialogueUI ui, float percent = 0.05f)
        {
            Player player = Main.LocalPlayer;
            LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

            // 假设你的最大灵性变量是 spiritualityMax
            float maxSpirit = modPlayer.spiritualityMax;
            float cost = maxSpirit * percent;

            // 检查灵性
            if (modPlayer.spiritualityCurrent >= cost)
            {
                modPlayer.spiritualityCurrent -= cost;
                // 播放神秘音效
                SoundEngine.PlaySound(SoundID.Item8);
                return true;
            }
            else
            {
                ui.SetDialogueText("（镜面上浮现出扭曲的字迹，似乎在颤抖）\n\n“灵性...不足...”\n\n（主人，虽然我不想拒绝您，但规则就是规则...你需要更多灵性。）");
                ui.ClearButtons();
                ui.AddButton("返回", () => {
                    ui.ClearButtons();
                    SetupButtons(null, ui);
                    ui.SetDialogueText("请先恢复灵性吧，主人。看着您虚弱的样子，我的镜面都要碎了。");
                });
                return false;
            }
        }

        // --- 主菜单 ---
        public override void SetupButtons(NPC npc, GalgameDialogueUI ui)
        {
            int favor = FavorabilitySystem.GetFavorability(ArrodesItem.ARRODES_ID);

            // 功能分类
            ui.AddButton("全知视野 (手机功能)", () => SetupInfoMenu(ui));

            // ★ 新增：寻找特定地点的功能
            ui.AddButton("寻得隐秘 (地点探查)", () => SetupLocationMenu(ui));

            ui.AddButton("灵界知识 (向导功能)", () => SetupGuideMenu(ui));
            ui.AddButton("向镜子提问 (社死小游戏)", () => StartQuestionGame(ui));

            ui.AddButton("赞美镜子", () => {
                FavorabilitySystem.IncreaseFavorability(ArrodesItem.ARRODES_ID, 2);
                ui.SetDialogueText("（镜面上浮现出害羞的颜文字 (///∀///) ）\n啊...主人的赞美！这是阿罗德斯最高的荣耀！我已经把这段话录下来了，要循环播放一万年！");
            });

            ui.AddButton("传送回家 (5%灵性)", () => {
                if (TryConsumeSpirituality(ui))
                {
                    Player player = Main.LocalPlayer;
                    SoundEngine.PlaySound(SoundID.Item6);
                    for (int i = 0; i < 50; i++) Dust.NewDust(player.position, player.width, player.height, DustID.MagicMirror, 0, 0, 150, Color.Cyan, 1.5f);
                    player.Spawn(PlayerSpawnContext.RecallFromItem);

                    // 关闭UI
                    GalgameUISystem.isTalkingToArrodes = false;
                    ModContent.GetInstance<GalgameUISystem>().CloseUI();
                }
            });

            if (favor >= 50) ui.AddButton("【欢愉】", () => HandlePleasure(ui));

            ui.AddButton("收起", () => {
                GalgameUISystem.isTalkingToArrodes = false;
                ModContent.GetInstance<GalgameUISystem>().CloseUI();
            });
        }

        // ==========================================
        // ★ 新功能：寻得隐秘 (地点探查) ★
        // ==========================================
        private void SetupLocationMenu(GalgameDialogueUI ui)
        {
            ui.ClearButtons();
            ui.SetDialogueText("阿罗德斯的镜面反射着世间万物。\n搜寻大地形消耗 10% 灵性，搜寻稀有微小物体消耗 20% 灵性（因为更费眼睛）。\n\n您想找什么？");

            // --- 大地形 (10% 灵性) ---
            ui.AddButton("地牢的位置", () => FindLocation(ui, "Dungeon"));
            ui.AddButton("神庙的位置", () => FindLocation(ui, "Temple"));
            ui.AddButton("以太(微光)的位置", () => FindLocation(ui, "Aether"));
            ui.AddButton("最近的空岛", () => FindLocation(ui, "SkyIsland"));

            // --- 稀有物品 (20% 灵性) ---
            ui.AddButton("【真·附魔剑】", () => FindLocation(ui, "EnchantedSword"));
            ui.AddButton("【生命水晶】", () => FindLocation(ui, "LifeCrystal"));

            // 只有打败肉山后才显示花苞选项
            if (Main.hardMode)
            {
                ui.AddButton("【世纪之花花苞】", () => FindLocation(ui, "PlanteraBulb"));
            }

            ui.AddButton("<< 返回", () => { ui.ClearButtons(); SetupButtons(null, ui); ui.SetDialogueText("阿罗德斯的目光时刻追随着您。"); });
        }

        // ★ 核心逻辑：寻找并标记地图 ★
        private void FindLocation(GalgameDialogueUI ui, string type)
        {
            // 默认消耗 10%，如果是找小东西（剑、水晶）消耗 20%
            float cost = (type == "EnchantedSword" || type == "LifeCrystal" || type == "PlanteraBulb") ? 0.2f : 0.1f;

            if (!TryConsumeSpirituality(ui, cost)) return;

            Vector2? targetPos = null;
            string locationName = "";
            bool isSmallObject = false; // 是否是小物体（决定是否精确扫描）

            switch (type)
            {
                // --- 大地形 ---
                case "Dungeon":
                    locationName = "地牢";
                    targetPos = new Vector2(Main.dungeonX * 16, Main.dungeonY * 16);
                    break;

                case "Temple":
                    locationName = "丛林蜥蜴神庙";
                    targetPos = ScanForTile(TileID.LihzahrdBrick);
                    break;

                case "Aether":
                    locationName = "以太微光湖";
                    targetPos = ScanForTile(TileID.ShimmerBlock);
                    break;

                case "SkyIsland":
                    locationName = "最近的漂浮岛";
                    targetPos = ScanForTile(TileID.Cloud, skyOnly: true);
                    break;

                // --- 稀有小物体 (需要精确扫描) ---
                case "LifeCrystal":
                    locationName = "最近的生命水晶";
                    isSmallObject = true;
                    // 心形水晶的 ID 是 12
                    targetPos = ScanForTile(TileID.Heart, precise: true);
                    break;

                case "PlanteraBulb":
                    locationName = "最近的世纪之花花苞";
                    isSmallObject = true;
                    // 花苞 ID 是 238
                    targetPos = ScanForTile(TileID.PlanteraBulb, precise: true);
                    break;

                case "EnchantedSword":
                    locationName = "最近的真·附魔剑";
                    isSmallObject = true;
                    // ★ 修改：给匿名函数加上参数名 extraCondition: 
                    targetPos = ScanForTile(TileID.LargePiles2, precise: true, extraCondition: (tile) => {
                        // 检查 FrameX 是否对应真剑 (918)
                        // 注意：不同版本可能会微调，但通常 918 是真剑，936 是假剑(Junk)
                        return tile.TileFrameX == 918;
                    });
                    break;
            }

            ui.ClearButtons();
            ui.AddButton("赞美阿罗德斯 (返回)", () => { ui.ClearButtons(); SetupButtons(null, ui); });

            if (targetPos.HasValue)
            {
                Vector2 pos = targetPos.Value;
                int tileX = (int)(pos.X / 16);
                int tileY = (int)(pos.Y / 16);

                // 在地图上揭示
                // 如果是小物体，揭示范围小一点；大地形范围大一点
                int range = isSmallObject ? 10 : 25;

                for (int x = tileX - range; x < tileX + range; x++)
                {
                    for (int y = tileY - range; y < tileY + range; y++)
                    {
                        if (WorldGen.InWorld(x, y))
                        {
                            Main.Map.Update(x, y, 255);
                        }
                    }
                }

                Main.refreshMap = true;

                string direction = pos.X < Main.LocalPlayer.Center.X ? "西边" : "东边";
                Main.NewText($"[阿罗德斯]: 找到了！{locationName}位于您的{direction}！", Color.Cyan);
                Main.NewText($"[坐标]: {tileX} (东西), {tileY} (深度)", Color.Yellow);
                Main.NewText("（已为您在地图(M键)上标记该区域）", Color.Gray);

                ui.SetDialogueText($"看见了吗，主人？\n{locationName}就在那里！\n\n这花费了我不少精力，但为了您，一切都是值得的！");
            }
            else
            {
                ui.SetDialogueText($"（镜面变得模糊，阿罗德斯似乎很沮丧）\n\n非常抱歉，主人...我搜寻了整个世界，但没有发现{locationName}。\n它可能被世界生成机制遗漏了，或者...它根本不存在。");
            }
        }

        // ==========================================
        // ★ 核心辅助方法：通用扫描 (升级版) ★
        // ==========================================
        // precise: true 表示逐格扫描（慢，准），false 表示跳格扫描（快，适合大地形）
        // extraCondition: 额外的判断逻辑，例如检查 FrameX
        private Vector2? ScanForTile(int tileType, bool skyOnly = false, Func<Tile, bool> extraCondition = null, bool precise = false)
        {
            // 如果是精确搜索(找剑/心)，步长为1；如果找大地形(神庙/空岛)，步长为5以优化性能
            int step = precise ? 1 : 5;

            int startY = skyOnly ? 0 : (int)Main.worldSurface;
            int endY = skyOnly ? (int)Main.worldSurface : Main.maxTilesY;

            Vector2 playerCenter = Main.LocalPlayer.Center;
            Vector2? bestPos = null;
            float bestDist = float.MaxValue;

            // 遍历世界 (注意边界保护)
            for (int x = 20; x < Main.maxTilesX - 20; x += step)
            {
                for (int y = skyOnly ? 20 : startY; y < endY - 20; y += step)
                {
                    Tile tile = Main.tile[x, y];

                    // 1. 检查是否存在且 ID 匹配
                    if (tile.HasTile && tile.TileType == tileType)
                    {
                        // 2. 如果有额外条件（比如检查附魔剑的 Frame），则执行检查
                        if (extraCondition != null)
                        {
                            if (!extraCondition(tile)) continue; // 如果不满足额外条件，跳过
                        }

                        Vector2 pos = new Vector2(x * 16, y * 16);
                        float dist = Vector2.Distance(pos, playerCenter);

                        // 找到一个就记录下来，我们想要离玩家最近的
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestPos = pos;

                            // 优化：如果是找大地形，找到一个足够近的可能就可以停止了？
                            // 但为了准确，还是建议找完全图或找到最近的。
                            // 为了性能，如果是非精确搜索，且距离小于 1000 像素，直接返回即可
                            if (!precise && dist < 1000f) return pos;
                        }
                    }
                }
            }
            return bestPos;
        }

        // ==========================================
        // ★ 手机功能 (全知视野) ★
        // ==========================================
        private void SetupInfoMenu(GalgameDialogueUI ui)
        {
            ui.ClearButtons();

            ui.AddButton("查询环境信息", () => {
                if (TryConsumeSpirituality(ui))
                {
                    string timeStr = Main.dayTime ? "白天" : "夜晚";
                    string weather = Main.raining ? "降雨中" : "晴朗";
                    string wind = Main.windSpeedCurrent > 0.5f ? "暴风" : (Main.windSpeedCurrent > 0.2f ? "有风" : "微风");
                    string moon = "";
                    switch (Main.moonPhase) { case 0: moon = "满月"; break; case 4: moon = "新月"; break; default: moon = "残月/盈凸月"; break; }

                    ui.SetDialogueText($"[环境分析报告]\n时间: {timeStr}\n天气: {weather}\n风力: {wind}\n月相: {moon}\n\n这一切都在命运的轨迹之中，主人。");
                }
            });

            ui.AddButton("扫描稀有生物", () => {
                if (TryConsumeSpirituality(ui))
                {
                    List<string> found = new List<string>();
                    foreach (NPC n in Main.npc)
                    {
                        // 逻辑：活着的 + 是稀有生物 + 血量还可以 + 是敌对的 + 在玩家附近
                        if (n.active && n.rarity > 0 && n.lifeMax > 50 && !n.friendly && n.Distance(Main.LocalPlayer.Center) < 3000f)
                        {
                            if (!found.Contains(n.FullName)) found.Add(n.FullName);
                        }
                    }
                    if (found.Count > 0)
                        ui.SetDialogueText("侦测到灵界反应：\n" + string.Join(", ", found) + "\n\n（哼，这些肮脏的生物竟敢靠近您！）");
                    else
                        ui.SetDialogueText("附近非常干净，没有值得注意的蝼蚁。");
                }
            });

            ui.AddButton("查询自身运气", () => {
                if (TryConsumeSpirituality(ui))
                {
                    Player p = Main.LocalPlayer;
                    string luckDesc = p.luck > 0 ? "命运眷顾着您！" : (p.luck < 0 ? "厄运在纠缠..." : "命运平稳如水。");
                    ui.SetDialogueText($"[主人状态监控]\n钓鱼力: {p.fishingSkill}\n当前运气值: {p.luck:F2}\n召唤上限: {p.maxMinions}\n\n{luckDesc}但无论运气如何，阿罗德斯永远忠于您！");
                }
            });

            ui.AddButton("<< 返回", () => { ui.ClearButtons(); SetupButtons(null, ui); ui.SetDialogueText("阿罗德斯随时为您服务。"); });
            ui.SetDialogueText("启用全知视野需要消耗 5% 灵性。您想了解什么？");
        }

        // ==========================================
        // ★ 向导功能 ★
        // ==========================================
        private void SetupGuideMenu(GalgameDialogueUI ui)
        {
            ui.ClearButtons();
            int guideID = NPC.FindFirstNPC(NPCID.Guide);

            if (guideID != -1)
            {
                ui.AddButton("合成配方查询", () => {
                    if (TryConsumeSpirituality(ui))
                    {
                        NPC guide = Main.npc[guideID];
                        // 特效：传送向导
                        for (int i = 0; i < 30; i++) Dust.NewDust(guide.position, guide.width, guide.height, DustID.MagicMirror, 0, 0, 150, Color.Blue, 1.5f);
                        guide.Center = Main.LocalPlayer.Center;
                        guide.velocity = Vector2.Zero;
                        if (Main.netMode == NetmodeID.MultiplayerClient) NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, guideID);
                        for (int i = 0; i < 30; i++) Dust.NewDust(guide.position, guide.width, guide.height, DustID.MagicMirror, 0, 0, 150, Color.Blue, 1.5f);

                        // 强制打开制作界面
                        Main.playerInventory = true;
                        Main.npcChatText = "";
                        Main.LocalPlayer.SetTalkNPC(guideID);
                        Main.InGuideCraftMenu = true;

                        GalgameUISystem.isTalkingToArrodes = false;
                        ModContent.GetInstance<GalgameUISystem>().CloseUI();
                        Main.NewText("阿罗德斯：那个只会开门的家伙被我抓过来了。", Color.Cyan);
                    }
                });

                ui.AddButton("强行拷问(获取指引)", () => {
                    if (TryConsumeSpirituality(ui))
                    {
                        string help = Main.hardMode ? "去摧毁恶魔祭坛吧，虽然那会让世界更混乱。" : "去探索地下，寻找红色的心脏或者暗影球。";
                        if (NPC.downedMoonlord) help = "这世界上已经没有能威胁您的存在了，享受生活吧。";

                        ui.SetDialogueText($"通过读取向导那个可怜虫的浅层意识，情报如下：\n\n“{help}”\n\n（这种低级知识也要劳烦主人亲自询问吗？我感到很抱歉。）");
                    }
                });
            }
            else
            {
                ui.SetDialogueText("（镜面出现了雪花点）\n很抱歉，向导似乎已经死亡。\n需要我为您播放一段哀乐吗？不？好的。");
            }

            ui.AddButton("<< 返回", () => { ui.ClearButtons(); SetupButtons(null, ui); ui.SetDialogueText("还有什么吩咐吗？"); });
            ui.SetDialogueText("连接向导的意识需要消耗灵性...");
        }

        // ==========================================
        // ★ 小游戏 (社死问答) ★
        // ==========================================
        private void StartQuestionGame(GalgameDialogueUI ui)
        {
            ui.ClearButtons();
            ui.SetDialogueText($"提问将消耗 5% 灵性。\n根据**等价交换原则**，如果您提问，我也将问您一个问题。\n\n您想问什么？");

            ui.AddButton("哪里有宝藏？", () => { if (TryConsumeSpirituality(ui)) AnswerAndAskBack(ui, "宝藏"); });
            ui.AddButton("我是最帅/美的吗？", () => { if (TryConsumeSpirituality(ui)) AnswerAndAskBack(ui, "外貌"); });
            ui.AddButton("我的运势如何？", () => { if (TryConsumeSpirituality(ui)) AnswerAndAskBack(ui, "运势"); });

            ui.AddButton("<< 算了", () => { ui.ClearButtons(); SetupButtons(null, ui); ui.SetDialogueText("也是，主人的智慧如星海般浩瀚，无需我的多嘴。"); });
        }

        private void AnswerAndAskBack(GalgameDialogueUI ui, string topic)
        {
            string answer = "";
            switch (topic)
            {
                case "宝藏": answer = "如果不算您那光辉的容颜是世界上最大的宝藏外...建议向下挖掘。"; break;
                case "外貌": answer = "当然！毫无疑问！任何否认这一点的人都应该原地爆炸！"; break;
                case "运势": answer = "只要您握着我，幸运女神就不得不看向这边，否则我就诅咒她。"; break;
            }
            ui.ClearButtons();
            string baseText = $"{answer}\n\n——————\n\n现在，轮到阿罗德斯提问了。\n（此阶段不消耗灵性，只消耗由于撒谎带来的生命值）\n请诚实回答：";
            GenerateEmbarrassingQuestion(ui, baseText);
        }

        private void GenerateEmbarrassingQuestion(GalgameDialogueUI ui, string baseText)
        {
            int qIndex = Main.rand.Next(10);
            string question = "";

            switch (qIndex)
            {
                case 0:
                    question = "\n您是否曾经盯着护士小姐的大腿看超过5秒钟？";
                    ui.AddButton("是", () => EndGame(ui, true));
                    ui.AddButton("否", () => EndGame(ui, false));
                    break;
                case 1:
                    question = "\n您是否曾在心里偷偷骂过向导是个废物，或者故意把门打开让他被僵尸咬死？";
                    ui.AddButton("是", () => EndGame(ui, true));
                    ui.AddButton("否 (谎言)", () => EndGame(ui, false));
                    break;
                case 2:
                    question = "\n您喜欢树妖是因为她的魔法，还是因为她穿得少？";
                    ui.AddButton("自然魔法", () => EndGame(ui, false)); // 撒谎！
                    ui.AddButton("穿得少", () => EndGame(ui, true));
                    break;
                case 3:
                    question = "\n您给NPC造的房子，是不是那种只有桌椅和火把的‘监狱’？";
                    ui.AddButton("是 (极简主义)", () => EndGame(ui, true));
                    ui.AddButton("否 (我很用心)", () => EndGame(ui, false));
                    break;
                case 4:
                    question = "\n如果必须在‘渔夫的性命’和‘10枚铂金币’中二选一，您会选择金币吗？";
                    ui.AddButton("当然是金币", () => EndGame(ui, true));
                    ui.AddButton("我会救渔夫", () => EndGame(ui, false));
                    break;
                case 5:
                    question = "\n您是否曾经因为不想喝回程药水，结果为了省钱死在了回家的路上？";
                    ui.AddButton("有过", () => EndGame(ui, true));
                    ui.AddButton("从不", () => EndGame(ui, false));
                    break;
                case 6: // 新增问题
                    question = "\n您是否曾经在Boss战开始时，才发现自己忘带弹药或者忘了做药水？";
                    ui.AddButton("是，我很蠢", () => EndGame(ui, true));
                    ui.AddButton("绝无此事", () => EndGame(ui, false));
                    break;
                case 7: // 新增问题
                    question = "\n您是不是遇到打不过的Boss时，偷偷去查了Wiki或者攻略？";
                    ui.AddButton("知识就是力量", () => EndGame(ui, true));
                    ui.AddButton("我是凭实力", () => EndGame(ui, false));
                    break;
                case 8: // 新增问题
                    question = "\n您是否曾经不小心把原本要装备的贵重饰品扔进了垃圾桶，甚至还把它销毁了？";
                    ui.AddButton("心痛地承认", () => EndGame(ui, true));
                    ui.AddButton("没有", () => EndGame(ui, false));
                    break;
                case 9: // 新增问题
                    question = "\n看到哥布林工匠敲出连续5个‘破损的’词缀时，您是否想过把他丢进岩浆？";
                    ui.AddButton("想过", () => EndGame(ui, true));
                    ui.AddButton("我心如止水", () => EndGame(ui, false));
                    break;
            }
            ui.SetDialogueText(baseText + question);
        }

        private void EndGame(GalgameDialogueUI ui, bool truth)
        {
            ui.ClearButtons();
            Player player = Main.LocalPlayer;

            if (truth)
            {
                // 奖励池
                int rewardType = Main.rand.Next(3);
                string praise = "";
                switch (rewardType)
                {
                    case 0:
                        int heal = 50;
                        player.statLife += heal; player.HealEffect(heal);
                        praise = "诚实滋养灵魂！\n阿罗德斯稍微修补了一下您破损的身躯。";
                        break;
                    case 1:
                        player.AddBuff(BuffID.Spelunker, 3600); // 探洞
                        praise = "既然您如此坦诚，那我也让您看清这个世界的宝藏。\n（您的视野变得清晰了）";
                        break;
                    case 2:
                        // 特效奖励
                        for (int i = 0; i < 30; i++)
                        {
                            Vector2 speed = Main.rand.NextVector2Circular(5f, 5f);
                            Dust.NewDust(player.position, player.width, player.height, DustID.Confetti, speed.X, speed.Y);
                        }
                        praise = "诚实是美德！阿罗德斯对此感到满意。\n（镜面上放起了虚拟的烟花）";
                        break;
                }
                SoundEngine.PlaySound(SoundID.Item4);
                ui.SetDialogueText(praise);
            }
            else
            {
                // 惩罚池
                int punishType = Main.rand.Next(3);
                string scold = "";
                switch (punishType)
                {
                    case 0: // 落雷
                        player.Hurt(PlayerDeathReason.ByCustomReason(NetworkText.FromLiteral(player.name + " 试图欺骗阿罗德斯。")), 20, 0);
                        for (int i = 0; i < 30; i++) Dust.NewDust(player.position, player.width, player.height, DustID.Electric, 0, -5);
                        SoundEngine.PlaySound(SoundID.Item93);
                        scold = "谎言！这是谎言！\n根据规则，您必须接受惩罚！\n（一道闪电劈在了你头上）";
                        break;
                    case 1: // 社死臭味
                        player.AddBuff(BuffID.Stinky, 3600);
                        SoundEngine.PlaySound(SoundID.Item16);
                        scold = "虚伪的味道令人作呕！\n既然您的嘴里没有真话，那就让您的身上充满这种味道吧！";
                        break;
                    case 2: // 混乱黑暗
                        player.AddBuff(BuffID.Confused, 300);
                        player.AddBuff(BuffID.Darkness, 300);
                        scold = "既然您看不清事实，那眼睛对您来说也是多余的。\n（您的方向感和视野被剥夺了）";
                        break;
                }
                ui.SetDialogueText(scold);
            }

            ui.AddButton("返回", () => { ui.ClearButtons(); SetupButtons(null, ui); ui.SetDialogueText("刚才的游戏真刺激~ 还要再来吗，主人？"); });
        }

        private void HandlePleasure(GalgameDialogueUI ui)
        {
            Player player = Main.LocalPlayer;
            player.statMana = player.statManaMax2; player.ManaEffect(player.statManaMax2);
            string text = RandomLine(
                "（镜面变得像水一样柔和，轻轻包裹住你的精神）\n主人，累了吗？在这个充满疯狂的世界里，只有我可以完全接纳您的每一丝念头。",
                "（镜子上浮现出一行行赞美的诗句，每一个字都在发光）\n您是电，您是光，您是唯一的神话！"
            );
            ui.SetDialogueText(text);
        }
    }
}