using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace zhashi.Content.UI
{
    public class DogUISystem : ModSystem
    {
        internal DogStatsUI dogStatsUI;
        private UserInterface dogInterface;

        public override void Load()
        {
            // 【关键修改】
            //如果是专用服务器（即没有画面的服务端），直接返回，不加载任何 UI。
            // 这样能防止服务器因为加载 UI 报错 (NullReferenceException) 而崩溃。
            if (Main.dedServ)
            {
                return;
            }

            // 游戏加载时初始化 UI (现在只有客户端会运行这部分代码)
            dogStatsUI = new DogStatsUI();
            dogStatsUI.Activate();
            dogInterface = new UserInterface();
            dogInterface.SetState(dogStatsUI);
        }

        public override void UpdateUI(GameTime gameTime)
        {
            // 这里的 ?. 也是必要的，确保 dogInterface 不为空才更新
            if (DogStatsUI.Visible)
            {
                dogInterface?.Update(gameTime);
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseTextIndex != -1)
            {
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "zhashi: DogStatsUI",
                    delegate
                    {
                        if (DogStatsUI.Visible)
                        {
                            // 只有 dogInterface 被成功初始化（即在客户端）时才绘制
                            dogInterface?.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }
    }
}