using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace zhashi.Content.UI
{
    public class RoselleDiaryUISystem : ModSystem
    {
        internal RoselleDiaryUI diaryUI;
        internal UserInterface diaryInterface;
        public static bool visible = false;

        public override void Load()
        {
            if (!Main.dedServ) // 只在客户端加载 UI
            {
                diaryUI = new RoselleDiaryUI();
                diaryUI.Activate();
                diaryInterface = new UserInterface();
            }
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (visible)
            {
                diaryInterface?.Update(gameTime);
            }
        }

        // 将 UI 插入到游戏绘制层中
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseTextIndex != -1)
            {
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "zhashi: Roselle Diary",
                    delegate
                    {
                        if (visible)
                        {
                            diaryInterface.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }

        // 切换开关
        public void ToggleUI()
        {
            visible = !visible;
            if (visible)
                diaryInterface.SetState(diaryUI);
            else
                diaryInterface.SetState(null);
        }

        // 强制关闭
        public void CloseUI()
        {
            visible = false;
            diaryInterface.SetState(null);
        }
    }
}