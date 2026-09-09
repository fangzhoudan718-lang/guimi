using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI.Chat;
using Terraria.ModLoader;
using Terraria.UI;
using System;
using Terraria.ID;
using zhashi.Content;

// 引用魔药命名空间
using zhashi.Content.Items.Potions;
using zhashi.Content.Items.Potions.Hunter;
using zhashi.Content.Items.Potions.Moon;
using zhashi.Content.Items.Potions.Fool;
using zhashi.Content.Items.Potions.Marauder;
using zhashi.Content.Items.Potions.Sun;
using zhashi.Content.Items.Potions.Demoness;
using zhashi.Content.Items.Potions.Wheel;

namespace zhashi.Content.UI
{
    public class SequenceInfoUI : UIState
    {
        private const int ICON_SIZE = 44;
        private const int LEFT_MARGIN = 20;
        private const int TOP_MARGIN = 120;

        private UIPanel _infoPanel;
        private UIList _textList;
        private UIScrollbar _scrollbar; // 垂直滚动条
        private SimpleHorizontalScrollbar _hScrollbar; // 水平滚动条

        private string _cachedText = "";
        private int _lastPotionId = -1;
        private int _hoverTimer = 0;

        public override void OnInitialize()
        {
            _infoPanel = new UIPanel();
            _infoPanel.SetPadding(15);
            _infoPanel.BackgroundColor = new Color(20, 20, 35) * 0.9f; // 深蓝灰背景，更有质感
            _infoPanel.BorderColor = Color.Black;
            _infoPanel.OverflowHidden = true; // 隐藏超出范围的内容

            _infoPanel.Left.Set(-9999, 0f);
            _infoPanel.Top.Set(TOP_MARGIN, 0f);
            _infoPanel.Width.Set(450, 0f);
            _infoPanel.Height.Set(400, 0f);

            // 1. 垂直滚动条 (放在右侧)
            _scrollbar = new UIScrollbar();
            _scrollbar.Height.Set(-35, 1f); // 【调整】底部留出 35 像素给水平条
            _scrollbar.Left.Set(-10, 1f);
            _scrollbar.Top.Set(0, 0f);

            // 2. 水平滚动条 (放在底部)
            _hScrollbar = new SimpleHorizontalScrollbar();
            _hScrollbar.Height.Set(24, 0f);
            _hScrollbar.Width.Set(-30, 1f); // 宽度减去垂直条的空间
            // 【核心修复】Top 设置为 -30 (从底部往上30像素)，之前是 0 导致跑出去了
            _hScrollbar.Top.Set(-30, 1f);
            _hScrollbar.Left.Set(0, 0f);

            // 3. 文本列表
            _textList = new UIList();
            _textList.Width.Set(-25, 1f);
            // 【调整】列表高度减小，防止遮挡底部的水平滚动条
            _textList.Height.Set(-40, 1f);
            _textList.ListPadding = 5f;
            _textList.SetScrollbar(_scrollbar);

            // 注意 Append 顺序：先加列表，再加滚动条
            _infoPanel.Append(_textList);
            _infoPanel.Append(_scrollbar);
            _infoPanel.Append(_hScrollbar);

            Append(_infoPanel);
        }

        private class UIColoredText : UIElement
        {
            private string _text;
            public float TextWidth { get; private set; }

            public UIColoredText(string text) { SetText(text); }
            public void SetText(string text)
            {
                _text = text;
                // 计算文本宽度 (强制单行不换行测量)
                Vector2 size = ChatManager.GetStringSize(FontAssets.MouseText.Value, _text, Vector2.One, -1f);
                this.Width.Set(size.X, 0f);
                this.Height.Set(size.Y, 0f);
                this.TextWidth = size.X;
            }
            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                if (string.IsNullOrEmpty(_text)) return;
                CalculatedStyle dimensions = GetInnerDimensions();
                // 绘制文本
                ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, _text, dimensions.Position(), Color.White, 0f, Vector2.Zero, Vector2.One);
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Player player = Main.LocalPlayer;
            if (player == null || !player.active || player.dead || player.ghost) return;

            LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();
            if (!modPlayer.IsBeyonder) return;

            int itemType = GetCurrentSequencePotionID(modPlayer);
            if (itemType <= 0) return;

            // 1. 绘制魔药图标
            Main.instance.LoadItem(itemType);
            Texture2D texture = TextureAssets.Item[itemType].Value;
            Vector2 drawPos = new Vector2(LEFT_MARGIN, TOP_MARGIN);
            Rectangle iconRect = new Rectangle((int)drawPos.X, (int)drawPos.Y, ICON_SIZE, ICON_SIZE);

            Terraria.Utils.DrawInvBG(spriteBatch, iconRect, new Color(20, 20, 40, 200));

            Rectangle frame = texture.Frame();
            float scale = 1f;
            if (frame.Width > 32 || frame.Height > 32) scale = 0.8f;
            Vector2 origin = frame.Size() / 2f;
            Vector2 center = drawPos + new Vector2(ICON_SIZE / 2f, ICON_SIZE / 2f);
            spriteBatch.Draw(texture, center, frame, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);

            // 2. 鼠标悬停逻辑
            Point mousePoint = Main.MouseScreen.ToPoint();
            bool isHoveringIcon = iconRect.Contains(mousePoint);

            if (isHoveringIcon)
            {
                _infoPanel.Left.Set(LEFT_MARGIN + ICON_SIZE + 2, 0f);
                _infoPanel.Recalculate();
                _hoverTimer = 15;
            }

            if (_infoPanel.IsMouseHovering || _hScrollbar.IsDragging)
            {
                _hoverTimer = 15;
                player.mouseInterface = true;
            }

            if (_hoverTimer > 0)
            {
                _hoverTimer--;

                string fullText = GetSequenceDescription(modPlayer);
                if (fullText != _cachedText)
                {
                    bool isSamePotion = (itemType == _lastPotionId);
                    float previousScroll = _scrollbar.ViewPosition;

                    _cachedText = fullText;
                    _lastPotionId = itemType;

                    _textList.Clear();
                    // 这里传入文本
                    _textList.Add(new UIColoredText(_cachedText));
                    _textList.Recalculate();

                    if (isSamePotion) _scrollbar.ViewPosition = previousScroll;
                    else _scrollbar.ViewPosition = 0f;
                }

                // =========================================================
                // 【水平滚动核心逻辑修复】
                // =========================================================
                float maxTextWidth = 0f;
                foreach (var element in _textList)
                {
                    if (element is UIColoredText textElement)
                    {
                        if (textElement.TextWidth > maxTextWidth) maxTextWidth = textElement.TextWidth;
                    }
                }

                float listVisibleWidth = _infoPanel.GetInnerDimensions().Width;

                // 【修复点】：增加 100 像素的额外 Padding，确保最右侧能完全显示
                // 之前只加20是不够的，因为UIList本身可能有内边距
                float contentWidth = maxTextWidth + 120f;

                // 设置滚动条
                _hScrollbar.SetView(listVisibleWidth, contentWidth);

                // 应用偏移
                _textList.Left.Set(-_hScrollbar.ViewPosition, 0f);
                _textList.Recalculate();

                base.Draw(spriteBatch);
            }
            else
            {
                _infoPanel.Left.Set(-9999, 0f);
            }
        }
        // --- 辅助方法：获取当前序列对应的魔药 ID ---
        private int GetCurrentSequencePotionID(LotMPlayer p)
        {
            // 优先检测低序列 (数值越小代表序列越高)

            // 1. 猎人途径
            if (p.currentHunterSequence <= 9)
            {
                switch (p.currentHunterSequence)
                {
                    case 9: return ModContent.ItemType<HunterPotion>();
                    case 8: return ModContent.ItemType<ProvokerPotion>();
                    case 7: return ModContent.ItemType<PyromaniacPotion>();
                    case 6: return ModContent.ItemType<ConspiratorPotion>();
                    case 5: return ModContent.ItemType<ReaperPotion>();
                    case 4: return ModContent.ItemType<IronBloodedKnightPotion>();
                    case 3: return ModContent.ItemType<WarBishopPotion>();
                    case 2: return ModContent.ItemType<WeatherWarlockPotion>();
                    case 1: return ModContent.ItemType<ConquerorPotion>();
                    default: return ModContent.ItemType<HunterPotion>();
                }
            }
            // 2. 巨人途径
            if (p.currentSequence <= 9)
            {
                switch (p.currentSequence)
                {
                    case 9: return ModContent.ItemType<WarriorPotion>();
                    case 8: return ModContent.ItemType<PugilistPotion>();
                    case 7: return ModContent.ItemType<WeaponMasterPotion>();
                    case 6: return ModContent.ItemType<DawnKnightPotion>();
                    case 5: return ModContent.ItemType<GuardianPotion>();
                    case 4: return ModContent.ItemType<DemonHunterPotion>();
                    case 3: return ModContent.ItemType<SilverKnightPotion>();
                    case 2: return ModContent.ItemType<GloryPotion>();
                    case 1: return ModContent.ItemType<HandOfGodPotion>();
                    default: return ModContent.ItemType<WarriorPotion>();
                }
            }
            // 3. 月亮途径
            if (p.currentMoonSequence <= 9)
            {
                switch (p.currentMoonSequence)
                {
                    case 9: return ModContent.ItemType<ApothecaryPotion>();
                    case 8: return ModContent.ItemType<BeastTamerPotion>();
                    case 7: return ModContent.ItemType<VampirePotion>();
                    case 6: return ModContent.ItemType<PotionsProfessorPotion>();
                    case 5: return ModContent.ItemType<ScarletScholarPotion>();
                    case 4: return ModContent.ItemType<WitchKingPotion>();
                    case 3: return ModContent.ItemType<SummoningMasterPotion>();
                    case 2: return ModContent.ItemType<LifeGiverPotion>();
                    case 1: return ModContent.ItemType<BeautyGoddessPotion>();
                    default: return ModContent.ItemType<ApothecaryPotion>();
                }
            }
            // 4. 愚者途径
            if (p.currentFoolSequence <= 9)
            {
                switch (p.currentFoolSequence)
                {
                    case 9: return ModContent.ItemType<SeerPotion>();
                    case 8: return ModContent.ItemType<ClownPotion>();
                    case 7: return ModContent.ItemType<MagicianPotion>();
                    case 6: return ModContent.ItemType<FacelessPotion>();
                    case 5: return ModContent.ItemType<MarionettistPotion>();
                    case 4: return ModContent.ItemType<BizarroSorcererPotion>();
                    case 3: return ModContent.ItemType<ScholarOfYorePotion>();
                    case 2: return ModContent.ItemType<MiracleInvokerPotion>();
                    case 1: return ModContent.ItemType<AttendantPotion>();
                    default: return ModContent.ItemType<SeerPotion>();
                }
            }
            // 5.错误途径
            if (p.currentMarauderSequence <= 9)
            {
                switch (p.currentMarauderSequence)
                {
                    case 9: return ModContent.ItemType<MarauderPotion>(); 
                    case 8: return ModContent.ItemType<SwindlerPotion>();
                    case 7: return ModContent.ItemType<CryptologistPotion>();
                    case 6: return ModContent.ItemType<PrometheusPotion>(); // [新增]
                    case 5: return ModContent.ItemType<DreamStealerPotion>();
                    case 4: return ModContent.ItemType<ParasitePotion>();
                    case 3: return ModContent.ItemType<MentorPotion>();
                    case 2: return ModContent.ItemType<TrojanHorsePotion>();
                    case 1: return ModContent.ItemType<WormOfTimePotion>();
                    default: return ModContent.ItemType<MarauderPotion>();
                }
            }
            if (p.currentSunSequence <= 9)
            {
                switch (p.currentSunSequence)
                {
                    case 9: return ModContent.ItemType<Content.Items.Potions.Sun.BardPotion>();
                    case 8: return ModContent.ItemType<LightSupplicantPotion>();   // 祈光人
                    case 7: return ModContent.ItemType<SolarHighPriestPotion>();   // 太阳神官
                    case 6: return ModContent.ItemType<NotaryPotion>();            // 公证人
                    case 5: return ModContent.ItemType<PriestPotion>();     // 光之祭司
                    case 4: return ModContent.ItemType<UnshadowedPotion>();        // 无暗者
                    case 3: return ModContent.ItemType<JusticeMentorPotion>();     // 正义导师
                    case 2: return ModContent.ItemType<LightSeekerPotion>();       // 逐光者
                    case 1: return ModContent.ItemType<WhiteAngelPotion>();        // 白天使
                    default: return ModContent.ItemType<Content.Items.Potions.Sun.BardPotion>();
                }
            }
            if (p.currentDemonessSequence <= 9)
            {
                switch (p.currentDemonessSequence)
                {
                    case 9: return ModContent.ItemType<AssassinPotion>();
                    case 8: return ModContent.ItemType<InstigatorPotion>();
                    case 7: return ModContent.ItemType<WitchPotion>();
                    case 6: return ModContent.ItemType<PleasureDemonessPotion>();
                    case 5: return ModContent.ItemType<AfflictionDemonessPotion>();
                    case 4: return ModContent.ItemType<DespairDemonessPotion>();
                    case 3: return ModContent.ItemType<UnagingDemonessPotion>();
                    case 2: return ModContent.ItemType<CatastropheDemonessPotion>();
                    case 1: return ModContent.ItemType<ApocalypseDemonessPotion>();
                    default: return ModContent.ItemType<AssassinPotion>();
                }
            }
            // 8. 命运途径 (Wheel)
            if (p.currentWheelSequence <= 9)
            {
                switch (p.currentWheelSequence)
                {
                    case 9: return ModContent.ItemType<MonsterPotion>();
                    case 8: return ModContent.ItemType<RobotPotion>();
                    case 7: return ModContent.ItemType<LuckyPotion>();
                    case 6: return ModContent.ItemType<CalamityPriestPotion>();
                    case 5: return ModContent.ItemType<WinnerPotion>();
                    case 4: return ModContent.ItemType<MisfortuneMagePotion>();
                    case 3: return ModContent.ItemType<AnomalyPotion>();
                    case 2: return ModContent.ItemType<ProphetPotion>();
                    case 1: return ModContent.ItemType<SerpentPotion>();
                    default: return ModContent.ItemType<MonsterPotion>();
                }
            }
            // 9. 学徒/门 途径 (Door)
            if (p.currentDoorSequence <= 9)
            {
                switch (p.currentDoorSequence)
                {
                    case 9: return ModContent.ItemType<Content.Items.Potions.Door.ApprenticePotion>();
                    case 8: return ModContent.ItemType<Content.Items.Potions.Door.TricksterPotion>();
                    case 7: return ModContent.ItemType<Content.Items.Potions.Door.AstrologerPotion>();
                    case 6: return ModContent.ItemType<Content.Items.Potions.Door.RecorderPotion>();
                    case 5: return ModContent.ItemType<Content.Items.Potions.Door.TravelerPotion>();
                    default: return ModContent.ItemType<Content.Items.Potions.Door.ApprenticePotion>();
                }
            }
            return 0;
        }

        // --- 辅助方法：生成纯文本介绍 ---
        private string GetSequenceDescription(LotMPlayer p)
        {
            string text = "";

            if (p.currentHunterSequence <= 9)
            {
                text += $"[c/FF4500:猎人途径 序列{p.currentHunterSequence}]\n";
                if (p.currentHunterSequence <= 9) // 猎人
                {
                    text += $"序列九: [c/FFA07A:猎人]\n";
                    text += "- [被动] 远程伤害+15% / 近战伤害+5%\n";
                    text += "- [被动] 生物监测 / 危险感知\n";
                }
                if (p.currentHunterSequence <= 8) // 挑衅者
                {
                    text += $"序列八: [c/FA8072:挑衅者]\n";
                    text += "- [被动] 仇恨值+300 / 防御+10 / 生命回复+2\n";
                }
                if (p.currentHunterSequence <= 7) // 纵火家
                {
                    text += $"序列七: [c/FF6347:纵火家]\n";

                    string cloakStatus = p.isFlameCloakActive ? " [c/00FF00:(开启)]" : "";
                    string chargeStatus = p.isChargingFireball ? " [c/FF0000:(蓄力中...)]" : "";

                    text += $"- [技能] 蓄力火球 (按住技能键){chargeStatus}: 发射高伤火球\n";
                    text += "- [技能] 纵火者炸弹 (按键投掷): 造成范围火焰伤害\n";
                    text += $"- [技能] 火焰披风 (按键切换){cloakStatus}: 免疫寒冷/近身灼烧\n";
                    text += "- [被动] 免疫火焰/燃烧/岩浆\n";
                }
                if (p.currentHunterSequence <= 6) // 阴谋家
                {
                    text += $"序列六: [c/FF4500:阴谋家]\n";

                    string cdFlash = p.fireTeleportCooldown > 0 ? $" [c/FF0000:({p.fireTeleportCooldown / 60}s)]" : "";
                    string enchantStatus = p.isFireEnchanted ? " [c/00FF00:(已附魔)]" : "";

                    text += $"- [技能] 火焰闪现 (按键触发){cdFlash}: 瞬间传送并留下诱饵\n";
                    text += $"- [技能] 武器附魔 (按键切换){enchantStatus}: 赋予武器火焰特效\n";
                    text += "- [被动] 暴击率+15% / 魔力消耗-20%\n";
                }
                if (p.currentHunterSequence <= 5) // 收割者
                {
                    text += $"序列五: [c/DC143C:收割者]\n";

                    text += "- [技能] 收割斩击 (按键释放): 释放大范围火焰斩\n";
                    text += "- [被动] 弱点处决: 对低血量(20%)非Boss敌人造成致死打击\n";
                    text += "- [被动] 护甲穿透+30 / 暴击率+20%\n";
                }
                if (p.currentHunterSequence <= 4) // 铁血骑士
                {
                    text += $"序列四: [c/B22222:铁血骑士]\n";

                    string armyStatus = p.isArmyOfOne ? " [c/00FF00:(集结)]" : "";
                    string formStatus = p.isFireForm ? " [c/00FF00:(元素化)]" : "";

                    text += $"- [技能] 战争集众 (按键切换){armyStatus}: 召唤栏位大幅增加\n";
                    text += $"- [技能] 火焰形态 (按键切换){formStatus}: \n";
                    text += "    > 免疫击退/无限飞行/大幅提升机动性与防御\n";
                    text += "    > 冲刺可对敌人造成毁灭性伤害\n";
                }
                if (p.currentHunterSequence <= 3) // 战争主教
                {
                    text += $"序列三: [c/8B0000:战争主教]\n";
                    text += "- [被动] 召唤物+10 / 哨兵+3 / 召唤伤害+40%\n";
                    text += "- [权柄] 战争领域: 极大增强军队作战能力\n";
                }
                if (p.currentHunterSequence <= 2) // 天气术士
                {
                    text += $"序列二: [c/800000:天气术士]\n";

                    string cdGlacier = p.glacierCooldown > 0 ? $" [c/FF0000:({p.glacierCooldown / 60}s)]" : "";
                    string giantStatus = p.isCalamityGiant ? " [c/00FF00:(化身中)]" : "";

                    text += "- [技能] 闪电风暴 (按键释放): 召唤大范围雷击\n";
                    text += $"- [技能] 冰河世纪 (按键释放){cdGlacier}: 全屏冻结与减速\n";
                    text += $"- [技能] 灾祸巨人 (按键切换){giantStatus}: \n";
                    text += "    > 化身百米高的雷火巨人，防御与生命值质变\n";

                    if (p.currentHunterSequence == 2)
                    {
                        bool isClear = p.conquerorRitualComplete;
                        string status = isClear ? "[已完成]" : "[进行中]";
                        string colorHex = isClear ? "00FF00" : "FFA500";

                        text += $"[c/{colorHex}:[晋升仪式] 征服者: {status}]\n";
                        text += "  (提示: 肃清视野内所有敌人，确立无敌之姿)\n";
                    }
                }
                if (p.currentHunterSequence <= 1) // 征服者
                {
                    text += $"序列一: [c/FF0000:征服者]\n";
                    text += "- [神性] 战争主宰: 暴击率+40% / 最终伤害+100%\n";
                    text += "- [被动] 威慑: 仇恨值极大提升，免疫大部分削弱效果\n";
                }
            }
            else if (p.currentSequence <= 9)
            {
                text += $"[c/C0C0C0:巨人途径 序列{p.currentSequence}]\n";

                // --- 序列9: 战士 ---
                if (p.currentSequence <= 9)
                {
                    text += $"序列九: [c/D3D3D3:战士]\n";
                    text += "- [被动] 基础防御+8 / 近战伤害+12%\n";
                    text += "- [被动] 生命上限+100 / 近战暴击+5%\n";
                    text += "- [能力] 战斗精通：熟练使用各类近战武器\n";
                }

                // --- 序列8: 格斗学者 ---
                if (p.currentSequence <= 8)
                {
                    text += $"序列八: [c/A9A9A9:格斗学者]\n";
                    text += "- [被动] 近战攻速+15% / 伤害减免+5%\n";
                    text += "- [被动] 钢铁意志：免疫击退\n";
                }

                // --- 序列7: 武器大师 ---
                if (p.currentSequence <= 7)
                {
                    text += $"序列七: [c/808080:武器大师]\n";
                    text += "- [被动] 全伤害+10% / 全暴击+5%\n";
                    text += "- [被动] 弱点洞悉：护甲穿透+10\n";
                }

                // --- 序列6: 黎明骑士 ---
                if (p.currentSequence <= 6)
                {
                    text += $"序列六: [c/FFFFE0:黎明骑士]\n";

                    // 铠甲状态显示
                    string armorStatus;
                    if (p.dawnArmorBroken)
                        armorStatus = $" [c/FF0000:(破碎: {p.dawnArmorCooldownTimer / 60 + 1}s)]";
                    else if (p.dawnArmorActive)
                        armorStatus = $" [c/00FF00:(护盾: {(int)p.dawnArmorCurrentHP}/{(int)p.MaxDawnArmorHP})]";
                    else
                        armorStatus = " [关闭]";

                    text += $"- [技能] 晨曦之铠 (按键切换){armorStatus}: \n";
                    text += "  > 召唤圣光铠甲，提供额外防御与独立护盾值\n";
                    text += "- [被动] 自带圣洁光照 / 生命回复+3\n";
                    if (p.currentSequence == 6)
                    {
                        int target = LotMPlayer.GUARDIAN_RITUAL_TARGET;
                        int current = p.guardianRitualProgress;
                        if (current > target) current = target;

                        string statusText = current >= target ? "[已完成]" : $"[{current}/{target}]";
                        string colorHex = current >= target ? "00FF00" : "FFA500";

                        text += $"[c/{colorHex}:[晋升仪式] 守护之心: {statusText}]\n";
                        text += "  (提示: 在城镇NPC附近承受伤害)\n";
                    }
                }

                // --- 序列5: 守护者 ---
                if (p.currentSequence <= 5)
                {
                    text += $"序列五: [c/708090:守护者]\n";
                    string guardStatus = p.isGuardianStance ? " [c/00FF00:(坚守中)]" : "";

                    text += $"- [技能] 守护姿态 (按住按键){guardStatus}: \n";
                    text += "    > 牺牲移动能力，换取防御+80与30%免伤\n";
                    text += "- [被动] 基础防御+20 / 免疫混乱\n";
                }

                // --- 序列4: 猎魔人 ---
                if (p.currentSequence <= 4)
                {
                    text += $"序列四: [c/696969:猎魔人]\n";
                    text += "- [半神] 猎魔体质：生命+500 / 全伤害+20%\n";
                    text += "- [被动] 超凡感官：获得夜视与生物探测\n";
                    text += "- [被动] 诅咒抗性：免疫咒火与暗影焰\n";
                }

                // --- 序列3: 银骑士 ---
                if (p.currentSequence <= 3)
                {
                    text += $"序列三: [c/C0C0C0:银骑士]\n";
                    string mercuryStatus = p.isMercuryForm ? " [c/00FF00:(化身中)]" : "";

                    text += $"- [技能] 水银化 (按键切换){mercuryStatus}: \n";
                    text += "    > 隐身并极大提升移速，对接触的敌人造成伤害\n";
                    text += "- [被动] 银白闪避：获得黑带闪避效果\n";
                    text += "- [强化] 近战攻速+20% / 生命回复+5\n";
                }

                // --- 序列2: 荣耀战神 ---
                if (p.currentSequence <= 2)
                {
                    text += $"序列二: [c/FFD700:荣耀战神]\n";
                    string cdRevive = p.twilightResurrectionCooldown > 0
                        ? $" [c/FF0000:({p.twilightResurrectionCooldown / 60}s)]"
                        : " [c/00FF00:(就绪)]";

                    text += $"- [神性] 黄昏复活{cdRevive}: \n";
                    text += "    > 受到致命伤时免疫死亡并瞬间回满生命\n";
                    text += "- [被动] 巨人神躯：生命+2000 / 防御+50 / 免伤+15%\n";
                }
                if (p.currentSequence <= 1)
                {
                    text += $"序列一: [c/FF4500:神明之手]\n";
                    text += "- [位格] 黄昏神躯：生命上限+5000 / 基础防御+100\n";
                    text += "- [被动] 衰败权柄：伤害减免+20% / 生命回复+20\n";
                    text += "- [升华] 技能神性化：\n";
                    text += "    > 守护姿态：额外获得10%免伤与强力荆棘反伤\n";
                    text += "    > 晨曦之铠：激活时额外提供50点防御力\n";
                    text += "- [特效] 神性显化：周身散发黄昏光辉\n";
                } 
            }
            else if (p.currentMoonSequence <= 9)
            {
                text += $"[c/EE82EE:月亮途径 序列{p.currentMoonSequence}]\n";
                if (p.currentMoonSequence <= 9) // 药师
                {
                    text += $"序列九: [c/D8BFD8:药师]\n";
                    text += "- [被动] 免疫中毒/剧毒，生命回复提升\n";
                    text += "- [被动] 药物精通：制作药水类物品翻倍(概率)\n";
                    text += "- [能力] 怪物感知：能看到周围的生物\n";
                }
                if (p.currentMoonSequence <= 8) // 驯兽师
                {
                    text += $"序列八: [c/DA70D6:驯兽师]\n";

                    string tameStatus = p.isTamingActive ? " [c/00FF00:(开启)]" : "";

                    text += $"- [能力] 驯兽模式 (按键切换){tameStatus}: \n";
                    text += "    > 使用鞭子攻击虚弱生物(25%血)可将其驯服\n";
                    text += "- [被动] 召唤栏位增加，移动速度提升\n";
                }
                if (p.currentMoonSequence <= 7) // 吸血鬼
                {
                    text += $"序列七: [c/BA55D3:吸血鬼]\n";

                    string wingsStatus = p.isVampireWings ? " [c/00FF00:(展开)]" : "";
                    string cdShackle = p.abyssShackleCooldown > 0 ? $" [c/FF0000:({p.abyssShackleCooldown / 60}s)]" : "";

                    text += $"- [技能] 黑暗之翼 (按键切换){wingsStatus}: 飞行/无视摔伤\n";
                    text += $"- [技能] 深渊枷锁 (按键释放){cdShackle}: 释放黑暗锁链束缚敌人\n";
                    text += "- [被动] 生命回复大幅提升 / 免疫寒冷与冰冻\n";
                }
                if (p.currentMoonSequence <= 6) // 魔药教授
                {
                    text += $"序列六: [c/9932CC:魔药教授]\n";

                    string cdElixir = p.elixirCooldown > 0 ? $" [c/FF0000:({p.elixirCooldown / 60}s)]" : "";

                    text += $"- [技能] 生命灵液 (按键服用){cdElixir}: 瞬间回复大量生命\n";
                    text += "- [技能] 炼金手雷 (按键投掷): 造成范围化学伤害\n";
                    text += "- [被动] 魔法伤害提升 / 免疫火焰\n";
                }
                if (p.currentMoonSequence <= 5) // 深红学者
                {
                    text += $"序列五: [c/8B008B:深红学者]\n";

                    string moonStatus = p.isFullMoonActive ? " [c/00FF00:(开启)]" : "";
                    string lightStatus = p.isMoonlightized ? " [c/00FF00:(化身中)]" : "";

                    text += $"- [技能] 满月领域 (按键切换){moonStatus}: \n";
                    text += "    > 制造满月环境，大幅提升魔法与回蓝\n";
                    text += $"- [技能] 月光化 (按键切换){lightStatus}: \n";
                    text += "    > 化为不可选中的月光，极速移动并免疫伤害\n";
                    text += "- [被动] 免疫黑暗/混乱/沉默\n";
                }
                if (p.currentMoonSequence <= 4) // 巫王
                {
                    text += $"序列四: [c/800080:巫王]\n";

                    string batStatus = p.isBatSwarm ? " [c/00FF00:(化身中)]" : "";
                    string cdGaze = p.darknessGazeCooldown > 0 ? $" [c/FF0000:({p.darknessGazeCooldown / 60}s)]" : "";
                    string cdPaper = p.paperFigurineCooldown > 0 ? $" [c/FF0000:({p.paperFigurineCooldown / 60}s)]" : "";

                    text += $"- [技能] 蝙蝠化身 (按键切换){batStatus}: 分散成蝙蝠群移动\n";
                    text += $"- [技能] 黑暗凝视 (按键释放){cdGaze}: 造成大范围伤害与致盲\n";
                    text += $"- [技能] 纸人替身 (按键触发){cdPaper}: 紧急回避伤害\n";
                }
                if (p.currentMoonSequence <= 3) // 召唤大师
                {
                    text += $"序列三: [c/4B0082:召唤大师]\n";

                    string cdGate = p.summonGateCooldown > 0 ? $" [c/FF0000:({p.summonGateCooldown / 60}s)]" : "";

                    text += $"- [技能] 召唤之门 (按键开启){cdGate}: \n";
                    text += "    > 洞开灵界大门，召唤未知生物助战\n";
                    text += "- [被动] 召唤栏大幅增加 / 召唤伤害提升\n";
                }
                if (p.currentMoonSequence <= 2) // 创生者
                {
                    text += $"序列二: [c/2E0854:创生者]\n";

                    string domainStatus = p.isCreationDomain ? " [c/00FF00:(开启)]" : "";
                    string cdPurify = p.purifyCooldown > 0 ? $" [c/FF0000:({p.purifyCooldown / 60}s)]" : "";
                    string cdLife = p.paperFigurineCooldown > 0 ? $" [c/FF0000:({p.paperFigurineCooldown / 60}s)]" : "";

                    text += $"- [技能] 创生领域 (按键切换){domainStatus}: \n";
                    text += "    > 持续治疗周围队友，并汲取敌人生命\n";
                    text += $"- [技能] 净化大地 (按键释放){cdPurify}: 净化周围的腐化/猩红\n";
                    text += $"- [强化] 纸人 -> 生命剥夺{cdLife}: 强制剥夺范围内敌人生命\n";
                }
                if (p.currentMoonSequence <= 1) // 美神
                {
                    text += $"序列一: [c/FFFFFF:美神]\n";

                    text += "- [被动] 魅惑光环: 周围敌人攻击减半并陷入混乱/爱慕\n";
                    text += "- [被动] 完美闪避: 25%几率完全免疫伤害\n";
                    text += "- [被动] 绯红之主: 全伤害大幅提升，免疫绝大部分负面状态\n";
                }
            }

            else if (p.currentFoolSequence <= 9)
            {
                text += $"[c/8A2BE2:愚者途径 序列{p.currentFoolSequence}]\n"; // 紫色标题

                if (p.currentFoolSequence <= 9) // 占卜家
                {
                    text += $"序列九: [c/9370DB:占卜家]\n";

                    string cdDiv = p.divinationCooldown > 0 ? $" [c/FF0000:({p.divinationCooldown / 60}s)]" : "";
                    string visionStatus = p.isSpiritVisionActive ? " [c/00FF00:(开启)]" : "";

                    text += "- [被动] 魔法伤害+10% / 危险感知\n";
                    text += $"- [能力] 灵视 (C键){visionStatus}: 探宝/见灵\n";
                    text += $"- [技能] 占卜术 (J键){cdDiv}: 随机获得一种启示或Buff\n";
                }
                if (p.currentFoolSequence <= 8) // 小丑
                {
                    text += $"序列八: [c/BA55D3:小丑]\n";

                    text += "- [被动] 身体掌控：移速/跳跃/攻速大幅提升\n";
                    text += "- [被动] 直觉预感：暴击+10% / 10%几率自动闪避\n";
                    text += "- [能力] 纸人飞刀：可以使用特制投掷武器\n";
                }
                if (p.currentFoolSequence <= 7) // 魔术师
                {
                    text += $"序列七: [c/9932CC:魔术师]\n";

                    string cdJump = p.flameJumpCooldown > 0 ? $" [c/FF0000:({p.flameJumpCooldown / 60}s)]" : "";
                    string cdTransfer = p.damageTransferCooldown > 0 ? $" [c/FF0000:({p.damageTransferCooldown / 60}s)]" : "";

                    text += $"- [能力] 火焰跳跃 (F键){cdJump}: 瞬间传送至鼠标指向的任何火焰\n";
                    text += $"- [被动] 伤害转移{cdTransfer}: 受到致命伤时由纸人替死\n";
                    text += "- [被动] 水下呼吸 & 免疫束缚/石化\n";
                    text += "- [技能] 空气弹: 获得专属魔法武器\n";
                    text += "- [能力] 纸人替身: 消耗纸人道具免疫伤害\n";
                }
                if (p.currentFoolSequence <= 6) // 无面人
                {
                    text += $"序列六: [c/8B008B:无面人]\n";

                    string faceStatus = p.isFacelessActive ? " [c/00FF00:(开启)]" : "";
                    string cdDistort = p.distortCooldown > 0 ? $" [c/FF0000:({p.distortCooldown / 60}s)]" : "";

                    text += $"- [技能] 无面伪装 (V键){faceStatus}: 隐身/降低仇恨/增加防御\n";
                    text += $"- [技能] 干扰直觉 (G键){cdDistort}: 使周围敌人混乱并削弱攻击\n";
                    text += "- [被动] 观察者：显示稀有生物/矿物/当前DPS\n";
                    text += "- [被动] 反占卜：40%几率闪避攻击\n";
                }
                if (p.currentFoolSequence <= 5) // 秘偶大师
                {
                    text += $"序列五: [c/800080:秘偶大师]\n";

                    string threadStatus = "";
                    if (p.spiritThreadTargetIndex != -1)
                    {
                        int progress = (int)((float)p.spiritThreadTimer / LotMPlayer.CONTROL_TIME_REQUIRED * 100);
                        threadStatus = $" [c/00FF00:(控制中: {progress}%)]";
                    }

                    text += $"- [技能] 操纵灵体之线 (按住Z键){threadStatus}: \n";
                    text += "    > 持续控制目标，满进度后将其转化为秘偶\n";
                    text += "- [能力] 秘偶化: 转化敌人为友军 (上限+3)\n";
                    text += "- [被动] 灵体视野: 全图探敌/危险感知\n";
                }
                if (p.currentFoolSequence <= 4) // 诡法师
                {
                    text += $"序列四: [c/4B0082:诡法师]\n";

                    string cdSwap = p.swapCooldown > 0 ? $" [c/FF0000:({p.swapCooldown / 60}s)]" : "";
                    string cdControl = p.spiritControlCooldown > 0 ? $" [c/FF0000:({p.spiritControlCooldown / 60}s)]" : "";

                    text += $"- [被动] 灵之虫: 拥有 {p.spiritWorms} 条灵之虫 (用于复活/施法)\n";
                    text += $"- [技能] 秘偶互换 (T键){cdSwap}: 与最近的秘偶瞬间换位\n";
                    text += $"- [技能] 灵体震慑 (R键){cdControl}: 大范围群体麻痹/混乱\n";
                    text += "- [强化] 占卜术: 效果增强，甚至可以预知未来\n";
                    text += "- [强化] 灵体之线: 转化速度提升 (上限10个秘偶)\n";
                }
                if (p.currentFoolSequence <= 3) // 古代学者
                {
                    text += $"序列三: [c/483D8B:古代学者]\n";

                    string borrowStatus = p.isBorrowingPower ? " [c/00FF00:(生效中)]" : "";

                    text += "- [技能] 历史投影 (Y键): 随机召唤曾击败过的Boss投影助战\n";
                    text += $"- [技能] 昨日重现 (U键){borrowStatus}: \n";
                    text += $"    > 向过去的自己借取力量 (剩余次数: {10 - p.borrowUsesDaily})\n";
                    text += "- [被动] 灵之虫++: 上限600条，自动快速回复\n";
                    text += "- [强化] 空气炮: 威力提升至岸防炮级别\n";
                }
                if (p.currentFoolSequence <= 2) // 奇迹师
                {
                    text += $"序列二: [c/2E0854:奇迹师]\n";

                    string cdMiracle = p.miracleCooldown > 0 ? $" [c/FF0000:({p.miracleCooldown / 60}s)]" : "";
                    string fateStatus = p.fateDisturbanceActive ? " [c/00FF00:(开启)]" : "";

                    string wishName = p.selectedWish == 0 ? "生命复苏" :
                                      p.selectedWish == 1 ? "毁灭天灾" :
                                      p.selectedWish == 2 ? "空间传送" : "昼夜更替";

                    text += $"- [技能] 奇迹愿望 (V键){cdMiracle}: \n";
                    text += $"    > 单按切换，长按实现: [c/00FFFF:{wishName}]\n";
                    text += $"- [技能] 命运干扰 (G键){fateStatus}: 开启光环，极大提升暴击与运气\n";
                    text += "- [技能] 历史场景 (Shift+Y): 召唤大型历史投影降临\n";
                    text += "- [被动] 复活奇迹: 死亡时引发全屏毁灭打击并复活\n";

                    if (p.currentFoolSequence == 2)
                    {
                        int target = LotMPlayer.ATTENDANT_RITUAL_TARGET; // 10
                        int current = p.attendantRitualProgress;
                        string status = current >= target ? "[已完成]" : $"[{current}/{target}]";
                        string colorHex = current >= target ? "00FF00" : "FFA500";

                        text += $"[c/{colorHex}:[晋升仪式] 诡秘侍者: {status}]\n";
                        text += "  (提示: 将10个城镇NPC转化为秘偶)\n";
                    }
                }
                if (p.currentFoolSequence <= 1) // 诡秘侍者
                {
                    text += $"序列一: [c/FFFFFF:诡秘侍者]\n";

                    string spiritFormStatus = p.isSpiritForm ? " [c/00FF00:(灵体化)]" : "";
                    string graftMode = p.graftingMode == 0 ? "关闭" : (p.graftingMode == 1 ? "空间反弹" : "概念必杀");
                    string cdGraft = p.graftingCooldown > 0 ? $" [c/FF0000:({p.graftingCooldown / 60}s)]" : "";

                    text += $"- [技能] 灵肉转化 (V键){spiritFormStatus}: 物理免疫/穿墙/高耗蓝\n";
                    text += "- [技能] 奇迹愿望 (Shift+V): 功能同序列2\n";
                    text += $"- [技能] 概念嫁接 (G键){cdGraft}: 当前模式: [c/00FFFF:{graftMode}]\n";
                    text += "- [技能] 命运干扰 (Shift+G): 开启/关闭光环\n";
                    text += "- [主动] 诡秘之境: 自动吞噬周围掉落物回血，压制敌人\n";
                    text += "- [被动] 概念防御: 受到致命伤时自动消耗灵性嫁接死亡\n";
                }
            }

            else if (p.currentMarauderSequence <= 9)
            {
                text += $"[c/3232C8:错误途径 序列{p.currentMarauderSequence}]\n";

                if (p.currentMarauderSequence <= 9)
                {
                    text += $"序列九: [c/E6E6FA:偷盗者]\n";
                    text += "- 卓越观察: 常驻探宝药水效果\n";
                    text += "- [主动]窃取: 拾取范围扩大，攻击几率偷钱\n";
                    text += "- 敏捷之手: 挖掘/放置速度提升\n";
                    text += "- 短兵精通: 短剑类武器大幅增强\n";
                }
                if (p.currentMarauderSequence <= 8) // 诈骗师
                {
                    text += $"序列八: [c/D8BFD8:诈骗师]\n";
                    text += "- [被动] 魅力与口才: 商店购物打折\n";
                    text += "- [被动] 欺诈大师: 移动速度大幅提升\n";
                    text += "- [被动] 精神干扰: 攻击有概率使敌人混乱\n";
                }
                if (p.currentMarauderSequence <= 7) // 解密学者
                {
                    text += $"序列七: [c/DA70D6:解密学者]\n";
                    text += "- [被动] 灵性直觉: 获得狩猎与危险感知视野\n";
                    text += "- [被动] 弱点解析: 护甲穿透+10 / 暴击+5%\n";
                }
                if (p.currentMarauderSequence <= 6) // 盗火人
                {
                    text += $"序列六: [c/BA55D3:盗火人]\n";

                    text += "- [主动] 窃取: 攻击概率直接获得怪物掉落物\n";
                    text += "- [被动] 窃取能力: 攻击吸取生命与魔力\n";
                    text += "- [被动] 精神抗性: 免疫大部分精神干扰Debuff\n";
                }
                if (p.currentMarauderSequence <= 5) // 窃梦家
                {
                    text += $"序列五: [c/9932CC:窃梦家]\n";

                    text += "- [被动] 窃取意图: 攻击使敌人呆滞/混乱\n";
                    text += "- [被动] 梦魇光环: 附近敌人减速且减防\n";
                    text += "- [特效] 记忆窃取: 攻击削弱敌人并回血\n";
                    text += "- [被动] 盗天机: 战斗中随机窃取强力Buff\n";
                    text += "- [能力] 梦境主宰: 拾取范围极大提升\n";

                    if (p.currentMarauderSequence == 5)
                    {
                        string status = p.parasiteRitualProgress >= 9 ? "[已完成]" : $"[{p.parasiteRitualProgress}/9]";
                        string colorHex = p.parasiteRitualProgress >= 9 ? "00FF00" : "FFA500";
                        text += $"[c/{colorHex}:[晋升仪式] 窃取供养: {status}]\n";
                    }
                }
                if (p.currentMarauderSequence <= 4) // 寄生者
                {
                    text += $"序列四: [c/8A2BE2:寄生者 (半神)]\n";

                    text += "- [被动] 半虫化: 除非命中要害，否则免疫一次致死伤害(长冷却)\n";
                    text += "- [主动] 寄生 (按P键): \n";
                    text += "    > 对城镇NPC: 浅层寄生，随身隐形并快速回血\n";
                    text += "    > 对敌人: 深层寄生，持续造成伤害并吸血/控制\n";
                    text += "- [主动] 概念窃取 (K键): \n";
                    text += "    > 窃取“距离”: 瞬间移动到鼠标位置\n";
                    text += "    > 窃取“位置”: 与目标互换位置\n";

                    if (p.currentMarauderSequence == 4)
                    {
                        string status = p.mentorRitualProgress >= 9 ? "[已完成]" : $"[{p.mentorRitualProgress}/9]";
                        string colorHex = p.mentorRitualProgress >= 9 ? "00FF00" : "FFA500";
                        text += $"[c/{colorHex}:[晋升仪式] 误导冤魂: {status}]\n";
                        text += "  (提示: 让敌人在[混乱]状态下死亡)\n";
                    }
                }
                if (p.currentMarauderSequence <= 3) // 欺瞒导师
                {
                    text += $"序列三: [c/800080:欺瞒导师 (圣者)]\n";

                    text += "- [被动] 欺诈权柄: 窃取技能判定次数 x3\n";
                    text += "- [主动] 欺瞒领域 (Shift+P): \n";
                    text += "    > 扭曲周围规则，使敌人强制混乱并减防\n";
                    text += "    > 序列二后，误导飞行道具，使其偏离甚至倒戈\n";

                    if (p.currentMarauderSequence == 3)
                    {
                        float percent = (float)p.trojanRitualTimer / LotMPlayer.TROJAN_RITUAL_TARGET * 100;
                        string status = percent >= 100 ? "[已完成]" : $"[{percent:F1}%]";
                        string colorHex = percent >= 100 ? "00FF00" : "FFA500";
                        text += $"[c/{colorHex}:[晋升仪式] 顶替身份: {status}]\n";
                        text += "  (提示: 寄生城镇NPC并保持一段时间)\n";
                    }
                }
                if (p.currentMarauderSequence <= 2) // 命运木马
                {
                    text += $"序列二: [c/4B0082:命运木马 (天使)]\n";

                    text += "- [被动] 命运权柄: 窃取判定次数 x6\n";
                    text += "- [被动] 命运预见: 受到致命伤时由分身替死 (冷却)\n";
                    text += "- [主动] 命运窃取 (Shift+O): \n";
                    text += "    > 嫁接命运：若敌人血量比你高，互换生命比例\n";
                    text += "    > 对Boss造成巨额真实伤害并剥夺其攻击能力\n";

                    if (p.currentMarauderSequence == 2)
                    {
                        float percent = (float)p.wormRitualTimer / LotMPlayer.WORM_RITUAL_TARGET * 100;
                        string status = percent >= 100 ? "[已完成]" : $"[{percent:F1}%]";
                        string colorHex = percent >= 100 ? "00FF00" : "FFA500";
                        text += $"[c/{colorHex}:[晋升仪式] 时光混乱: {status}]\n";
                        text += "  (提示: 在城镇中开启欺瞒领域并维持)\n";
                    }
                }
                if (p.currentMarauderSequence <= 1) // 时之虫
                {
                    text += $"序列一: [c/FFFFFF:时之虫 (天使之王)]\n";

                    text += "- [被动] 时间权柄: 免疫大部分时间控制/减速效果\n";
                    text += "- [主动] 时之虫领域 (Shift+P): \n";
                    text += "    > 召唤壁钟虚影，范围内敌人极度减速并持续衰老(掉血)\n";
                    text += "- [主动] 窃取时间 (Shift+O): \n";
                    text += "    > 瞬间剥夺目标生命(老化)，并赋予自身极速\n";
                    text += "- [能力] 错误化身: 死亡时由序列2分身通过窃取命运来复活(强化版)\n";
                }
            }
            // 6. 太阳途径 (Sun)
            else if (p.currentSunSequence <= 9)
            {
                // 标题颜色：金色
                text += $"[c/FFD700:太阳途径 序列{p.currentSunSequence}]\n";

                if (p.currentSunSequence <= 9) // 歌颂者
                {
                    string timeSing = p.isSinging && p.singTimer > 0 ? $" [c/00FF00:(剩余{p.singTimer / 60}s)]" : "";

                    text += $"序列九: [c/FFFFE0:歌颂者]\n";
                    text += "- [被动] 体质增强: 生命上限/防御力/生命回复提升\n";
                    text += $"- [主动] 赞美太阳 (Z键){timeSing}: \n"; // 这里加上了时间变量
                    text += "    > 吟唱圣歌，净化周围队友的负面状态\n";
                    text += "    > 赋予勇气(免疫恐惧)、力量(增伤)、敏捷(加速)\n";
                    text += "    > 持续回复灵性，对太阳信徒有额外加成\n";
                }
                if (p.currentSunSequence <= 8) // 祈光人
                {
                    text += $"序列八: [c/FFFACD:祈光人]\n";
                    string cdRadiance = p.sunRadianceCooldown > 0 ? $" [c/FF0000:({p.sunRadianceCooldown / 60}s)]" : "";

                    text += "- [被动] 白昼: 自带大范围圣洁光照\n";
                    text += "- [被动] 驱魔: 你的歌声现在能赋予队友寒冷免疫与亡灵克制\n";
                    text += $"- [主动] 日照 (X键){cdRadiance}: \n"; // 这里加上了冷却变量
                    text += "    > 召唤如正午烈阳般的光辉，对周围造成伤害\n";
                    text += "    > 对死尸/鬼魂造成3倍暴击，并致盲生者\n";
                    text += "- [仪式] 圣水制造: 可在水源旁廉价转化圣水\n";
                }
                if (p.currentSunSequence <= 7) // 太阳神官
                {

                    text += $"序列七: [c/FFD700:太阳神官]\n";
                    string cdLight = p.holyLightCooldown > 0 ? $" [c/FF0000:({p.holyLightCooldown / 60}s)]" : "";
                    string cdOath = p.holyOathCooldown > 0 ? $" [c/FF0000:({p.holyOathCooldown / 60}s)]" : "";
                    string cdFire = p.fireOceanCooldown > 0 ? $" [c/FF0000:({p.fireOceanCooldown / 60}s)]" : "";

                    text += "- [被动] 神圣体质: 免疫各类恶劣环境与负面状态\n";
                    text += "- [被动] 太阳光环: 持续净化并强化周围队友\n";
                    text += $"- [技能] 召唤圣光 (C键){cdLight}: 降下毁灭光柱\n";
                    text += $"- [技能] 神圣誓约 (V键){cdOath}: 获得随机强力增益\n";
                    text += $"- [技能] 光明之火 (G键){cdFire}: 召唤熔化大地的火海\n";
                }
                if (p.currentSunSequence <= 6) // 公证人
                {
                    text += $"序列六: [c/FFA500:公证人]\n";
                    string cdNotarize = p.notarizeCooldown > 0 ? $" [c/FF0000:({p.notarizeCooldown / 60}s)]" : "";

                    text += "- [被动] 商业公证: 商店购物半价，拾取范围扩大\n";
                    text += "- [被动] 规则体质: 生命防御大幅提升，免疫流血/中毒\n";
                    text += $"- [技能] 公证 (Z键){cdNotarize}: \n";
                    text += "    > 队友: 驱散Debuff并赋予增益\n";
                    text += "    > 敌人: 造成神圣伤害并削弱\n";

                    if (p.currentSunSequence == 6)
                    {
                        int target = 100; // LotMPlayer.PURIFICATION_RITUAL_TARGET
                        int current = p.purificationProgress;

                        string status = current >= target ? "[已完成]" : $"[{current}/{target}]";
                        string colorHex = current >= target ? "00FF00" : "FFA500";

                        text += $"\n[c/{colorHex}:[晋升仪式] 净化之路: {status}]\n";
                        text += "  (提示: 需净化100个不死生物以晋升光之祭司)\n";
                    }
                }

                if (p.currentSunSequence <= 5) // 光之祭司
                {
                    text += $"序列五: [c/FF8C00:光之祭司]\n";
                    string cdHoly = p.holyLightCooldown > 0 ? $" [c/FF0000:({p.holyLightCooldown / 60}s)]" : "";

                    text += "- [被动] 光之眼: 获得夜视与危险感知\n";
                    text += "- [被动] 净化光环: 自动灼烧周围死灵\n";
                    text += $"- [质变] 神圣之光 (C键){cdHoly}: 召唤光柱净化污秽\n";

                    // 【修正】序列 5 显示晋升序列 4 的条件
                    // 条件：审判进度 + 无暗十字
                    if (p.currentSunSequence == 5)
                    {
                        // 1. 击杀进度
                        int target = 20; // LotMPlayer.JUDGMENT_RITUAL_TARGET
                        int current = p.judgmentProgress;
                        string killStatus = current >= target ? "[审判完成]" : $"[审判: {current}/{target}]";

                        // 2. 物品要求
                        bool hasItem = p.Player.HasItem(ModContent.ItemType<Items.UnshadowedCross>());
                        string itemStatus = hasItem ? "[十字就绪]" : "[缺无暗十字]";

                        // 综合判定
                        bool ready = (current >= target) && hasItem;
                        string colorHex = ready ? "00FF00" : "FFA500";

                        text += $"\n[c/{colorHex}:[晋升仪式] 无暗者]\n";
                        text += $"  1. {killStatus}\n";
                        text += $"  2. 条件: {itemStatus}\n";
                        text += "  (提示: 审判20个强敌，并持有无暗十字)\n";
                    }
                }

                if (p.currentSunSequence <= 4) // 无暗者
                {
                    text += $"序列四: [c/FF4500:无暗者 (半神)]\n";
                    string cdNotarize = p.notarizeCooldown > 0 ? $" [c/FF0000:({p.notarizeCooldown / 60}s)]" : "";
                    string cdSpear = p.sunRadianceCooldown > 0 ? $" [c/FF0000:({p.sunRadianceCooldown / 60}s)]" : "";
                    string cdSun = p.fireOceanCooldown > 0 ? $" [c/FF0000:({p.fireOceanCooldown / 60}s)]" : "";

                    text += "- [半神] 无暗之域: 免疫黑暗，看破隐形\n";
                    text += "- [被动] 神圣盔甲: 极高防御与减伤\n";
                    text += $"- [技能] 无暗之枪 (X键){cdSpear}\n";
                    text += $"- [大招] 阳炎 (G键){cdSun}\n";

                    // 【修正】序列 4 显示晋升序列 3 的条件
                    // 条件：白天 + 徽章 + 纯净
                    if (p.currentSunSequence == 4)
                    {
                        bool isDay = Main.dayTime;
                        bool hasEmblem = p.Player.HasItem(ItemID.AvengerEmblem);
                        bool isClean = !p.Player.HasBuff(BuffID.Bleeding) && !p.Player.HasBuff(BuffID.Poisoned);

                        string timeStatus = isDay ? "[白天]" : "[等待白天]";
                        string itemStatus = hasEmblem ? "[徽章就绪]" : "[缺复仇者徽章]";
                        string cleanStatus = isClean ? "[身体纯净]" : "[有不洁状态]";

                        string colorHex = (isDay && hasEmblem && isClean) ? "00FF00" : "FFA500";

                        text += $"\n[c/{colorHex}:[晋升仪式] 正义导师]\n";
                        text += $"  条件: {timeStatus} {itemStatus} {cleanStatus}\n";
                        text += "  (提示: 白天，持有复仇者徽章，且无流血/中毒状态)\n";
                    }
                }

                if (p.currentSunSequence <= 3) // 正义导师
                {
                    text += $"序列三: [c/FF6347:正义导师 (圣者)]\n";
                    string cdJudge = p.notarizeCooldown > 0 ? $" [c/FF0000:({p.notarizeCooldown / 60}s)]" : "";
                    string cdContract = p.holyOathCooldown > 0 ? $" [c/FF0000:({p.holyOathCooldown / 60}s)]" : "";

                    text += "- [被动] 圣者体质 / 召唤上限+3\n";
                    text += $"- [质变] 正义审判 (Z键){cdJudge}: 巨额单体伤害\n";
                    text += $"- [技能] 神圣契约 (V键){cdContract}: 提升全属性\n";

                    if (p.currentSunSequence == 3)
                    {
                        // 这里沿用你的逻辑：需击败光之女皇
                        bool condition = NPC.downedEmpressOfLight;
                        // 补上序列2魔药里写的条件：需持有日耀圣物
                        bool hasArtifact = p.Player.HasItem(ItemID.DayBreak) || p.Player.HasItem(ItemID.SolarEruption);

                        string bossStatus = condition ? "[光女已败]" : "[需击败光之女皇]";
                        string itemStatus = hasArtifact ? "[圣物就绪]" : "[缺日耀武器]";

                        bool ready = condition && hasArtifact;
                        string colorHex = ready ? "00FF00" : "FFA500";

                        text += $"\n[c/{colorHex}:[晋升仪式] 逐光者]\n";
                        text += $"  条件: {bossStatus} {itemStatus}\n";
                        text += "  (提示: 击败光之女皇，并持有日耀圣物)\n";
                    }
                }

                if (p.currentSunSequence <= 2) // 逐光者
                {
                    text += $"序列二: [c/FF0000:逐光者 (天使)]\n";
                    text += "- [被动] 光化重组: 闪避瞬移\n";
                    text += "- [技能] 太阳使者 (P键): 无限飞行与狱火光环\n";

                    if (p.currentSunSequence == 2)
                    {
                        bool isNoon = Main.dayTime && Main.time >= 27000 && Main.time <= 36000;
                        int npcCount = 0;
                        for (int i = 0; i < Main.maxNPCs; i++) if (Main.npc[i].active && Main.npc[i].townNPC) npcCount++;

                        string countStatus = npcCount >= 15 ? "[信徒充足]" : $"[信徒: {npcCount}/15]";
                        string timeStatus = isNoon ? "[正午]" : "[等待正午]";

                        bool ready = isNoon && npcCount >= 15;
                        string colorHex = ready ? "00FF00" : "FFA500";

                        text += $"\n[c/{colorHex}:[晋升仪式] 纯白国度]\n";
                        text += $"  条件: {timeStatus} {countStatus}\n";
                        text += "  (提示: 正午时分，至少15位城镇NPC存活)\n";
                    }
                }

                if (p.currentSunSequence <= 1)
                {
                    text += $"[c/FFFFFF:序列1: 纯白天使 (天使之王)]\n";
                    text += "- [被动] 神圣之国: 压制全场，斩杀亡灵\n";
                    text += "- [技能] 纯白之光 / 信仰之仆\n";
                    text += "  (已达当前版本顶峰)";
                }
            }
            else if (p.currentDemonessSequence <= 9)
            {
                text += $"[c/FF1493:魔女途径 序列{p.currentDemonessSequence}]\n";

                if (p.currentDemonessSequence <= 9) // 刺客
                {
                    text += $"序列九: [c/FF69B4:刺客]\n"; // 热粉色
                    text += "- [被动] 鹰之视力: 获得夜视与生物探测\n";
                    text += "- [被动] 体态轻盈: 移速+20% /跳跃提升免疫摔伤\n";
                    text += "- [被动] 致命一击: 全暴击+10% / 护甲穿透+5\n";
                    text += "- [被动] 阴影潜伏: 大幅降低敌人仇恨\n";
                }
                if (p.currentDemonessSequence <= 8)
                {
                    text += $"序列八: [c/FF1493:教唆者]\n"; // 深粉色
                    text += "- [被动] 教唆: 攻击有20%几率使敌人混乱\n";
                    text += "- [被动] 借刀杀人: 召唤伤害+15% / 栏位+1\n";
                    text += "- [被动] 幕后黑手: 仇恨值大幅降低\n";
                }
                if (p.currentDemonessSequence <= 7)
                {
                    text += $"序列七: [c/FF00FF:女巫]\n"; // 亮紫色
                    text += "- [被动] 魅惑: 商店打折 / 仇恨极大降低\n";
                    text += "- [被动] 黑魔法: 攻击附带霜冻与暗影焰\n";
                    text += "- [被动] 隐身: 站立不动时潜入阴影(隐形+增伤)\n";
                    text += "- [魔法] 魔法伤害+20%\n";
                    text += "- [诅咒] 对负面状态缠身的敌人造成爆发伤害\n";
                }
                if (p.currentDemonessSequence <= 6) // 欢愉魔女
                {
                    text += $"序列六: [c/FF00FF:欢愉魔女]\n";
                    text += "- [被动] 魅惑EX: 仇恨极低 / 魔法暴击+10%\n";
                    text += "- [被动] 欢愉之触: 攻击必定附加剧毒与虚弱\n";
                    text += "- [特效] 魔女蛛丝: 攻击几率生成蛛丝定身敌人\n";

                    string keyName = "未绑定";
                    if (LotMKeybinds.Demoness_MirrorSwitch != null && LotMKeybinds.Demoness_MirrorSwitch.GetAssignedKeys().Count > 0)
                    {
                        keyName = LotMKeybinds.Demoness_MirrorSwitch.GetAssignedKeys()[0];
                    }
                    text += $"- [主动] 镜子替身 (Z键): 消耗灵性瞬移\n";

                    if (p.currentDemonessSequence == 6)
                    {
                        int currentTimer = p.afflictionRitualTimer; // 活火站立时间
                        int targetTimer = 3600;
                        int currentMins = currentTimer / 60; // 显示秒数更直观

                        bool isRitualReady = currentTimer >= targetTimer;
                        string ritualStatus = isRitualReady ? "[肉体已重铸]" : $"[烈火焚身: {currentMins}/60 秒]";

                        string colorHex = isRitualReady ? "00FF00" : "FFA500";

                        text += $"\n[c/{colorHex}:[晋升仪式] 痛苦魔女]\n";
                        text += $"  条件: {ritualStatus}\n";
                        text += "  (提示: 站在活火块上忍受灼烧1分钟)\n";
                    }
                }
                if (p.currentDemonessSequence <= 5) // 痛苦魔女
                {
                    text += $"序列五: [c/FF1493:痛苦魔女]\n";

                    text += "- [被动] 痛楚光环 / 免疫猛毒，自动散播疾病\n";
                    text += "- [技能] 镜面分身 (Z键): 消耗灵性制造虚幻倒影\n";
                    text += "- [空间] 镜中世界 : 在秘银砧制作镜子密钥前往镜世界\n";
                    text += "- [技能] 魔女之发 (X键): 自动追踪的黑焰发丝\n";
                    text += "- [技能] 蛛丝操控 (C键): 投掷蛛网定身敌人\n";

                    if (p.currentDemonessSequence == 5)
                    {
                        string status = "[需制造灾难]";

                        text += $"\n[c/FFA500:[晋升仪式] 绝望魔女]\n";
                        text += $"  条件: {status}\n";
                        text += "  (提示: 散播绝望，在盛大的灾难中收割生命)\n";
                    }
                }
                if (p.currentDemonessSequence <= 4) // 绝望魔女
                {
                    text += $"序列四: [c/DC143C:绝望魔女 (半神)]\n";
                    string keyName = "未绑定";
                    if (LotMKeybinds.Demoness_DespairSkill != null && LotMKeybinds.Demoness_DespairSkill.GetAssignedKeys().Count > 0)
                    {
                        keyName = LotMKeybinds.Demoness_DespairSkill.GetAssignedKeys()[0];
                    }
                    text += "- [被动] 绝望灾祸: 瘟疫范围翻倍，附加黑焰/冻伤/破甲\n";
                    text += "- [被动] 半神之躯: 获得15%免伤与极强自愈，免疫黑火\n";
                    text += $"- [技能] 绝望冰晶 ({keyName}键): 消耗灵性爆发黑焰冰弹\n";
                    if (p.currentDemonessSequence == 4)
                    {
                        string status = "[需探寻不老之谜]";

                        text += $"\n[c/FFA500:[晋升仪式] 不老魔女]\n";
                        text += $"  条件: {status}\n";
                        text += "  (提示: 追寻永恒，让时光在你身上停驻)\n";
                    }
                }
                if (p.currentDemonessSequence <= 3) // 不老魔女
                {
                    text += $"序列三: [c/B22222:不老魔女 (圣者)]\n";

                    string keyName = "未绑定";
                    if (LotMKeybinds.Demoness_PetrifySkill != null && LotMKeybinds.Demoness_PetrifySkill.GetAssignedKeys().Count > 0)
                    {
                        keyName = LotMKeybinds.Demoness_PetrifySkill.GetAssignedKeys()[0];
                    }

                    text += "- [被动] 青春永驻: 极高的生命再生 / 免疫衰老与虚弱\n";

                    string rebirthStatus = (p.unagingRebirthCooldown > 0)
                        ? $"[c/FF0000:冷却中 {(p.unagingRebirthCooldown / 60f):F1}s]" // 显示小数
                        : "[c/00FF00:就绪]";
                    text += $"- [被动] 镜面重生: 消耗灵性抵挡致死伤害 {rebirthStatus}\n";

                    text += "- [被动] 欢愉汲取: 攻击有概率吸取敌人生命\n";

                    int currentCD = p.PetrificationGazeCD;
                    string skillStatus;

                    if (currentCD > 0)
                    {
                        // 【优化】使用浮点除法并保留1位小数，实现实时跳动效果
                        skillStatus = $"[c/FF4500:冷却: {(currentCD / 60f):F1}s]";
                    }
                    else
                    {
                        if (p.spiritualityCurrent >= 500)
                            skillStatus = "[c/00FF00:就绪]";
                        else
                            skillStatus = "[c/808080:灵性不足]";
                    }

                    text += $"- [技能] 石化凝视 ({keyName}键): 时停并石化万物 {skillStatus}\n";

                    if (p.currentDemonessSequence == 3)
                    {
                        text += $"\n[c/FF4500:[晋升仪式] 灾难魔女]\n";
                        text += "  条件: [需散播毁灭与寒冷]\n";
                        text += "  (提示: 只有在大规模的死亡与冻结中，灾难的魔药才会凝结)\n";
                    }
                }
                if (p.currentDemonessSequence <= 2) // 灾难魔女
                {
                    text += $"序列二: [c/DC143C:灾难魔女 (天使)]\n";
                    string keyName = "未绑定";
                    if (LotMKeybinds.Demoness_Catastrophe != null && LotMKeybinds.Demoness_Catastrophe.GetAssignedKeys().Count > 0)
                    {
                        keyName = LotMKeybinds.Demoness_Catastrophe.GetAssignedKeys()[0];
                    }

                    text += "- [被动] 灾难之躯: 免疫火焰、寒冰、雷电、窒息与强风\n";

                    bool weatherBuffActive = Main.raining || Main.windSpeedCurrent > 20 || p.Player.ZoneSnow || p.Player.ZoneDesert;
                    string weatherStatus = weatherBuffActive ? "[c/00FF00:已激活]" : "[c/808080:未激活]";
                    text += $"- [被动] 自然主宰: 在恶劣天气下属性大幅提升 {weatherStatus}\n";

                    text += "- [被动] 毁灭本源: 魔法伤害+60% / 极高生命回复\n";
                    int currentCD = p.catastropheCooldown;
                    string skillStatus;

                    if (currentCD > 0)
                    {
                        skillStatus = $"[c/FF0000:冷却中: {(currentCD / 60f):F1}s]";
                    }
                    else
                    {
                        if (p.spiritualityCurrent >= 5000)
                            skillStatus = "[c/00FF00:就绪]";
                        else
                            skillStatus = "[c/808080:灵性不足 (需5000)]";
                    }

                    text += $"- [大招] 天灾降临 ({keyName}键): 引发全图气象灾难与毁灭打击 {skillStatus}\n";

                    if (p.currentDemonessSequence == 2)
                    {
                        text += $"\n[c/FF4500:[晋升仪式] 末日魔女]\n";
                        text += "  条件: [需毁灭旧时代的象征]\n";
                        text += "  (提示: 让真正的末日降临于世)\n";
                    }
                }
                if (p.currentDemonessSequence <= 1) // 末日魔女
                {
                    text += $"序列一: [c/800080:末日魔女 (从神)]\n";

                    string keyName = "未绑定";
                    if (LotMKeybinds.Demoness_Apocalypse != null && LotMKeybinds.Demoness_Apocalypse.GetAssignedKeys().Count > 0)
                    {
                        keyName = LotMKeybinds.Demoness_Apocalypse.GetAssignedKeys()[0];
                    }

                    text += "- [被动] 神性之躯: 免疫几乎所有负面状态\n";
                    text += "- [被动] 末日威压: 普通敌人无法行动，Boss攻击力大幅削减\n";
                    text += "- [被动] 终结: 魔法消耗减半，受到伤害减免40%\n";
                    int currentCD = p.apocalypseCooldown;
                    string skillStatus;
                    if (currentCD > 0)
                    {
                        int minutes = currentCD / 3600;
                        int seconds = (currentCD % 3600) / 60;
                        skillStatus = $"[c/FF0000:冷却中: {minutes}m {seconds}s]";
                    }
                    else
                    {
                        if (p.spiritualityCurrent >= 10000)
                            skillStatus = "[c/00FF00:就绪]";
                        else
                            skillStatus = "[c/808080:灵性不足 (需10000)]";
                    }

                    text += $"- [权柄] 末日降临 ({keyName}键): 冻结时空，抹除弹幕，处决众生 {skillStatus}\n";
                }
            }
            // 8. 命运途径 (Wheel)
            else if (p.currentWheelSequence <= 9)
            {
                // 标题颜色：银色/水银色
                text += $"[c/C0C0C0:命运途径 序列{p.currentWheelSequence}]\n";

                if (p.currentWheelSequence <= 9) // 怪物
                {
                    text += $"序列九: [c/D3D3D3:怪物]\n"; // 浅灰色
                    text += "- [被动] 危险直觉: 获得危险感知与生物探测视野\n";
                    text += "- [被动] 命运眷顾: 暴击率+5% / 运气和生命上限小幅提升\n";
                    text += "- [被动] 怪物感官: 获得夜视能力 / 移动速度提升\n";
                }
                if (p.currentWheelSequence <= 8) // 机器
                {
                    text += $"序列八: [c/A9A9A9:机器]\n"; // 深灰色
                    text += "- [被动] 精准计算: 暴击率+10% / 挖掘距离+2\n";
                    text += "- [被动] 格斗射击: 近战/远程伤害+20% / 防御+5\n";
                    text += "- [能力] 占卜: 按 J 键使用占卜术（与愚者途径共用按键）\n";
                    text += "- [被动] 反占卜: 拥有概率闪避攻击的能力\n";
                }
                if (p.currentWheelSequence <= 7)
                {
                    text += $"序列七: [c/00FF00:幸运儿]\n";
                    text += $"- [状态] 当前运气: {Main.LocalPlayer.luck:F2}\n";
                    text += "- [被动] 意外之财: 移动时偶尔会在脚边捡到钱\n";
                    text += "- [被动] 人体描边: 受到攻击时有几率幸运地毫发无伤\n";
                    string sign = p.luckFluctuation >= 0 ? "+" : "";
                    text += $"- [被动] 命运骰子: 暴击率波动 {sign}{p.luckFluctuation:F1}%\n";
                }
                if (p.currentWheelSequence <= 6) // 灾祸教士
                {
                    text += $"序列六: [c/FF4500:灾祸教士]\n";
                    text += "- [被动] 灾祸光环: 自动引来陨石与雷电攻击周围敌人\n";
                    text += "- [被动] 厄运规避: 大幅提升防御与免伤\n";
                    string cdText = "";
                    if (p.psychicStormCooldown > 0)
                    {
                        float cdSeconds = p.psychicStormCooldown / 60f;
                        cdText = $" [c/FF0000:(冷却中: {cdSeconds:F1}s)]"; // 红色显示冷却
                    }
                    else
                    {
                        cdText = " [c/00FF00:(就绪)]"; // 绿色显示就绪
                    }
                    text += $"- [主动] 精神风暴: 按 [c/FFFF00:精神风暴键(V)] 触发{cdText}\n";
                    text += "  消耗50灵性，使周围敌人混乱并造成伤害\n";
                }
                if (p.currentWheelSequence <= 5) // 赢家
                {
                    text += $"序列五: [c/FFD700:赢家]\n"; // 金色

                    text += $"- [核心] 命运主宰: 幸运值大幅提升 (当前: {Main.LocalPlayer.luck:F2})\n";
                    float saveChance = 0.20f + (Main.LocalPlayer.luck * 0.1f);
                    if (saveChance > 0.6f) saveChance = 0.6f;
                    if (Main.LocalPlayer.luck <= 0) saveChance = 0f;
                    text += $"- [被动] 赢家效率: {saveChance:P0} 几率不消耗弹药\n"; // :P0 格式化为百分比

                    text += "- [被动] 戏剧逆转: 致命危机时有几率触发剧情杀闪避\n";
                    text += "- [被动] 自动追击: 幸运时子弹会自动追踪敌人\n";
                    text += "- [攻击] 厄运赋予: 攻击导致敌人炸膛、手滑或弱点暴露\n";
                }
                if (p.currentWheelSequence <= 4) // 厄运法师
                {
                    text += $"序列四: [c/8A2BE2:厄运法师 (半神)]\n"; // 紫色
                    text += "- [被动] 水银之躯: 防御大幅增加，获得15%绝对免伤\n";
                    text += "- [被动] 绝对灵感: 对Boss级敌人暴击率飙升 (+20%)\n";
                    text += $"- [被动] 厄运领域: 随幸运值扩大范围，自动诅咒并概率秒杀周围敌人\n";
                    text += "- [升级] 精神风暴: 范围翻倍，附带“疯狂”腐蚀\n";
                    string blessCD = p.fateBlessingCooldown > 0 ? $"[c/FF0000:({p.fateBlessingCooldown / 60f:F1}s)]" : "[c/00FF00:(就绪)]";
                    text += $"- [主动] 命运赐福: 按 [c/FFFF00:命运赐福键(B)]，透支自身运气为全队加上六种Buff {blessCD}\n";
                }
                if (p.currentWheelSequence <= 3) // 怪人
                {
                    text += $"\n序列三: [c/9932CC:怪人 (圣者)]\n";
                    text += "- [被动] 怪人之躯: 生命+600 / 通用伤+10% / 暴击率+10% / 防御+15 / 免伤+15%\n";
                    text += "- [被动] 怪人之眼: 暴击伤害 +30%\n";
                    string nullifyCD = p.fateNullifyCooldown > 0 ? $"[c/FF0000:({p.fateNullifyCooldown / 60f:F1}s)]" : "[c/00FF00:(就绪)]";
                    text += $"- [被动] 不可定数: 致命伤30%几率被命运抹除 {nullifyCD}\n";
                    text += $"- [被动] 概率反噬: 每持有一个减益, 运气+0.2 (上限+1.0)  当前堆叠: {p.anomalyDebuffStack}\n";
                    text += "- [被动] 厄运链: 厄运领域暴毙的敌人会向邻近最多3个传染厄运 (30%血量真伤)\n";
                    text += "- [升级] 厄运暴毙: 概率×3 (敌人更易被命运反噬)\n";
                    string diceCD = p.fateDiceCooldown > 0 ? $"[c/FF0000:({p.fateDiceCooldown / 60f:F1}s)]" : "[c/00FF00:(就绪)]";
                    text += $"- [主动] 命运骰子 (M): 消耗200灵性投出1-6点, 触发不同的命运效果 {diceCD}\n";
                    if (p.fateBlessingActiveTimer > 0)
                        text += $"  [c/FFD700:✦ 命运庇护激活中: {p.fateBlessingActiveTimer / 60f:F1}s (额外50%减伤)]\n";
                }
                if (p.currentWheelSequence <= 2) // 先知
                {
                    text += $"\n序列二: [c/B080FF:先知 (半神 - 命运的宠儿)]\n";
                    text += "- [被动] 完整神话生物形态: 生命+900 / 通用伤+15% / 暴击率+15% / 防御+25 / 免伤+20%\n";
                    text += "- [被动] 命运洞察: 暴击伤害再+20% (累积+50%)\n";
                    text += "- [被动] 命运的宠儿: 幸运+2.0, 水银CD结束时再+1.0\n";
                    string mercuryState;
                    if (p.mercuryDodgeTimer > 0)
                        mercuryState = $"[c/C0C0C0:✦ 回避激活中 {p.mercuryDodgeTimer / 60f:F1}s]";
                    else if (p.mercuryDodgeCooldown > 0)
                        mercuryState = $"[c/FF0000:冷却 {p.mercuryDodgeCooldown / 60f:F1}s]";
                    else
                        mercuryState = "[c/00FF00:就绪]";
                    text += $"- [被动] 水银之躯: 受伤后10秒免疫一切伤害和减益 {mercuryState}\n";
                    text += "- [被动] 预言术: 杀敌时15%几率(启示中30%)预言下一击, 命中即秒杀\n";
                    if (p.prophecyMarked)
                        text += $"  [c/B080FF:✦ 已预言下一击: 剩 {p.prophecyDuration / 60f:F1}s]\n";
                    string fortuneCD = p.wordsOfFortuneCooldown > 0 ? $"[c/FF0000:({p.wordsOfFortuneCooldown / 60f:F1}s)]" : "[c/00FF00:(就绪)]";
                    string misfortuneCD = p.wordsOfMisfortuneCooldown > 0 ? $"[c/FF0000:({p.wordsOfMisfortuneCooldown / 60f:F1}s)]" : "[c/00FF00:(就绪)]";
                    string revelCD = p.revelationCooldown > 0 ? $"[c/FF0000:({p.revelationCooldown / 60f:F1}s)]" : "[c/00FF00:(就绪)]";
                    text += $"- [主动] 福祸之言-福 ([c/FFFF00:福祸之福键(K)]): 消耗300灵性,4000范围友军获60秒10种Buff+治愈+灵性满 {fortuneCD}\n";
                    text += $"- [主动] 福祸之言-祸 ([c/FFFF00:福祸之祸键(L)]): 消耗300灵性,4000范围敌人受当前血25%真伤+60秒8种厄运(Boss削10%) {misfortuneCD}\n";
                    text += $"- [主动] 命运启示 ([c/FFFF00:命运启示键(U)]): 消耗500灵性,10秒内所有概率事件极致偏向最优解 {revelCD}\n";
                    if (p.revelationActiveTimer > 0)
                        text += $"  [c/B080FF:✦ 命运启示激活中: {p.revelationActiveTimer / 60f:F1}s (暴击率+50/伤害+25%/减伤+30%)]\n";
                    else if (p.revelationBackfireTimer > 0)
                        text += $"  [c/AA88BB:✱ 启示反噬期: {p.revelationBackfireTimer / 60f:F1}s (-5%伤害)]\n";
                }
                if (p.currentWheelSequence <= 1) // 巨蛇
                {
                    text += $"\n序列一: [c/C0C0FF:巨蛇 (从神 - 水银/吞尾/命运之蛇)]\n";
                    text += "- [被动] 命运之蛇之躯: 生命+2500 / 通用伤+50% / 暴击+35 / 防御+80 / 免伤+20% / 攻速+40% / 移速+50%\n";
                    text += "- [被动] 万物之蛇: 暴击伤害再+30% (累积+80%)\n";
                    text += "- [被动] 命运的化身: 幸运+5.0\n";
                    text += "- [被动] 隐藏命运: 免疫几乎所有 debuff (灵液/诅咒/燃烧/虚弱/破甲/混乱/眩晕/沉默/冰冻等)\n";
                    string phaseState;
                    if (p.mercuryPhaseTimer > 0)
                        phaseState = $"[c/C0C0FF:✦ 相位中 {p.mercuryPhaseTimer / 60f:F1}s (无敌)]";
                    else
                        phaseState = $"[c/AAAAFF:下次 {p.mercuryPhaseCooldown / 60f:F1}s]";
                    text += $"- [被动] 水银相位: 每10秒进入3秒完全无敌 {phaseState}\n";
                    string loopCD = p.fateLoopCooldown > 0 ? $"[c/FF0000:({p.fateLoopCooldown / 60f:F1}s)]" : "[c/00FF00:(就绪)]";
                    text += $"- [主动] 命运循环 ([c/FFFF00:Y]): 消耗1000灵性,800范围5秒后回滚NPC位置(Boss除外) {loopCD}\n";
                    if (p.fateLoopActiveTimer > 0)
                        text += $"  [c/C880FF:✦ 命运循环激活中: {p.fateLoopActiveTimer / 60f:F1}s]\n";
                    string autoState = p.restartAutoCooldown > 0
                        ? $"[c/FF8888:CD {p.restartAutoCooldown / 60f:F0}s]" : "[c/00FF00:(就绪)]";
                    string manState = p.restartManualCooldown > 0
                        ? $"[c/FF8888:CD {p.restartManualCooldown / 60f:F0}s]" : "[c/00FF00:(就绪)]";
                    text += $"- [被动] 重启循环: 死亡瞬间消耗5000灵性,回到10秒前(满血+清debuff) {autoState}\n";
                    text += $"- [主动] 主动重启 ([c/FFFF00:H]): 消耗3000灵性主动撤退至10秒前 {manState}\n";
                }
            }
            // 9. 学徒/门 途径 (Door)
            else if (p.currentDoorSequence <= 9)
            {
                text += $"[c/E8C878:学徒/门途径 序列{p.currentDoorSequence}]\n"; // 金黄色

                if (p.currentDoorSequence <= 9) // 学徒
                {
                    text += $"序列九: [c/F0D080:学徒]\n";
                    text += "- [被动] 旅行家身躯: 生命+50 / 通用伤+5% / 暴击+5 / 防御+5\n";
                    text += "- [被动] 自由的足迹: 移动速度+20% / 挖掘速度+20%\n";
                    string doorState = p.doorOpenCooldown > 0
                        ? $"[c/FF8888:CD {p.doorOpenCooldown / 60f:F1}s]" : "[c/00FF00:(就绪)]";
                    text += $"- [主动] 开门 ([c/FFFF00:E]): 10灵性, 穿过面前1-5格厚的常规墙壁 (3秒CD) {doorState}\n";
                    text += "  [c/A0A0A0:墙壁过厚(>5格)或墙的另一边是固体时无法穿越]\n";
                }
                if (p.currentDoorSequence <= 8) // 戏法大师
                {
                    text += $"\n序列八: [c/E8C878:戏法大师]\n";
                    text += "- [被动] 戏法之躯: 生命+50(累计+100) / 法术伤+10% / 暴击+5 / 法力+60 / 移速+30% / 跳跃增强\n";
                    string trickState = p.trickCooldown > 0
                        ? $"[c/FF8888:CD {p.trickCooldown / 60f:F1}s]" : "[c/00FF00:(就绪)]";
                    text += $"- [主动] 戏法 ([c/FFFF00:切换T / 释放R]): 每次20灵性 {trickState}\n";
                    text += $"  当前选中: [c/FFE878:{LotMPlayer.TrickNames[p.selectedTrick]}] ({p.selectedTrick + 1}/11)\n";
                    text += "  [c/A0A0A0:闪光/黑幕/转移气体/巨响/冰冻射线/电击/造雾/刮风/点火/摔倒术/驱物]\n";
                    string escState = p.trickEscapeCooldown > 0
                        ? $"[c/FF8888:CD {p.trickEscapeCooldown / 60f:F0}s]" : "[c/00FF00:(就绪)]";
                    text += $"- [被动] 逃脱戏法: 受到大额伤害时自动闪现躲避 (30秒CD) {escState}\n";
                    if (p.electricChargeTimer > 0)
                        text += $"  [c/A0C8FF:✦ 电击充能中: {p.electricChargeTimer / 60f:F1}s (近战附加感电)]\n";
                }
                if (p.currentDoorSequence <= 7) // 占星人
                {
                    text += $"\n序列七: [c/FFD080:占星人]\n";
                    text += "- [被动] 占星之躯: 生命+50(累计+150) / 法术伤+10% / 暴击+8 / 防御+10\n";
                    text += "- [被动] 灵性直觉: 受到大伤害时30%几率自动闪避 (60秒CD)\n";
                    string intState = p.astrologyIntuitionCooldown > 0
                        ? $"[c/FF8888:CD {p.astrologyIntuitionCooldown / 60f:F0}s]" : "[c/00FF00:(就绪)]";
                    text += $"  {intState}\n";
                    text += "- [被动] 危险预感: 附近有Boss/精英敌人时自动+15%减伤\n";
                    text += "- [被动] 灵性干扰: 每5秒1/3几率让附近敌人陷入混乱\n";
                    string astroState = p.astrologyCooldown > 0
                        ? $"[c/FF8888:CD {p.astrologyCooldown / 60f:F0}s]" : "[c/00FF00:(就绪)]";
                    text += $"- [主动] 占星术 ([c/FFFF00:G]): 300灵性,15秒预兆增益(伤害+25%/暴击+25/减伤+10%) {astroState}\n";
                    if (p.astrologyActiveTimer > 0)
                        text += $"  [c/FFD850:✦ 占星激活中: {p.astrologyActiveTimer / 60f:F1}s]\n";
                }
                if (p.currentDoorSequence <= 6) // 记录官
                {
                    text += $"\n序列六: [c/E8A8FF:记录官]\n";
                    text += "- [被动] 记录之躯: 生命+50(累计+200) / 法术伤+10% / 暴击+5 / 防御+10 / 法力+40 / 回血+2hp/s\n";
                    text += "- [被动] 我来到我看见我记录: 击杀有非凡能力的敌人(在能力库内)记录其符号\n";
                    text += "  [c/A0A0A0:借用蠕动饥饿能力库 | 凡兽100%/半神30%/神性5% | 自动去重]\n";

                    text += $"- [主动] 普通记录 ([c/FFFF00:F]): 储存 [c/FFE878:{p.recorderNormalList.Count}/{LotMPlayer.RECORDER_NORMAL_MAX}]\n";
                    text += "  单按 F = 循环切换选中 | Shift+F = 释放当前选中的能力\n";
                    if (p.recorderNormalList.Count > 0)
                    {
                        int idx = p.recorderSelectedNormal % p.recorderNormalList.Count;
                        int npcType = p.recorderNormalList[idx];
                        string name = Lang.GetNPCNameValue(npcType);
                        text += $"  当前选中: [c/FFE878:{name}] ({idx + 1}/{p.recorderNormalList.Count})\n";
                    }

                    text += $"- [主动] 神性记录 ([c/FFFF00:C]): 储存 [c/D080FF:{p.recorderDivineList.Count}/{p.GetRecorderDivineMax()}]\n";
                    if (p.recorderDivineList.Count > 0)
                    {
                        int idx = p.recorderSelectedDivine % p.recorderDivineList.Count;
                        int npcType = p.recorderDivineList[idx];
                        string name = Lang.GetNPCNameValue(npcType);
                        text += $"  当前神性: [c/D080FF:{name}]\n";
                    }
                    string cdState = p.recorderCooldown > 0
                        ? $"[c/FF8888:CD {p.recorderCooldown / 60f:F1}s]" : "[c/00FF00:(就绪)]";
                    text += $"  {cdState}\n";
                }
                if (p.currentDoorSequence <= 5) // 旅行家
                {
                    text += $"\n序列五: [c/B0E0FF:旅行家]\n";
                    text += "- [被动] 旅行家之躯: 生命+50(累计+250) / 法术伤+10% / 防御+12 / 移速+10%\n";
                    text += $"- [被动] 定位: 你的当前坐标 ({(int)(p.Player.position.X / 16f)}, {(int)(p.Player.position.Y / 16f)})\n";
                    text += "- [被动] 无形之手: 20格范围内的物品/金币自动吸取至你身边\n";
                    text += "- [被动] 记录强化: 神性记录上限从1提升至4个\n";
                    string gateState = p.travelerGateCooldown > 0
                        ? $"[c/FF8888:CD {p.travelerGateCooldown / 60f:F0}s]" : "[c/00FF00:(就绪)]";
                    text += $"- [主动] 旅行家之门 ([c/FFFF00:J]): 500灵性, 打开地图后点击任意位置全图传送 (30秒CD) {gateState}\n";
                    if (p.travelerGateAwaitingMapClick)
                        text += "  [c/FFD000:▶ 等待地图点击中 (再按J取消)]\n";
                    string blinkState = p.travelerBlinkCooldown > 0
                        ? $"[c/FF8888:CD {p.travelerBlinkCooldown / 60f:F1}s]" : "[c/00FF00:(就绪)]";
                    text += $"- [主动] 闪现 ([c/FFFF00:K]): 50灵性, 朝鼠标方向短距闪现30格 (1秒CD) {blinkState}\n";
                }
            }

            return text;
        }
    }
    // ========================================================================
    // 【美化版】自定义水平滚动条
    // ========================================================================
    public class SimpleHorizontalScrollbar : UIElement
    {
        public float ViewPosition = 0f;
        public bool IsDragging = false;

        private float _viewSize = 1f;
        private float _maxViewSize = 20f;
        private float _dragOffsetX = 0f;

        public SimpleHorizontalScrollbar()
        {
            this.Height.Set(24f, 0f); // 稍微加高
        }

        public void SetView(float viewSize, float maxViewSize)
        {
            _viewSize = viewSize;
            _maxViewSize = maxViewSize;
            ViewPosition = MathHelper.Clamp(ViewPosition, 0f, Math.Max(0f, _maxViewSize - _viewSize));
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (IsDragging)
            {
                if (!Main.mouseLeft)
                {
                    IsDragging = false;
                }
                else
                {
                    CalculatedStyle inner = GetInnerDimensions();
                    float trackWidth = inner.Width;
                    float handleWidth = GetHandleWidth();
                    float availableWidth = trackWidth - handleWidth;

                    if (availableWidth > 0)
                    {
                        float mouseX = Main.mouseX - inner.X - _dragOffsetX;
                        float pct = mouseX / availableWidth;
                        pct = MathHelper.Clamp(pct, 0f, 1f);
                        ViewPosition = pct * (_maxViewSize - _viewSize);
                    }
                }
            }
        }

        public override void LeftMouseDown(UIMouseEvent evt)
        {
            base.LeftMouseDown(evt);
            CalculatedStyle inner = GetInnerDimensions();

            float trackWidth = inner.Width;
            float handleWidth = GetHandleWidth();
            float availableWidth = trackWidth - handleWidth;
            float scrollPct = 0f;
            if (_maxViewSize > _viewSize)
                scrollPct = ViewPosition / (_maxViewSize - _viewSize);

            float handleX = inner.X + scrollPct * availableWidth;

            // 扩大点击判定范围，防止点不到
            if (evt.MousePosition.X >= handleX && evt.MousePosition.X <= handleX + handleWidth)
            {
                IsDragging = true;
                _dragOffsetX = evt.MousePosition.X - handleX;
            }
            // 点击轨道空处直接跳转 (类似原版)
            else if (evt.MousePosition.X > inner.X && evt.MousePosition.X < inner.X + inner.Width)
            {
                if (evt.MousePosition.X < handleX)
                    ViewPosition = Math.Max(0f, ViewPosition - _viewSize); // Page Up
                else
                    ViewPosition = Math.Min(_maxViewSize - _viewSize, ViewPosition + _viewSize); // Page Down
            }
        }

        private float GetHandleWidth()
        {
            CalculatedStyle inner = GetInnerDimensions();
            if (_maxViewSize <= _viewSize) return inner.Width;
            // 最小宽度限制为 30，防止滑块太小
            return Math.Max(30f, inner.Width * (_viewSize / _maxViewSize));
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle inner = GetInnerDimensions();

            // 1. 绘制轨道背景 (更有质感的深色槽)
            // 使用 Terraria.Utils.DrawInvBG 绘制类似物品栏的背景风格
            Rectangle trackRect = new Rectangle((int)inner.X, (int)inner.Y + 4, (int)inner.Width, 16);
            Terraria.Utils.DrawInvBG(spriteBatch, trackRect, new Color(20, 20, 30, 200));

            // 如果内容不需要滚动，就不画滑块
            if (_maxViewSize <= _viewSize) return;

            // 2. 计算滑块位置
            float trackWidth = inner.Width;
            float handleWidth = GetHandleWidth();
            float availableWidth = trackWidth - handleWidth;
            float scrollPct = ViewPosition / (_maxViewSize - _viewSize);
            float handleX = inner.X + scrollPct * availableWidth;

            // 3. 绘制滑块 (仿原版风格)
            Rectangle handleRect = new Rectangle((int)handleX, (int)inner.Y + 4, (int)handleWidth, 16);

            // 滑块颜色：拖拽/悬停时变亮
            Color handleColor = IsDragging || IsMouseHovering ? new Color(255, 255, 200) : new Color(200, 200, 200);

            // 绘制主体
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, handleRect, handleColor);

            // 绘制边框 (让它看起来立体一点)
            Terraria.Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.ItemStack.Value, "", handleX, inner.Y + 6, Color.Black, Color.Transparent, Vector2.Zero);

            // 或者简单地画一个带黑边的框
            Terraria.Utils.DrawInvBG(spriteBatch, handleRect, handleColor * 0.8f);
        }
    }
}