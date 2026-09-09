using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using SubworldLibrary;
using System.Collections.Generic;
using zhashi.Content.NPCs.Bosses.Aurmir;
// using zhashi.Content.NPCs; // 删除了TestBoss所在的命名空间引用（如果TestBoss在NPCs下，这行其实不删也不影响，但为了整洁建议删掉）

namespace zhashi.Content.Dimensions
{
    public class SpiritWorldSpawns : GlobalNPC
    {
        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            // 只有在灵界生效
            if (SubworldSystem.IsActive<SpiritWorld>())
            {
                // ==== 疯狂刷怪模式 ====
                spawnRate = 5;
                maxSpawns = 80;
            }
        }

        public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
            if (SubworldSystem.IsActive<SpiritWorld>())
            {
                pool.Clear(); // 清空原版生物

                // 1. 基础灵界生物
                pool.Add(NPCID.Ghost, 1.0f);
                pool.Add(NPCID.Wraith, 0.8f);
                pool.Add(NPCID.Poltergeist, 0.6f);
                pool.Add(NPCID.ChaosElemental, 0.4f);
                pool.Add(NPCID.Pixie, 0.5f);
                pool.Add(NPCID.Gastropod, 0.3f);

                // 2. 检查当前场上的 Boss 数量 (限制 5 个以内)
                int currentBossCount = 0;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && (Main.npc[i].boss || Main.npc[i].type == NPCID.EaterofWorldsHead))
                    {
                        currentBossCount++;
                    }
                }

                // 3. 随机 Boss 生成
                if (currentBossCount < 5)
                {
                    float bossWeight = 0.001f; // 0.1% 的生成权重


                    // === 原版 Boss ===
                    pool.Add(NPCID.KingSlime, bossWeight);
                    pool.Add(NPCID.EyeofCthulhu, bossWeight);
                    pool.Add(NPCID.QueenBee, bossWeight);
                    pool.Add(NPCID.SkeletronHead, bossWeight);
                    pool.Add(NPCID.TheDestroyer, bossWeight);
                    pool.Add(NPCID.Retinazer, bossWeight);
                    pool.Add(NPCID.Spazmatism, bossWeight);
                    pool.Add(NPCID.SkeletronPrime, bossWeight);
                    pool.Add(NPCID.Plantera, bossWeight);
                    pool.Add(NPCID.Golem, bossWeight);
                    pool.Add(NPCID.DukeFishron, bossWeight);
                    pool.Add(NPCID.HallowBoss, bossWeight);
                    pool.Add(NPCID.CultistBoss, bossWeight);
                    pool.Add(NPCID.MoonLordCore, 0.001f); // 月总还是少出点好

                    // === ★★★ 灾厄 Mod (Calamity) 兼容支持 ★★★ ===
                    if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
                    {
                        // 灾厄常见 Boss 的内部名称列表
                        // 注意：这里列出的是主要 Boss，多体 Boss 通常只加头部或核心
                        string[] calamityBosses = new string[]
                        {
                            "DesertScourgeHead",        // 荒漠灾虫
                            "Crabulon",                 // 菌生蟹
                            "HiveMind",                 // 腐巢意志
                            "PerforatorHive",           // 血肉宿主
                            "SlimeGodCore",             // 史莱姆之神
                            "Cryogen",                  // 极地之灵
                            "AquaticScourgeHead",       // 渊海灾虫
                            "BrimstoneElemental",       // 硫磺火元素
                            "CalamitasClone",           // 灾厄之眼 (克隆)
                            "Leviathan",                // 利维坦
                            "AstrumAureus",             // 白金星舰
                            "PlaguebringerGoliath",     // 瘟疫使者歌莉娅
                            "RavagerBody",              // 毁灭魔像
                            "AstrumDeusHead",           // 星神游龙
                            "ProfanedGuardianCommander",// 亵渎守卫
                            "Providence",               // 亵渎天神
                            "StormWeaverHead",          // 风暴编织者
                            "CeaselessVoid",            // 无尽虚空
                            "Signus",                   // 西格纳斯
                            "Polterghast",              // 噬魂幽花
                            "OldDuke",                  // 老旧公爵
                            "DevourerofGodsHead",       // 神明吞噬者
                            "Yharon",                   // 丛林龙
                            "SupremeCalamitas"          // 至尊灾厄
                        };

                        // 遍历并尝试添加
                        foreach (string name in calamityBosses)
                        {
                            // TryFind 可以安全地查找 ModNPC，找不到也不会报错（比如灾厄改名了或没装）
                            if (calamity.TryFind<ModNPC>(name, out ModNPC bossNPC))
                            {
                                pool.Add(bossNPC.Type, bossWeight);
                            }
                        }
                    }
                }
            }
        }
    }
}