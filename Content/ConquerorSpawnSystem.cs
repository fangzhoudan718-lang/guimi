using Terraria;
using Terraria.ModLoader;

namespace zhashi.Content
{
    public class ConquerorSpawnSystem : ModSystem
    {
        public static bool StopSpawning = false;

        // 每帧检查
        public override void PreUpdateNPCs()
        {
            if (StopSpawning)
            {
                Player p = Main.LocalPlayer;
                if (p.dead || !p.active)
                {
                    StopSpawning = false;
                }
            }
        }
        public override void OnWorldUnload()
        {
            StopSpawning = false;
        }
    }

    public class ConquerorSpawnRateControl : GlobalNPC
    {
        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (ConquerorSpawnSystem.StopSpawning)
            {
                spawnRate = int.MaxValue;
                maxSpawns = 0;
            }
        }
    }
}