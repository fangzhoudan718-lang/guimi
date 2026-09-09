using Terraria.ModLoader;

namespace zhashi.Content.DaqianLu
{
    public class DaqianLuKeybinds : ModSceneEffect // 或者简单的 ModSystem
    {
        public static ModKeybind UseDaqianLu { get; private set; }

        public override void Load()
        {
            UseDaqianLu = KeybindLoader.RegisterKeybind(Mod, "大千录：祭", "Z");
        }

        public override void Unload()
        {
            UseDaqianLu = null;
        }
    }
}