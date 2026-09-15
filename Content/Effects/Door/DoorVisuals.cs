using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace zhashi.Content.Effects.Door
{
    public enum DoorTheme { Illusory, KeyOfStars, Planeswalker, Wanderer, SecretsSorcerer }
    /// <summary>空间牢笼圆盘的三层：固定底盘，以及两层用来做视差的星野。</summary>
    public enum DoorDomainLayer { Void, Far, Near }

    // Normal AlphaBlend only. No batch restarts, additive layers or lighting mutations.
    // Animation uses cached source rectangles, as demonstrated by MIT ExampleMod.
    public class DoorVisuals : ModSystem
    {
        public static readonly Color Violet = new Color(126, 119, 151);
        public static readonly Color Cyan = new Color(121, 150, 161);
        public static readonly Color Gold = new Color(165, 151, 120);
        public static readonly Color Pale = new Color(178, 185, 195);
        public static readonly Color Ink = new Color(18, 21, 29);
        public static readonly Vector2 DoorRadii = new Vector2(25, 44);
        private static Asset<Texture2D> door, crack, tear, box, veil, shard, ring, mote, dimensionalSightEye;
        private static Asset<Texture2D> domainVoid, domainFar, domainNear;
        private static Asset<Texture2D> riftVoid, riftFar, riftNear;
        // 原本来自“门途径 · 克制特效”配置项；该设置已删除，这里固定为当时的默认观感。
        private const float Detail = 0.65f;
        private static Asset<Texture2D> Load(string name) => ModContent.Request<Texture2D>(
            "zhashi/Content/Effects/Door/Textures/" + name, AssetRequestMode.ImmediateLoad);
        private static void EnsureTextures()
        {
            if (Main.dedServ || door != null) return;
            door = Load("QuietDoorAtlas");
            crack = Load("SpaceCrackAtlas"); tear = Load("TearAtlas");
            box = Load("GlassBox"); veil = Load("SoftVeil");
            shard = Load("SpaceShard"); ring = Load("SoftRingThin"); mote = Load("DoorMote");
            dimensionalSightEye = Load("DimensionalSightEye");
            domainVoid = Load("SpatialDomainVoid");
            domainFar = Load("SpatialDomainFar");
            domainNear = Load("SpatialDomainNear");
            riftVoid = Load("SpatialRiftVoid");
            riftFar = Load("SpatialRiftFar");
            riftNear = Load("SpatialRiftNear");
        }
        public override void Unload() => door = crack = tear = box = veil = shard = ring = mote = dimensionalSightEye =
            domainVoid = domainFar = domainNear = riftVoid = riftFar = riftNear = null;
        public static Color Tint(Color color, float opacity) => color * MathHelper.Clamp(opacity, 0, 1);
        public static Color Alpha(Color color, float opacity) => Tint(color, opacity);
        public static bool Visible(Vector2 world, float margin = 250) =>
            world.X > Main.screenPosition.X - margin && world.Y > Main.screenPosition.Y - margin &&
            world.X < Main.screenPosition.X + Main.screenWidth + margin && world.Y < Main.screenPosition.Y + Main.screenHeight + margin;
        public static DoorTheme ThemeForSequence(int sequence) => sequence switch
        { <= 1 => DoorTheme.KeyOfStars, 2 => DoorTheme.Planeswalker, 3 => DoorTheme.Wanderer, _ => DoorTheme.SecretsSorcerer };
        public static Color ThemeColor(DoorTheme theme) => theme switch
        { DoorTheme.KeyOfStars => Gold, DoorTheme.Planeswalker => Cyan, DoorTheme.Illusory => Pale, _ => Violet };

        private static void Draw(Asset<Texture2D> texture, Rectangle? frame, Vector2 world, Vector2 size, Color color, float opacity, float rotation = 0)
        {
            if (Main.dedServ || texture == null || opacity <= 0.004f || !Visible(world, Math.Max(size.X, size.Y) / 2 + 40)) return;
            Vector2 sourceSize = frame?.Size() ?? texture.Value.Size();
            Main.spriteBatch.Draw(texture.Value, world - Main.screenPosition, frame, Tint(color, opacity), rotation,
                sourceSize / 2, size / sourceSize, SpriteEffects.None, 0);
        }

        public static void Portal(Vector2 center, Vector2 radii, Color color, float openness, float clock,
            DoorTheme theme = DoorTheme.SecretsSorcerer, float opacity = 1)
        {
            if (Main.dedServ || !Visible(center, radii.Y + 50)) return;
            EnsureTextures();
            openness = MathHelper.Clamp(openness, 0, 1);
            int frame = Math.Clamp((int)MathF.Round(openness * 5), 0, 5);
            var source = new Rectangle(frame % 3 * 512, frame / 3 * 512, 512, 512);
            // Art fills 224 x 400 of each preserved 512px cell. Normalize only the runtime draw.
            Vector2 size = new Vector2(radii.X * 2 * 512 / 224, radii.Y * 2 * 512 / 400);
            Color environment = Lighting.GetColor((int)center.X / 16, (int)center.Y / 16);
            Color lit = Color.Lerp(environment, Color.White, 0.42f);
            Color material = Color.Lerp(lit, color, 0.10f);
            float fade = Math.Min(1, openness * 8) * opacity;
            Draw(door, source, center, size, material, fade * (0.64f + Detail * 0.30f));
        }
        // CC0 Kenney-derived masks remain local details, never full-screen glow surfaces.
        public static void Haze(Vector2 world, Vector2 size, Color color, float opacity, float rotation = 0)
        { EnsureTextures(); Draw(veil, null, world, size, color, Math.Min(opacity, 0.18f) * Detail, rotation); }
        public static void Mote(Vector2 world, float size, Color color, float opacity, float rotation = 0)
        { EnsureTextures(); Draw(mote, null, world, new Vector2(Math.Min(size, 12)), color, opacity * Detail, rotation); }
        public static void Shard(Vector2 world, float size, Color color, float opacity, float rotation)
        { EnsureTextures(); Draw(shard, null, world, new Vector2(size), color, opacity * Detail, rotation); }
        public static void Box(Vector2 world, float radius, Color color, float rotation, float opacity)
        { EnsureTextures(); Draw(box, null, world, new Vector2(radius * 2.05f), color, opacity * Detail, rotation); }
        public static void Ring(Vector2 world, float radius, Color color, float opacity, float rotation = 0, float thickness = 1, float squish = 1)
        { EnsureTextures(); Draw(ring, null, world, new Vector2(radius * 2, radius * 2 * squish), color, opacity * Detail, rotation); }
        public static void Crack(Vector2 center, float radius, float progress, Color color, float opacity, float rotation = 0)
        {
            EnsureTextures();
            int frame = Math.Clamp((int)(progress * 4), 0, 3);
            Draw(crack, new Rectangle(frame * 256, 0, 256, 256), center, new Vector2(radius * 2), color, opacity * Detail, rotation);
        }
        public static void Tear(Vector2 center, float length, float thickness, float rotation, Color color, float opacity, float progress)
        {
            EnsureTextures();
            int frame = Math.Clamp((int)(progress * 4), 0, 3);
            // Source is vertical. Align with the supplied world-space collision line.
            Draw(tear, new Rectangle(frame * 256, 0, 256, 256), center, new Vector2(thickness, length), color,
                opacity * (0.55f + Detail * 0.45f), rotation - MathHelper.PiOver2);
        }

        /// <summary>
        /// 空间牢笼的一层圆盘背景。贴图自带圆形遮罩，所以只要按直径缩放；
        /// 三层各自不同的位置偏移就是“从外面看进另一个世界”的视差。
        /// </summary>
        public static void Domain(DoorDomainLayer layer, Vector2 world, float radius, float opacity)
        {
            if (Main.dedServ || radius <= 4f || opacity <= 0.004f) return;
            EnsureTextures();
            Asset<Texture2D> texture = layer switch
            {
                DoorDomainLayer.Void => domainVoid,
                DoorDomainLayer.Far => domainFar,
                _ => domainNear
            };
            if (texture == null || !texture.IsLoaded) return;
            if (!Visible(world, radius + 60f)) return;
            float scale = radius * 2f / texture.Value.Width;
            Main.spriteBatch.Draw(texture.Value, world - Main.screenPosition, null, Tint(Color.White, opacity),
                0f, texture.Value.Size() / 2f, scale, SpriteEffects.None, 0f);
        }

        /// <summary>
        /// 撕裂空间的一层裂痕。贴图是竖着的，所以按长度等比缩放，
        /// 再旋转到与世界坐标里的裂痕轴线对齐；三层各自偏移就是 3D 视差。
        /// </summary>
        public static void Rift(DoorDomainLayer layer, Vector2 world, float length, float axisRotation, float opacity)
        {
            if (Main.dedServ || length <= 4f || opacity <= 0.004f) return;
            EnsureTextures();
            Asset<Texture2D> texture = layer switch
            {
                DoorDomainLayer.Void => riftVoid,
                DoorDomainLayer.Far => riftFar,
                _ => riftNear
            };
            if (texture == null || !texture.IsLoaded) return;
            if (!Visible(world, length * 0.5f + 60f)) return;
            float scale = length / texture.Value.Height;
            // 贴图长轴朝下，转到世界里的裂痕轴线：局部 +Y 要映射到 UnitX.RotatedBy(axisRotation)。
            Main.spriteBatch.Draw(texture.Value, world - Main.screenPosition, null, Tint(Color.White, opacity),
                axisRotation - MathHelper.PiOver2, texture.Value.Size() / 2f, scale, SpriteEffects.None, 0f);
        }

        /// <summary>维度之视：竖瞳先像空间裂口一样睁开，随后以极轻的呼吸和视差维持立体感。</summary>
        public static void DimensionalEye(Vector2 world, float age, int timeLeft, float opacity)
        {
            if (Main.dedServ || opacity <= 0.004f || !Visible(world, 150f)) return;
            EnsureTextures();
            if (dimensionalSightEye == null || !dimensionalSightEye.IsLoaded) return;

            float open = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(age / 16f, 0f, 1f));
            float close = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(timeLeft / 18f, 0f, 1f));
            float breathe = 1f + MathF.Sin(age * 0.055f) * 0.018f;
            float gazeTilt = MathHelper.Clamp((Main.LocalPlayer.Center.X - world.X) / 1600f, -0.025f, 0.025f);
            // 贴图主体约占画布 80%，所以画布绘制 248px 时，可见眼睛约为 200px。
            Vector2 size = new Vector2(248f * breathe, 248f * breathe * Math.Max(0.045f, open * close));

            Draw(dimensionalSightEye, null, world + new Vector2(0f, 3f), size * 1.035f,
                Ink, opacity * 0.42f, gazeTilt);
            Draw(dimensionalSightEye, null, world, size, Color.White, opacity, gazeTilt);
        }

    }
}
