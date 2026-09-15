using System.Collections.Generic;
using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Dimensions
{
    public class SpiritWorldSpawns : GlobalNPC
    {
        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (!SubworldSystem.IsActive<SpiritWorld>()) return;
            spawnRate = NPC.downedPlantBoss ? 28 : (Main.hardMode ? 45 : 90);
            maxSpawns = NPC.downedPlantBoss ? 18 : (Main.hardMode ? 12 : 6);
        }

        public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
            if (!SubworldSystem.IsActive<SpiritWorld>()) return;

            pool.Clear();
            pool.Add(NPCID.Ghost, 1f);

            if (Main.hardMode)
            {
                pool.Add(NPCID.Pixie, 0.45f);
                pool.Add(NPCID.Wraith, 0.75f);
                pool.Add(NPCID.ChaosElemental, 0.4f);
                pool.Add(NPCID.Gastropod, 0.25f);
            }

            if (NPC.downedPlantBoss)
            {
                pool.Add(NPCID.Poltergeist, 0.55f);
                pool.Add(NPCID.EnchantedSword, 0.25f);
            }
        }
    }
}
