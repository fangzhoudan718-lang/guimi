using Terraria;
using Terraria.ModLoader;

namespace zhashi.Content.DaqianLu
{
    public class DaqianLuSystem : ModSystem
    {
        public override void PostUpdateEverything()
        {
            var lotmPlayer = Main.LocalPlayer.GetModPlayer<LotMPlayer>();

            // 极致还原5：当理智值过低时，大千录会自动诱惑玩家
            if (lotmPlayer.sanityCurrent < 30f && Main.rand.NextBool(1000))
            {
                string[] whispers = { "把皮剥下来...", "再划一刀...", "这些痛苦还不够..." };
                Main.NewText(whispers[Main.rand.Next(whispers.Length)], 150, 0, 0);
            }
        }
    }
}