using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace zhashi.Content.Utils
{
    public class EasyTrail
    {
        // 添加了一个新参数：blendState，允许你指定混合模式
        public static void Draw(List<Vector2> points, Vector2 offset, Func<float, float> widthFunc, Func<float, Color> colorFunc, Texture2D texture, BlendState blendState = null)
        {
            if (points.Count < 2) return;

            // 如果没传，默认用 Additive (兼容你之前的光效)
            if (blendState == null) blendState = BlendState.Additive;

            List<VertexPositionColorTexture> vertices = new List<VertexPositionColorTexture>();
            for (int i = 0; i < points.Count - 1; i++)
            {
                float progress = (float)i / points.Count;
                float nextProgress = (float)(i + 1) / points.Count;

                Vector2 current = points[i] + offset;
                Vector2 next = points[i + 1] + offset;

                Vector2 dir = next - current;
                float rot = dir.ToRotation();

                // 宽度计算
                float width = widthFunc(progress);
                float nextWidth = widthFunc(nextProgress);

                Color color = colorFunc(progress);
                Color nextColor = colorFunc(nextProgress);

                Vector2 normal = new Vector2(0, -1).RotatedBy(rot);

                // 构建三角形顶点
                vertices.Add(new VertexPositionColorTexture(new Vector3(current - normal * width, 0), color, new Vector2(progress, 0)));
                vertices.Add(new VertexPositionColorTexture(new Vector3(current + normal * width, 0), color, new Vector2(progress, 1)));
                vertices.Add(new VertexPositionColorTexture(new Vector3(next - normal * nextWidth, 0), nextColor, new Vector2(nextProgress, 0)));

                vertices.Add(new VertexPositionColorTexture(new Vector3(next - normal * nextWidth, 0), nextColor, new Vector2(nextProgress, 0)));
                vertices.Add(new VertexPositionColorTexture(new Vector3(current + normal * width, 0), color, new Vector2(progress, 1)));
                vertices.Add(new VertexPositionColorTexture(new Vector3(next + normal * nextWidth, 0), nextColor, new Vector2(nextProgress, 1)));
            }

            // === 渲染核心修复 ===
            Main.spriteBatch.End();

            // 使用传入的 blendState，而不是强制 Additive
            // 使用 NonPremultiplied 通常比 AlphaBlend 更适合处理半透明纹理边缘的黑边问题
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, blendState, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            var device = Main.graphics.GraphicsDevice;
            if (vertices.Count >= 3)
            {
                device.Textures[0] = texture;
                device.SamplerStates[0] = SamplerState.LinearWrap; // 确保纹理平滑重复

                // 绘制图元
                device.DrawUserPrimitives(PrimitiveType.TriangleList, vertices.ToArray(), 0, vertices.Count / 3);
            }

            Main.spriteBatch.End();
            // 恢复原版默认绘制状态
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}