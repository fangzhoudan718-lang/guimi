using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using zhashi.Content.Projectiles.Door;

namespace zhashi.Content.Effects.Door
{
    /// <summary>
    /// 空间破碎的屏幕空间引力透镜。
    /// 只在画面中存在空间破碎弹幕时启用：先缓存原画面，再用灰度引力场扭曲背景，
    /// 最后覆盖不受扭曲影响的事件视界。专用 RenderTarget 会随分辨率一起重建。
    /// </summary>
    public sealed class SpaceShatterDistortionSystem : ModSystem
    {
        private const string TextureRoot = "zhashi/Content/Effects/Door/Textures/";
        private const string ShaderPath = "zhashi/Content/Effects/Door/Shaders/SpaceShatterDistortion";

        private static Asset<Texture2D> distortionField;
        private static Asset<Texture2D> eventHorizon;
        private static Asset<Effect> distortionShader;
        private static RenderTarget2D distortionTarget;
        private readonly List<SpaceShatterProjectile> visibleShatters = new();

        public override void Load()
        {
            if (Main.dedServ) return;

            distortionField = ModContent.Request<Texture2D>(TextureRoot + "SpaceShatterDistortion", AssetRequestMode.ImmediateLoad);
            eventHorizon = ModContent.Request<Texture2D>(TextureRoot + "SpaceShatterHorizon", AssetRequestMode.ImmediateLoad);
            distortionShader = ModContent.Request<Effect>(ShaderPath, AssetRequestMode.ImmediateLoad);

            On_FilterManager.EndCapture += DrawDistortedScene;
            On_Main.InitTargets_int_int += RecreateTargetWithScreen;
            On_Main.EnsureRenderTargetContent += EnsureTargetWithScreen;
        }

        public override void Unload()
        {
            if (!Main.dedServ)
            {
                On_FilterManager.EndCapture -= DrawDistortedScene;
                On_Main.InitTargets_int_int -= RecreateTargetWithScreen;
                On_Main.EnsureRenderTargetContent -= EnsureTargetWithScreen;
            }

            QueueTargetDisposal();
            distortionField = null;
            eventHorizon = null;
            distortionShader = null;
            visibleShatters.Clear();
        }

        private static void RecreateTargetWithScreen(On_Main.orig_InitTargets_int_int orig, Main self, int width, int height)
        {
            DisposeTarget();
            orig(self, width, height);
            EnsureTarget();
        }

        private static void EnsureTargetWithScreen(On_Main.orig_EnsureRenderTargetContent orig, Main self)
        {
            orig(self);
            EnsureTarget();
        }

        private static void EnsureTarget()
        {
            if (Main.dedServ || Main.graphics?.GraphicsDevice == null || Main.screenWidth <= 0 || Main.screenHeight <= 0)
                return;

            if (distortionTarget != null && !distortionTarget.IsDisposed &&
                distortionTarget.Width == Main.screenWidth && distortionTarget.Height == Main.screenHeight)
                return;

            DisposeTarget();
            GraphicsDevice device = Main.graphics.GraphicsDevice;
            distortionTarget = new RenderTarget2D(device, Main.screenWidth, Main.screenHeight, false,
                device.PresentationParameters.BackBufferFormat, DepthFormat.None);
        }

        private static void DisposeTarget()
        {
            if (distortionTarget != null && !distortionTarget.IsDisposed)
                distortionTarget.Dispose();
            distortionTarget = null;
        }

        /// <summary>
        /// Mod 卸载由 tML 的后台工作线程执行，FNA 图形资源不能在那里 Dispose。
        /// 先断开静态引用，再把真正的释放排进主线程；即使下一次更新尚未来得及执行，卸载也不会被中断。
        /// </summary>
        private static void QueueTargetDisposal()
        {
            RenderTarget2D target = distortionTarget;
            distortionTarget = null;
            if (target == null || target.IsDisposed) return;
            Main.QueueMainThreadAction(() =>
            {
                if (!target.IsDisposed) target.Dispose();
            });
        }

        private void CollectVisibleShatters()
        {
            visibleShatters.Clear();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.ModProjectile is not SpaceShatterProjectile shatter) continue;
                shatter.GetVisualState(out float coreRadius, out _, out float opacity);
                if (opacity > 0.004f && DoorVisuals.Visible(projectile.Center, coreRadius * 4f + 180f))
                    visibleShatters.Add(shatter);
            }
        }

        private void DrawDistortedScene(On_FilterManager.orig_EndCapture orig, FilterManager self,
            RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
        {
            CollectVisibleShatters();
            EnsureTarget();
            if (visibleShatters.Count == 0 || distortionTarget == null || distortionTarget.IsDisposed ||
                distortionField?.Value == null || eventHorizon?.Value == null || distortionShader?.Value == null)
            {
                orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
                return;
            }

            GraphicsDevice device = Main.graphics.GraphicsDevice;
            SpriteBatch spriteBatch = Main.spriteBatch;

            // 保存未经扭曲的世界画面，并先压暗引力井、画出细窄光子环。
            device.SetRenderTarget(Main.screenTargetSwap);
            device.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            spriteBatch.End();

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            foreach (SpaceShatterProjectile shatter in visibleShatters)
                DrawWellAndPhotonRing(spriteBatch, shatter);
            spriteBatch.End();

            // 将各黑洞的灰度引力场合并到一张全屏遮罩，只执行一次全屏 shader pass。
            device.SetRenderTarget(distortionTarget);
            device.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            float strongestDistortion = 0f;
            foreach (SpaceShatterProjectile shatter in visibleShatters)
            {
                shatter.GetVisualState(out float coreRadius, out _, out float opacity);
                float scale = coreRadius * 3.35f / distortionField.Value.Width;
                spriteBatch.Draw(distortionField.Value, shatter.Projectile.Center - Main.screenPosition, null,
                    Color.White * (0.48f * opacity), 0f, distortionField.Value.Size() * 0.5f,
                    scale, SpriteEffects.None, 0f);
                strongestDistortion = Math.Max(strongestDistortion, shatter.VisualDistortionStrength * opacity);
            }
            spriteBatch.End();

            // 对缓存画面作局部折射。强度随成形/坍缩变化，远低于参考模组的常驻最大值，避免眩光与晕动。
            device.SetRenderTarget(Main.screenTarget);
            device.Clear(Color.Transparent);
            Effect effect = distortionShader.Value;
            device.Textures[0] = Main.screenTargetSwap;
            device.Textures[1] = distortionTarget;
            device.Textures[2] = distortionTarget;
            effect.Parameters["R"].SetValue(5f + Main.GlobalTimeWrappedHourly * 0.035f);
            effect.Parameters["OtherStrength"].SetValue(MathHelper.Lerp(0.7f, 1.55f,
                MathHelper.Clamp(strongestDistortion, 0f, 1f)));
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, Main.Rasterizer, effect);
            spriteBatch.Draw(Main.screenTargetSwap, Vector2.Zero, Color.White);
            spriteBatch.End();

            // 事件视界最后覆盖，保持纯黑、边缘锐利，不让自身被透镜 shader 拉花。
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            foreach (SpaceShatterProjectile shatter in visibleShatters)
                DrawEventHorizon(spriteBatch, shatter);
            spriteBatch.End();

            device.Textures[1] = null;
            device.Textures[2] = null;
            orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
        }

        private static void DrawWellAndPhotonRing(SpriteBatch spriteBatch, SpaceShatterProjectile shatter)
        {
            shatter.GetVisualState(out float coreRadius, out float tilt, out float opacity);
            Vector2 position = shatter.Projectile.Center - Main.screenPosition;

            float fieldScale = coreRadius * 3.55f / distortionField.Value.Width;
            spriteBatch.Draw(distortionField.Value, position, null, Color.Black * (0.46f * opacity),
                0f, distortionField.Value.Size() * 0.5f, fieldScale, SpriteEffects.None, 0f);

            // 保留参考效果的“观察角变化”，但加入技能自身的缓慢进动，避免光子环像一张静态贴纸。
            float verticalOffset = MathHelper.Clamp((Main.LocalPlayer.Center.Y - shatter.Projectile.Center.Y) * 0.004f,
                -MathHelper.PiOver2, MathHelper.PiOver2);
            float perspective = MathHelper.Lerp(0.40f, 0.055f, MathF.Cos(verticalOffset));
            Vector2 ringScale = new Vector2(coreRadius * 5.8f / eventHorizon.Value.Width,
                coreRadius * 5.8f / eventHorizon.Value.Height * perspective);
            Color restrainedWhite = new Color(255, 255, 255, 0) * (0.44f * opacity);
            spriteBatch.Draw(eventHorizon.Value, position, null, restrainedWhite, tilt,
                eventHorizon.Value.Size() * 0.5f, ringScale, SpriteEffects.None, 0f);
        }

        private static void DrawEventHorizon(SpriteBatch spriteBatch, SpaceShatterProjectile shatter)
        {
            shatter.GetVisualState(out float coreRadius, out _, out float opacity);
            float scale = coreRadius * 2.62f / distortionField.Value.Width;
            spriteBatch.Draw(distortionField.Value, shatter.Projectile.Center - Main.screenPosition, null,
                Color.Black * (0.98f * opacity), 0f, distortionField.Value.Size() * 0.5f,
                scale, SpriteEffects.None, 0f);
        }
    }
}
