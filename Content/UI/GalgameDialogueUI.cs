using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Audio;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using zhashi.Content.UI.Profiles;
using zhashi.Content.Items;
using zhashi.Content.NPCs.Town;

namespace zhashi.Content.UI
{
    public class GalgameDialogueUI : UIState
    {
        // UI 控件
        private UIElement rootArea;
        private UIElement textPanel;
        private UIImage standingPortrait;
        private UIPanel backgroundPanel;
        private UIImage npcPortrait;
        private UIText npcNameText;
        private UIText dialogueText;
        private UIList optionList;
        private UIScrollbar optionScrollbar;
        private UIFavorabilityHeart heartIcon;

        // 逻辑控制
        private int currentNPCIndex = -1;
        private Dictionary<int, GalgameProfile> profiles;
        private GalgameProfile currentProfile;

        // 手动模式标记
        private bool isManualMode = false;
        public bool IsManualMode => isManualMode;

        // 布局常量
        private const float PANEL_WIDTH = 800f;
        private const float PANEL_HEIGHT = 240f;
        private const float PANEL_VALIGN = 0.85f;

        public override void OnInitialize()
        {
            if (profiles == null) profiles = new Dictionary<int, GalgameProfile>();

            // 注册原版 NPC
            if (!profiles.ContainsKey(NPCID.Nurse)) profiles[NPCID.Nurse] = new NurseProfile();
            if (!profiles.ContainsKey(NPCID.Guide)) profiles[NPCID.Guide] = new GuideProfile();
            if (!profiles.ContainsKey(NPCID.Dryad)) profiles[NPCID.Dryad] = new DryadProfile();
            if (!profiles.ContainsKey(NPCID.Angler)) profiles[NPCID.Angler] = new AnglerProfile();
            if (!profiles.ContainsKey(ArrodesItem.ARRODES_ID)) profiles[ArrodesItem.ARRODES_ID] = new ArrodesProfile();
            int dunnID = ModContent.NPCType<DunnSmith>();
            if (!profiles.ContainsKey(dunnID))
            {
                profiles[dunnID] = new DunnProfile();
            }
            int oldNeilID = ModContent.NPCType<OldNeil>(); // 获取老尼尔的ID
            if (!profiles.ContainsKey(oldNeilID))
            {
                profiles[oldNeilID] = new OldNeilProfile(); // 关联他的档案
            }

            if (currentProfile == null) currentProfile = new GalgameProfile();

            rootArea = new UIElement(); rootArea.Width.Set(0, 1f); rootArea.Height.Set(0, 1f); Append(rootArea);

            standingPortrait = new UIImage(TextureAssets.Npc[0]);
            standingPortrait.HAlign = 0f; standingPortrait.VAlign = 0f;
            standingPortrait.Color = Color.White;
            rootArea.Append(standingPortrait);

            textPanel = new UIElement(); textPanel.Width.Set(PANEL_WIDTH, 0f); textPanel.Height.Set(PANEL_HEIGHT, 0f);
            textPanel.HAlign = 0.5f; textPanel.VAlign = PANEL_VALIGN; textPanel.OverflowHidden = false;
            rootArea.Append(textPanel);

            backgroundPanel = new UIPanel(); backgroundPanel.Width.Set(0, 1f); backgroundPanel.Height.Set(0, 1f); textPanel.Append(backgroundPanel);

            npcPortrait = new UIImage(TextureAssets.NpcHead[0]); npcPortrait.Left.Set(20f, 0f); npcPortrait.Top.Set(20f, 0f); npcPortrait.Width.Set(80f, 0f); npcPortrait.Height.Set(80f, 0f); textPanel.Append(npcPortrait);

            npcNameText = new UIText("NPC", 0.8f, true); npcNameText.Left.Set(120f, 0f); npcNameText.Top.Set(20f, 0f); textPanel.Append(npcNameText);

            heartIcon = new UIFavorabilityHeart(); heartIcon.Left.Set(260f, 0f); heartIcon.Top.Set(18f, 0f); heartIcon.Width.Set(32f, 0f); heartIcon.Height.Set(32f, 0f); textPanel.Append(heartIcon);

            dialogueText = new UIText("...", 1.0f); dialogueText.Left.Set(120f, 0f); dialogueText.Top.Set(60f, 0f); dialogueText.Width.Set(-360f, 1f); dialogueText.Height.Set(-40f, 1f); dialogueText.IsWrapped = true; dialogueText.TextColor = Color.White; textPanel.Append(dialogueText);

            optionList = new UIList(); optionList.Width.Set(200f, 0f); optionList.Height.Set(-40f, 1f); optionList.Left.Set(-230f, 1f); optionList.Top.Set(20f, 0f); optionList.ListPadding = 5f; textPanel.Append(optionList);

            optionScrollbar = new UIScrollbar(); optionScrollbar.SetView(100f, 1000f); optionScrollbar.Height.Set(-40f, 1f); optionScrollbar.Left.Set(-20f, 1f); optionScrollbar.Top.Set(20f, 0f); optionList.SetScrollbar(optionScrollbar); textPanel.Append(optionScrollbar);
        }

        public void RegisterProfile(int npcType, GalgameProfile profile)
        {
            if (profiles == null) profiles = new Dictionary<int, GalgameProfile>();
            profiles[npcType] = profile;
        }

        public void SetCustomProfile(GalgameProfile profile)
        {
            isManualMode = true;
            currentProfile = profile;
            currentNPCIndex = -1;
            UpdateStaticInfo(null);
            ClearButtons();
            currentProfile.SetupButtons(null, this);
            SetDialogueText(currentProfile.GetDialogue(null));
            Recalculate();
            ForcePositionUpdate();
        }

        public void Show()
        {
            ModContent.GetInstance<GalgameUISystem>().OpenUI();
        }

        public void Close()
        {
            isManualMode = false;
            ModContent.GetInstance<GalgameUISystem>().CloseUI();
        }

        public override void OnDeactivate()
        {
            isManualMode = false;
            currentNPCIndex = -1;
            base.OnDeactivate();
        }

        public void AddButton(string text, Action onClick)
        {
            UITextPanel<string> button = new UITextPanel<string>(text); button.Width.Set(0, 1f); button.Height.Set(40f, 0f);
            Color baseColor = currentProfile.BackgroundColor * 0.9f; if (text == "【欢愉】") baseColor = new Color(100, 20, 60) * 0.9f;
            button.BackgroundColor = baseColor; button.OnMouseOver += (evt, e) => button.BackgroundColor = currentProfile.NameColor; button.OnMouseOut += (evt, e) => button.BackgroundColor = baseColor;
            button.OnLeftClick += (evt, e) => { SoundEngine.PlaySound(SoundID.MenuTick); onClick?.Invoke(); }; optionList.Add(button);
        }

        public void SetDialogueText(string text) => dialogueText.SetText(text);
        public void ClearButtons() => optionList.Clear();

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // ★★★ 核心修复1：当鼠标悬停在UI上时，阻止物品使用 ★★★
            if (ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;
            }

            if (Main.playerInventory)
            {
                if (GalgameUISystem.visible) Close();
                return;
            }

            if (isManualMode) return;

            int targetID = -1;
            if (GalgameUISystem.isTalkingToArrodes) targetID = ArrodesItem.ARRODES_ID;
            else if (Main.LocalPlayer.talkNPC != -1) targetID = Main.npc[Main.LocalPlayer.talkNPC].type;

            if (targetID == -1)
            {
                if (GalgameUISystem.visible) Close();
                currentNPCIndex = -1;
                return;
            }

            if (targetID != -1 && !GalgameUISystem.isTalkingToArrodes)
            {
                int score = FavorabilitySystem.GetFavorability(targetID);
                heartIcon.SetFavorability(score);
            }

            int checkIndex = GalgameUISystem.isTalkingToArrodes ? ArrodesItem.ARRODES_ID : Main.LocalPlayer.talkNPC;

            if (checkIndex != currentNPCIndex)
            {
                currentNPCIndex = checkIndex;
                if (profiles.ContainsKey(targetID)) currentProfile = profiles[targetID];
                else currentProfile = new GalgameProfile();

                NPC npcObj = GalgameUISystem.isTalkingToArrodes ? null : Main.npc[Main.LocalPlayer.talkNPC];
                UpdateStaticInfo(npcObj);
                optionList.Clear();
                currentProfile.SetupButtons(npcObj, this);

                if (!GalgameUISystem.isTalkingToArrodes) FavorabilitySystem.RecordInteraction(targetID);
                if (GalgameUISystem.isTalkingToArrodes) SoundEngine.PlaySound(new SoundStyle("zhashi/Assets/Sounds/ArrodesOpen"));

                string initialText = currentProfile.GetDialogue(npcObj);
                if (string.IsNullOrEmpty(initialText) && !GalgameUISystem.isTalkingToArrodes && npcObj != null) initialText = Main.npcChatText;
                dialogueText.SetText(initialText);

                Recalculate();
                ForcePositionUpdate();
            }
        }

        private void UpdateStaticInfo(NPC npc)
        {
            // 日记模式：隐藏名字、立绘、爱心，并拓宽文本框
            if (isManualMode && npc == null)
            {
                npcNameText.SetText("");
                heartIcon.Left.Set(-1000, 0f);

                // 拓宽文本框
                dialogueText.Left.Set(40f, 0f);
                dialogueText.Width.Set(-260f, 1f);
            }
            else
            {
                heartIcon.Left.Set(260f, 0f);

                // 恢复默认布局
                dialogueText.Left.Set(120f, 0f);
                dialogueText.Width.Set(-360f, 1f);

                if (GalgameUISystem.isTalkingToArrodes) npcNameText.SetText("阿罗德斯");
                else if (npc != null) npcNameText.SetText(npc.GivenName);
            }

            backgroundPanel.BackgroundColor = currentProfile.BackgroundColor;
            backgroundPanel.BorderColor = currentProfile.BorderColor;
            npcNameText.TextColor = currentProfile.NameColor;

            string headPath = currentProfile.HeadTexture;
            if (!string.IsNullOrEmpty(headPath) && ModContent.RequestIfExists<Texture2D>(headPath, out var headAsset))
            {
                npcPortrait.SetImage(headAsset);
                npcPortrait.Color = Color.White;
            }
            else if (npc != null)
            {
                int headIndex = NPC.TypeToDefaultHeadIndex(npc.type);
                if (headIndex >= 0) { npcPortrait.SetImage(TextureAssets.NpcHead[headIndex]); npcPortrait.Color = Color.White; }
            }
            else
            {
                npcPortrait.SetImage(TextureAssets.NpcHead[0]);
                npcPortrait.Color = Color.Transparent;
            }

            string standPath = currentProfile.StandingTexture;
            if (!string.IsNullOrEmpty(standPath) && ModContent.RequestIfExists<Texture2D>(standPath, out var standAsset, AssetRequestMode.ImmediateLoad))
            {
                standingPortrait.SetImage(standAsset);
                standingPortrait.Color = Color.White;
                standingPortrait.Width.Set(standAsset.Value.Width, 0f);
                standingPortrait.Height.Set(standAsset.Value.Height, 0f);
            }
            else
            {
                standingPortrait.Color = Color.Transparent;
            }

            textPanel.Recalculate();
        }

        private void ForcePositionUpdate()
        {
            if (standingPortrait != null && standingPortrait.Color.A > 0)
            {
                float imgHeight = standingPortrait.Height.Pixels;
                if (imgHeight <= 1f) return;
                var panelRect = textPanel.GetDimensions();
                if (panelRect.Y > 0)
                {
                    standingPortrait.HAlign = 0f;
                    standingPortrait.VAlign = 0f;
                    float targetLeft = panelRect.X + currentProfile.OffsetX;
                    float targetTop = panelRect.Y - imgHeight + currentProfile.OffsetY;
                    standingPortrait.Left.Set(targetLeft, 0f);
                    standingPortrait.Top.Set(targetTop, 0f);
                    standingPortrait.Recalculate();
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch) { ForcePositionUpdate(); base.Draw(spriteBatch); }
    }

    public class UIFavorabilityHeart : UIElement
    {
        private float fillPercent = 0.6f;
        public void SetFavorability(int score) { fillPercent = MathHelper.Clamp(score / 100f, 0f, 1f); }
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            Texture2D heartTex = TextureAssets.Heart.Value;
            int frameSize = heartTex.Width;
            CalculatedStyle dimensions = GetDimensions();
            Vector2 position = new Vector2(dimensions.X, dimensions.Y);
            Rectangle fullRect = new Rectangle(0, 0, frameSize, frameSize);
            spriteBatch.Draw(heartTex, position, fullRect, Color.Black * 0.5f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            int fillHeight = (int)(frameSize * fillPercent);
            int topOffset = frameSize - fillHeight;
            Rectangle sourceRect = new Rectangle(0, topOffset, frameSize, fillHeight);
            Vector2 drawPos = position + new Vector2(0, topOffset);
            Color heartColor = Color.Lerp(Color.PaleVioletRed, Color.Crimson, fillPercent);
            spriteBatch.Draw(heartTex, drawPos, sourceRect, heartColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            if (IsMouseHovering) { Main.instance.MouseText($"好感度: {(int)(fillPercent * 100)}/100" + (fillPercent >= 1f ? " ❤" : "")); }
        }
    }
}