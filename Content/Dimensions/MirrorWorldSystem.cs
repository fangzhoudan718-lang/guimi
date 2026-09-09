using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria.Graphics.Effects;

namespace zhashi.Content.Dimensions
{
    public class MirrorWorldSystem : ModSystem
    {
        // ★★★ 新增：环境锁定逻辑 ★★★
        public override void PreUpdateWorld()
        {
            // 只有在镜中世界时生效
            if (SubworldSystem.IsActive<MirrorWorld>())
            {
                // 1. 锁定时间：永远是午夜 (18000 = 半夜12点)
                Main.dayTime = false;
                Main.time = 18000.0;

                // 2. 锁定天气：无雨、无风、无云
                Main.raining = false;
                Main.rainTime = 0;
                Main.maxRaining = 0f;

                Main.windSpeedCurrent = 0f;
                Main.windSpeedTarget = 0f;

                Main.cloudAlpha = 0f; // 云完全透明
                Main.numClouds = 0;   // 云的数量为0

                // 3. 禁用背景 (可选，配合 ModifySunLightColor 使用)
                // Main.bgStyle = 0; // 强制设为虚空背景样式
            }
        }

        // 光照颜色 (保持之前的冷色调设定)
        public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
        {
            if (SubworldSystem.IsActive<MirrorWorld>())
            {
                // 背景：纯黑深邃
                backgroundColor = new Color(0, 0, 10);
                // 物体光：冷白色
                tileColor = new Color(200, 220, 255);
            }
        }

        // 视觉特效 (包含之前的修复)
        public override void PostUpdateEverything()
        {
            if (Main.netMode == Terraria.ID.NetmodeID.Server || Main.gameMenu) return;

            try
            {
                if (SubworldSystem.IsActive<MirrorWorld>())
                {
                    if (Filters.Scene["TowerVortex"] != null && !Filters.Scene["TowerVortex"].IsActive())
                    {
                        Filters.Scene.Activate("TowerVortex", Vector2.Zero).GetShader()?.UseIntensity(0.5f);
                    }
                    // 浮动感
                    Main.screenPosition.Y += (float)System.Math.Sin(Main.GameUpdateCount / 30f) * 2f;
                }
                else
                {
                    if (Filters.Scene["TowerVortex"] != null && Filters.Scene["TowerVortex"].IsActive())
                    {
                        Filters.Scene["TowerVortex"].Deactivate();
                    }
                }
            }
            catch { }
        }
    }
}