using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using Terraria.IO;
using Terraria.ID;
using System.Collections.Generic;
using SubworldLibrary;
using Terraria.GameContent.Generation;
using Microsoft.Xna.Framework;
using zhashi.Content.Items;
using zhashi.Content.Items.Materials;

namespace zhashi.Content.Dimensions
{
    public class SpiritWorld : Subworld
    {
        public override int Width => 4200;
        public override int Height => 1200;
        public override bool ShouldSave => true;
        public override string Name => "Spirit World";

        public override List<GenPass> Tasks => new List<GenPass>()
        {
            // 1. 初始化
            new PassLegacy("Clearing", (progress, configuration) =>
            {
                progress.Message = "Constructing the Spirit Realm...";
                Main.worldSurface = Height / 2;
                Main.rockLayer = Main.worldSurface + 200;
            }),

            // 2. 生成生态岛屿 (★★★ 联动修改：支持灾厄群系 ★★★)
            new PassLegacy("Generating Biome Islands", (progress, configuration) =>
            {
                progress.Message = "Manifesting ecosystems...";

                int islandCount = Width / 3;

                // 检查是否加载了灾厄
                bool hasCalamity = ModLoader.TryGetMod("CalamityMod", out _);
                int biomeTypesCount = hasCalamity ? 9 : 6; // 如果有灾厄，增加 3 种新群系

                for (int i = 0; i < islandCount; i++)
                {
                    int x = WorldGen.genRand.Next(100, Width - 100);
                    int y = WorldGen.genRand.Next(100, Height - 100);

                    // 随机选择一种群系类型
                    int biomeType = WorldGen.genRand.Next(biomeTypesCount);

                    GenerateBiomeIsland(x, y, biomeType);
                }
            }),

            // 3. 几何异象
            new PassLegacy("Geometric Anomalies", (progress, configuration) =>
            {
                progress.Message = "Distorting reality...";
                int anomalies = Width / 40;
                for(int i = 0; i < anomalies; i++)
                {
                    int x = WorldGen.genRand.Next(100, Width - 100);
                    int y = WorldGen.genRand.Next(100, Height - 100);
                    if(WorldGen.genRand.NextBool()) GenerateHollowSphere(x, y);
                    else GenerateCubeFrame(x, y);
                }
            }),

            // 4. 遗迹
            new PassLegacy("Constructing Ruins", (progress, configuration) =>
            {
                progress.Message = "Restoring ancient history...";
                int structureCount = Width / 20;
                for (int i = 0; i < structureCount; i++)
                {
                    int x = WorldGen.genRand.Next(100, Width - 100);
                    int y = WorldGen.genRand.Next(100, Height - 100);
                    GeneratePlatform(x, y); // 强制生成
                }
            }),

            // 5. 陷阱
            new PassLegacy("Placing Deadly Traps", (progress, configuration) =>
            {
                progress.Message = "Hiding dangers...";
                int trapAttempts = Width * 20;
                for (int i = 0; i < trapAttempts; i++)
                {
                    int x = WorldGen.genRand.Next(100, Width - 100);
                    int y = WorldGen.genRand.Next(100, Height - 100);
                    if (!Main.tile[x, y].HasTile && Main.tile[x, y + 1].HasTile && Main.tileSolid[Main.tile[x, y + 1].TileType])
                    {
                        if (WorldGen.genRand.NextBool(100)) WorldGen.PlaceTile(x, y, TileID.Spikes, true);
                        else if (WorldGen.genRand.NextBool(200)) WorldGen.PlaceTile(x, y, TileID.LandMine, true);
                    }
                    if (Main.tile[x, y].HasTile && WorldGen.genRand.NextBool(1000)) WorldGen.PlaceTile(x, y, TileID.Explosives, true);
                }
            }),

            // 6. 泼漆
            new PassLegacy("Painting the Void", (progress, configuration) =>
            {
                progress.Message = "Coloring the unknown...";
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        if (Main.tile[x, y].HasTile && WorldGen.genRand.NextBool(15))
                        {
                            WorldGen.paintTile(x, y, (byte)WorldGen.genRand.Next(1, 31));
                        }
                    }
                }
            }),

            // 7. 出生点
            new PassLegacy("Spawn Point", (progress, configuration) =>
            {
                int centerX = Width / 2;
                int centerY = Height / 2;
                for (int x = centerX - 20; x < centerX + 20; x++)
                {
                    for (int y = centerY - 10; y < centerY + 10; y++)
                    {
                        if (Main.tile[x, y].TileType == TileID.LandMine || Main.tile[x, y].TileType == TileID.Spikes)
                            Main.tile[x, y].ClearTile();
                    }
                }
                for (int x = centerX - 10; x < centerX + 10; x++)
                {
                    for (int y = centerY; y < centerY + 5; y++)
                    {
                        WorldGen.PlaceTile(x, y, TileID.DiamondGemspark, true, true);
                    }
                }
                Main.spawnTileX = centerX;
                Main.spawnTileY = centerY - 2;
            })
        };

        public override void OnEnter() { }

        // ==========================================
        // ★★★ 核心方法：生成生态岛屿 (含灾厄联动) ★★★
        // ==========================================
        private void GenerateBiomeIsland(int x, int y, int biomeType)
        {
            ushort tileType = TileID.Dirt;
            ushort grassType = TileID.Grass;
            ushort stoneType = TileID.Stone;
            int chestStyle = 0;

            // 检查灾厄Mod
            bool calamityLoaded = ModLoader.TryGetMod("CalamityMod", out Mod calamity);

            switch (biomeType)
            {
                case 0: // 森林
                    tileType = TileID.Dirt; grassType = TileID.Grass; stoneType = TileID.Stone; chestStyle = 0; break;
                case 1: // 丛林
                    tileType = TileID.Mud; grassType = TileID.JungleGrass; stoneType = TileID.Stone; chestStyle = 10; break;
                case 2: // 雪原
                    tileType = TileID.SnowBlock; grassType = TileID.SnowBlock; stoneType = TileID.IceBlock; chestStyle = 11; break;
                case 3: // 沙漠
                    tileType = TileID.Sand; grassType = TileID.Sand; stoneType = TileID.Sandstone; chestStyle = 1; break;
                case 4: // 腐化
                    tileType = TileID.Dirt; grassType = TileID.CorruptGrass; stoneType = TileID.Ebonstone; chestStyle = 0; break;
                case 5: // 神圣
                    tileType = TileID.Dirt; grassType = TileID.HallowedGrass; stoneType = TileID.Pearlstone; chestStyle = 0; break;

                // === 灾厄群系 ===
                case 6: // 星辉瘟疫 (Astral Infection)
                    if (calamityLoaded)
                    {
                        // 使用 Find 安全获取 ModTile，防止没装Mod报错
                        if (calamity.TryFind<ModTile>("AstralDirt", out var aDirt)) tileType = aDirt.Type;
                        if (calamity.TryFind<ModTile>("AstralStone", out var aStone)) stoneType = aStone.Type;
                        grassType = tileType; // 星辉土自带草或者不需要草
                        chestStyle = 44; // 火星箱代替，因为灾厄箱子不好放置
                    }
                    break;
                case 7: // 硫磺海 (Sulphurous Sea)
                    if (calamityLoaded)
                    {
                        if (calamity.TryFind<ModTile>("SulphurousSand", out var sSand)) tileType = sSand.Type;
                        if (calamity.TryFind<ModTile>("SulphurousSandstone", out var sStone)) stoneType = sStone.Type;
                        grassType = tileType;
                        chestStyle = 1; // 金箱
                    }
                    break;
                case 8: // 硫火之崖 (Brimstone Crag)
                    if (calamityLoaded)
                    {
                        if (calamity.TryFind<ModTile>("BrimstoneSlag", out var bSlag)) tileType = bSlag.Type;
                        if (calamity.TryFind<ModTile>("InfernalSuevite", out var bStone)) stoneType = bStone.Type;
                        grassType = tileType;
                        chestStyle = 50; // 黑曜石箱
                    }
                    break;
            }

            // 2. 生成岛屿主体 (addTile = true)
            int size = WorldGen.genRand.Next(8, 20);
            WorldGen.TileRunner(x, y, size, WorldGen.genRand.Next(5, 10), tileType, true, 0f, 0f, true, true);
            WorldGen.TileRunner(x, y, size / 2, WorldGen.genRand.Next(3, 8), stoneType, true, 0f, 0f, true, true);

            // 3. 表面处理
            int radius = size + 5;
            bool chestPlaced = false;

            for (int i = x - radius; i <= x + radius; i++)
            {
                for (int j = y - radius; j <= y + radius; j++)
                {
                    if (!WorldGen.InWorld(i, j)) continue;

                    Tile tile = Main.tile[i, j];
                    Tile upTile = Main.tile[i, j - 1];

                    // 确保方块存在且上方为空
                    if (tile.HasTile && (tile.TileType == tileType || tile.TileType == stoneType) && !upTile.HasTile)
                    {
                        // 种草 (仅针对原版泥土淤泥)
                        if (tile.TileType == TileID.Dirt || tile.TileType == TileID.Mud)
                        {
                            tile.TileType = grassType;
                        }

                        // 随机种植物
                        if (WorldGen.genRand.NextBool(5))
                        {
                            if (biomeType == 3) WorldGen.PlantCactus(i, j); // 原版沙漠
                            else if (biomeType == 7 && calamityLoaded) { /* 硫磺海可以尝试种点别的，暂时留空 */ }
                            else
                            {
                                if (WorldGen.genRand.NextBool(3)) WorldGen.GrowTree(i, j);
                                else WorldGen.PlaceTile(i, j - 1, TileID.Plants, true);
                            }
                        }

                        // 放置宝箱
                        if (!chestPlaced && WorldGen.genRand.NextBool(15))
                        {
                            if (Main.tile[i + 1, j].HasTile)
                            {
                                int chestIndex = WorldGen.PlaceChest(i, j - 1, style: chestStyle);
                                if (chestIndex != -1)
                                {
                                    FillBiomeChest(chestIndex, biomeType);
                                    chestPlaced = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        // ==========================================
        // ★★★ 核心方法：填充战利品 (含灾厄联动) ★★★
        // ==========================================
        private void FillBiomeChest(int chestIndex, int biomeType)
        {
            Chest chest = Main.chest[chestIndex];
            int slot = 0;
            bool calamityLoaded = ModLoader.TryGetMod("CalamityMod", out Mod calamity);

            // 1. 核心饰品池 (原版)
            List<int> accessories = new List<int>
            {
                ItemID.CloudinaBottle, ItemID.HermesBoots, ItemID.BandofRegeneration,
                ItemID.MagicMirror, ItemID.LuckyHorseshoe, ItemID.Aglet, ItemID.AnkletoftheWind
            };

            // === 联动：添加灾厄饰品 ===
            if (calamityLoaded)
            {
                // 尝试添加一些灾厄前期饰品
                if (calamity.TryFind<ModItem>("RoverDrive", out var i1)) accessories.Add(i1.Type);
                if (calamity.TryFind<ModItem>("FungalSymbiote", out var i2)) accessories.Add(i2.Type);
                if (calamity.TryFind<ModItem>("LuxorsGift", out var i3)) accessories.Add(i3.Type);
                if (calamity.TryFind<ModItem>("TrinketofChi", out var i4)) accessories.Add(i4.Type);
            }

            // 随机选一个
            int rareItem = accessories[WorldGen.genRand.Next(accessories.Count)];

            // 群系特定掉落
            if (biomeType == 1 && WorldGen.genRand.NextBool()) rareItem = ItemID.FeralClaws;
            if (biomeType == 2 && WorldGen.genRand.NextBool()) rareItem = ItemID.BlizzardinaBottle;
            if (biomeType == 3 && WorldGen.genRand.NextBool()) rareItem = ItemID.SandstorminaBottle;

            chest.item[slot].SetDefaults(rareItem);
            slot++;

            // 2. 武器/辅助
            if (WorldGen.genRand.NextBool())
            {
                int weapon = WorldGen.genRand.Next(new int[] { ItemID.EnchantedBoomerang, ItemID.Shuriken, ItemID.Blowpipe });
                chest.item[slot].SetDefaults(weapon);
                if (weapon == ItemID.Shuriken) chest.item[slot].stack = 50;
                slot++;
            }

            // 3. 药水池 (原版 + 灾厄)
            List<int> potions = new List<int> { ItemID.HealingPotion, ItemID.ManaPotion, ItemID.IronskinPotion, ItemID.SwiftnessPotion, ItemID.RegenerationPotion, ItemID.NightOwlPotion };

            // === 联动：添加灾厄药水 ===
            if (calamityLoaded)
            {
                if (calamity.TryFind<ModItem>("SupremeHealingPotion", out var p1)) potions.Add(p1.Type);
                if (calamity.TryFind<ModItem>("OmegaHealingPotion", out var p2)) potions.Add(p2.Type);
                if (calamity.TryFind<ModItem>("SulphurskinPotion", out var p3)) potions.Add(p3.Type); // 硫磺皮肤药水
                if (calamity.TryFind<ModItem>("ZenPotion", out var p4)) potions.Add(p4.Type); // 禅定药水
            }

            int potionCount = WorldGen.genRand.Next(2, 4);
            for (int k = 0; k < potionCount; k++)
            {
                chest.item[slot].SetDefaults(potions[WorldGen.genRand.Next(potions.Count)]);
                chest.item[slot].stack = WorldGen.genRand.Next(3, 10);
                slot++;
            }

            // 4. 钱币 (灾厄通常给的多一点)
            chest.item[slot].SetDefaults(ItemID.GoldCoin);
            chest.item[slot].stack = calamityLoaded ? WorldGen.genRand.Next(20, 80) : WorldGen.genRand.Next(10, 50);
            slot++;

            // 5. 亵渎石板 (Mod特色)
            if (WorldGen.genRand.NextBool(5))
            {
                chest.item[slot].SetDefaults(ModContent.ItemType<BlasphemySlate>());
                chest.item[slot].stack = 1;
                slot++;
            }
        }

        private void GenerateHollowSphere(int x, int y)
        {
            int radius = WorldGen.genRand.Next(8, 15);
            ushort tileType = TileID.Glass;
            for (int i = x - radius; i <= x + radius; i++)
            {
                for (int j = y - radius; j <= y + radius; j++)
                {
                    float dist = Vector2.Distance(new Vector2(i, j), new Vector2(x, y));
                    if (dist > radius - 1 && dist < radius + 1)
                        WorldGen.PlaceTile(i, j, tileType, true, true);
                }
            }
        }

        private void GenerateCubeFrame(int x, int y)
        {
            int size = WorldGen.genRand.Next(10, 20);
            ushort tileType = TileID.LivingFire;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (i == 0 || i == size - 1 || j == 0 || j == size - 1)
                        WorldGen.PlaceTile(x + i, y - j, tileType, true, true);
                }
            }
        }

        private void GeneratePlatform(int x, int y)
        {
            int width = WorldGen.genRand.Next(10, 20);
            for (int i = 0; i < width; i++)
            {
                WorldGen.PlaceTile(x + i, y, TileID.MarbleBlock, true, true);
            }
            if (WorldGen.genRand.NextBool(3))
                WorldGen.PlaceTile(x + WorldGen.genRand.Next(1, width - 1), y - 1, TileID.Spikes, true);

            int chestX = x + width / 2;
            int chestY = y - 1;
            int chestIndex = WorldGen.PlaceChest(chestX, chestY, style: 13);

            if (chestIndex != -1)
            {
                Chest chest = Main.chest[chestIndex];
                int slot = 0;
                chest.item[slot].SetDefaults(ModContent.ItemType<BlasphemySlate>());
                chest.item[slot].stack = WorldGen.genRand.Next(1, 3);
                slot++;
                chest.item[slot].SetDefaults(ItemID.GoldCoin);
                chest.item[slot].stack = WorldGen.genRand.Next(10, 50);
                slot++;
                chest.item[slot].SetDefaults(ItemID.SuperHealingPotion);
                chest.item[slot].stack = WorldGen.genRand.Next(5, 15);
                slot++;
            }
        }
    }
}