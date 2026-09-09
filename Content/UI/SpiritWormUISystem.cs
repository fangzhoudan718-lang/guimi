using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace zhashi.Content.UI
{
    [Autoload(Side = ModSide.Client)] // 只在客户端运行
    public class SpiritWormUISystem : ModSystem
    {
        internal SpiritWormBar spiritWormBar;
        private UserInterface spiritWormInterface;
        public override void Load()
        {
            spiritWormBar = new SpiritWormBar();
            spiritWormBar.Activate();
            spiritWormInterface = new UserInterface();
            spiritWormInterface.SetState(spiritWormBar);
        }
        public override void UpdateUI(GameTime gameTime)
        {
            spiritWormInterface?.Update(gameTime);
        }
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int resourceBarIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Resource Bars"));

            if (resourceBarIndex != -1)
            {
                layers.Insert(resourceBarIndex + 1, new LegacyGameInterfaceLayer(
                    "zhashi: Spirit Worm Bar",
                    delegate {
                        spiritWormInterface.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }
    }
}