using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace zhashi.Content
{
    public class GlacierVisualSystem : ModSystem
    {
        public static float flashAlpha = 0f; // 控制滤镜透明度

        // 调用此方法触发闪光
        public static void StartFlash()
        {
            flashAlpha = 0.8f; // 初始不透明度 (0.0 ~ 1.0)
        }

        public override void UpdateUI(GameTime gameTime)
        {
            // 每一帧让透明度降低 (淡出)
            if (flashAlpha > 0f)
            {
                flashAlpha -= 0.02f; // 约 50 帧 (不到1秒) 消失
                if (flashAlpha < 0f) flashAlpha = 0f;
            }
        }

        // 在界面层绘制全屏蓝色
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (flashAlpha > 0f)
            {
                int mouseIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
                if (mouseIndex != -1)
                {
                    layers.Insert(mouseIndex, new LegacyGameInterfaceLayer(
                        "zhashi: Glacier Filter",
                        delegate
                        {
                            // 绘制全屏冰蓝色矩形
                            Main.spriteBatch.Draw(
                                Terraria.GameContent.TextureAssets.MagicPixel.Value,
                                new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                                new Color(0, 200, 255) * flashAlpha // 冰蓝色
                            );
                            return true;
                        },
                        InterfaceScaleType.UI)
                    );
                }
            }
        }
    }
}