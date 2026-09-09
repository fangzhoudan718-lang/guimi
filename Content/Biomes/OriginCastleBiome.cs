using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using SubworldLibrary;
using zhashi.Content.Dimensions;

namespace zhashi.Content.Biomes
{
    public class OriginCastleBiome : ModBiome
    {
        public override bool IsBiomeActive(Player player)
        {
            return SubworldSystem.IsActive<OriginCastle>();
        }

        public override void SpecialVisuals(Player player, bool isActive)
        {
            if (player.whoAmI != Main.myPlayer) return;

            try
            {
                if (isActive)
                {
                    if (!SkyManager.Instance["Vortex"].IsActive())
                        SkyManager.Instance.Activate("Vortex");
                }
                else
                {
                    if (SkyManager.Instance["Vortex"].IsActive())
                        SkyManager.Instance.Deactivate("Vortex");
                }

                string filterName = "MonolithVoid";
                if (Filters.Scene[filterName] != null)
                {
                    if (isActive && !Filters.Scene[filterName].IsActive())
                        Filters.Scene.Activate(filterName);
                    else if (!isActive && Filters.Scene[filterName].IsActive())
                        Filters.Scene.Deactivate(filterName);
                }
            }
            catch { }
        }

        // 【核心修改】引用自定义音乐
        // 确保你的文件路径是 Assets/Music/Music1.mp3 (不用加 .mp3 后缀)
        public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Music/Music1");

        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;
    }
}