using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using zhashi.Content.UI;

namespace zhashi.Content.UI
{
    [Autoload(Side = ModSide.Client)]
    public class SequenceUISystem : ModSystem
    {
        private UserInterface _interface;
        // 【修复】这里之前写的是 SequenceStatusUI，现在改为正确的 SequenceInfoUI
        private SequenceInfoUI _uiState;

        public override void Load()
        {
            _interface = new UserInterface();
            _uiState = new SequenceInfoUI(); // 【修复】同上
            _uiState.Activate();
            _interface.SetState(_uiState);
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (_interface != null)
                _interface.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int resourceBarIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Resource Bars"));
            if (resourceBarIndex != -1)
            {
                layers.Insert(resourceBarIndex, new LegacyGameInterfaceLayer(
                    "zhashi: Sequence Info",
                    delegate {
                        _interface.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }
    }
}