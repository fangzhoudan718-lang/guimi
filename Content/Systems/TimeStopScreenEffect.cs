using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.UI;
using Terraria.ModLoader;

namespace zhashi.Content.Systems
{
    public class TimeStopScreenEffect : ModSystem
    {
        public static bool IsActive = false;

        // 激活方法：你原来的代码可以直接调用这个
        public static void Activate(Vector2 center)
        {
            IsActive = true;
        }

        // 关闭方法：记得在时停结束时调用
        public static void Deactivate()
        {
            IsActive = false;
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (!IsActive) return;

            // 我们把特效插在鼠标之下，其他UI之上（或者你可以根据需要插在最底层）
            int layerIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (layerIndex != -1)
            {
                layers.Insert(layerIndex, new LegacyGameInterfaceLayer(
                    "zhashi: ZaWarudoEffect",
                    delegate
                    {
                        DrawNegativeEffect();
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }

        private void DrawNegativeEffect()
        {
            // 1. 结束当前的绘制批次
            Main.spriteBatch.End();

            // 2. 定义“反色”混合模式
            // 原理：最终颜色 = 源颜色(白) - 目标颜色(屏幕原色)
            // 1.0 - Color = 反色
            BlendState invertBlend = new BlendState
            {
                ColorSourceBlend = Blend.One,
                ColorDestinationBlend = Blend.One,
                ColorBlendFunction = BlendFunction.Subtract, // 关键：减法混合
                AlphaSourceBlend = Blend.One,
                AlphaDestinationBlend = Blend.One,
                AlphaBlendFunction = BlendFunction.Subtract
            };

            // 3. 使用反色模式绘制一个全屏白块
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, invertBlend, null, null, null, null, Main.UIScaleMatrix);

            // 绘制白色矩形 -> 屏幕变成反色（负片）
            Main.spriteBatch.Draw(Terraria.GameContent.TextureAssets.MagicPixel.Value,
                new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White);

            Main.spriteBatch.End();

            // 4. (可选) 叠加一层半透明蓝色，增加时停的神秘感
            // 使用常规混合模式
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, Main.UIScaleMatrix);

            // 蓝色覆盖层 (R=0, G=10, B=150, A=80) -> 半透明深蓝
            Main.spriteBatch.Draw(Terraria.GameContent.TextureAssets.MagicPixel.Value,
                new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), new Color(0, 10, 150, 80));

            Main.spriteBatch.End();

            // 5. 恢复正常的 UI 绘制设置，以免影响后续的 UI（比如鼠标）
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
        }
    }
}