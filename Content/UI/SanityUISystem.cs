using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using zhashi.Content.DaqianLu; // 引用 DaqianLuPlayer

namespace zhashi.Content.UI
{
    [Autoload(true, Side = ModSide.Client)]
    public class SanityUISystem : ModSystem
    {
        internal SanityUI sanityUI;
        private UserInterface sanityInterface;

        public override void Load()
        {
            sanityUI = new SanityUI();
            sanityUI.Activate();
            sanityInterface = new UserInterface();
            sanityInterface.SetState(sanityUI);
        }

        public override void UpdateUI(GameTime gameTime)
        {
            // 获取当前玩家的大千录状态
            // 如果玩家不在游戏中，直接返回
            if (Main.LocalPlayer == null || !Main.LocalPlayer.active) return;

            var dqPlayer = Main.LocalPlayer.GetModPlayer<DaqianLuPlayer>();

            // 逻辑判断：只有持有大千录 OR 戴着面罩时，才更新UI
            if (dqPlayer.hasDaqianLu || dqPlayer.isWearingMask)
            {
                sanityInterface?.Update(gameTime);
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseTextIndex != -1)
            {
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "zhashi: SanityBar",
                    delegate
                    {
                        // 安全检查
                        if (Main.gameMenu || sanityUI == null || !Main.LocalPlayer.active) return true;

                        // 获取玩家实例
                        var dqPlayer = Main.LocalPlayer.GetModPlayer<DaqianLuPlayer>();

                        // --- 核心显示控制 ---
                        // 只有当 (持有大千录) 或 (戴着面罩) 时，才绘制 UI
                        if (dqPlayer.hasDaqianLu || dqPlayer.isWearingMask)
                        {
                            sanityInterface.Draw(Main.spriteBatch, new GameTime());
                        }

                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }
    }
}