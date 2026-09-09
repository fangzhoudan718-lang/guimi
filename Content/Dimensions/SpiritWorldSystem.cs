using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using SubworldLibrary;

namespace zhashi.Content.Dimensions
{
    public class SpiritWorldSystem : ModSystem
    {
        // 修改光照颜色：让世界看起来诡异（比如偏紫色或灰色）
        public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
        {
            if (SubworldSystem.IsActive<SpiritWorld>())
            {
                // 强行把背景光变成深紫色
                backgroundColor = new Color(50, 20, 80);
                // 地块的光照变成一种诡异的青色
                tileColor = new Color(100, 255, 200);
            }
        }

        // 修改屏幕效果：增加晃动或致幻感
        public override void PostUpdateEverything()
        {
            if (SubworldSystem.IsActive<SpiritWorld>())
            {
                // 只有在客户端执行视觉效果
                if (Main.netMode != Terraria.ID.NetmodeID.Server)
                {
                    // 屏幕轻微扭曲/晃动效果
                    // 随着时间产生正弦波晃动
                    Main.screenPosition.X += (float)System.Math.Sin(Main.GameUpdateCount / 20f) * 2f;
                    Main.screenPosition.Y += (float)System.Math.Cos(Main.GameUpdateCount / 15f) * 2f;

                    // 偶尔让屏幕色彩反转一下（模拟灵界的信息过载，可选）
                    if (Main.rand.NextBool(600)) // 每10秒大概一次
                    {
                        // 这里可以添加更复杂的屏幕滤镜，新手暂时先不做 Shader
                    }
                }
            }
        }
    }
}