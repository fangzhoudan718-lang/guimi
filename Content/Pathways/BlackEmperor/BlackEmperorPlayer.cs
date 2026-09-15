using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using zhashi.Content.Buffs;
using zhashi.Content.Buffs.BlackEmperor;
using zhashi.Content.Projectiles.BlackEmperor;
using zhashi.Content.Systems;

namespace zhashi.Content.Pathways.BlackEmperor
{
    /// <summary>黑皇帝的「律令」：同一时间只能维持一条，守住有奖励，违约会反噬。</summary>
    public enum BlackEmperorLaw
    {
        None = 0,
        /// <summary>禁足：12 秒内位移不超过 300 px，期间近战攻击被格挡并反弹。</summary>
        StayPut = 1,
        /// <summary>禁止接近：半径 220 内敌人被推开并持续受伤。</summary>
        NoApproach = 2,
        /// <summary>审判：标记被告，15 秒内命中五次即定罪。</summary>
        Trial = 3,
        /// <summary>等价交换：造成伤害的一部分转为治疗，代价是受到伤害提高。</summary>
        EquivalentExchange = 4,
        /// <summary>秩序倾覆：禁止贿赂，但攻击附带目标减益层数的额外伤害。</summary>
        OrderOverthrow = 5
    }

    /// <summary>堕落伯爵「赠予」的三种负面状态。</summary>
    public enum BlackEmperorGiftMode { Slack = 0, Greed = 1, Despair = 2 }

    /// <summary>堕落伯爵「放大」的两种用法，一次只能挂一种。</summary>
    public enum BlackEmperorAmplifyMode { Execute = 0, Bind = 1 }


    /// <summary>弑序亲王的「定义」：把什么东西定义成什么，秩序就照着你的说法走。</summary>
    public enum BlackEmperorDefinition
    {
        Enemy = 0,      // 敌人：它就是敌人，你打它更疼
        Bribe = 1,      // 贿赂：你的每一次攻击都被当成一笔贿赂
        Corrupt = 2,    // 腐败者：被你碰过的人都成了腐败者
        StandIn = 3     // 替身：该死的那一下由替身承受
    }

    /// <summary>
     /// 黑皇帝的主动技能标识。客户端按下按键以后只负责说「我想做什么」，
    /// 真正的世界改动（弹幕归属、敌人状态）交给服务器裁定。
    /// 后续序列的技能往这里追加即可，不必再开新的网络包类型。
    /// </summary>
    public enum BlackEmperorAbility
    {
        /// <summary>序列六 腐化男爵 · 扭曲：改写弹幕的归属，也改写敌人的意图。</summary>
        Twist = 0,
        /// <summary>序列五 混乱导师 · 混乱场：在目光所及之处按下自己的秩序。</summary>
        ChaosField = 1,
        /// <summary>序列五 混乱导师 · 扭曲概念：让目标在概念上替你分担。</summary>
        TwistConcept = 2,
        /// <summary>序列四 堕落伯爵 · 赠予：把一种负面状态直接送给目标。</summary>
        Gift = 3,
        /// <summary>序列四 堕落伯爵 · 放大：让下一次命中的分量被放大一次。</summary>
        Amplify = 4,
        /// <summary>序列三 狂乱法师 · 狂乱：一阵谁也无法预测结果的波动。</summary>
        Rage = 5,
        /// <summary>黑皇帝 · 名义：戴上某个称号后，用它附带的主动能力。</summary>
        TitleSkill = 6,
        /// <summary>序列二 熵之公爵 · 兑现：把攒下的熵一次掷出去。</summary>
        Entropy = 7
    }

    /// <summary>
    /// 黑皇帝途径（律师 → 弑序亲王）。
    ///
    /// 这条途径的核心不是数值，而是「动词」：契约（给自己立规矩）、贿赂（花钱买通）、扭曲与定义（改写含义）。
    /// 本文件目前实现序列九「律师」的契约系统与被动，后续序列在此基础上追加。
    /// </summary>
    public class BlackEmperorPlayer : ModPlayer
    {
        public const int LawDuration = 12 * 60;
        /// <summary>律令冷却：与其它技能统一压到 6 秒。</summary>
        public const int LawCooldown = 6 * 60;
        // 灵性消耗随序列递增：序列九只要 50，越往上越贵。
        // 各类技能在这个基数上再乘一个系数（贿赂还要额外付真钱）。
        /// <summary>本序列使用一次技能的基础灵性消耗（未计牌折扣）。</summary>
        public float BaseSkillCost => BlackEmperorMath.SequenceCost(Sequence);
        /// <summary>契约宣告的实际消耗。</summary>
        public float LawCost => AdjustedCost(BaseSkillCost);
        /// <summary>贿赂的实际消耗；额外还要付真钱。</summary>
        public float BribeCost => AdjustedCost(BaseSkillCost * (HasTitle(BlackEmperorTitle.Gambler) ? 0.56f : 0.8f));
        /// <summary>扭曲与定义的实际消耗。</summary>
        public float TwistCost => AdjustedCost(BaseSkillCost * 1.5f);
        /// <summary>混乱场的实际消耗。</summary>
        public float ChaosCost => AdjustedCost(BaseSkillCost * 1.5f);
        /// <summary>赠予的实际消耗。</summary>
        public float GiftCost => AdjustedCost(BaseSkillCost);
        /// <summary>放大的实际消耗。</summary>
        public float AmplifyCost => AdjustedCost(BaseSkillCost * 1.5f);
        /// <summary>狂乱的实际消耗。</summary>
        public float RageCost => AdjustedCost(BaseSkillCost * 1.5f);
        /// <summary>熵兑现的实际消耗。</summary>
        public float EntropyCost => AdjustedCost(BaseSkillCost * 3f);
        /// <summary>禁足的位移上限。</summary>
        public const float StayPutRadius = 300f;
        /// <summary>禁止接近的作用半径。</summary>
        public const float NoApproachRadius = 220f;

        // ── 契约状态 ──────────────────────────────────────────────
        public BlackEmperorLaw activeLaw;
        public int lawTimer;
        public int lawCooldown;
        public Vector2 lawAnchor;
        /// <summary>Shift+契约键时用来循环选择律令。</summary>
        public int lawChoice;

        // ── 审判律令的进度 ────────────────────────────────────────
        public int trialTarget = -1;
        public int trialHits;

        // ── 序列八 野蛮人 ─────────────────────────────────────────
        /// <summary>以力破法：可以无视一次契约反噬。</summary>
        public bool lawBreakerReady;
        /// <summary>以力破法：用重击命中敌人的次数，攒够三次换来一次免疫反噬。</summary>
        public int lawBreakerCharge;
        public int bruteCooldown;
        public int unstoppableCooldown;
        public int unstoppableTimer;
        public const int LawBreakerChargeRequired = 3;
        /// <summary>本途径统一的技能冷却：6 秒。</summary>
        public const int SkillCooldown = 6 * 60;

        // ── 序列七 贿赂者 ─────────────────────────────────────────
        public int bribeMode;
        public int bribeCooldown;
        /// <summary>关联模式：当前替玩家分担伤害的目标。</summary>
        public int linkedNpc = -1;
        /// <summary>向城镇 NPC 行贿换来的庇护剩余时间。</summary>
        public int townFavorTimer;

        // ── 序列六 腐化男爵 ───────────────────────────────────────
        /// <summary>扭曲的冷却。</summary>
        public int twistCooldown;
        /// <summary>腐蚀光环的作用半径：身边二十格左右的人，心里会一点点长出阴影。</summary>
        public const float CorruptionRadius = 320f;
        /// <summary>腐蚀光环的节奏：每半秒叠一层。</summary>
        public const int CorruptionInterval = 30;
        /// <summary>扭曲的作用半径。</summary>
        public const float TwistRadius = 480f;
        /// <summary>被扭曲的意图能维持多久。</summary>
        public const int TwistDuration = 6 * 60;
        private int corruptionTick;
        private int corruptionBeat;

        // ── 序列五 混乱导师 ───────────────────────────────────────
        /// <summary>混乱场的冷却。</summary>
        public int chaosCooldown;
        /// <summary>混乱场是否展开中。</summary>
        public bool chaosFieldActive;
        public int chaosFieldTimer;
        public Vector2 chaosFieldCenter;
        /// <summary>混乱场的半径：与画面里那块圆盘严格一致，站在其中连「距离」都会失去意义。</summary>
        public const float ChaosFieldRadius = 320f;
        public const int ChaosFieldDuration = 10 * 60;
        /// <summary>混乱场每隔半秒拨动一次场内的事物。</summary>
        public const int ChaosFieldPulse = 30;
        /// <summary>站在混乱场里被完全错判（免伤）的概率。</summary>
        public const float ChaosEvadeChance = 0.35f;
        private int chaosPulse;

        /// <summary>扭曲概念：被概念绑住、替你分担伤害与诅咒的目标。</summary>
        public int conceptTarget = -1;
        public int conceptTicks;
        public const int ConceptDuration = 8 * 60;
        private int conceptCopyCooldown;

        // ── 序列四 堕落伯爵 ───────────────────────────────────────
        /// <summary>赠予当前选中的负面状态。</summary>
        public int giftMode;
        public int giftCooldown;
        /// <summary>放大当前选中的用法。</summary>
        public int amplifyMode;
        public int amplifyCooldown;
        /// <summary>已经放大过一次，等着下一次命中把它用掉。</summary>
        public bool amplifyReady;
        public const int GiftDuration = 10 * 60;

        /// <summary>利用·滞空：把「离开大地」这个状态延长一会儿。</summary>
        public int hoverTicks;
        public int hoverCooldown;
        public const int HoverMaxTicks = 3 * 60;
        /// <summary>「利用」给增益续命时的上限，免得刷出无穷长的状态。</summary>
        public const int MaxExploitBuffTime = 60 * 60 * 24;

        /// <summary>规则领域：律令立下之处，规矩会被改写成有利于你的样子。</summary>
        public const float RuleDomainRadius = 300f;

        // ── 亵渎之牌 · 秩序威压 ─────────────────────────────────
        /// <summary>牌中神性向持有者周围投下的秩序范围。</summary>
        public const float CardOrderPressureRadius = 360f;
        /// <summary>秩序威压对近身目标的最终伤害增幅。</summary>
        public const float CardOrderPressureDamageBonus = 0.12f;
        /// <summary>秩序威压为持有者改写承伤规则的比例。</summary>
        public const float CardOrderPressureDamageReduction = 0.10f;

        /// <summary>
        /// 只有真正踏上黑皇帝途径的人能读取牌中的权柄。
        /// 此值来自每 tick 的饰品状态，不保存也不单独同步；服务端会按装备重新计算。
        /// </summary>
        public bool CardOrderPressureActive
            => Sequence <= 9 && Player.GetModPlayer<LotMPlayer>().isBlackEmperorCardEquipped;

        // ── 晋升仪式：一个国家的中高层（序列五 → 四）────────────
        /// <summary>已经拉拢过的城镇 NPC 种类。</summary>
        public List<int> corruptedTownTypes = new();
        /// <summary>是否已经在城里推行过一项政策（把一条律令在城中走完）。</summary>
        public bool policyImplemented;
        public const int CorruptedTownTarget = 7;
        /// <summary>拉拢过的人数：只用于同步与界面显示，名单本身留在服务器上。</summary>
        public int corruptedTownCount;
        public int CorruptedTownCount => corruptedTownTypes.Count > corruptedTownCount ? corruptedTownTypes.Count : corruptedTownCount;


        // ── 晋升仪式：城市地底的秩序（序列六 → 五）───────────────
        /// <summary>已经压住的地底之夜数。</summary>
        public int chaosRitualNights;
        /// <summary>当晚在城市地底杀掉的敌人数。</summary>
        public int chaosRitualKills;
        /// <summary>这一夜是否已经在记账。</summary>
        public bool chaosRitualNightActive;
        /// <summary>仪式是否已经完成。</summary>
        public bool chaosRitualComplete;
        public const int ChaosRitualNightTarget = 3;
        public const int ChaosRitualKillsPerNight = 50;
        public const int ChaosRitualTownNpcTarget = 3;
        /// <summary>身边有多少位居民（给界面看的）。</summary>
        public int NearbyTownCount => CountNearbyTownNpcs(600f);
        /// <summary>整个世界里有多少位居民（给界面看的）。</summary>
        public int TownNpcCountNow => CountTownNpcs();
        /// <summary>此刻是否在洞穴层或更深处。</summary>
        public bool InDeepCavern => Player.ZoneRockLayerHeight || Player.ZoneUnderworldHeight;
        /// <summary>此刻手里是否空着。</summary>
        public bool HandsEmpty => Player.HeldItem.IsAir;

        // ── 序列三 狂乱法师 ───────────────────────────────────────
        /// <summary>狂乱的冷却。</summary>
        public int rageCooldown;
        /// <summary>狂乱波的半径。</summary>
        public const float RageRadius = 420f;
        /// <summary>狂乱给双方挂上的状态持续时间。</summary>
        public const int RageDuration = 12 * 60;

        // ── 晋升仪式：三场被改写过的仪式（序列四 → 三）──────────
        /// <summary>已经打赢过的 Boss 种类。</summary>
        public List<int> rageRitualBosses = new();
        /// <summary>同步用的已打赢场次。</summary>
        public int rageRitualCount;
        /// <summary>这一场里已经放过狂乱。</summary>
        public bool rageRitualArmed;
        /// <summary>这一场里死过，账就作废。</summary>
        public bool rageRitualFailed;
        public const int RageRitualTarget = 3;

        public int RageRitualProgress => rageRitualBosses.Count > rageRitualCount ? rageRitualBosses.Count : rageRitualCount;
        public bool RageRitualComplete => RageRitualProgress >= RageRitualTarget;

        /// <summary>当前戴着的称号带来的被动。</summary>
        private void ApplyTitleEffects()
        {
            bool underground = Player.ZoneRockLayerHeight || Player.ZoneDirtLayerHeight;
            switch (Worn)
            {
                case BlackEmperorTitle.MushroomKing:
                    Player.statLifeMax2 += 20;
                    Player.endurance += 0.06f;
                    break;
                case BlackEmperorTitle.SeaKing:
                    Player.ignoreWater = true;
                    if (Player.wet || Player.ZoneBeach)
                    {
                        Player.moveSpeed += 0.30f;
                        Player.GetDamage(DamageClass.Generic) += 0.20f;
                    }
                    break;
                case BlackEmperorTitle.DragonSlayer:
                    Player.GetDamage(DamageClass.Generic) += 0.10f;
                    Player.noKnockback = true;
                    break;
                case BlackEmperorTitle.Delver:
                    Player.pickSpeed -= 0.25f;
                    Player.statDefense += 6;
                    break;
                case BlackEmperorTitle.Tyrant:
                    Player.GetDamage(DamageClass.Generic) += 0.15f;
                    Player.GetCritChance(DamageClass.Generic) += 8f;
                    break;
                case BlackEmperorTitle.Conqueror:
                    Player.GetDamage(DamageClass.Generic) += 0.10f;
                    Player.moveSpeed += 0.15f;
                    Player.noKnockback = true;
                    break;
                case BlackEmperorTitle.Godslayer:
                    Player.GetDamage(DamageClass.Generic) += 0.12f;
                    Player.statDefense += 10;
                    Player.endurance += 0.08f;
                    break;
                case BlackEmperorTitle.Legislator:
                    Player.statDefense += 6;
                    break;
                case BlackEmperorTitle.Gambler:
                    Player.luck += 0.3f;
                    break;
                case BlackEmperorTitle.Abyssal:
                    Player.statDefense += underground ? 8 : 0;
                    if (underground)
                    {
                        Player.GetDamage(DamageClass.Generic) += 0.15f;
                        Player.buffImmune[BuffID.Blackout] = true;
                        Player.buffImmune[BuffID.Darkness] = true;
                    }
                    break;
                case BlackEmperorTitle.KingSlayer:
                    // 猎王者：有 Boss 在场时更抗打（打得重的那一半在 ModifyHitNPC 里）。
                    foreach (NPC boss in Main.ActiveNPCs)
                    {
                        if (!boss.active || !BlackEmperorMath.IsBoss(boss)) continue;
                        if (boss.Distance(Player.Center) > 2400f) continue;
                        Player.endurance += 0.10f;
                        break;
                    }
                    break;
                default:
                    Player.GetDamage(DamageClass.Generic) += 0.03f;
                    break;
            }
        }

        /// <summary>蘑菇王：周身不断浮起带伤害的孢子。</summary>
        private void UpdateSporeAura()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (Worn != BlackEmperorTitle.MushroomKing) return;

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2Circular(72f, 56f),
                    DustID.GlowingMushroom, Main.rand.NextVector2Circular(0.6f, 0.6f), 120, default, 0.9f);
                dust.noGravity = true;
            }

            if (++sporeTick < 30) return;
            sporeTick = 0;

            int damage = BlackEmperorMath.ScaledDamage(45, Sequence);
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!CanAffectEnemy(npc)) continue;
                if (npc.Distance(Player.Center) > SporeRadius) continue;

                npc.SimpleStrikeNPC(damage, npc.Center.X >= Player.Center.X ? 1 : -1, false, 0f, DamageClass.Generic, false);
                npc.AddBuff(BuffID.Slow, 2 * 60);
            }
        }

        /// <summary>称号附带的主动能力；不是每个称号都有。</summary>
        private void ExecuteTitleSkill(Vector2 aimPoint)
        {
            if (!TitleSystemActive) { Feedback("【名义】只有黑皇帝途径的非凡者才戴得住称号。"); return; }
            if (!BlackEmperorTitles.HasActive(Worn))
            {
                Feedback($"【名义】《{BlackEmperorTitles.Name(Worn)}》没有主动能力，换一个称号试试。");
                return;
            }
            if (titleSkillCooldown > 0) { Feedback($"【{BlackEmperorTitles.ActiveName(Worn)}】尚在冷却：{titleSkillCooldown / 60f:F1} 秒。"); return; }

            float cost = AdjustedCost(BaseSkillCost);
            if (!Player.GetModPlayer<LotMPlayer>().TryConsumeSpirituality(cost, true))
            {
                Feedback($"【{BlackEmperorTitles.ActiveName(Worn)}】灵性不足，需要 {cost:F0} 点。");
                return;
            }
            titleSkillCooldown = TitleSkillCooldown;

            Vector2 aim = (aimPoint - Player.Center).SafeNormalize(Vector2.UnitX);
            int affected = 0;
            int damage = BlackEmperorMath.ScaledDamage(220, Sequence);
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!CanAffectEnemy(npc)) continue;
                if (npc.Distance(Player.Center) > TitleSkillRadius) continue;

                var state = npc.GetGlobalNPC<BlackEmperorGlobalNPC>();
                switch (Worn)
                {
                    case BlackEmperorTitle.MushroomKing:
                        npc.SimpleStrikeNPC(damage, npc.Center.X >= Player.Center.X ? 1 : -1, false, 2f, DamageClass.Generic, false);
                        npc.AddBuff(BuffID.Poisoned, 8 * 60);
                        npc.AddBuff(BuffID.Slow, 8 * 60);
                        break;
                    case BlackEmperorTitle.SeaKing:
                        // 潮汐：把周围的东西一起推开。
                        float tidePush = BlackEmperorMath.IsBoss(npc) ? 2.5f : 9f;
                        npc.velocity += (npc.Center - Player.Center).SafeNormalize(aim) * tidePush;
                        npc.AddBuff(BuffID.Wet, 10 * 60);
                        break;
                    case BlackEmperorTitle.DragonSlayer:
                        // 龙威：让它们不敢靠近（沿用「丧失斗志」的那套行为）。
                        state.despairTicks = Math.Max(state.despairTicks, 8 * 60);
                        break;
                    case BlackEmperorTitle.Tyrant:
                        // 威压：原地定住。
                        state.boundTicks = Math.Max(state.boundTicks, BlackEmperorGlobalNPC.BoundDuration * 2);
                        break;
                    case BlackEmperorTitle.Godslayer:
                        npc.SimpleStrikeNPC(damage * 2, npc.Center.X >= Player.Center.X ? 1 : -1, false, 4f, DamageClass.Generic, true);
                        break;
                }
                npc.netUpdate = true;
                affected++;
            }

            if (!Main.dedServ)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.5f, Pitch = 0.2f }, Player.Center);
                for (int i = 0; i < 18; i++)
                {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(TitleSkillRadius, TitleSkillRadius);
                    Dust dust = Dust.NewDustPerfect(Player.Center + offset * 0.6f, DustID.PurpleCrystalShard,
                        offset.SafeNormalize(Vector2.UnitX) * 3f, 120, default, 1.1f);
                    dust.noGravity = true;
                }
            }

            Announce($"【{BlackEmperorTitles.ActiveName(Worn)}】{affected} 个目标被你的名义压住了。");
        }

        // ── 序列二 熵之公爵 ───────────────────────────────────────
        /// <summary>熵层：交战中每三秒叠一层，叠得越高越强，也越难收场。</summary>
        public int entropyStacks;
        public int entropyCooldown;
        public const int EntropyMaxStacks = 15;
        public const int EntropyGainInterval = 3 * 60;
        public const float EntropyCombatRadius = 800f;
        /// <summary>兑现时波及的半径。</summary>
        public const float EntropyCashInRadius = 460f;
        /// <summary>兑现之后灵性回复减半的时间。</summary>
        public int entropyBacklashTicks;
        public const int EntropyBacklashDuration = 5 * 60;
        private int entropyTimer;
        /// <summary>空中冲刺的冷却。</summary>
        public int airDashCooldown;

        // 晋升仪式：一个国度的内部崩塌（序列三 → 二）
        /// <summary>是否在城里引爆过一次满层熵。</summary>
        public bool entropyRitualBurst;
        /// <summary>「在城镇里」的判定：游戏自己认的城镇区域，或者身边六十格内有居民。</summary>
        public bool EntropyRitualInTown => Player.townNPCs >= 1f || CountNearbyTownNpcs(960f) >= 1;
        /// <summary>「乱世」的判定：血月、入侵、霜月、南瓜月或日食都算。</summary>
        public static bool EntropyRitualUnrest =>
            Main.bloodMoon || Main.invasionType > 0 || Main.pumpkinMoon || Main.snowMoon || Main.eclipse;
        public bool EntropyRitualSceneReady => EntropyRitualInTown && EntropyRitualUnrest;
        public bool EntropyRitualComplete => entropyRitualBurst;

        /// <summary>身边有多少位居民。</summary>
        private int CountNearbyTownNpcs(float radius)
        {
            int count = 0;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.townNPC || npc.life <= 0) continue;
                if (npc.Distance(Player.Center) > radius) continue;
                count++;
            }
            return count;
        }

        // ── 序列一 弑序亲王 ───────────────────────────────────────
        /// <summary>当前生效的「定义」。</summary>
        public int definitionMode;
        public const int DefinitionCount = 4;
        /// <summary>替身替你挡死之后的冷却。</summary>
        public int standInCooldown;
        public const int StandInCooldown = 90 * 60;
        /// <summary>定义·敌人带来的加成。</summary>
        public float DefinitionDamageBonus => definitionMode == (int)BlackEmperorDefinition.Enemy ? 0.10f : 0f;

        // 晋升仪式：以自身秩序取代原本的秩序（序列二 → 一）
        /// <summary>已经记过的日子数。</summary>
        public int usurpRitualDays;
        /// <summary>上一次记账是在第几个游戏日。</summary>
        public int usurpRitualLastDay = -1;
        /// <summary>从开局算起经过了多少个白天。</summary>
        public int usurpDayIndex;
        private bool usurpWasDay;
        public const int UsurpRitualTarget = 3;
        public const int UsurpRitualTownNpcs = 3;

        public bool UsurpRitualComplete => usurpRitualDays >= UsurpRitualTarget;

        private static readonly int[] CoinTypes =
        {
            ItemID.PlatinumCoin, ItemID.GoldCoin, ItemID.SilverCoin, ItemID.CopperCoin
        };

        private int feedbackCooldown;

        public int Sequence => Player.GetModPlayer<LotMPlayer>().currentBlackEmperorSequence;

        /// <summary>黑皇帝权柄可以触及 Boss；无敌阶段仍能挂规则状态，但不会绕过其伤害免疫。</summary>
        public static bool CanAffectEnemy(NPC npc) => npc != null && npc.active && npc.life > 0 &&
            !npc.friendly && !npc.townNPC && (npc.CanBeChasedBy() || BlackEmperorMath.IsBoss(npc));

        /// <summary>该序列能用的律令；随序列解锁。</summary>
        private BlackEmperorLaw[] AvailableLaws()
        {
            int seq = Sequence;
            if (seq > 9)
            {
                return Array.Empty<BlackEmperorLaw>();
            }
            var laws = new System.Collections.Generic.List<BlackEmperorLaw>(5)
            {
                BlackEmperorLaw.StayPut,
                BlackEmperorLaw.NoApproach
            };
            if (seq <= 6) laws.Add(BlackEmperorLaw.Trial);
            if (seq <= 4) laws.Add(BlackEmperorLaw.EquivalentExchange);
            if (seq <= 3) laws.Add(BlackEmperorLaw.OrderOverthrow);
            return laws.ToArray();
        }

        public static string LawName(BlackEmperorLaw law) => law switch
        {
            BlackEmperorLaw.StayPut => "禁足",
            BlackEmperorLaw.NoApproach => "禁止接近",
            BlackEmperorLaw.Trial => "审判",
            BlackEmperorLaw.EquivalentExchange => "等价交换",
            BlackEmperorLaw.OrderOverthrow => "秩序倾覆",
            _ => "无"
        };

        // ── 名义 / 称号（黑皇帝途径专属）──────────────────────────
        /// <summary>已解锁的称号位图；位 0 是无名者，生来就有。</summary>
        public int titlesMask = 1;
        /// <summary>当前戴着的称号。</summary>
        public int wornTitle;
        /// <summary>称号主动能力的冷却。</summary>
        public int titleSkillCooldown;
        public const int TitleSkillCooldown = 20 * 60;
        /// <summary>蘑菇王孢子光环的作用半径。</summary>
        public const float SporeRadius = 200f;
        /// <summary>称号主动能力的作用半径。</summary>
        public const float TitleSkillRadius = 340f;
        private int sporeTick;
        private int worldTitleTick;
        private int titleSyncTick;

        // 各条称号的进度
        public int mushroomsGathered;
        public int fishCaught;
        public int dragonsSlain;
        public int tilesMined;
        public int bloodMoonKills;
        public int abyssKills;
        public int kingKills;
        public int lawsDeclared;
        public int bribesCast;
        public bool invasionWon;
        public bool moonlordSlain;

        public BlackEmperorTitle Worn => (BlackEmperorTitle)Math.Clamp(wornTitle, 0, BlackEmperorTitles.Count - 1);
        public bool HasTitle(BlackEmperorTitle title) => (titlesMask & (1 << (int)title)) != 0;
        /// <summary>称号属于整条途径：只要还没走出这条路（序列九以内）就戴得住。</summary>
        public bool TitleSystemActive => Sequence <= 9;

        /// <summary>
        /// 解锁一个称号。第一次拿到就自动戴上——玩家做完一件事，
        /// 应该立刻看到那个称呼落到自己头上，而不是还要去翻列表。
        /// </summary>
        private void UnlockTitle(BlackEmperorTitle title)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            int bit = 1 << (int)title;
            if ((titlesMask & bit) != 0) return;

            titlesMask |= bit;
            wornTitle = (int)title;
            SyncLaw();
            Announce($"【名义】你获得了称号《{BlackEmperorTitles.Name(title)}》：{BlackEmperorTitles.Effect(title)}。");
            Announce("【名义】用称号键可以在已解锁的称号之间随时更换。");
        }

        /// <summary>在已解锁的称号之间切换；dir 为 +1 或 -1。</summary>
        public void CycleTitle(int dir)
        {
            if (!TitleSystemActive) { Feedback("【名义】只有黑皇帝途径的非凡者才戴得住称号。"); return; }

            for (int step = 1; step <= BlackEmperorTitles.Count; step++)
            {
                int index = ((wornTitle + dir * step) % BlackEmperorTitles.Count + BlackEmperorTitles.Count) % BlackEmperorTitles.Count;
                if ((titlesMask & (1 << index)) == 0) continue;

                wornTitle = index;
                SyncLaw();
                string active = BlackEmperorTitles.HasActive((BlackEmperorTitle)index)
                    ? $"（Shift + 称号键使用「{BlackEmperorTitles.ActiveName((BlackEmperorTitle)index)}」）"
                    : "";
                Feedback($"【名义】你现在是《{BlackEmperorTitles.Name((BlackEmperorTitle)index)}》{active}");
                return;
            }
            Feedback("【名义】你还没有别的称号可以换。");
        }

        // ── 称号进度 ─────────────────────────────────────────────

        public void CountMushroomCollected(int amount = 1)
        {
            if (!TitleSystemActive || HasTitle(BlackEmperorTitle.MushroomKing)) return;
            mushroomsGathered += amount;
            if (mushroomsGathered >= BlackEmperorTitles.MushroomTarget) UnlockTitle(BlackEmperorTitle.MushroomKing);
        }

        public void CountFishCaught(int amount = 1)
        {
            if (!TitleSystemActive || HasTitle(BlackEmperorTitle.SeaKing)) return;
            fishCaught += amount;
            if (fishCaught >= BlackEmperorTitles.FishTarget) UnlockTitle(BlackEmperorTitle.SeaKing);
        }

        public void CountTileMined()
        {
            if (!TitleSystemActive || HasTitle(BlackEmperorTitle.Delver)) return;
            tilesMined++;
            if (tilesMined >= BlackEmperorTitles.DelveTarget) UnlockTitle(BlackEmperorTitle.Delver);
        }

        public void CountDragonSlain()
        {
            if (!TitleSystemActive || HasTitle(BlackEmperorTitle.DragonSlayer)) return;
            dragonsSlain++;
            if (dragonsSlain >= BlackEmperorTitles.DragonTarget) UnlockTitle(BlackEmperorTitle.DragonSlayer);
        }

        public void CountBloodMoonKill()
        {
            if (!TitleSystemActive || HasTitle(BlackEmperorTitle.Tyrant) || !Main.bloodMoon) return;
            bloodMoonKills++;
            if (bloodMoonKills >= BlackEmperorTitles.BloodMoonTarget) UnlockTitle(BlackEmperorTitle.Tyrant);
        }

        public void CountAbyssKill()
        {
            if (!TitleSystemActive || HasTitle(BlackEmperorTitle.Abyssal)) return;
            abyssKills++;
            if (abyssKills >= BlackEmperorTitles.AbyssTarget) UnlockTitle(BlackEmperorTitle.Abyssal);
        }

        public void CountKingKill()
        {
            if (!TitleSystemActive || HasTitle(BlackEmperorTitle.KingSlayer)) return;
            kingKills++;
            if (kingKills >= BlackEmperorTitles.KingTarget) UnlockTitle(BlackEmperorTitle.KingSlayer);
        }

        public void CountLawDeclared()
        {
            if (!TitleSystemActive || HasTitle(BlackEmperorTitle.Legislator)) return;
            lawsDeclared++;
            if (lawsDeclared >= BlackEmperorTitles.LawTarget) UnlockTitle(BlackEmperorTitle.Legislator);
        }

        public void CountBribeCast()
        {
            if (!TitleSystemActive || HasTitle(BlackEmperorTitle.Gambler)) return;
            bribesCast++;
            if (bribesCast >= BlackEmperorTitles.BribeTarget) UnlockTitle(BlackEmperorTitle.Gambler);
        }

        /// <summary>有些称号不用计数：世界的状态到了，名义就成立。</summary>
        private void CheckWorldTitles()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !TitleSystemActive) return;

            if (!moonlordSlain && NPC.downedMoonlord)
            {
                moonlordSlain = true;
                UnlockTitle(BlackEmperorTitle.Godslayer);
            }
            if (!invasionWon && (NPC.downedGoblins || NPC.downedPirates || NPC.downedMartians))
            {
                invasionWon = true;
                UnlockTitle(BlackEmperorTitle.Conqueror);
            }
        }

        /// <summary>当前称号的进度，给界面用。</summary>
        public string TitleProgressText()
        {
            return Worn switch
            {
                BlackEmperorTitle.MushroomKing => $"{mushroomsGathered}/{BlackEmperorTitles.MushroomTarget}",
                BlackEmperorTitle.SeaKing => $"{fishCaught}/{BlackEmperorTitles.FishTarget}",
                BlackEmperorTitle.DragonSlayer => $"{dragonsSlain}/{BlackEmperorTitles.DragonTarget}",
                BlackEmperorTitle.Delver => $"{tilesMined}/{BlackEmperorTitles.DelveTarget}",
                BlackEmperorTitle.Tyrant => $"{bloodMoonKills}/{BlackEmperorTitles.BloodMoonTarget}",
                BlackEmperorTitle.Abyssal => $"{abyssKills}/{BlackEmperorTitles.AbyssTarget}",
                BlackEmperorTitle.KingSlayer => $"{kingKills}/{BlackEmperorTitles.KingTarget}",
                BlackEmperorTitle.Legislator => $"{lawsDeclared}/{BlackEmperorTitles.LawTarget}",
                BlackEmperorTitle.Gambler => $"{bribesCast}/{BlackEmperorTitles.BribeTarget}",
                _ => ""
            };
        }

        public override void ResetEffects()
        {
            // 契约状态本身跨 tick 保留，这里不做清空。
        }

        public override void PostUpdateEquips()
        {
            if (Sequence > 9) return;
            // 世界的位格决定这份力量能发挥多少：动态世界等级 ×（位格压制的阶段上限）。
            float world = BalanceSystem.GetEffectiveWorldMultiplier();

            // 被动·善辩：存在感被口才冲淡，敌人更不容易盯上你。
            Player.aggro -= 600;

            // 被动·体魄（序列八）：可怕的力量与出类拔萃的精神抵抗。
            if (Sequence <= 8)
            {
                Player.statLifeMax2 += (int)(40 * world);
                Player.statDefense += (int)(6 * world);
                Player.buffImmune[BuffID.Confused] = true;
            }

            // 主动·硬闯（序列八）：三秒内不被打断、不被控住。
            if (unstoppableTimer > 0)
            {
                Player.noKnockback = true;
                Player.moveSpeed += 0.25f;
                Player.buffImmune[BuffID.Frozen] = true;
                Player.buffImmune[BuffID.Webbed] = true;
                Player.buffImmune[BuffID.Slow] = true;
            }

            // 贿赂城镇 NPC 换来的庇护：一段时间的运气与减伤。
            if (townFavorTimer > 0)
            {
                Player.luck += 0.5f;
                Player.endurance += 0.08f;
            }

            // 被动·身体（序列六）：秩序与阴影同时沉淀进身体，再结实一层。
            if (Sequence <= 6)
            {
                Player.statLifeMax2 += (int)(25 * world);
                Player.statDefense += (int)(3 * world);
                Player.endurance += 0.04f;
            }

            // 被动·威严（序列五）：混乱导师开口之前，周围的生灵已经先矮了一头。
            if (Sequence <= 5)
            {
                Player.statLifeMax2 += (int)(30 * world);
                Player.statDefense += (int)(4 * world);
                Player.endurance += 0.05f;
                Player.aggro -= 400;
            }

            // 被动·半神之躯（序列四）：规则在身上留下的痕迹也变浅了。
            if (Sequence <= 4)
            {
                Player.statLifeMax2 += (int)(40 * world);
                Player.statDefense += (int)(6 * world);
                Player.endurance += 0.05f;
                Player.noFallDmg = true;
            }
            // 被动·名义（序列三）：戴上哪张面具，就吃哪一份威严。
            // 名义属于整条途径：哪怕还在序列九，称号也照样生效。
            if (TitleSystemActive) ApplyTitleEffects();

            // 走到序列三之后，半神之躯再厚一层。
            if (Sequence <= 3)
            {
                Player.statLifeMax2 += (int)(40 * world);
                Player.statDefense += (int)(6 * world);
            }

            // 序列二：熵本身就在给你加成，也把重力这条规则拧松一点。
            if (Sequence <= 2)
            {
                Player.statLifeMax2 += (int)(40 * world);
                Player.statDefense += (int)(8 * world);
                Player.endurance += 0.05f;
                Player.gravity *= 0.75f;
                Player.maxFallSpeed *= 1.25f;
                Player.jumpSpeedBoost += 1f;
                if (entropyStacks > 0)
                {
                    Player.GetDamage(DamageClass.Generic) += 0.01f * entropyStacks;
                    Player.statDefense += entropyStacks;
                }
            }

            // 序列一：弑序亲王。
            if (Sequence <= 1)
            {
                Player.statLifeMax2 += (int)(50 * world);
                Player.statDefense += (int)(10 * world);
                Player.endurance += 0.05f;
                Player.GetDamage(DamageClass.Generic) += DefinitionDamageBonus;

                // 律令还在，就没有什么能按住你。
                if (activeLaw != BlackEmperorLaw.None && lawTimer > 0)
                {
                    Player.buffImmune[BuffID.Frozen] = true;
                    Player.buffImmune[BuffID.Webbed] = true;
                    Player.buffImmune[BuffID.Slow] = true;
                    Player.buffImmune[BuffID.Confused] = true;
                    Player.buffImmune[BuffID.Stoned] = true;
                }
            }
        }

        public override void PostUpdate()
        {
            if (feedbackCooldown > 0) feedbackCooldown--;
            if (lawCooldown > 0) lawCooldown--;
            if (bruteCooldown > 0) bruteCooldown--;
            if (unstoppableCooldown > 0) unstoppableCooldown--;
            if (bribeCooldown > 0) bribeCooldown--;
            if (twistCooldown > 0) twistCooldown--;
            if (chaosCooldown > 0) chaosCooldown--;
            if (conceptCopyCooldown > 0) conceptCopyCooldown--;
            if (giftCooldown > 0) giftCooldown--;
            if (amplifyCooldown > 0) amplifyCooldown--;
            if (hoverCooldown > 0) hoverCooldown--;
            if (rageCooldown > 0) rageCooldown--;
            if (titleSkillCooldown > 0) titleSkillCooldown--;
            if (entropyCooldown > 0) entropyCooldown--;
            if (airDashCooldown > 0) airDashCooldown--;
            if (standInCooldown > 0) standInCooldown--;

            UpdateStandIn();

            // 兑现之后的空虚：灵性回复减半（LotMPlayer 在算回复时会看这个开关）。
            Player.GetModPlayer<LotMPlayer>().spiritualityRegenHalved = entropyBacklashTicks > 0;
            if (unstoppableTimer > 0) unstoppableTimer--;
            if (townFavorTimer > 0) townFavorTimer--;

            if (Sequence > 9)
            {
                if (activeLaw != BlackEmperorLaw.None) EndLaw(false);
                return;
            }

            // 被动·腐蚀（序列六）：不用开口，身边的人自己会慢慢变得阴暗。
            if (Sequence <= 6) UpdateCorruptionAura();
            else corruptionTick = corruptionBeat = 0;

            // 主动·混乱场（序列五）：往地上按一块「距离与敌我都不作数」的地方。
            if (Sequence <= 5) UpdateChaosField();
            else chaosFieldActive = false;

            // 主动·扭曲概念（序列五）：被概念绑住的目标替你分担伤害与诅咒。
            if (conceptTicks > 0)
            {
                conceptTicks--;
                UpdateConceptLink();
            }
            else conceptTarget = -1;

            // 被动·利用（序列四）：规则里没写不能这样做，那就这样做。
            if (Sequence <= 4) UpdateRuleExploit();

            // 熵从序列三就开始在意了：晋升仪式要的就是在城里引爆一次满层的熵，
            // 所以叠层与兑现都必须在晋升之前就能用。极致利用才是序列二自己的东西。
            if (Sequence <= 3) UpdateEntropy();
            else entropyStacks = entropyTimer = 0;
            if (Sequence <= 2) UpdateAirDash();

            // 被动·规则领域（序列四）：律令落下的地方，规矩由你改写。
            UpdateRuleDomain();

            // 序列三的仪式：这一场里死过就作废。
            if (Player.dead && rageRitualArmed)
            {
                rageRitualArmed = false;
                rageRitualFailed = true;
                SyncLaw();
            }

            // 序列一：数着昼夜，等这个世界改口。
            if (Sequence <= 1) TickUsurpDays();

            // 名义：蘑菇王的孢子、世界状态带来的称号（都只在服务器上算）。
            if (TitleSystemActive)
            {
                UpdateSporeAura();
                if (++worldTitleTick >= 60)
                {
                    worldTitleTick = 0;
                    CheckWorldTitles();
                }
            }

            // 已完成的仪式就不再打扰玩家。
            if (!chaosRitualComplete) UpdateChaosRitual();

            if (activeLaw == BlackEmperorLaw.None) return;

            lawTimer--;
            if (lawTimer <= 0)
            {
                CompletePolicyIfInTown();
                TryCountUsurpRitual();
                EndLaw(false);
                return;
            }

            switch (activeLaw)
            {
                case BlackEmperorLaw.StayPut:
                    // 走远了就是违约：自己吃反噬。
                    if (Vector2.Distance(Player.Center, lawAnchor) > StayPutRadius) BreakLaw("【禁足】你离开了律令划定的范围。");
                    break;

                case BlackEmperorLaw.NoApproach:
                    if (Vector2.Distance(Player.Center, lawAnchor) > 500f)
                    {
                        BreakLaw("【禁止接近】你走得太远，这条律令失去了落脚点。");
                        break;
                    }
                    RepelIntruders();
                    break;

                case BlackEmperorLaw.Trial:
                    if (trialTarget < 0 || trialTarget >= Main.maxNPCs || !Main.npc[trialTarget].active)
                    {
                        // 目标没了，判决自动撤销，不罚。
                        EndLaw(false);
                    }
                    break;
            }
        }

        /// <summary>禁止接近：把踏入范围的敌人推开，并按秒结算一次伤害。</summary>
        private void RepelIntruders()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            int damage = BlackEmperorMath.ScaledDamage(30, Sequence);
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!CanAffectEnemy(npc)) continue;
                Vector2 offset = npc.Center - Player.Center;
                float distance = offset.Length();
                if (distance > NoApproachRadius) continue;

                Vector2 direction = offset.SafeNormalize(Vector2.UnitX);
                // 越靠近越推得狠，敌人很难贴脸。
                float push = MathHelper.Lerp(7f, 2.5f, distance / NoApproachRadius);
                npc.velocity += direction * push * (BlackEmperorMath.IsBoss(npc) ? 0.35f : 1f);
                if (lawTimer % 60 == 0)
                {
                    npc.SimpleStrikeNPC(damage, direction.X >= 0 ? 1 : -1, false, 2f, DamageClass.Generic, false);
                }
            }
        }

        // ── 按键 ─────────────────────────────────────────────────
        public override void ProcessTriggers(Terraria.GameInput.TriggersSet triggersSet)
        {
            if (Player.whoAmI != Main.myPlayer || Player.dead) return;
            int seq = Sequence;
            if (seq > 9) return;

            bool shift = Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) ||
                         Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift);

            if (LotMKeybinds.BlackEmperor_Contract.JustPressed)
            {
                if (shift) CycleLaw();
                else ToggleLaw();
            }
            if (LotMKeybinds.BlackEmperor_Bribe.JustPressed)
            {
                bool bribeShift = shift || Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) ||
                                  Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightControl);
                if (bribeShift) CycleBribeMode();
                else CastBribe();
            }
            if (LotMKeybinds.BlackEmperor_Twist.JustPressed)
            {
                bool twistCtrl = Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) ||
                                 Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightControl);
                if (twistCtrl) RequestAbility(BlackEmperorAbility.TwistConcept);
                else if (shift && Sequence <= 1) CycleDefinition();   // 序列一：Shift 换成「定义」
                else if (shift) RequestAbility(BlackEmperorAbility.TwistConcept);
                else RequestAbility(BlackEmperorAbility.Twist);
            }
            if (LotMKeybinds.BlackEmperor_Chaos.JustPressed)
                RequestAbility(BlackEmperorAbility.ChaosField);
            if (LotMKeybinds.BlackEmperor_Gift.JustPressed)
            {
                if (shift) CycleGiftMode();
                else RequestAbility(BlackEmperorAbility.Gift);
            }
            if (LotMKeybinds.BlackEmperor_Amplify.JustPressed)
            {
                if (shift) CycleAmplifyMode();
                else RequestAbility(BlackEmperorAbility.Amplify);
            }
            if (LotMKeybinds.BlackEmperor_Rage.JustPressed)
                RequestAbility(BlackEmperorAbility.Rage);
            if (LotMKeybinds.BlackEmperor_Title.JustPressed)
            {
                // 不按 Shift 就是换一个称号戴着，按 Shift 才是用它的能力。
                if (shift) RequestAbility(BlackEmperorAbility.TitleSkill);
                else if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) ||
                         Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightControl))
                    CycleTitle(-1);
                else CycleTitle(1);
            }
            if (LotMKeybinds.BlackEmperor_Entropy.JustPressed)
                RequestAbility(BlackEmperorAbility.Entropy);
            if (LotMKeybinds.BlackEmperor_Lawless.JustPressed) CastLawlessStrike();
            if (LotMKeybinds.BlackEmperor_Unstoppable.JustPressed) CastUnstoppable();
        }

        // ── 序列七 贿赂者 ────────────────────────────────────────

        public static string BribeModeName(BlackEmperorBribeMode mode) => mode switch
        {
            BlackEmperorBribeMode.Weaken => "削弱",
            BlackEmperorBribeMode.Charm => "魅惑",
            BlackEmperorBribeMode.Arrogant => "狂妄",
            _ => "关联"
        };

        private void CycleBribeMode()
        {
            if (Sequence > 7) { Feedback("【贿赂】序列七「贿赂者」解锁。"); return; }
            bribeMode = (bribeMode + 1) % 4;
            Feedback($"【贿赂】已选择：{BribeModeName((BlackEmperorBribeMode)bribeMode)}");
        }

        /// <summary>钱币等级决定强度：优先用身上面额最大的一种钱。</summary>
        private int PickCoinTier(out int coinType)
        {
            for (int i = 0; i < CoinTypes.Length; i++)
            {
                if (Player.CountItem(CoinTypes[i]) > 0)
                {
                    coinType = CoinTypes[i];
                    return CoinTypes.Length - 1 - i;   // 铂金=3 金=2 银=1 铜=0
                }
            }
            coinType = 0;
            return -1;
        }

        private static int BribeDurationTicks(BlackEmperorBribeMode mode, int tier) => mode switch
        {
            BlackEmperorBribeMode.Weaken => new[] { 4, 6, 8, 12 }[tier] * 60,
            BlackEmperorBribeMode.Charm => new[] { 6, 9, 12, 18 }[tier] * 60,
            BlackEmperorBribeMode.Arrogant => new[] { 5, 8, 10, 15 }[tier] * 60,
            _ => new[] { 6, 8, 12, 16 }[tier] * 60
        };

        private void CastBribe()
        {
            if (Sequence > 7) { Feedback("【贿赂】序列七「贿赂者」解锁。"); return; }
            if (bribeCooldown > 0) { Feedback($"【贿赂】尚在冷却：{bribeCooldown / 60f:F1} 秒。"); return; }
            // 《秩序倾覆》立着的时候，这条路自己把贿赂封了——换来的是攻击更重。
            if (activeLaw == BlackEmperorLaw.OrderOverthrow && lawTimer > 0)
            {
                Feedback("【贿赂】《秩序倾覆》禁止贿赂：这条律令立着，钱就不管用。");
                return;
            }

            NPC target = FindBribeTarget();
            NPC townNpc = target == null ? FindTownNpc() : null;
            if (target == null && townNpc == null)
            {
                Feedback("【贿赂】把鼠标指向 600 像素以内的敌人或城镇 NPC。");
                return;
            }

            int tier = PickCoinTier(out int coinType);
            if (tier < 0)
            {
                Feedback("【贿赂】贿赂要付真钱：身上至少要有 1 枚钱币。");
                return;
            }
            float cost = BribeCost;
            LotMPlayer lotm = Player.GetModPlayer<LotMPlayer>();
            if (!lotm.TryConsumeSpirituality(cost, true))
            {
                Feedback($"【贿赂】灵性不足，需要 {cost:F0} 点。");
                return;
            }
            Player.ConsumeItem(coinType);
            bribeCooldown = SkillCooldown;
            CountBribeCast();

            BlackEmperorBribeMode mode = (BlackEmperorBribeMode)bribeMode;
            string tierName = new[] { "铜币", "银币", "金币", "铂金币" }[tier];

            if (townNpc != null)
            {
                // 非战斗用法：买通城镇 NPC，换一段时间的好运与庇护。
                townFavorTimer = 6 * 60 * 60;
                Announce($"【贿赂】你请 {townNpc.GivenOrTypeName} 收下了你的{tierName}，它会在一段时间内对你睁一只眼闭一只眼。");
                // 堕落伯爵的仪式要的正是一个个站到你这边的人。
                RegisterCorruptedTown(townNpc.type);
                return;
            }

            int ticks = BribeDurationTicks(mode, tier);
            if (HasTitle(BlackEmperorTitle.Gambler)) ticks = (int)(ticks * 1.5f);   // 赌徒买来的时间更长
            if (definitionMode == (int)BlackEmperorDefinition.Bribe) ticks = (int)(ticks * 1.5f);   // 你连「贿赂」都是自己定义的
            bool boss = BlackEmperorMath.IsBoss(target);
            target.GetGlobalNPC<BlackEmperorGlobalNPC>().ApplyBribe(target, mode, ticks, Player.whoAmI, boss);
            if (mode == BlackEmperorBribeMode.Link) linkedNpc = target.whoAmI;
            target.netUpdate = true;

            Announce(boss
                ? $"【贿赂·{BribeModeName(mode)}】{target.GivenOrTypeName} 也接受了交易，但位格让效果持续时间缩短。"
                : $"【贿赂·{BribeModeName(mode)}】用{tierName}买通了 {target.GivenOrTypeName}，持续 {ticks / 60f:F0} 秒。");
        }

        private NPC FindBribeTarget()
        {
            NPC best = null;
            float bestDistance = 600f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!CanAffectEnemy(npc)) continue;
                float distance = npc.Distance(Main.MouseWorld);
                if (distance < bestDistance) { bestDistance = distance; best = npc; }
            }
            return best;
        }

        private NPC FindTownNpc()
        {
            NPC best = null;
            float bestDistance = 600f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.townNPC || !npc.active) continue;
                float distance = npc.Distance(Main.MouseWorld);
                if (distance < bestDistance) { bestDistance = distance; best = npc; }
            }
            return best;
        }

        /// <summary>关联：找到替玩家分担伤害的目标。</summary>
        private NPC FindLinkedNpc()
        {
            if (linkedNpc < 0 || linkedNpc >= Main.maxNPCs) return null;
            NPC npc = Main.npc[linkedNpc];
            if (!npc.active) { linkedNpc = -1; return null; }
            var state = npc.GetGlobalNPC<BlackEmperorGlobalNPC>();
            if (state.linkedTicks <= 0 || state.linkedOwner != Player.whoAmI) { linkedNpc = -1; return null; }
            return npc;
        }

        /// <summary>
        /// 序列八 · 无法之地：不能依靠法律的时候，就用力量解决。
        /// 朝鼠标方向挥出一记重击，命中扇形内的所有敌人；Boss 吃较弱击退并额外承受 45% 伤害。
        /// 每次命中都会为「以力破法」充能。
        /// </summary>
        private void CastLawlessStrike()
        {
            if (Sequence > 8)
            {
                Feedback("【无法之地】序列八「野蛮人」解锁。");
                return;
            }
            if (bruteCooldown > 0)
            {
                Feedback($"【无法之地】尚在冷却：{bruteCooldown / 60f:F1} 秒。");
                return;
            }
            float cost = LawCost;
            if (!Player.GetModPlayer<LotMPlayer>().TryConsumeSpirituality(cost, true))
            {
                Feedback($"【无法之地】灵性不足，需要 {cost:F0} 点。");
                return;
            }
            bruteCooldown = SkillCooldown;

            Vector2 aim = (Main.MouseWorld - Player.Center).SafeNormalize(Vector2.UnitX);
            int damage = BlackEmperorMath.ScaledDamage(190, Sequence);
            int hits = 0;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!CanAffectEnemy(npc)) continue;
                Vector2 offset = npc.Center - Player.Center;
                if (offset.Length() > 180f) continue;
                // 只有鼠标方向 ±60° 扇形内的敌人会被这一击扫到。
                if (Vector2.Dot(offset.SafeNormalize(aim), aim) < 0.5f) continue;

                bool boss = BlackEmperorMath.IsBoss(npc);
                int final = boss ? (int)(damage * 1.45f) : damage;
                npc.SimpleStrikeNPC(final, aim.X >= 0 ? 1 : -1, false, boss ? 2f : 12f, DamageClass.Generic, false);
                hits++;
            }

            if (hits > 0) ChargeLawBreaker(hits);

            if (!Main.dedServ)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.5f, Pitch = -0.25f }, Player.Center);
                for (int i = 0; i < 12; i++)
                {
                    Vector2 speed = aim.RotatedBy(Main.rand.NextFloat(-0.5f, 0.5f)) * Main.rand.NextFloat(3f, 7f);
                    Dust d = Dust.NewDustPerfect(Player.Center + aim * 24f, DustID.Stone, speed, 100, default, 1.2f);
                    d.noGravity = true;
                }
            }
            Announce(hits > 0 ? $"【无法之地】重击命中 {hits} 个目标。" : "【无法之地】挥空了。");
        }

        /// <summary>序列八 · 硬闯：三秒内不被打断，并立刻摆脱所有控制类减益。</summary>
        private void CastUnstoppable()
        {
            if (Sequence > 8)
            {
                Feedback("【硬闯】序列八「野蛮人」解锁。");
                return;
            }
            if (unstoppableCooldown > 0)
            {
                Feedback($"【硬闯】尚在冷却：{unstoppableCooldown / 60f:F1} 秒。");
                return;
            }
            float cost = LawCost;
            if (!Player.GetModPlayer<LotMPlayer>().TryConsumeSpirituality(cost, true))
            {
                Feedback($"【硬闯】灵性不足，需要 {cost:F0} 点。");
                return;
            }
            unstoppableTimer = 3 * 60;
            unstoppableCooldown = SkillCooldown;
            ClearControlDebuffs();
            Announce("【硬闯】规矩拦不住你——三秒内不被打断。");
        }

        /// <summary>把身上的控制类减益一次性清掉。</summary>
        private void ClearControlDebuffs()
        {
            int[] control = { BuffID.Frozen, BuffID.Webbed, BuffID.Slow, BuffID.Confused, BuffID.Stoned };
            foreach (int type in control)
            {
                int index = Player.FindBuffIndex(type);
                if (index >= 0) Player.DelBuff(index);
            }
        }

        /// <summary>以力破法：重击每命中一次充能一次，攒满三次换一次「无视契约反噬」。</summary>
        private void ChargeLawBreaker(int hits)
        {
            if (lawBreakerReady) return;
            if (Sequence > 8) return;
            lawBreakerCharge += hits;
            if (lawBreakerCharge < LawBreakerChargeRequired) return;
            lawBreakerCharge = 0;
            lawBreakerReady = true;
            Announce("【以力破法】力量已经攒够，下一次契约反噬会被硬扛下来。");
        }

        private void CycleLaw()
        {
            var laws = AvailableLaws();
            if (laws.Length == 0) return;
            lawChoice = (lawChoice + 1) % laws.Length;
            Feedback($"【契约】已选择：{LawName(laws[lawChoice])}");
        }

        // ── 序列六 腐化男爵 ───────────────────────────────────────

        /// <summary>
        /// 被动·腐蚀：身边二十格左右的人，心里会一点点长出阴影。
        /// 每层都让它打人更轻、挨打更痛、动作更慢；离开光环之后才开始褪。
        /// 叠层与褪层都只在服务器上跑，客户端只跟着图标与同步走。
        /// </summary>
        private void UpdateCorruptionAura()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (++corruptionTick < CorruptionInterval) return;
            corruptionTick = 0;
            corruptionBeat++;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!CanAffectEnemy(npc)) continue;
                if (npc.Distance(Player.Center) > CorruptionRadius) continue;

                var state = npc.GetGlobalNPC<BlackEmperorGlobalNPC>();
                state.corruptionRefresh = 90;
                if (BlackEmperorMath.IsBoss(npc) && corruptionBeat % 2 == 1) continue;
                if (state.corruptionStacks >= BlackEmperorGlobalNPC.MaxCorruptionStacks) continue;

                state.corruptionStacks++;
                npc.netUpdate = true;
            }
        }

        /// <summary>
        /// 客户端按下技能键以后，只把「想做什么」和鼠标位置交给服务器；
        /// 单机与主机则直接就地结算。世界改动永远不会由客户端自己拍板。
        /// </summary>
        private void RequestAbility(BlackEmperorAbility ability)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write((byte)LotMNetMsg.BlackEmperorAbilityRequest);
                packet.Write((byte)ability);
                packet.WriteVector2(Main.MouseWorld);
                packet.Send();
            }
            else ExecuteAbility(ability, Main.MouseWorld);
        }

        public void ExecuteAbility(BlackEmperorAbility ability, Vector2 target)
        {
            switch (ability)
            {
                case BlackEmperorAbility.Twist:
                    ExecuteTwist(target);
                    break;
                case BlackEmperorAbility.TwistConcept:
                    ExecuteTwistConcept(target);
                    break;
                case BlackEmperorAbility.ChaosField:
                    ExecuteChaosField(target);
                    break;
                case BlackEmperorAbility.Gift:
                    ExecuteGift(target);
                    break;
                case BlackEmperorAbility.Amplify:
                    ExecuteAmplify(target);
                    break;
                case BlackEmperorAbility.Rage:
                    ExecuteRage(target);
                    break;
                case BlackEmperorAbility.TitleSkill:
                    ExecuteTitleSkill(target);
                    break;
                case BlackEmperorAbility.Entropy:
                    ExecuteEntropyCashIn(target);
                    break;
            }
        }

        public static void ReceiveAbilityRequest(BinaryReader reader, int whoAmI)
        {
            BlackEmperorAbility ability = (BlackEmperorAbility)reader.ReadByte();
            Vector2 target = reader.ReadVector2();
            if (Main.netMode != NetmodeID.Server) return;
            if (whoAmI < 0 || whoAmI >= Main.maxPlayers || !Main.player[whoAmI].active) return;

            Player player = Main.player[whoAmI];
            if (player.dead || !Enum.IsDefined(typeof(BlackEmperorAbility), ability)) return;
            if (!float.IsFinite(target.X) || !float.IsFinite(target.Y)) target = player.Center;

            player.GetModPlayer<BlackEmperorPlayer>().ExecuteAbility(ability, target);
        }

        /// <summary>扭曲的实际半径：到了狂乱法师这一步，连炮弹的轨道都能直接扭过来。</summary>
        private float EffectiveTwistRadius =>
            TwistRadius * (Sequence <= 1 ? 1.8f : Sequence <= 3 ? 1.4f : 1f);

        /// <summary>
         /// 序列六 · 扭曲：腐化男爵擅长的从来不是正面对抗，而是改写含义。
        /// 「这是谁的攻击」被改写，飞过来的弹幕就掉头去打它原来的主人；
        /// 「前进是哪一边」被改写，扑上来的敌人会认定你在它背后。
        /// Boss 同样会被改写意图，但持续时间和移动幅度会按位格折减。
        /// </summary>
        private void ExecuteTwist(Vector2 aimPoint)
        {
            if (Sequence > 6) { Feedback("【扭曲】序列六「腐化男爵」解锁。"); return; }
            if (twistCooldown > 0) { Feedback($"【扭曲】尚在冷却：{twistCooldown / 60f:F1} 秒。"); return; }

            float cost = TwistCost;
            if (!Player.GetModPlayer<LotMPlayer>().TryConsumeSpirituality(cost, true))
            {
                Feedback($"【扭曲】灵性不足，需要 {cost:F0} 点。");
                return;
            }
            twistCooldown = SkillCooldown;

            Vector2 aim = (aimPoint - Player.Center).SafeNormalize(Vector2.UnitX);
            int turned = TwistProjectiles(aim);
            int confused = TwistIntent();

            if (!Main.dedServ)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.45f, Pitch = 0.35f }, Player.Center);
                // 意义被扭过来的手感：一圈灰尘从外往里收，不做亮光。
                for (int i = 0; i < 26; i++)
                {
                    Vector2 offset = (MathHelper.TwoPi * i / 26f).ToRotationVector2() * EffectiveTwistRadius * 0.55f;
                    Vector2 inward = -offset.SafeNormalize(Vector2.UnitX);
                    Dust dust = Dust.NewDustPerfect(Player.Center + offset, DustID.PurpleCrystalShard,
                        inward * Main.rand.NextFloat(1.2f, 2.6f), 120, default, 1.05f);
                    dust.noGravity = true;
                }
            }

            Announce(turned > 0 || confused > 0
                ? $"【扭曲】{turned} 道弹幕改换了主人，{confused} 个目标的意图被改写。"
                : "【扭曲】附近没有可以被改写的攻击或意图。");
        }

        /// <summary>把范围内的敌对弹幕收归己用，并让它们朝你指的方向飞去。</summary>
        private int TwistProjectiles(Vector2 aim)
        {
            int count = 0;
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.hostile || projectile.friendly) continue;
                if (projectile.Distance(Player.Center) > EffectiveTwistRadius) continue;

                FlipHostileProjectile(projectile, aim);
                count++;
            }
            return count;
        }

        /// <summary>把一道敌对弹幕改判成友方，并让它朝指定方向飞去。</summary>
        private bool FlipHostileProjectile(Projectile projectile, Vector2 aim)
        {
            int damage = BlackEmperorMath.ScaledDamage(150, Sequence);
            projectile.hostile = false;
            projectile.friendly = true;
            projectile.owner = Player.whoAmI;
            // 归属改了，威力也得按你的序列重新说一遍才算数。
            projectile.damage = Math.Max(projectile.damage, damage);
            projectile.originalDamage = Math.Max(projectile.originalDamage, damage);
            projectile.velocity = aim.SafeNormalize(Vector2.UnitX) * Math.Max(projectile.velocity.Length(), 6f);
            projectile.timeLeft = Math.Max(projectile.timeLeft, 45);
            projectile.netUpdate = true;
            return true;
        }

        /// <summary>改写范围内敌人的「前进」与「敌我」，让它掉头离开你。</summary>
        private int TwistIntent()
        {
            int count = 0;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!CanAffectEnemy(npc)) continue;
                if (npc.Distance(Player.Center) > EffectiveTwistRadius) continue;

                var state = npc.GetGlobalNPC<BlackEmperorGlobalNPC>();
                // 序列三之后，「前行」这个意图本身也被强化：它停不下来了。
                bool raging = Sequence <= 3;
                int twistTime = TwistDuration * (Sequence <= 1 ? 3 : raging ? 2 : 1);
                if (BlackEmperorMath.IsBoss(npc)) twistTime /= 3;   // Boss 被扭的时间短一些
                state.twistedTicks = Math.Max(state.twistedTicks, twistTime);
                state.twistedUnstoppable = raging;
                npc.netUpdate = true;
                count++;
            }
            return count;
        }

        // ── 序列五 混乱导师 ───────────────────────────────────────

        /// <summary>
        /// 主动·混乱场：以自己为圆心割下一块地，你站在正中央。
        /// 场内没有「正确的距离」，也没有「正确的敌人」——攻击容易落空，
        /// 生物会朝错误的方向迈步，飞进来的弹幕可能忽然认错主人。再按一次即可收起。
        /// </summary>
        private void ExecuteChaosField(Vector2 center)
        {
            if (Sequence > 5) { Feedback("【混乱场】序列五「混乱导师」解锁。"); return; }

            if (chaosFieldActive)
            {
                chaosFieldActive = false;
                chaosFieldTimer = 0;
                KillChaosDomain();
                Announce("【混乱场】你收起了这块地，距离重新变得可靠。");
                SyncLaw();
                return;
            }

            if (chaosCooldown > 0) { Feedback($"【混乱场】尚在冷却：{chaosCooldown / 60f:F1} 秒。"); return; }

            float cost = ChaosCost;
            if (!Player.GetModPlayer<LotMPlayer>().TryConsumeSpirituality(cost, true))
            {
                Feedback($"【混乱场】灵性不足，需要 {cost:F0} 点。");
                return;
            }

            chaosFieldActive = true;
            chaosFieldTimer = ChaosFieldDuration;
            // 玩家永远站在正中央：圆心就是自己，走到哪里这块地就跟到哪里。
            chaosFieldCenter = Player.Center;
            chaosPulse = 0;
            chaosCooldown = SkillCooldown;
            SpawnChaosDomain();
            SyncLaw();   // 其它端也要看得见这块地

            if (!Main.dedServ)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.4f, Pitch = -0.2f }, Player.Center);
                // 切口张开的那一下：一圈灰尘从圆心向外铺开，之后交给圆盘自己转。
                for (int i = 0; i < 14; i++)
                {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(ChaosFieldRadius, ChaosFieldRadius);
                    Dust dust = Dust.NewDustPerfect(Player.Center + offset, DustID.PurpleCrystalShard, Vector2.Zero, 140, default, 0.9f);
                    dust.noGravity = true;
                    dust.velocity = offset.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(0.4f, 1.4f);
                }
            }
            Announce("【混乱场】在这块地里，距离和敌我都不再作数。");
        }

        /// <summary>
        /// 混乱场的持续效果。客户端只负责把倒计时走完与画出边界，
        /// 场内发生的事（误判、弹幕改判）全部由服务器裁定。
        /// </summary>
        private void UpdateChaosField()
        {
            if (!chaosFieldActive) return;

            if (chaosFieldTimer > 0) chaosFieldTimer--;
            if (chaosFieldTimer <= 0)
            {
                chaosFieldActive = false;
                if (Main.netMode != NetmodeID.MultiplayerClient) Announce("【混乱场】这一块地重新变得规整。");
                SyncLaw();
                return;
            }

            // 圆心跟着自己走：这块地是你走到哪里、哪里就不讲道理。
            chaosFieldCenter = Player.Center;

            if (!Main.dedServ) ChaosFieldVisuals();

            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (++chaosPulse < ChaosFieldPulse) return;
            chaosPulse = 0;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!CanAffectEnemy(npc)) continue;
                if (npc.Distance(chaosFieldCenter) > ChaosFieldRadius) continue;
                // 站在场里就算「还在误判」，走出去之后自己会清醒。
                npc.GetGlobalNPC<BlackEmperorGlobalNPC>().chaosTicks = 60;
                npc.netUpdate = true;
            }

            // 飞进这块地的弹幕也可能忽然认错主人。
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.hostile || projectile.friendly) continue;
                if (projectile.Distance(chaosFieldCenter) > ChaosFieldRadius) continue;
                if (!Main.rand.NextBool(3)) continue;
                FlipHostileProjectile(projectile, Main.rand.NextVector2CircularEdge(1f, 1f));
            }
        }

        /// <summary>张开设法者脚下的那块地。只在服务器/单机上生成，客户端跟着弹幕同步走。</summary>
        private void SpawnChaosDomain()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            KillChaosDomain();   // 反复开关也不会叠出第二个圆盘
            Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero,
                ModContent.ProjectileType<ChaosDomainProjectile>(), 0, 0f, Player.whoAmI, ChaosFieldRadius);
        }

        /// <summary>收起这块地：圆盘自己会用 24 tick 淡出，这里只是不再续期。</summary>
        private void KillChaosDomain()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != Player.whoAmI) continue;
                if (projectile.type != ModContent.ProjectileType<ChaosDomainProjectile>()) continue;
                if (projectile.timeLeft > 24) projectile.timeLeft = 24;
            }
        }

        /// <summary>边界上慢慢飘过的暗色灰尘：看得见这块地，但不会被它晃眼。</summary>
        private void ChaosFieldVisuals()
        {
            if (!Main.rand.NextBool(5)) return;

            Vector2 offset = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * ChaosFieldRadius;
            Vector2 tangent = offset.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            Dust dust = Dust.NewDustPerfect(chaosFieldCenter + offset, DustID.PurpleCrystalShard,
                tangent * Main.rand.NextFloat(0.2f, 0.8f), 150, default, 0.8f);
            dust.noGravity = true;
        }

        /// <summary>
        /// 主动·扭曲概念：序列五的扭曲不再只改弹幕，而是直接改「谁在承受」。
        /// 概念绑住的目标会替你分担一半伤害，你沾上的诅咒与疾病也会顺着这条概念爬过去。
        /// 再按一次即可松开。
        /// </summary>
        private void ExecuteTwistConcept(Vector2 aimPoint)
        {
            if (Sequence > 5) { Feedback("【扭曲概念】序列五「混乱导师」解锁。"); return; }

            if (conceptTicks > 0)
            {
                conceptTicks = 0;
                conceptTarget = -1;
                Announce("【扭曲概念】你松开了这条概念。");
                SyncLaw();
                return;
            }

            NPC target = FindConceptTarget(aimPoint);
            if (target == null)
            {
                Feedback("【扭曲概念】把鼠标指向 600 像素以内的目标。");
                return;
            }

            if (twistCooldown > 0) { Feedback($"【扭曲概念】尚在冷却：{twistCooldown / 60f:F1} 秒。"); return; }

            float cost = TwistCost;
            if (!Player.GetModPlayer<LotMPlayer>().TryConsumeSpirituality(cost, true))
            {
                Feedback($"【扭曲概念】灵性不足，需要 {cost:F0} 点。");
                return;
            }

            conceptTarget = target.whoAmI;
            conceptTicks = ConceptDuration;
            twistCooldown = SkillCooldown;
            var state = target.GetGlobalNPC<BlackEmperorGlobalNPC>();
            state.conceptOwner = Player.whoAmI;
            state.conceptTicks = conceptTicks;
            target.netUpdate = true;
            SyncLaw();

            if (!Main.dedServ)
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.4f, Pitch = 0.1f }, target.Center);

            Announce($"【扭曲概念】{target.GivenOrTypeName} 的「承受」已经被改写——你受的伤，它也要担一半。");
        }

        private NPC FindConceptTarget(Vector2 point)
        {
            NPC best = null;
            float bestDistance = 600f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!CanAffectEnemy(npc)) continue;
                float distance = npc.Distance(point);
                if (distance < bestDistance) { bestDistance = distance; best = npc; }
            }
            return best;
        }

        /// <summary>概念上的联系：目标没了就断，还在就把诅咒一条条抄过去。</summary>
        private NPC FindConceptNpc()
        {
            if (conceptTicks <= 0 || conceptTarget < 0 || conceptTarget >= Main.maxNPCs) return null;
            NPC npc = Main.npc[conceptTarget];
            if (!npc.active) return null;
            return npc;
        }

        private void UpdateConceptLink()
        {
            if (FindConceptNpc() == null)
            {
                conceptTarget = -1;
                conceptTicks = 0;
                return;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient) return;   // 改世界的事交给服务器

            NPC target = Main.npc[conceptTarget];
            var state = target.GetGlobalNPC<BlackEmperorGlobalNPC>();
            state.conceptOwner = Player.whoAmI;
            state.conceptTicks = conceptTicks;

            // 你沾上的诅咒与疾病会顺着这条概念爬过去。
            if (conceptCopyCooldown > 0) return;
            conceptCopyCooldown = 30;
            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                int type = Player.buffType[i];
                if (type <= 0 || !Main.debuff[type]) continue;
                target.AddBuff(type, 300);
                break;   // 一次只抄一条，别让目标一口气吃满
            }
        }

        // ── 晋升仪式：城市地底的秩序（序列六 → 五）───────────────

        /// <summary>
        /// 仪式：城里养着至少五个人，然后连续三个夜晚在城市地底压下秩序。
        /// 每一夜都要亲手了结足够多的敌人；中途动了城里的人，连夜的账就一笔作废。
        /// </summary>
        private void UpdateChaosRitual()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (Sequence > 6) return;   // 只有走到序列六、准备晋升的人才会记账

            if (!Main.dayTime)
            {
                // 天黑就开始记账；中途进游戏的玩家也从这一夜算起。
                chaosRitualNightActive = true;
                return;
            }

            if (!chaosRitualNightActive) return;
            chaosRitualNightActive = false;

            bool enoughPeople = CountTownNpcs() >= ChaosRitualTownNpcTarget;
            if (enoughPeople && chaosRitualKills >= ChaosRitualKillsPerNight)
            {
                chaosRitualNights++;
                if (chaosRitualNights >= ChaosRitualNightTarget)
                {
                    chaosRitualComplete = true;
                    Announce("【晋升仪式·混乱】三个夜晚之后，城市的地底已经只认你的规矩。");
                }
                else
                {
                    Announce($"【晋升仪式·混乱】地底安静了这一夜 {chaosRitualNights}/{ChaosRitualNightTarget}。");
                }
            }
            else if (chaosRitualNights > 0)
            {
                chaosRitualNights = 0;
                Announce("【晋升仪式·混乱】这一夜没能压住地底，连夜的秩序断了。");
            }
            chaosRitualKills = 0;
            SyncLaw();   // 天亮结算之后，把结果推给玩家自己的界面
        }

        /// <summary>仪式记账：只有夜里、在城市地底、亲手了结的敌人才算数。</summary>
        public void RecordUndergroundKill(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (chaosRitualComplete || Sequence > 6) return;
            if (Main.dayTime) return;
            if (!Player.ZoneRockLayerHeight && !Player.ZoneUnderworldHeight) return;
            if (npc.friendly || npc.townNPC || npc.SpawnedFromStatue) return;
            if (npc.lifeMax < 10 || BlackEmperorMath.IsBoss(npc)) return;
            if (NPCID.Sets.CountsAsCritter[npc.type]) return;

            chaosRitualNightActive = true;
            chaosRitualKills++;
            SyncLaw();   // 让玩家自己的界面立刻看到进度
        }

        /// <summary>仪式断了：杀了城里的人，或者别的犯规行为。</summary>
        public void BreakChaosRitual(string reason)
        {
            if (chaosRitualNights <= 0 && chaosRitualKills <= 0) return;
            chaosRitualNights = 0;
            chaosRitualKills = 0;
            chaosRitualNightActive = false;
            Announce($"【晋升仪式·混乱】{reason}，连夜的账一笔作废。");
            SyncLaw();
        }

        public static int CountTownNpcs()
        {
            int count = 0;
            foreach (NPC npc in Main.ActiveNPCs)
                if (npc.townNPC && npc.life > 0) count++;
            return count;
        }

        // ── 序列四 堕落伯爵 ───────────────────────────────────────

        public static string GiftModeName(BlackEmperorGiftMode mode) => mode switch
        {
            BlackEmperorGiftMode.Slack => "消极怠工",
            BlackEmperorGiftMode.Greed => "贪婪急切",
            _ => "丧失斗志"
        };

        public static string AmplifyModeName(BlackEmperorAmplifyMode mode) => mode switch
        {
            BlackEmperorAmplifyMode.Execute => "处决",
            _ => "束缚"
        };

        private void CycleGiftMode()
        {
            if (Sequence > 4) { Feedback("【赠予】序列四「堕落伯爵」解锁。"); return; }
            giftMode = (giftMode + 1) % 3;
            Feedback($"【赠予】已选择：{GiftModeName((BlackEmperorGiftMode)giftMode)}");
        }

        private void CycleAmplifyMode()
        {
            if (Sequence > 4) { Feedback("【放大】序列四「堕落伯爵」解锁。"); return; }
            amplifyMode = (amplifyMode + 1) % 2;
            Feedback($"【放大】已选择：{AmplifyModeName((BlackEmperorAmplifyMode)amplifyMode)}");
        }

        /// <summary>
        /// 主动·赠予：到了这个层次，半神不必亲自动手，只要把一种状态「送」过去。
        /// 消极怠工让它打不动人，贪婪急切让它眼里只剩钱，丧失斗志让它不敢靠近你。
        /// </summary>
        private void ExecuteGift(Vector2 aimPoint)
        {
            if (Sequence > 4) { Feedback("【赠予】序列四「堕落伯爵」解锁。"); return; }
            if (giftCooldown > 0) { Feedback($"【赠予】尚在冷却：{giftCooldown / 60f:F1} 秒。"); return; }

            NPC target = FindConceptTarget(aimPoint);   // 与扭曲概念同一套取目标：鼠标 600 像素内
            if (target == null)
            {
                Feedback("【赠予】把鼠标指向 600 像素以内的目标。");
                return;
            }

            float cost = GiftCost;
            if (!Player.GetModPlayer<LotMPlayer>().TryConsumeSpirituality(cost, true))
            {
                Feedback($"【赠予】灵性不足，需要 {cost:F0} 点。");
                return;
            }
            giftCooldown = SkillCooldown;

            BlackEmperorGiftMode mode = (BlackEmperorGiftMode)giftMode;
            bool boss = BlackEmperorMath.IsBoss(target);
            target.GetGlobalNPC<BlackEmperorGlobalNPC>().ApplyGift(mode, GiftDuration, boss);
            target.netUpdate = true;

            if (!Main.dedServ)
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.35f, Pitch = 0.5f }, target.Center);

            Announce(boss
                ? $"【赠予·{GiftModeName(mode)}】{target.GivenOrTypeName} 收下了这份礼物；位格只缩短持续时间，不再免疫效果。"
                : $"【赠予·{GiftModeName(mode)}】你把这份「礼物」交给了 {target.GivenOrTypeName}。");
        }

        /// <summary>
        /// 主动·放大：同一时间只放大一件事。
        /// 处决让下一击按目标的虚弱程度加重，束缚让下一击变成隔着距离的一个拥抱。
        /// </summary>
        private void ExecuteAmplify(Vector2 aimPoint)
        {
            if (Sequence > 4) { Feedback("【放大】序列四「堕落伯爵」解锁。"); return; }
            if (amplifyReady) { Feedback("【放大】已经有一件事被放大了，先用掉它。"); return; }
            if (amplifyCooldown > 0) { Feedback($"【放大】尚在冷却：{amplifyCooldown / 60f:F1} 秒。"); return; }

            float cost = AmplifyCost;
            if (!Player.GetModPlayer<LotMPlayer>().TryConsumeSpirituality(cost, true))
            {
                Feedback($"【放大】灵性不足，需要 {cost:F0} 点。");
                return;
            }

            amplifyReady = true;
            amplifyCooldown = SkillCooldown;
            SyncLaw();
            if (!Main.dedServ)
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.35f, Pitch = -0.1f }, Player.Center);
            Announce($"【放大·{AmplifyModeName((BlackEmperorAmplifyMode)amplifyMode)}】规则已经写好，下一次命中会被放大。");
        }


        /// <summary>
        /// 被动·利用：规则只管它写过的事。
        /// 「离开大地」可以被延长成滞空；自己身上的增益走得更慢，减益走得更快。
        /// 滞空由本地端算（位置本来就是本地裁定的），状态时间由服务器算，免得各端倒计时打架。
        /// </summary>
        private void UpdateRuleExploit()
        {
            if (Player.whoAmI == Main.myPlayer) UpdateHover();
            if (Main.netMode != NetmodeID.MultiplayerClient) UpdateStatusDurations();
        }

        /// <summary>滞空：离地后按住跳跃键，把「在空中」这个状态拖长一会儿。</summary>
        private void UpdateHover()
        {
            bool grounded = Player.velocity.Y == 0f;
            bool airborne = !grounded && Player.velocity.Y > -1f && !Player.mount.Active;
            if (airborne && Player.controlJump && hoverCooldown <= 0 && hoverTicks < HoverMaxTicks && !Player.dead)
            {
                hoverTicks++;
                Player.velocity.Y = MathHelper.Clamp(Player.velocity.Y, -0.02f, 0.02f);
                Player.fallStart = (int)(Player.position.Y / 16f);
                Player.fallStart2 = Player.fallStart;
                if (hoverTicks % 6 == 0)
                {
                    Dust dust = Dust.NewDustPerfect(Player.Bottom + new Vector2(Main.rand.NextFloat(-10f, 10f), 0f),
                        DustID.PurpleCrystalShard, new Vector2(0f, Main.rand.NextFloat(0.2f, 0.9f)), 150, default, 0.8f);
                    dust.noGravity = true;
                }
                return;
            }

            if (grounded) hoverCooldown = 0;
            if (hoverTicks >= HoverMaxTicks && hoverCooldown <= 0) hoverCooldown = 90;
            hoverTicks = 0;
        }

        /// <summary>增益每两 tick 才走一格（多活一半），减益每 tick 多走一格（少活一半）。</summary>
        private void UpdateStatusDurations()
        {
            bool slowBuffs = Main.GameUpdateCount % 2 == 0;
            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                int type = Player.buffType[i];
                int time = Player.buffTime[i];
                if (type <= 0 || time <= 0) continue;

                bool debuff = type < Main.debuff.Length && Main.debuff[type];
                if (debuff)
                {
                    if (time > 1) Player.buffTime[i] = time - 1;
                }
                else if (slowBuffs && time < MaxExploitBuffTime)
                {
                    Player.buffTime[i] = time + 1;
                }
            }
        }

        /// <summary>
        /// 规则领域：律令立下之处，规矩会被改写成有利于你的样子。
        /// 圈内的敌人出手更轻、也更容易被击穿；这是「划出一片区域篡改规则」的雏形。
        /// </summary>
        private void UpdateRuleDomain()
        {
            if (activeLaw == BlackEmperorLaw.None || Sequence > 4)
            {
                KillRuleRing();
                return;
            }
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!CanAffectEnemy(npc)) continue;
                if (npc.Distance(lawAnchor) > RuleDomainRadius) continue;
                npc.GetGlobalNPC<BlackEmperorGlobalNPC>().ruleTicks = 60;
                npc.netUpdate = true;
            }
            EnsureRuleRing();
        }

        /// <summary>规则领域只留一圈边界，钉在律令落下的地方。</summary>
        private void EnsureRuleRing()
        {
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != Player.whoAmI) continue;
                if (projectile.type != ModContent.ProjectileType<ChaosDomainProjectile>()) continue;
                if ((int)projectile.ai[1] != 1) continue;
                projectile.timeLeft = 600;
                projectile.netUpdate = true;
                return;
            }

            Projectile.NewProjectile(Player.GetSource_FromThis(), lawAnchor, Vector2.Zero,
                ModContent.ProjectileType<ChaosDomainProjectile>(), 0, 0f, Player.whoAmI, RuleDomainRadius, 1f);
        }

        private void KillRuleRing()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != Player.whoAmI) continue;
                if (projectile.type != ModContent.ProjectileType<ChaosDomainProjectile>()) continue;
                if ((int)projectile.ai[1] != 1) continue;
                if (projectile.timeLeft > 24) projectile.timeLeft = 24;
            }
        }

        /// <summary>律令在城里自然走完一次，就算推行过一项政策。</summary>
        private void CompletePolicyIfInTown()
        {
            if (policyImplemented || Sequence > 4) return;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            int nearby = 0;
            foreach (NPC npc in Main.ActiveNPCs)
                if (npc.townNPC && npc.life > 0 && npc.Distance(Player.Center) <= 600f) nearby++;
            if (nearby < 3) return;

            policyImplemented = true;
            Announce("【晋升仪式·堕落伯爵】你在城里推行的那条规矩走完了——政策已经落地。");
            SyncLaw();
        }

        /// <summary>拉拢腐化同阶层的人：记下这位城镇居民，凑够七个才算结成利益共同体。</summary>
        private void RegisterCorruptedTown(int npcType)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (Sequence > 4 || corruptedTownTypes.Contains(npcType)) return;

            corruptedTownTypes.Add(npcType);
            SyncLaw();
            Announce(corruptedTownTypes.Count >= CorruptedTownTarget
                ? $"【晋升仪式·堕落伯爵】第七个同僚也站到了你这边（{corruptedTownTypes.Count}/{CorruptedTownTarget}）。"
                : $"【晋升仪式·堕落伯爵】你已经拉拢了 {corruptedTownTypes.Count}/{CorruptedTownTarget} 个人。");
        }


        // ── 序列三 狂乱法师 ───────────────────────────────────────

        /// <summary>
        /// 主动·狂乱：一阵连施法者自己都读不准的波动。
        /// 你自己的状态会往有利的方向乱，周围敌人的状态会往不利的方向乱，
        /// 具体乱成什么样子，谁也不知道——这正是「狂乱」两个字的意思。
        /// </summary>
        private void ExecuteRage(Vector2 aimPoint)
        {
            if (Sequence > 3) { Feedback("【狂乱】序列三「狂乱法师」解锁。"); return; }
            if (rageCooldown > 0) { Feedback($"【狂乱】尚在冷却：{rageCooldown / 60f:F1} 秒。"); return; }

            float cost = RageCost;
            if (!Player.GetModPlayer<LotMPlayer>().TryConsumeSpirituality(cost, true))
            {
                Feedback($"【狂乱】灵性不足，需要 {cost:F0} 点。");
                return;
            }
            rageCooldown = SkillCooldown;

            // 自己：随机一两份好处。
            int[] good = { BuffID.Rage, BuffID.Wrath, BuffID.Swiftness, BuffID.Ironskin,
                BuffID.Regeneration, BuffID.MagicPower, BuffID.Endurance, BuffID.Lifeforce, BuffID.Thorns };
            Player.AddBuff(good[Main.rand.Next(good.Length)], RageDuration);
            if (Main.rand.NextBool(2)) Player.AddBuff(good[Main.rand.Next(good.Length)], RageDuration);

            // 周围：不分敌我，全部改成互相攻击——Boss 也一样。
            int affected = 0;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || npc.townNPC || npc.friendly) continue;
                if (!CanAffectEnemy(npc)) continue;
                if (npc.Distance(Player.Center) > RageRadius) continue;

                npc.GetGlobalNPC<BlackEmperorGlobalNPC>().rageInfightTicks = RageDuration;
                // 头顶挂一个「混乱」图标：让玩家一眼看出谁已经不听自己的了。
                npc.AddBuff(BuffID.Confused, RageDuration);
                npc.netUpdate = true;
                affected++;
            }

            // 这一场如果有 Boss 在场，就顺手记下仪式。
            ArmRageRitual();

            SpawnRageWave();
            if (!Main.dedServ)
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.45f, Pitch = -0.4f }, Player.Center);

            Announce(affected > 0
                ? $"【狂乱】波动扫过 {affected} 个目标：从现在起它们不分敌我，互相撕咬——连 Boss 也不例外。"
                : "【狂乱】波动散开了，附近没有可以被搅乱的东西。");
        }

        /// <summary>狂乱的扩散波：一圈从脚底推开、推到尽头就散的紫色边界。</summary>
        private void SpawnRageWave()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero,
                ModContent.ProjectileType<ChaosDomainProjectile>(), 0, 0f, Player.whoAmI, 40f, 2f);
        }

        /// <summary>这一场如果有 Boss 在场，就开始记账：全程不死才算数。</summary>
        private void ArmRageRitual()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (RageRitualComplete) return;

            bool bossNearby = false;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || !BlackEmperorMath.IsBoss(npc)) continue;
                if (npc.Distance(Player.Center) > 4000f) continue;
                bossNearby = true;
                break;
            }
            if (!bossNearby) return;

            rageRitualArmed = true;
            rageRitualFailed = false;
            Announce("【晋升仪式·狂乱法师】你在这场战斗里动过手了，活着走完它。");
        }

        /// <summary>Boss 倒下时结算：这一场动过狂乱、而且没死过，才算一场。</summary>
        public void RecordRageBoss(int npcType)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (RageRitualComplete || !rageRitualArmed || rageRitualFailed) return;
            if (rageRitualBosses.Contains(npcType)) { rageRitualArmed = false; return; }

            rageRitualBosses.Add(npcType);
            rageRitualArmed = false;
            SyncLaw();

            Announce(RageRitualComplete
                ? "【晋升仪式·狂乱法师】三场被你改写过的仪式都活了下来——狂乱法师的门开了。"
                : $"【晋升仪式·狂乱法师】第 {RageRitualProgress}/{RageRitualTarget} 场仪式活了下来。");
        }

        // ── 序列二 熵之公爵 ───────────────────────────────────────

        /// <summary>
        /// 熵：交战中每三秒叠一层。层数越高，你自己越强，周围的敌人越乱——
        /// 但这是一笔赌注，兑现之后有五秒钟的空虚。
        /// </summary>
        private void UpdateEntropy()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            bool inCombat = false;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!CanAffectEnemy(npc)) continue;
                if (npc.Distance(Player.Center) > EntropyCombatRadius) continue;
                inCombat = true;
                break;
            }
            if (!inCombat) { entropyTimer = 0; return; }

            if (++entropyTimer < EntropyGainInterval) return;
            entropyTimer = 0;
            if (entropyStacks >= EntropyMaxStacks) return;

            entropyStacks++;
            ScatterEntropy();
            if (entropyStacks >= EntropyMaxStacks)
                Announce("【熵】已经乱到极限了——再乱下去，连你自己也说不清会发生什么。");
        }

        /// <summary>每叠一层，周围的东西就更不讲道理一点；层数很高时会有东西陷入寂灭。</summary>
        private void ScatterEntropy()
        {
            int[] bad = { BuffID.Weak, BuffID.Slow, BuffID.Confused, BuffID.BrokenArmor,
                BuffID.Poisoned, BuffID.OnFire3, BuffID.Ichor, BuffID.Cursed };

            NPC doomed = null;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!CanAffectEnemy(npc)) continue;
                if (npc.Distance(Player.Center) > EntropyCombatRadius) continue;

                npc.AddBuff(bad[Main.rand.Next(bad.Length)], 5 * 60);
                if (BlackEmperorMath.IsBoss(npc))
                {
                    // 许多 Boss 免疫原版减益；额外写入自身状态，保证这一层熵不是空效果。
                    var state = npc.GetGlobalNPC<BlackEmperorGlobalNPC>();
                    state.chaosTicks = Math.Max(state.chaosTicks, 90);
                    state.weakenedTicks = Math.Max(state.weakenedTicks, 90);
                }
                npc.netUpdate = true;
                if (Main.rand.NextBool(6)) doomed = npc;
            }

            // 层数够高时，总有某个倒霉的东西「寂灭」在原地。
            if (entropyStacks >= 10 && doomed != null)
            {
                doomed.GetGlobalNPC<BlackEmperorGlobalNPC>().boundTicks = Math.Max(
                    doomed.GetGlobalNPC<BlackEmperorGlobalNPC>().boundTicks, 60);
                doomed.netUpdate = true;
            }
        }

        /// <summary>
        /// 主动·兑现：把攒下来的乱一次性掷出去。
        /// 伤害随层数递增，另外还会触发一个随机结果——层数越高越强，
        /// 但兑现之后五秒里灵性回复减半。这是赌博，不是无脑叠层。
        /// </summary>
        private void ExecuteEntropyCashIn(Vector2 aimPoint)
        {
            // 序列三就能用：晋升仪式要凭它完成；踏入序列二之后，威力照旧随序列提升。
            if (Sequence > 3) { Feedback("【兑现】走到序列三才摸得到这份权柄。"); return; }
            if (entropyCooldown > 0) { Feedback($"【兑现】尚在冷却：{entropyCooldown / 60f:F1} 秒。"); return; }
            if (entropyStacks <= 0) { Feedback("【兑现】你身上一点熵都没攒下。"); return; }

            float cost = EntropyCost;
            if (!Player.GetModPlayer<LotMPlayer>().TryConsumeSpirituality(cost, true))
            {
                Feedback($"【兑现】灵性不足，需要 {cost:F0} 点。");
                return;
            }

            int stacks = entropyStacks;
            entropyStacks = 0;
            entropyTimer = 0;
            entropyCooldown = SkillCooldown;
            entropyBacklashTicks = EntropyBacklashDuration;
            SyncLaw();

            // 满层在城里引爆，就是序列二仪式要的那一下「从内部崩塌」。
            TryCompleteEntropyRitual(stacks);

            int damage = BlackEmperorMath.ScaledDamage(120 * stacks, Sequence);
            int affected = 0;
            int radius = (int)EntropyCashInRadius;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!CanAffectEnemy(npc)) continue;
                if (npc.Distance(Player.Center) > radius) continue;

                npc.SimpleStrikeNPC(damage, npc.Center.X >= Player.Center.X ? 1 : -1, false, 6f, DamageClass.Generic, false);
                affected++;
            }

            // 随机结果：掷出什么就是什么。
            string outcome = RollEntropyOutcome(radius);

            if (!Main.dedServ)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.6f, Pitch = -0.5f }, Player.Center);
                for (int i = 0; i < 26; i++)
                {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(radius * 0.8f, radius * 0.8f);
                    Dust dust = Dust.NewDustPerfect(Player.Center + offset, DustID.PurpleCrystalShard,
                        -offset.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(2f, 5f), 120, default, 1.1f);
                    dust.noGravity = true;
                }
            }

            Announce($"【兑现】{stacks} 层熵一次掷出：{affected} 个目标被卷进去，{outcome}");
            Announce("【代价】接下来五秒里，你的灵性回复只有一半。");
        }

        /// <summary>兑现时掷出的那个随机结果。</summary>
        private string RollEntropyOutcome(float radius)
        {
            int roll = Main.rand.Next(5);
            switch (roll)
            {
                case 0:
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!CanAffectEnemy(npc) || npc.Distance(Player.Center) > radius) continue;
                        var state = npc.GetGlobalNPC<BlackEmperorGlobalNPC>();
                        state.boundTicks = Math.Max(state.boundTicks, BlackEmperorGlobalNPC.BoundDuration);
                        npc.netUpdate = true;
                    }
                    return "周围的敌人被按在原地「寂灭」了三秒";
                case 1:
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!CanAffectEnemy(npc) || npc.Distance(Player.Center) > radius) continue;
                        npc.GetGlobalNPC<BlackEmperorGlobalNPC>().rageInfightTicks = 8 * 60;
                        npc.netUpdate = true;
                    }
                    return "秩序被打散，它们开始不分敌我";
                case 2:
                    Player.statLife = Math.Min(Player.statLifeMax2, Player.statLife + (int)(Player.statLifeMax2 * 0.25f));
                    CombatText.NewText(Player.getRect(), new Color(190, 150, 230), "+25%", true);
                    return "混乱里反而捞回了一条命";
                case 3:
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!CanAffectEnemy(npc) || npc.Distance(Player.Center) > radius) continue;
                        float push = BlackEmperorMath.IsBoss(npc) ? 3.5f : 14f;
                        npc.velocity += (npc.Center - Player.Center).SafeNormalize(Vector2.UnitX) * push;
                        npc.AddBuff(BuffID.OnFire3, 8 * 60);
                        npc.netUpdate = true;
                    }
                    return "尺度崩了，周围的东西被远远推开";
                default:
                    Player.AddBuff(BuffID.Rage, 10 * 60);
                    Player.AddBuff(BuffID.Wrath, 10 * 60);
                    Player.AddBuff(BuffID.Ironskin, 10 * 60);
                    return "熵替你换来了三种增益";
            }
        }

        /// <summary>满层熵在城里引爆，就算把一个国度从内部推了一把。</summary>
        private void TryCompleteEntropyRitual(int stacks)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (entropyRitualBurst || stacks < EntropyMaxStacks) return;
            if (!EntropyRitualSceneReady) return;   // 满层 + 在城镇里 + 乱世之夜

            entropyRitualBurst = true;
            SyncLaw();
            Announce("【晋升仪式·熵之公爵】乱世里，满层的熵从城镇的心脏炸开——这个国度开始从内部崩塌了。");
        }

        /// <summary>极致利用：空中再按一次跳跃，就能踩着规则本身往前冲。</summary>
        private void UpdateAirDash()
        {
            if (Player.whoAmI != Main.myPlayer) return;
            if (airDashCooldown > 0 || Player.dead) return;
            if (Player.velocity.Y == 0f || Player.mount.Active) return;
            if (!Player.controlJump || Player.releaseJump) return;

            float direction = 0f;
            if (Player.controlLeft) direction -= 1f;
            if (Player.controlRight) direction += 1f;
            if (direction == 0f) direction = Player.direction;

            Player.velocity = new Vector2(direction * 15f, -2.5f);
            airDashCooldown = 75;
            Player.noFallDmg = true;

            if (!Main.dedServ)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.DoubleJump with { Volume = 0.4f, Pitch = 0.2f }, Player.Center);
                for (int i = 0; i < 10; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Player.Center, DustID.PurpleCrystalShard,
                        new Vector2(-direction * Main.rand.NextFloat(1f, 3f), Main.rand.NextFloat(-1f, 1f)), 120, default, 1f);
                    dust.noGravity = true;
                }
            }
        }

        // ── 序列一 弑序亲王 ───────────────────────────────────────

        public static string DefinitionName(BlackEmperorDefinition definition) => definition switch
        {
            BlackEmperorDefinition.Bribe => "贿赂",
            BlackEmperorDefinition.Corrupt => "腐败者",
            BlackEmperorDefinition.StandIn => "替身",
            _ => "敌人"
        };

        public static string DefinitionEffect(BlackEmperorDefinition definition) => definition switch
        {
            BlackEmperorDefinition.Bribe => "你的每一次攻击都被当成一笔贿赂：命中即削弱目标，贿赂时间也更长",
            BlackEmperorDefinition.Corrupt => "被你碰过的敌人从此是「腐败者」：挨打更痛，死后掉出更多",
            BlackEmperorDefinition.StandIn => "制造一具与你相同、能自行行走和跳跃的替身；致命伤到来时由它真正死去，九十秒后重塑",
            _ => "说谁是敌人，谁就是敌人：你对它的伤害提高一成"
        };

        private int FindStandInProjectile()
        {
            int type = ModContent.ProjectileType<StandInProjectile>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner == Player.whoAmI && projectile.type == type)
                    return projectile.whoAmI;
            }
            return -1;
        }

        private void UpdateStandIn()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            bool shouldExist = Player.active && !Player.dead && Sequence <= 1 &&
                definitionMode == (int)BlackEmperorDefinition.StandIn && standInCooldown <= 0;
            int existing = FindStandInProjectile();
            if (!shouldExist)
            {
                if (existing >= 0) Main.projectile[existing].Kill();
                return;
            }
            if (existing >= 0) return;

            Vector2 spawn = Player.Bottom + new Vector2(-Player.direction * 72f, -42f);
            int index = Projectile.NewProjectile(Player.GetSource_FromThis(), spawn, Vector2.Zero,
                ModContent.ProjectileType<StandInProjectile>(), 0, 0f, Player.whoAmI);
            if (index >= 0 && index < Main.maxProjectiles)
            {
                Main.projectile[index].direction = Main.projectile[index].spriteDirection = Player.direction;
                Main.projectile[index].netUpdate = true;
            }
        }

        private bool SacrificeStandIn()
        {
            int index = FindStandInProjectile();
            if (index < 0) return false;

            Projectile standIn = Main.projectile[index];
            standIn.ai[1] = 1f;
            standIn.netUpdate = true;
            standIn.Kill();
            return true;
        }

        /// <summary>
        /// 定义：换一个说法，秩序就照着你的说法走。
        /// 同一时间只有一条定义生效，换词条就是重新解释这个世界。
        /// </summary>
        private void CycleDefinition()
        {
            if (Sequence > 1) { Feedback("【定义】走到序列一「弑序亲王」才说得动这个世界。"); return; }
            definitionMode = (definitionMode + 1) % DefinitionCount;
            var definition = (BlackEmperorDefinition)definitionMode;
            SyncLaw();
            Announce($"【定义】从现在起，「{DefinitionName(definition)}」由你说了算——{DefinitionEffect(definition)}。");
        }

        /// <summary>定义·替身：场上必须真的存在替身，致命伤才会改判给它。</summary>
        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore,
            ref Terraria.DataStructures.PlayerDeathReason damageSource)
        {
            if (Sequence > 1) return true;
            if (definitionMode != (int)BlackEmperorDefinition.StandIn || standInCooldown > 0) return true;
            if (!SacrificeStandIn()) return true;

            standInCooldown = StandInCooldown;
            Player.statLife = Math.Max(1, Player.statLifeMax2 / 5);
            Player.immune = true;
            Player.immuneTime = Math.Max(Player.immuneTime, 120);
            Player.hurtCooldowns[0] = Math.Max(Player.hurtCooldowns[0], 120);
            Player.hurtCooldowns[1] = Math.Max(Player.hurtCooldowns[1], 120);
            CombatText.NewText(Player.getRect(), new Color(200, 170, 240), "替身", true);
            SyncLaw();
            Announce("【定义·替身】替身在你眼前真正死去；世界据此撤销了你的死亡。九十秒后才能重塑。");

            return false;
        }

        /// <summary>以自身秩序取代原本的秩序：空手在城里把一条律令走完，一天只算一次。</summary>
        private void TryCountUsurpRitual()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (UsurpRitualComplete || Sequence > 1) return;
            if (usurpRitualLastDay == usurpDayIndex) return;                 // 今天已经记过
            if (!Player.HeldItem.IsAir) return;                              // 手里得空着：不是最强者的姿态
            if (CountNearbyTownNpcs(600f) < UsurpRitualTownNpcs) return;     // 得在城里

            usurpRitualDays++;
            usurpRitualLastDay = usurpDayIndex;
            SyncLaw();
            Announce(UsurpRitualComplete
                ? "【晋升仪式·弑序亲王】三个昼夜之后，这片地方改口叫你定的规矩了。"
                : $"【晋升仪式·弑序亲王】第 {usurpRitualDays}/{UsurpRitualTarget} 个昼夜归你管了。");
        }

        /// <summary>数日子用的：每过一个白天算一天。</summary>
        private void TickUsurpDays()
        {
            if (Main.dayTime && !usurpWasDay) usurpDayIndex++;
            usurpWasDay = Main.dayTime;
        }

        private void ToggleLaw()
        {
            if (activeLaw != BlackEmperorLaw.None)
            {
                EndLaw(true);
                return;
            }
            if (lawCooldown > 0)
            {
                Feedback($"【契约】尚在冷却：{lawCooldown / 60f:F1} 秒。");
                return;
            }

            // 使用技能要消耗灵性：立契约本身就要付出代价。
            LotMPlayer lotm = Player.GetModPlayer<LotMPlayer>();
            float cost = LawCost;
            if (!lotm.TryConsumeSpirituality(cost, true))
            {
                Feedback($"【契约】灵性不足，需要 {cost:F0} 点。");
                return;
            }

            var laws = AvailableLaws();
            if (laws.Length == 0) return;
            lawChoice = Math.Clamp(lawChoice, 0, laws.Length - 1);
            BlackEmperorLaw law = laws[lawChoice];

            // 审判需要一个被告。
            if (law == BlackEmperorLaw.Trial)
            {
                NPC target = FindTrialTarget();
                if (target == null)
                {
                    Feedback("【审判】把鼠标指向 600 像素以内的敌人，再宣告这条律令。");
                    return;
                }
                trialTarget = target.whoAmI;
                trialHits = 0;
            }

            activeLaw = law;
            // 立法者的名义会让律令站得更久。
            lawTimer = (int)(LawDuration * (HasTitle(BlackEmperorTitle.Legislator) ? 1.25f : 1f) *
                (Sequence <= 1 ? 1.5f : 1f));
            lawAnchor = Player.Center;
            CountLawDeclared();
            Announce($"【契约·{LawName(law)}】律令已经立下，守住它。");
        }

        /// <summary>亵渎之牌在身时，黑皇帝的一切消耗打八折（与门途径的牌同一规矩）。</summary>
        public float AdjustedCost(float cost)
            => Player.GetModPlayer<LotMPlayer>().isBlackEmperorCardEquipped ? cost * 0.8f : cost;

        /// <summary>开发者道具用：把所有仪式标记拉满（后续序列的仪式做好后在这里补）。</summary>
        public void CompleteAllRitualsForDebug()
        {
            // 序列九只有契约，没有需要标记的仪式；序列八的「以力破法」直接给上。
            lawBreakerReady = true;
            lawChoice = 0;
            // 序列五的晋升仪式：城市地底的秩序。
            chaosRitualNights = ChaosRitualNightTarget;
            chaosRitualKills = ChaosRitualKillsPerNight;
            chaosRitualComplete = true;
            // 序列四的晋升仪式：拉拢七个人 + 在城里推行过一项政策。
            corruptedTownCount = CorruptedTownTarget;
            policyImplemented = true;
            // 序列三的晋升仪式：三场活下来的仪式。
            rageRitualCount = RageRitualTarget;
            // 开发者道具：把所有称号一并解锁，方便直接试效果。
            titlesMask = (1 << BlackEmperorTitles.Count) - 1;
            // 序列二的晋升仪式：默认也一并拉满。
            entropyRitualBurst = true;
            // 序列一的晋升仪式：三个昼夜也一并算上。
            usurpRitualDays = UsurpRitualTarget;
        }

        /// <summary>开发者道具用：清空全部冷却。</summary>
        public void ResetAllCooldowns()
        {
            lawCooldown = 0;
            bribeCooldown = 0;
            bruteCooldown = 0;
            twistCooldown = 0;
            chaosCooldown = 0;
            giftCooldown = 0;
            amplifyCooldown = 0;
            rageCooldown = 0;
        }

        private NPC FindTrialTarget()
        {
            NPC best = null;
            float bestDistance = 600f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!CanAffectEnemy(npc)) continue;
                float distance = npc.Distance(Main.MouseWorld);
                if (distance < bestDistance) { bestDistance = distance; best = npc; }
            }
            return best;
        }

        /// <summary>主动收起（不罚）。</summary>
        private void EndLaw(bool announce)
        {
            if (activeLaw == BlackEmperorLaw.None) return;
            BlackEmperorLaw previous = activeLaw;
            activeLaw = BlackEmperorLaw.None;
            lawTimer = 0;
            trialTarget = -1;
            trialHits = 0;
            lawCooldown = (int)(LawCooldown * (HasTitle(BlackEmperorTitle.Legislator) ? 0.7f : 1f) *
                (Sequence <= 1 ? 0.8f : 1f));
            if (announce) Announce($"【契约·{LawName(previous)}】律令解除。");
            SyncLaw();
        }

        /// <summary>违约：吃反噬；序列八之后可以用「以力破法」免掉一次。</summary>
        private void BreakLaw(string reason)
        {
            BlackEmperorLaw broken = activeLaw;
            bool negated = lawBreakerReady;
            EndLaw(false);

            if (negated)
            {
                lawBreakerReady = false;
                Announce($"【以力破法】{reason}——野蛮人不需要遵守规矩。");
                return;
            }

            // 反噬：损失 15% 最大生命并减速。
            int penalty = Math.Max(1, (int)(Player.statLifeMax2 * 0.15f));
            Player.statLife -= penalty;
            Player.AddBuff(BuffID.Slow, 5 * 60);
            CombatText.NewText(Player.getRect(), new Color(220, 120, 120), $"-{penalty}", true);
            if (Player.statLife <= 0) Player.KillMe(Terraria.DataStructures.PlayerDeathReason.ByCustomReason(
                Terraria.Localization.NetworkText.FromLiteral($"{Player.name} 被自己立下的《{LawName(broken)}》反噬。")), 10, 0);
            Announce($"【违约】{reason}律令反噬！");
        }

        // ── 契约效果挂钩 ─────────────────────────────────────────

        /// <summary>禁足守约：近战攻击被完全格挡，并把 60% 伤害反弹给攻击者。</summary>
        public override bool FreeDodge(Player.HurtInfo info)
        {
            // 混乱场：站在这块地里，攻击很容易「找错地方」。
            // 圆心就在自己脚下，所以只要这块地还在，人就在正中央。
            if (chaosFieldActive && chaosFieldTimer > 0 && Main.rand.NextFloat() < ChaosEvadeChance)
            {
                CombatText.NewText(Player.getRect(), new Color(200, 150, 220), "错判", true);
                if (Main.netMode != NetmodeID.Server && !Main.dedServ)
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.3f, Pitch = 0.5f }, Player.Center);
                return true;
            }

            if (activeLaw != BlackEmperorLaw.StayPut || lawTimer <= 0) return false;
            // 只格挡来自实体的直接攻击（近战/接触），环境伤害照旧。
            if (!info.DamageSource.TryGetCausingEntity(out Entity entity)) return false;
            if (entity is not NPC npc || !npc.active) return false;

            int reflect = Math.Max(1, (int)(info.Damage * 0.6f));
            npc.SimpleStrikeNPC(reflect, -info.HitDirection, false, 3f, DamageClass.Generic, false);
            CombatText.NewText(Player.getRect(), new Color(230, 210, 140), "格挡", true);
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.4f, Pitch = 0.3f }, Player.Center);
            return true;
        }

        /// <summary>等价交换：造成伤害的一部分转为治疗，代价在 ModifyHurt 里。</summary>
        public override void OnHurt(Player.HurtInfo info)
        {
            if (info.Damage <= 0) return;

            // 扭曲概念：被概念绑住的目标替你担掉一半。
            NPC concept = FindConceptNpc();
            if (concept != null)
            {
                int half = Math.Max(1, info.Damage / 2);
                concept.SimpleStrikeNPC(half, info.HitDirection, false, 0f, DamageClass.Generic, false);
                CombatText.NewText(concept.getRect(), new Color(200, 150, 220), $"承担 {half}", false);
            }

            // 关联：把另一半伤害转嫁给被买通的目标。
            NPC linked = FindLinkedNpc();
            if (linked != null && concept == null)
            {
                int shared = Math.Max(1, info.Damage / 2);
                linked.SimpleStrikeNPC(shared, info.HitDirection, false, 0f, DamageClass.Generic, false);
                CombatText.NewText(linked.getRect(), new Color(210, 170, 120), $"承担 {shared}", false);
            }

            if (activeLaw != BlackEmperorLaw.EquivalentExchange || lawTimer <= 0) return;
            int extra = Math.Max(1, (int)(info.Damage * 0.2f * BlackEmperorMath.SequencePower(Sequence)));
            Player.statLife = Math.Min(Player.statLifeMax2, Player.statLife + extra);
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            // 秩序威压：牌中的规则先替持有者削去一成最终伤害。
            if (CardOrderPressureActive)
                modifiers.FinalDamage *= 1f - CardOrderPressureDamageReduction;

            if (lawTimer > 0 && activeLaw == BlackEmperorLaw.EquivalentExchange) modifiers.FinalDamage *= 1.2f;

            // 扭曲概念优先：概念绑住的目标替你担掉一半，就不再和被买通的人重复计算。
            if (FindConceptNpc() != null) modifiers.FinalDamage *= 0.5f;
            else if (FindLinkedNpc() != null) modifiers.FinalDamage *= 0.5f;
        }

        /// <summary>被动·抓漏洞：攻击带有任意减益的敌人时伤害提高；秩序倾覆时改看减益层数。</summary>
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Sequence > 9) return;

            // 进入威压范围的敌人被迫服从持有者的秩序。限定距离让玩家需要主动控场，
            // 而不是把亵渎之牌做成另一条无条件全局增伤。
            if (CardOrderPressureActive && target.Distance(Player.Center) <= CardOrderPressureRadius)
                modifiers.FinalDamage *= 1f + CardOrderPressureDamageBonus;

            // 猎王者：这一拳对 Boss 格外重。
            if (Worn == BlackEmperorTitle.KingSlayer && BlackEmperorMath.IsBoss(target))
                modifiers.FinalDamage *= 1.12f;

            // 定义：说谁是敌人，谁就是敌人；说谁是腐败者，它就更经不起打。
            if (Sequence <= 1)
            {
                if (definitionMode == (int)BlackEmperorDefinition.Enemy) modifiers.FinalDamage *= 1.10f;
                if (target.GetGlobalNPC<BlackEmperorGlobalNPC>().Condemned) modifiers.FinalDamage *= 1.10f;
            }

            // 放大：把这一击的分量重新说一遍，用完就没了。
            if (amplifyReady)
            {
                amplifyReady = false;
                if ((BlackEmperorAmplifyMode)amplifyMode == BlackEmperorAmplifyMode.Execute)
                {
                    // 处决：目标越虚弱，这一下越接近「了结」。
                    float missing = 1f - Math.Clamp((float)target.life / Math.Max(1, target.lifeMax), 0f, 1f);
                    float bonus = BlackEmperorMath.IsBoss(target) ? 1f + missing * 1.2f : 1f + missing * 3f;
                    modifiers.FinalDamage *= bonus;
                    CombatText.NewText(target.getRect(), new Color(230, 200, 140), "处决", true);
                }
                else
                {
                    // 束缚：隔着距离的一个拥抱。
                    target.GetGlobalNPC<BlackEmperorGlobalNPC>().boundTicks = BlackEmperorGlobalNPC.BoundDuration;
                    target.netUpdate = true;
                    CombatText.NewText(target.getRect(), new Color(206, 176, 230), "束缚", true);
                }
            }

            if (activeLaw == BlackEmperorLaw.OrderOverthrow && lawTimer > 0)
            {
                int stacks = CountDebuffs(target);
                if (stacks > 0) modifiers.FinalDamage *= 1f + 0.12f * stacks * BlackEmperorMath.SequencePower(Sequence);
                return;
            }
            if (CountDebuffs(target) > 0) modifiers.FinalDamage *= 1.15f;
        }

        /// <summary>审判：统计命中被告的次数。</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 定义：把这一击解释成别的什么。
            if (Sequence <= 1 && CanAffectEnemy(target))
            {
                var definitionState = target.GetGlobalNPC<BlackEmperorGlobalNPC>();
                if (definitionMode == (int)BlackEmperorDefinition.Bribe)
                    definitionState.weakenedTicks = Math.Max(definitionState.weakenedTicks, 5 * 60);
                else if (definitionMode == (int)BlackEmperorDefinition.Corrupt)
                    definitionState.Condemned = true;
                target.netUpdate = true;
            }

            if (activeLaw != BlackEmperorLaw.Trial || lawTimer <= 0) return;
            if (target.whoAmI != trialTarget) return;
            trialHits++;
            if (trialHits < 5) return;

            // 判决成立：真实伤害 + 定罪（掉落提高）。
            int verdict = BlackEmperorMath.ScaledDamage(400, Sequence);
            target.SimpleStrikeNPC(verdict, hit.HitDirection, true, 8f, DamageClass.Generic, false);
            target.GetGlobalNPC<BlackEmperorGlobalNPC>().Condemned = true;
            Announce($"【审判】判决成立，{target.GivenOrTypeName} 已被定罪。");
            EndLaw(false);
        }

        private static int CountDebuffs(NPC npc)
        {
            int count = 0;
            for (int i = 0; i < npc.buffType.Length; i++)
            {
                int type = npc.buffType[i];
                if (type <= 0) continue;
                if (type < Main.debuff.Length && Main.debuff[type]) count++;
            }
            return count;
        }

        // ── 反馈与同步 ───────────────────────────────────────────
        private void Feedback(string message)
        {
            if (feedbackCooldown > 0) return;
            feedbackCooldown = 45;
            Announce(message);
        }

        private void Announce(string message)
        {
            if (Main.netMode == NetmodeID.Server)
                Terraria.Chat.ChatHelper.SendChatMessageToClient(
                    Terraria.Localization.NetworkText.FromLiteral(message), new Color(210, 170, 120), Player.whoAmI);
            else Main.NewText(message, 210, 170, 120);
        }

        public void SyncLaw(int toWho = -1, int fromWho = -1)
        {
            if (Main.netMode == NetmodeID.SinglePlayer) return;
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)LotMNetMsg.BlackEmperorStateSync);
            packet.Write((byte)Player.whoAmI);
            packet.Write((byte)activeLaw);
            packet.Write((short)Math.Clamp(lawTimer, 0, short.MaxValue));
            packet.Write((short)Math.Clamp(lawCooldown, 0, short.MaxValue));
            packet.WriteVector2(lawAnchor);
            packet.Write((byte)lawChoice);
            packet.Write(lawBreakerReady);
            packet.Write((short)Math.Clamp(twistCooldown, 0, short.MaxValue));
            // 序列五：混乱场与扭曲概念的状态
            packet.Write(chaosFieldActive);
            packet.Write((short)Math.Clamp(chaosFieldTimer, 0, short.MaxValue));
            packet.WriteVector2(chaosFieldCenter);
            packet.Write((short)Math.Clamp(conceptTarget + 1, 0, short.MaxValue));   // +1 让「没有目标」(-1) 也能原样传过去
            packet.Write((short)Math.Clamp(conceptTicks, 0, short.MaxValue));
            // 晋升仪式：城市地底的秩序
            packet.Write((byte)Math.Clamp(chaosRitualNights, 0, 255));
            packet.Write((short)Math.Clamp(chaosRitualKills, 0, short.MaxValue));
            packet.Write(chaosRitualComplete);

            // 序列四：赠予 / 放大 / 晋升仪式
            packet.Write((byte)Math.Clamp(giftMode, 0, 255));
            packet.Write((byte)Math.Clamp(amplifyMode, 0, 255));
            packet.Write(amplifyReady);
            packet.Write(policyImplemented);
            packet.Write((byte)Math.Clamp(corruptedTownTypes.Count, 0, 255));

            // 序列三：狂乱与它的仪式
            packet.Write((byte)Math.Clamp(rageRitualBosses.Count, 0, 255));
            packet.Write(rageRitualArmed);

            // 名义：称号位图、当前称号与各条进度
            packet.Write(titlesMask);
            packet.Write((byte)Math.Clamp(wornTitle, 0, 255));
            packet.Write((short)Math.Clamp(mushroomsGathered, 0, short.MaxValue));
            packet.Write((short)Math.Clamp(fishCaught, 0, short.MaxValue));
            packet.Write((short)Math.Clamp(dragonsSlain, 0, short.MaxValue));
            packet.Write((short)Math.Clamp(tilesMined, 0, short.MaxValue));
            packet.Write((short)Math.Clamp(bloodMoonKills, 0, short.MaxValue));
            packet.Write((short)Math.Clamp(abyssKills, 0, short.MaxValue));
            packet.Write((short)Math.Clamp(kingKills, 0, short.MaxValue));
            packet.Write((short)Math.Clamp(lawsDeclared, 0, short.MaxValue));
            packet.Write((short)Math.Clamp(bribesCast, 0, short.MaxValue));
            packet.Write(invasionWon);
            packet.Write(moonlordSlain);

            // 序列二：熵层与它的仪式
            packet.Write((short)Math.Clamp(entropyStacks, 0, short.MaxValue));
            packet.Write(entropyRitualBurst);

            // 序列一：定义与仪式
            packet.Write((byte)Math.Clamp(definitionMode, 0, 255));
            packet.Write((short)Math.Clamp(standInCooldown, 0, short.MaxValue));
            packet.Write((short)Math.Clamp(usurpRitualDays, 0, short.MaxValue));

            packet.Send(toWho, fromWho);
        }

        public static void ReceiveLaw(BinaryReader reader, int whoAmI)
        {
            byte index = reader.ReadByte();
            var law = (BlackEmperorLaw)reader.ReadByte();
            int timer = reader.ReadInt16();
            int cooldown = reader.ReadInt16();
            Vector2 anchor = reader.ReadVector2();
            byte choice = reader.ReadByte();
            bool lawBreaker = reader.ReadBoolean();
            int twist = reader.ReadInt16();
            bool chaosActive = reader.ReadBoolean();
            int chaosTimer = reader.ReadInt16();
            Vector2 chaosCenter = reader.ReadVector2();
            int conceptNpc = reader.ReadInt16() - 1;
            int conceptTime = reader.ReadInt16();
            int ritualNights = reader.ReadByte();
            int ritualKills = reader.ReadInt16();
            bool ritualDone = reader.ReadBoolean();
            int gift = reader.ReadByte();
            int amplify = reader.ReadByte();
            bool amplified = reader.ReadBoolean();
            bool policy = reader.ReadBoolean();
            int corruptedTowns = reader.ReadByte();
            int rageCount = reader.ReadByte();
            bool rageArmed = reader.ReadBoolean();
            int titles = reader.ReadInt32();
            int worn = reader.ReadByte();
            int mushrooms = reader.ReadInt16();
            int fish = reader.ReadInt16();
            int dragons = reader.ReadInt16();
            int mined = reader.ReadInt16();
            int bloodKills = reader.ReadInt16();
            int abyss = reader.ReadInt16();
            int kings = reader.ReadInt16();
            int laws = reader.ReadInt16();
            int bribes = reader.ReadInt16();
            bool invasion = reader.ReadBoolean();
            bool moonlord = reader.ReadBoolean();
            int entropy = reader.ReadInt16();
            bool entropyBurst = reader.ReadBoolean();
            int definition = reader.ReadByte();
            int standIn = reader.ReadInt16();
            int usurpDays = reader.ReadInt16();

            if (index >= Main.maxPlayers) return;
            // 客户端只能提交自己的黑皇帝状态；包体已完整读取后再拒绝，避免读流错位。
            if (Main.netMode == NetmodeID.Server && index != whoAmI) return;
            var player = Main.player[index].GetModPlayer<BlackEmperorPlayer>();
            bool standInWasConsumed = standIn > player.standInCooldown && standIn > 0;
            if (standInWasConsumed) player.SacrificeStandIn();
            player.activeLaw = law;
            player.lawTimer = timer;
            player.lawCooldown = cooldown;
            player.lawAnchor = anchor;
            player.lawChoice = choice;
            player.lawBreakerReady = lawBreaker;
            player.twistCooldown = twist;
            player.chaosFieldActive = chaosActive;
            player.chaosFieldTimer = chaosTimer;
            player.chaosFieldCenter = chaosCenter;
            player.conceptTarget = conceptNpc;
            player.conceptTicks = conceptTime;
            player.chaosRitualNights = ritualNights;
            player.chaosRitualKills = ritualKills;
            player.chaosRitualComplete = ritualDone;
            player.giftMode = gift;
            player.amplifyMode = amplify;
            player.amplifyReady = amplified;
            player.policyImplemented = policy;
            player.corruptedTownCount = corruptedTowns;
            player.rageRitualCount = rageCount;
            player.rageRitualArmed = rageArmed;
            player.titlesMask = titles;
            player.wornTitle = worn;
            player.mushroomsGathered = mushrooms;
            player.fishCaught = fish;
            player.dragonsSlain = dragons;
            player.tilesMined = mined;
            player.bloodMoonKills = bloodKills;
            player.abyssKills = abyss;
            player.kingKills = kings;
            player.lawsDeclared = laws;
            player.bribesCast = bribes;
            player.invasionWon = invasion;
            player.moonlordSlain = moonlord;
            player.entropyStacks = entropy;
            player.entropyRitualBurst = entropyBurst;
            player.definitionMode = definition;
            player.standInCooldown = standIn;
            player.usurpRitualDays = usurpDays;

            if (Main.netMode == NetmodeID.Server)
                player.SyncLaw(-1, whoAmI);
        }

        public override void SaveData(TagCompound tag)
        {
            tag["BlackEmperorChaosRitualNights"] = chaosRitualNights;
            tag["BlackEmperorChaosRitualKills"] = chaosRitualKills;
            tag["BlackEmperorChaosRitualComplete"] = chaosRitualComplete;
            tag["BlackEmperorCorruptedTowns"] = corruptedTownTypes;
            tag["BlackEmperorPolicy"] = policyImplemented;
            tag["BlackEmperorRageBosses"] = rageRitualBosses;
            tag["BlackEmperorTitles"] = titlesMask;
            tag["BlackEmperorWornTitle"] = wornTitle;
            tag["BlackEmperorMushrooms"] = mushroomsGathered;
            tag["BlackEmperorFish"] = fishCaught;
            tag["BlackEmperorDragons"] = dragonsSlain;
            tag["BlackEmperorMined"] = tilesMined;
            tag["BlackEmperorBloodKills"] = bloodMoonKills;
            tag["BlackEmperorAbyssKills"] = abyssKills;
            tag["BlackEmperorKingKills"] = kingKills;
            tag["BlackEmperorLaws"] = lawsDeclared;
            tag["BlackEmperorBribes"] = bribesCast;
            tag["BlackEmperorInvasionWon"] = invasionWon;
            tag["BlackEmperorMoonlord"] = moonlordSlain;
            tag["BlackEmperorEntropyBurst"] = entropyRitualBurst;
            tag["BlackEmperorDefinition"] = definitionMode;
            tag["BlackEmperorUsurpDays"] = usurpRitualDays;
            tag["BlackEmperorUsurpLastDay"] = usurpRitualLastDay;
            tag["BlackEmperorUsurpDayIndex"] = usurpDayIndex;
        }

        public override void LoadData(TagCompound tag)
        {
            chaosRitualNights = tag.GetInt("BlackEmperorChaosRitualNights");
            chaosRitualKills = tag.GetInt("BlackEmperorChaosRitualKills");
            chaosRitualComplete = tag.GetBool("BlackEmperorChaosRitualComplete");
            corruptedTownTypes = tag.ContainsKey("BlackEmperorCorruptedTowns")
                ? tag.GetList<int>("BlackEmperorCorruptedTowns").Distinct().Take(CorruptedTownTarget).ToList()
                : new List<int>();
            corruptedTownCount = corruptedTownTypes.Count;
            policyImplemented = tag.GetBool("BlackEmperorPolicy");
            rageRitualBosses = tag.ContainsKey("BlackEmperorRageBosses")
                ? tag.GetList<int>("BlackEmperorRageBosses").Distinct().Take(RageRitualTarget).ToList()
                : new List<int>();
            rageRitualCount = rageRitualBosses.Count;
            titlesMask = Math.Max(1, tag.GetInt("BlackEmperorTitles"));
            wornTitle = tag.GetInt("BlackEmperorWornTitle");
            mushroomsGathered = tag.GetInt("BlackEmperorMushrooms");
            fishCaught = tag.GetInt("BlackEmperorFish");
            dragonsSlain = tag.GetInt("BlackEmperorDragons");
            tilesMined = tag.GetInt("BlackEmperorMined");
            bloodMoonKills = tag.GetInt("BlackEmperorBloodKills");
            abyssKills = tag.GetInt("BlackEmperorAbyssKills");
            kingKills = tag.GetInt("BlackEmperorKingKills");
            lawsDeclared = tag.GetInt("BlackEmperorLaws");
            bribesCast = tag.GetInt("BlackEmperorBribes");
            invasionWon = tag.GetBool("BlackEmperorInvasionWon");
            moonlordSlain = tag.GetBool("BlackEmperorMoonlord");
            entropyRitualBurst = tag.GetBool("BlackEmperorEntropyBurst");
            definitionMode = tag.GetInt("BlackEmperorDefinition");
            usurpRitualDays = tag.GetInt("BlackEmperorUsurpDays");
            usurpRitualLastDay = tag.GetInt("BlackEmperorUsurpLastDay");
            usurpDayIndex = tag.GetInt("BlackEmperorUsurpDayIndex");
        }


        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) => SyncLaw(toWho, fromWho);

        public override void CopyClientState(ModPlayer targetCopy)
        {
            var clone = (BlackEmperorPlayer)targetCopy;
            clone.activeLaw = activeLaw;
            clone.lawTimer = lawTimer;
            clone.lawCooldown = lawCooldown;
            clone.lawAnchor = lawAnchor;
            clone.lawChoice = lawChoice;
            clone.lawBreakerReady = lawBreakerReady;
            clone.trialTarget = trialTarget;
            clone.trialHits = trialHits;
            clone.twistCooldown = twistCooldown;
            clone.chaosFieldActive = chaosFieldActive;
            clone.chaosFieldTimer = chaosFieldTimer;
            clone.chaosFieldCenter = chaosFieldCenter;
            clone.conceptTarget = conceptTarget;
            clone.conceptTicks = conceptTicks;
            clone.chaosRitualNights = chaosRitualNights;
            clone.chaosRitualKills = chaosRitualKills;
            clone.chaosRitualComplete = chaosRitualComplete;
            clone.giftMode = giftMode;
            clone.amplifyMode = amplifyMode;
            clone.amplifyReady = amplifyReady;
            clone.policyImplemented = policyImplemented;
            clone.corruptedTownCount = CorruptedTownCount;
            clone.rageRitualCount = RageRitualProgress;
            clone.rageRitualArmed = rageRitualArmed;
            clone.titlesMask = titlesMask;
            clone.wornTitle = wornTitle;
            clone.mushroomsGathered = mushroomsGathered;
            clone.fishCaught = fishCaught;
            clone.dragonsSlain = dragonsSlain;
            clone.tilesMined = tilesMined;
            clone.bloodMoonKills = bloodMoonKills;
            clone.abyssKills = abyssKills;
            clone.kingKills = kingKills;
            clone.lawsDeclared = lawsDeclared;
            clone.bribesCast = bribesCast;
            clone.invasionWon = invasionWon;
            clone.moonlordSlain = moonlordSlain;
            clone.entropyStacks = entropyStacks;
            clone.entropyRitualBurst = entropyRitualBurst;
            clone.definitionMode = definitionMode;
            clone.standInCooldown = standInCooldown;
            clone.usurpRitualDays = usurpRitualDays;

        }

        public override void SendClientChanges(ModPlayer clientPlayer)
        {
            var old = (BlackEmperorPlayer)clientPlayer;
            // 只比「状态」不比「计时」：lawTimer / lawCooldown 每 tick 都在变，
            // 把它们放进来会让每 tick 都发一次包。倒计时各端自己走，到点了各自收尾。
            if (old.activeLaw != activeLaw || old.lawAnchor != lawAnchor ||
                old.lawChoice != lawChoice || old.lawBreakerReady != lawBreakerReady)
                SyncLaw();
            if (old.activeLaw != activeLaw || old.lawAnchor != lawAnchor ||
                old.lawChoice != lawChoice || old.lawBreakerReady != lawBreakerReady ||
                old.chaosFieldActive != chaosFieldActive || old.chaosFieldCenter != chaosFieldCenter ||
                old.conceptTarget != conceptTarget || old.chaosRitualNights != chaosRitualNights ||
                old.chaosRitualKills != chaosRitualKills || old.chaosRitualComplete != chaosRitualComplete ||
                old.giftMode != giftMode || old.amplifyMode != amplifyMode ||
                old.amplifyReady != amplifyReady || old.policyImplemented != policyImplemented ||
                old.corruptedTownCount != CorruptedTownCount ||
                old.rageRitualCount != RageRitualProgress || old.rageRitualArmed != rageRitualArmed)
                SyncLaw();

            if (old.activeLaw != activeLaw || old.lawAnchor != lawAnchor ||
                old.lawChoice != lawChoice || old.lawBreakerReady != lawBreakerReady ||
                old.chaosFieldActive != chaosFieldActive || old.chaosFieldCenter != chaosFieldCenter ||
                old.conceptTarget != conceptTarget || old.chaosRitualNights != chaosRitualNights ||
                old.chaosRitualKills != chaosRitualKills || old.chaosRitualComplete != chaosRitualComplete ||
                old.giftMode != giftMode || old.amplifyMode != amplifyMode ||
                old.amplifyReady != amplifyReady || old.policyImplemented != policyImplemented ||
                old.corruptedTownCount != CorruptedTownCount ||
                old.rageRitualCount != RageRitualProgress || old.rageRitualArmed != rageRitualArmed ||
                old.titlesMask != titlesMask || old.wornTitle != wornTitle ||
                old.entropyRitualBurst != entropyRitualBurst ||
                old.definitionMode != definitionMode || old.usurpRitualDays != usurpRitualDays ||
                (old.standInCooldown <= 0) != (standInCooldown <= 0))
            {
                SyncLaw();
                return;
            }

            // 计数类进度攒两秒再发一次：挖两千格不该发两千个包。
            if (++titleSyncTick < 120) return;
            titleSyncTick = 0;
            if (old.mushroomsGathered != mushroomsGathered || old.fishCaught != fishCaught ||
                old.dragonsSlain != dragonsSlain || old.tilesMined != tilesMined ||
                old.bloodMoonKills != bloodMoonKills || old.abyssKills != abyssKills ||
                old.kingKills != kingKills || old.lawsDeclared != lawsDeclared ||
                old.bribesCast != bribesCast || old.invasionWon != invasionWon ||
                old.moonlordSlain != moonlordSlain)
                SyncLaw();
        }

        public override void OnEnterWorld()
        {
            activeLaw = BlackEmperorLaw.None;
            lawTimer = lawCooldown = 0;
            trialTarget = -1;
            trialHits = 0;
            linkedNpc = -1;
            townFavorTimer = 0;
            chaosFieldActive = false;
            chaosFieldTimer = 0;
            conceptTarget = -1;
            conceptTicks = 0;
            giftCooldown = 0;
            amplifyCooldown = 0;
            amplifyReady = false;
            hoverTicks = 0;
            hoverCooldown = 0;
            rageCooldown = 0;
            rageRitualArmed = false;
            rageRitualFailed = false;
            titleSkillCooldown = 0;
            standInCooldown = 0;
            sporeTick = 0;
            worldTitleTick = 0;
        }
    }

    /// <summary>黑皇帝通用的强度换算：序列四为 1.0，与门途径的曲线对齐。</summary>
    public static class BlackEmperorMath
    {
        public static float SequencePower(int sequence) => sequence switch
        {
            <= 1 => 4.20f,
            2 => 2.60f,
            3 => 1.60f,
            4 => 1.00f,
            5 => 0.72f,
            6 => 0.55f,
            7 => 0.42f,
            8 => 0.32f,
            _ => 0.24f
        };

        public static int ScaledDamage(int baseDamage, int sequence)
            => Math.Max(1, (int)MathF.Round(baseDamage * SequencePower(sequence) *
                Systems.BalanceSystem.GetEffectiveWorldMultiplier()));

        /// <summary>
        /// 全局消耗系数：黑皇帝的手感偏贵，这里统一砍半。
        /// 之后要再调，只改这一个数就够了（0.5 = 原始值的一半）。
        /// </summary>
        public const float CostScale = 0.5f;

        /// <summary>
        /// 序列对应的技能基础灵性消耗（已计入 <see cref="CostScale"/>）。序列九最低，逐级上升，
        /// 大致维持「一次技能花掉当前灵性上限的一小部分」这个手感。
        /// </summary>
        public static float SequenceCost(int sequence) => CostScale * BaseSequenceCost(sequence);

        private static float BaseSequenceCost(int sequence) => sequence switch
        {
            <= 1 => 50000f,
            2 => 20000f,
            3 => 5000f,
            4 => 1200f,
            5 => 350f,
            6 => 300f,
            7 => 150f,
            8 => 100f,
            _ => 50f
        };

        /// <summary>统一的「这是 Boss 吗」判断，避免各处写法不一致。</summary>
        public static bool IsBoss(NPC npc) => npc.boss || npc.realLife >= 0 ||
            NPCID.Sets.ShouldBeCountedAsBoss[npc.type];
    }
}
