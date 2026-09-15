using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace zhashi.Content.Effects.BlackEmperor
{
    /// <summary>混乱场圆盘的三层：固定切口，以及两层用来做视差的内部世界。</summary>
    public enum ChaosDomainLayer { Void, Far, Near }

    /// <summary>
    /// 混乱场的「另一个世界」。做法与门途径的空间牢笼同源：
    /// 固定的一层负责那条清晰的切口，两层内景遮罩得比切口更小，
    /// 于是视差再怎么偏移也顶不出边界——切口不动，里面的东西在滑；
    /// 站在圆心的施法者看到的是内景在缓缓自转（见 ChaosDomainProjectile）。
    ///
    /// 贴图由 SourceAssets/BlackEmperor/Build-ChaosDomain.ps1 生成，
    /// 与其他手绘带一样保存为预乘 alpha。
    /// </summary>
    public class BlackEmperorVisuals : ModSystem
    {
        private static Asset<Texture2D> voidLayer, farLayer, nearLayer, ring;

        private static Asset<Texture2D> Load(string name) => ModContent.Request<Texture2D>(
            "zhashi/Content/Effects/BlackEmperor/Textures/" + name, AssetRequestMode.ImmediateLoad);

        private static void EnsureTextures()
        {
            if (Main.dedServ || voidLayer != null) return;
            voidLayer = Load("ChaosDomainVoid");
            farLayer = Load("ChaosDomainFar");
            nearLayer = Load("ChaosDomainNear");
            ring = Load("ChaosRing");
        }

        public override void Unload() => voidLayer = farLayer = nearLayer = ring = null;

        private static Color Tint(Color color, float opacity) => color * MathHelper.Clamp(opacity, 0, 1);

        public static bool IsOnScreen(Vector2 world, float margin) =>
            world.X > Main.screenPosition.X - margin && world.Y > Main.screenPosition.Y - margin &&
            world.X < Main.screenPosition.X + Main.screenWidth + margin &&
            world.Y < Main.screenPosition.Y + Main.screenHeight + margin;

        /// <summary>圆盘的一层内景；贴图自带圆形遮罩，所以只要按直径缩放。</summary>
        public static void Domain(ChaosDomainLayer layer, Vector2 world, float radius, float opacity, float rotation)
        {
            if (Main.dedServ || radius <= 4f || opacity <= 0.004f) return;
            EnsureTextures();
            Asset<Texture2D> texture = layer switch
            {
                ChaosDomainLayer.Void => voidLayer,
                ChaosDomainLayer.Far => farLayer,
                _ => nearLayer
            };
            if (texture == null || !texture.IsLoaded) return;
            if (!IsOnScreen(world, radius + 60f)) return;
            float scale = radius * 2f / texture.Value.Width;
            Main.spriteBatch.Draw(texture.Value, world - Main.screenPosition, null, Tint(Color.White, opacity),
                rotation, texture.Value.Size() / 2f, scale, SpriteEffects.None, 0f);
        }

        /// <summary>边界环：告诉玩家这块地的边界在哪里，亮度压得很低。</summary>
        public static void Boundary(Vector2 world, float radius, float opacity, float rotation)
        {
            if (Main.dedServ || radius <= 4f || opacity <= 0.004f) return;
            EnsureTextures();
            if (ring == null || !ring.IsLoaded) return;
            if (!IsOnScreen(world, radius + 60f)) return;
            float scale = radius * 2f / ring.Value.Width;
            Main.spriteBatch.Draw(ring.Value, world - Main.screenPosition, null, Tint(new Color(216, 176, 240), opacity),
                rotation, ring.Value.Size() / 2f, scale, SpriteEffects.None, 0f);
        }
    }
}
