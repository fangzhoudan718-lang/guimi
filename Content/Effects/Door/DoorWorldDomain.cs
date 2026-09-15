using Terraria.ModLoader;

namespace zhashi.Content.Effects.Door
{
    // Compatibility surface for the former full-screen domain overlay.
    // Spatial effects now remain in world space; never cover terrain, hostile shots or the HUD.
    // The previous implementation is archived under SourceAssets/Door/Legacy for reference.
    public class DoorWorldDomain : ModSystem
    {
        public static bool Active => false;
        public static float Strength => 0;
    }
}
