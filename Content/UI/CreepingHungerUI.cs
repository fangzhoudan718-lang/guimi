using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;
using Terraria.ModLoader;
using Terraria.ID;
using zhashi.Content.Items.Weapons;

namespace zhashi.Content.UI
{
    public class CreepingHungerWheelState : UIState
    {
        public static bool Visible = false;
        private int selectedIndex = -1;

        public CreepingHunger CurrentGlove = null;

        // 防止左键连点
        private bool wasClicking = false;

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (!Visible || CurrentGlove == null) return;

            Main.LocalPlayer.mouseInterface = true;

            List<int> souls = CurrentGlove.GetSouls();
            int count = souls.Count;
            if (count == 0) return;

            Vector2 center = new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f);
            Vector2 mousePos = new Vector2(Main.mouseX, Main.mouseY);
            Vector2 dist = mousePos - center;

            if (dist.Length() < 40f)
            {
                selectedIndex = -1;
            }
            else
            {
                // 计算角度选择
                float angle = (float)Math.Atan2(dist.Y, dist.X);
                angle += MathHelper.TwoPi;
                float slice = MathHelper.TwoPi / count;
                float offsetAngle = angle + (slice / 2f);
                selectedIndex = (int)((offsetAngle % MathHelper.TwoPi) / slice);
            }

            // === 核心逻辑：检测左键点击删除 ===
            if (Main.mouseLeft)
            {
                if (!wasClicking && selectedIndex != -1 && selectedIndex < souls.Count)
                {
                    // 获取当前悬停的 NPC ID
                    int soulToRemove = souls[selectedIndex];

                    // 0 (无) 不能删
                    if (soulToRemove != 0)
                    {
                        // 调用手套的删除方法
                        CurrentGlove.RemoveSoul(soulToRemove);
                    }
                }
                wasClicking = true;
            }
            else
            {
                wasClicking = false;
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (!Visible || CurrentGlove == null) return;

            List<int> souls = CurrentGlove.GetSouls();
            int count = souls.Count;
            if (count == 0) return;

            Vector2 center = new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f);
            float radius = 140f;
            float slice = MathHelper.TwoPi / count;

            for (int i = 0; i < count; i++)
            {
                int npcId = souls[i];
                bool isSelected = (i == selectedIndex);
                float angle = i * slice;
                Vector2 pos = center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;

                // 连线
                if (isSelected)
                    Terraria.Utils.DrawLine(spriteBatch, center, pos, Color.Red, Color.Transparent, 2f);

                // 绘制头像
                Main.instance.LoadNPC(npcId);
                Texture2D npcTexture = TextureAssets.Npc[npcId].Value;

                int frameHeight = npcTexture.Height / Main.npcFrameCount[npcId];
                Rectangle sourceRect = new Rectangle(0, 0, npcTexture.Width, frameHeight);
                Vector2 origin = sourceRect.Size() / 2f;
                float scale = isSelected ? 1.2f : 0.8f;

                if (sourceRect.Width > 64 || sourceRect.Height > 64) scale *= 0.6f;

                spriteBatch.Draw(npcTexture, pos, sourceRect, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);

                // 文字提示
                if (isSelected)
                {
                    string name = Lang.GetNPCNameValue(npcId);
                    if (npcId == 0) name = "无";

                    Vector2 textSize = FontAssets.MouseText.Value.MeasureString(name);
                    Terraria.Utils.DrawBorderString(spriteBatch, name, pos + new Vector2(0, 40) - textSize / 2f, Color.Gold);

                    // 【新增】提示可以删除
                    if (npcId != 0)
                    {
                        string tip = "[左键点击以释放]";
                        Vector2 tipSize = FontAssets.MouseText.Value.MeasureString(tip);
                        Terraria.Utils.DrawBorderString(spriteBatch, tip, pos + new Vector2(0, 65) - tipSize / 2f, Color.Gray, 0.8f);
                    }
                }
            }
        }

        public int GetSelectedSoulNPCID()
        {
            if (CurrentGlove != null && selectedIndex != -1)
            {
                var souls = CurrentGlove.GetSouls();
                // 增加边界检查，防止刚删完数组变小了导致的越界
                if (selectedIndex < souls.Count) return souls[selectedIndex];
            }
            return -1;
        }
    }

    public class CreepingHungerUISystem : ModSystem
    {
        private UserInterface _interface;
        private CreepingHungerWheelState _wheelState;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                _interface = new UserInterface();
                _wheelState = new CreepingHungerWheelState();
                _wheelState.Activate();
                _interface.SetState(_wheelState);
            }
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (CreepingHungerWheelState.Visible && _interface != null)
                _interface.Update(gameTime);
        }

        public void OpenWheel(CreepingHunger glove)
        {
            _wheelState.CurrentGlove = glove;
            CreepingHungerWheelState.Visible = true;
        }

        public void CloseWheel()
        {
            CreepingHungerWheelState.Visible = false;
            _wheelState.CurrentGlove = null;
        }

        public CreepingHungerWheelState GetState() => _wheelState;

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseIndex != -1)
            {
                layers.Insert(mouseIndex, new LegacyGameInterfaceLayer(
                    "zhashi: Creeping Hunger Wheel",
                    delegate {
                        if (CreepingHungerWheelState.Visible)
                            _interface.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }
    }
}