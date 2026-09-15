using Terraria;
using Terraria.ModLoader;

namespace zhashi.Content
{
    public class ConquerorSpawnSystem : ModSystem
    {
        public static bool StopSpawning = false;
        public static int StopSpawningOwner = -1;

        // 每帧检查
        public override void PreUpdateNPCs()
        {
            if (StopSpawning)
            {
                bool ownerInvalid = StopSpawningOwner < 0 || StopSpawningOwner >= Main.maxPlayers;
                Player p = ownerInvalid ? null : Main.player[StopSpawningOwner];
                if (ownerInvalid || p == null || p.dead || !p.active)
                {
                    StopSpawning = false;
                    StopSpawningOwner = -1;
                    if (Main.netMode == Terraria.ID.NetmodeID.Server)
                        NetMessage.SendData(Terraria.ID.MessageID.WorldData);
                }
            }
        }

        public override void NetSend(System.IO.BinaryWriter writer)
        {
            writer.Write(StopSpawning);
            writer.Write(StopSpawningOwner);
        }

        public override void NetReceive(System.IO.BinaryReader reader)
        {
            StopSpawning = reader.ReadBoolean();
            StopSpawningOwner = reader.ReadInt32();
        }

        public override void OnWorldUnload()
        {
            StopSpawning = false;
            StopSpawningOwner = -1;
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
