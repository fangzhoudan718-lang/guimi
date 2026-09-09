using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using System.Collections.Generic;
using zhashi.Content.Configs;
// 引用你的魔药文件夹
using zhashi.Content.Items.Potions.Sun;
using zhashi.Content.Items.Potions.Fool;
using zhashi.Content.Items.Potions.Moon;
using zhashi.Content.Items.Potions.Hunter;
using zhashi.Content.Items.Potions.Marauder;
using zhashi.Content.Items.Potions.Demoness;
using zhashi.Content.Items.Potions.Wheel;
using zhashi.Content.Items.Potions;

namespace zhashi.Content.Globals
{
    public class CalamityPotionLock : GlobalItem
    {
        public override bool CanUseItem(Item item, Player player)
        {
            // 1. 如果没开“灾厄适配模式”，直接放行
            if (!ModContent.GetInstance<LotMConfig>().CalamityAdaptationMode)
                return base.CanUseItem(item, player);

            // 2. 判断是否是本模组的物品
            if (item.ModItem?.Mod == ModLoader.GetMod("zhashi"))
            {
                int sequenceToCheck = -1;

                // 序列 9 (起始)
                if (item.type == ModContent.ItemType<BardPotion>() || item.type == ModContent.ItemType<SeerPotion>() ||
                    item.type == ModContent.ItemType<HunterPotion>() || item.type == ModContent.ItemType<ApothecaryPotion>() ||
                    item.type == ModContent.ItemType<MarauderPotion>() || item.type == ModContent.ItemType<AssassinPotion>() ||
                    item.type == ModContent.ItemType<MonsterPotion>()) sequenceToCheck = 9;

                // 序列 8
                else if (item.type == ModContent.ItemType<LightSupplicantPotion>() || item.type == ModContent.ItemType<ClownPotion>() ||
                         item.type == ModContent.ItemType<ProvokerPotion>() || item.type == ModContent.ItemType<BeastTamerPotion>() ||
                         item.type == ModContent.ItemType<SwindlerPotion>() || item.type == ModContent.ItemType<InstigatorPotion>() ||
                         item.type == ModContent.ItemType<RobotPotion>()) sequenceToCheck = 8;

                // 序列 7
                else if (item.type == ModContent.ItemType<SolarHighPriestPotion>() || item.type == ModContent.ItemType<MagicianPotion>() ||
                         item.type == ModContent.ItemType<PyromaniacPotion>() || item.type == ModContent.ItemType<VampirePotion>() ||
                         item.type == ModContent.ItemType<CryptologistPotion>() || item.type == ModContent.ItemType<WitchPotion>() ||
                         item.type == ModContent.ItemType<LuckyPotion>()) sequenceToCheck = 7;

                // 序列 6
                else if (item.type == ModContent.ItemType<NotaryPotion>() || item.type == ModContent.ItemType<FacelessPotion>() ||
                         item.type == ModContent.ItemType<ConspiratorPotion>() || item.type == ModContent.ItemType<PotionsProfessorPotion>() ||
                         item.type == ModContent.ItemType<PrometheusPotion>() || item.type == ModContent.ItemType<PleasureDemonessPotion>() ||
                         item.type == ModContent.ItemType<CalamityPriestPotion>()) sequenceToCheck = 6;

                // 序列 5 (中序列起点)
                else if (item.type == ModContent.ItemType<PriestPotion>() || item.type == ModContent.ItemType<MarionettistPotion>() ||
                         item.type == ModContent.ItemType<ReaperPotion>() || item.type == ModContent.ItemType<ScarletScholarPotion>() ||
                         item.type == ModContent.ItemType<DreamStealerPotion>() || item.type == ModContent.ItemType<AfflictionDemonessPotion>() ||
                         item.type == ModContent.ItemType<WinnerPotion>()) sequenceToCheck = 5;

                // 序列 4 (半神 - 质变点)
                else if (item.type == ModContent.ItemType<UnshadowedPotion>() || item.type == ModContent.ItemType<BizarroSorcererPotion>() ||
                         item.type == ModContent.ItemType<IronBloodedKnightPotion>() || item.type == ModContent.ItemType<WitchKingPotion>() ||
                         item.type == ModContent.ItemType<ParasitePotion>() || item.type == ModContent.ItemType<DespairDemonessPotion>() ||
                         item.type == ModContent.ItemType<MisfortuneMagePotion>()) sequenceToCheck = 4;

                // 序列 3 (圣者)
                else if (item.type == ModContent.ItemType<JusticeMentorPotion>() || item.type == ModContent.ItemType<ScholarOfYorePotion>() ||
                         item.type == ModContent.ItemType<WarBishopPotion>() || item.type == ModContent.ItemType<SummoningMasterPotion>() ||
                         item.type == ModContent.ItemType<MentorPotion>() || item.type == ModContent.ItemType<UnagingDemonessPotion>() ||
                         item.type == ModContent.ItemType<AnomalyPotion>()) sequenceToCheck = 3;

                // 序列 2 (天使 - 顶级战力)
                else if (item.type == ModContent.ItemType<LightSeekerPotion>() || item.type == ModContent.ItemType<MiracleInvokerPotion>() ||
                         item.type == ModContent.ItemType<WeatherWarlockPotion>() || item.type == ModContent.ItemType<LifeGiverPotion>() ||
                         item.type == ModContent.ItemType<TrojanHorsePotion>() || item.type == ModContent.ItemType<CatastropheDemonessPotion>() ||
                         item.type == ModContent.ItemType<ProphetPotion>()) sequenceToCheck = 2;

                // 序列 1 (天使之王 - 接近神)
                else if (item.type == ModContent.ItemType<WhiteAngelPotion>() || item.type == ModContent.ItemType<AttendantPotion>() ||
                         item.type == ModContent.ItemType<ConquerorPotion>() || item.type == ModContent.ItemType<BeautyGoddessPotion>() ||
                         item.type == ModContent.ItemType<WormOfTimePotion>() || item.type == ModContent.ItemType<ApocalypseDemonessPotion>() ||
                         item.type == ModContent.ItemType<SerpentPotion>()) sequenceToCheck = 1;


                // 3. 核心：检查 Boss 击杀条件 (适配灾厄流程)
                if (sequenceToCheck != -1)
                {
                    string missingBoss = "";
                    bool isCalamityLoaded = ModLoader.TryGetMod("CalamityMod", out Mod calamity);

                    switch (sequenceToCheck)
                    {
                        case 9: // 开局可用
                            break;

                        case 8: // 克苏鲁之眼 / 荒漠灾虫
                            if (isCalamityLoaded && !(bool)calamity.Call("GetBossDowned", "desertscourge") && !NPC.downedBoss1)
                                missingBoss = "克苏鲁之眼 或 荒漠灾虫";
                            else if (!isCalamityLoaded && !NPC.downedBoss1)
                                missingBoss = "克苏鲁之眼";
                            break;

                        case 7: // 克脑/世吞 / 腐巢/宿主
                            if (!NPC.downedBoss2) missingBoss = "世界吞噬者 或 克苏鲁之脑";
                            break;

                        case 6: // 骷髅王 (肉前毕业)
                            if (!NPC.downedBoss3) missingBoss = "骷髅王";
                            break;

                        case 5: // 肉山 (开启困难模式)
                            // 序列5通常很强，放在肉后初期
                            if (!Main.hardMode) missingBoss = "血肉墙";
                            break;

                        case 4: // 半神 (原版毕业 -> 灾厄起步)
                            if (!NPC.downedMoonlord) missingBoss = "月球领主";
                            break;

                        case 3: // 圣者 (对应亵渎天神)
                            if (isCalamityLoaded && !(bool)calamity.Call("GetBossDowned", "providence"))
                                missingBoss = "亵渎天神";
                            else if (!isCalamityLoaded && !NPC.downedMoonlord)
                                missingBoss = "月球领主"; // 没灾厄就还是月总
                            break;

                        case 2: // 天使 (对应神之吞噬者)
                            if (isCalamityLoaded && !(bool)calamity.Call("GetBossDowned", "devourerofgods"))
                                missingBoss = "神之吞噬者";
                            break;

                        case 1: // 天使之王 (对应丛林龙)
                            if (isCalamityLoaded && !(bool)calamity.Call("GetBossDowned", "yharon"))
                                missingBoss = "丛林龙, 犽戎";
                            break;
                    }

                    // 4. 拦截并提示
                    if (missingBoss != "")
                    {
                        Main.NewText($"[灾厄平衡] 灵性受阻！晋升序列 {sequenceToCheck} 过于强大，需先击败：{missingBoss}", 255, 100, 100);
                        return false;
                    }
                }
            }

            return base.CanUseItem(item, player);
        }
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (!ModContent.GetInstance<LotMConfig>().CalamityAdaptationMode) return;
            if (item.ModItem?.Mod != ModLoader.GetMod("zhashi")) return;

            // 简单判断是否是魔药 (根据名字或类型，这里简单演示)
            if (item.Name.Contains("Potion") || item.Name.Contains("魔药"))
            {
                tooltips.Add(new TooltipLine(Mod, "CalamityLock",
                    "[c/FF0000:灾厄平衡模式已开启]\n" +
                    "服用该魔药可能有 Boss 击杀限制"));
            }
        }
    }
}