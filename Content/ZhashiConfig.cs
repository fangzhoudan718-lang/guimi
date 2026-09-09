using System.ComponentModel;
using Terraria.ModLoader.Config;
using Microsoft.Xna.Framework;

namespace zhashi.Content.Configs
{
    public class ZhashiConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        public static ZhashiConfig Instance;

        [Header("灵性条设置")]

        [Label("缩放比例")]
        [Tooltip("调整灵性条的大小")]
        [Range(0.5f, 2.0f)]
        [Increment(0.1f)]
        [DefaultValue(1f)]
        [Slider]
        public float BarScale { get; set; }

        [Label("位置坐标")]
        [DefaultValue(typeof(Vector2), "500, 20")]
        public Vector2 BarPosition { get; set; }
    }
}