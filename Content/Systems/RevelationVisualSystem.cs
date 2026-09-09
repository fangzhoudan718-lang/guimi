using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Systems
{
    /// <summary>
    /// 命运启示视觉系统 - 启示激活时屏幕笼罩漩涡滤镜
    /// 修复:
    ///   - 在 PostUpdateEverything 每帧重新确认激活,即使被某些系统关闭也能恢复
    ///   - 屏幕浮动已移至 LotMPlayer.ModifyScreenPosition (正确的钩子)
    /// </summary>
    public class RevelationVisualSystem : ModSystem
    {
        // 用于追踪上次状态, 避免无谓重复调用
        private static bool wasActive = false;

        public override void PostUpdateEverything()
        {
            if (Main.netMode == NetmodeID.Server) return;
            if (Main.dedServ) return;

            try
            {
                Player player = Main.LocalPlayer;
                if (player == null || !player.active)
                {
                    DeactivateAll();
                    return;
                }

                var modPlayer = player.GetModPlayer<LotMPlayer>();
                bool revActive = modPlayer.revelationActiveTimer > 0;

                if (revActive)
                {
                    // 每帧确认激活 - 即使其它系统切换了滤镜也能保持
                    if (Filters.Scene["TowerVortex"] != null)
                    {
                        if (!Filters.Scene["TowerVortex"].IsActive())
                        {
                            Filters.Scene.Activate("TowerVortex", player.Center);
                        }
                        // 持续推新强度+中心,防止被其它系统覆盖
                        var shader = Filters.Scene["TowerVortex"].GetShader();
                        if (shader != null)
                        {
                            shader.UseIntensity(0.6f);
                            shader.UseProgress(2.5f);
                            shader.UseTargetPosition(player.Center);
                            shader.UseColor(new Color(180, 130, 230)); // 紫金
                        }
                    }
                    wasActive = true;
                }
                else if (wasActive)
                {
                    DeactivateAll();
                    wasActive = false;
                }
            }
            catch { /* 任何渲染异常都不应崩游戏 */ }
        }

        private static void DeactivateAll()
        {
            try
            {
                if (Filters.Scene["TowerVortex"] != null && Filters.Scene["TowerVortex"].IsActive())
                {
                    Filters.Scene["TowerVortex"].Deactivate();
                }
            }
            catch { }
        }
    }
}

