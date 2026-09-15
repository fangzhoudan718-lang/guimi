using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Localization;
using zhashi.Content.Items.Materials;

namespace zhashi.Content.Dimensions
{
    public class SpiritWorldExplorerPlayer : ModPlayer
    {
        private readonly HashSet<int> discoveredRegions = new();
        private int updateTimer;
        private int lastCurrent = -1;

        public override void SaveData(TagCompound tag) => tag["spiritWorldRegions"] = new List<int>(discoveredRegions);

        public override void LoadData(TagCompound tag)
        {
            discoveredRegions.Clear();
            foreach (int region in tag.GetList<int>("spiritWorldRegions")) discoveredRegions.Add(region);
        }

        public override void PostUpdate()
        {
            if (!SubworldSystem.IsActive<SpiritWorld>() || Player.dead)
            {
                lastCurrent = -1;
                return;
            }
            if (++updateTimer < 60) return;
            updateTimer = 0;

            int regionX = (int)(Player.Center.X / 1920f);
            int regionY = (int)(Player.Center.Y / 1440f);
            int regionId = (regionX << 16) ^ (regionY & 0xFFFF);
            int current = PositiveHash(regionX * 73856093 ^ regionY * 19349663 ^ Main.worldID) % 4;
            ApplyCurrent(current);

            if (current != lastCurrent && Player.whoAmI == Main.myPlayer)
            {
                Main.NewText(GetCurrentName(current), new Color(135, 205, 195));
                lastCurrent = current;
            }

            if (discoveredRegions.Add(regionId))
            {
                if (Player.whoAmI == Main.myPlayer)
                    Main.NewText(Language.GetTextValue("Mods.zhashi.Messages.SpiritCurrents.Discovery", discoveredRegions.Count), new Color(175, 215, 200));
                if (Main.netMode != NetmodeID.MultiplayerClient && discoveredRegions.Count % 4 == 0)
                    Player.QuickSpawnItem(Player.GetSource_Misc("SpiritWorldDiscovery"), NPC.downedPlantBoss ? ItemID.Ectoplasm : ItemID.FallenStar, 1 + discoveredRegions.Count / 36);
                if (Main.netMode != NetmodeID.MultiplayerClient && NPC.downedPlantBoss && discoveredRegions.Count % 12 == 0)
                    Player.QuickSpawnItem(Player.GetSource_Misc("SpiritWorldDiscovery"), ModContent.ItemType<WhisperOfSpiritWorld>());
            }
        }

        private void ApplyCurrent(int current)
        {
            switch (current)
            {
                case 0: Player.AddBuff(BuffID.Swiftness, 90); break;
                case 1: Player.AddBuff(BuffID.Dangersense, 90); break;
                case 2: Player.AddBuff(BuffID.Featherfall, 90); break;
                default: Player.AddBuff(BuffID.Regeneration, 90); break;
            }
        }

        private static int PositiveHash(int value) => value == int.MinValue ? 0 : System.Math.Abs(value);

        public bool TryFindNearestUnexplored(out string arrow, out int regionsAway)
        {
            int originX = (int)(Player.Center.X / 1920f);
            int originY = (int)(Player.Center.Y / 1440f);
            int maxX = System.Math.Max(0, (Main.maxTilesX * 16 - 1) / 1920);
            int maxY = System.Math.Max(0, (Main.maxTilesY * 16 - 1) / 1440);

            for (int radius = 1; radius <= 12; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int dyMagnitude = radius - System.Math.Abs(dx);
                    for (int sign = -1; sign <= 1; sign += 2)
                    {
                        int dy = dyMagnitude * sign;
                        if (dyMagnitude == 0 && sign > -1) continue;
                        int x = originX + dx;
                        int y = originY + dy;
                        if (x < 0 || y < 0 || x > maxX || y > maxY) continue;
                        int id = (x << 16) ^ (y & 0xFFFF);
                        if (discoveredRegions.Contains(id)) continue;

                        arrow = System.Math.Abs(dx) >= System.Math.Abs(dy)
                            ? (dx < 0 ? "←" : "→")
                            : (dy < 0 ? "↑" : "↓");
                        regionsAway = radius;
                        return true;
                    }
                }
            }
            arrow = "·";
            regionsAway = 0;
            return false;
        }

        private static string GetCurrentName(int current) => Language.GetTextValue(current switch
        {
            0 => "Mods.zhashi.Messages.SpiritCurrents.Haste",
            1 => "Mods.zhashi.Messages.SpiritCurrents.Insight",
            2 => "Mods.zhashi.Messages.SpiritCurrents.Weightless",
            _ => "Mods.zhashi.Messages.SpiritCurrents.Rest"
        });
    }
}
