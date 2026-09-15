using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Configs;
using zhashi.Content.Items.Potions;
using zhashi.Content.Items.Potions.BlackEmperor;
using zhashi.Content.Items.Potions.Demoness;
using zhashi.Content.Items.Potions.Door;
using zhashi.Content.Items.Potions.Fool;
using zhashi.Content.Items.Potions.Hunter;
using zhashi.Content.Items.Potions.Marauder;
using zhashi.Content.Items.Potions.Moon;
using zhashi.Content.Items.Potions.Sun;
using zhashi.Content.Items.Potions.Wheel;

namespace zhashi.Content.Globals
{
    /// <summary>
    /// 灾厄适配模式：把九条途径的晋升魔药按“序列 → 该阶段应有的进度”锁住。
    ///
    /// 判定只看序列、不看途径，所以九条途径（太阳/愚者/猎人/月亮/掠夺者/魔女/命运/巨人/门）
    /// 共用同一张进度表；以后接入新途径，只要把它的魔药加进对应序列即可。
    /// 灾厄专属 Boss 一律作为“替代条件”，也就是多一条通关路径，而不是额外加锁。
    /// </summary>
    public class CalamityPotionLock : GlobalItem
    {
        /// <summary>返回这瓶魔药会让玩家晋升到的序列；不是本模组的晋升魔药时返回 -1。</summary>
        private static int SequenceForPotion(Item item)
        {
            if (item.ModItem?.Mod != ModLoader.GetMod("zhashi")) return -1;

            // 序列 9——九条途径的起点
            if (item.type == ModContent.ItemType<BardPotion>() || item.type == ModContent.ItemType<SeerPotion>() ||
                item.type == ModContent.ItemType<HunterPotion>() || item.type == ModContent.ItemType<ApothecaryPotion>() ||
                item.type == ModContent.ItemType<MarauderPotion>() || item.type == ModContent.ItemType<AssassinPotion>() ||
                item.type == ModContent.ItemType<MonsterPotion>() || item.type == ModContent.ItemType<WarriorPotion>() ||
                item.type == ModContent.ItemType<ApprenticePotion>() ||
                item.type == ModContent.ItemType<LawyerPotion>()) return 9;

            // 序列 8
            if (item.type == ModContent.ItemType<LightSupplicantPotion>() || item.type == ModContent.ItemType<ClownPotion>() ||
                item.type == ModContent.ItemType<ProvokerPotion>() || item.type == ModContent.ItemType<BeastTamerPotion>() ||
                item.type == ModContent.ItemType<SwindlerPotion>() || item.type == ModContent.ItemType<InstigatorPotion>() ||
                item.type == ModContent.ItemType<RobotPotion>() || item.type == ModContent.ItemType<PugilistPotion>() ||
                item.type == ModContent.ItemType<TricksterPotion>() ||
                item.type == ModContent.ItemType<SavagePotion>()) return 8;

            // 序列 7
            if (item.type == ModContent.ItemType<SolarHighPriestPotion>() || item.type == ModContent.ItemType<MagicianPotion>() ||
                item.type == ModContent.ItemType<PyromaniacPotion>() || item.type == ModContent.ItemType<VampirePotion>() ||
                item.type == ModContent.ItemType<CryptologistPotion>() || item.type == ModContent.ItemType<WitchPotion>() ||
                item.type == ModContent.ItemType<LuckyPotion>() || item.type == ModContent.ItemType<WeaponMasterPotion>() ||
                item.type == ModContent.ItemType<AstrologerPotion>() ||
                item.type == ModContent.ItemType<BriberPotion>()) return 7;

            // 序列 6
            if (item.type == ModContent.ItemType<NotaryPotion>() || item.type == ModContent.ItemType<FacelessPotion>() ||
                item.type == ModContent.ItemType<ConspiratorPotion>() || item.type == ModContent.ItemType<PotionsProfessorPotion>() ||
                item.type == ModContent.ItemType<PrometheusPotion>() || item.type == ModContent.ItemType<PleasureDemonessPotion>() ||
                item.type == ModContent.ItemType<CalamityPriestPotion>() || item.type == ModContent.ItemType<DawnKnightPotion>() ||
                item.type == ModContent.ItemType<RecorderPotion>() ||
                item.type == ModContent.ItemType<CorruptBaronPotion>()) return 6;

            // 序列 5
            if (item.type == ModContent.ItemType<PriestPotion>() || item.type == ModContent.ItemType<MarionettistPotion>() ||
                item.type == ModContent.ItemType<ReaperPotion>() || item.type == ModContent.ItemType<ScarletScholarPotion>() ||
                item.type == ModContent.ItemType<DreamStealerPotion>() || item.type == ModContent.ItemType<AfflictionDemonessPotion>() ||
                item.type == ModContent.ItemType<WinnerPotion>() || item.type == ModContent.ItemType<GuardianPotion>() ||
                item.type == ModContent.ItemType<TravelerPotion>() ||
                item.type == ModContent.ItemType<ChaosMentorPotion>()) return 5;

            // 序列 4——半神，原版毕业线
            if (item.type == ModContent.ItemType<UnshadowedPotion>() || item.type == ModContent.ItemType<BizarroSorcererPotion>() ||
                item.type == ModContent.ItemType<IronBloodedKnightPotion>() || item.type == ModContent.ItemType<WitchKingPotion>() ||
                item.type == ModContent.ItemType<ParasitePotion>() || item.type == ModContent.ItemType<DespairDemonessPotion>() ||
                item.type == ModContent.ItemType<MisfortuneMagePotion>() || item.type == ModContent.ItemType<DemonHunterPotion>() ||
                item.type == ModContent.ItemType<SecretsSorcererPotion>() ||
                item.type == ModContent.ItemType<FallenEarlPotion>()) return 4;

            // 序列 3——圣者，灾厄上路线起点
            if (item.type == ModContent.ItemType<JusticeMentorPotion>() || item.type == ModContent.ItemType<ScholarOfYorePotion>() ||
                item.type == ModContent.ItemType<WarBishopPotion>() || item.type == ModContent.ItemType<SummoningMasterPotion>() ||
                item.type == ModContent.ItemType<MentorPotion>() || item.type == ModContent.ItemType<UnagingDemonessPotion>() ||
                item.type == ModContent.ItemType<AnomalyPotion>() || item.type == ModContent.ItemType<SilverKnightPotion>() ||
                item.type == ModContent.ItemType<WandererPotion>() ||
                item.type == ModContent.ItemType<RageMagePotion>()) return 3;

            // 序列 2——天使
            if (item.type == ModContent.ItemType<LightSeekerPotion>() || item.type == ModContent.ItemType<MiracleInvokerPotion>() ||
                item.type == ModContent.ItemType<WeatherWarlockPotion>() || item.type == ModContent.ItemType<LifeGiverPotion>() ||
                item.type == ModContent.ItemType<TrojanHorsePotion>() || item.type == ModContent.ItemType<CatastropheDemonessPotion>() ||
                item.type == ModContent.ItemType<ProphetPotion>() || item.type == ModContent.ItemType<GloryPotion>() ||
                item.type == ModContent.ItemType<PlaneswalkerPotion>() ||
                item.type == ModContent.ItemType<EntropyDukePotion>()) return 2;

            // 序列 1——天使之王
            if (item.type == ModContent.ItemType<WhiteAngelPotion>() || item.type == ModContent.ItemType<AttendantPotion>() ||
                item.type == ModContent.ItemType<ConquerorPotion>() || item.type == ModContent.ItemType<BeautyGoddessPotion>() ||
                item.type == ModContent.ItemType<WormOfTimePotion>() || item.type == ModContent.ItemType<ApocalypseDemonessPotion>() ||
                item.type == ModContent.ItemType<SerpentPotion>() || item.type == ModContent.ItemType<HandOfGodPotion>() ||
                item.type == ModContent.ItemType<KeyOfStarsPotion>() ||
                item.type == ModContent.ItemType<UsurperPrincePotion>()) return 1;

            return -1;
        }

        /// <summary>灾厄 GetBossDowned 的名字写错时只返回 false，不会把玩家永久卡在门外。</summary>
        private static bool CalamityDowned(Mod calamity, string bossName)
        {
            if (calamity == null) return false;
            try { return calamity.Call("GetBossDowned", bossName) is bool downed && downed; }
            catch { return false; }
        }

        /// <summary>该序列要求的进度；返回空字符串表示这一档没有门槛。</summary>
        private static string RequirementText(int sequence, Mod calamity)
        {
            switch (sequence)
            {
                case 9: return "";
                case 8:
                    return !NPC.downedBoss1 && !CalamityDowned(calamity, "desertscourge")
                        ? "克苏鲁之眼 或 荒漠灾虫" : "";
                case 7:
                    // 灾厄的腐巢意志/血肉宿主可以替代原版世吞/克脑。
                    return !NPC.downedBoss2 && !CalamityDowned(calamity, "hivemind") && !CalamityDowned(calamity, "perforators")
                        ? "世界吞噬者/克苏鲁之脑 或 腐巢意志/血肉宿主" : "";
                case 6:
                    return NPC.downedBoss3 ? "" : "骷髅王";
                case 5:
                    return Main.hardMode ? "" : "血肉墙";
                case 4:
                    return NPC.downedMoonlord ? "" : "月球领主";
                case 3:
                    if (calamity != null) return CalamityDowned(calamity, "providence") ? "" : "亵渎天神";
                    return NPC.downedMoonlord ? "" : "月球领主";
                case 2:
                    if (calamity != null) return CalamityDowned(calamity, "devourerofgods") ? "" : "神之吞噬者";
                    return NPC.downedMoonlord ? "" : "月球领主";
                case 1:
                    if (calamity != null) return CalamityDowned(calamity, "yharon") ? "" : "丛林龙，犽戎";
                    return NPC.downedMoonlord ? "" : "月球领主";
                default: return "";
            }
        }

        public override bool CanUseItem(Item item, Player player)
        {
            if (!ModContent.GetInstance<LotMConfig>().CalamityAdaptationMode)
                return base.CanUseItem(item, player);

            int sequence = SequenceForPotion(item);
            if (sequence == -1) return base.CanUseItem(item, player);

            ModLoader.TryGetMod("CalamityMod", out Mod calamity);
            string missingBoss = RequirementText(sequence, calamity);
            if (missingBoss != "")
            {
                Main.NewText($"[灾厄平衡] 灵性受阻！晋升序列 {sequence} 过于强大，需先击败：{missingBoss}", 255, 100, 100);
                return false;
            }
            return base.CanUseItem(item, player);
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (!ModContent.GetInstance<LotMConfig>().CalamityAdaptationMode) return;
            int sequence = SequenceForPotion(item);
            if (sequence == -1) return;

            ModLoader.TryGetMod("CalamityMod", out Mod calamity);
            string missingBoss = RequirementText(sequence, calamity);
            tooltips.Add(new TooltipLine(Mod, "CalamityLock", missingBoss == ""
                ? $"灾厄适配：已达序列 {sequence} 的进度要求"
                : $"灾厄适配：需先击败 {missingBoss}"));
        }
    }
}
