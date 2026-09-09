using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.GameContent.ItemDropRules;
using Terraria.Audio;
using zhashi;
using zhashi.Content;
using zhashi.Content.Projectiles;
using zhashi.Content.Items.Weapons;
using zhashi.Content.Buffs;
using zhashi.Content.Items;
using zhashi.Content.Projectiles.Demoness;
using zhashi.Content.Systems;
using ReLogic.Utilities;
using zhashi.Content.Configs;
using Terraria.Graphics.Effects;
using Terraria.Localization;
using Terraria.DataStructures;
using SubworldLibrary; 
using zhashi.Content.Dimensions; 
using zhashi.Content.DaqianLu;
using zhashi.Content.Buffs.Debuffs; 


namespace zhashi.Content
{
    public class LotMPlayer : ModPlayer
    {
        public override IEnumerable<Item> AddStartingItems(bool mediumCoreDeath)
        {
            return new[] {
                new Item(ModContent.ItemType<RoselleDiary>())
            };
        }
        public override void OnEnterWorld()
        {
            string name = Player.name;
            bool isGehrman = name.Contains("格尔曼") && name.Contains("斯帕罗");
            if (!isGehrman) isGehrman = name.ToLower().Contains("gehrman") && name.ToLower().Contains("sparrow");
            if (isGehrman)
            {
                if (!Player.HasItem(ModContent.ItemType<CreepingHunger>()))
                {
                    Player.QuickSpawnItem(Player.GetSource_GiftOrReward(), ModContent.ItemType<CreepingHunger>());
                    Main.NewText(Terraria.Localization.NetworkText.FromLiteral("疯狂的冒险家，你的手套归来了。"), new Microsoft.Xna.Framework.Color(180, 80, 255));
                }
            }
            if (name == "周明瑞" || name == "Klein Moretti")
            {
                if (!Player.HasItem(ModContent.ItemType<Items.Consumables.LuckEnhancementRitual>()))
                {
                    Player.QuickSpawnItem(Player.GetSource_GiftOrReward(), ModContent.ItemType<Items.Consumables.LuckEnhancementRitual>());

                    Main.NewText(Terraria.Localization.NetworkText.FromLiteral("一段古老的记忆在你脑海中苏醒..."), Color.LightGray);
                    Main.NewText(Terraria.Localization.NetworkText.FromLiteral("获得了 [i:" + ModContent.ItemType<Items.Consumables.LuckEnhancementRitual>() + "] 转运仪式"), Color.Gold);
                }
            }
        }

        // ==================== 仪式职业系统 ====================

        // 恢复计时器
        public bool ConsumePixels(int amount)
        {

            if (spiritualityCurrent >= amount)
            {
                spiritualityCurrent -= amount;
                return true;
            }
            return false;
        }

        public void AddPixels(int amount)
        {
            spiritualityCurrent += amount;

            if (spiritualityCurrent > spiritualityMax)
                spiritualityCurrent = spiritualityMax;

            if (amount > 0)
            {
                CombatText.NewText(Player.getRect(), new Color(180, 80, 255), $"+{amount}");
            }
        }
        // ======================================================

        // ===================================================
        // 1. 核心变量定义
        // ===================================================

        public int baseSequence = 10;
        public int baseHunterSequence = 10;
        public int baseMoonSequence = 10;
        public int baseFoolSequence = 10;
        public int baseMarauderSequence = 10;
        public int baseSunSequence = 10;
        public int baseDemonessSequence = 10;
        public int baseWheelSequence = 10;
        public int baseDoorSequence = 10;


        public int currentSequence = 10;       // 巨人途径 
        public int currentGiantSequence = 10;
        public int currentHunterSequence = 10; // 猎人途径 (9-1)
        public int currentMoonSequence = 10;   // 月亮途径 (9-1)
        public int currentFoolSequence = 10;   // 愚者途径 (9-1)
        public int currentMarauderSequence = 10; // 错误途径 (9-1)
        public int currentSunSequence = 10;    // 太阳途径 (9-1)
        public int currentDemonessSequence = 10; // 刺客途径
        public int currentWheelSequence = 10; // 命运途径
        public int currentDoorSequence = 10;  // 学徒/门途径

        // === 学徒/门 途径技能字段 ===
        public int doorOpenCooldown = 0;          // "开门"穿墙CD (180帧=3秒)
        public const int DOOR_OPEN_CD_MAX = 180;

        // === 序列8 戏法大师 ===
        public int selectedTrick = 0;             // 当前选中的戏法 0-11
        public int trickCooldown = 0;             // 戏法通用CD
        public const int TRICK_CD_MAX = 90;       // 1.5秒
        public int electricChargeTimer = 0;       // 电击附魔剩余帧数(下次近战感电)
        public int trickEscapeCooldown = 0;       // 逃脱戏法CD (自动)
        public const int TRICK_ESCAPE_CD_MAX = 1800; // 30秒
        public const int TRICK_COUNT = 12;        // 戏法总数

        // === 序列7 占星人 ===
        public int astrologyCooldown = 0;         // 占卜CD (45秒)
        public const int ASTROLOGY_CD_MAX = 2700;
        public int astrologyActiveTimer = 0;      // 占卜激活剩余 (15秒)
        public const int ASTROLOGY_DURATION = 900;
        public int astrologyIntuitionCooldown = 0; // 灵性直觉触发CD (60秒)
        public const int ASTROLOGY_INTUITION_CD = 3600;

        // === 序列6 记录官 - 学习蠕动饥饿: 存储具体敌人ID,使用时拟态该敌人能力 ===
        public List<int> recorderNormalList = new List<int>();   // 普通记录NPC ID列表(上限20)
        public const int RECORDER_NORMAL_MAX = 20;
        public List<int> recorderDivineList = new List<int>();   // 神性记录NPC ID列表(上限1)
        public const int RECORDER_DIVINE_MAX = 1;
        /// <summary>实际神性记录上限(序列5+提升到4)</summary>
        public int GetRecorderDivineMax() => recorderDivineMaxOverride > 0 ? recorderDivineMaxOverride : RECORDER_DIVINE_MAX;
        public int recorderSelectedNormal = 0;     // 当前选中的普通记录索引
        public int recorderSelectedDivine = 0;     // 当前选中的神性记录索引
        public int recorderCooldown = 0;           // 使用CD(10秒)
        public const int RECORDER_CD_MAX = 600;

        // === 序列5 旅行家 ===
        public int travelerGateCooldown = 0;      // 旅行家之门(远距传送)CD
        public const int TRAVELER_GATE_CD = 1800; // 30秒
        public int travelerBlinkCooldown = 0;     // 闪现CD
        public const int TRAVELER_BLINK_CD = 60;  // 1秒
        public bool travelerGateAwaitingMapClick = false; // 是否在等待地图点击
        // 序列5+: 神性记录上限提升到4
        public int recorderDivineMaxOverride = 0;  // >0时覆盖RECORDER_DIVINE_MAX
        public static readonly string[] TrickNames = {
            "闪光", "黑幕", "转移气体", "巨响", "冰冻射线", "电击",
            "造雾", "刮风", "点火", "摔倒术", "驱物", "逃脱戏法"
        };

        //亵渎之牌
        public bool isFoolCardEquipped = false;
        public bool isStrengthCardEquipped = false;
        public bool isAntiDivinationActive = false;
        public int blasphemyCardEquippedCount = 0;
        public bool isLoversCardEquipped = false;
        public bool isRedPriestCardEquipped = false;
        public bool isSunCardEquipped = false;
        public bool isMoonCardEquipped = false;
        public bool isDoorCardEquipped = false;
        public bool isWhiteTowerCardEquipped = false;
        public bool isVisionaryCardEquipped = false;
        public bool isBlackEmperorCardEquipped = false;
        public bool isTyrantCardEquipped = false;
        public bool isHangedManCardEquipped = false;
        public bool isDeathCardEquipped = false;
        public bool isDarknessCardEquipped = false;
        public bool isJusticiarCardEquipped = false;
        public bool isDemonessCardEquipped = false;
        public bool isAbyssCardEquipped = false;
        public bool isChainedCardEquipped = false;
        public bool isHermitCardEquipped = false;
        public bool isPerfectionistCardEquipped = false;
        public bool isMotherCardEquipped = false;
        public bool isWheelOfFortuneCardEquipped = false;



        public bool IsBeyonder => currentSequence < 10 || currentHunterSequence < 10 || currentMoonSequence < 10 || currentFoolSequence < 10 || currentMarauderSequence < 10 || currentSunSequence < 10 || currentDemonessSequence < 10 || currentWheelSequence < 10 || currentDoorSequence < 10;

        // 灵性系统
        public float spiritualityCurrent = 100;
        public int spiritualityMax = 100;
        public int spiritualityRegenTimer = 0; 

        // --- 巨人途径技能状态 ---
        public bool dawnArmorActive = false;
        public bool dawnArmorBroken = false;
        public int dawnArmorCurrentHP = 250;
        public int MaxDawnArmorHP => (int)(500 * GetSequenceMultiplier(currentSequence));
        public int dawnArmorCooldownTimer = 0;
        public const int DAWN_ARMOR_COOLDOWN_MAX = 900;
        public bool isGuardianStance = false;
        public bool isMercuryForm = false;
        public int twilightResurrectionCooldown = 0;
        public const int TWILIGHT_RESURRECTION_MAX = 18000;

        // --- 猎人途径技能状态 ---
        public bool isFlameCloakActive = false;
        public bool arsonistFireImmune = false;
        public bool isFireEnchanted = false;
        public int fireTeleportCooldown = 0;
        public const int FIRE_TELEPORT_MAX = 60;
        public int fireballChargeTimer = 0;
        public bool isChargingFireball = false;
        public bool isFireForm = false;
        public bool isArmyOfOne = false;
        public bool isCalamityGiant = false;
        public int glacierCooldown = 0;
        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        { 
            if (currentHunterSequence == 5)
            {
                if (ironBloodRitualProgress > 0)
                {
                    ironBloodRitualProgress = 0;
                    Main.NewText("你已死亡，铁血仪式进度归零...", 255, 50, 50);
                }
            }
        }

        // --- 月亮途径技能状态 ---
        public bool isTamingActive = false;
        public bool isVampireWings = false;    // 序列7：黑暗之翼
        public int abyssShackleCooldown = 0;   // 序列7：深渊枷锁
        public int elixirCooldown = 0;         // 序列6：生命灵液
        public bool isFullMoonActive = false;  // 序列5：满月领域
        public bool isMoonlightized = false;   // 序列5：月光化
        public bool isBatSwarm = false;        // 序列4：蝙蝠化身
        public int paperFigurineCooldown = 0;  // 序列4：月亮纸人
        public int darknessGazeCooldown = 0;   // 序列4：黑暗凝视
        public int summonGateCooldown = 0;     // 序列3：召唤之门
        public bool isCreationDomain = false;  // 序列2：创生领域
        public int purifyCooldown = 0;         // 序列2：净化大地

        // --- 愚者途径技能状态 ---
        public bool isSpiritVisionActive = false; // 灵视开关
        public int divinationCooldown = 0;        // 占卜冷却
        public int damageTransferCooldown = 0; // 伤害转移冷却
        public int flameJumpCooldown = 0;      // 火焰跳跃冷却
        public int distortCooldown = 0;           // 干扰直觉冷却
        public bool isFacelessActive = false;     // 无面伪装开关
        public int spiritThreadTargetIndex = -1; // 当前控制的目标 NPC 索引
        public int spiritThreadTimer = 0;        // 控制进度计时器
        public const int CONTROL_TIME_REQUIRED = 180; // 需要控制3秒 (3 * 60)
        public int swapCooldown = 0;              // 互换冷却
        public int spiritControlCooldown = 0;     // 控灵冷却
        public bool isSpiritForm = false;         // 是否处于灵体状态
        public int graftingMode = 0;              // 嫁接模式: 0=无, 1=反弹(空间), 2=必杀(攻击)
        public int graftingCooldown = 0;          // 嫁接冷却
        public int realmRange = 1500;             // 诡秘之境范围
        public bool waitingForTeleport = false; // 标记：是否正在等待点击传送
        public int spiritWorms = 50;              // 当前灵之虫数量
        public const int MAX_SPIRIT_WORMS = 50;
        public int wormRegenTimer = 0;            // 回复计时器
        public int historyUses = 0;               // 历史投影当前维持数量
        public int borrowUsesDaily = 0;           // 昨日重现今日已用次数
        public bool isBorrowingPower = false;     // 是否正在借用力量
        public int borrowTimer = 0;               // 借用力量剩余时间
        public int selectedWish = 0;              // 当前选择的愿望 (0-3)
        public int miracleCooldown = 0;           // 愿望/奇迹冷却
        public bool fateDisturbanceActive = false;// 干扰命运开关
        public int wishCastTimer = 0;             // 按键长按计时
        public bool isRealmOfMysteriesActive = false; // 诡秘之境开关

        // --- 错误途径技能状态 ---
        public bool isPassiveStealEnabled = true;
        public int dreamWalkCooldown = 0;      // 梦境穿行冷却
        public const int DREAM_WALK_MAX = 180; // 3秒冷却
        public bool isParasitizing = false;     // 是否正在寄生
        public int parasiteTargetIndex = -1;    // 寄生目标的 NPC 索引
        public bool parasiteIsTownNPC = false;  // 寄生的是否为城镇 NPC
        public int wormificationCooldown = 0;   // 半虫化不死能力的冷却
        public const int WORMIFICATION_COOLDOWN_MAX = 36000; // 10分钟冷却
        public int conceptStealCooldown = 0;    // 概念窃取（窃取距离/位置）冷却
        public int parasiteRitualProgress = 0; // 寄生者仪式进度
        public const int PARASITE_RITUAL_TARGET = 9; // 目标次数
        public int mentorRitualProgress = 0;   // 仪式进度
        public const int MENTOR_RITUAL_TARGET = 9; // 需要误导9个冤魂
        public bool isDeceitDomainActive = false; // 欺瞒领域开关
        public int deceitCooldown = 0;         // 技能冷却
        public int trojanRitualTimer = 0;      // 仪式计时器
        public const int TROJAN_RITUAL_TARGET = 18000; // 目标：5分钟 (60帧 * 300秒)
        public int fateTheftCooldown = 0;      // 命运窃取冷却
        public bool isTrojanResurrection = false; // 是否触发了木马替死
        public bool stealMode = false;         // 是否开启窃取模式
        public int stealAggroTimer = 0;        // 被发现后的惩罚计时器
        public int wormRitualTimer = 0;        // 仪式计时
        public const int WORM_RITUAL_TARGET = 25200; // 7分钟 (60帧 * 420秒)
        public int timeTheftCooldown = 0;      // 窃取时间冷却
        public bool isTimeClockActive = false; // 时之虫领域是否开启
        public bool parasiteIsPlayer = false;

        // --- 太阳途径 ---
        public static readonly SoundStyle BardSongStyle = new SoundStyle("zhashi/Assets/Sounds/BardSong")
        {
            Volume = 1.0f,
            IsLooped = false,
            MaxInstances = 1,
        };
        public bool isSinging = false;
        public int singTimer = 0; // 歌唱持续时间计时器
        public int sunRadianceCooldown = 0; // 日照技能冷却
        public int holyLightCooldown = 0;   // 召唤圣光冷却
        public int holyOathCooldown = 0;    // 神圣誓约冷却
        public int fireOceanCooldown = 0;   // 光明之火冷却
        public bool isCleansingSlash = false; // 净化之斩开关 (被动)
        public int notarizeCooldown = 0;
        public bool isSunMessenger = false; // 是否开启太阳使者形态


        // --- 魔女途径 ---
        public bool instigatorEffect = false;
        private bool preventRecursiveOp = false;
        public bool witchIceEffect = false; // 冰霜开关
        public int witchCurseCooldown = 0;  // 诅咒冷却 (放在这里定义)
        public bool pleasureDemonessEffect = false; // 序列6 开关
        public int mirrorSubstituteCooldown = 0;    // 镜子替身冷却
        public int spiderSilkCooldown = 0;          // 蛛丝控制冷却(防止无限晕)
        public int afflictionRitualTimer = 0; // 仪式计时器
        public bool isAfflictionDemoness = false; // 序列5能力开关
        public bool mirrorCloneActive = false;
        public int despairRitualCount = 0;
        public int unagingRebirthCooldown = 0; // 重生冷却
        public const int REBIRTH_COOLDOWN_MAX = 18000; // 5分钟冷却 (60 * 60 * 5)
        public int PetrificationGazeTimer = 0;
        public int PetrificationGazeCD = 0;
        public int catastropheCooldown = 0; // 灾难降临技能冷却
        public bool isDisasterForm = false; // 是否开启灾难形态（与自然融合）
        public int catastropheRitualCount = 0; // 仪式进度：记录制造的破坏
        public const int CATASTROPHE_RITUAL_TARGET = 50000; // 仪式目标值 (例如造成总伤害或击杀数)
        public int apocalypseCooldown = 0; // 末日大招冷却
        public bool isApocalypseForm = false; // 末日形态开关 (用于视觉/无敌)
        
        public bool canUseWitchBroom = false; // 是否可以使用女巫扫帚
        private bool wasMountedBeforeUpdate = false; // 上一帧是否骑乘 (用于防冲突)

        // --- 命运途径 ---
        public int luckyEventTimer = 0;
        public float luckFluctuation = 0f;
        public int calamityTimer = 0;
        public int psychicStormCooldown = 0;
        public int misfortuneRitualTimer = 0;
        public const int MISFORTUNE_RITUAL_TARGET = 86400;
        public bool hasGrayClover = false;
        public bool misfortuneMageRitualComplete = false;
        public int fateBlessingCooldown = 0; // 赐福CD
        public bool isMisfortuneDomainActive = false; // 厄运领域/灾祸光环 开关

        // --- 序列3 怪人 ---
        public int anomalyRitualProgress = 0;           // "见证三次绝境逆转" 仪式进度
        public const int ANOMALY_RITUAL_TARGET = 3;     // 需要3次低血翻盘
        public bool anomalyRitualComplete = false;      // 仪式是否完成 (持久化保存)
        public int fateNullifyCooldown = 0;             // "不可定数" - 致命伤抹除冷却 (30秒)
        public const int FATE_NULLIFY_CD_MAX = 1800;    // 60帧 * 30秒
        public int fateDiceCooldown = 0;                // 命运骰子冷却 (30秒)
        public const int FATE_DICE_CD_MAX = 1800;       // 60帧 * 30秒
        public int fateBlessingActiveTimer = 0;         // 骰子点数4 - 命运庇护剩余时间
        public int anomalyDebuffStack = 0;              // 缓存当前减益数量(用于Luck计算)
        // 记录玩家上一帧血量百分比，用于检测"绝境逆转"仪式
        public float lastLifePercent = 1f;
        public bool wasInDeepDanger = false;            // 是否曾在<10%血量时与boss/强敌交战

        // === 命运OnHitNPC的节流计时器(防多发武器卡顿/崩溃) ===
        public int wheelOnHitCooldown = 0;              // 命运OnHit触发节流(15帧间隔)

        // --- 序列2 先知 ---
        // 仪式: 在序列3时, "不可定数" 成功触发5次
        public int prophetRitualProgress = 0;           // "见证5次必死之劫"进度
        public const int PROPHET_RITUAL_TARGET = 5;     // 需要5次不可定数触发
        public bool prophetRitualComplete = false;      // 仪式完成(持久化)

        // 福祸之言/命运启示 独立CD (各自独立, 不互相影响)
        public int wordsOfFortuneCooldown = 0;          // 福祸之福CD (30秒)
        public const int FORTUNE_CD_MAX = 1800;
        public int wordsOfMisfortuneCooldown = 0;       // 福祸之祸CD (30秒)
        public const int MISFORTUNE_CD_MAX = 1800;
        public int revelationCooldown = 0;              // 命运启示CD (60秒, 因为效果强)
        public const int REVELATION_CD_MAX = 3600;
        // 保留旧字段名以避免其它地方编译错误 (deprecated, 实际不再使用)
        public int prophecyCooldown = 0;
        public const int PROPHECY_CD_MAX = 2700;

        // 命运启示状态(持续10秒,所有概率事件极度偏向最优)
        public int revelationActiveTimer = 0;           // 启示状态剩余时间
        public int revelationBackfireTimer = 0;         // 启示状态后的反噬(60秒无加成)

        // 预言术: 标记下一击秒杀
        public bool prophecyMarked = false;             // 是否有"预言"待发动
        public int prophecyDuration = 0;                // 预言剩余有效时间

        // 水银之躯: 受伤后10秒"命运回避"
        public int mercuryDodgeTimer = 0;               // 命运回避剩余时间(10秒)
        public int mercuryDodgeCooldown = 0;            // 命运回避CD(60秒)
        public const int MERCURY_DODGE_DURATION = 600;  // 10秒
        public const int MERCURY_DODGE_CD_MAX = 3600;   // 60秒

        // 福祸之言键长按检测
        public int diceKeyHoldFrames = 0;               // 骰子键按住的帧数

        // --- 序列1 巨蛇 (水银之蛇 / 吞尾之蛇 / 命运之蛇) ---
        // 仪式: 在序列2状态下击败月亮领主 + 触发命运启示5次
        public int serpentRitualProgress = 0;           // 启示触发次数(用于序列1仪式)
        public const int SERPENT_RITUAL_TARGET = 5;     // 需要5次命运启示
        public bool serpentRitualBeatMoonLord = false;  // 是否在序列2状态下击败过月亮领主
        public bool serpentRitualComplete = false;      // 仪式完成(持久化)

        // [水银相位] 每10秒进入一次3秒无敌
        public int mercuryPhaseCooldown = 0;            // 距下次相位剩余帧数
        public int mercuryPhaseTimer = 0;               // 当前相位剩余帧数(3秒=180)
        public const int MERCURY_PHASE_INTERVAL = 600;  // 10秒间隔
        public const int MERCURY_PHASE_DURATION = 180;  // 3秒持续

        // [命运循环] 主动技能 - 5秒后回滚位置
        public int fateLoopCooldown = 0;                // 循环CD (90秒)
        public const int FATE_LOOP_CD_MAX = 5400;       // 90秒
        public int fateLoopActiveTimer = 0;             // 循环激活剩余(5秒=300)
        public const int FATE_LOOP_DURATION = 300;      // 5秒
        public uint fateLoopExpireTick = 0;             // 命运循环到期时的 GameUpdateCount (取代依赖计时器)
        public bool fateLoopActive = false;             // 是否处于激活状态(显式标记)
        public Vector2 fateLoopCenter = Vector2.Zero;   // 循环领域中心
        // 用于回滚的快照: NPC ID → (起始位置)
        private Dictionary<int, Vector2> _loopedNpcStartPos = new Dictionary<int, Vector2>();

        // [重启循环] 自动复活 + 主动撤退
        public int restartAutoCooldown = 0;             // 自动复活CD (10分钟=36000)
        public const int RESTART_AUTO_CD_MAX = 36000;
        public int restartManualCooldown = 0;           // 主动撤退CD (3分钟=10800)
        public const int RESTART_MANUAL_CD_MAX = 10800;
        // 历史位置记录用于"回到10秒前"
        public Vector2[] positionHistory = new Vector2[600]; // 10秒位置历史(60fps×10)
        public int positionHistoryIdx = 0;
        public bool positionHistoryFilled = false;
        // [命运多面骰] 投掷动画结束(60帧)后才执行效果
        public int polyhedronDelayTimer = 0;
        public int polyhedronQueuedFaces = 0;
        public int polyhedronQueuedResult = 0;

        // ===================================================
        // 【新增】狗的数据存储 (绑定在玩家身上)
        // ===================================================
        public string DogName = "旺财";
        public int DogPathway = 0;
        public int DogSequence = 10;
        public int DogBonusHP = 0;
        public Item[] DogInventory = new Item[3];

        // 标记日期变化
        public bool lastDayState = false;

        // --- 仪式与杂项 ---
        public int guardianRitualProgress = 0;
        public const int GUARDIAN_RITUAL_TARGET = 1000;
        public int demonHunterRitualProgress = 0;
        public const int DEMON_HUNTER_RITUAL_TARGET = 10;
        public int ironBloodRitualProgress = 0;
        public const int IRON_BLOOD_RITUAL_TARGET = 100;
        public int weatherRitualCount = 0;
        public int weatherRitualTimer = 0;
        public bool weatherRitualComplete = false;
        public bool conquerorRitualComplete = false;
        public int stealthTimer = 0;
        public int shakeTime = 0;
        public float shakePower = 0f;
        public int attendantRitualProgress = 0;
        public const int ATTENDANT_RITUAL_TARGET = 10;
        public bool attendantRitualComplete = false;
        public SlotId singingSoundSlot;
        public int purificationProgress = 0; // 序列5仪式：净化不死生物
        public const int PURIFICATION_RITUAL_TARGET = 100;

        public int judgmentProgress = 0;     // 序列4仪式：审判强敌
        public const int JUDGMENT_RITUAL_TARGET = 20;
        public float screenShakeMagnitude = 0f;


        // --- 方案1: 理智值系统变量 ---
        public float sanityCurrent = 100f;
        public float sanityMax = 100f;
        public int sanityRegenTimer = 0;
        public bool isLosingControl = false; // 是否处于失控状态

        // ===================================================
        // 2. 数据存档与读取
        // ===================================================
        public override void SaveData(TagCompound tag)
        {


            tag["BaseSequence"] = baseSequence;         
            tag["HunterSequence"] = baseHunterSequence;
            tag["MoonSequence"] = baseMoonSequence;
            tag["FoolSequence"] = baseFoolSequence;
            tag["MarauderSequence"] = baseMarauderSequence; 
            tag["SunSequence"] = baseSunSequence;          
            tag["DemonessSequence"] = baseDemonessSequence;
            tag["WheelSequence"] = baseWheelSequence;
            tag["DoorSequence"] = baseDoorSequence;
            tag["RecorderNormalList"] = recorderNormalList;
            tag["RecorderDivineList"] = recorderDivineList;


            tag["Spirituality"] = spiritualityCurrent;
            tag["GuardianRitual"] = guardianRitualProgress;
            tag["DemonHunterRitual"] = demonHunterRitualProgress;
            tag["IronBloodRitual"] = ironBloodRitualProgress;
            tag["WeatherRitualComplete"] = weatherRitualComplete;
            tag["ConquerorRitual"] = conquerorRitualComplete;
            tag["ResurrectionCooldown"] = twilightResurrectionCooldown;
            tag["BorrowUses"] = borrowUsesDaily;
            tag["AttendantRitual"] = attendantRitualProgress;
            tag["AttendantRitualComplete"] = attendantRitualComplete;

            tag["WormificationCD"] = wormificationCooldown;
            tag["ParasiteRitual"] = parasiteRitualProgress;
            tag["MentorRitual"] = mentorRitualProgress;
            tag["TrojanRitual"] = trojanRitualTimer;
            tag["WormRitual"] = wormRitualTimer;
            tag["MyDog_Name"] = DogName;
            tag["MyDog_Pathway"] = DogPathway;
            tag["MyDog_Sequence"] = DogSequence;
            tag["MyDog_BonusHP"] = DogBonusHP;

            tag["PurificationProgress"] = purificationProgress;
            tag["JudgmentProgress"] = judgmentProgress;
            tag["Sanity"] = sanityCurrent;
            tag["AfflictionTimer"] = afflictionRitualTimer;
            tag["DespairKills"] = despairRitualCount;
            tag["CatastropheRitual"] = catastropheRitualCount;
            tag["RealmActive"] = isRealmOfMysteriesActive;

            // 序列3 怪人 仪式
            tag["AnomalyRitual"] = anomalyRitualProgress;
            tag["AnomalyRitualComplete"] = anomalyRitualComplete;

            // 序列2 先知 仪式
            tag["ProphetRitual"] = prophetRitualProgress;
            tag["ProphetRitualComplete"] = prophetRitualComplete;

            // 序列1 巨蛇 仪式
            tag["SerpentRitual"] = serpentRitualProgress;
            tag["SerpentRitualBeatMoonLord"] = serpentRitualBeatMoonLord;
            tag["SerpentRitualComplete"] = serpentRitualComplete;

            if (DogInventory == null) DogInventory = new Item[3];
            for (int i = 0; i < 3; i++)
            {
                if (DogInventory[i] == null) DogInventory[i] = new Item();
                tag[$"MyDog_Inv_{i}"] = ItemIO.Save(DogInventory[i]);
            }
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.ContainsKey("BaseSequence")) baseSequence = tag.GetInt("BaseSequence");
            else if (tag.ContainsKey("CurrentSequence")) baseSequence = tag.GetInt("CurrentSequence");

            if (tag.ContainsKey("HunterSequence")) baseHunterSequence = tag.GetInt("HunterSequence");
            if (tag.ContainsKey("MoonSequence")) baseMoonSequence = tag.GetInt("MoonSequence");
            if (tag.ContainsKey("FoolSequence")) baseFoolSequence = tag.GetInt("FoolSequence");
            if (tag.ContainsKey("MarauderSequence")) baseMarauderSequence = tag.GetInt("MarauderSequence");
            if (tag.ContainsKey("SunSequence")) baseSunSequence = tag.GetInt("SunSequence");
            if (tag.ContainsKey("DemonessSequence")) baseDemonessSequence = tag.GetInt("DemonessSequence");
            if (tag.ContainsKey("WheelSequence")) baseWheelSequence = tag.GetInt("WheelSequence");
            if (tag.ContainsKey("DoorSequence")) baseDoorSequence = tag.GetInt("DoorSequence");
            if (tag.ContainsKey("RecorderNormalList")) recorderNormalList = tag.GetList<int>("RecorderNormalList") as List<int>;
            if (tag.ContainsKey("RecorderDivineList")) recorderDivineList = tag.GetList<int>("RecorderDivineList") as List<int>;
            if (recorderNormalList == null) recorderNormalList = new List<int>();
            if (recorderDivineList == null) recorderDivineList = new List<int>();

            if (tag.ContainsKey("Spirituality")) spiritualityCurrent = tag.GetFloat("Spirituality");
            if (tag.ContainsKey("GuardianRitual")) guardianRitualProgress = tag.GetInt("GuardianRitual");
            if (tag.ContainsKey("DemonHunterRitual")) demonHunterRitualProgress = tag.GetInt("DemonHunterRitual");
            if (tag.ContainsKey("IronBloodRitual")) ironBloodRitualProgress = tag.GetInt("IronBloodRitual");
            if (tag.ContainsKey("WeatherRitualComplete")) weatherRitualComplete = tag.GetBool("WeatherRitualComplete");
            if (tag.ContainsKey("ConquerorRitual")) conquerorRitualComplete = tag.GetBool("ConquerorRitual");
            if (tag.ContainsKey("ResurrectionCooldown")) twilightResurrectionCooldown = tag.GetInt("ResurrectionCooldown");
            if (tag.ContainsKey("BorrowUses")) borrowUsesDaily = tag.GetInt("BorrowUses");
            if (tag.ContainsKey("AttendantRitual")) attendantRitualProgress = tag.GetInt("AttendantRitual");
            if (tag.ContainsKey("AttendantRitualComplete")) attendantRitualComplete = tag.GetBool("AttendantRitualComplete");
            if (tag.ContainsKey("MarauderSequence")) currentMarauderSequence = tag.GetInt("MarauderSequence");
            if (tag.ContainsKey("WormificationCD")) wormificationCooldown = tag.GetInt("WormificationCD");
            if (tag.ContainsKey("ParasiteRitual")) parasiteRitualProgress = tag.GetInt("ParasiteRitual");
            if (tag.ContainsKey("MentorRitual")) mentorRitualProgress = tag.GetInt("MentorRitual");
            if (tag.ContainsKey("TrojanRitual")) trojanRitualTimer = tag.GetInt("TrojanRitual");
            if (tag.ContainsKey("WormRitual")) wormRitualTimer = tag.GetInt("WormRitual");
            if (tag.ContainsKey("MyDog_Name")) DogName = tag.GetString("MyDog_Name");
            if (tag.ContainsKey("MyDog_Pathway")) DogPathway = tag.GetInt("MyDog_Pathway");
            if (tag.ContainsKey("MyDog_Sequence")) DogSequence = tag.GetInt("MyDog_Sequence");
            if (tag.ContainsKey("MyDog_BonusHP")) DogBonusHP = tag.GetInt("MyDog_BonusHP");
            if (tag.ContainsKey("PurificationProgress")) purificationProgress = tag.GetInt("PurificationProgress");
            if (tag.ContainsKey("JudgmentProgress")) judgmentProgress = tag.GetInt("JudgmentProgress");
            if (tag.ContainsKey("Sanity")) sanityCurrent = tag.GetFloat("Sanity");
            if (tag.ContainsKey("AfflictionTimer")) afflictionRitualTimer = tag.GetInt("AfflictionTimer");
            if (tag.ContainsKey("DespairKills")) despairRitualCount = tag.GetInt("DespairKills");
            if (tag.ContainsKey("CatastropheRitual")) catastropheRitualCount = tag.GetInt("CatastropheRitual");
            if (tag.ContainsKey("RealmActive")) isRealmOfMysteriesActive = tag.GetBool("RealmActive");

            // 序列3 怪人 仪式
            if (tag.ContainsKey("AnomalyRitual")) anomalyRitualProgress = tag.GetInt("AnomalyRitual");
            if (tag.ContainsKey("AnomalyRitualComplete")) anomalyRitualComplete = tag.GetBool("AnomalyRitualComplete");

            // 序列2 先知 仪式
            if (tag.ContainsKey("ProphetRitual")) prophetRitualProgress = tag.GetInt("ProphetRitual");
            if (tag.ContainsKey("ProphetRitualComplete")) prophetRitualComplete = tag.GetBool("ProphetRitualComplete");

            // 序列1 巨蛇 仪式
            if (tag.ContainsKey("SerpentRitual")) serpentRitualProgress = tag.GetInt("SerpentRitual");
            if (tag.ContainsKey("SerpentRitualBeatMoonLord")) serpentRitualBeatMoonLord = tag.GetBool("SerpentRitualBeatMoonLord");
            if (tag.ContainsKey("SerpentRitualComplete")) serpentRitualComplete = tag.GetBool("SerpentRitualComplete");



            DogInventory = new Item[3];
            for (int i = 0; i < 3; i++)
            {
                if (tag.ContainsKey($"MyDog_Inv_{i}"))
                    DogInventory[i] = ItemIO.Load(tag.GetCompound($"MyDog_Inv_{i}"));
                else
                {
                    DogInventory[i] = new Item();
                    DogInventory[i].SetDefaults(0);
                }
            }
        }
        // ===================================================
        // 【终极修复版】联机同步代码 (版本 4.0 - 全途径完整版)
        // ===================================================

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)LotMNetMsg.PlayerSync);
            packet.Write((byte)Player.whoAmI);

            packet.Write(baseSequence);          // 通用序列
            packet.Write(baseMarauderSequence);  // 偷盗者基础
            packet.Write(baseFoolSequence);      // 愚者基础
            packet.Write(baseHunterSequence);    // 猎人基础
            packet.Write(baseMoonSequence);      // 月亮基础
            packet.Write(baseSunSequence);       // 太阳基础
            packet.Write(baseDemonessSequence);   // 刺客基础
            packet.Write(baseWheelSequence);      // 命运基础
            packet.Write(baseDoorSequence);       // 学徒/门基础

            // --- [1] 基础数值 (7个) ---
            packet.Write(currentSequence);
            packet.Write(currentMarauderSequence);
            packet.Write(currentFoolSequence);
            packet.Write(currentHunterSequence);
            packet.Write(currentMoonSequence);
            packet.Write(currentSunSequence);
            packet.Write(currentDemonessSequence);
            packet.Write(currentWheelSequence);
            packet.Write(currentDoorSequence);
            packet.Write(spiritualityCurrent); // float

            // --- [2] 寄生与仪式 (8个) ---
            packet.Write(isParasitizing);
            packet.Write(parasiteTargetIndex);
            packet.Write(parasiteIsTownNPC);
            packet.Write(parasiteIsPlayer);
            packet.Write(purificationProgress);      // 太阳序列6
            packet.Write(judgmentProgress);          // 太阳序列4
            packet.Write(ironBloodRitualProgress);   // 猎人序列4
            packet.Write(despairRitualCount);        // 魔女序列4
            packet.Write(afflictionRitualTimer);

            // --- [3] 核心资源 (1个) ---
            packet.Write(spiritWorms);

            // --- [4] 愚者途径状态 (4个) ---
            packet.Write(isSpiritVisionActive);    // 灵视
            packet.Write(isSpiritForm);            // 灵体状态
            packet.Write(graftingMode);            // 嫁接模式
            packet.Write(spiritThreadTargetIndex); // 灵体之线目标
            packet.Write(isRealmOfMysteriesActive);

            // --- [5] 错误途径状态 (2个) ---
            packet.Write(isDeceitDomainActive);    // 欺诈领域
            packet.Write(isTimeClockActive);       // 时之虫钟表

            // --- [6] 月亮途径状态 (6个) ---
            packet.Write(isTamingActive);          // 驯兽
            packet.Write(isVampireWings);          // 吸血鬼翅膀
            packet.Write(isBatSwarm);              // 蝙蝠化身
            packet.Write(isMoonlightized);         // 月光化
            packet.Write(isFullMoonActive);        // 满月
            packet.Write(isCreationDomain);        // 创生领域

            // --- [7] 猎人途径状态 (3个) ---
            packet.Write(isFireForm);              // 火焰形态
            packet.Write(isCalamityGiant);         // 灾祸巨人
            packet.Write(isFlameCloakActive);      // 火焰披风

            // --- [8] 巨人/战士途径状态 (3个) ---
            packet.Write(isGuardianStance);        // 守护姿态
            packet.Write(isMercuryForm);           // 水银化
            packet.Write(dawnArmorActive);         // 黎明铠甲

            // --- [9] 太阳途径状态 (2个) ---
            packet.Write(isSinging);               // 歌颂
            packet.Write(isSunMessenger);          // 太阳使者

            // --- [10] 魔女途径状态 (3个) ---
            packet.Write(isApocalypseForm); 
            packet.Write(isDisasterForm);   


            packet.Write(isPassiveStealEnabled);

            packet.Send(toWho, fromWho);
        }

        public override void CopyClientState(ModPlayer targetCopy)
        {
            LotMPlayer clone = targetCopy as LotMPlayer;

            // 基础复制
            clone.baseSequence = baseSequence;
            clone.baseMarauderSequence = baseMarauderSequence;
            clone.baseFoolSequence = baseFoolSequence;
            clone.baseHunterSequence = baseHunterSequence;
            clone.baseMoonSequence = baseMoonSequence;
            clone.baseSunSequence = baseSunSequence;
            clone.baseDemonessSequence = baseDemonessSequence;
            clone.baseWheelSequence = baseWheelSequence;
            clone.baseDoorSequence = baseDoorSequence;

            // [1]
            clone.currentSequence = currentSequence;
            clone.currentMarauderSequence = currentMarauderSequence;
            clone.currentFoolSequence = currentFoolSequence;
            clone.currentHunterSequence = currentHunterSequence;
            clone.currentMoonSequence = currentMoonSequence;
            clone.currentSunSequence = currentSunSequence;
            clone.currentDemonessSequence = currentDemonessSequence;
            clone.currentWheelSequence = currentWheelSequence;
            clone.currentDoorSequence = currentDoorSequence;
            clone.spiritualityCurrent = spiritualityCurrent;

            // [2]
            clone.isParasitizing = isParasitizing;
            clone.parasiteTargetIndex = parasiteTargetIndex;
            clone.parasiteIsTownNPC = parasiteIsTownNPC;
            clone.parasiteIsPlayer = parasiteIsPlayer;
            clone.purificationProgress = purificationProgress;
            clone.judgmentProgress = judgmentProgress;
            clone.ironBloodRitualProgress = ironBloodRitualProgress;
            clone.despairRitualCount = despairRitualCount;
            clone.afflictionRitualTimer = afflictionRitualTimer;

            // [3]
            clone.spiritWorms = spiritWorms;

            // [4]
            clone.isSpiritVisionActive = isSpiritVisionActive;
            clone.isSpiritForm = isSpiritForm;
            clone.graftingMode = graftingMode;
            clone.spiritThreadTargetIndex = spiritThreadTargetIndex;
            clone.isRealmOfMysteriesActive = isRealmOfMysteriesActive;

            // [5]
            clone.isDeceitDomainActive = isDeceitDomainActive;
            clone.isTimeClockActive = isTimeClockActive;

            // [6]
            clone.isTamingActive = isTamingActive;
            clone.isVampireWings = isVampireWings;
            clone.isBatSwarm = isBatSwarm;
            clone.isMoonlightized = isMoonlightized;
            clone.isFullMoonActive = isFullMoonActive;
            clone.isCreationDomain = isCreationDomain;

            // [7]
            clone.isFireForm = isFireForm;
            clone.isCalamityGiant = isCalamityGiant;
            clone.isFlameCloakActive = isFlameCloakActive;

            // [8]
            clone.isGuardianStance = isGuardianStance;
            clone.isMercuryForm = isMercuryForm;
            clone.dawnArmorActive = dawnArmorActive;

            // [9]
            clone.isSinging = isSinging;
            clone.isSunMessenger = isSunMessenger;

            // [10] 魔女
            clone.isApocalypseForm = isApocalypseForm; 
            clone.isDisasterForm = isDisasterForm;     

            clone.isPassiveStealEnabled = isPassiveStealEnabled;
        }

        public override void SendClientChanges(ModPlayer clientPlayer)
        {
            LotMPlayer clone = clientPlayer as LotMPlayer;

            bool changed =
                clone.baseSequence != baseSequence ||
                clone.baseMarauderSequence != baseMarauderSequence ||
                clone.baseFoolSequence != baseFoolSequence ||
                clone.baseHunterSequence != baseHunterSequence ||
                clone.baseMoonSequence != baseMoonSequence ||
                clone.baseSunSequence != baseSunSequence ||
                clone.baseDemonessSequence != baseDemonessSequence ||
                clone.baseWheelSequence != baseWheelSequence ||

                clone.currentSequence != currentSequence ||
                clone.currentMarauderSequence != currentMarauderSequence ||
                clone.currentFoolSequence != currentFoolSequence ||
                clone.currentHunterSequence != currentHunterSequence ||
                clone.currentMoonSequence != currentMoonSequence ||
                clone.currentSunSequence != currentSunSequence ||
                clone.currentDemonessSequence != currentDemonessSequence ||
                clone.currentWheelSequence != currentWheelSequence ||
                Math.Abs(clone.spiritualityCurrent - spiritualityCurrent) > 0.1f ||

                clone.isParasitizing != isParasitizing ||
                clone.parasiteTargetIndex != parasiteTargetIndex ||
                clone.parasiteIsTownNPC != parasiteIsTownNPC ||
                clone.parasiteIsPlayer != parasiteIsPlayer ||
                clone.purificationProgress != purificationProgress ||
                clone.judgmentProgress != judgmentProgress ||
                clone.ironBloodRitualProgress != ironBloodRitualProgress ||
                clone.despairRitualCount != despairRitualCount ||
                clone.afflictionRitualTimer != afflictionRitualTimer ||

                clone.spiritWorms != spiritWorms ||

                clone.isSpiritVisionActive != isSpiritVisionActive ||
                clone.isSpiritForm != isSpiritForm ||
                clone.graftingMode != graftingMode ||
                clone.spiritThreadTargetIndex != spiritThreadTargetIndex ||
                clone.isRealmOfMysteriesActive != isRealmOfMysteriesActive ||

                clone.isDeceitDomainActive != isDeceitDomainActive ||
                clone.isTimeClockActive != isTimeClockActive ||

                clone.isTamingActive != isTamingActive ||
                clone.isVampireWings != isVampireWings ||
                clone.isBatSwarm != isBatSwarm ||
                clone.isMoonlightized != isMoonlightized ||
                clone.isFullMoonActive != isFullMoonActive ||
                clone.isCreationDomain != isCreationDomain ||

                clone.isFireForm != isFireForm ||
                clone.isCalamityGiant != isCalamityGiant ||
                clone.isFlameCloakActive != isFlameCloakActive ||

                clone.isGuardianStance != isGuardianStance ||
                clone.isMercuryForm != isMercuryForm ||
                clone.dawnArmorActive != dawnArmorActive ||

                clone.isSinging != isSinging ||
                clone.isSunMessenger != isSunMessenger ||
                clone.isApocalypseForm != isApocalypseForm ||
                clone.isDisasterForm != isDisasterForm ||


                clone.isPassiveStealEnabled != isPassiveStealEnabled;

            if (changed)
            {
                SyncPlayer(toWho: -1, fromWho: Main.myPlayer, newPlayer: false);
            }
        }

        // ===================================================
        // 3. 属性重置与核心逻辑
        // ===================================================
        public override void PreUpdate()
        {
            if (psychicStormCooldown > 0)
            {
                psychicStormCooldown--;
            }
            if (fateBlessingCooldown > 0)
            {
                fateBlessingCooldown--;
            }

            // === 序列3 怪人 冷却递减 + 仪式检测 ===
            if (fateNullifyCooldown > 0) fateNullifyCooldown--;
            if (fateDiceCooldown > 0) fateDiceCooldown--;
            if (wheelOnHitCooldown > 0) wheelOnHitCooldown--;

            // 命运庇护(骰子点数4)持续与到期处理
            if (fateBlessingActiveTimer > 0)
            {
                fateBlessingActiveTimer--;
                // 视觉:金色环绕粒子
                if (Main.GameUpdateCount % 4 == 0)
                {
                    Dust d = Dust.NewDustPerfect(
                        Player.Center + Main.rand.NextVector2CircularEdge(40, 40),
                        DustID.GoldCoin, Vector2.Zero, 0, default, 1.2f);
                    d.noGravity = true;
                }
                if (fateBlessingActiveTimer == 0)
                {
                    Main.NewText("命运庇护消散。", 200, 200, 200);
                }
            }

            // "见证三次绝境逆转" 仪式检测 (只在序列4且仪式未完成时记录)
            // 思路: 进入 <10% 血量 (deepDanger) 后, 若血量恢复回 60%+ 则计 1 次
            if (baseWheelSequence == 4 && !anomalyRitualComplete && Player.statLifeMax2 > 0)
            {
                float lifePercent = (float)Player.statLife / Player.statLifeMax2;

                // 进入濒死状态
                if (lifePercent <= 0.10f && lifePercent > 0f)
                {
                    wasInDeepDanger = true;
                }
                // 从濒死翻盘回到60%以上
                else if (wasInDeepDanger && lifePercent >= 0.60f)
                {
                    wasInDeepDanger = false;
                    anomalyRitualProgress++;
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        CombatText.NewText(Player.getRect(), new Color(255, 215, 0),
                            $"绝境逆转 ({anomalyRitualProgress}/{ANOMALY_RITUAL_TARGET})", true);
                        Main.NewText($"命运的偏离记录在案... ({anomalyRitualProgress}/{ANOMALY_RITUAL_TARGET})",
                            255, 215, 0);
                        if (anomalyRitualProgress >= ANOMALY_RITUAL_TARGET)
                        {
                            anomalyRitualComplete = true;
                            Main.NewText("【怪人仪式完成】你已三度从命运的咽喉中逃出，现在可以服用怪人魔药了。",
                                255, 100, 255);
                        }
                    }
                }
                lastLifePercent = lifePercent;
            }

            // === 序列2 先知 - 计时器与状态 ===
            if (wordsOfFortuneCooldown > 0) wordsOfFortuneCooldown--;
            if (wordsOfMisfortuneCooldown > 0) wordsOfMisfortuneCooldown--;
            if (revelationCooldown > 0) revelationCooldown--;
            if (prophecyCooldown > 0) prophecyCooldown--; // 保留兼容
            if (mercuryDodgeCooldown > 0) mercuryDodgeCooldown--;

            // === 序列1 巨蛇 - 计时器+位置历史+水银相位+命运循环 ===
            if (currentWheelSequence <= 1)
            {
                // [位置历史] 每帧记录玩家位置(用于"重启"回滚)
                positionHistory[positionHistoryIdx] = Player.Center;
                positionHistoryIdx = (positionHistoryIdx + 1) % positionHistory.Length;
                if (positionHistoryIdx == 0) positionHistoryFilled = true;

                // [水银相位] 每10秒自动进入3秒无敌
                if (mercuryPhaseTimer > 0)
                {
                    mercuryPhaseTimer--;
                    if (mercuryPhaseTimer == 0)
                    {
                        mercuryPhaseCooldown = MERCURY_PHASE_INTERVAL;
                        if (Main.myPlayer == Player.whoAmI)
                            Main.NewText("水银相位结束。", 200, 200, 220);
                    }
                }
                else if (mercuryPhaseCooldown > 0)
                {
                    mercuryPhaseCooldown--;
                    if (mercuryPhaseCooldown == 0)
                    {
                        mercuryPhaseTimer = MERCURY_PHASE_DURATION;
                        if (Main.myPlayer == Player.whoAmI)
                        {
                            Main.NewText("水银相位激活: 3秒无敌。", 220, 220, 255);
                            // 银色光环
                            for (int k = 0; k < 60; k++)
                            {
                                float angle = (k / 60f) * MathHelper.TwoPi;
                                Vector2 pos = Player.Center + new Vector2((float)System.Math.Cos(angle), (float)System.Math.Sin(angle)) * 80f;
                                Dust d = Dust.NewDustPerfect(pos, DustID.SilverCoin, Vector2.Zero, 0, default, 2f);
                                d.noGravity = true;
                                d.fadeIn = 1.5f;
                            }
                        }
                    }
                }

                // [命运循环] CD与激活计时 - 改用时间戳, 不依赖计时器递减
                if (fateLoopCooldown > 0) fateLoopCooldown--;

                if (fateLoopActive)
                {
                    // 持续视觉: 领域边界齿轮纹
                    if (Main.myPlayer == Player.whoAmI && Main.GameUpdateCount % 2 == 0)
                    {
                        for (int k = 0; k < 16; k++)
                        {
                            float angle = (k / 16f) * MathHelper.TwoPi + Main.GameUpdateCount * 0.02f;
                            Vector2 pos = fateLoopCenter + new Vector2((float)System.Math.Cos(angle), (float)System.Math.Sin(angle)) * 800f;
                            Dust d = Dust.NewDustPerfect(pos, DustID.GoldCoin, Vector2.Zero, 0, default, 1.4f);
                            d.noGravity = true;
                        }
                        for (int k = 0; k < 12; k++)
                        {
                            float angle = (k / 12f) * MathHelper.TwoPi - Main.GameUpdateCount * 0.03f;
                            Vector2 pos = fateLoopCenter + new Vector2((float)System.Math.Cos(angle), (float)System.Math.Sin(angle)) * 600f;
                            Dust d = Dust.NewDustPerfect(pos, DustID.PurpleCrystalShard, Vector2.Zero, 0, default, 1.3f);
                            d.noGravity = true;
                        }
                    }

                    // 计算剩余时间用于反馈
                    long remainingTicks = (long)fateLoopExpireTick - (long)Main.GameUpdateCount;
                    if (remainingTicks < 0) remainingTicks = 0;
                    fateLoopActiveTimer = (int)remainingTicks; // 同步给UI显示

                    // 每秒提示倒计时
                    if (Main.myPlayer == Player.whoAmI && remainingTicks > 0 && remainingTicks % 60 == 0)
                    {
                        Main.NewText($"命运循环倒计时: {remainingTicks / 60}s (已记录 {_loopedNpcStartPos.Count} 个目标)", 200, 150, 255);
                    }

                    // 到期: 用时间戳判定, 不依赖递减
                    if (Main.GameUpdateCount >= fateLoopExpireTick)
                    {
                        fateLoopActive = false;
                        fateLoopActiveTimer = 0;

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int rolledBack = 0;
                            foreach (var kv in _loopedNpcStartPos)
                            {
                                int npcIdx = kv.Key;
                                if (npcIdx >= 0 && npcIdx < Main.maxNPCs)
                                {
                                    NPC npc = Main.npc[npcIdx];
                                    if (npc.active && !npc.boss)
                                    {
                                        npc.position = kv.Value;
                                        npc.velocity = Vector2.Zero;
                                        npc.netUpdate = true;
                                        for (int k = 0; k < 15; k++)
                                        {
                                            Dust d = Dust.NewDustPerfect(npc.Center, DustID.GoldCoin,
                                                Main.rand.NextVector2Circular(4, 4), 0, default, 1.5f);
                                            d.noGravity = true;
                                        }
                                        CombatText.NewText(npc.getRect(), Color.MediumPurple, "循环回滚!", true);
                                        rolledBack++;
                                    }
                                }
                            }
                            if (Main.myPlayer == Player.whoAmI)
                                Main.NewText($"【命运循环·闭合】 {rolledBack} 个敌人被回滚到5秒前的位置。", 200, 150, 255);
                        }
                        _loopedNpcStartPos.Clear();
                    }
                }

                // [重启] CD递减
                if (restartAutoCooldown > 0) restartAutoCooldown--;
                if (restartManualCooldown > 0) restartManualCooldown--;

                // [命运多面骰] 等动画结束执行实际效果
                if (polyhedronDelayTimer > 0)
                {
                    polyhedronDelayTimer--;
                    if (polyhedronDelayTimer == 0 && polyhedronQueuedFaces > 0)
                    {
                        ExecutePolyhedronEffect();
                    }
                }
            }
            if (mercuryDodgeTimer > 0)
            {
                mercuryDodgeTimer--;
                // 命运回避激活: 视觉表现+无敌帧维持
                Player.immune = true;
                if (Player.immuneTime < 2) Player.immuneTime = 2;
                // 银色粒子环绕
                if (Main.GameUpdateCount % 3 == 0 && Main.myPlayer == Player.whoAmI)
                {
                    Dust d = Dust.NewDustPerfect(
                        Player.Center + Main.rand.NextVector2CircularEdge(35, 35),
                        DustID.SilverCoin, Vector2.Zero, 0, default, 1.4f);
                    d.noGravity = true;
                }
                if (mercuryDodgeTimer == 0 && Main.myPlayer == Player.whoAmI)
                {
                    Main.NewText("水银之躯回避结束。", 200, 200, 220);
                }
            }
            if (revelationActiveTimer > 0)
            {
                revelationActiveTimer--;

                if (Main.myPlayer == Player.whoAmI)
                {
                    // [视觉1] 命运齿轮 - 玩家头顶3个金色齿轮按各自速率旋转
                    // 三个齿轮代表"过去/现在/未来"
                    Vector2 gearCenter = Player.Center + new Vector2(0, -90);
                    for (int gearIdx = 0; gearIdx < 3; gearIdx++)
                    {
                        float radius = 18 + gearIdx * 12;
                        float speed = (gearIdx % 2 == 0) ? 0.04f : -0.05f; // 交替正反转
                        int teeth = 6 + gearIdx * 2; // 齿轮齿数
                        for (int t = 0; t < teeth; t++)
                        {
                            float angle = (t / (float)teeth) * MathHelper.TwoPi
                                          + Main.GameUpdateCount * speed;
                            Vector2 pos = gearCenter + new Vector2((float)System.Math.Cos(angle), (float)System.Math.Sin(angle)) * radius;
                            if (Main.GameUpdateCount % 3 == 0)
                            {
                                Dust d = Dust.NewDustPerfect(pos, DustID.GoldCoin,
                                    Vector2.Zero, 0, default, 1.3f);
                                d.noGravity = true;
                                d.fadeIn = 1.2f;
                            }
                        }
                    }

                    // [视觉2] 命运符文环 - 玩家身体周围3层符文环绕(交替方向)
                    if (Main.GameUpdateCount % 2 == 0)
                    {
                        for (int ring = 0; ring < 3; ring++)
                        {
                            float r = 70 + ring * 25;
                            float baseAngle = Main.GameUpdateCount * (ring % 2 == 0 ? 0.025f : -0.025f);
                            int dustOnRing = 16;
                            for (int k = 0; k < dustOnRing; k++)
                            {
                                float angle = (k / (float)dustOnRing) * MathHelper.TwoPi + baseAngle;
                                Vector2 pos = Player.Center + new Vector2((float)System.Math.Cos(angle), (float)System.Math.Sin(angle)) * r;
                                // 交替金/紫: 内圈紫,外圈金
                                int dustType = (ring == 0) ? DustID.PurpleCrystalShard
                                            : (ring == 1) ? DustID.GoldCoin : DustID.YellowTorch;
                                Dust d = Dust.NewDustPerfect(pos, dustType, Vector2.Zero, 0, default, 1f);
                                d.noGravity = true;
                                d.fadeIn = 1.1f;
                            }
                        }
                    }

                    // [视觉3] 命运之线 - 每30帧扫描一次, 向附近敌人连出金色丝线
                    if (Main.GameUpdateCount % 30 == 0)
                    {
                        int lineCount = 0;
                        foreach (NPC npc in Main.ActiveNPCs)
                        {
                            if (lineCount >= 8) break; // 最多同时8条线避免爆炸
                            if (npc.friendly || npc.dontTakeDamage) continue;
                            if (npc.Distance(Player.Center) > 1200f) continue;

                            // 沿线绘制粒子
                            int segments = (int)(npc.Distance(Player.Center) / 30);
                            if (segments > 40) segments = 40;
                            for (int s = 0; s < segments; s++)
                            {
                                float t = s / (float)segments;
                                Vector2 pos = Vector2.Lerp(Player.Center, npc.Center, t)
                                    + new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3));
                                Dust d = Dust.NewDustPerfect(pos, DustID.GoldCoin,
                                    Vector2.Zero, 100, default, 0.9f);
                                d.noGravity = true;
                                d.fadeIn = 0.8f;
                            }
                            lineCount++;
                        }
                    }

                    // [视觉4] 玩家脚下持续涌出的紫色长河水波
                    if (Main.GameUpdateCount % 4 == 0)
                    {
                        Vector2 footPos = Player.Bottom + new Vector2(Main.rand.NextFloat(-30, 30), 0);
                        Dust d = Dust.NewDustPerfect(footPos, DustID.PurpleCrystalShard,
                            new Vector2(Main.rand.NextFloat(-2, 2), -Main.rand.NextFloat(0.5f, 2f)), 0, default, 1.2f);
                        d.noGravity = true;
                    }

                    // [视觉5 玩法层] 未来轨迹标记 - 每个敌人当前位置紫光,90帧后位置金光
                    if (Main.GameUpdateCount % 8 == 0)
                    {
                        foreach (NPC npc in Main.ActiveNPCs)
                        {
                            if (npc.friendly || npc.dontTakeDamage) continue;
                            if (npc.Distance(Player.Center) > 1500f) continue;

                            // 当前位置: 紫光
                            Dust now = Dust.NewDustPerfect(npc.Center, DustID.PurpleCrystalShard,
                                Vector2.Zero, 0, default, 1f);
                            now.noGravity = true;
                            now.fadeIn = 1.3f;

                            // 90帧后预测位置 (用当前velocity线性预测)
                            Vector2 futurePos = npc.Center + npc.velocity * 90f;
                            Dust future = Dust.NewDustPerfect(futurePos, DustID.GoldCoin,
                                Vector2.Zero, 0, default, 1.2f);
                            future.noGravity = true;
                            future.fadeIn = 1.5f;
                        }
                    }

                    // [光照] 玩家周围金紫色光照
                    Lighting.AddLight(Player.Center, 0.7f, 0.55f, 0.9f);
                }

                // 启示结束的提示与反噬开启
                if (revelationActiveTimer == 0)
                {
                    revelationBackfireTimer = 3600; // 启示结束后60秒反噬期
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        Main.NewText("命运启示褪去，认知重归凡尘 (60秒反噬期)", 150, 100, 200);
                        // 视觉收束: 大量粒子向玩家中心聚拢
                        for (int k = 0; k < 60; k++)
                        {
                            float angle = Main.rand.NextFloat() * MathHelper.TwoPi;
                            Vector2 startPos = Player.Center + new Vector2((float)System.Math.Cos(angle), (float)System.Math.Sin(angle)) * 200;
                            Vector2 vel = (Player.Center - startPos) / 12f;
                            Dust d = Dust.NewDustPerfect(startPos, DustID.GoldCoin, vel, 0, default, 1.5f);
                            d.noGravity = true;
                        }
                    }
                }
            }
            if (revelationBackfireTimer > 0) revelationBackfireTimer--;
            if (prophecyDuration > 0)
            {
                prophecyDuration--;
                if (prophecyDuration == 0)
                {
                    prophecyMarked = false;
                    if (Main.myPlayer == Player.whoAmI)
                        Main.NewText("预言未应验，命运嘲笑了你...", 150, 150, 150);
                }
            }

            // (废弃) 长按检测已废弃, 改为独立按键 Wheel_Revelation/WordsOfFortune/WordsOfMisfortune

            // "见证5次必死之劫"仪式: 不可定数触发计数 (由FreeDodge 里递增)
            // 自动判定完成
            if (baseWheelSequence == 3 && !prophetRitualComplete && prophetRitualProgress >= PROPHET_RITUAL_TARGET)
            {
                prophetRitualComplete = true;
                if (Main.myPlayer == Player.whoAmI)
                {
                    Main.NewText("【先知仪式完成】你已五次见证命运拒绝死亡，可服用先知魔药。", 200, 100, 255);
                }
            }

            // 巨蛇仪式自动完成: 启示5次 且 击败过月亮领主
            if (baseWheelSequence == 2 && !serpentRitualComplete
                && serpentRitualProgress >= SERPENT_RITUAL_TARGET
                && serpentRitualBeatMoonLord)
            {
                serpentRitualComplete = true;
                if (Main.myPlayer == Player.whoAmI)
                {
                    Main.NewText("【巨蛇仪式完成】 你已凝视命运长河五次,且终结了月之主的存在。命运之蛇向你显形。", 220, 220, 255);
                }
            }

            if (currentWheelSequence <= 7)
            {
                luckyEventTimer++;
                if (luckyEventTimer % 60 == 0) luckFluctuation = Main.rand.NextFloat(-5f, 15f);

                // 核心：幸运值大于0才能捡钱，且幸运值越高，触发间隔越短
                float moneyChance = 2000f - (Player.luck * 500f);
                if (moneyChance < 500f) moneyChance = 500f; // 封顶几率

                if (Player.luck > 0 && Math.Abs(Player.velocity.X) > 0.1f && Main.rand.NextBool((int)moneyChance))
                {
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        int coinType = Main.rand.NextBool(10) ? ItemID.GoldCoin : ItemID.SilverCoin;
                        int amount = Main.rand.Next(1, 5);
                        Item.NewItem(Player.GetSource_FromThis(), Player.Center + new Vector2(0, -50), coinType, amount);
                        CombatText.NewText(Player.getRect(), Color.Gold, "出门捡钱!", true);
                    }
                }
            }

            // 【主动领域】：灾祸光环(序列6) -> 厄运领域(序列4)
            if (isMisfortuneDomainActive)
            {
                // 维持领域需要消耗灵性 (半神消耗更大)
                float cost = (currentWheelSequence <= 4) ? 5.0f : 2.0f;

                if (!TryConsumeSpirituality(cost / 60f, true))
                {
                    isMisfortuneDomainActive = false;
                    Main.NewText("灵性枯竭，领域被迫收起。", 255, 50, 50);
                }
                else
                {
                    // 【进阶版】序列4：厄运领域 (范围诅咒 + 暴毙)
                    if (currentWheelSequence <= 4)
                    {
                        if (Main.GameUpdateCount % 60 == 0)
                        {
                            // 范围与幸运值强关联：基础1600像素，幸运+1再加1600
                            float domainRadius = 1600f + (Player.luck > 0 ? Player.luck * 1600f : 0);
                            if (domainRadius > 4800f) domainRadius = 4800f; // 上限

                            foreach (NPC npc in Main.ActiveNPCs)
                            {
                                if (!npc.friendly && !npc.dontTakeDamage && npc.Distance(Player.Center) < domainRadius)
                                {
                                    // 随机上高阶Debuff
                                    int choice = Main.rand.Next(3);
                                    if (choice == 0) npc.AddBuff(BuffID.BetsysCurse, 120);
                                    if (choice == 1) npc.AddBuff(BuffID.ShadowFlame, 120);
                                    if (choice == 2) npc.AddBuff(BuffID.Ichor, 120);

                                    // 厄运暴毙：基础1%几率，每点运气增加1%
                                    float killChance = 0.01f + (Player.luck * 0.01f);
                                    if (killChance < 0) killChance = 0f;
                                    // 序列3 怪人 - 概率翻倍
                                    if (currentWheelSequence <= 3) killChance *= 3f;

                                    if (!npc.boss && Main.rand.NextFloat() < killChance)
                                    {
                                        npc.SimpleStrikeNPC(npc.lifeMax, 0, false, 0, DamageClass.Default, true);
                                        npc.netUpdate = true;
                                        CombatText.NewText(npc.getRect(), Color.DarkRed, "厄运暴毙!", true);

                                        // 序列3 怪人 "厄运链"：连锁感染邻近敌人
                                        if (currentWheelSequence <= 3)
                                        {
                                            int chainCount = 0;
                                            foreach (NPC near in Main.ActiveNPCs)
                                            {
                                                if (chainCount >= 3) break;
                                                if (near.whoAmI == npc.whoAmI) continue;
                                                if (near.friendly || near.dontTakeDamage || near.boss) continue;
                                                if (near.Distance(npc.Center) < 400f)
                                                {
                                                    int chainDmg = (int)(near.life * 0.30f);
                                                    near.SimpleStrikeNPC(chainDmg, 0, false, 0, DamageClass.Default, true);
                                                    if (!near.HasBuff(BuffID.ShadowFlame)) near.AddBuff(BuffID.ShadowFlame, 180);
                                                    near.netUpdate = true;
                                                    CombatText.NewText(near.getRect(), new Color(148, 0, 211), "厄运链!", true);
                                                    chainCount++;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    // 【基础版】序列6：灾祸光环 (天降异象)
                    else if (currentWheelSequence <= 6)
                    {
                        calamityTimer++;
                        // 频率与幸运值强关联：默认120帧一次，运气+1变成约80帧
                        int calamityFreq = 120 - (int)(Player.luck * 40);
                        if (calamityFreq < 40) calamityFreq = 40; // 最快40帧
                        if (Player.luck < 0) calamityFreq = 240;  // 倒霉时很慢

                        if (calamityTimer >= calamityFreq)
                        {
                            calamityTimer = 0;
                            SpawnCalamity(); // 生成陨石/炸弹
                        }
                    }
                }
            }

            // 【序列5：赢家】莫名其妙的好事 (随幸运值浮动)
            if (currentWheelSequence <= 5)
            {
                // 只有幸运大于0才会发生好事，且幸运越高几率越大
                float goodEventChance = 3600f - (Player.luck * 600f);
                if (goodEventChance < 600f) goodEventChance = 600f;

                if (Player.luck > 0 && Main.rand.NextBool((int)goodEventChance))
                {
                    int eventType = Main.rand.Next(3);
                    if (eventType == 0)
                    {
                        Item.NewItem(Player.GetSource_FromThis(), Player.Center, ItemID.PlatinumCoin, 1);
                        CombatText.NewText(Player.getRect(), Color.Gold, "意外遗产!", true);
                    }
                    else if (eventType == 1)
                    {
                        bool hit = false;
                        foreach (NPC npc in Main.npc)
                        {
                            if (npc.active && !npc.friendly && npc.Distance(Player.Center) < 800f)
                            {
                                npc.AddBuff(BuffID.Confused, 300);
                                npc.AddBuff(BuffID.Stoned, 60);
                                hit = true;
                            }
                        }
                        if (hit) CombatText.NewText(Player.getRect(), Color.LightBlue, "敌人迷路了!", true);
                    }
                    else
                    {
                        Player.Heal(50);
                        Player.AddBuff(BuffID.Regeneration, 600);
                        CombatText.NewText(Player.getRect(), Color.Pink, "心情愉悦!", true);
                    }
                }
            }
            if (Player.dead) waitingForTeleport = false;
            if (waitingForTeleport)
            {
                if (Main.rand.NextBool(5))
                    Dust.NewDust(Player.position, Player.width, Player.height, DustID.Vortex, 0, 0, 0, default, 1f);
                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    Vector2 targetPos = Main.MouseWorld;
                    if (Main.mapFullscreen)
                    {
                        float scale = Main.mapFullscreenScale;
                        float dx = (Main.mouseX - Main.screenWidth / 2f) / scale;
                        float dy = (Main.mouseY - Main.screenHeight / 2f) / scale;
                        targetPos = new Vector2(
                            (Main.mapFullscreenPos.X + dx) * 16f,
                            (Main.mapFullscreenPos.Y + dy) * 16f
                        );
                        Main.mapFullscreen = false;
                    }

                    Player.Teleport(targetPos, 1);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, Player.position); // 播放传送音效
                    Main.NewText("空间跨越成功！", 0, 255, 255);

                    waitingForTeleport = false;
                }
            }
            // ============================
            
            if (isDeceitDomainActive)
            {
                if (!TryConsumeSpirituality(1.5f, true))
                {
                    isDeceitDomainActive = false;
                    Main.NewText("灵性枯竭，欺瞒领域消散。", 150, 150, 150);
                }
                else
                {
                    if (Main.GameUpdateCount % 10 == 0)
                    {
                        Vector2 pos = Player.Center + Main.rand.NextVector2Circular(400, 400);
                        Dust d = Dust.NewDustPerfect(pos, DustID.Vortex, Vector2.Zero, 150, default, 0.5f);
                        d.noGravity = true;
                    }
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!npc.friendly && !npc.dontTakeDamage && npc.Distance(Player.Center) < 500f)
                        {
                            npc.AddBuff(BuffID.Confused, 60); 
                            npc.defense = (int)(npc.defDefense * 0.5f);
                        }
                    }
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        Projectile p = Main.projectile[i];
                        if (p.active && p.hostile && p.Distance(Player.Center) < 200f)
                        {
                            Vector2 push = (p.Center - Player.Center).SafeNormalize(Vector2.Zero) * 2f;
                            p.velocity += push;
                            if (currentMarauderSequence <= 2 && Main.rand.NextBool(50))
                            {
                                p.hostile = false;
                                p.friendly = true;
                            }
                        }
                    }
                }
                if (currentMarauderSequence == 2)
                {
                    // 条件：欺瞒领域开启 + 处于城镇中 (周围有NPC)
                    if (isDeceitDomainActive && Player.townNPCs >= 3f)
                    {
                        wormRitualTimer++;

                        if (wormRitualTimer % 3600 == 0)
                        {
                            int minutes = wormRitualTimer / 3600;
                            Main.NewText($"周边的时光正在发生错乱... ({minutes}/7 分钟)", 150, 150, 255);
                        }

                        if (wormRitualTimer == WORM_RITUAL_TARGET)
                        {
                            Main.NewText("仪式完成：古老的壁钟虚影已笼罩这座城市！(7/7)", 0, 255, 255);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item119, Player.position); // 神秘的声音
                        }
                    }
                }
            }
            // 序列1：美神 强制转变为女性
            if (currentMoonSequence <= 1) Player.Male = false;
            base.PreUpdate();
            if (Main.dayTime && !lastDayState)
            {
                if (Main.time < 60)
                {
                    borrowUsesDaily = 0;
                    if (currentFoolSequence <= 3)
                    {
                        Main.NewText("新的一天，过去的力量已重置。", 200, 200, 255);
                    }
                }
            }
            lastDayState = Main.dayTime;

            // 2. 借用力量计时
            if (isBorrowingPower)
            {
                borrowTimer--;
                if (borrowTimer <= 0)
                {
                    isBorrowingPower = false;
                    Main.NewText("借来的力量消退了...", 150, 150, 150);
                }
            }

            // 3.灵体化穿墙逻辑 
            if (isSpiritForm)
            {
                Player.gravity = 0f;
                Player.velocity = Vector2.Zero;
                Player.fallStart = (int)(Player.position.Y / 16f); // 防止解除时受到摔落伤害

                float speed = 12f; // 灵体飞行速度

                if (Player.controlLeft) Player.position.X -= speed;
                if (Player.controlRight) Player.position.X += speed;
                if (Player.controlUp) Player.position.Y -= speed;
                if (Player.controlDown) Player.position.Y += speed;
                Player.noKnockback = true;
            }
            if (currentFoolSequence <= 1)
            {
                if (LotMKeybinds.Fool_RealmSwitch.JustPressed)
                {
                    isRealmOfMysteriesActive = !isRealmOfMysteriesActive;
                    if (isRealmOfMysteriesActive)
                        Main.NewText("诡秘之境：[c/00FF00:开启] (吞噬万物)", 255, 255, 255);
                    else
                        Main.NewText("诡秘之境：[c/FF0000:关闭] (收敛气息)", 255, 255, 255);
                }

                if (isRealmOfMysteriesActive)
                {
                    if (Main.GameUpdateCount % 30 == 0)
                    {
                        ProcessRealmOfMysteries();
                    }
                }
            }
            wasMountedBeforeUpdate = Player.mount.Active;

            base.PreUpdate();
        }

        public override void ResetEffects()
        {
            //亵渎之牌
            isAntiDivinationActive = false;
            blasphemyCardEquippedCount = 0;
            isFoolCardEquipped = false;
            isStrengthCardEquipped = false;//力量
            isLoversCardEquipped = false;
            isRedPriestCardEquipped = false;
            isSunCardEquipped = false;
            isMoonCardEquipped = false;
            isDoorCardEquipped = false;//门
            isWhiteTowerCardEquipped = false;//白塔
            isVisionaryCardEquipped = false;//空想家
            isBlackEmperorCardEquipped = false;//黑皇帝
            isTyrantCardEquipped = false;//暴君
            isHangedManCardEquipped = false;//倒吊人
            isDeathCardEquipped = false;//死神
            isDarknessCardEquipped = false;//黑暗
            isJusticiarCardEquipped = false;//审判者
            isDemonessCardEquipped = false;//魔女
            isAbyssCardEquipped = false;//深渊
            isChainedCardEquipped = false;//被束缚者
            isHermitCardEquipped = false;//隐者
            isPerfectionistCardEquipped = false;//完美者
            isMotherCardEquipped = false;//母亲
            isWheelOfFortuneCardEquipped = false;//命运之轮


            currentSequence = baseSequence;
            currentHunterSequence = baseHunterSequence;
            currentMoonSequence = baseMoonSequence;
            currentFoolSequence = baseFoolSequence;
            currentMarauderSequence = baseMarauderSequence;
            currentSunSequence = baseSunSequence;
            currentDemonessSequence = baseDemonessSequence;
            currentWheelSequence = baseWheelSequence;
            currentDoorSequence = baseDoorSequence;

            instigatorEffect = false;
            witchIceEffect = false;
            pleasureDemonessEffect = false;
            isAfflictionDemoness = false;
            canUseWitchBroom = false;
            hasGrayClover = false;
            anomalyDebuffStack = 0; // 每帧重置(在PostUpdateEquips里重新统计)

            CalculateMaxSpirituality();
            HandleSpiritualityRegen();
            HandleDawnArmorLogic();



            currentGiantSequence = currentSequence;
            

            // 冷却倒计时
            if (twilightResurrectionCooldown > 0) twilightResurrectionCooldown--;
            if (fireTeleportCooldown > 0) fireTeleportCooldown--;
            if (glacierCooldown > 0) glacierCooldown--;
            if (abyssShackleCooldown > 0) abyssShackleCooldown--;
            if (elixirCooldown > 0) elixirCooldown--;
            if (paperFigurineCooldown > 0) paperFigurineCooldown--;
            if (darknessGazeCooldown > 0) darknessGazeCooldown--;
            if (summonGateCooldown > 0) summonGateCooldown--;
            if (purifyCooldown > 0) purifyCooldown--;
            if (divinationCooldown > 0) divinationCooldown--;
            if (damageTransferCooldown > 0) damageTransferCooldown--;
            if (flameJumpCooldown > 0) flameJumpCooldown--;
            if (distortCooldown > 0) distortCooldown--;
            if (swapCooldown > 0) swapCooldown--;
            if (spiritControlCooldown > 0) spiritControlCooldown--;
            if (miracleCooldown > 0) miracleCooldown--;
            if (graftingCooldown > 0) graftingCooldown--;
            if (dreamWalkCooldown > 0) dreamWalkCooldown--;
            if (wormificationCooldown > 0) wormificationCooldown--;
            if (conceptStealCooldown > 0) conceptStealCooldown--;
            if (fateTheftCooldown > 0) fateTheftCooldown--;
            if (timeTheftCooldown > 0) timeTheftCooldown--;
            if (sunRadianceCooldown > 0) sunRadianceCooldown--;
            if (holyLightCooldown > 0) holyLightCooldown--;
            if (holyOathCooldown > 0) holyOathCooldown--;
            if (fireOceanCooldown > 0) fireOceanCooldown--;
            if (notarizeCooldown > 0) notarizeCooldown--;
            if (witchCurseCooldown > 0) witchCurseCooldown--;
            if (mirrorSubstituteCooldown > 0) mirrorSubstituteCooldown--;
            if (spiderSilkCooldown > 0) spiderSilkCooldown--;
            if (unagingRebirthCooldown > 0) unagingRebirthCooldown--;
            if (PetrificationGazeCD > 0) PetrificationGazeCD--;
            if (catastropheCooldown > 0) catastropheCooldown--;
            if (apocalypseCooldown > 0) apocalypseCooldown--;

            // ==========================================
            // 3. 灵之虫自动再生系统
            // ==========================================
            if (currentFoolSequence <= 4)
            {
                int wormCap = 50;       // 序列4 默认
                int regenSpeed = 1800;  // 序列4: 30秒回1条

                if (currentFoolSequence <= 3)
                {
                    wormCap = 600;
                    regenSpeed = 300;   // 序列3: 5秒回1条
                }

                if (currentFoolSequence <= 2)
                {
                    wormCap = 1200;
                    regenSpeed = 60;    // 序列2: 1秒回1条 (极快)
                }

                // 执行回复逻辑
                if (spiritWorms < wormCap)
                {
                    wormRegenTimer++;
                    if (wormRegenTimer >= regenSpeed)
                    {
                        spiritWorms++;
                        wormRegenTimer = 0;
                    }
                }
            }
            // 灵肉转化：每帧消耗灵性 (支持平衡性削弱)
            if (isSpiritForm)
            {
                // 1. 获取配置：是否开启削弱 (需创建 LotMConfig.cs)
                bool nerf = ModContent.GetInstance<LotMConfig>().NerfDivineAbilities;

                // 2. 决定消耗：削弱模式下消耗激增 (50/帧), 原著模式 (5/帧)
                float cost = nerf ? 50.0f : 5.0f;

                if (!TryConsumeSpirituality(cost, true))
                {
                    isSpiritForm = false;
                    Main.NewText("灵性枯竭，被迫回归血肉之躯。", 255, 50, 50);
                }
                else
                {
                    // 3. 决定防御机制
                    if (nerf)
                    {
                        // 【平衡模式】
                        // 移除完全无敌，改为高额减伤和闪避
                        Player.endurance += 0.6f; // 60% 免伤
                        Player.statDefense += 200; // 额外防御
                        // 注意：这里绝对不能写 Player.immune = true;
                    }
                    else
                    {
                        // 【原著模式】
                        // 物理免疫 (无敌)
                        Player.immune = true;
                    }

                    // 视觉效果保持一致
                    Lighting.AddLight(Player.Center, 0.6f, 0.6f, 0.8f);
                }
            }

            // 嫁接模式特效
            if (graftingMode != 0)
            {
                // 持续消耗灵性维持嫁接概念
                if (!TryConsumeSpirituality(2.0f, true))
                {
                    graftingMode = 0;
                    Main.NewText("嫁接中断。", 150, 150, 150);
                }
            }
            if (weatherRitualTimer > 0)
            {
                weatherRitualTimer--;
                if (weatherRitualTimer == 0 && !weatherRitualComplete)
                {
                    if (weatherRitualCount > 0)
                        Main.NewText("符文共鸣消散了... (需快速连续触发)", 200, 200, 200);
                    weatherRitualCount = 0;
                }
            }
            // --- 窃取惩罚逻辑 ---
            if (stealAggroTimer > 0)
            {
                stealAggroTimer--;

                // 每秒 (60帧) 受到一次攻击
                if (stealAggroTimer % 60 == 0)
                {
                    Player.Hurt(PlayerDeathReason.ByCustomReason("被愤怒的店主暴打！"), 20, 0);
                    SoundEngine.PlaySound(SoundID.Item14, Player.position);
                    for (int i = 0; i < 10; i++) Dust.NewDust(Player.position, Player.width, Player.height, DustID.Smoke, 0, 0, 0, default, 1.5f);
                }

                // 惩罚期间强制断开对话
                if (Player.talkNPC != -1)
                {
                    Player.SetTalkNPC(-1);
                    Main.playerInventory = false;
                    stealMode = false;
                }
            }
            if (Player.talkNPC == -1)
            {
                stealMode = false;
            }

            // 状态自动解除
            if (currentSequence > 3) isMercuryForm = false;
            if (currentHunterSequence > 4) { isFireForm = false; isArmyOfOne = false; }
            if (currentHunterSequence > 2) isCalamityGiant = false;
            if (currentMoonSequence > 7) isVampireWings = false;
            if (currentMoonSequence > 5) { isFullMoonActive = false; isMoonlightized = false; }
            if (currentMoonSequence > 4) isBatSwarm = false;
            if (currentMoonSequence > 2) isCreationDomain = false;

            arsonistFireImmune = false;

            ApplySequenceStats();
            ApplyMarauderStats();
            CheckConquerorRitual();


            // === 灾厄适配模式：强力压制 ===
            if (ModContent.GetInstance<LotMConfig>().CalamityAdaptationMode)
            {
                // 1. 伤害直接砍半再砍半
                Player.GetDamage(DamageClass.Generic) *= 0.3f;

                // 2. 削减暴击率
                Player.GetCritChance(DamageClass.Generic) -= 20;

                // 3. 削弱防御力
                Player.statDefense *= 0.5f;

                // 4. 限制伤害减免 (DR)
                Player.endurance *= 0.5f; // 所有的免伤效果减半

                // 5. 削弱生命恢复
                Player.lifeRegen /= 3;
                if (currentDemonessSequence <= 3)
                {
                    Player.lifeRegen /= 2; // 再次减半
                }
            }

        }

        // ===================================================
        // 4. 数值加成系统
        // ===================================================
        private void ApplySequenceStats()
        {
            float worldMult = Systems.BalanceSystem.GetWorldTierMultiplier();

            if (ModContent.GetInstance<Configs.LotMConfig>().EnableWorldRestriction)
            {
                float powerCap = 1.0f;

                // 设定阶段上限
                if (!Main.hardMode) powerCap = 0.15f;      // 肉山前：最多发挥 15% 实力
                else if (!NPC.downedMoonlord) powerCap = 0.6f; // 月后前：最多发挥 60% 实力
                else powerCap = 1.0f;                      // 毕业后：100% 实力

                // 强制压制 worldMult
                if (worldMult > powerCap) worldMult = powerCap;
            }

            float giantMult = GetSequenceMultiplier(currentSequence);
            float hunterMult = GetSequenceMultiplier(currentHunterSequence);
            float moonMult = GetSequenceMultiplier(currentMoonSequence);
            float foolMult = GetSequenceMultiplier(currentFoolSequence);
            float marauderMult = GetSequenceMultiplier(currentMarauderSequence);
            float sunMult = GetSequenceMultiplier(currentSunSequence);
            float demonessMult = GetSequenceMultiplier(currentDemonessSequence);
            float wheelMult = GetSequenceMultiplier(currentWheelSequence);

            // 2. 动态调整血量、伤害、防御的基础倍率
            // 原来的逻辑是直接 +5000 血，现在改为：世界越强，加成越高

            // --- 通用成长 ---
            float maxMult = Math.Max(giantMult, Math.Max(hunterMult, Math.Max(moonMult, foolMult)));
            if (maxMult > 1f)
            {
                Player.moveSpeed += 0.15f * maxMult;
                Player.maxRunSpeed += 1.5f * maxMult;
                Player.jumpSpeedBoost += 1.2f * (maxMult - 1f);
            }

            // ==========================================
            // 动态数值应用 (替换原版死数值)
            // ==========================================

            // --- 巨人/战士 ---
            if (currentSequence <= 9) { Player.statDefense += (int)(8 * giantMult * worldMult); Player.GetDamage(DamageClass.Melee) += 0.12f * giantMult; Player.GetCritChance(DamageClass.Melee) += 5; Player.statLifeMax2 += (int)(100 * giantMult * worldMult); }
            if (currentSequence <= 8) { Player.GetAttackSpeed(DamageClass.Melee) += 0.15f; Player.endurance += 0.05f; Player.noKnockback = true; }
            if (currentSequence <= 7) { Player.GetDamage(DamageClass.Generic) += 0.10f; Player.GetCritChance(DamageClass.Generic) += 5; Player.GetArmorPenetration(DamageClass.Generic) += 10 * giantMult; }
            if (currentSequence <= 6)
            {
                Lighting.AddLight(Player.Center, 1.5f, 1.5f, 1.5f);
                Player.statDefense += (int)(15 * worldMult);
                Player.lifeRegen += (int)(3 * giantMult);
                if (dawnArmorActive && !dawnArmorBroken)
                {
                    Player.statDefense += (int)(40 * giantMult * worldMult);
                    Player.endurance += 0.15f * giantMult;
                }
            }
            if (currentSequence <= 5)
            {
                Player.statDefense += (int)(20 * worldMult);
                Player.endurance += 0.05f;
                Player.buffImmune[BuffID.Confused] = true;
                if (isGuardianStance)
                {
                    Player.statDefense += (int)(100 * giantMult * worldMult);
                    Player.endurance += 0.3f * giantMult;
                }
            }
            if (currentSequence <= 4) { Player.statLifeMax2 += (int)(500 * giantMult * worldMult); Player.GetDamage(DamageClass.Generic) += 0.20f; Player.GetCritChance(DamageClass.Generic) += 10; Player.nightVision = true; Player.detectCreature = true; Player.buffImmune[BuffID.CursedInferno] = true; Player.buffImmune[BuffID.ShadowFlame] = true; }
            if (currentSequence <= 3) { Player.statDefense += (int)(30 * worldMult); Player.lifeRegen += 5; Player.GetAttackSpeed(DamageClass.Melee) += 0.20f; Player.blackBelt = true; }
            if (currentSequence <= 2) { Player.statLifeMax2 += (int)(2000 * worldMult); Player.statDefense += (int)(50 * worldMult); Player.endurance += 0.15f; Player.GetDamage(DamageClass.Generic) += 0.20f; }
            if (currentSequence <= 1)
            {
                Player.statLifeMax2 += (int)(5000 * worldMult);
                Player.statDefense += (int)(100 * worldMult);
                Player.endurance += 0.2f;
                Player.lifeRegen += 20;

                if (isGuardianStance)
                {
                    Player.endurance += 0.1f;
                    Player.thorns += 2.0f;
                }

                if (dawnArmorActive && !dawnArmorBroken)
                {
                    Player.statDefense += 50;
                }

                // 黄昏神性特效
                if (Main.GameUpdateCount % 10 == 0)
                {
                    Dust.NewDust(Player.position, Player.width, Player.height, DustID.OrangeTorch, 0, -2, 0, default, 1.5f);
                }
            }

            // --- 猎人途径 ---
            if (currentHunterSequence <= 9) { Player.GetDamage(DamageClass.Ranged) += 0.15f; Player.GetDamage(DamageClass.Melee) += 0.05f; Player.detectCreature = true; Player.dangerSense = true; }
            if (currentHunterSequence <= 8) { Player.statDefense += 10; Player.aggro += 300; Player.lifeRegen += 2; }
            if (currentHunterSequence <= 7){Player.GetDamage(DamageClass.Generic) += 0.15f * hunterMult;Player.buffImmune[BuffID.OnFire] = true; Player.buffImmune[BuffID.OnFire3] = true;Player.buffImmune[BuffID.Frostburn] = true;Player.resistCold = true;Player.lavaImmune = true;Player.fireWalk = true;}
            if (currentHunterSequence <= 6) { Player.GetCritChance(DamageClass.Generic) += 15; Player.manaCost -= 0.20f; }
            if (currentHunterSequence <= 5) { Player.GetArmorPenetration(DamageClass.Generic) += 30 * hunterMult; Player.GetCritChance(DamageClass.Generic) += 20; }
            if (currentHunterSequence <= 4) { Player.statDefense += (int)(50 * hunterMult * worldMult); Player.endurance += 0.10f; Player.maxMinions += 5; Player.noKnockback = true; }
            if (currentHunterSequence <= 3) { Player.maxMinions += 10; Player.maxTurrets += 3; Player.GetDamage(DamageClass.Summon) += 0.40f; }
            if (currentHunterSequence <= 2) { Player.statLifeMax2 += (int)(600 * worldMult); Player.statManaMax2 += 300; Player.GetDamage(DamageClass.Generic) += 0.40f; Player.buffImmune[BuffID.WindPushed] = true; }
            if (currentHunterSequence <= 1) { Player.statDefense += (int)(100 * worldMult); Player.endurance += 0.25f; Player.GetDamage(DamageClass.Generic) += 1.0f; Player.GetCritChance(DamageClass.Generic) += 40; Player.aggro += 2000; Player.buffImmune[BuffID.Weak] = true; Player.buffImmune[BuffID.BrokenArmor] = true; Player.buffImmune[BuffID.WitheredArmor] = true; Player.buffImmune[BuffID.WitheredWeapon] = true; }

            // --- 月亮途径 ---
            if (currentMoonSequence <= 9)
            {
                Player.buffImmune[BuffID.Poisoned] = true;
                Player.buffImmune[BuffID.Venom] = true;
                Player.detectCreature = true;
                Player.lifeRegen += (int)(3 * moonMult);
                Player.statLifeMax2 += (int)(30 * moonMult * worldMult);
            }
            if (currentMoonSequence <= 8) { Player.statDefense += 8; Player.GetDamage(DamageClass.Generic) += 0.15f * moonMult; Player.moveSpeed += 0.3f; Player.maxMinions += (int)(2 * moonMult); Player.dangerSense = true; }
            if (currentMoonSequence <= 7)
            {
                Player.statLifeMax2 += (int)(100 * worldMult);
                Player.lifeRegen += (int)(8 * moonMult);
                Player.moveSpeed += 0.3f;
                Player.noFallDmg = true;
                if (Main.dayTime && Player.ZoneOverworldHeight && Player.behindBackWall == false) { Player.statDefense -= 10; Player.lifeRegen -= 5; Player.GetDamage(DamageClass.Generic) -= 0.1f; }
            }
            if (currentMoonSequence <= 6) { Player.statManaMax2 += 100; Player.GetDamage(DamageClass.Magic) += 0.20f * moonMult; Player.pStone = true; Player.buffImmune[BuffID.OnFire] = true; Player.buffImmune[BuffID.Frostburn] = true; Player.buffImmune[BuffID.CursedInferno] = true; }
            if (currentMoonSequence <= 5)
            {
                Player.lifeRegen += 20;
                Player.buffImmune[BuffID.Confused] = true; Player.buffImmune[BuffID.Darkness] = true; Player.buffImmune[BuffID.Silenced] = true; Player.buffImmune[BuffID.Blackout] = true;
                if (isFullMoonActive) { Player.GetDamage(DamageClass.Magic) += 0.40f * moonMult; Player.statDefense -= 10; Lighting.AddLight(Player.Center, 0.6f, 0.7f, 0.9f); Player.manaRegen += (int)(30 * moonMult); Player.manaRegenDelayBonus += 5; }
            }
            if (currentMoonSequence <= 4) { Player.statLifeMax2 += (int)(300 * worldMult); Player.statManaMax2 += 200; Player.GetDamage(DamageClass.Magic) += 0.30f * moonMult; Player.GetDamage(DamageClass.Summon) += 0.30f * moonMult; }
            if (currentMoonSequence <= 3) { Player.maxMinions += (int)(5 * moonMult); Player.GetDamage(DamageClass.Summon) += 0.40f * moonMult; Player.GetKnockback(DamageClass.Summon) += 3f; Player.statManaMax2 += 300; Player.manaCost -= 0.25f; }
            if (currentMoonSequence <= 2)
            {
                Player.statLifeMax2 += (int)(1500 * worldMult);
                Player.lifeRegen += 60;
                Player.buffImmune[BuffID.Bleeding] = true; Player.buffImmune[BuffID.Poisoned] = true; Player.buffImmune[BuffID.Venom] = true; Player.buffImmune[BuffID.CursedInferno] = true; Player.buffImmune[BuffID.Ichor] = true; Player.buffImmune[BuffID.Frozen] = true;
                if (isCreationDomain) { Player.lifeRegen += 60; Player.statDefense += (int)(50 * worldMult); Lighting.AddLight(Player.Center, 0.1f, 0.8f, 0.2f); Player.flowerBoots = true; }
            }
            if (currentMoonSequence <= 1)
            {
                Player.statLifeMax2 += (int)(3000 * worldMult);
                Player.endurance += 0.20f;
                Player.GetDamage(DamageClass.Generic) += 0.80f;
                for (int i = 0; i < BuffID.Count; i++) { if (Main.debuff[i]) Player.buffImmune[i] = true; }
                if (isCreationDomain) { Player.GetDamage(DamageClass.Generic) += 0.50f; Lighting.AddLight(Player.Center, 1.0f, 0.4f, 0.7f); }
            }

            // --- 愚者途径 (The Fool) ---
            if (currentFoolSequence <= 9) { Player.GetDamage(DamageClass.Magic) += 0.10f * foolMult; Player.GetCritChance(DamageClass.Magic) += 5; Player.statManaMax2 += (int)(40 * foolMult); Player.dangerSense = true; if (isSpiritVisionActive) { if (!TryConsumeSpirituality(0.1f, true)) { isSpiritVisionActive = false; Main.NewText("灵性枯竭，灵视被迫中断！", 255, 50, 50); } else { Lighting.AddLight(Player.Center, 0.4f, 0.4f, 1.0f); Player.findTreasure = true; } } Player.luck += 0.5f * foolMult; }
            if (currentFoolSequence <= 8) { Player.moveSpeed += 0.3f; Player.jumpSpeedBoost += 1.5f; Player.accRunSpeed += 2.0f; Player.GetDamage(DamageClass.Generic) += 0.15f * foolMult; Player.GetAttackSpeed(DamageClass.Melee) += 0.15f; Player.GetCritChance(DamageClass.Generic) += 10; Player.blackBelt = true; Player.statManaMax2 += (int)(60 * foolMult); }
            if (currentFoolSequence <= 7)
            {
                Player.GetAttackSpeed(DamageClass.Generic) += 0.2f;
                Player.manaCost -= 0.15f;
                Player.buffImmune[BuffID.Webbed] = true;
                Player.buffImmune[BuffID.Stoned] = true;
                if (Player.wet) Player.gills = true;

                Player.ignoreWater = true;
                Player.statManaMax2 += (int)(100 * foolMult);
            }
            if (currentFoolSequence <= 6)
            {
                Player.accCritterGuide = true;
                Player.accStopwatch = true;
                Player.accOreFinder = true;
                Player.GetDamage(DamageClass.Generic) += 0.15f * foolMult;
                Player.GetCritChance(DamageClass.Generic) += 10;
                Player.GetDamage(DamageClass.Magic) += 0.3f * foolMult;
                Player.gills = true;
                if (isFacelessActive)
                {
                    if (Player.itemAnimation > 0)
                    {
                        isFacelessActive = false;
                        Main.NewText("攻击暴露了你的伪装！", 255, 50, 50);
                    }
                    if (isFacelessActive)
                    {
                        Player.aggro -= 1000;
                        Player.shroomiteStealth = true;
                        Player.statDefense += 10;
                        if (!TryConsumeSpirituality(1.0f, true))
                        {
                            isFacelessActive = false;
                            Main.NewText("灵性不足，伪装失效！", 255, 50, 50);
                        }
                    }
                }

                Player.statManaMax2 += (int)(150 * foolMult);
            }
            if (currentFoolSequence <= 5) { Player.detectCreature = true; Player.dangerSense = true; Player.findTreasure = true; Player.maxMinions += 3; Player.GetDamage(DamageClass.Magic) += 0.2f * foolMult; Player.statManaMax2 += (int)(200 * foolMult); }
            if (currentFoolSequence <= 4)
            {
                Player.statLifeMax2 += (int)(100 * worldMult); // 砍半
                Player.statDefense += 20;
                Player.GetDamage(DamageClass.Generic) += 0.2f * foolMult;
                Player.maxMinions += 7;
                Player.aggro -= 2000;
                Player.statManaMax2 += (int)(400 * foolMult);
            }
            if (currentFoolSequence <= 3)
            {
                Player.statLifeMax2 += (int)(100 * worldMult); // 砍半
                Player.statDefense += (int)(40 * worldMult);
                Player.GetDamage(DamageClass.Generic) += 0.4f * foolMult;
                if (isBorrowingPower)
                {
                    Player.GetDamage(DamageClass.Generic) += 0.5f;
                    Player.statDefense += 50;
                    Player.lifeRegen += 20;
                    Player.moveSpeed += 0.5f;
                    Player.endurance += 0.2f;
                    if (Main.rand.NextBool(3)) Dust.NewDust(Player.position, Player.width, Player.height, DustID.Smoke, 0, 0, 100, Color.Gray, 1.5f);
                }
            }
            if (currentFoolSequence <= 2)
            {
                Player.statLifeMax2 += (int)(100 * worldMult); // 砍半
                Player.statDefense += (int)(60 * worldMult);
                Player.GetDamage(DamageClass.Generic) += 0.5f * foolMult;

                if (fateDisturbanceActive)
                {
                    Player.GetCritChance(DamageClass.Generic) += 50;
                    Player.luck += 1.0f;
                    if (!TryConsumeSpirituality(2.0f, true))
                    {
                        fateDisturbanceActive = false;
                        Main.NewText("灵性不足，命运干扰停止。", 150, 150, 150);
                    }
                }
                Player.statManaMax2 += 1000;
            }
            if (currentFoolSequence <= 1)
            {
                Player.statLifeMax2 += (int)(100 * worldMult); // 砍半
                Player.statDefense += (int)(100 * worldMult);
                Player.GetDamage(DamageClass.Generic) += 1.0f * foolMult;
                Player.statManaMax2 += 1000;

                if (graftingMode == 2)
                {
                    Player.GetCritChance(DamageClass.Generic) += 100;
                    Player.GetArmorPenetration(DamageClass.Generic) += 9999;
                }
            }
            // 错误途径
            if (currentMarauderSequence <= 9) // 偷盗者
            {
                Player.statLifeMax2 += (int)(30 * worldMult);
                Player.moveSpeed += 0.05f;
            }

            if (currentMarauderSequence <= 8) // 诈骗师
            {
                Player.statLifeMax2 += (int)(50 * worldMult);
                Player.statDefense += (int)(4 * worldMult);
            }

            if (currentMarauderSequence <= 7) // 解密学者
            {
                Player.statLifeMax2 += (int)(75 * worldMult);
                Player.GetCritChance(DamageClass.Generic) += 5;
                Player.GetArmorPenetration(DamageClass.Generic) += 5;
            }
            if (currentMarauderSequence <= 6) // 盗火人
            {
                Player.statLifeMax2 += (int)(125 * worldMult);
                Player.statDefense += (int)(10 * worldMult);
                Player.GetDamage(DamageClass.Generic) += 0.05f;
            }

            if (currentMarauderSequence <= 5) // 窃梦家
            {
                Player.statLifeMax2 += (int)(200 * worldMult);
                Player.statManaMax2 += 50;
                Player.endurance += 0.05f;
            }

            if (currentMarauderSequence <= 4) // 寄生者 (半神)
            {
                Player.statLifeMax2 += (int)(400 * worldMult);
                Player.statDefense += (int)(20 * worldMult);
                Player.lifeRegen += 3;
            }

            if (currentMarauderSequence <= 3) // 欺瞒导师
            {
                Player.statLifeMax2 += (int)(600 * worldMult);
                Player.GetDamage(DamageClass.Generic) += 0.10f;
                Player.GetCritChance(DamageClass.Generic) += 5;
            }

            if (currentMarauderSequence <= 2) // 命运木马 (天使)
            {
                Player.statLifeMax2 += (int)(1000 * worldMult);
                Player.statDefense += (int)(30 * worldMult);
                Player.statManaMax2 += 100;
                Player.endurance += 0.05f;
            }

            if (currentMarauderSequence <= 1) // 时之虫 (天使之王)
            {
                Player.statLifeMax2 += (int)(1750 * worldMult);

                Player.statDefense += (int)(40 * worldMult);
                Player.GetDamage(DamageClass.Generic) += 0.15f;
                Player.GetAttackSpeed(DamageClass.Generic) += 0.15f;
                Player.endurance += 0.05f;
                Player.manaCost -= 0.25f;
            }
            // --- 新增：太阳途径 (Sun) ---
            if (currentSunSequence <= 9)
            {
                Player.statLifeMax2 += (int)(20 * worldMult); 
                Player.statDefense += (int)(4 * worldMult);
                Player.lifeRegen += 2;
            }

            if (currentSunSequence <= 8)
            {
                Lighting.AddLight(Player.Center, 1.2f, 1.1f, 0.9f);

                Player.statLifeMax2 += (int)(40 * worldMult);
                Player.statDefense += (int)(8 * worldMult);
            }
            if (currentSunSequence <= 7)
            {
                Player.buffImmune[BuffID.Horrified] = true;
                Player.buffImmune[BuffID.TheTongue] = true;

                Player.buffImmune[BuffID.Chilled] = true;
                Player.buffImmune[BuffID.Frozen] = true;
                Player.buffImmune[BuffID.Darkness] = true;
                Player.buffImmune[BuffID.Blackout] = true;
                Player.buffImmune[BuffID.Bleeding] = true; // 疾病抵抗
                Player.buffImmune[BuffID.Poisoned] = true;
                isCleansingSlash = true;
            }
            else
            {
                isCleansingSlash = false;
            }
            if (currentSunSequence <= 6)
            {
                Player.statLifeMax2 += (int)(100 * sunMult * worldMult); // 大幅加血
                Player.statDefense += (int)(15 * worldMult);             // 大幅加防
                Player.endurance += 0.1f;                                // 10% 伤害减免
                Player.discountAvailable = true; // 修正变量名
                Player.goldRing = true;       // 扩大金币拾取范围 (金戒指效果)
                Player.hasLuckyCoin = true;      // 幸运币 (攻击掉钱)  <-- 必须是这个名字
                Player.buffImmune[BuffID.Bleeding] = true;
                Player.buffImmune[BuffID.Poisoned] = true;
                Player.buffImmune[BuffID.OnFire] = true;
                Player.buffImmune[BuffID.Venom] = true;
                Player.buffImmune[BuffID.Weak] = true;
            }
            if (currentSunSequence <= 5)
            {
                Player.nightVision = true;      // 夜视
                Player.dangerSense = true;      // 危险感知 (看透污秽)
                Player.detectCreature = true;   // 生物探测
                Player.statLifeMax2 += (int)(200 * worldMult); // 血量再次飞跃
                Player.statDefense += (int)(20 * worldMult);
                Player.GetDamage(DamageClass.Generic) += 0.2f; // 全伤害+20%
            }
            if (currentSunSequence <= 4)
            {
                Player.statLifeMax2 += (int)(400 * worldMult); // 血量极高
                Player.statDefense += (int)(40 * worldMult);   // 神圣盔甲效果
                Player.endurance += 0.15f;                     // 额外15%免伤
                Player.lifeRegen += 10;                        // 极快回血
                Lighting.AddLight(Player.Center, 2.0f, 1.8f, 1.2f); // 极强的白金光
                Player.detectCreature = true;
                Player.dangerSense = true;
                Player.findTreasure = true; // 无暗者能发现所有隐藏
                Player.buffImmune[BuffID.Darkness] = true;
                Player.buffImmune[BuffID.Blackout] = true;
                Player.buffImmune[BuffID.Obstructed] = true;
            }
            if (currentSunSequence <= 3)
            {
                Player.statLifeMax2 += (int)(1000 * worldMult); // 圣者血量质变
                Player.statDefense += (int)(60 * worldMult);
                Player.endurance += 0.2f; // 20% 免伤
                Player.lifeRegen += 20;   // 极速再生
                Player.buffImmune[BuffID.Silenced] = true;
                Player.buffImmune[BuffID.Cursed] = true;
                Player.buffImmune[BuffID.Stoned] = true; // 免疫石化
                Player.buffImmune[BuffID.Webbed] = true;
                Player.buffImmune[BuffID.VortexDebuff] = true; // 免疫扭曲
                Player.maxMinions += 3;
            }
            if (currentSunSequence <= 2)
            {
                Player.buffImmune[BuffID.Darkness] = true;
                Player.buffImmune[BuffID.Blackout] = true;
                Player.buffImmune[BuffID.Chilled] = true;
                Player.buffImmune[BuffID.Frozen] = true;
                Player.buffImmune[BuffID.Frostburn] = true;
                Player.buffImmune[BuffID.Frostburn2] = true;
                Player.buffImmune[BuffID.Poisoned] = true;
                Player.buffImmune[BuffID.Venom] = true;
                Player.buffImmune[BuffID.WitheredArmor] = true;
                Player.buffImmune[BuffID.WitheredWeapon] = true;

                Player.dangerSense = true;
                Player.detectCreature = true;
                Player.findTreasure = true;
                Player.buffImmune[BuffID.Confused] = true; // 免疫混乱

                Player.statLifeMax2 += (int)(2000 * worldMult);
                Player.statDefense += (int)(80 * worldMult);
                Player.endurance += 0.25f; // 25% 免伤
                Player.lifeRegen += 40;    // 天使级回复

                if (Main.GameUpdateCount % 60 == 0)
                {
                    for (int i = 0; i < Player.MaxBuffs; i++)
                    {
                        if (Player.buffType[i] > 0 && Main.debuff[Player.buffType[i]])
                        {
                            Player.DelBuff(i);
                            i--;
                        }
                    }
                }
            }
            if (currentSunSequence <= 1)
            {
                Player.buffImmune[BuffID.OnFire] = true;
                Player.buffImmune[BuffID.OnFire3] = true;
                Player.buffImmune[BuffID.CursedInferno] = true;
                Player.buffImmune[BuffID.ShadowFlame] = true;
                Player.buffImmune[BuffID.Electrified] = true;
                Player.buffImmune[BuffID.Burning] = true;
                Player.buffImmune[BuffID.Suffocation] = true; // 不需要呼吸

                int townCount = 0;
                for (int i = 0; i < Main.maxNPCs; i++) if (Main.npc[i].active && Main.npc[i].townNPC) townCount++;

                Player.statDefense += (int)(townCount * 2 * worldMult); // 每个NPC提供额外防御
                Player.lifeRegen += townCount; // 每个NPC提供回血
                Player.statLifeMax2 += (int)(5000 * worldMult);
                Player.statDefense += (int)(150 * worldMult);
                Player.endurance += 0.3f; // 30% 免伤

                Player.GetDamage(DamageClass.Generic) += 0.5f;
            }

            // --- 新增：刺客(魔女)途径 (Demoness) ---
            if (currentDemonessSequence <= 9) // 序列9 刺客
            {
                // 削弱 75%: 30 -> 7
                Player.statLifeMax2 += (int)(5 * worldMult);

                Player.nightVision = true;      // 夜视药水效果
                Player.detectCreature = true;   // 生物分析仪效果
                Player.dangerSense = true;      // 危险感知

                Player.moveSpeed += 0.20f;
                Player.jumpSpeedBoost += 1.2f;
                Player.noFallDmg = true;
                Player.runAcceleration += 0.1f;

                Player.GetCritChance(DamageClass.Generic) += 10;
                Player.GetArmorPenetration(DamageClass.Generic) += 5;

                Player.aggro -= 200;
            }

            if (currentDemonessSequence <= 8) // 序列8 教唆者
            {
                // 削弱 75%: 50 -> 12
                Player.statLifeMax2 += (int)(10 * worldMult);

                instigatorEffect = true;
                Player.GetDamage(DamageClass.Summon) += 0.15f;
                Player.maxMinions += 1;
                Player.aggro -= 300;
            }

            if (currentDemonessSequence <= 7) // 序列7 女巫
            {
                // 削弱 75%: 75 -> 19
                Player.statLifeMax2 += (int)(20 * worldMult);

                Player.Male = false;
                Player.discountAvailable = true;
                Player.aggro -= 400;
                witchIceEffect = true;

                Player.GetDamage(DamageClass.Magic) += 0.20f;
                Player.manaCost -= 0.15f;
                Player.statManaMax2 += 60;

                if (Player.velocity.Length() < 1.5f)
                {
                    Player.shroomiteStealth = true;
                    Player.stealth = 0.1f;
                    Player.GetDamage(DamageClass.Generic) += 0.2f;
                }
            }

            if (currentDemonessSequence <= 6) // 序列6 欢愉魔女
            {
                // 削弱 75%: 125 -> 31
                Player.statLifeMax2 += (int)(30 * worldMult);

                pleasureDemonessEffect = true;
                Player.aggro -= 500;
                Player.GetCritChance(DamageClass.Magic) += 10;
                Player.GetDamage(DamageClass.Magic) += 0.15f;
                Player.statManaMax2 += 100;
            }

            if (currentDemonessSequence <= 5) // 序列5 痛苦魔女
            {
                isAfflictionDemoness = true;

                // 削弱 75%: 200 -> 50
                Player.statLifeMax2 += (int)(50 * worldMult);
                Player.GetDamage(DamageClass.Magic) += 0.2f;

                Player.lifeRegen += 2;

                Player.buffImmune[BuffID.Poisoned] = true;
                Player.buffImmune[BuffID.Venom] = true;
                Player.buffImmune[BuffID.Rabies] = true;
            }

            if (currentDemonessSequence <= 4) // 序列4 绝望魔女
            {
                // 削弱 75%: 400 -> 100
                Player.statLifeMax2 += (int)(100 * worldMult);

                Player.lifeRegen += 3;

                Player.GetDamage(DamageClass.Magic) += 0.3f;
                Player.endurance += 0.15f;

                Player.buffImmune[BuffID.OnFire] = true;
                Player.buffImmune[BuffID.Frostburn] = true;
                Player.buffImmune[BuffID.CursedInferno] = true;
            }

            if (currentDemonessSequence <= 3) // 序列3 不老魔女
            {
                // 削弱 75%: 600 -> 150
                Player.statLifeMax2 += (int)(150 * worldMult);

                Player.lifeRegen += 4;

                Player.GetDamage(DamageClass.Magic) += 0.4f;
                Player.buffImmune[BuffID.Slow] = true;
                Player.buffImmune[BuffID.Weak] = true;
                Player.buffImmune[BuffID.Silenced] = true;
            }

            if (currentDemonessSequence <= 2) // 序列2 灾难魔女 (天使)
            {
                // 削弱 75%: 1000 -> 250
                Player.statLifeMax2 += (int)(250 * worldMult);
                Player.lifeRegen += 5;

                Player.GetDamage(DamageClass.Magic) += 0.6f;

                Player.buffImmune[BuffID.OnFire] = true;
                Player.buffImmune[BuffID.OnFire3] = true;
                Player.buffImmune[BuffID.Frostburn] = true;
                Player.buffImmune[BuffID.Frostburn2] = true;
                Player.buffImmune[BuffID.Electrified] = true;
                Player.buffImmune[BuffID.Suffocation] = true;
                Player.buffImmune[BuffID.WindPushed] = true;

                if (Main.raining || Main.windSpeedCurrent > 20 || Player.ZoneSnow || Player.ZoneDesert)
                {
                    Player.moveSpeed += 0.5f;
                    Player.GetDamage(DamageClass.Generic) += 0.2f;
                }
            }

            if (currentDemonessSequence <= 1) // 序列1 末日魔女 (天使之王/从神)
            {
                // 削弱 75%: 1750 -> 437
                Player.statLifeMax2 += (int)(400 * worldMult);

                Player.lifeRegen += 6;
                Player.endurance += 0.4f;
                Player.manaCost -= 0.5f;

                for (int i = 0; i < BuffID.Count; i++)
                {
                    if (Main.debuff[i]) Player.buffImmune[i] = true;
                }
                float suppressionRange = 2000f;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (npc.active && !npc.friendly && npc.Distance(Player.Center) < suppressionRange)
                    {
                        if (!npc.boss)
                        {
                            npc.AddBuff(BuffID.Confused, 2);
                            npc.AddBuff(BuffID.Slow, 2);
                            npc.damage = 0;
                        }
                        else
                        {
                            npc.damage = (int)(npc.defDamage * 0.7f);
                        }
                    }
                }
            }
            // --- 新增：怪物(命运)途径 (Demoness) ---
            if (currentWheelSequence <= 9)
            {
                Player.dangerSense = true;
                Player.detectCreature = true;

                Player.GetCritChance(DamageClass.Generic) += 5;

                Player.statLifeMax2 += (int)(20 * worldMult); // 血量小幅提升
                Player.moveSpeed += 0.1f; // 动作稍微灵敏一点

                Player.nightVision = true;
            }
            if (currentWheelSequence <= 8)
            {
                Player.GetCritChance(DamageClass.Generic) += 10;
                Player.GetDamage(DamageClass.Melee) += 0.20f;
                Player.GetDamage(DamageClass.Ranged) += 0.20f;
                Player.statDefense += 5;       // 机器之躯，防御增加
                Player.moveSpeed += 0.15f;     // 动作更精准迅速
                Player.tileRangeX += 2;
                Player.tileRangeY += 2;
            }
            if (currentWheelSequence <= 7)
            {
                Player.GetCritChance(DamageClass.Generic) += (10 + luckFluctuation);
                Player.moveSpeed += 0.2f;
            }
            if (currentWheelSequence <= 6)
            {

                Player.statDefense += 10; // 护甲提升
                Player.endurance += 0.1f; // 10% 免伤
            }
            if (currentWheelSequence <= 5)
            {
                Player.statLifeMax2 += 100; // 赢家血厚一点，命硬
                Player.GetCritChance(DamageClass.Generic) += 15;
                spiritualityMax += 300;
                if (Player.luck > 0)
                {
                    Player.GetCritChance(DamageClass.Generic) += (int)(Player.luck * 5);
                    Player.moveSpeed += 0.01f * Player.luck;
                }
            }
            if (currentWheelSequence <= 4)
            {
                Player.statDefense += 25;       // 高额护甲
                Player.endurance += 0.15f;      // 15% 绝对免伤
                Player.moveSpeed += 0.3f;       // 极快移速
                Player.statLifeMax2 += 150;     // 半神血量质变
                Player.dangerSense = true;      // 察觉危险
                Player.detectCreature = true;   // 察觉生灵
                Player.nightVision = true;      // 穿透黑暗
                Player.GetCritChance(DamageClass.Generic) += 20;
            }
            // --- 序列3：怪人 (Anomaly) ---
            // 命运扭曲的诡异存在；几率事件全部偏向你，敌人则被概率链反噬
            if (currentWheelSequence <= 3)
            {
                Player.statLifeMax2 += (int)(600 * worldMult);   // 与其它途径序列3对齐
                Player.GetDamage(DamageClass.Generic) += 0.10f;  // +10% 通用伤
                Player.GetCritChance(DamageClass.Generic) += 10; // 累加到序列4，共 +30%
                // 暴击伤害+30% (怪人之眼) 在 ModifyHitNPC / ModifyHitNPCWithProj 钩子里实现
                Player.statDefense += 15;                         // 累加到 +40
                Player.endurance += 0.15f;                        // 累加到 30%

                // 命运骰子-命运庇护：30秒内额外50%伤害减免
                if (fateBlessingActiveTimer > 0)
                {
                    // endurance是乘法叠加，0.5代表减伤50%
                    // 注意:与原有endurance叠加方式是(1-e1)*(1-e2)，所以这里加0.5意味着再衰减50%剩余伤害
                    Player.endurance += 0.50f;
                }

                // 概率反噬：身上每一个减益让幸运 +0.2（最高+1.0）
                int debuffCount = 0;
                for (int i = 0; i < Player.MaxBuffs; i++)
                {
                    int buffType = Player.buffType[i];
                    if (buffType > 0 && Main.debuff[buffType] && !Main.buffNoTimeDisplay[buffType])
                    {
                        debuffCount++;
                    }
                }
                if (debuffCount > 5) debuffCount = 5;
                // 注：实际luck在ModifyLuck里处理；此处仅缓存，下方Luck钩子里读
                anomalyDebuffStack = debuffCount;
            }
            // --- 序列2：先知 (Prophet) ---
            // 命运的宠儿，完整的神话生物形态，三大下位权柄已成
            if (currentWheelSequence <= 2)
            {
                Player.statLifeMax2 += (int)(900 * worldMult);    // 累加到+1500
                Player.GetDamage(DamageClass.Generic) += 0.15f;   // 累加到+25%
                Player.GetCritChance(DamageClass.Generic) += 15;  // 累加到+45
                Player.statDefense += 25;                          // 累加到+65
                Player.endurance += 0.20f;                         // 累加到约50%

                // 命运启示激活期间的极致BUFF
                if (revelationActiveTimer > 0)
                {
                    Player.GetCritChance(DamageClass.Generic) += 100;   // 暴击率拉满
                    Player.GetDamage(DamageClass.Generic) += 0.50f;     // 启示中伤害+50%
                    Player.endurance += 0.50f;                          // 额外50%减伤
                    Player.GetAttackSpeed(DamageClass.Generic) += 0.50f;// 攻速+50%
                    Player.moveSpeed += 0.5f;                            // 移速+50% (你看到了最优路径)

                    // 命运庇护期间持续无敌帧
                    Player.SetImmuneTimeForAllTypes(2);
                }
                // 反噬期: 略微衰弱(代表预言代价)
                else if (revelationBackfireTimer > 0)
                {
                    Player.GetDamage(DamageClass.Generic) -= 0.05f;
                }

                // 水银之躯CD结束后,持续+1幸运(由ModifyLuck处理)
            }

            // --- 序列1：巨蛇 (Serpent of Mercury / 吞尾之蛇) ---
            // 从神级 - 命运的化身, 拥有循环 / 保存 / 选择 三大下位权柄
            if (currentWheelSequence <= 1)
            {
                Player.statLifeMax2 += (int)(2500 * worldMult);     // 累计 ~+6500+
                Player.GetDamage(DamageClass.Generic) += 0.50f;     // 累计 ~+90%
                Player.GetCritChance(DamageClass.Generic) += 35;    // 累计 ~+95
                Player.statDefense += 80;                            // 累计 ~+145
                Player.endurance += 0.20f;                           // 累计 ~70%
                Player.GetAttackSpeed(DamageClass.Generic) += 0.40f; // +40% 攻速
                Player.moveSpeed += 0.5f;                             // +50% 移速
                Player.lifeRegen += 20;                               // +10 hp/s

                // [隐藏命运] 免疫所有"标记/诅咒/追踪"类debuff
                Player.buffImmune[BuffID.Ichor] = true;
                Player.buffImmune[BuffID.CursedInferno] = true;
                Player.buffImmune[BuffID.BetsysCurse] = true;
                Player.buffImmune[BuffID.Daybreak] = true;
                Player.buffImmune[BuffID.ShadowFlame] = true;
                Player.buffImmune[BuffID.Frostburn] = true;
                Player.buffImmune[BuffID.OnFire] = true;
                Player.buffImmune[BuffID.OnFire3] = true;
                Player.buffImmune[BuffID.Venom] = true;
                Player.buffImmune[BuffID.Poisoned] = true;
                Player.buffImmune[BuffID.Bleeding] = true;
                Player.buffImmune[BuffID.BrokenArmor] = true;
                Player.buffImmune[BuffID.Weak] = true;
                Player.buffImmune[BuffID.Confused] = true;
                Player.buffImmune[BuffID.Slow] = true;
                Player.buffImmune[BuffID.Silenced] = true;
                Player.buffImmune[BuffID.Cursed] = true;
                Player.buffImmune[BuffID.Darkness] = true;
                Player.buffImmune[BuffID.Blackout] = true;
                Player.buffImmune[BuffID.Chilled] = true;
                Player.buffImmune[BuffID.Frozen] = true;
                Player.buffImmune[BuffID.WitheredArmor] = true;
                Player.buffImmune[BuffID.WitheredWeapon] = true;
                Player.buffImmune[BuffID.Horrified] = true;
                Player.buffImmune[BuffID.TheTongue] = true;
                Player.buffImmune[BuffID.Webbed] = true;
                Player.buffImmune[BuffID.Stinky] = true;

                // [水银相位] 每10秒进入3秒无敌
                if (mercuryPhaseTimer > 0)
                {
                    Player.SetImmuneTimeForAllTypes(2);
                    // 银色半透明表现
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        // 玩家中心持续银色拖影
                        for (int k = 0; k < 5; k++)
                        {
                            Vector2 offset = Main.rand.NextVector2Circular(20, 30);
                            Dust d = Dust.NewDustPerfect(Player.Center + offset,
                                DustID.SilverCoin, Main.rand.NextVector2Circular(2, 2), 0, default, 1.6f);
                            d.noGravity = true;
                            d.fadeIn = 1.2f;
                        }
                    }
                }

            }

            // ========================================================
            // === 学徒 / 门 途径 ===
            // ========================================================
            // --- 序列9: 学徒 (Apprentice) - 自由的足迹 ---
            if (currentDoorSequence <= 9)
            {
                Player.statLifeMax2 += (int)(50 * worldMult);    // 血量+50
                Player.GetDamage(DamageClass.Generic) += 0.05f;  // 通用伤害+5%
                Player.GetCritChance(DamageClass.Generic) += 5;  // 暴击+5
                Player.statDefense += 5;                          // 防御+5
                Player.moveSpeed += 0.20f;                        // 移速+20% (旅行家身躯)
                Player.pickSpeed -= 0.20f;                        // 挖掘加速 (穿越障碍的能力扩展)

                // CD递减
                if (doorOpenCooldown > 0) doorOpenCooldown--;
            }

            // --- 序列8: 戏法大师 (Trickster) - 各种奇怪但不强力的戏法 ---
            if (currentDoorSequence <= 8)
            {
                Player.statLifeMax2 += (int)(50 * worldMult);    // 血量+50 (累计+100)
                Player.GetDamage(DamageClass.Magic) += 0.10f;    // 法术伤害+10%
                Player.GetDamage(DamageClass.Generic) += 0.05f;  // 通用+5%(累计+10%)
                Player.GetCritChance(DamageClass.Generic) += 5;  // 暴击(累计+10)
                Player.statDefense += 8;                          // 防御(累计+13)
                Player.statManaMax2 += 60;                        // 法力+60
                Player.moveSpeed += 0.10f;                        // 移速(累计+30%)
                Player.jumpSpeedBoost += 1.0f;                    // 跳跃增强

                // 戏法CD递减
                if (trickCooldown > 0) trickCooldown--;
                if (trickEscapeCooldown > 0) trickEscapeCooldown--;

                // 电击附魔: 持续期间近战命中附加感电
                if (electricChargeTimer > 0)
                {
                    electricChargeTimer--;
                    // 手上电光粒子
                    if (Main.myPlayer == Player.whoAmI && Main.GameUpdateCount % 4 == 0)
                    {
                        Dust d = Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2Circular(20, 20),
                            DustID.Electric, Vector2.Zero, 0, default, 1.0f);
                        d.noGravity = true;
                    }
                }
            }

            // --- 序列7: 占星人 (Astrologer) - 灵性直觉 / 占卜 / 干扰 ---
            if (currentDoorSequence <= 7)
            {
                Player.statLifeMax2 += (int)(50 * worldMult);    // 血量+50 (累计+150)
                Player.GetDamage(DamageClass.Magic) += 0.10f;    // 法术伤(累计+20%)
                Player.GetDamage(DamageClass.Generic) += 0.05f;  // 通用(累计+15%)
                Player.GetCritChance(DamageClass.Generic) += 8;  // 暴击(累计+18)
                Player.statDefense += 10;                         // 防御(累计+23)
                Player.statManaMax2 += 60;                        // 法力(累计+120)
                Player.moveSpeed += 0.08f;                        // 移速(累计+38%)

                // === 占星术CD递减 ===
                if (astrologyCooldown > 0) astrologyCooldown--;
                if (astrologyIntuitionCooldown > 0) astrologyIntuitionCooldown--;
                if (astrologyActiveTimer > 0)
                {
                    astrologyActiveTimer--;
                    // 占卜激活时增益:
                    Player.GetDamage(DamageClass.Generic) += 0.25f;  // 伤害+25%
                    Player.GetCritChance(DamageClass.Generic) += 25; // 暴击+25
                    Player.endurance += 0.10f;                        // 减伤+10%
                    Player.lifeRegen += 8;                            // 回血+4hp/s

                    // 持续星象粒子
                    if (Main.myPlayer == Player.whoAmI && Main.GameUpdateCount % 3 == 0)
                    {
                        for (int k = 0; k < 3; k++)
                        {
                            float ang = Main.rand.NextFloat() * MathHelper.TwoPi;
                            float r = Main.rand.NextFloat(40, 80);
                            Vector2 pos = Player.Center + new Vector2((float)System.Math.Cos(ang), (float)System.Math.Sin(ang)) * r;
                            Dust d = Dust.NewDustPerfect(pos,
                                Main.rand.NextBool() ? DustID.GoldCoin : DustID.MagicMirror,
                                Vector2.Zero, 0, default, 1.3f);
                            d.noGravity = true;
                            d.fadeIn = 1.2f;
                        }
                    }
                }

                // === 灵性直觉 ===
                // 主动检测附近Boss/精英敌人,自动+减伤
                bool dangerNearby = false;
                foreach (NPC n in Main.ActiveNPCs)
                {
                    if (!n.friendly && !n.dontTakeDamage
                        && (n.boss || n.lifeMax > 500)
                        && n.Distance(Player.Center) < 1200f)
                    {
                        dangerNearby = true;
                        break;
                    }
                }
                if (dangerNearby)
                {
                    Player.endurance += 0.15f;  // 危险时+15%减伤

                    // 偶尔显示"危险预感"
                    if (Main.myPlayer == Player.whoAmI && Main.GameUpdateCount % 360 == 0)
                    {
                        CombatText.NewText(Player.getRect(), new Color(255, 200, 100), "⚠ 危险预感", true);
                    }
                }

                // === 干扰能力: 敌人随机混乱(灵性干扰) ===
                // 每5秒有几率随机让1只附近的非Boss敌人陷入混乱
                if (Main.netMode != NetmodeID.MultiplayerClient && Main.GameUpdateCount % 300 == 0)
                {
                    foreach (NPC n in Main.ActiveNPCs)
                    {
                        if (!n.friendly && !n.dontTakeDamage && !n.boss
                            && n.Distance(Player.Center) < 600f && Main.rand.NextBool(3))
                        {
                            n.AddBuff(BuffID.Confused, 180);
                            n.netUpdate = true;
                            break;
                        }
                    }
                }

                // === 闪避加成 ===
                // 灵性直觉让玩家闪避更多攻击 (FreeDodge里已处理)
            }

            // --- 序列6: 记录官 (Recorder) - 记录敌人非凡能力为己用 ---
            // 设计借鉴蠕动的饥饿: 记录具体NPC ID, 使用时释放该敌人专属能力
            if (currentDoorSequence <= 6)
            {
                Player.statLifeMax2 += (int)(50 * worldMult);    // 血量+50 (累计+200)
                Player.GetDamage(DamageClass.Magic) += 0.10f;    // 法术伤(累计+30%)
                Player.GetDamage(DamageClass.Generic) += 0.05f;  // 通用(累计+20%)
                Player.GetCritChance(DamageClass.Generic) += 5;  // 暴击(累计+23)
                Player.statDefense += 10;                         // 防御(累计+33)
                Player.statManaMax2 += 40;                        // 法力(累计+160)
                Player.lifeRegen += 4;                            // 回血+2hp/s

                // CD递减
                if (recorderCooldown > 0) recorderCooldown--;
            }

            // --- 序列5: 旅行家 (Traveler) - 定位/传送/无形之手 ---
            if (currentDoorSequence <= 5)
            {
                Player.statLifeMax2 += (int)(50 * worldMult);    // 血量+50 (累计+250)
                Player.GetDamage(DamageClass.Magic) += 0.10f;    // 法术伤(累计+40%)
                Player.GetDamage(DamageClass.Generic) += 0.08f;  // 通用(累计+28%)
                Player.GetCritChance(DamageClass.Generic) += 7;  // 暴击(累计+30)
                Player.statDefense += 12;                         // 防御(累计+45)
                Player.statManaMax2 += 40;                        // 法力(累计+200)
                Player.moveSpeed += 0.10f;                        // 移速加成

                // === 定位 (被动) ===
                // 见 SequenceInfoUI 显示坐标

                // === 无形之手: 范围内物品自动飞向玩家 ===
                float pullRange = 320f; // 20格
                for (int i = 0; i < Main.maxItems; i++)
                {
                    Item it = Main.item[i];
                    if (!it.active || it.IsAir || it.noGrabDelay > 0) continue;
                    float dist = it.Distance(Player.Center);
                    if (dist < pullRange && dist > 30f)
                    {
                        Vector2 toPlayer = (Player.Center - it.Center).SafeNormalize(Vector2.UnitY);
                        it.velocity = Vector2.Lerp(it.velocity, toPlayer * 12f, 0.3f);
                    }
                }

                // === 记录官能力提升: 神性记录上限+3 (总共4个) ===
                recorderDivineMaxOverride = 4;

                // CD递减
                if (travelerGateCooldown > 0) travelerGateCooldown--;
                if (travelerBlinkCooldown > 0) travelerBlinkCooldown--;
            }
            else
            {
                recorderDivineMaxOverride = 0; // 序列高于5时恢复默认
            }
        }
        public override void ModifyLuck(ref float luck)
        {
            bool hasBadLuckBuff = Player.HasBuff(ModContent.BuffType<ExtremeBadLuckBuff>());
            bool isWinner = currentWheelSequence <= 5 && currentWheelSequence > 0;

            // --- 逻辑核心 ---

            if (hasBadLuckBuff)
            {
                luck -= 10.0f;
            }
            // --- 序列9：怪物 ---
            if (currentWheelSequence <= 9)
            {
                luck += 0.2f; 
            }

            // --- 序列7：幸运儿 ---
            if (currentWheelSequence <= 7)
            {
                luck += 0.5f; 
                luck += luckFluctuation * 0.01f; 
            }
            if (currentWheelSequence <= 6)
            {
                luck += 0.3f; 
            }
            if (currentWheelSequence <= 5)
            {
                luck += 1.0f;
            }
            if (currentWheelSequence <= 4)
            {
                luck += 1.0f;
            }
            // --- 序列3：怪人 ---
            if (currentWheelSequence <= 3)
            {
                luck += 0.5f;                              // 怪人本体加成
                luck += anomalyDebuffStack * 0.2f;         // 概率反噬：每个减益+0.2
            }
            // --- 序列2：先知 (累加) ---
            if (currentWheelSequence <= 2)
            {
                luck += 2.0f;                              // 命运的宠儿
                // 水银之躯回避CD结束后, 持续+1幸运
                if (mercuryDodgeTimer == 0 && mercuryDodgeCooldown == 0)
                {
                    luck += 1.0f;
                }
            }
            // --- 序列1：巨蛇 (从神级累加) ---
            if (currentWheelSequence <= 1)
            {
                luck += 5.0f;                              // 命运的化身
            }
        }
        private void ProcessRealmOfMysteries()
        {
            // 1. 缓存变量
            Vector2 playerCenter = Player.Center;
            float rangeSQ = realmRange * realmRange; // 平方距离
            int spiritDebuff = ModContent.BuffType<Buffs.SpiritControlDebuff>();

            // 2. A. 压制敌人
            // 使用 for 循环遍历 ActiveNPCs，性能微优于 foreach
            foreach (NPC npc in Main.ActiveNPCs)
            {
                // 使用 DistanceSQ 代替 Distance，省去开根号运算，性能提升很大
                if (!npc.friendly && !npc.dontTakeDamage && npc.Center.DistanceSQ(playerCenter) < rangeSQ)
                {
                    if (!npc.HasBuff(BuffID.Slow)) npc.AddBuff(BuffID.Slow, 60);
                    if (!npc.HasBuff(spiritDebuff)) npc.AddBuff(spiritDebuff, 60);

                    if (Main.rand.NextBool(50)) npc.AddBuff(BuffID.Confused, 120);
                }
            }

            // 3. B. 再生：吞噬掉落物
            // 使用 for 循环遍历数组，比 foreach 更快且无垃圾回收(GC)压力
            for (int i = 0; i < Main.maxItems; i++)
            {
                Item item = Main.item[i];
                
                // 先判断 active，这步最快，能过滤掉绝大多数空槽位
                if (!item.active || item.value <= 0) continue;

                // 距离判定优化
                float distSQ = item.Center.DistanceSQ(playerCenter);
                
                if (distSQ < rangeSQ)
                {
                    // 吸取逻辑：让物品飞向玩家
                    item.velocity = (playerCenter - item.Center).SafeNormalize(Vector2.Zero) * 15f;

                    // 接触判定 (60 * 60 = 3600)
                    if (distSQ < 3600f)
                    {
                        int value = item.value * item.stack;
                        int heal = Math.Max(1, value / 1000);
                        if (heal > 50) heal = 50;

                        Player.statLife += heal;
                        Player.HealEffect(heal);

                        // 彻底删除
                        item.TurnToAir();
                        item.active = false;

                        // 限制粒子数量
                        if (Main.rand.NextBool(3)) 
                        {
                             Dust d = Dust.NewDustPerfect(playerCenter, DustID.SpectreStaff, Main.rand.NextVector2Circular(2f, 2f), 0, default, 1.5f);
                             d.noGravity = true;
                        }
                    }
                }
            }
        }
        private void ApplyMarauderStats()
        {
            float marauderMult = GetSequenceMultiplier(currentMarauderSequence);

            // 1. 序列9 偷盗者
            if (currentMarauderSequence <= 9)
            {
                Player.pickSpeed -= 0.1f * marauderMult;
                Player.wallSpeed += 0.1f * marauderMult;
                Player.tileSpeed += 0.1f * marauderMult;
                Player.treasureMagnet = true; 
                Player.goldRing = true;
                Player.findTreasure = true;
                if (Player.HeldItem.useStyle == ItemUseStyleID.Rapier)
                {
                    // 只有手持短剑时才生效
                    Player.GetDamage(DamageClass.Melee) += 0.4f * marauderMult; // 伤害+40% (非常高，让短剑能用)
                    Player.GetAttackSpeed(DamageClass.Melee) += 0.3f;           // 攻速+30%
                    Player.GetCritChance(DamageClass.Melee) += 10;              // 暴击+10%
                    Player.GetArmorPenetration(DamageClass.Melee) += 10;        // 穿透+10
                }
            }

            // 2. 序列8 诈骗师
            if (currentMarauderSequence <= 8)
            {
                Player.discountAvailable = true;
                Player.moveSpeed += 0.2f;
                Player.runAcceleration += 0.1f;
            }

            // 3. 序列7 解密学者
            if (currentMarauderSequence <= 7)
            {
                Player.detectCreature = true;
                Player.dangerSense = true;
                Player.GetArmorPenetration(DamageClass.Generic) += 10;
                Player.GetCritChance(DamageClass.Generic) += 5;
            }

            // 4. 序列6 盗火人
            if (currentMarauderSequence <= 6)
            {
                Player.statDefense += 15;
                Player.moveSpeed += 0.4f;
                Player.GetDamage(DamageClass.Generic) += 0.15f;
                Player.lifeRegen += 3;
                Player.buffImmune[BuffID.Confused] = true;
                Player.buffImmune[BuffID.Darkness] = true;
                Player.buffImmune[BuffID.Blackout] = true;
                Player.buffImmune[BuffID.Obstructed] = true;
                Player.findTreasure = true;
                Lighting.AddLight(Player.Center, 0.8f, 0.6f, 0.0f);
            }

            // 5. 序列5 窃梦家
            if (currentMarauderSequence <= 5)
            {
                Player.manaMagnet = true;
                Player.lifeMagnet = true;

                // 梦魇光环
                int auraRadius = 300;
                foreach (NPC target in Main.ActiveNPCs)
                {
                    if (target.active && !target.friendly && !target.dontTakeDamage && target.Distance(Player.Center) < auraRadius)
                    {
                        target.AddBuff(BuffID.Slow, 10);
                        target.AddBuff(BuffID.Ichor, 10);
                        if (Main.rand.NextBool(30)) Dust.NewDust(target.position, target.width, target.height, DustID.DungeonSpirit, 0, 0, 150, default, 0.8f);
                    }
                }

                // 盗天机
                bool inCombat = (Player.aggro > -1000 && Player.itemAnimation > 0);
                if (inCombat && Main.GameUpdateCount % 180 == 0 && Main.rand.NextBool(3))
                {
                    int[] stolenBuffs = { BuffID.Rage, BuffID.Wrath, BuffID.Endurance, BuffID.Lifeforce, BuffID.Ironskin, BuffID.Regeneration, BuffID.MagicPower, BuffID.Titan };
                    int buff = stolenBuffs[Main.rand.Next(stolenBuffs.Length)];
                    Player.AddBuff(buff, 600);
                    CombatText.NewText(Player.getRect(), new Color(100, 149, 237), "盗天机!", false, true);
                }
            }
        }
        // ===================================================
        // 5. 视觉特效与翅膀
        // ===================================================
        public override void FrameEffects()
        {
            if (currentSequence <= 6 && dawnArmorActive && !dawnArmorBroken) { int dyeId = GameShaders.Armor.GetShaderIdFromItemId(ItemID.BrightSilverDye); Player.cHead = dyeId; Player.cBody = dyeId; Player.cLegs = dyeId; }
            if (isVampireWings) { Player.wings = 12; int blackDyeId = GameShaders.Armor.GetShaderIdFromItemId(ItemID.BlackDye); Player.cWings = blackDyeId; }
            if (isMoonlightized) { int redDyeId = GameShaders.Armor.GetShaderIdFromItemId(ItemID.RedAcidDye); Player.cHead = redDyeId; Player.cBody = redDyeId; Player.cLegs = redDyeId; }

            //亵渎之牌换肤
            if (isFoolCardEquipped)
            {

                var godHead = Terraria.ID.ContentSamples.ItemsByType[ItemID.NebulaHelmet];
                var godBody = Terraria.ID.ContentSamples.ItemsByType[ItemID.NebulaBreastplate];

                var godLegs = Terraria.ID.ContentSamples.ItemsByType[ItemID.DjinnsCurse];

                Player.head = godHead.headSlot;
                Player.body = godBody.bodySlot;
                Player.legs = godLegs.legSlot;

                int dyeId = GameShaders.Armor.GetShaderIdFromItemId(ItemID.MirageDye);

                Player.cHead = dyeId;
                Player.cBody = dyeId;
                Player.cLegs = dyeId;
            }
            if (isStrengthCardEquipped)
            {
                var head = Terraria.ID.ContentSamples.ItemsByType[ItemID.SolarFlareHelmet];
                var body = Terraria.ID.ContentSamples.ItemsByType[ItemID.SolarFlareBreastplate];
                var legs = Terraria.ID.ContentSamples.ItemsByType[ItemID.SolarFlareLeggings];

                Player.head = head.headSlot;
                Player.body = body.bodySlot;
                Player.legs = legs.legSlot;

                int dyeId = GameShaders.Armor.GetShaderIdFromItemId(ItemID.ReflectiveSilverDye);
                Player.cHead = dyeId;
                Player.cBody = dyeId;
                Player.cLegs = dyeId;
            }
            if (isLoversCardEquipped)
            {
                var godHead = Terraria.ID.ContentSamples.ItemsByType[ItemID.VortexHelmet];
                var godBody = Terraria.ID.ContentSamples.ItemsByType[ItemID.VortexBreastplate];
                var godLegs = Terraria.ID.ContentSamples.ItemsByType[ItemID.VortexLeggings];

                Player.head = godHead.headSlot;
                Player.body = godBody.bodySlot;
                Player.legs = godLegs.legSlot;

                int dyeId = GameShaders.Armor.GetShaderIdFromItemId(ItemID.PhaseDye);

                Player.cHead = dyeId;
                Player.cBody = dyeId;
                Player.cLegs = dyeId;
            }
            if (isRedPriestCardEquipped)
            {
                var godHead = Terraria.ID.ContentSamples.ItemsByType[ItemID.SolarFlareHelmet];
                var godBody = Terraria.ID.ContentSamples.ItemsByType[ItemID.SolarFlareBreastplate];
                var godLegs = Terraria.ID.ContentSamples.ItemsByType[ItemID.SolarFlareLeggings];

                Player.head = godHead.headSlot;
                Player.body = godBody.bodySlot;
                Player.legs = godLegs.legSlot;

                int dyeId = GameShaders.Armor.GetShaderIdFromItemId(ItemID.RedAcidDye);

                Player.cHead = dyeId;
                Player.cBody = dyeId;
                Player.cLegs = dyeId;
            }
            if (isSunCardEquipped)
            {
                var godHead = Terraria.ID.ContentSamples.ItemsByType[ItemID.AncientHallowedMask];
                var godBody = Terraria.ID.ContentSamples.ItemsByType[ItemID.AncientHallowedPlateMail];
                var godLegs = Terraria.ID.ContentSamples.ItemsByType[ItemID.AncientHallowedGreaves];

                Player.head = godHead.headSlot;
                Player.body = godBody.bodySlot;
                Player.legs = godLegs.legSlot;

                int dyeId = GameShaders.Armor.GetShaderIdFromItemId(ItemID.ReflectiveGoldDye);

                Player.cHead = dyeId;
                Player.cBody = dyeId;
                Player.cLegs = dyeId;
            }
            if (isMoonCardEquipped)
            {
                var moonMask = Terraria.ID.ContentSamples.ItemsByType[ItemID.MoonMask];
                Player.head = moonMask.headSlot;

                var tuxBody = Terraria.ID.ContentSamples.ItemsByType[ItemID.TuxedoShirt];
                Player.body = tuxBody.bodySlot;

                var tuxLegs = Terraria.ID.ContentSamples.ItemsByType[ItemID.TuxedoPants];
                Player.legs = tuxLegs.legSlot;

                var redCape = Terraria.ID.ContentSamples.ItemsByType[ItemID.RedCape];
                Player.back = redCape.backSlot;

                int silverDye = GameShaders.Armor.GetShaderIdFromItemId(ItemID.ReflectiveSilverDye);
                Player.cHead = silverDye;

                int grimDye = GameShaders.Armor.GetShaderIdFromItemId(ItemID.GrimDye);
                Player.cBody = grimDye; // 染衣服
                Player.cLegs = grimDye; // 染裤子
            }
            if (isDoorCardEquipped)
            {
                var starHead = Terraria.ID.ContentSamples.ItemsByType[ItemID.VortexHelmet];
                var starBody = Terraria.ID.ContentSamples.ItemsByType[ItemID.VortexBreastplate];
                var starLegs = Terraria.ID.ContentSamples.ItemsByType[ItemID.VortexLeggings];

                Player.head = starHead.headSlot;
                Player.body = starBody.bodySlot;
                Player.legs = starLegs.legSlot;

                int spaceDye = GameShaders.Armor.GetShaderIdFromItemId(ItemID.MartianArmorDye);

                Player.cHead = spaceDye;
                Player.cBody = spaceDye;
                Player.cLegs = spaceDye;
            }
            if (isWhiteTowerCardEquipped)
            {
                var sageHead = Terraria.ID.ContentSamples.ItemsByType[ItemID.HallowedHeadgear];
                var sageBody = Terraria.ID.ContentSamples.ItemsByType[ItemID.HallowedPlateMail]; // 自带披风
                var sageLegs = Terraria.ID.ContentSamples.ItemsByType[ItemID.HallowedGreaves];

                Player.head = sageHead.headSlot;
                Player.body = sageBody.bodySlot;
                Player.legs = sageLegs.legSlot;

                int whiteDye = GameShaders.Armor.GetShaderIdFromItemId(ItemID.ReflectiveSilverDye);

                Player.cHead = whiteDye;
                Player.cBody = whiteDye;
                Player.cLegs = whiteDye;
            }
            if (isBlackEmperorCardEquipped)
            {
                var crown = Terraria.ID.ContentSamples.ItemsByType[ItemID.GoldCrown];
                Player.head = crown.headSlot;

                var armorBody = Terraria.ID.ContentSamples.ItemsByType[ItemID.HallowedPlateMail];
                Player.body = armorBody.bodySlot;

                var armorLegs = Terraria.ID.ContentSamples.ItemsByType[ItemID.HallowedGreaves];
                Player.legs = armorLegs.legSlot;

                int blackDye = GameShaders.Armor.GetShaderIdFromItemId(ItemID.ShadowDye);

                Player.cHead = blackDye;
                Player.cBody = blackDye;
                Player.cLegs = blackDye;
            }
            if (isTyrantCardEquipped)
            {
                var tyrantHead = Terraria.ID.ContentSamples.ItemsByType[ItemID.HallowedMask];
                Player.head = tyrantHead.headSlot;

                var tyrantBody = Terraria.ID.ContentSamples.ItemsByType[ItemID.HallowedPlateMail];
                Player.body = tyrantBody.bodySlot;

                var tyrantLegs = Terraria.ID.ContentSamples.ItemsByType[ItemID.HallowedGreaves];
                Player.legs = tyrantLegs.legSlot;

                int oceanDye = GameShaders.Armor.GetShaderIdFromItemId(ItemID.LivingOceanDye);

                Player.cHead = oceanDye;
                Player.cBody = oceanDye;
                Player.cLegs = oceanDye;
            }
            if (isHangedManCardEquipped)
            {
                var cultHead = Terraria.ID.ContentSamples.ItemsByType[ItemID.ReaperHood];
                Player.head = cultHead.headSlot;

                var cultBody = Terraria.ID.ContentSamples.ItemsByType[ItemID.ReaperRobe];
                Player.body = cultBody.bodySlot;

                var blackPants = Terraria.ID.ContentSamples.ItemsByType[ItemID.TuxedoPants];
                Player.legs = blackPants.legSlot;

                int chaosDye = GameShaders.Armor.GetShaderIdFromItemId(ItemID.TwilightDye);

                Player.cHead = chaosDye;
                Player.cBody = chaosDye;
                Player.cLegs = chaosDye;
            }
            if (isDeathCardEquipped)
            {
                var ghostHead = Terraria.ID.ContentSamples.ItemsByType[ItemID.SpectreHood];
                Player.head = ghostHead.headSlot;

                var ghostBody = Terraria.ID.ContentSamples.ItemsByType[ItemID.SpectreRobe];
                Player.body = ghostBody.bodySlot;

                var ghostLegs = Terraria.ID.ContentSamples.ItemsByType[ItemID.SpectrePants];
                Player.legs = ghostLegs.legSlot;

                int voidDye = GameShaders.Armor.GetShaderIdFromItemId(ItemID.VoidDye);

                Player.cHead = voidDye;
                Player.cBody = voidDye;
                Player.cLegs = voidDye;

            }
            if (isDarknessCardEquipped)
            {
                var starHelm = Terraria.ID.ContentSamples.ItemsByType[ItemID.StardustHelmet];
                Player.head = starHelm.headSlot;

                var starBody = Terraria.ID.ContentSamples.ItemsByType[ItemID.StardustBreastplate];
                Player.body = starBody.bodySlot;

                var starLegs = Terraria.ID.ContentSamples.ItemsByType[ItemID.StardustLeggings];
                Player.legs = starLegs.legSlot;

                int nightDye = GameShaders.Armor.GetShaderIdFromItemId(ItemID.MidnightRainbowDye);

                Player.cHead = nightDye;
                Player.cBody = nightDye;
                Player.cLegs = nightDye;
            }
            if (isJusticiarCardEquipped)
            {
                var ironHead = Terraria.ID.ContentSamples.ItemsByType[ItemID.TitaniumMask];
                Player.head = ironHead.headSlot;

                var ironBody = Terraria.ID.ContentSamples.ItemsByType[ItemID.TitaniumBreastplate];
                Player.body = ironBody.bodySlot;

                var ironLegs = Terraria.ID.ContentSamples.ItemsByType[ItemID.TitaniumLeggings];
                Player.legs = ironLegs.legSlot;

                int lawDye = GameShaders.Armor.GetShaderIdFromItemId(ItemID.ReflectiveMetalDye);

                Player.cHead = lawDye;
                Player.cBody = lawDye;
                Player.cLegs = lawDye;
            }
            if (isDemonessCardEquipped)
            {
                var witchHead = Terraria.ID.ContentSamples.ItemsByType[ItemID.NebulaHelmet];
                Player.head = witchHead.headSlot;

                var witchBody = Terraria.ID.ContentSamples.ItemsByType[ItemID.NebulaBreastplate];
                Player.body = witchBody.bodySlot;

                var witchLegs = Terraria.ID.ContentSamples.ItemsByType[ItemID.NebulaLeggings];
                Player.legs = witchLegs.legSlot;

                int painDye = GameShaders.Armor.GetShaderIdFromItemId(ItemID.GrimDye);

                Player.cHead = painDye;
                Player.cBody = painDye;
                Player.cLegs = painDye;
            }
            if (isAbyssCardEquipped)
            {
                var devilHead = Terraria.ID.ContentSamples.ItemsByType[ItemID.MoltenHelmet];
                Player.head = devilHead.headSlot;

                var devilBody = Terraria.ID.ContentSamples.ItemsByType[ItemID.MoltenBreastplate];
                Player.body = devilBody.bodySlot;

                var devilLegs = Terraria.ID.ContentSamples.ItemsByType[ItemID.MoltenGreaves];
                Player.legs = devilLegs.legSlot;

                int hellDye = GameShaders.Armor.GetShaderIdFromItemId(ItemID.BurningHadesDye);

                Player.cHead = hellDye;
                Player.cBody = hellDye;
                Player.cLegs = hellDye;
            }
            if (isChainedCardEquipped)
            {
                var mummyHead = Terraria.ID.ContentSamples.ItemsByType[ItemID.MummyMask];
                Player.head = mummyHead.headSlot;

                var mummyBody = Terraria.ID.ContentSamples.ItemsByType[ItemID.MummyShirt];
                Player.body = mummyBody.bodySlot;

                var mummyLegs = Terraria.ID.ContentSamples.ItemsByType[ItemID.MummyPants];
                Player.legs = mummyLegs.legSlot;

                int shadowDye = GameShaders.Armor.GetShaderIdFromItemId(ItemID.ShadowDye);

                Player.cHead = shadowDye;
                Player.cBody = shadowDye;
                Player.cLegs = shadowDye;
            }
            if (isHermitCardEquipped)
            {
                var runeHead = Terraria.ID.ContentSamples.ItemsByType[ItemID.RuneHat];
                Player.head = runeHead.headSlot;

                var runeBody = Terraria.ID.ContentSamples.ItemsByType[ItemID.RuneRobe];
                Player.body = runeBody.bodySlot;

                var genericLegs = Terraria.ID.ContentSamples.ItemsByType[ItemID.TuxedoPants];
                Player.legs = genericLegs.legSlot;

                int mysteryDye = GameShaders.Armor.GetShaderIdFromItemId(ItemID.NebulaDye);

                Player.cHead = mysteryDye;
                Player.cBody = mysteryDye;
                Player.cLegs = mysteryDye;
            }
            if (isPerfectionistCardEquipped)
            {
                var mechHead = Terraria.ID.ContentSamples.ItemsByType[ItemID.ShroomiteHelmet];
                Player.head = mechHead.headSlot;

                var mechBody = Terraria.ID.ContentSamples.ItemsByType[ItemID.ShroomiteBreastplate];
                Player.body = mechBody.bodySlot;

                var mechLegs = Terraria.ID.ContentSamples.ItemsByType[ItemID.ShroomiteLeggings];
                Player.legs = mechLegs.legSlot;

                int steamDye = GameShaders.Armor.GetShaderIdFromItemId(ItemID.ReflectiveCopperDye);

                Player.cHead = steamDye;
                Player.cBody = steamDye;
                Player.cLegs = steamDye;

            }
            if (isMotherCardEquipped)
            {
                var natureHead = Terraria.ID.ContentSamples.ItemsByType[ItemID.ChlorophyteMask];
                Player.head = natureHead.headSlot;

                var natureBody = Terraria.ID.ContentSamples.ItemsByType[ItemID.ChlorophytePlateMail];
                Player.body = natureBody.bodySlot;

                var natureLegs = Terraria.ID.ContentSamples.ItemsByType[ItemID.ChlorophyteGreaves];
                Player.legs = natureLegs.legSlot;

                int lifeDye = GameShaders.Armor.GetShaderIdFromItemId(ItemID.GreenFlameDye);

                Player.cHead = lifeDye;
                Player.cBody = lifeDye;
                Player.cLegs = lifeDye;

            }
            if (isWheelOfFortuneCardEquipped)
            {
                var fateHead = Terraria.ID.ContentSamples.ItemsByType[ItemID.AncientHallowedHeadgear];
                Player.head = fateHead.headSlot;

                var fateBody = Terraria.ID.ContentSamples.ItemsByType[ItemID.AncientHallowedPlateMail];
                Player.body = fateBody.bodySlot;

                var fateLegs = Terraria.ID.ContentSamples.ItemsByType[ItemID.AncientHallowedGreaves];
                Player.legs = fateLegs.legSlot;

                int mercuryDye = GameShaders.Armor.GetShaderIdFromItemId(ItemID.ReflectiveSilverDye);

                Player.cHead = mercuryDye;
                Player.cBody = mercuryDye;
                Player.cLegs = mercuryDye;
            }
        }
        public override void PostUpdateEquips()
        {
            // 这是一个好习惯，虽然目前 base 可能没做什么，但保留它是个保险
            base.PostUpdateEquips();
            if (blasphemyCardEquippedCount > 1)
            {
                Player.AddBuff(Terraria.ID.BuffID.OnFire3, 2);
                var reason = Terraria.DataStructures.PlayerDeathReason.ByCustomReason(
                    Terraria.Localization.NetworkText.FromLiteral(Player.name + " 无法承受复数亵渎之牌的力量，灵体崩溃了！")
                );
                Player.KillMe(reason, 999999, 0);
            }
        }

        public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {
            if (isBatSwarm || isSunMessenger || isMercuryForm)
            {
                // 步骤A: 告诉游戏引擎“隐藏玩家整体”
                drawInfo.hideEntirePlayer = true;

                // 步骤B: 将透明度设为 0 (双重保险)
                a = 0f;

                drawInfo.drawPlayer.head = 0;
                drawInfo.drawPlayer.body = 0;
                drawInfo.drawPlayer.legs = 0;

                drawInfo.drawPlayer.wings = 0;
                drawInfo.drawPlayer.back = 0;
                drawInfo.drawPlayer.front = 0;
                drawInfo.drawPlayer.shoe = 0;
                drawInfo.drawPlayer.waist = 0;
                drawInfo.drawPlayer.shield = 0;
                drawInfo.drawPlayer.neck = 0;
                drawInfo.drawPlayer.face = 0;
                drawInfo.drawPlayer.handon = 0;
                drawInfo.drawPlayer.handoff = 0;
                drawInfo.drawPlayer.beard = 0; // 胡子也要隐藏

                // 步骤D: 移除手持物品的数据引用
                drawInfo.heldItem = null;

                // 既然已经完全隐身，后续的颜色计算就不需要了，直接返回
                return;
            }
            if (graftingMode != 0)
            {
                if (graftingMode == 1)
                {
                    r *= 0.8f;
                    g *= 0.5f;
                    b *= 1.0f;
                    if (!isSpiritForm) a = 0.8f;
                }
                else if (graftingMode == 2)
                {
                    r *= 1.0f;
                    g *= 0.5f;
                    b *= 0.5f;
                }
            }
            if (isCalamityGiant || isFireForm || isMercuryForm || isMoonlightized || isSunMessenger || isApocalypseForm)
            {
                drawInfo.hideEntirePlayer = true;
                return;
            }
            if (currentSequence <= 3 && stealthTimer > 60) { drawInfo.hideEntirePlayer = true; drawInfo.shadow = 0f; a = 0f; return; }
            if (currentSequence <= 6 && dawnArmorActive && !dawnArmorBroken && !Player.shroomiteStealth) { r = 2.0f; g = 2.0f; b = 2.0f; fullBright = true; if (Main.rand.NextBool(20)) Dust.NewDustPerfect(Player.Center, DustID.Enchanted_Gold, Vector2.Zero, 0, default, 0.8f).noGravity = true; }

            if (currentMoonSequence <= 1) { Lighting.AddLight(Player.Center, 0.8f, 0.4f, 0.6f); if (Main.rand.NextBool(10)) Dust.NewDustDirect(Player.position, Player.width, Player.height, DustID.PinkTorch, 0, 0, 100, default, 1.0f).noGravity = true; }

        }
        public override void HideDrawLayers(PlayerDrawSet drawInfo)
        {
            // 只判断你要求的三个状态：蝙蝠化、太阳使者、水银化
            if (isBatSwarm || isSunMessenger || isMercuryForm)
            {
                foreach (var layer in Terraria.ModLoader.PlayerDrawLayerLoader.Layers)
                {
                    layer.Hide();
                }

                Terraria.DataStructures.PlayerDrawLayers.HeldItem.Hide();
            }
        }

        public override void PostUpdateMiscEffects()
        {
            float moonMult = GetSequenceMultiplier(currentMoonSequence);
            float giantMult = GetSequenceMultiplier(currentSequence);
            float hunterMult = GetSequenceMultiplier(currentHunterSequence);


            if (SubworldSystem.IsActive<SpiritWorld>())
            {
                // 设定每帧消耗的灵性 (60帧 = 1秒)
                // 这里设定为每秒消耗 5 点灵性
                float drainAmount = 5f / 60f;

                if (spiritualityCurrent > 0)
                {
                    spiritualityCurrent -= drainAmount;
                }
                else
                {
                    spiritualityCurrent = 0;
                    // 灵性耗尽的惩罚：扣血
                    // 每秒扣 10 点血
                    if (Player.whoAmI == Main.myPlayer && Main.GameUpdateCount % 60 == 0)
                    {
                        // 使用 NetworkText 修复之前的过时警告
                        Player.Hurt(PlayerDeathReason.ByCustomReason(NetworkText.FromLiteral(Player.name + " 在灵界耗尽了灵性...")), 10, 0);
                    }
                }
            }

            // --- 新增：歌唱光环逻辑 ---
            if (isSinging)
            {
                // 倒计时
                singTimer--;
                if (singTimer <= 0) isSinging = false;

                // 头顶出现音符特效
                if (Main.rand.NextBool(15))
                {
                    CombatText.NewText(Player.getRect(), Color.Gold, "♫", false, true);
                }

                // 给周围同伴加 Buff (范围 50格)
                float buffRange = 800f;

                // 遍历玩家
                foreach (Player p in Main.player)
                {
                    if (p.active && !p.dead && p.Distance(Player.Center) < buffRange)
                    {
                        ApplyBardBuffs(p);
                    }
                }

                // 遍历友方 NPC
                foreach (NPC npc in Main.npc)
                {
                    if (npc.active && (npc.friendly || npc.townNPC) && npc.Distance(Player.Center) < buffRange)
                    {
                        Lighting.AddLight(npc.Center, 0.5f, 0.4f, 0.1f);
                    }
                }
            }
            if (currentSunSequence <= 7)
            {
                float radius = 1000f;
                if (currentSunSequence <= 5) radius = 1500f;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (p.active && !p.dead && p.Distance(Player.Center) < radius)
                    {
                        p.buffImmune[BuffID.Horrified] = true;
                        p.statDefense += 10;
                        if (Main.GameUpdateCount % 60 == 0) // 每秒净化一次
                        {
                            if (p.HasBuff(BuffID.Poisoned)) p.DelBuff(p.FindBuffIndex(BuffID.Poisoned));
                            if (p.HasBuff(BuffID.Bleeding)) p.DelBuff(p.FindBuffIndex(BuffID.Bleeding));
                            if (p.HasBuff(BuffID.Confused)) p.DelBuff(p.FindBuffIndex(BuffID.Confused));
                        }
                    }
                }

                // 2. 视觉光环
                if (currentSunSequence <= 5)
                {
                    if (Main.GameUpdateCount % 30 == 0)
                    {
                        for (int i = 0; i < 360; i += 15) // 稍微稀疏一点，性能更好
                        {
                            Vector2 vel = MathHelper.ToRadians(i).ToRotationVector2() * 6f;
                            Dust d = Dust.NewDustPerfect(Player.Center, DustID.GoldFlame, vel + Player.velocity, 0, default, 1.5f);
                            d.noGravity = true;
                        }
                    }
                    if (Main.rand.NextBool(2))
                    {
                        Vector2 offset = Main.rand.NextVector2Circular(30, 30);
                        Dust d = Dust.NewDustPerfect(Player.Center + offset, DustID.Enchanted_Gold, Player.velocity, 100, default, 1.0f);
                        d.noGravity = true;
                        d.fadeIn = 1.2f;
                    }
                    if (Main.GameUpdateCount % 20 == 0)
                    {
                        foreach (NPC npc in Main.ActiveNPCs)
                        {
                            if (!npc.friendly && npc.Distance(Player.Center) < radius)
                            {
                                bool isUnholy = NPCID.Sets.Zombies[npc.type] || NPCID.Sets.Skeletons[npc.type];
                                if (isUnholy)
                                {
                                    int burnDmg = (int)(50 * GetSequenceMultiplier(currentSunSequence));
                                    Player.ApplyDamageToNPC(npc, burnDmg, 0, 0, false);
                                }
                            }
                        }
                    }
                }
                else if (Main.GameUpdateCount % 20 == 0)
                {
                    // 画一个淡淡的金圈
                    for (int i = 0; i < 360; i += 10)
                    {
                        Vector2 pos = Player.Center + Microsoft.Xna.Framework.Vector2.One.RotatedBy(MathHelper.ToRadians(i)) * radius;
                        Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame, Vector2.Zero, 200, default, 0.5f);
                        d.noGravity = true;
                    }
                }
            }
            if (currentSunSequence <= 3)
            {
                float auraRange = 4000f;
                float visualRange = 250f;

                for (int k = 0; k < 2; k++)
                {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(visualRange, visualRange);
                    Dust d = Dust.NewDustPerfect(Player.Center + offset, DustID.GoldFlame, Vector2.Zero, 0, default, 1.5f);
                    d.noGravity = true;
                    d.velocity = -offset.SafeNormalize(Vector2.Zero) * 4f; // 快速吸入中心
                }

                // 2. 对队友/友方
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (p.active && !p.dead && p.Distance(Player.Center) < auraRange)
                    {
                        p.AddBuff(BuffID.Endurance, 2);
                        p.AddBuff(BuffID.Wrath, 2);
                        p.AddBuff(BuffID.Lifeforce, 2);
                        p.lifeRegen += 20; // 回血加强

                        // 队友身上的特效
                        if (Main.rand.NextBool(10))
                        {
                            Dust.NewDust(p.position, p.width, p.height, DustID.Enchanted_Gold, 0, -2, 0, default, 1f);
                        }
                    }
                }

                // 3. 对敌人 (压制与净化)
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.friendly && !npc.dontTakeDamage && npc.Distance(Player.Center) < auraRange)
                    {
                        // 基础压制
                        npc.AddBuff(BuffID.Slow, 60);
                        npc.AddBuff(BuffID.Midas, 60);
                        npc.AddBuff(BuffID.BrokenArmor, 60);

                        // 视觉：敌人身上燃烧金火
                        if (Main.rand.NextBool(5))
                        {
                            Dust.NewDust(npc.position, npc.width, npc.height, DustID.GoldFlame, 0, 0, 0, default, 2f);
                        }

                        // 对“邪恶”生物 (怨魂恶灵/亡灵) 的绝对净化
                        bool isEvil = NPCID.Sets.Zombies[npc.type] || NPCID.Sets.Skeletons[npc.type] || npc.aiStyle == 22;

                        if (isEvil)
                        {
                            // 伤害频率提高
                            if (Main.GameUpdateCount % 10 == 0) // 每0.16秒烫一次
                            {
                                int purifyDmg = (int)(100 * GetSequenceMultiplier(currentSunSequence));
                                Player.ApplyDamageToNPC(npc, purifyDmg, 0f, 0, false);
                            }

                            // 强力减速
                            if (!npc.boss) npc.velocity *= 0.6f;
                        }
                    }
                }
                if (isSunMessenger)
                {
                    // 1. 消耗灵性 (300点/秒)
                    if (!TryConsumeSpirituality(300f / 60f, true))
                    {
                        isSunMessenger = false;
                        Main.NewText("灵性枯竭，太阳形态解除！", 255, 50, 50);
                        return;
                    }

                    // 2. 物理逻辑
                    Player.gravity = 0f;       // 无重力
                    Player.noFallDmg = true;   // 无摔伤
                    Player.wingTime = 9999;    // 无限飞行

                    Player.controlDown = true;

                    float acc = 1.5f;
                    float maxSpeed = 20f;

                    if (Player.controlUp)
                    {
                        Player.velocity.Y -= acc;
                    }

                    else if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.S) || Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Down))
                    {
                        Player.velocity.Y += acc;
                    }
                    else
                    {
                        Player.velocity.Y *= 0.9f; // 减速悬停
                    }

                    // 水平移动
                    if (Player.controlLeft) Player.velocity.X -= acc;
                    else if (Player.controlRight) Player.velocity.X += acc;
                    else Player.velocity.X *= 0.9f;

                    // 限制最大速度
                    if (Player.velocity.Length() > maxSpeed)
                        Player.velocity = Player.velocity.SafeNormalize(Vector2.Zero) * maxSpeed;

                    // 4. 特效：大号狱火药水
                    float radius = 120f;
                    int dustCount = 4;
                    float rotateSpeed = Main.GameUpdateCount * 0.1f;

                    int mainDust = DustID.SolarFlare; // 默认橙色
                    int subDust = DustID.GoldFlame;

                    if (currentSunSequence <= 1)
                    {
                        mainDust = DustID.WhiteTorch; // 序列1：纯白
                        subDust = DustID.SilverFlame; // 银色
                    }

                    for (int i = 0; i < dustCount; i++)
                    {
                        float angle = rotateSpeed + (MathHelper.TwoPi / dustCount * i);
                        Vector2 offset = angle.ToRotationVector2() * radius;

                        // 使用动态颜色 ID
                        Dust d = Dust.NewDustPerfect(Player.Center + offset, mainDust, Vector2.Zero, 0, default, 3f);
                        d.noGravity = true;
                        d.velocity = offset.SafeNormalize(Vector2.Zero) * 2f;
                    }

                    if (Main.rand.NextBool(3))
                    {
                        Dust d = Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2Circular(radius, radius), subDust, new Vector2(0, -2), 0, default, 2f);
                        d.noGravity = true;
                    }

                    // 5. 伤害光环
                    if (Main.GameUpdateCount % 10 == 0)
                    {
                        float range = 800f;
                        foreach (NPC npc in Main.ActiveNPCs)
                        {
                            if (!npc.friendly && !npc.dontTakeDamage && npc.Distance(Player.Center) < range)
                            {
                                int dmg = (int)(500 * GetSequenceMultiplier(currentSunSequence));
                                bool isEvil = IsUndeadCreature(npc) || npc.boss;
                                if (isEvil) dmg *= 2;
                                Player.ApplyDamageToNPC(npc, dmg, 0f, 0, false);
                                npc.AddBuff(BuffID.Daybreak, 60);
                                npc.AddBuff(BuffID.OnFire3, 60);
                            }
                        }
                    }
                    Lighting.AddLight(Player.Center, 3.0f, 2.5f, 1.5f);
                }
                if (currentSunSequence <= 1)
                {
                    // 1. 领域范围
                    float kingdomRange = 3000f; // 半个屏幕

                    // 2. 视觉：空气中飘浮纯白粒子
                    if (Main.GameUpdateCount % 5 == 0)
                    {
                        Vector2 pos = Player.Center + Main.rand.NextVector2Circular(1200, 800);
                        Dust d = Dust.NewDustPerfect(pos, DustID.WhiteTorch, new Vector2(0, -0.5f), 150, default, 1.0f);
                        d.noGravity = true;
                    }

                    // 3. 压制与毁灭
                    if (Main.GameUpdateCount % 20 == 0)
                    {
                        foreach (NPC npc in Main.ActiveNPCs)
                        {
                            if (!npc.friendly && !npc.dontTakeDamage && npc.Distance(Player.Center) < kingdomRange)
                            {
                                // 秩序压制：所有敌人无法暴击(模拟)，攻击力降低
                                npc.damage = (int)(npc.defDamage * 0.7f);

                                // 净化伤害：持续灼烧
                                int holyDmg = (int)(200 * GetSequenceMultiplier(currentSunSequence));

                                // 对不死生物毁灭性打击
                                if (NPCID.Sets.Zombies[npc.type] || NPCID.Sets.Skeletons[npc.type])
                                {
                                    holyDmg *= 5;
                                    if (npc.life < npc.lifeMax * 0.2f && !npc.boss) npc.SimpleStrikeNPC(9999, 0); // 斩杀低血量亡灵
                                }

                                Player.ApplyDamageToNPC(npc, holyDmg, 0, 0, false);
                            }
                        }
                    }
                }
            }

            // 美神被动
            if (currentMoonSequence <= 1) { foreach (NPC npc in Main.ActiveNPCs) { if (!npc.friendly && !npc.dontTakeDamage && !npc.boss && npc.Distance(Player.Center) < 800f) { npc.AddBuff(BuffID.Confused, 2); npc.AddBuff(BuffID.Lovestruck, 2); npc.damage = (int)(npc.defDamage * 0.5f); } } }

            // 猎人/巨人特效
            if (isCalamityGiant) { if (!TryConsumeSpirituality(100.0f, true)) isCalamityGiant = false; else { Player.statDefense += 300; Player.wingsLogic = 0; Player.wingTime = 9999; Player.gravity = 0f; Player.statLifeMax2 += 3000; Player.invis = true; if (Main.rand.NextBool(2)) Dust.NewDust(Player.position, Player.width, Player.height, DustID.Electric, 0, 0, 0, default, 1.5f); } }
            if (isArmyOfOne) { if (currentHunterSequence <= 4 && TryConsumeSpirituality(5.0f, true)) { int bonusMinions = 5; if (currentHunterSequence <= 1) bonusMinions = 40; else if (currentHunterSequence <= 2) bonusMinions = 20; else if (currentHunterSequence <= 3) bonusMinions = 10; Player.maxMinions += bonusMinions; } else { isArmyOfOne = false; } }

            if (isFireForm)
            {
                if (!TryConsumeSpirituality(50.0f, true)) { isFireForm = false; Main.NewText("灵性枯竭！", 255, 50, 50); return; }
                Player.noKnockback = true; Player.wingsLogic = 0; Player.wingTime = 9999; Player.rocketTime = 9999; Player.noItems = true;
                if (currentHunterSequence <= 1) { Player.moveSpeed += 5.0f; Player.maxRunSpeed += 30f; Player.runAcceleration *= 10f; Player.jumpSpeedBoost += 30f; Player.statDefense += 200; Player.endurance += 0.4f; Vector2 tip = Player.Center + Player.velocity * 2f; for (int i = 0; i < 5; i++) { Vector2 pos = Vector2.Lerp(Player.Center, tip, i / 5f) + Main.rand.NextVector2Circular(10, 10); int d = Dust.NewDust(pos, 0, 0, DustID.Shadowflame, 0, 0, 0, default, 2.5f); Main.dust[d].noGravity = true; Main.dust[d].velocity = -Player.velocity * 0.2f; } Rectangle myRect = Player.getRect(); myRect.Inflate(40, 40); if (Player.velocity.Length() > 5f) { for (int i = 0; i < Main.maxNPCs; i++) { NPC npc = Main.npc[i]; if (npc.active && !npc.friendly && !npc.dontTakeDamage && npc.getRect().Intersects(myRect)) { if (npc.immune[Player.whoAmI] <= 0) { int dashDamage = (int)(Player.GetDamage(DamageClass.Melee).ApplyTo(3000) * hunterMult); Player.ApplyDamageToNPC(npc, dashDamage, 30f, Player.direction, true); npc.AddBuff(ModContent.BuffType<ConquerorWill>(), 600); npc.immune[Player.whoAmI] = 6; SoundEngine.PlaySound(SoundID.Item74, npc.Center); for (int k = 0; k < 20; k++) Dust.NewDust(npc.position, npc.width, npc.height, DustID.Shadowflame, 0, 0, 0, default, 3f); } } } } } else { Player.moveSpeed += 3.0f; Player.maxRunSpeed += 15f; Player.runAcceleration *= 4f; Player.jumpSpeedBoost += 20f; Player.gravity *= 0.5f; Player.statDefense += 100; for (int i = 0; i < 3; i++) { int d = Dust.NewDust(Player.position, Player.width, Player.height, DustID.SolarFlare, 0, 0, 0, default, 2f); Main.dust[d].noGravity = true; Main.dust[d].velocity = Player.velocity * 0.5f; } }
            }

            // 月亮特效
            if (isVampireWings) { if (!TryConsumeSpirituality(0.5f, true)) { isVampireWings = false; Main.NewText("灵性耗尽，黑暗之翼消散！", 200, 50, 50); return; } Player.wingTime = 1000; Player.wingTimeMax = 1000; Player.wingsLogic = 12; Player.noFallDmg = true; if (Main.rand.NextBool(4)) Dust.NewDustDirect(Player.position, Player.width, Player.height, DustID.Shadowflame, 0, 0, 100).noGravity = true; }
            if (isMoonlightized) { if (!TryConsumeSpirituality(4f, true)) { isMoonlightized = false; Main.NewText("灵性耗尽！", 200, 50, 50); return; } Player.immune = true; Player.immuneTime = 2; Player.noItems = true; Player.noKnockback = true; Player.invis = true; Player.maxRunSpeed += 10f; Player.moveSpeed += 2.0f; if (Main.rand.NextBool(2)) { int d = Dust.NewDust(Player.position, Player.width, Player.height, DustID.VampireHeal, Player.velocity.X, Player.velocity.Y, 100, default, 1.5f); Main.dust[d].noGravity = true; } }
            if (isFullMoonActive) { if (!TryConsumeSpirituality(0.1f, true)) { isFullMoonActive = false; return; } Lighting.AddLight(Player.Center, 0.6f, 0.7f, 0.9f); if (Player.ownedProjectileCounts[ModContent.ProjectileType<FullMoonCircle>()] < 1) { Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, ModContent.ProjectileType<FullMoonCircle>(), 0, 0, Player.whoAmI); } if (Main.rand.NextBool(10)) { Vector2 pos = Player.Center + Main.rand.NextVector2Circular(100, 100); Dust d = Dust.NewDustPerfect(pos, DustID.BlueCrystalShard, Vector2.Zero, 150, default, 1.0f); d.noGravity = true; } }
            if (isTamingActive)
            {
                if (!TryConsumeSpirituality(2.0f / 60f, true))
                {
                    isTamingActive = false;
                    Main.NewText("灵性不足，驯兽模式被迫中断。", 255, 50, 50);
                }
                if (Main.rand.NextBool(10))
                {
                    Dust d = Dust.NewDustDirect(Player.position, Player.width, Player.height, DustID.Terra, 0, 0, 0, default, 1.0f);
                    d.noGravity = true;
                    d.velocity *= 0.5f;
                }
            }
            // 蝙蝠化身
            if (isBatSwarm)
            {
                if (!TryConsumeSpirituality(0.5f, true)) { isBatSwarm = false; Main.NewText("灵性耗尽，蝙蝠化身解除！", 200, 50, 50); return; }
                Player.mount.Dismount(Player); Player.noFallDmg = true; Player.noKnockback = true; Player.wingTime = 1000; Player.wingsLogic = 12; Player.noItems = true; Player.invis = true;
                int maxBats = 60; int targetBatCount = (int)(maxBats * ((float)Player.statLife / Player.statLifeMax2));
                if (targetBatCount < 1) { isBatSwarm = false; Main.NewText("重伤解除！", 255, 50, 50); return; }
                int currentBats = Player.ownedProjectileCounts[ModContent.ProjectileType<BatSwarmProjectile>()];
                if (currentBats < targetBatCount) { Vector2 spawnPos = Player.Center + Main.rand.NextVector2Circular(20, 20); Projectile.NewProjectile(Player.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<BatSwarmProjectile>(), (int)(300 * moonMult), 2f, Player.whoAmI); }
                else if (currentBats > targetBatCount) { int toKill = currentBats - targetBatCount; for (int i = 0; i < Main.maxProjectiles; i++) { Projectile p = Main.projectile[i]; if (p.active && p.owner == Player.whoAmI && p.type == ModContent.ProjectileType<BatSwarmProjectile>()) { p.Kill(); toKill--; if (toKill <= 0) break; } } }
            }

            // 创生领域
            if (isCreationDomain)
            {
                if (!TryConsumeSpirituality(1.0f, true)) { isCreationDomain = false; Main.NewText("灵性枯竭，领域消散", 100, 255, 100); return; }
                if (Player.ownedProjectileCounts[ModContent.ProjectileType<CreationDomainProjectile>()] < 1) { Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, ModContent.ProjectileType<CreationDomainProjectile>(), 0, 0, Player.whoAmI); }
                Lighting.AddLight(Player.Center, 0.2f, 0.8f, 0.3f);
                if (Main.GameUpdateCount % 60 == 0)
                {
                    foreach (NPC npc in Main.ActiveNPCs) { if (!npc.friendly && !npc.dontTakeDamage && npc.Distance(Player.Center) < 1000f) { int dmg = (int)(npc.lifeMax * 0.05f); if (currentMoonSequence <= 1) dmg = (int)(npc.lifeMax * 0.1f); if (dmg > 20000) dmg = 20000; npc.SimpleStrikeNPC(dmg, 0, true, 0, DamageClass.Magic, true); } }
                    for (int i = 0; i < Main.maxPlayers; i++) { Player p = Main.player[i]; if (p.active && !p.dead && p.Distance(Player.Center) < 800f) { p.HealEffect(50); p.statLife += 50; if (p.statLife > p.statLifeMax2) p.statLife = p.statLifeMax2; } }
                }
            }
            if (currentFoolSequence <= 2 && fateDisturbanceActive)
            {
                if (Main.GameUpdateCount % 20 == 0)
                {
                    for (int i = 0; i < 30; i++)
                    {
                        Vector2 pos = Player.Center + Main.rand.NextVector2Circular(600, 600);
                        Dust d = Dust.NewDustPerfect(pos, DustID.Enchanted_Gold, Vector2.Zero, 150, default, 0.5f);
                        d.noGravity = true;
                    }
                }
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.friendly && !npc.dontTakeDamage && npc.Distance(Player.Center) < 600f)
                    {
                        npc.damage = (int)(npc.defDamage * 0.5f);
                        npc.AddBuff(BuffID.Confused, 2);
                        npc.AddBuff(BuffID.Midas, 2); // 掉落增加 (好运)
                    }
                }
            }
            // =================================================
            // 序列1：时之虫领域 (Time Clock Domain)
            // =================================================
            if (isTimeClockActive)
            {
                if (!TryConsumeSpirituality(20.0f, true))
                {
                    isTimeClockActive = false;
                    Main.NewText("灵性枯竭，时钟虚影消散。", 150, 150, 150);
                    return;
                }

                if (Player.ownedProjectileCounts[ModContent.ProjectileType<TimeClockVisual>()] < 1)
                {
                    Projectile.NewProjectile(
                        Player.GetSource_FromThis(),
                        Player.Center,
                        Vector2.Zero,
                        ModContent.ProjectileType<TimeClockVisual>(),
                        0,
                        0,
                        Player.whoAmI
                    );
                }
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.friendly && !npc.dontTakeDamage && npc.Distance(Player.Center) < 1000f) // 范围加大到 1000
                    {
                        npc.velocity *= 0.1f; // 速度变为原来的 10%，几乎走不动

                        // B. 衰老伤害 (每秒 60 次判定)
                        if (Main.GameUpdateCount % 20 == 0) // 频率提高：每秒跳 3 次伤害 (原为每秒1次)
                        {
                            int decay = 0;

                            if (npc.boss)
                            {
                                decay = (int)(npc.life * 0.005f) + 2000;
                                if (decay > 10000) decay = 10000;
                            }
                            else
                            {
                                decay = (int)(npc.life * 0.10f) + 5000;
                            }
                            Player.ApplyDamageToNPC(npc, decay, 0, 0, false);
                        }
                        npc.AddBuff(BuffID.Slow, 10);
                        npc.AddBuff(BuffID.WitheredArmor, 10); // 护甲衰老
                        npc.AddBuff(BuffID.WitheredWeapon, 10); // 攻击衰老
                        npc.AddBuff(BuffID.ShadowFlame, 10);    // 视觉特效
                    }
                }
            }

            if (isFireEnchanted) { if (currentHunterSequence <= 6 && TryConsumeSpirituality(0.16f, true)) { } else isFireEnchanted = false; }
            if (isFlameCloakActive)
            {
                if (currentHunterSequence <= 7 && TryConsumeSpirituality(1.0f, true))
                {
                    Player.buffImmune[BuffID.Chilled] = true;
                    Player.buffImmune[BuffID.Frozen] = true; // 顺便免疫冰冻
                    Player.statDefense += 8;
                    if (Main.rand.NextBool(3))
                    {
                        Dust d = Dust.NewDustDirect(Player.position, Player.width, Player.height, DustID.Torch, 0, 0, 100, default, 1.5f);
                        d.noGravity = true;
                        d.velocity *= 0.5f;
                    }
                    if (Main.GameUpdateCount % 10 == 0)
                    {
                        float range = 150f; // 范围
                        foreach (NPC npc in Main.ActiveNPCs)
                        {
                            if (!npc.friendly && !npc.dontTakeDamage && npc.Distance(Player.Center) < range)
                            {
                                npc.SimpleStrikeNPC(20, 0, true, 0, DamageClass.Generic); // 20点基础伤害
                                npc.AddBuff(BuffID.OnFire3, 120); // 狱火 Debuff
                            }
                        }
                    }
                }
                else isFlameCloakActive = false;
            }
            if (fireTeleportCooldown > 0) fireTeleportCooldown--;
            if (isMercuryForm) { if (!TryConsumeSpirituality(20.0f, true)) isMercuryForm = false; else { Player.moveSpeed += 2.0f; Player.invis = true; Rectangle myRect = Player.getRect(); myRect.Inflate(10, 10); foreach (NPC npc in Main.npc) { if (npc.active && !npc.friendly && !npc.dontTakeDamage && npc.getRect().Intersects(myRect)) { if (npc.immune[Player.whoAmI] == 0) { int damage = (int)((Player.GetDamage(DamageClass.Melee).ApplyTo(50) * 5f) * giantMult); Player.ApplyDamageToNPC(npc, damage, 10f, Player.direction, false); npc.immune[Player.whoAmI] = 10; npc.AddBuff(BuffID.Slow, 300); npc.AddBuff(BuffID.Frostburn, 300); } } } } }
            if (currentSequence <= 3 && !isMercuryForm) { if (Player.velocity.Length() < 0.1f) { stealthTimer++; if (stealthTimer > 60) { Player.invis = true; Player.aggro -= 1000; } } else { stealthTimer = 0; } }
            if (isGuardianStance) { Player.velocity.X = 0; Player.statDefense += 80; Player.noKnockback = true; }
            if (glacierCooldown > 0) glacierCooldown--;


            // =================================================================
            // 平衡性系统：灵性即理智 (Spirituality as Sanity)
            // =================================================================

            var config = ModContent.GetInstance<Configs.LotMConfig>();
            if (config == null) return;

            if (config.EnableSanitySystem)
            {
                float spiritRatio = (float)spiritualityCurrent / spiritualityMax;

                // ---------------------------------------------------------
                // 1. 【危险阶段】灵性 < 30% -> 警告 + 惩罚
                // ---------------------------------------------------------
                if (spiritRatio < 0.3f)
                {
                    Player.AddBuff(ModContent.BuffType<Content.Buffs.Curse.SanityDangerBuff>(), 2);

                    // 视觉特效
                    if (Main.netMode != NetmodeID.Server && Player.whoAmI == Main.myPlayer)
                    {
                        Player.AddBuff(BuffID.Blackout, 20);
                        Player.AddBuff(BuffID.Obstructed, 20);
                        if (!SkyManager.Instance["MoonLord"].IsActive())
                            SkyManager.Instance.Activate("MoonLord");
                    }

                    // 文字提示
                    if (Main.GameUpdateCount % 300 == 0)
                        CombatText.NewText(Player.getRect(), new Color(120, 0, 0), "灵性枯竭...疯狂临近...", true);

                    // 属性惩罚
                    float penaltyFactor = 0.5f + (spiritRatio / 0.6f);
                    if (penaltyFactor > 1f) penaltyFactor = 1f;
                    Player.statDefense *= penaltyFactor;
                    Player.GetDamage(DamageClass.Generic) *= penaltyFactor;
                }
                else
                {
                    // 恢复正常时关闭特效
                    if (Main.netMode != NetmodeID.Server && Player.whoAmI == Main.myPlayer)
                    {
                        if (SkyManager.Instance["MoonLord"].IsActive())
                            SkyManager.Instance.Deactivate("MoonLord");
                    }
                }

                // ---------------------------------------------------------
                // 2. 【死亡阶段】灵性归零 -> 强制暴毙
                // ---------------------------------------------------------
                // 只要灵性小于等于 1 (防止浮点数计算误差导致正好是0.0001没死)，直接判定死亡
                if (spiritualityCurrent <= 1.0f)
                {
                    // 1. 强制关闭所有变身/高耗能状态
                    // (防止复活瞬间因为状态还在，每帧扣灵性，导致刚复活又瞬间暴毙的死循环)
                    isFireForm = false; isMercuryForm = false; isCalamityGiant = false;
                    isSunMessenger = false; isSpiritForm = false; isVampireWings = false;
                    isGuardianStance = false;
                    isDeceitDomainActive = false; isTimeClockActive = false;
                    isFullMoonActive = false; isCreationDomain = false;

                    // 2. 播放恐怖音效
                    SoundEngine.PlaySound(SoundID.Roar, Player.position);
                    SoundEngine.PlaySound(SoundID.NPCDeath10, Player.position); // 添加一个血液飞溅/惨叫的声音

                    // 3. 红色文字警告
                    Main.NewText("灵性彻底枯竭！你的精神崩溃了！", 255, 0, 0);

                    // 4. 【核心】强制处决
                    // 这里的 999999 伤害 + CustomReason 确保无视防御和闪避
                    Player.KillMe(
                        PlayerDeathReason.ByCustomReason(NetworkText.FromLiteral(Player.name + " 灵性耗尽，当场失控暴毙！")),
                        999999.0,
                        0
                    );
                }
            }
            else
            {
                // 如果在配置里关闭了SanitySystem，确保特效被移除
                if (Main.netMode != NetmodeID.Server && Player.whoAmI == Main.myPlayer)
                {
                    if (SkyManager.Instance["MoonLord"].IsActive())
                        SkyManager.Instance.Deactivate("MoonLord");
                }
            }

            // --- 方案3: 神性副作用 (Divine Curse) ---
            if (config.EnableDivineCurse)
            {
                // 1. 太阳途径：[阳极必阴]
                if (currentSunSequence <= 9 && !Main.dayTime)
                {
                    // 【UI】添加 Buff 图标
                    Player.AddBuff(ModContent.BuffType<Content.Buffs.Curse.SunCurseBuff>(), 2);

                    Player.statDefense *= 0f;
                    Player.GetDamage(DamageClass.Generic) *= 0.4f;
                    Player.endurance -= 0.5f;
                    Player.moveSpeed *= 0.5f;
                    Player.maxRunSpeed *= 0.5f;

                    if (Main.GameUpdateCount % 3600 == 0)
                        CombatText.NewText(Player.getRect(), Color.Gold, "太阳沉没...凡人躯壳...", true);
                }

                // 2. 月亮途径：[吸血鬼体质]
                if (currentMoonSequence <= 7 && Main.dayTime && Player.ZoneOverworldHeight &&
                    !Player.behindBackWall)
                {
                    // 【UI】添加 Buff 图标
                    Player.AddBuff(ModContent.BuffType<Content.Buffs.Curse.MoonCurseBuff>(), 2);

                    Player.lifeRegen = -60;
                    Player.onFire2 = true;
                    Player.GetDamage(DamageClass.Generic) *= 0.5f;

                    if (Main.GameUpdateCount % 60 == 0)
                        Player.statLife -= 10;
                }

                // 3. 巨人/战士途径：[神性僵化]
                // 确保这里用的是 currentGiantSequence
                if (currentGiantSequence <= 9)
                {
                    // 【UI】添加 Buff 图标
                    Player.AddBuff(ModContent.BuffType<Content.Buffs.Curse.GiantCurseBuff>(), 2);

                    // 飞行限制
                    if (currentGiantSequence > 4)
                        Player.wingTimeMax = 0; // 序列9-5：完全无法飞行
                    else
                        Player.wingTimeMax = (int)(Player.wingTimeMax * 0.3f); // 半神：削减70%

                    // 【核心修复】tModLoader 1.4.4 禁用二段跳的新写法
                    Player.GetJumpState(ExtraJump.CloudInABottle).Disable();     // 禁用云瓶
                    Player.GetJumpState(ExtraJump.SandstormInABottle).Disable(); // 禁用沙暴瓶
                    Player.GetJumpState(ExtraJump.BlizzardInABottle).Disable();  // 禁用暴雪瓶
                    Player.GetJumpState(ExtraJump.FartInAJar).Disable();         // 禁用屁瓶
                    Player.GetJumpState(ExtraJump.TsunamiInABottle).Disable();   // 禁用海啸瓶

                    // 移动限制
                    if (Player.maxRunSpeed > 4f) Player.maxRunSpeed = 4f;
                    Player.accRunSpeed *= 0.5f;
                }

                // 4. 猎人/红祭司途径：[战争渴望]
                if (currentHunterSequence <= 7)
                {
                    // 【UI】添加 Buff 图标 (常驻显示)
                    Player.AddBuff(ModContent.BuffType<Content.Buffs.Curse.HunterCurseBuff>(), 2);

                    if (Player.wet || (Main.raining && Player.ZoneOverworldHeight))
                    {
                        Player.statDefense *= 0.5f;
                        Player.moveSpeed *= 0.5f;
                        Player.AddBuff(BuffID.Chilled, 2);
                    }

                    Player.lifeRegen = 0;
                    Player.lifeRegenTime = 0;
                    Player.statDefense *= 0.7f; // ✅ 必须使用 *= 运算符
                }

                // 5. 愚者/错误途径：[体质孱弱]
                if (currentFoolSequence <= 9 || currentMarauderSequence <= 9)
                {
                    // 【UI】添加 Buff 图标
                    Player.AddBuff(ModContent.BuffType<Content.Buffs.Curse.FoolCurseBuff>(), 2);

                    Player.statDefense *= 0f;
                    Player.statLifeMax2 = (int)(Player.statLifeMax2 * 0.6f);
                    Player.endurance -= 1.0f;
                }
                // 【新增】6. 刺客/魔女途径：[原初的诅咒]
                if (currentDemonessSequence <= 9)
                {
                    Player.AddBuff(ModContent.BuffType<Content.Buffs.Curse.DemonessCurseBuff>(), 2);
                    Player.statDefense *= 0.8f; // 最终防御降低 20%
                    Player.endurance -= 0.1f;   // 受到伤害增加 10% (负免伤)
                    Player.lifeRegen -= 2;
                    if (Player.HasBuff(BuffID.OnFire) || Player.HasBuff(BuffID.Burning))
                    {
                        Player.GetDamage(DamageClass.Generic) *= 0.8f; // 着火时攻击力下降
                    }
                }

                // 【新增】7. 命运途径：[星辰之灾]
                // 夜晚地表大幅提升星星坠落概率,直接砸玩家
                if (currentWheelSequence <= 9 && !Main.dayTime
                    && Player.ZoneOverworldHeight && !Player.behindBackWall)
                {
                    Player.AddBuff(ModContent.BuffType<Content.Buffs.Curse.WheelCurseBuff>(), 2);

                    // 序列越低(越接近1) → 越强 → 星砸频率越高
                    // 序列9: 每120帧(2s)一次; 序列1: 每20帧(1/3秒)一次
                    int interval = System.Math.Max(20, 120 - (9 - currentWheelSequence) * 12);
                    if (Main.GameUpdateCount % (uint)interval == 0 && Main.myPlayer == Player.whoAmI)
                    {
                        // 星星从玩家上方800-1200像素的随机位置坠落,朝玩家方向
                        Vector2 spawnPos = Player.Center
                            + new Vector2(Main.rand.NextFloat(-300f, 300f), Main.rand.NextFloat(-1200f, -800f));
                        Vector2 toPlayer = (Player.Center - spawnPos).SafeNormalize(Vector2.UnitY);
                        Vector2 velocity = toPlayer * Main.rand.NextFloat(15f, 22f);
                        // 序列越高伤害越大 (序列1-5的伤害更大,因为玩家更强)
                        int starDmg = 80 + (9 - currentWheelSequence) * 25;
                        Projectile.NewProjectile(Player.GetSource_FromThis(),
                            spawnPos, velocity,
                            ModContent.ProjectileType<CurseStar>(),
                            starDmg, 5f, Main.myPlayer);
                    }

                    // 偶尔提示
                    if (Main.GameUpdateCount % 1800 == 0)
                        CombatText.NewText(Player.getRect(), Color.Gold, "命运长河翻涌,星辰陨落...", true);
                }
            }
            // ==========================================
            // 痛苦魔女：仪式与能力逻辑
            // ==========================================

            // 1. 仪式逻辑：只有序列6需要做
            if (baseDemonessSequence == 6)
            {
                // 检查脚下是不是“活火块” (Living Fire, ID 350)
                // 或者是被烧着了 (BuffID.Burning) 也算
                bool onFireBlock = false;
                Point tilePos = Player.Center.ToTileCoordinates();

                // 检查脚下和身体所在的方块
                if (Main.tile[tilePos.X, tilePos.Y].TileType == TileID.LivingFire ||
                    Main.tile[tilePos.X, tilePos.Y + 1].TileType == TileID.LivingFire)
                {
                    onFireBlock = true;
                }

                if (onFireBlock)
                {
                    afflictionRitualTimer++;
                    // 每分钟提示一次
                    if (afflictionRitualTimer % 3600 == 0)
                    {
                        int mins = afflictionRitualTimer / 3600;
                        CombatText.NewText(Player.getRect(), new Microsoft.Xna.Framework.Color(255, 100, 50), $"痛苦煎熬: {mins}/1 分钟", true);
                    }

                    // 达到15分钟 (54000帧) 提示完成
                    if (afflictionRitualTimer == 54000)
                    {
                        Main.NewText("烈火已将你的痛苦铭刻进灵体，魔药已准备就绪！", 255, 0, 255);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item37, Player.position);
                    }
                }
                else
                {
                    // 离开火焰，进度缓慢衰减（或清零，看你难度要求，这里设为不衰减但暂停）
                    // 如果想增加难度，可以写: afflictionRitualTimer = 0;
                }
            }

            // 2. 序列5 能力：瘟疫与魅惑光环
            if (isAfflictionDemoness)
            {
                // 每秒触发一次 (60帧)
                if (Main.GameUpdateCount % 60 == 0)
                {
                    // --- 改动 1: 动态范围判定 ---
                    // 序列5范围约 1600 (100格)，序列4及以上范围扩大到 3200 (200格)
                    float range = (currentDemonessSequence <= 4) ? 3200f : 1600f;

                    foreach (NPC target in Main.ActiveNPCs)
                    {
                        if (target.friendly || target.dontTakeDamage) continue;

                        if (target.Distance(Player.Center) < range)
                        {
                            // =================================================
                            // A. 基础能力：痛苦瘟疫 (序列5及以上生效)
                            // =================================================
                            int rand = Main.rand.Next(4);
                            if (rand == 0) target.AddBuff(BuffID.Poisoned, 300);
                            if (rand == 1) target.AddBuff(BuffID.Venom, 300);
                            if (rand == 2) target.AddBuff(BuffID.Weak, 300);
                            if (rand == 3) target.AddBuff(ModContent.BuffType<Buffs.Curse.AfflictionCurseBuff>(), 300);

                            // =================================================
                            // B. 进阶能力：绝望灾祸 (序列4新增)
                            // =================================================
                            if (currentDemonessSequence <= 4)
                            {
                                // 必定施加黑焰 (对应黑焰能力)
                                target.AddBuff(BuffID.ShadowFlame, 300);

                                // 必定施加冻伤 (对应冰霜能力)
                                target.AddBuff(BuffID.Frostburn2, 300);

                                // 施加灵液(破甲)，模拟敌人因“绝望”而放弃抵抗
                                target.AddBuff(BuffID.Ichor, 300);
                            }

                            // =================================================
                            // C. 魅惑/驯化 (通用)
                            // =================================================
                            bool facingPlayer = (target.Center.X < Player.Center.X && target.direction == 1) ||
                                                (target.Center.X > Player.Center.X && target.direction == -1);

                            // 序列4的魅惑概率稍微提高一点 (10% -> 15%)
                            int charmChance = (currentDemonessSequence <= 4) ? 7 : 10;

                            if (facingPlayer && Main.rand.NextBool(charmChance) && !target.boss)
                            {
                                target.AddBuff(BuffID.Lovestruck, 120);
                                target.AddBuff(BuffID.Confused, 120);
                            }
                        }
                    }
                }
            }
            // 检查是否有分身存在
            if (Player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Demoness.MirrorCloneProjectile>()] > 0)
            {
                // 每秒消耗 50 点 (60帧)
                if (Main.GameUpdateCount % 60 == 0)
                {
                    if (spiritualityCurrent >= 50) 
                    {
                        spiritualityCurrent -= 50; 
                    }
                    else
                    {
                        // 灵性耗尽，强制关闭分身
                        ToggleMirrorClone();
                        Main.NewText("灵性枯竭，镜像消散了。", 255, 50, 50);
                    }
                }
            }
        }

        // 6. 攻击
        private void ApplyDemonessHitEffects(NPC target, int damageDone)
        {
            // 1. 序列7：女巫 (冰霜)
            if (witchIceEffect)
            {
                target.AddBuff(BuffID.Frostburn, 180);
                if (Main.rand.NextBool(3)) target.AddBuff(BuffID.ShadowFlame, 180);

                // 诅咒爆发逻辑 (保持不变)
                bool conditionMet = target.HasBuff(BuffID.Frostburn) && (target.HasBuff(BuffID.Confused) || target.HasBuff(BuffID.ShadowFlame));
                if (conditionMet && witchCurseCooldown <= 0)
                {
                    if (!preventRecursiveOp)
                    {
                        preventRecursiveOp = true;
                        try
                        {
                            int burstDmg = (int)(damageDone * 0.5f) + 20;
                            CombatText.NewText(target.getRect(), new Color(148, 0, 211), "诅咒爆发!", true);
                            Player.ApplyDamageToNPC(target, burstDmg, 0f, 0, false);
                            witchCurseCooldown = 120;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103, target.Center);
                            for (int i = 0; i < 15; i++)
                                Dust.NewDust(target.position, target.width, target.height, DustID.IceTorch, 0, 0, 0, default, 1.2f);
                        }
                        finally { preventRecursiveOp = false; }
                    }
                }
            }

            // 2. 序列6：欢愉魔女 (剧毒、虚弱、蛛丝)
            if (pleasureDemonessEffect)
            {
                // 【新增】在这里给所有攻击都加上剧毒和虚弱
                target.AddBuff(BuffID.Poisoned, 180); // 3秒
                target.AddBuff(BuffID.Weak, 180);     // 3秒

                // 机制一：实体蛛丝
                if (Main.rand.NextBool(5) && Main.myPlayer == Player.whoAmI) // 几率提高 1/7 -> 1/5
                {
                    Vector2 spawnPos = Player.Center + Main.rand.NextVector2Circular(20, 20);
                    Vector2 velocity = (target.Center - spawnPos).SafeNormalize(Vector2.Zero) * 12f; // 速度 10 -> 12

                    // 使用简写，因为同名空间下可以直接访问，或者你需要加 using
                    int projType = ModContent.ProjectileType<DemonessSpiderSilk>();

                    Projectile.NewProjectile(
                    Player.GetSource_OnHit(target),
                    spawnPos,
                    velocity,
                    projType, // <--- 检查这里！必须是 projType
                    (int)(damageDone * 0.8f),
                    2f,
                    Player.whoAmI
                );
                }

                // 机制二：欢愉崩溃 (保持不变)
                if (!target.HasBuff(BuffID.Lovestruck))
                {
                    if (Main.rand.NextBool(4))
                    {
                        target.AddBuff(BuffID.Lovestruck, 300);
                        CombatText.NewText(target.getRect(), Color.Pink, "❤", true);
                    }
                }
                else
                {
                    if (Main.rand.NextBool(5))
                    {
                        int heal = damageDone / 10;
                        if (heal > 0)
                        {
                            Player.statLife += heal;
                            Player.HealEffect(heal);
                        }
                    }
                }
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 戏法大师 电击充能: 近战命中附加感电伤害
            if (currentDoorSequence <= 8 && electricChargeTimer > 0 && hit.DamageType.CountsAsClass(DamageClass.Melee))
            {
                target.AddBuff(BuffID.Electrified, 180);
                target.netUpdate = true;
                if (Main.myPlayer == Player.whoAmI)
                {
                    for (int k = 0; k < 8; k++)
                    {
                        Dust d = Dust.NewDustPerfect(target.Center, DustID.Electric,
                            Main.rand.NextVector2Circular(4, 4), 0, default, 1.2f);
                        d.noGravity = true;
                    }
                }
            }

            if (currentWheelSequence <= 9)
            {
                // === 性能保护 ===
                // 1. 节流: 全局限频, 每15帧最多触发一次该段(避免多发武器/AOE炸帧)
                // 2. 不在递归中触发: 防止"炸膛"二次调用OnHitNPC造成调用爆栈
                // 3. 持续型buff改用RefreshOnly(已有就不刷新), 减小buff写入压力
                if (!preventRecursiveOp && wheelOnHitCooldown <= 0)
                {
                    wheelOnHitCooldown = 15; // 1/4 秒间隔

                    // 1. 基础诅咒 (仅在敌人未持有该 buff 时施加, 不每次刷新)
                    int baseDuration = 300 + (int)(Player.luck > 0 ? Player.luck * 120 : 0);
                    if (!target.HasBuff(BuffID.BetsysCurse))
                        target.AddBuff(BuffID.BetsysCurse, baseDuration);
                    if (!target.HasBuff(BuffID.Midas))
                        target.AddBuff(BuffID.Midas, baseDuration);

                    // 2. 只有在玩家运气 > 0 时触发特殊厄运
                    if (Player.luck > 0)
                    {
                        // [事件A] 致命弱点 - 概率事件，伤害不会触发递归
                        float weakChance = 0.10f + (Player.luck * 0.15f);
                        if (Main.rand.NextFloat() < weakChance)
                        {
                            float damageMult = 0.5f + (Player.luck * 0.25f);
                            int extraDmg = (int)(damageDone * damageMult);
                            // 用 SimpleStrikeNPC 而非 ApplyDamageToNPC,后者会再次走OnHitNPC造成递归
                            target.SimpleStrikeNPC(extraDmg, 0, false, 0, DamageClass.Default, true);
                            CombatText.NewText(target.getRect(), Color.Gray, "弱点!", true);
                        }

                        // [事件B] 炸膛 - 改用 SimpleStrikeNPC(同样绕开OnHitNPC递归)
                        float backfireChance = 0.05f + (Player.luck * 0.10f);
                        if (Main.rand.NextFloat() < backfireChance)
                        {
                            float backfireMult = 2f + Player.luck;
                            int backfireDamage = (int)(target.damage * backfireMult);
                            if (backfireDamage < 50) backfireDamage = 50;
                            target.SimpleStrikeNPC(backfireDamage, 0, false, 0, DamageClass.Default, true);

                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, target.position);
                            // 烟雾粒子数量减半,大型战斗时少生成
                            for (int i = 0; i < 5; i++) Dust.NewDust(target.position, target.width, target.height, DustID.Smoke, 0, 0, 100, default, 1.5f);
                            CombatText.NewText(target.getRect(), Color.OrangeRed, "炸膛!", true);
                        }

                        // [事件C] 手滑 - 仅在敌人未持有时施加
                        float disarmChance = 0.10f + (Player.luck * 0.10f);
                        if (Main.rand.NextFloat() < disarmChance)
                        {
                            int debuffTime = 300 + (int)(Player.luck * 180);
                            if (!target.HasBuff(BuffID.Weak))
                                target.AddBuff(BuffID.Weak, debuffTime);
                            if (!target.HasBuff(BuffID.BrokenArmor))
                                target.AddBuff(BuffID.BrokenArmor, debuffTime);
                            CombatText.NewText(target.getRect(), Color.LightYellow, "手滑!", true);
                        }
                    }
                }
            }

            // === 序列2 先知 - 预言术 ===
            if (currentWheelSequence <= 2 && !preventRecursiveOp)
            {
                // [1] 命中已被"预言"的敌人 -> 直接秒杀(非Boss)或扣20%血(Boss)
                if (prophecyMarked && prophecyDuration > 0)
                {
                    prophecyMarked = false;
                    prophecyDuration = 0;

                    int prophDmg;
                    if (target.boss)
                        prophDmg = (int)(target.life * 0.20f);
                    else
                        prophDmg = target.life;

                    if (prophDmg > 0)
                    {
                        target.SimpleStrikeNPC(prophDmg, 0, false, 0, DamageClass.Default, true);
                        target.netUpdate = true;
                        CombatText.NewText(target.getRect(), new Color(200, 100, 255), "预言应验!", true);

                        if (Main.myPlayer == Player.whoAmI)
                        {
                            for (int i = 0; i < 25; i++)
                            {
                                Dust d = Dust.NewDustPerfect(target.Center,
                                    DustID.PurpleCrystalShard, Main.rand.NextVector2Circular(5, 5), 0, default, 1.5f);
                                d.noGravity = true;
                            }
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, target.position);
                        }
                    }
                }
                // [2] 杀死敌人时15%概率预言下一击 (启示中提升至30%)
                if (target.life <= 0)
                {
                    float prophChance = (revelationActiveTimer > 0) ? 1.0f : 0.15f;
                    if (Main.rand.NextFloat() < prophChance && !prophecyMarked)
                    {
                        prophecyMarked = true;
                        prophecyDuration = 600; // 10秒内有效
                        if (Main.myPlayer == Player.whoAmI)
                        {
                            CombatText.NewText(Player.getRect(), new Color(180, 100, 255), "已预言下一击!", true);
                        }
                    }
                }
            }

            if (preventRecursiveOp) return;
            ApplyHitEffects(target); CheckRitualKill(target); CheckExecution(target); if (isPassiveStealEnabled)
            {
                if (currentMarauderSequence <= 9)
                {
                    // 1. 金币窃取 (任何序列9)
                    // 如果佩戴恋人牌，金币窃取几率提升至 100%
                    if (isLoversCardEquipped || Main.rand.NextBool(5))
                    {
                        target.value *= 1.2f;
                        Dust.NewDust(target.position, target.width, target.height, DustID.GoldCoin, 0, 0, 0, default, 0.8f);
                    }

                    // 2. 物品窃取 (序列6 盗火人)
                    if (currentMarauderSequence <= 6)
                    {
                        float baseChance = target.boss ? 0.002f : 0.02f;
                        float multiplier = 1f + (6 - currentMarauderSequence) * 0.3f;
                        float finalChance = baseChance * multiplier;

                        // === [新增逻辑] 恋人牌判定 ===
                        if (isLoversCardEquipped)
                        {
                            finalChance = 1.0f; // 强制几率为 100% (必定掉落)
                        }
                        // ===========================

                        // 原有的上限限制 (如果有恋人牌则突破限制)
                        if (!isLoversCardEquipped)
                        {
                            if (!target.boss && finalChance > 0.1f) finalChance = 0.1f;
                            if (target.boss && finalChance > 0.01f) finalChance = 0.01f;
                        }

                        // 3. 触发次数
                        int stealAttempts = (currentMarauderSequence <= 2) ? 6 : (currentMarauderSequence <= 3 ? 3 : 1);

                        for (int n = 0; n < stealAttempts; n++)
                        {
                            if (Main.rand.NextFloat() < finalChance && target.life > 0)
                            {
                                var dropInfo = new Terraria.GameContent.ItemDropRules.DropAttemptInfo
                                {
                                    player = Player,
                                    npc = target,
                                    IsExpertMode = Main.expertMode,
                                    IsMasterMode = Main.masterMode,
                                    IsInSimulation = false,
                                    rng = Main.rand
                                };
                                Main.ItemDropSolver.TryDropping(dropInfo);

                                // 只有第一次显示文字
                                if (n == 0)
                                {
                                    string text = "窃取!";
                                    Color color = new Color(255, 165, 0); // 橙色

                                    if (isLoversCardEquipped)
                                    {
                                        text = "命运窃取!";
                                        color = new Color(178, 102, 255); // 紫色
                                    }
                                    else if (currentMarauderSequence <= 2) text = "命运窃取 (x6)!";
                                    else if (currentMarauderSequence <= 3) text = "三重窃取!";

                                    CombatText.NewText(target.getRect(), color, text, true);
                                }

                                // 窃取成功时的金光特效 (这里可能用的是 i，没关系)
                                for (int i = 0; i < 5; i++)
                                    Dust.NewDust(target.position, target.width, target.height, DustID.GoldFlame, 0, 0, 0, default, 1.0f);

                                // --- 仪式逻辑 (这里面可能原本包含了一个 int k 的循环) ---
                                if (currentMarauderSequence == 5 && parasiteRitualProgress < PARASITE_RITUAL_TARGET)
                                {
                                    parasiteRitualProgress++;
                                    if (parasiteRitualProgress >= PARASITE_RITUAL_TARGET)
                                    {
                                        Main.NewText("仪式完成：命运的馈赠已集齐... (9/9)", 220, 20, 60);
                                        SoundEngine.PlaySound(SoundID.Roar, Player.position);

                                        // 【这里就是冲突的根源】原来的代码里有 int k
                                        // 现在外层改成了 n，这里就可以安全地使用 k 了
                                        for (int k = 0; k < 20; k++)
                                        {
                                            Dust.NewDust(Player.position, Player.width, Player.height, DustID.PurpleCrystalShard, 0, 0, 0, default, 1.5f);
                                        }
                                    }
                                    else
                                    {
                                        Main.NewText($"从目标处获得了‘供养’... ({parasiteRitualProgress}/{PARASITE_RITUAL_TARGET})", 150, 150, 150);
                                    }
                                }
                                for (int i = 0; i < 5; i++)
                                    Dust.NewDust(target.position, target.width, target.height, DustID.GoldFlame, 0, 0, 0, default, 1.0f);
                            }

                            // 2. 窃取能力 (模拟：吸取生命/魔力/Buff)
                            // 每次攻击有概率回复生命或魔力，模拟“偷走了对方的力量”
                            if (Main.rand.NextBool(10))
                            {
                                int stealAmount = (int)(damageDone * 0.1f); // 偷取 10% 伤害值的生命/蓝
                                if (stealAmount < 1) stealAmount = 1;
                                if (stealAmount > 20) stealAmount = 20;

                                if (Main.rand.NextBool())
                                {
                                    Player.statLife += stealAmount;
                                    Player.HealEffect(stealAmount);
                                }
                                else
                                {
                                    Player.statMana += stealAmount;
                                    Player.ManaEffect(stealAmount);
                                }
                            }
                        }

                    }
                }
            }
            if (currentMarauderSequence <= 5)
            {
                // 10% 概率偷走对方“攻击/移动”的想法 -> 造成强力减速或混乱
                if (Main.rand.NextBool(10))
                {
                    target.AddBuff(BuffID.Confused, 180); // 混乱3秒
                    target.AddBuff(BuffID.Slow, 300);     // 减速5秒

                    // 如果是 Boss，可能免疫混乱，但可以稍微减速
                    if (target.boss)
                    {
                        // 可以在这里写特殊的Boss减速逻辑，或者直接忽略
                    }
                    else
                    {
                        // 普通怪直接呆滞一瞬间 (速度归零)
                        target.velocity *= 0.1f;
                    }

                    // 视觉反馈：梦境气泡
                    for (int i = 0; i < 5; i++)
                        Dust.NewDust(target.position, target.width, target.height, DustID.DungeonSpirit, 0, 0, 100, default, 1f);
                }
                if (Main.rand.NextBool(5))
                {
                    // 削弱敌人
                    target.AddBuff(BuffID.Weak, 300);       // 虚弱 (减攻/减速)
                    target.AddBuff(BuffID.BrokenArmor, 300);// 破甲 (减防)

                    // 回复自身 (将记忆转化为精神养分)
                    int heal = 2;
                    Player.statLife += heal;
                    Player.HealEffect(heal);

                    // 特效
                    CombatText.NewText(target.getRect(), new Color(147, 112, 219), "记忆窃取!", true);
                }
            }
            if (isStrengthCardEquipped && hit.DamageType.CountsAsClass(DamageClass.Melee))
            {
                target.AddBuff(BuffID.BrokenArmor, 600); // 破甲 10秒
                target.AddBuff(BuffID.Ichor, 300);       // 灵液 5秒 (降低20防御)

                // 2. 震荡波特效 (视觉效果)
                if (Main.rand.NextBool(3))
                {
                    SoundEngine.PlaySound(SoundID.Item14, target.Center); // 爆炸音效

                    for (int i = 0; i < 15; i++)
                    {
                        Vector2 speed = Main.rand.NextVector2Circular(6f, 6f);
                        Dust d = Dust.NewDustPerfect(target.Center, DustID.Smoke, speed, 100, default, 1.5f);
                        d.noGravity = true;
                    }
                }
            }
            if (instigatorEffect)
            {
                // 20% 几率让非Boss敌人陷入混乱 3秒
                if (Main.rand.NextBool(5) && !target.boss)
                {
                    target.AddBuff(Terraria.ID.BuffID.Confused, 180);
                }

                // 攻击原本就混乱的敌人，造成额外真实伤害
                if (target.HasBuff(Terraria.ID.BuffID.Confused))
                {
                    // 【关键修改】：先上锁，再造成伤害，最后解锁
                    preventRecursiveOp = true;

                    try
                    {
                        // 这里的伤害会再次触发 OnHitNPC，但因为锁上了，会直接 return，不会死循环
                        Player.ApplyDamageToNPC(target, (int)(damageDone * 0.2f), 0f, 0, false);
                    }
                    finally
                    {
                        preventRecursiveOp = false; // 无论发生什么，一定要把锁解开
                    }
                }
            }
            ApplyDemonessHitEffects(target, damageDone);

            base.OnHitNPC(target, hit, damageDone);
        }
        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 1. 基础检查：开启了模式 + 序列8 + 必须是鞭子
            if (isTamingActive && currentMoonSequence <= 8 && ProjectileID.Sets.IsAWhip[proj.type])
            {
                // 2. 目标检查：活的 + 敌对 + 非Boss + 非无敌
                if (target.active && !target.friendly && !target.boss && !target.dontTakeDamage)
                {
                    // 3. 血量检查：虚弱(25%血) 或 弱小生物(血上限<50)
                    bool isWeak = (target.life <= target.lifeMax * 0.25f) || (target.lifeMax < 50);

                    if (isWeak)
                    {
                        // 4. 尝试消耗灵性进行契约 (20点)
                        if (TryConsumeSpirituality(20))
                        {
                            SoundEngine.PlaySound(SoundID.Item29, target.position); // 成功音效

                            // === 核心修复：强制满血 + 视觉显示 ===
                            int healAmount = target.lifeMax - target.life; // 计算需要回多少血
                            target.life = target.lifeMax; // 1. 实际加血
                            target.HealEffect(healAmount); // 2. 【关键】显示绿色回血数字！

                            // 清除所有负面状态 (防止驯服后被之前的流血/中毒死)
                            for (int i = 0; i < target.buffType.Length; i++)
                            {
                                if (target.buffType[i] > 0 && Main.debuff[target.buffType[i]])
                                {
                                    target.DelBuff(i);
                                    i--;
                                }
                            }

                            // 添加驯服 Buff
                            target.AddBuff(ModContent.BuffType<Buffs.TamedBuff>(), 18000);
                            if (target.TryGetGlobalNPC(out global::zhashi.Content.NPCs.TamingGlobalNPC tamingGlobal))
                            {
                                tamingGlobal.ownerIndex = Player.whoAmI;
                                // 强制同步这个变化给所有客户端和服务器
                                target.netUpdate = true;
                            }

                            // 特效
                            CombatText.NewText(target.getRect(), Color.LightGreen, "驯服成功!", true);
                            for (int k = 0; k < 20; k++)
                            {
                                Dust.NewDust(target.position, target.width, target.height, DustID.HeartCrystal, 0, 0, 0, default, 1.5f);
                            }
                        }
                        else
                        {
                            Main.NewText("灵性不足以完成契约！", 255, 50, 50);
                        }
                    }
                    else
                    {
                        // 提示需虚弱 (加上概率防止刷屏)
                        if (Main.rand.NextBool(10))
                            CombatText.NewText(target.getRect(), Color.Gray, "需虚弱(25%血)!", true);
                    }
                }
            }
            if (currentDemonessSequence <= 3)
            {
                LifeSteal(damageDone);
            }
            ApplyDemonessHitEffects(target, damageDone);

            base.OnHitNPCWithProj(proj, target, hit, damageDone);
        }
        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (currentDemonessSequence <= 3) // 序列3能力
            {
                LifeSteal(damageDone);
            }
        }
        private void LifeSteal(int damage)
        {
            // 只有没满血时才吸，吸取伤害的 5%，最小 1 点
            if (Player.statLife < Player.statLifeMax2 && Main.rand.NextBool(3)) // 33% 几率触发，防止太变态
            {
                int heal = damage / 20;
                if (heal < 1) heal = 1;
                if (heal > 10) heal = 10; // 单次上限

                Player.Heal(heal);
                // 吸血特效
                Dust.NewDust(Player.position, Player.width, Player.height, DustID.HeartCrystal);
            }
        }

        private void ApplyHitEffects(NPC target) { if (currentSequence <= 4) target.AddBuff(BuffID.Ichor, 300); if (currentHunterSequence <= 7) target.AddBuff(BuffID.OnFire, 300); if (isCalamityGiant) target.AddBuff(BuffID.Electrified, 300); if (currentHunterSequence <= 1) target.AddBuff(ModContent.BuffType<ConquerorWill>(), 600); if (currentMoonSequence <= 7 && (Player.HeldItem.DamageType == DamageClass.Melee || Player.HeldItem.DamageType == DamageClass.SummonMeleeSpeed || Player.HeldItem.DamageType == DamageClass.Summon)) { target.AddBuff(BuffID.Ichor, 300); if (Main.rand.NextBool(3)) target.AddBuff(BuffID.Poisoned, 300); } }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (currentHunterSequence <= 5)
            {
                modifiers.CritDamage += 0.5f;
            }

            // 序列3 怪人 - 怪人之眼: 暴击伤害+30%
            if (currentWheelSequence <= 3)
            {
                modifiers.CritDamage += 0.3f;
            }
            // 序列2 先知 - 命运洞察: 暴击伤害再+20% (叠加共+50%)
            if (currentWheelSequence <= 2)
            {
                modifiers.CritDamage += 0.2f;
            }
            // 序列1 巨蛇 - 万物之蛇: 暴击伤害再+30% (叠加共+80%)
            if (currentWheelSequence <= 1)
            {
                modifiers.CritDamage += 0.3f;
            }

            if (currentFoolSequence <= 1 && graftingMode == 2)
            {
                bool nerf = ModContent.GetInstance<LotMConfig>().NerfDivineAbilities;

                if (nerf)
                {
                    float worldMult = Systems.BalanceSystem.GetWorldTierMultiplier();
                    int damageCap = (int)(5000 * worldMult);

                    // 基础附加：Boss 1%, 小怪 10%
                    int bonusDamage = target.lifeMax / (target.boss ? 100 : 10);
                    if (bonusDamage > damageCap) bonusDamage = damageCap;

                    modifiers.FinalDamage.Flat += bonusDamage;
                    modifiers.SetCrit(); // 必暴
                }
                else
                {
                    // 【原著模式】秒杀
                    modifiers.SetCrit();
                    modifiers.FinalDamage *= 100;
                    modifiers.ArmorPenetration += 9999;
                }
            }

            // 3. 处理净化斩 (需警惕联机同步问题)
            if (isCleansingSlash)
            {
                // 判定亡灵生物
                bool isUndead = NPCID.Sets.Zombies[target.type] ||
                                NPCID.Sets.Skeletons[target.type] ||
                                target.aiStyle == 22 ||
                                target.coldDamage; // 冷知识：冷伤害怪通常被视作亡灵相关

                if (isUndead)
                {
                    modifiers.FinalDamage *= 1.5f; // 额外50%伤害
                    modifiers.SetCrit(); // 必暴

                    if (Main.netMode != NetmodeID.Server)
                    {
                        for (int i = 0; i < 5; i++)
                            Dust.NewDust(target.position, target.width, target.height, DustID.GoldFlame, 0, 0, 0, default, 1f);
                    }
                }

                // 基础附加伤害
                modifiers.FlatBonusDamage += 10;
            }

            // 保持基类逻辑 (通常放在最后)
            base.ModifyHitNPC(target, ref modifiers);
        }
        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers) { if (currentHunterSequence <= 5) modifiers.CritDamage += 0.5f; if (currentWheelSequence <= 3) modifiers.CritDamage += 0.3f; if (currentWheelSequence <= 2) modifiers.CritDamage += 0.2f; if (currentWheelSequence <= 1) modifiers.CritDamage += 0.3f; }
        private void CheckExecution(NPC target) { if (currentHunterSequence <= 5 && !target.boss && target.life < target.lifeMax * 0.2f) target.SimpleStrikeNPC(9999, 0); }
        private void CheckRitualKill(NPC target) { if (target.life <= 0) { if (currentSequence == 5 && demonHunterRitualProgress < DEMON_HUNTER_RITUAL_TARGET) { if (target.type == NPCID.RedDevil) { demonHunterRitualProgress++; } } }
            if (currentMarauderSequence == 4 && mentorRitualProgress < MENTOR_RITUAL_TARGET)
            {
                if (target.HasBuff(BuffID.Confused))
                {
                    mentorRitualProgress++;
                    if (mentorRitualProgress >= MENTOR_RITUAL_TARGET)
                    {
                        Main.NewText("仪式完成：秩序已瓦解，九个被误导的冤魂正在哀嚎... (9/9)", 0, 255, 127); // 碧绿色提示
                        SoundEngine.PlaySound(SoundID.ZombieMoan, Player.position);
                    }
                    else
                    {
                        Main.NewText($"冤魂 +1 ({mentorRitualProgress}/{MENTOR_RITUAL_TARGET})", 200, 200, 200);
                    }
                }
            }
            if (currentSunSequence == 6 && purificationProgress < PURIFICATION_RITUAL_TARGET)
            {
                bool isUndead = IsUndeadCreature(target);
                if (isUndead)
                {
                    purificationProgress++;
                    if (purificationProgress >= PURIFICATION_RITUAL_TARGET)
                    {
                        purificationProgress = PURIFICATION_RITUAL_TARGET;
                        Main.NewText("仪式完成：光芒已净化这片土地... (100/100)", 255, 215, 0); // 金色提示
                        SoundEngine.PlaySound(SoundID.Item29, Player.position);
                    }
                }
            }

            if (currentSunSequence == 5 && judgmentProgress < JUDGMENT_RITUAL_TARGET)
            {
                if (target.boss || target.lifeMax > 2000)
                {
                    judgmentProgress++;
                    if (judgmentProgress >= JUDGMENT_RITUAL_TARGET)
                    {
                        judgmentProgress = JUDGMENT_RITUAL_TARGET;
                        Main.NewText("仪式完成：契约已签订，罪恶已受审判... (20/20)", 255, 69, 0); // 橙红色提示
                        SoundEngine.PlaySound(SoundID.Item29, Player.position);
                    }
                    else
                    {
                        // 提示进度
                        Main.NewText($"审判强敌: {judgmentProgress}/{JUDGMENT_RITUAL_TARGET}", 255, 200, 100);
                    }
                }
            }
        }
        public override void PostHurt(Player.HurtInfo info) { if (currentSequence <= 6 && dawnArmorActive && !dawnArmorBroken) { dawnArmorCurrentHP -= info.Damage; if (dawnArmorCurrentHP <= 0) { dawnArmorCurrentHP = 0; dawnArmorActive = false; dawnArmorBroken = true; dawnArmorCooldownTimer = DAWN_ARMOR_COOLDOWN_MAX; Main.NewText("铠甲已重铸", 100, 255, 100); } }
            if (currentSequence == 6 && guardianRitualProgress < GUARDIAN_RITUAL_TARGET)
            {
                bool npcNearby = false;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if ((npc.townNPC || npc.type == NPCID.TravellingMerchant || npc.type == NPCID.SkeletonMerchant) && npc.Distance(Player.Center) < 800f)
                    {
                        npcNearby = true;
                        break;
                    }
                }

                if (npcNearby)
                {
                    guardianRitualProgress += info.Damage;
                    if (guardianRitualProgress >= GUARDIAN_RITUAL_TARGET)
                    {
                        guardianRitualProgress = GUARDIAN_RITUAL_TARGET;
                        Main.NewText("仪式完成：你已证明了守护的决心！(1000/1000)", 255, 215, 0); // 金色提示
                        SoundEngine.PlaySound(SoundID.Item37, Player.position); // 播放一个提示音效
                    }
                }
            }
        }
        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            // =================================================
            // 【序列1 巨蛇】重启循环 - 死亡时自动重置到10秒前的状态
            // =================================================
            if (currentWheelSequence <= 1 && restartAutoCooldown <= 0 && TryConsumeSpirituality(5000))
            {
                restartAutoCooldown = RESTART_AUTO_CD_MAX;

                // 满血+清debuff+50%灵性
                Player.statLife = Player.statLifeMax2;
                Player.HealEffect(Player.statLifeMax2);
                spiritualityCurrent = spiritualityMax * 0.5f;
                for (int b = 0; b < Player.MaxBuffs; b++)
                {
                    int bt = Player.buffType[b];
                    if (bt > 0 && Main.debuff[bt]) Player.DelBuff(b);
                }
                Player.immune = true;
                Player.immuneTime = 300; // 5秒无敌防连击死

                // 回到10秒前的位置
                if (positionHistoryFilled)
                {
                    Vector2 pastPos = positionHistory[positionHistoryIdx]; // 缓冲区下一个写入位置即"最早"的记录
                    Player.position = pastPos - Player.Size / 2f;
                    Player.velocity = Vector2.Zero;
                }

                // 视觉震撼: 屏幕全白 + 时间倒流回放
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item104, Player.position);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath52, Player.position);
                Main.NewText("【重启循环】命运被你逆转 — 死亡只是另一个时间线。", 200, 200, 255);
                Main.NewText("（自动复活已使用，10 分钟后可再次触发）", 150, 150, 200);

                if (Main.myPlayer == Player.whoAmI)
                {
                    // 拖影回放: 在历史位置上洒满银色拖影
                    for (int i = 0; i < positionHistory.Length; i += 10)
                    {
                        int idx = (positionHistoryIdx + i) % positionHistory.Length;
                        Vector2 hp = positionHistory[idx];
                        if (hp == Vector2.Zero) continue;
                        Dust d = Dust.NewDustPerfect(hp, DustID.SilverCoin, Vector2.Zero, 0, default, 1.6f);
                        d.noGravity = true;
                        d.fadeIn = 1.5f;
                    }
                    // 中心爆发: 200颗银紫粒子+360度发散
                    for (int k = 0; k < 200; k++)
                    {
                        float ang = (k / 200f) * MathHelper.TwoPi;
                        Dust d = Dust.NewDustPerfect(Player.Center,
                            (k % 2 == 0) ? DustID.SilverCoin : DustID.PurpleCrystalShard,
                            new Vector2((float)System.Math.Cos(ang), (float)System.Math.Sin(ang)) * 8f, 0, default, 2.5f);
                        d.noGravity = true;
                        d.fadeIn = 1.8f;
                    }
                    // 镜头大幅震动
                    Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                        Player.Center, new Vector2(0.5f, -0.5f), 20f, 10f, 30, 1000f));
                }

                return false; // 取消死亡
            }

            // =================================================
            // 【新增】序列1：概念嫁接 (防御模式)
            // =================================================
            if (currentFoolSequence <= 1)
            {
                // 需要 5000 灵性来嫁接一次死亡
                if (TryConsumeSpirituality(50000))
                {
                    SoundEngine.PlaySound(SoundID.Item4, Player.position);

                    // 恢复满血
                    Player.statLife = Player.statLifeMax2;
                    Player.HealEffect(Player.statLifeMax2);

                    // 短暂无敌
                    Player.immune = true;
                    Player.immuneTime = 60;

                    Main.NewText("嫁接：死亡的概念被移除了。", 200, 200, 255);

                    // 特效：扭曲的虚空
                    for (int i = 0; i < 50; i++)
                        Dust.NewDust(Player.position, Player.width, Player.height, DustID.Vortex, 0, 0, 0, default, 2f);

                    return false; // 取消死亡
                }
            }
            // =================================================
            // 1. 荣耀战神复活 (巨人途径 序列2)
            // =================================================
            if (currentSequence <= 2 && twilightResurrectionCooldown <= 0)
            {
                SoundEngine.PlaySound(SoundID.Item119, Player.position);
                Player.statLife = Player.statLifeMax2;
                Player.HealEffect(Player.statLifeMax2);
                Player.immune = true;
                Player.immuneTime = 180;
                twilightResurrectionCooldown = TWILIGHT_RESURRECTION_MAX;
                return false; // 取消死亡
            }

            // =================================================
            // 2. 伤害转移 (愚者途径 序列7 魔术师)
            // =================================================
            // 只有当不是半神(序列4以上)时，主要依赖这个。
            // 到了半神主要靠灵之虫，但这个作为最后手段也可以保留。
            if (currentFoolSequence <= 7 && damageTransferCooldown <= 0 && TryConsumeSpirituality(50, true))
            {
                // 如果是半神且有足够的灵之虫，优先触发灵之虫复活，跳过伤害转移
                if (currentFoolSequence <= 4 && spiritWorms >= 10)
                {
                    // 跳过此逻辑，进入下面的灵之虫判断
                }
                else
                {
                    SoundEngine.PlaySound(SoundID.Item8, Player.position);
                    Player.statLife = 50;
                    Player.immune = true;
                    Player.immuneTime = 120;

                    // 序列6冷却减半
                    damageTransferCooldown = (currentFoolSequence <= 6) ? 5400 : 10800;

                    CombatText.NewText(Player.getRect(), Color.Red, "伤害转移!", true);
                    for (int i = 0; i < 30; i++) Dust.NewDust(Player.position, Player.width, Player.height, DustID.Blood, 0, 0, 0, default, 1.5f);
                    return false; // 取消死亡
                }
            }

            // =================================================
            // 3. 灵之虫复活 (愚者途径 序列4 - 序列2)
            // =================================================
            if (currentFoolSequence <= 4)
            {
                // --- 动态计算消耗 ---
                int wormsNeeded = 10; // 序列4基础消耗
                if (currentFoolSequence <= 3) wormsNeeded = 100;
                if (currentFoolSequence <= 2) wormsNeeded = 300;

                // 检查虫子够不够
                if (spiritWorms >= wormsNeeded)
                {
                    spiritWorms -= wormsNeeded;
                    SoundEngine.PlaySound(SoundID.Item29, Player.position);

                    // --- 动态计算回血 ---
                    int heal = Player.statLifeMax2; // 默认满血 (序列3, 2)
                    if (currentFoolSequence == 4) heal = Player.statLifeMax2 / 2; // 序列4回一半

                    Player.statLife = heal;
                    Player.HealEffect(heal);

                    Player.immune = true;
                    Player.immuneTime = (currentFoolSequence <= 2) ? 300 : 180; // 序列2无敌时间更长

                    // --- 序列2特权：复活奇迹 (清屏伤害) ---
                    if (currentFoolSequence <= 2)
                    {
                        Main.NewText("奇迹降临：死而复生！", 255, 215, 0);
                        // 对全屏敌人造成伤害
                        foreach (NPC npc in Main.ActiveNPCs)
                        {
                            if (!npc.friendly && !npc.dontTakeDamage)
                            {
                                int dmg = 5000;
                                Player.ApplyDamageToNPC(npc, dmg, 0, 0, false);
                                // 生成特效
                                for (int i = 0; i < 5; i++) Dust.NewDust(npc.position, npc.width, npc.height, DustID.Enchanted_Gold, 0, 0, 0, default, 1.5f);
                            }
                        }
                    }
                    else
                    {
                        // 普通复活提示
                        Main.NewText($"灵之虫替你承受了死亡... (剩余: {spiritWorms})", 200, 200, 200);
                    }

                    // 视觉特效：虫子重组
                    for (int i = 0; i < 40; i++) Dust.NewDust(Player.position, Player.width, Player.height, DustID.Worm, Main.rand.NextFloat(-5, 5), Main.rand.NextFloat(-5, 5), 0, default, 1.5f);

                    return false; // 取消死亡
                }
            }
            // =================================================
            // 错误途径序列4：半虫化 (不死之身)
            // =================================================
            if (currentMarauderSequence <= 4 && wormificationCooldown <= 0)
            {
                // 消耗大量灵性重组身体
                if (TryConsumeSpirituality(200, true)) // 紧急消耗，允许透支一点
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath13, Player.position); // 虫子恶心的声音

                    // 恢复部分生命
                    int heal = Player.statLifeMax2 / 2; // 恢复一半血
                    Player.statLife = heal;
                    Player.HealEffect(heal);

                    // 给予无敌时间
                    Player.immune = true;
                    Player.immuneTime = 180; // 3秒无敌

                    // 视觉特效：身体分散成虫子又聚合
                    for (int i = 0; i < 30; i++)
                    {
                        Dust.NewDust(Player.position, Player.width, Player.height, DustID.Worm, Main.rand.NextFloat(-5, 5), Main.rand.NextFloat(-5, 5), 0, default, 1.2f);
                    }

                    Main.NewText("身体瞬间分散成无数虫豸，躲过了致命一击！", 175, 238, 238);

                    wormificationCooldown = WORMIFICATION_COOLDOWN_MAX; // 进入长冷却
                    return false; // 拒绝死亡
                }
            }
            // =================================================
            // 错误途径序列2：命运木马 (分身替死)
            // =================================================
            if (currentMarauderSequence <= 2 && !isTrojanResurrection)
            {
                // 消耗大量灵性
                if (TryConsumeSpirituality(300, true))
                {
                    isTrojanResurrection = true; // 标记已触发，防止无限触发（需在重置效果或冷却中重置）
                    twilightResurrectionCooldown = 3600; // 复用通用的复活冷却变量，或者新建一个

                    Player.statLife = Player.statLifeMax2; // 满血复活
                    Player.HealEffect(Player.statLifeMax2);
                    Player.immune = true;
                    Player.immuneTime = 120;

                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29, Player.position); // 时空扭曲声
                    Main.NewText("命运的浪花已被预见，死亡的是你的‘过去’...", 200, 200, 255);

                    // 特效：生成一个假身破碎
                    for (int i = 0; i < 30; i++)
                    {
                        Dust.NewDust(Player.position, Player.width, Player.height, DustID.SilverCoin, Main.rand.NextFloat(-5, 5), Main.rand.NextFloat(-5, 5), 0, default, 1.5f);
                    }

                    return false; // 拒绝死亡
                }
            }
            if (currentDemonessSequence <= 3 && unagingRebirthCooldown <= 0 && spiritualityCurrent >= 500)
            {
                spiritualityCurrent -= 500;
                unagingRebirthCooldown = REBIRTH_COOLDOWN_MAX; // 进入5分钟冷却

                // 1. 恢复满血
                Player.statLife = Player.statLifeMax2;

                // 2. 移除所有负面Buff
                for (int i = 0; i < Player.MaxBuffs; i++)
                {
                    if (Main.debuff[Player.buffType[i]] && !Main.buffNoTimeDisplay[Player.buffType[i]])
                    {
                        Player.DelBuff(i);
                        i--;
                    }
                }

                // 3. 特效与提示
                Main.NewText("镜面破碎，你从虚幻中归来！", 255, 20, 147);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Shatter, Player.position);

                // 生成大量玻璃碎片特效
                for (int i = 0; i < 50; i++)
                {
                    Dust.NewDust(Player.position, Player.width, Player.height, DustID.Glass, 0, 0, 100, default, 2f);
                }

                // 4. 短暂无敌
                Player.AddBuff(BuffID.ShadowDodge, 180); // 3秒闪避

                return false; // 阻止死亡！
            }

            return true; // 允许死亡 (如果上面都没触发)
        }


        // 【美神被动】
        public override bool FreeDodge(Player.HurtInfo info)
        {
            // --- 序列7 占星人 "灵性直觉" ---
            // 受到任何>10%最大血的伤害时,概率30%自动闪避(灵性预警),60秒CD
            if (currentDoorSequence <= 7 && astrologyIntuitionCooldown <= 0
                && info.Damage > Player.statLifeMax2 * 0.10f
                && Main.rand.NextBool(3, 10)) // 30%概率
            {
                astrologyIntuitionCooldown = ASTROLOGY_INTUITION_CD;

                if (Main.myPlayer == Player.whoAmI)
                {
                    // 短暂无敌防连击
                    Player.SetImmuneTimeForAllTypes(45);

                    // 视觉: 玩家周围出现星象
                    for (int k = 0; k < 24; k++)
                    {
                        float ang = (k / 24f) * MathHelper.TwoPi;
                        Vector2 pos = Player.Center + new Vector2((float)System.Math.Cos(ang), (float)System.Math.Sin(ang)) * 50f;
                        Dust d = Dust.NewDustPerfect(pos, DustID.GoldCoin, Vector2.Zero, 0, default, 1.5f);
                        d.noGravity = true;
                        d.fadeIn = 1.4f;
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9, Player.position);
                    CombatText.NewText(Player.getRect(), new Color(255, 220, 100), "灵性直觉!", true);
                    Main.NewText("◆ 灵性直觉! 你预知了这一击,本能地避开了。", 255, 220, 130);
                }
                return true; // 闪避
            }

            // --- 序列8 戏法大师 "逃脱戏法" ---
            // 受到较大伤害时(>15%最大血),自动闪现到附近躲避一次,30秒CD
            if (currentDoorSequence <= 8 && trickEscapeCooldown <= 0
                && info.Damage > Player.statLifeMax2 * 0.15f)
            {
                trickEscapeCooldown = TRICK_ESCAPE_CD_MAX;

                if (Main.myPlayer == Player.whoAmI)
                {
                    // 闪现: 向远离最近威胁的方向移动 8 格
                    Vector2 escapeDir = new Vector2(-Player.direction, -0.3f).SafeNormalize(Vector2.UnitX);
                    Vector2 target = Player.Center + escapeDir * 128f; // 8格

                    // 起点黑幕+闪光(注意力转移)
                    for (int k = 0; k < 30; k++)
                    {
                        Dust d = Dust.NewDustPerfect(Player.Center, DustID.WhiteTorch,
                            Main.rand.NextVector2Circular(8, 8), 0, default, 2f);
                        d.noGravity = true;
                    }
                    Player.position = target - Player.Size / 2f;
                    Player.velocity *= 0.2f;
                    // 终点出现
                    for (int k = 0; k < 30; k++)
                    {
                        Dust d = Dust.NewDustPerfect(Player.Center, DustID.Smoke,
                            Main.rand.NextVector2Circular(6, 6), 80, default, 1.6f);
                        d.noGravity = true;
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, Player.position);
                    CombatText.NewText(Player.getRect(), Color.LightGray, "逃脱戏法!", true);
                    Main.NewText("◆ 逃脱戏法! 你在敌人眼前消失,又出现在别处。", 200, 200, 220);
                }
                return true; // 完全闪避这次伤害
            }

            // --- 序列2 先知 "水银之躯" ---
            // 受到伤害时,10秒内进入命运回避状态(免疫一切伤害与减益), CD 60秒
            if (currentWheelSequence <= 2 && mercuryDodgeCooldown <= 0 && mercuryDodgeTimer <= 0)
            {
                mercuryDodgeTimer = MERCURY_DODGE_DURATION;
                mercuryDodgeCooldown = MERCURY_DODGE_CD_MAX;
                Player.SetImmuneTimeForAllTypes(60);

                if (Main.myPlayer == Player.whoAmI)
                {
                    CombatText.NewText(Player.getRect(), Color.Silver, "水银之躯!", true);
                    Main.NewText("水银之躯：命运与你融为一体，10秒内不会被任何事物影响。", 192, 192, 220);
                }
                for (int i = 0; i < 40; i++)
                {
                    Dust d = Dust.NewDustPerfect(Player.Center,
                        DustID.SilverCoin, Main.rand.NextVector2Circular(6, 6), 0, default, 1.8f);
                    d.noGravity = true;
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29, Player.position);
                return true; // 第一次受伤直接抹掉
            }

            // --- 序列3 怪人 "不可定数" ---
            // 致命伤30%几率被命运抹除(改为1点伤害),30秒CD
            if (currentWheelSequence <= 3 && fateNullifyCooldown <= 0 && info.Damage >= Player.statLife)
            {
                if (Main.rand.NextFloat() < 0.30f)
                {
                    fateNullifyCooldown = FATE_NULLIFY_CD_MAX;
                    Player.SetImmuneTimeForAllTypes(60); // 1秒无敌避免连击死

                    // 先知仪式计数: 每次不可定数成功触发, 计1次
                    if (baseWheelSequence == 3 && !prophetRitualComplete && Main.myPlayer == Player.whoAmI)
                    {
                        prophetRitualProgress++;
                        CombatText.NewText(Player.getRect(), new Color(200, 100, 255),
                            $"必死之劫 ({prophetRitualProgress}/{PROPHET_RITUAL_TARGET})", true);
                        Main.NewText($"你又一次见证了命运拒绝死亡... ({prophetRitualProgress}/{PROPHET_RITUAL_TARGET})", 200, 100, 255);
                    }

                    // 视觉:命运齿轮特效
                    for (int i = 0; i < 30; i++)
                    {
                        Dust d = Dust.NewDustPerfect(Player.Center,
                            DustID.GoldCoin, Main.rand.NextVector2Circular(8, 8), 0, default, 2f);
                        d.noGravity = true;
                    }
                    CombatText.NewText(Player.getRect(), new Color(255, 215, 0), "命运拒绝!", true);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29, Player.position);
                    return true; // 完全免疫此次伤害
                }
            }

            if (currentSunSequence <= 2 && Main.rand.NextFloat() < 0.35f)
            {
                // 1. 赋予无敌时间 (90帧 = 1.5秒)
                Player.SetImmuneTimeForAllTypes(90);

                // 2. 特效：身体崩解成光点
                for (int i = 0; i < 50; i++)
                {
                    Dust d = Dust.NewDustPerfect(Player.Center, DustID.GoldFlame, Main.rand.NextVector2Circular(10, 10), 0, default, 2.5f);
                    d.noGravity = true;
                }

                // 3. 瞬移一点距离 (重组)
                Vector2 dodgePos = Player.position + Main.rand.NextVector2Circular(150, 150);
                // 确保瞬移位置不是墙里
                if (!Collision.SolidCollision(dodgePos, Player.width, Player.height))
                {
                    // 产生瞬移线
                    for (int k = 0; k < 20; k++)
                    {
                        Vector2 lerpPos = Vector2.Lerp(Player.position, dodgePos, k / 20f);
                        Dust.NewDustPerfect(lerpPos, DustID.SolarFlare, Vector2.Zero, 0, default, 1f).noGravity = true;
                    }
                    Player.position = dodgePos;
                }

                // 4. 提示文字
                CombatText.NewText(Player.getRect(), Color.Gold, "光化重组!", true);
                return true; // 成功闪避，不再执行后续判定
            }

            // 【新增】序列1：诡秘侍者 (灵体状态下免疫大部分伤害)
            if (isSpiritForm)
            {
                // 产生一点幽灵特效
                if (Main.rand.NextBool(3))
                    Dust.NewDust(Player.position, Player.width, Player.height, DustID.SpectreStaff, 0, 0, 150, default, 1.5f);
                return true; // 直接免疫此次伤害
            }

            // 1. 序列1 美神 (月亮途径) - 25% 几率
            if (currentMoonSequence <= 1 && Main.rand.NextFloat() < 0.25f)
            {
                Player.SetImmuneTimeForAllTypes(60);
                CombatText.NewText(Player.getRect(), Color.Pink, "Miss", true);
                return true;
            }

            // 2. 序列7 魔术师 & 序列6 无面人 (愚者途径)
            if (currentFoolSequence <= 7)
            {
                // 序列6几率提升至 40%，序列7为 25%
                float paperChance = (currentFoolSequence <= 6) ? 0.4f : 0.25f;

                if (Main.rand.NextFloat() < paperChance) // 修正：需要先判定触发概率
                {
                    int paperItemType = ModContent.ItemType<Content.Items.Consumables.PaperFigurine>();

                    if (Player.CountItem(paperItemType) > 0)
                    {
                        Player.ConsumeItem(paperItemType);
                        Player.SetImmuneTimeForAllTypes(120); // 2秒无敌

                        // 特效
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item65, Player.position);
                        for (int i = 0; i < 20; i++) Dust.NewDust(Player.position, Player.width, Player.height, DustID.Confetti, 0, 0, 0, default, 1.5f);

                        // 随机位移
                        for (int i = 0; i < 10; i++) Dust.NewDust(Player.position, Player.width, Player.height, DustID.Smoke, 0, 0, 100, Color.Gray, 2f);
                        Vector2 randomPos = Player.position + Main.rand.NextVector2Circular(200, 200);
                        if (!Collision.SolidCollision(randomPos, Player.width, Player.height)) Player.position = randomPos;

                        CombatText.NewText(Player.getRect(), Color.White, "纸人替身!", true);
                        return true; // 闪避成功
                    }
                }
            }

            // -------------------------------------------------------------
            // C. 执行闪避判定
            // -------------------------------------------------------------
            float baseDodgeChance = info.PvP ? 0.05f : 0.15f;

            bool isRobot = currentWheelSequence <= 8;   // 序列8 机器
            bool isLucky = currentWheelSequence <= 7;   // 序列7 幸运儿
            bool isWinner = currentWheelSequence <= 5;  // 序列5 赢家
            bool isFaceless = currentFoolSequence <= 6; // 序列6 无面人

            // 【核心】计算幸运系数 (Luck Factor)
            float luckFactor = 0f;
            if (Player.luck > 0)
            {
                // 运气越高闪避越高，比如运气1.0时，系数为 1.2
                luckFactor = 1.0f + (Player.luck * 0.2f);
            }
            if (Player.luck <= 0)
            {
                // 如果厄运缠身 (运气<=0)，除了无面人外的依靠概率的闪避通通失效！
                luckFactor = 0f;
            }

            float finalChance = baseDodgeChance * luckFactor;

            // 愚者牌特权加成：如果是愚者途径且有牌，概率提升 50%
            if (currentFoolSequence <= 9 && isFoolCardEquipped)
            {
                finalChance *= 1.5f; // 修正：将dodgeChance改为了正确的finalChance
            }

            // 1. 常规反占卜闪避
            if ((isFaceless || (luckFactor > 0 && (isRobot || isAntiDivinationActive))) && Main.rand.NextFloat() < finalChance)
            {
                Player.SetImmuneTimeForAllTypes(60);

                // 修正：确保清理Debuff不会报错
                if (!info.PvP)
                {
                    for (int i = 0; i < Player.MaxBuffs; i++)
                    {
                        if (Player.buffType[i] > 0 && Main.debuff[Player.buffType[i]])
                        {
                            Player.DelBuff(i);
                            i--;
                        }
                    }
                }

                for (int i = 0; i < 15; i++) Dust.NewDustPerfect(Player.Center, DustID.DungeonSpirit, Main.rand.NextVector2Circular(3f, 3f), 150, default, 1.2f).noGravity = true;

                if (isFoolCardEquipped && currentFoolSequence <= 9) CombatText.NewText(Player.getRect(), new Color(186, 85, 211), "命运隐匿!", true);
                else CombatText.NewText(Player.getRect(), Color.Gray, "反占卜!", true);
                return true;
            }

            // 2. 序列7：人体描边 (高度依赖幸运)
            if (isLucky && luckFactor > 0)
            {
                float drawChance = 0.15f * luckFactor;
                if (drawChance > 0.4f) drawChance = 0.4f; // 封顶40%
                if (Main.rand.NextFloat() < drawChance)
                {
                    Player.SetImmuneTimeForAllTypes(40);
                    for (int i = 0; i < 10; i++) Dust.NewDustPerfect(Player.Center, DustID.GoldCoin, Main.rand.NextVector2Circular(2f, 2f));
                    string text = Main.rand.NextBool() ? "人体描边!" : "就差一点!";
                    CombatText.NewText(Player.getRect(), new Color(255, 215, 0), text, true);
                    return true;
                }
            }

            // 3. 序列5：戏剧性逆转 (高度依赖幸运)
            if (isWinner && luckFactor > 0)
            {
                float dramaChance = 0.33f * luckFactor;
                if (dramaChance > 0.6f) dramaChance = 0.6f; // 封顶60%
                if (Main.rand.NextFloat() < dramaChance)
                {
                    Player.SetImmuneTimeForAllTypes(90);
                    for (int i = 0; i < 20; i++) Dust.NewDustPerfect(Player.Center, DustID.RainbowMk2, Main.rand.NextVector2Circular(4f, 4f));
                    CombatText.NewText(Player.getRect(), Color.Gold, "戏剧性逆转!", true);
                    return true;
                }
            }

            // 4. 特殊变身形态 (默认拥有闪避/虚化效果)
            if (isCalamityGiant || isMercuryForm || isMoonlightized || isFacelessActive)
                return true;

            return base.FreeDodge(info);
        }

        // 7. 按键
        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            float moonMult = GetSequenceMultiplier(currentMoonSequence);
            float hunterMult = GetSequenceMultiplier(currentHunterSequence);
            float giantMult = GetSequenceMultiplier(currentSequence);
            float foolMult = GetSequenceMultiplier(currentFoolSequence);

            //大千录
            if (DaqianLuKeybinds.UseDaqianLu.JustPressed)
            {
                DaqianLuActions.ExecuteSkillWithDice(Player);
            }

            if (LotMKeybinds.Moon_Wings.JustPressed && currentMoonSequence <= 7) { isVampireWings = !isVampireWings; if (isVampireWings) { isBatSwarm = false; SoundEngine.PlaySound(SoundID.Item103, Player.position); Main.NewText("黑暗之翼：展开", 180, 0, 0); } else Main.NewText("黑暗之翼：收起", 200, 200, 200); }
            if (LotMKeybinds.Moon_BatSwarm.JustPressed && currentMoonSequence <= 4) { isBatSwarm = !isBatSwarm; if (isBatSwarm) { isVampireWings = false; isMoonlightized = false; SoundEngine.PlaySound(SoundID.Item103, Player.position); Main.NewText("化身为蝙蝠群...", 100, 0, 200); } else Main.NewText("解除化身", 200, 200, 200); }

            // J键
            if (LotMKeybinds.Moon_PaperFigurine.JustPressed)
            {
                if (currentMoonSequence <= 2)
                {
                    if (paperFigurineCooldown <= 0 && TryConsumeSpirituality(5000)) { SoundEngine.PlaySound(SoundID.Item119, Player.position); Main.NewText("万物滋长：生命归顺！", 100, 255, 100); foreach (NPC npc in Main.ActiveNPCs) { if (!npc.friendly && !npc.dontTakeDamage && npc.Distance(Player.Center) < 1500f) { if (npc.boss) { int dmg = (int)(5000 * moonMult); npc.SimpleStrikeNPC(dmg, 0, true, 0, DamageClass.Magic); CombatText.NewText(npc.getRect(), Color.Red, "生命剥夺!", true); } else { npc.AddBuff(ModContent.BuffType<TamedBuff>(), 36000); CombatText.NewText(npc.getRect(), Color.LightGreen, "新生!", true); for (int i = 0; i < 20; i++) Dust.NewDust(npc.position, npc.width, npc.height, DustID.Terra, 0, 0, 0, default, 1.5f); } } } for (int i = 0; i < Main.maxPlayers; i++) { Player p = Main.player[i]; if (p.active && !p.dead && p.whoAmI != Player.whoAmI && p.hostile && Player.hostile && p.Distance(Player.Center) < 1000f) { p.AddBuff(BuffID.Confused, 300); p.AddBuff(BuffID.Silenced, 300); } } paperFigurineCooldown = 18000; } else if (paperFigurineCooldown > 0) Main.NewText($"转化冷却: {paperFigurineCooldown / 60}s", 150, 150, 150);
                }
                else if (currentMoonSequence <= 4) { if (paperFigurineCooldown <= 0 && TryConsumeSpirituality(100)) { Player.velocity = -Player.velocity * 2f; Player.immune = true; Player.immuneTime = 60; SoundEngine.PlaySound(SoundID.Item6, Player.position); paperFigurineCooldown = 900; } }
            }

            if (LotMKeybinds.Moon_Gaze.JustPressed && currentMoonSequence <= 4) { if (darknessGazeCooldown <= 0 && TryConsumeSpirituality(200)) { bool hit = false; for (int i = 0; i < Main.maxNPCs; i++) { NPC npc = Main.npc[i]; if (npc.active && !npc.friendly && npc.getRect().Contains(Main.MouseWorld.ToPoint())) { int dmg = (int)(5000 * moonMult); if (currentMoonSequence <= 1 && !npc.boss) dmg = 999999; npc.SimpleStrikeNPC(dmg, 0, true, 0, DamageClass.Magic); npc.AddBuff(BuffID.Darkness, 600); npc.AddBuff(BuffID.ShadowFlame, 600); hit = true; } } if (hit) { SoundEngine.PlaySound(SoundID.Item104, Main.MouseWorld); darknessGazeCooldown = 1200; } } else if (darknessGazeCooldown > 0) Main.NewText($"凝视冷却: {darknessGazeCooldown / 60}s", 150, 150, 150); }
            if (LotMKeybinds.Moon_Shackles.JustPressed && currentMoonSequence <= 7) { if (abyssShackleCooldown <= 0 && TryConsumeSpirituality(30)) { Vector2 dir = (Main.MouseWorld - Player.Center).SafeNormalize(Vector2.Zero) * 12f; Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, dir, ModContent.ProjectileType<AbyssShackleProjectile>(), 20, 0f, Player.whoAmI); SoundEngine.PlaySound(SoundID.Item8, Player.position); abyssShackleCooldown = 180; } }
            if (LotMKeybinds.Moon_Grenade.JustPressed && currentMoonSequence <= 6)
            {
                if (currentMoonSequence <= 2) { if (purifyCooldown <= 0 && TryConsumeSpirituality(1000)) { int radius = 60; int centerX = (int)(Player.Center.X / 16f); int centerY = (int)(Player.Center.Y / 16f); for (int x = centerX - radius; x <= centerX + radius; x++) for (int y = centerY - radius; y <= centerY + radius; y++) WorldGen.Convert(x, y, 0, 0, false, false); SoundEngine.PlaySound(SoundID.Item29, Player.position); Main.NewText("大地重获新生。", 100, 255, 100); purifyCooldown = 600; } }
                else if (TryConsumeSpirituality(20)) { Vector2 dir = (Main.MouseWorld - Player.Center).SafeNormalize(Vector2.Zero) * 14f; int dmg = (int)(60 * moonMult); Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, dir, ModContent.ProjectileType<AlchemicalGrenade>(), dmg, 5f, Player.whoAmI); SoundEngine.PlaySound(SoundID.Item1, Player.position); }
            }
            if (LotMKeybinds.Moon_Elixir.JustPressed && currentMoonSequence <= 6)
            {
                if (currentMoonSequence <= 2) { if (elixirCooldown <= 0 && TryConsumeSpirituality(2000)) { int heal = Player.statLifeMax2 - Player.statLife; Player.statLife = Player.statLifeMax2; Player.HealEffect(heal); for (int i = 0; i < Player.MaxBuffs; i++) if (Main.debuff[Player.buffType[i]]) Player.DelBuff(i); SoundEngine.PlaySound(SoundID.Item4, Player.position); Main.NewText("生命奇迹：重获新生！", 0, 255, 255); elixirCooldown = 3600; } }
                else if (elixirCooldown <= 0 && TryConsumeSpirituality(200)) { int heal = (int)(1000 * moonMult); Player.statLife += heal; if (Player.statLife > Player.statLifeMax2) Player.statLife = Player.statLifeMax2; Player.HealEffect(heal); SoundEngine.PlaySound(SoundID.Item3, Player.position); elixirCooldown = 3600; Main.NewText("服用生命灵液！", 50, 255, 50); }
            }
            if (LotMKeybinds.Moon_Moonlight.JustPressed && currentMoonSequence <= 5) { isMoonlightized = !isMoonlightized; if (isMoonlightized) { isBatSwarm = false; SoundEngine.PlaySound(SoundID.Item8, Player.position); Main.NewText("身体化为绯红月光...", 255, 100, 100); } else Main.NewText("解除月光化", 200, 200, 200); }
            if (LotMKeybinds.Moon_FullMoon.JustPressed && currentMoonSequence <= 5)
            {
                if (currentMoonSequence <= 2) { isCreationDomain = !isCreationDomain; if (isCreationDomain) { isFullMoonActive = false; SoundEngine.PlaySound(SoundID.Item29, Player.position); Main.NewText("创生领域：生命主宰", 0, 255, 0); } else Main.NewText("领域收起", 200, 200, 200); }
                else { isFullMoonActive = !isFullMoonActive; if (isFullMoonActive) { SoundEngine.PlaySound(SoundID.Item29, Player.position); Main.NewText("满月降临", 255, 50, 50); } else Main.NewText("满月隐去", 200, 200, 200); }
            }
            if (LotMKeybinds.Moon_SummonGate.JustPressed && currentMoonSequence <= 3) { if (summonGateCooldown <= 0 && TryConsumeSpirituality(300)) { Vector2 spawnPos = Player.Center + new Vector2(Player.direction * 50, -50); Projectile.NewProjectile(Player.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SummoningDoorProjectile>(), 0, 0f, Player.whoAmI); summonGateCooldown = 3600; Main.NewText("召唤之门已打开！", 150, 100, 255); } else if (summonGateCooldown > 0) Main.NewText($"召唤冷却: {summonGateCooldown / 60}s", 150, 150, 150); }
            if (LotMKeybinds.Moon_Tame.JustPressed && currentMoonSequence <= 8)
            {
                isTamingActive = !isTamingActive;
                if (isTamingActive)
                {
                    SoundEngine.PlaySound(SoundID.Item4, Player.position);
                    Main.NewText("驯兽模式：开启 (持续消耗灵性，使用鞭子驯服)", 100, 255, 100);
                }
                else
                {
                    Main.NewText("驯兽模式：关闭", 200, 200, 200);
                }
            }
            // 状态自动解除 (如果死掉或序列不对)
            if (currentMoonSequence > 8) isTamingActive = false;

            if (LotMKeybinds.RP_Transformation.JustPressed) { if (currentHunterSequence <= 2) { isCalamityGiant = !isCalamityGiant; if (isCalamityGiant) Main.NewText("灾祸巨人形态", 0, 255, 255); } else if (currentHunterSequence <= 4) { isFireForm = !isFireForm; if (isFireForm) Main.NewText("火焰形态", 255, 100, 0); } }
            if (LotMKeybinds.RP_Flash.JustPressed && currentHunterSequence <= 6)
            {
                if (fireTeleportCooldown <= 0 && TryConsumeSpirituality(100))
                {
                    Vector2 flashPos = Main.MouseWorld;
                    if (Player.Distance(flashPos) < 600f && Collision.CanHit(Player.position, Player.width, Player.height, flashPos, Player.width, Player.height))
                    {
                        SoundEngine.PlaySound(SoundID.Item14, Player.position);
                        for (int i = 0; i < 20; i++) Dust.NewDust(Player.position, Player.width, Player.height, DustID.Torch, 0, 0, 0, default, 2f);
                        Player.Teleport(flashPos, 1);
                        fireTeleportCooldown = 60;
                    }
                }
            }
            if (LotMKeybinds.RP_Bomb.JustPressed && currentHunterSequence <= 7) { if (TryConsumeSpirituality(50)) { int dmg = (int)(100 * hunterMult); Vector2 dir = (Main.MouseWorld - Player.Center).SafeNormalize(Vector2.Zero) * 10f; Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, dir, ModContent.ProjectileType<PyromaniacBomb>(), dmg, 5f, Player.whoAmI); } }
            if (LotMKeybinds.RP_Cloak.JustPressed && currentHunterSequence <= 7) { isFlameCloakActive = !isFlameCloakActive; if (isFlameCloakActive) Main.NewText("火焰披风开启", 255, 100, 0); }
            if (LotMKeybinds.RP_Slash.JustPressed && currentHunterSequence <= 5) { if (!isFireForm && TryConsumeSpirituality(100)) { int dmg = (int)(200 * hunterMult); Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, ModContent.ProjectileType<ReaperSlashProjectile>(), dmg, 10f, Player.whoAmI); } }
            if (LotMKeybinds.RP_Enchant.JustPressed && currentHunterSequence <= 6) { isFireEnchanted = !isFireEnchanted; Main.NewText("武器附魔切换", 255, 100, 0); }
            if (currentHunterSequence <= 7) { if (LotMKeybinds.RP_Skill.Current) { if (TryConsumeSpirituality(0.5f)) { isChargingFireball = true; fireballChargeTimer++; } } if (LotMKeybinds.RP_Skill.JustReleased && isChargingFireball) { int dmg = (int)(100 * hunterMult); Vector2 dir = (Main.MouseWorld - Player.Center).SafeNormalize(Vector2.Zero) * 12f; Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, dir, ModContent.ProjectileType<PyromaniacFireball>(), dmg, 4f, Player.whoAmI); isChargingFireball = false; fireballChargeTimer = 0; } }
            if (LotMKeybinds.RP_Army.JustPressed && currentHunterSequence <= 4) { if (!isFireForm) { isArmyOfOne = !isArmyOfOne; Main.NewText("集众切换", 255, 100, 0); } }
            if (LotMKeybinds.RP_Weather.JustPressed && currentHunterSequence <= 2) { if (TryConsumeSpirituality(200, true)) { int dmg = (int)(500 * hunterMult); Projectile.NewProjectile(Player.GetSource_FromThis(), Main.MouseWorld, Vector2.Zero, ModContent.ProjectileType<WeatherStrikeLightning>(), dmg, 5f, Player.whoAmI); } }
            if (LotMKeybinds.RP_Glacier.JustPressed && currentHunterSequence <= 2) { if (glacierCooldown <= 0 && TryConsumeSpirituality(1000)) { Main.NewText("冰河世纪！", 0, 255, 255); glacierCooldown = 1800; foreach (NPC npc in Main.ActiveNPCs) { if (!npc.friendly && !npc.dontTakeDamage && npc.Distance(Player.Center) < 2000f) { npc.AddBuff(BuffID.Frozen, 300); npc.AddBuff(BuffID.Frostburn2, 600); npc.AddBuff(BuffID.Slow, 600); if (!npc.boss) npc.velocity = Vector2.Zero; } } } }
            if (LotMKeybinds.Giant_Mercury.JustPressed && currentSequence <= 3) { if (!isGuardianStance) { if (!isMercuryForm) { if (TryConsumeSpirituality(500)) isMercuryForm = true; } else isMercuryForm = false; } }
            if (LotMKeybinds.Giant_Armor.JustPressed && currentSequence <= 6) { if (!isMercuryForm && !dawnArmorBroken) dawnArmorActive = !dawnArmorActive; }
            if (currentSequence <= 5 && LotMKeybinds.Giant_Guardian.Current && !isMercuryForm)
            {
                if (TryConsumeSpirituality(10.0f))
                {
                    isGuardianStance = true;
                    if (Player.ownedProjectileCounts[ModContent.ProjectileType<GuardianShieldProjectile>()] < 1)
                    {
                        Projectile.NewProjectile(
                            Player.GetSource_FromThis(),
                            Player.Center,
                            Vector2.Zero,
                            ModContent.ProjectileType<GuardianShieldProjectile>(),
                            0,
                            0,
                            Player.whoAmI
                        );
                    }
                }
                else
                {
                    isGuardianStance = false;
                }
            }
            else
            {
                isGuardianStance = false;
            }

            bool vKeyJustPressed = LotMKeybinds.Fool_SpiritForm.JustPressed || LotMKeybinds.Fool_Miracle.JustPressed || LotMKeybinds.Fool_Faceless.JustPressed;
            bool vKeyCurrent = LotMKeybinds.Fool_SpiritForm.Current || LotMKeybinds.Fool_Miracle.Current || LotMKeybinds.Fool_Faceless.Current;
            bool shiftPressed = Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift);

            // 【情况 A】序列 1 诡秘侍者 (Shift+V 愿望, V 灵体化)
            if (currentFoolSequence <= 1)
            {
                // 1. 愿望逻辑 (Shift + V)
                bool isChoosing = (currentFoolSequence <= 1 && shiftPressed && vKeyCurrent) || (currentFoolSequence == 2 && vKeyCurrent);
                bool isJustPressed = (currentFoolSequence <= 1 && shiftPressed && vKeyJustPressed) || (currentFoolSequence == 2 && vKeyJustPressed);

                if (isChoosing)
                {
                    if (isJustPressed)
                    {
                        selectedWish++;
                        if (selectedWish > 7) selectedWish = 0; // 扩展到 0-7

                        string wishName = "";
                        switch (selectedWish)
                        {
                            case 0: wishName = "生命复苏 (治疗/解控)"; break;
                            case 1: wishName = "毁灭天灾 (雷暴伤害)"; break;
                            case 2: wishName = "空间传送 (全图位移)"; break;
                            case 3: wishName = "昼夜更替 (改变时间)"; break;
                            case 4: wishName = "呼风唤雨 (切换天气)"; break; // 新增
                            case 5: wishName = "绯红之月 (调整月相)"; break; // 新增
                            case 6: wishName = "历史投影 (召唤建筑)"; break; // 新增
                            case 7: wishName = "欺诈外貌 (变形他人)"; break; // 新增
                        }

                        Main.NewText($"[奇迹愿望]: {wishName}", 255, 215, 0);
                        wishCastTimer = 0; // 重置蓄力
                    }

                    wishCastTimer++;
                    // 蓄力特效
                    if (wishCastTimer % 10 == 0)
                        Dust.NewDust(Player.position, Player.width, Player.height, DustID.Enchanted_Gold, 0, -2);

                    // 蓄力 1 秒后释放
                    if (wishCastTimer >= 60)
                    {
                        CastMiracleWish();
                        wishCastTimer = 0;
                    }
                }
                // 2. 灵体化逻辑 (单按 V)
                else if (vKeyJustPressed && !shiftPressed)
                {
                    isSpiritForm = !isSpiritForm;
                    if (isSpiritForm)
                    {
                        SoundEngine.PlaySound(SoundID.Item8, Player.position);
                        Main.NewText("灵体化：开启 (物理免疫/穿墙/高耗蓝)", 200, 200, 255);
                    }
                    else Main.NewText("回归血肉之躯", 150, 150, 150);

                    wishCastTimer = 0;
                }
                else if (!vKeyCurrent) wishCastTimer = 0;
            }
            // 【情况 B】序列 2 奇迹师 (直接 V 愿望)
            else if (currentFoolSequence <= 2)
            {
                if (vKeyJustPressed)
                {
                    selectedWish++; if (selectedWish > 3) selectedWish = 0;
                    string n = selectedWish == 0 ? "生命复苏" : selectedWish == 1 ? "毁灭天灾" : selectedWish == 2 ? "空间传送" : "昼夜更替";
                    Main.NewText($"[奇迹愿望]: {n} (长按V实现)", 255, 215, 0);
                    wishCastTimer = 0;
                }
                if (vKeyCurrent)
                {
                    wishCastTimer++;
                    if (wishCastTimer >= 60) { CastMiracleWish(); wishCastTimer = 0; }
                }
                else wishCastTimer = 0;
            }
            // 【情况 C】序列 6 无面人 (V 伪装)
            else if (currentFoolSequence <= 6)
            {
                if (vKeyJustPressed)
                {
                    isFacelessActive = !isFacelessActive;
                    if (isFacelessActive) { SoundEngine.PlaySound(SoundID.Item8, Player.position); Main.NewText("无面伪装：开启", 200, 200, 200); }
                    else Main.NewText("无面伪装：关闭", 150, 150, 150);
                }
            }

            bool gKeyJustPressed = LotMKeybinds.Fool_Grafting.JustPressed || LotMKeybinds.Fool_Distort.JustPressed;

            // --- G键逻辑 ---
            if (currentFoolSequence <= 1) // 序列1
            {
                // Shift + G : 命运干扰光环
                if (shiftPressed && gKeyJustPressed)
                {
                    fateDisturbanceActive = !fateDisturbanceActive;
                    Main.NewText(fateDisturbanceActive ? "命运干扰: 开启" : "命运干扰: 关闭", 150, 100, 255);
                }
                // 单按 G : 嫁接切换
                else if (gKeyJustPressed && !shiftPressed)
                {
                    graftingMode++; if (graftingMode > 2) graftingMode = 0;
                    string m = graftingMode == 0 ? "关闭" : (graftingMode == 1 ? "空间嫁接(反弹)" : "概念嫁接(必杀)");
                    Main.NewText($"嫁接模式: {m}", 100, 100, 255);
                    SoundEngine.PlaySound(SoundID.Item4, Player.position);
                }
            }
            else if (currentFoolSequence <= 2) // 序列2
            {
                if (gKeyJustPressed)
                {
                    fateDisturbanceActive = !fateDisturbanceActive;
                    Main.NewText(fateDisturbanceActive ? "命运干扰: 开启" : "命运干扰: 关闭", 150, 100, 255);
                }
            }
            else if (currentFoolSequence <= 6) // 序列6
            {
                if (gKeyJustPressed && distortCooldown <= 0 && TryConsumeSpirituality(30))
                {
                    SoundEngine.PlaySound(SoundID.Item18, Player.position);
                    distortCooldown = 600;
                    foreach (NPC npc in Main.ActiveNPCs) if (!npc.friendly && !npc.dontTakeDamage && npc.Distance(Player.Center) < 600f) { npc.AddBuff(BuffID.Confused, 300); npc.AddBuff(BuffID.Midas, 600); npc.damage = (int)(npc.damage * 0.8f); }
                    Main.NewText("命运已被短暂干扰。", 255, 215, 0);
                }
            }

            // --- Y键逻辑 (历史投影) ---
            if (currentFoolSequence <= 3)
            {
                // Shift + Y : 历史场景 (序列2特权)
                if (currentFoolSequence <= 2 && shiftPressed && LotMKeybinds.Fool_History.JustPressed)
                {
                    bool exists = false;
                    for (int i = 0; i < Main.maxProjectiles; i++) { if (Main.projectile[i].active && Main.projectile[i].owner == Player.whoAmI && Main.projectile[i].type == ModContent.ProjectileType<Projectiles.HistoricalSceneProjectile>()) { Main.projectile[i].Kill(); exists = true; Main.NewText("场景消散。", 150, 150, 150); break; } }
                    if (!exists && TryConsumeSpirituality(1000))
                    {
                        Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.HistoricalSceneProjectile>(), 0, 0, Player.whoAmI);
                        SoundEngine.PlaySound(SoundID.Item4, Player.position);
                        Main.NewText("历史场景降临...", 200, 200, 255);
                    }
                }
                // 单按 Y : Boss 投影
                else if (LotMKeybinds.Fool_History.JustPressed && !shiftPressed)
                {
                    int currentProjections = 0;
                    for (int i = 0; i < Main.maxProjectiles; i++) { if (Main.projectile[i].active && Main.projectile[i].owner == Player.whoAmI && Main.projectile[i].type == ModContent.ProjectileType<Projectiles.HistoricalBossProjectile>()) currentProjections++; }

                    int maxProj = 1;
                    if (currentProjections < maxProj)
                    {
                        if (TryConsumeSpirituality(500))
                        {
                            System.Collections.Generic.List<int> bossIDs = new System.Collections.Generic.List<int>();
                            System.Collections.Generic.List<int> bossPowers = new System.Collections.Generic.List<int>();
                            void Add(int id, int p) { bossIDs.Add(id); bossPowers.Add(p); }

                            if (NPC.downedSlimeKing) Add(NPCID.KingSlime, 40);
                            if (NPC.downedBoss1) Add(NPCID.EyeofCthulhu, 50);
                            if (NPC.downedBoss2) Add(NPCID.EaterofWorldsHead, 60);
                            if (NPC.downedBoss3) Add(NPCID.SkeletronHead, 80);
                            if (Main.hardMode) Add(NPCID.WallofFlesh, 100);
                            if (NPC.downedMechBossAny) Add(NPCID.TheDestroyer, 140);
                            if (NPC.downedPlantBoss) Add(NPCID.Plantera, 180);
                            if (NPC.downedMoonlord) Add(NPCID.MoonLordHead, 350);
                            if (bossIDs.Count == 0) Add(NPCID.BlueSlime, 20);

                            int idx = Main.rand.Next(bossIDs.Count);
                            int dmg = (int)(bossPowers[idx] * 5 * foolMult);
                            if (currentFoolSequence <= 2) dmg *= 2;

                            Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.HistoricalBossProjectile>(), dmg, 4f, Player.whoAmI, bossIDs[idx]);
                            Main.NewText("历史投影降临!", 200, 200, 200);
                            SoundEngine.PlaySound(SoundID.Item113, Player.position);
                        }
                    }
                    else Main.NewText("投影数量已达上限。", 150, 150, 150);
                }
            }

            // U: 昨日重现 (序列3)
            if (LotMKeybinds.Fool_Borrow.JustPressed && currentFoolSequence <= 3)
            {
                if (!Player.HasBuff(ModContent.BuffType<Buffs.YesterdayBuff>()))
                {
                    if (borrowUsesDaily < 10 && TryConsumeSpirituality(100))
                    {
                        borrowUsesDaily++; Player.AddBuff(ModContent.BuffType<Buffs.YesterdayBuff>(), 18000);
                        Player.statLife = Player.statLifeMax2; Player.HealEffect(Player.statLifeMax2);
                        Main.NewText($"昨日重现! (剩余{10 - borrowUsesDaily}次)", 0, 255, 255);
                    }
                    else Main.NewText("次数耗尽。", 150, 150, 150);
                }
                else Main.NewText("力量正在涌动...", 150, 150, 150);
            }

            // T: 秘偶互换 (序列4)
            if (LotMKeybinds.Fool_Swap.JustPressed && currentFoolSequence <= 4)
            {
                if (swapCooldown <= 0 && TryConsumeSpirituality(50))
                {
                    Projectile closest = null; float minDst = 9999f;

                    // 遍历所有弹幕
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        Projectile p = Main.projectile[i];
                        // 寻找自己的 秘偶弹幕 或 历史投影
                        if (p.active && p.owner == Player.whoAmI &&
                           (p.type == ModContent.ProjectileType<MarionetteMinion>() ||
                            p.type == ModContent.ProjectileType<Projectiles.HistoricalBossProjectile>()))
                        {
                            float d = Vector2.Distance(Player.Center, p.Center);
                            if (d < minDst) { minDst = d; closest = p; }
                        }
                    }

                    if (closest != null)
                    {
                        Vector2 tmp = Player.Center;
                        Player.Teleport(closest.Center, 1);
                        closest.Center = tmp;

                        swapCooldown = 60;
                        SoundEngine.PlaySound(SoundID.Item6, Player.position);
                        Main.NewText("位置互换", 200, 200, 255);
                    }
                    else Main.NewText("无秘偶可换。", 150, 150, 150);
                }
            }

            // R: 控灵 (序列4)
            if (LotMKeybinds.Fool_Control.JustPressed && currentFoolSequence <= 4)
            {
                if (spiritControlCooldown <= 0 && TryConsumeSpirituality(100))
                {
                    spiritControlCooldown = 1200; SoundEngine.PlaySound(SoundID.Item103, Player.position);
                    foreach (NPC n in Main.ActiveNPCs) if (!n.friendly && !n.dontTakeDamage && n.Distance(Player.Center) < 1000f) { n.AddBuff(BuffID.Stoned, 180); n.AddBuff(BuffID.Confused, 300); }
                    Main.NewText("灵体震慑!", 148, 0, 211);
                }
                else Main.NewText("冷却中...", 150, 150, 150);
            }

            // Z: 灵体之线 (序列5)
            if (LotMKeybinds.Fool_Threads.Current && currentFoolSequence <= 5)
            {
                if (spiritThreadTargetIndex == -1)
                {
                    float minD = 400f; int idx = -1;
                    foreach (NPC n in Main.ActiveNPCs)
                    {
                        // 修正：townNPC判定需在序列2以上才生效
                        bool valid = n.CanBeChasedBy() || (n.townNPC && currentFoolSequence <= 2);
                        if (valid && n.Distance(Main.MouseWorld) < minD) { minD = n.Distance(Main.MouseWorld); idx = n.whoAmI; }
                    }
                    if (idx != -1) { spiritThreadTargetIndex = idx; Main.NewText("抓住灵体之线...", 180, 80, 255); }
                }
                if (spiritThreadTargetIndex != -1 && TryConsumeSpirituality(1.0f))
                {
                    NPC t = Main.npc[spiritThreadTargetIndex];
                    if (!t.active || t.Distance(Player.Center) > 1000f) { spiritThreadTargetIndex = -1; spiritThreadTimer = 0; }
                    else
                    {
                        t.AddBuff(ModContent.BuffType<Buffs.SpiritControlDebuff>(), 2);
                        if (Main.rand.NextBool(2)) Dust.NewDust(Vector2.Lerp(Player.Center, t.Center, Main.rand.NextFloat()), 0, 0, DustID.PurpleCrystalShard);
                        spiritThreadTimer += (currentFoolSequence <= 4 ? 5 : 1);
                        if (spiritThreadTimer >= CONTROL_TIME_REQUIRED)
                        {
                            if (t.townNPC)
                            {
                                // 【核心修复】检查是否已经有Buff，如果没有，则增加进度
                                // 这样防止对着同一个NPC重复刷进度
                                if (!t.HasBuff(ModContent.BuffType<Buffs.MarionetteTownNPCBuff>()))
                                {
                                    if (currentFoolSequence == 2 && attendantRitualProgress < ATTENDANT_RITUAL_TARGET)
                                    {
                                        attendantRitualProgress++;
                                        if (attendantRitualProgress >= ATTENDANT_RITUAL_TARGET)
                                        {
                                            attendantRitualComplete = true;

                                            Main.NewText("仪式完成：诡秘的侍者正在注视着你... (10/10)", 220, 20, 60);
                                            SoundEngine.PlaySound(SoundID.Roar, Player.position);
                                        }
                                        else
                                        {
                                            Main.NewText($"仪式进度: {attendantRitualProgress}/{ATTENDANT_RITUAL_TARGET}", 150, 100, 255);
                                        }
                                    }
                                }

                                t.AddBuff(ModContent.BuffType<Buffs.MarionetteTownNPCBuff>(), 36000);
                                Main.NewText($"{t.FullName} 已转化为秘偶!", 148, 0, 211);
                                t.life = t.lifeMax;
                            }
                            else
                            {
                                // === 【还原】直接转化逻辑 ===
                                int dmg = 999999;
                                if (t.boss || t.realLife != -1 || Terraria.ID.NPCID.Sets.ShouldBeCountedAsBoss[t.type])
                                {
                                    dmg = 2000; // 对 Boss 或其肢体只造成 2000 伤害
                                }

                                Player.ApplyDamageToNPC(t, dmg, 0, 0, false);

                                if (!t.boss)
                                {
                                    // 检查数量限制
                                    int currentCount = Player.ownedProjectileCounts[ModContent.ProjectileType<MarionetteMinion>()];
                                    int maxCount = (currentFoolSequence <= 4) ? 10 : 3;

                                    if (currentCount < maxCount)
                                    {
                                        // 直接在怪物位置生成秘偶弹幕
                                        Projectile.NewProjectile(Player.GetSource_FromThis(), t.Center, Vector2.Zero, ModContent.ProjectileType<MarionetteMinion>(), (int)(100 * foolMult), 2f, Player.whoAmI);
                                        Main.NewText("转化成功!", 200, 100, 255);
                                    }
                                    else
                                    {
                                        Main.NewText("秘偶数量已达上限，直接处决。", 150, 150, 150);
                                    }
                                }
                                else
                                {
                                    Main.NewText("目标位格过高，只能造成伤害!", 255, 100, 100);
                                }
                            }
                            spiritThreadTargetIndex = -1; spiritThreadTimer = 0;
                        }
                    }
                }
            }
            else { spiritThreadTargetIndex = -1; spiritThreadTimer = 0; }

            // C: 灵视 (序列9)
            if (LotMKeybinds.Fool_SpiritVision.JustPressed && currentFoolSequence <= 9)
            {
                isSpiritVisionActive = !isSpiritVisionActive;
                Main.NewText(isSpiritVisionActive ? "灵视开启" : "灵视关闭", 200, 200, 255);
            }
            // F: 火焰跳跃 (序列7) - 增强版：万火皆可跃
            if (LotMKeybinds.Fool_FlameJump.JustPressed && currentFoolSequence <= 7)
            {
                if (flameJumpCooldown <= 0)
                {
                    bool hasCardUpgrade = isFoolCardEquipped && currentFoolSequence < 10;

                    if (hasCardUpgrade)
                    {
                        if (TryConsumeSpirituality(50)) // 稍微降低一点消耗奖励玩家
                        {
                            // 【修复】改名为 upgradePos，防止与外部变量冲突
                            Vector2 upgradePos = Main.MouseWorld;
                            Player.Teleport(upgradePos, 1); // Style 1 是普通的传送

                            // 特效
                            SoundEngine.PlaySound(SoundID.Item115, Player.position); // 比较迷幻的声音
                            for (int i = 0; i < 30; i++)
                            {
                                Dust.NewDust(Player.position, Player.width, Player.height, DustID.DungeonSpirit, 0, 0, 0, default, 2f);
                            }

                            flameJumpCooldown = 30; // 冷却大幅降低！
                            Main.NewText("愚者权柄：空间跨越", 148, 0, 211);
                        }
                        else
                        {
                            Main.NewText("灵性不足 (需50点)", 255, 50, 50);
                        }
                        return; // 升级版执行完毕后直接返回，不走下面的判定
                    }


                    // ===================================================
                    // 2. 普通版：寻找附近的火焰进行跳跃
                    // ===================================================

                    // 【修复】改名为 jumpTargetPos，防止与外部变量冲突
                    Vector2 jumpTargetPos = Vector2.Zero;
                    bool foundFire = false;
                    float searchRange = 100f; // 鼠标周围 100 像素范围

                    // A. 检测物块 (Tile)
                    Point mouseTile = Main.MouseWorld.ToTileCoordinates();
                    int tileRange = 6;

                    for (int x = mouseTile.X - tileRange; x <= mouseTile.X + tileRange; x++)
                    {
                        for (int y = mouseTile.Y - tileRange; y <= mouseTile.Y + tileRange; y++)
                        {
                            if (!WorldGen.InWorld(x, y)) continue;
                            Tile tile = Main.tile[x, y];
                            if (tile.HasTile && (
                                tile.TileType == TileID.Torches || tile.TileType == TileID.Campfire ||
                                tile.TileType == TileID.Candles || tile.TileType == TileID.PlatinumCandle ||
                                tile.TileType == TileID.LivingFire || tile.TileType == TileID.LivingCursedFire ||
                                tile.TileType == TileID.LivingDemonFire || tile.TileType == TileID.LivingFrostFire ||
                                tile.TileType == TileID.Fireplace || tile.TileType == TileID.Chandeliers ||
                                tile.TileType == TileID.Candelabras || tile.TileType == TileID.HangingLanterns ||
                                tile.TileType == TileID.ChineseLanterns || tile.TileType == TileID.Jackolanterns ||
                                tile.TileType == TileID.SkullLanterns || tile.TileType == TileID.Lamps ||
                                tile.TileType == TileID.Furnaces || tile.TileType == TileID.Hellforge ||
                                tile.TileType == TileID.AdamantiteForge || tile.TileType == TileID.GlassKiln
                                ))
                            {
                                foundFire = true;
                                if (tile.TileType == TileID.Chandeliers || tile.TileType == TileID.HangingLanterns || tile.TileType == TileID.ChineseLanterns)
                                    jumpTargetPos = new Vector2(x * 16 + 8 - Player.width / 2, y * 16 + 16);
                                else if (Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
                                    jumpTargetPos = new Vector2(x * 16 + 8 - Player.width / 2, y * 16 - Player.height);
                                else
                                    jumpTargetPos = new Vector2(x * 16 + 8 - Player.width / 2, y * 16 - Player.height / 2);
                                break;
                            }
                        }
                        if (foundFire) break;
                    }

                    // B. 检测弹幕 (Projectile)
                    if (!foundFire)
                    {
                        foreach (Projectile p in Main.projectile)
                        {
                            if (p.active && p.Distance(Main.MouseWorld) < searchRange)
                            {
                                string name = p.Name.ToLower();
                                bool isFireProjectile =
                                    name.Contains("fire") || name.Contains("flame") || name.Contains("torch") ||
                                    name.Contains("magma") || name.Contains("solar") || name.Contains("napalm") ||
                                    p.type == ProjectileID.MolotovFire || p.type == ProjectileID.GreekFire1 ||
                                    p.type == ProjectileID.GreekFire2 || p.type == ProjectileID.GreekFire3 ||
                                    p.type == ModContent.ProjectileType<PyromaniacFireball>() ||
                                    p.type == ModContent.ProjectileType<PyromaniacBomb>();

                                if (isFireProjectile)
                                {
                                    foundFire = true;
                                    jumpTargetPos = p.Center - new Vector2(0, Player.height / 2);
                                    break;
                                }
                            }
                        }
                    }

                    // C. 检测 NPC
                    if (!foundFire)
                    {
                        foreach (NPC npc in Main.npc)
                        {
                            if (npc.active && npc.Distance(Main.MouseWorld) < searchRange)
                            {
                                bool isBurning =
                                    npc.HasBuff(BuffID.OnFire) || npc.HasBuff(BuffID.OnFire3) ||
                                    npc.HasBuff(BuffID.CursedInferno) || npc.HasBuff(BuffID.ShadowFlame) ||
                                    npc.HasBuff(BuffID.Frostburn) || npc.HasBuff(BuffID.Frostburn2);

                                bool isFireMob =
                                    npc.type == NPCID.Hellbat || npc.type == NPCID.Lavabat ||
                                    npc.type == NPCID.LavaSlime || npc.type == NPCID.MeteorHead ||
                                    npc.type == NPCID.FireImp || npc.type == NPCID.BurningSphere;

                                if (isBurning || isFireMob)
                                {
                                    foundFire = true;
                                    jumpTargetPos = npc.Center - new Vector2(0, Player.height);
                                    break;
                                }
                            }
                        }
                    }

                    // D. 执行传送
                    if (foundFire)
                    {
                        if (TryConsumeSpirituality(100))
                        {
                            Vector2 oldPos = Player.Center;
                            // 使用新的变量名 jumpTargetPos
                            Player.Teleport(jumpTargetPos, 1);

                            SoundEngine.PlaySound(SoundID.Item45, Player.position);
                            SoundEngine.PlaySound(SoundID.Item20, Player.position);

                            for (int i = 0; i < 20; i++) Dust.NewDust(oldPos, Player.width, Player.height, DustID.Torch, 0, 0, 0, default, 2f);
                            for (int i = 0; i < 20; i++) Dust.NewDust(Player.position, Player.width, Player.height, DustID.Torch, 0, 0, 0, default, 2f);

                            flameJumpCooldown = 60;
                        }
                        else
                        {
                            Main.NewText("灵性不足 (需100点)", 255, 50, 50);
                        }
                    }
                    else
                    {
                        CombatText.NewText(Player.getRect(), Color.Gray, "未感知到火焰...", true);
                    }
                }
            }

            // J: 占卜 (序列9)
            if (LotMKeybinds.Fool_Divination.JustPressed && (currentFoolSequence <= 9 || currentWheelSequence <= 8))
            {
                if (divinationCooldown <= 0 && TryConsumeSpirituality(10))
                {
                    divinationCooldown = 3600; // 冷却60秒

                    int resultIndex = Main.rand.Next(6);
                    int buffDuration = 3600; // 持续60秒

                    string resultText = "";
                    Color textColor = Color.White;

                    switch (resultIndex)
                    {
                        case 0: // 厄运
                            resultText = "厄运：危险正在靠近...";
                            // 【修复】BuffID.DangerSense -> BuffID.Dangersense (注意小写s)
                            Player.AddBuff(BuffID.Dangersense, buffDuration);
                            Player.AddBuff(BuffID.Battle, buffDuration);
                            textColor = Color.Red;
                            break;

                        case 1: // 财富
                            resultText = "财富：金光闪烁的前路。";
                            Player.AddBuff(BuffID.Spelunker, buffDuration);
                            Player.AddBuff(BuffID.Midas, buffDuration);
                            textColor = Color.Gold;
                            break;

                        case 2: // 启示
                            resultText = "启示：迷雾消散。";
                            Player.AddBuff(BuffID.Hunter, buffDuration);
                            Player.AddBuff(BuffID.NightOwl, buffDuration);
                            textColor = Color.Cyan;
                            break;

                        case 3: // 战斗
                            resultText = "战斗：你的血液开始沸腾！";
                            Player.AddBuff(BuffID.Wrath, buffDuration);
                            textColor = Color.OrangeRed;
                            break;

                        case 4: // 生存
                            resultText = "生存：活下去的希望。";
                            Player.AddBuff(BuffID.Ironskin, buffDuration);
                            Player.AddBuff(BuffID.Regeneration, buffDuration);
                            textColor = Color.LightGreen;
                            break;

                        case 5: // 眷顾
                            resultText = "眷顾：女神在注视着你。";
                            Player.AddBuff(BuffID.Lucky, buffDuration);
                            spiritualityCurrent += 30;
                            if (spiritualityCurrent > spiritualityMax) spiritualityCurrent = spiritualityMax;
                            textColor = Color.Pink;
                            break;
                    }

                    Main.NewText($"占卜结果: {resultText}", textColor);
                }
                else if (divinationCooldown > 0)
                {
                    Main.NewText($"灵性直觉正在平复... ({divinationCooldown / 60}s)", 150, 150, 150);
                }
                else
                {
                    Main.NewText("灵性不足。", 255, 50, 50);
                }
            }
            if (currentWheelSequence <= 6 && LotMKeybinds.Wheel_PsychicStorm.JustPressed && psychicStormCooldown <= 0)
            {
                CastPsychicStorm();
            }

            // 2. 【新增】命运赐福 / 群体赐福 (按 B 键)
            if (currentWheelSequence <= 4 && LotMKeybinds.Wheel_Blessing.JustPressed && fateBlessingCooldown <= 0)
            {
                // 赐福代价：极大地透支自己的运气 (持续1分钟的倒霉)
                Player.AddBuff(ModContent.BuffType<ExtremeBadLuckBuff>(), 3600);
                fateBlessingCooldown = 7200; // 2分钟CD

                // 赐福自己
                Player.AddBuff(BuffID.Lucky, 3600);
                Player.AddBuff(BuffID.RapidHealing, 3600);
                Player.AddBuff(BuffID.Lifeforce, 3600);
                Player.AddBuff(BuffID.Endurance, 3600);
                Player.AddBuff(BuffID.Rage, 3600);
                Player.AddBuff(BuffID.Wrath, 3600);

                // 赐福周围队友
                foreach (Player p in Main.player)
                {
                    if (p.active && p.whoAmI != Player.whoAmI && p.Distance(Player.Center) < 1000f)
                    {
                        p.AddBuff(BuffID.Lucky, 3600);
                        p.AddBuff(BuffID.Lifeforce, 3600);
                        p.AddBuff(BuffID.RapidHealing, 3600);
                        CombatText.NewText(p.getRect(), Color.Gold, "命运赐福!", true);
                    }
                }

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29, Player.position);
                for (int i = 0; i < 50; i++) Dust.NewDust(Player.position, Player.width, Player.height, DustID.GoldCoin, 0, -3, 0, default, 2f);
                Main.NewText("你透支了命运，降下了群体赐福！", 255, 215, 0);
            }
            // 2. 厄运领域 / 灾祸光环 开关 (按键触发)
            if (currentWheelSequence <= 6 && LotMKeybinds.Wheel_Domain.JustPressed)
            {
                isMisfortuneDomainActive = !isMisfortuneDomainActive;
                if (isMisfortuneDomainActive)
                {
                    SoundEngine.PlaySound(SoundID.Item119, Player.position);
                    if (currentWheelSequence <= 4)
                        Main.NewText("厄运领域：展开 (范围内敌人将遭受厄运吞噬)", 148, 0, 211);
                    else
                        Main.NewText("灾祸光环：开启 (自动引来天灾轰击敌人)", 255, 69, 0);
                }
                else
                {
                    Main.NewText("领域已收起。", 150, 150, 150);
                }
            }

            // 3. 命运主动技能 - 独立按键, 独立CD
            // - M键: 命运骰子(序列3+) - 200灵性 - 30s CD
            // - K键: 福祸之福(序列2+)  - 300灵性 - 30s CD
            // - L键: 福祸之祸(序列2+)  - 300灵性 - 30s CD
            // - U键: 命运启示(序列2+)  - 500灵性 - 60s CD

            // [先知·福祸之福] K键: 给60秒强力10种Buff + 治愈 + 灵性恢复
            if (currentWheelSequence <= 2 && LotMKeybinds.Wheel_WordsOfFortune.JustPressed)
            {
                if (wordsOfFortuneCooldown > 0)
                {
                    Main.NewText($"福祸之福冷却中... ({wordsOfFortuneCooldown / 60}秒)", 255, 100, 100);
                }
                else if (!TryConsumeSpirituality(300))
                {
                    Main.NewText("灵性不足 (福祸之福需 300 点)", 255, 50, 50);
                }
                else
                {
                    wordsOfFortuneCooldown = FORTUNE_CD_MAX;
                    int buffed = 0;

                    // 给4000像素范围内所有友军施加10种Buff(60秒)
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player p = Main.player[i];
                        if (p.active && !p.dead && p.Distance(Player.Center) < 4000f)
                        {
                            int dur = 3600; // 60秒
                            p.AddBuff(BuffID.Heartreach, dur);
                            p.AddBuff(BuffID.WellFed3, dur);
                            p.AddBuff(BuffID.Lifeforce, dur);
                            p.AddBuff(BuffID.Endurance, dur);
                            p.AddBuff(BuffID.AmmoReservation, dur);
                            p.AddBuff(BuffID.Rage, dur);
                            p.AddBuff(BuffID.Wrath, dur);
                            p.AddBuff(BuffID.Ironskin, dur);
                            p.AddBuff(BuffID.Regeneration, dur);
                            p.AddBuff(BuffID.Panic, 600); // 短10秒急速

                            // 即时治愈到满
                            int healDelta = p.statLifeMax2 - p.statLife;
                            if (healDelta > 0)
                            {
                                p.statLife = p.statLifeMax2;
                                p.HealEffect(healDelta, true);
                            }
                            // 灵性补满
                            var pmp = p.GetModPlayer<LotMPlayer>();
                            pmp.spiritualityCurrent = pmp.spiritualityMax;

                            // 每个友军周围一圈金色光环
                            for (int k = 0; k < 80; k++)
                            {
                                float angle = (k / 80f) * MathHelper.TwoPi;
                                Vector2 ringPos = p.Center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 60f;
                                Dust d = Dust.NewDustPerfect(ringPos, DustID.GoldCoin,
                                    new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 4f, 0, default, 2f);
                                d.noGravity = true;
                            }
                            // 金币雨 (从天而降)
                            for (int k = 0; k < 25; k++)
                            {
                                Vector2 rainStart = p.Center + new Vector2(Main.rand.NextFloat(-200, 200), -300);
                                Dust d = Dust.NewDustPerfect(rainStart, DustID.GoldCoin,
                                    new Vector2(0, Main.rand.NextFloat(5, 9)), 0, Color.Gold, 1.8f);
                                d.noGravity = false;
                            }
                            buffed++;
                        }
                    }

                    // 释放者中心: 巨大金色法阵爆发
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        for (int ring = 0; ring < 3; ring++) // 3 圈光环
                        {
                            float radius = 120 + ring * 80;
                            int count = 60 + ring * 30;
                            for (int k = 0; k < count; k++)
                            {
                                float angle = (k / (float)count) * MathHelper.TwoPi;
                                Vector2 pos = Player.Center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
                                Dust d = Dust.NewDustPerfect(pos, DustID.YellowTorch,
                                    Vector2.Zero, 0, default, 2.5f);
                                d.noGravity = true;
                                d.fadeIn = 1.5f;
                            }
                        }
                        // 中心垂直光柱
                        for (int k = 0; k < 100; k++)
                        {
                            Vector2 pos = Player.Center + new Vector2(Main.rand.NextFloat(-15, 15), Main.rand.NextFloat(-400, 0));
                            Dust d = Dust.NewDustPerfect(pos, DustID.GoldCoin,
                                new Vector2(0, -Main.rand.NextFloat(3, 7)), 0, default, 2f);
                            d.noGravity = true;
                        }
                    }

                    Main.NewText($"【福祸之言·福】 你与周围 {buffed} 名友军被命运彻底眷顾,60秒强力增益,生命与灵性涌泉而出。", 255, 215, 100);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item123, Player.position);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29, Player.position);
                }
            }

            // [先知·福祸之祸] L键: 给60秒强力8种Debuff + 当前血25%真伤 + Boss削10%血 + 厄运闪电
            if (currentWheelSequence <= 2 && LotMKeybinds.Wheel_WordsOfMisfortune.JustPressed)
            {
                if (wordsOfMisfortuneCooldown > 0)
                {
                    Main.NewText($"福祸之祸冷却中... ({wordsOfMisfortuneCooldown / 60}秒)", 255, 100, 100);
                }
                else if (!TryConsumeSpirituality(300))
                {
                    Main.NewText("灵性不足 (福祸之祸需 300 点)", 255, 50, 50);
                }
                else
                {
                    wordsOfMisfortuneCooldown = MISFORTUNE_CD_MAX;
                    int affected = 0;

                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!npc.friendly && !npc.dontTakeDamage && npc.Distance(Player.Center) < 4000f)
                        {
                            int dur = 3600; // 60秒
                            // 8种Debuff
                            if (!npc.HasBuff(BuffID.BetsysCurse)) npc.AddBuff(BuffID.BetsysCurse, dur);
                            if (!npc.HasBuff(BuffID.OnFire3)) npc.AddBuff(BuffID.OnFire3, dur);
                            if (!npc.HasBuff(BuffID.Weak)) npc.AddBuff(BuffID.Weak, dur);
                            if (!npc.HasBuff(BuffID.BrokenArmor)) npc.AddBuff(BuffID.BrokenArmor, dur);
                            if (!npc.HasBuff(BuffID.Slow)) npc.AddBuff(BuffID.Slow, dur);
                            if (!npc.HasBuff(BuffID.Confused)) npc.AddBuff(BuffID.Confused, dur);
                            if (!npc.HasBuff(BuffID.Ichor)) npc.AddBuff(BuffID.Ichor, dur);
                            if (!npc.HasBuff(BuffID.Daybreak)) npc.AddBuff(BuffID.Daybreak, dur);

                            // 即时真伤: 非Boss 25% 当前血, Boss 10% 当前血
                            int instDmg;
                            if (npc.boss) instDmg = (int)(npc.life * 0.10f);
                            else instDmg = (int)(npc.life * 0.25f);
                            if (instDmg > 0)
                            {
                                npc.SimpleStrikeNPC(instDmg, 0, false, 0, DamageClass.Default, true);
                            }
                            npc.netUpdate = true;
                            affected++;
                            CombatText.NewText(npc.getRect(), new Color(180, 60, 220), "厄运降临!", true);

                            // 华丽特效: 每个敌人头顶降下紫色厄运闪电(粒子柱)
                            Vector2 lightTop = npc.Center + new Vector2(0, -500);
                            // 闪电柱: 从天而降的紫色粒子
                            for (int k = 0; k < 35; k++)
                            {
                                float t = k / 35f;
                                Vector2 pos = Vector2.Lerp(lightTop, npc.Center, t)
                                    + new Vector2(Main.rand.NextFloat(-8, 8), 0);
                                Dust d = Dust.NewDustPerfect(pos, DustID.PurpleCrystalShard,
                                    Vector2.Zero, 0, default, 2f);
                                d.noGravity = true;
                                d.fadeIn = 1.2f;
                            }
                            // 着地点: 紫黑色爆裂环
                            for (int k = 0; k < 40; k++)
                            {
                                float angle = (k / 40f) * MathHelper.TwoPi;
                                Vector2 vel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 6f;
                                Dust d = Dust.NewDustPerfect(npc.Center, DustID.ShadowbeamStaff,
                                    vel, 0, default, 1.8f);
                                d.noGravity = true;
                            }
                            // 阴影乌鸦感: 黑色烟雾
                            for (int k = 0; k < 12; k++)
                            {
                                Dust d = Dust.NewDustPerfect(npc.Center,
                                    DustID.Shadowflame, Main.rand.NextVector2Circular(4, 4), 0, default, 1.6f);
                                d.noGravity = true;
                            }
                        }
                    }

                    // 释放者中心: 紫色暗黑法阵
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        for (int ring = 0; ring < 3; ring++)
                        {
                            float radius = 120 + ring * 80;
                            int count = 60 + ring * 30;
                            for (int k = 0; k < count; k++)
                            {
                                float angle = (k / (float)count) * MathHelper.TwoPi;
                                Vector2 pos = Player.Center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
                                Dust d = Dust.NewDustPerfect(pos, DustID.PurpleCrystalShard,
                                    Vector2.Zero, 0, default, 2.5f);
                                d.noGravity = true;
                                d.fadeIn = 1.5f;
                            }
                        }
                        // 释放者头顶降下黑紫色雷云感
                        for (int k = 0; k < 60; k++)
                        {
                            Vector2 pos = Player.Center + new Vector2(Main.rand.NextFloat(-100, 100), -250 + Main.rand.NextFloat(-60, 60));
                            Dust d = Dust.NewDustPerfect(pos, DustID.Shadowflame,
                                Main.rand.NextVector2Circular(2, 2), 0, default, 2.2f);
                            d.noGravity = true;
                        }
                    }

                    Main.NewText($"【福祸之言·祸】 你诅咒了周围 {affected} 个敌人,厄运闪电降临,60秒重度厄运缠身。", 180, 60, 220);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, Player.position);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item62, Player.position); // 厚重轰鸣
                }
            }

            // [先知·命运启示] U键: 10秒所有概率事件偏向最优解
            if (currentWheelSequence <= 2 && LotMKeybinds.Wheel_Revelation.JustPressed)
            {
                if (revelationCooldown > 0)
                {
                    Main.NewText($"命运启示冷却中... ({revelationCooldown / 60}秒)", 255, 100, 100);
                }
                else if (!TryConsumeSpirituality(500))
                {
                    Main.NewText("灵性不足 (启示需 500 点)", 255, 50, 50);
                }
                else
                {
                    revelationCooldown = REVELATION_CD_MAX;
                    revelationActiveTimer = 600; // 10秒启示状态

                    // 序列1 巨蛇仪式: 每次启示+1
                    if (baseWheelSequence == 2 && !serpentRitualComplete && Main.myPlayer == Player.whoAmI)
                    {
                        serpentRitualProgress++;
                        Main.NewText($"凝视命运长河... ({serpentRitualProgress}/{SERPENT_RITUAL_TARGET}) | 巨蛇仪式进度", 200, 150, 255);
                    }

                    // 双重音效叠加: 启示之声 + 厚重轰鸣
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item104, Player.position);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, Player.position);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item62, Player.position);
                    Main.NewText("【命运启示】你凝视命运长河,看到了所有可能性中的最优解...", 200, 150, 255);
                    Main.NewText("时间在你眼中扭曲缓慢，万物都被命运之线所牵...", 180, 130, 230);

                    if (Main.myPlayer == Player.whoAmI)
                    {
                        // [开场震撼A] 五圈同心紫金法阵从内向外炸开
                        for (int ring = 0; ring < 5; ring++)
                        {
                            float radius = 80 + ring * 60;
                            int count = 50 + ring * 25;
                            for (int k = 0; k < count; k++)
                            {
                                float angle = (k / (float)count) * MathHelper.TwoPi;
                                Vector2 pos = Player.Center + new Vector2((float)System.Math.Cos(angle), (float)System.Math.Sin(angle)) * radius;
                                // 内圈紫, 外圈金
                                int dustType = ring < 2 ? DustID.PurpleCrystalShard
                                            : ring < 4 ? DustID.YellowTorch : DustID.GoldCoin;
                                Dust d = Dust.NewDustPerfect(pos, dustType,
                                    new Vector2((float)System.Math.Cos(angle), (float)System.Math.Sin(angle)) * 3f, 0, default, 2.5f);
                                d.noGravity = true;
                                d.fadeIn = 1.5f;
                            }
                        }

                        // [开场震撼B] 玩家正上方降下"命运光柱"
                        for (int k = 0; k < 120; k++)
                        {
                            Vector2 pos = Player.Center + new Vector2(Main.rand.NextFloat(-25, 25), Main.rand.NextFloat(-600, 0));
                            int t = Main.rand.NextBool() ? DustID.GoldCoin : DustID.PurpleCrystalShard;
                            Dust d = Dust.NewDustPerfect(pos, t,
                                new Vector2(0, Main.rand.NextFloat(2, 5)), 0, default, 2f);
                            d.noGravity = true;
                        }

                        // [开场震撼C] 屏幕全角发散粒子(像启示画面般四散)
                        for (int k = 0; k < 200; k++)
                        {
                            float angle = Main.rand.NextFloat() * MathHelper.TwoPi;
                            Vector2 dir = new Vector2((float)System.Math.Cos(angle), (float)System.Math.Sin(angle));
                            Vector2 startPos = Player.Center + dir * Main.rand.NextFloat(50, 800);
                            Dust d = Dust.NewDustPerfect(startPos, DustID.YellowTorch,
                                dir * Main.rand.NextFloat(-3, 0), 0, default, 1.6f);
                            d.noGravity = true;
                            d.fadeIn = 1.2f;
                        }

                        // [开场震撼D] 镜头震动
                        Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                            Player.Center, new Vector2(0.3f, -0.7f), 12f, 8f, 18, 1000f));

                        // [玩法层E] 命运的偏移 - 屏幕内所有敌人被吸引向玩家
                        // (代表"你已看到他们所有可能的位置,把他们拉到对你最有利的那个")
                        foreach (NPC npc in Main.ActiveNPCs)
                        {
                            if (!npc.friendly && !npc.dontTakeDamage && !npc.boss
                                && npc.Distance(Player.Center) < 1500f
                                && npc.Distance(Player.Center) > 100f)
                            {
                                Vector2 dir = (Player.Center - npc.Center).SafeNormalize(Vector2.Zero);
                                npc.velocity = dir * 25f; // 强力推向玩家
                                npc.netUpdate = true;
                                for (int k = 0; k < 8; k++)
                                {
                                    Dust d = Dust.NewDustPerfect(npc.Center,
                                        DustID.GoldCoin, -dir * 3f, 0, default, 1.2f);
                                    d.noGravity = true;
                                }
                            }
                        }

                        // [玩法层F] 随机生成一行"预言文字"作为预告
                        string[] prophecies = new string[] {
                            "命运预言: 你将在下一击中送一个敌人入死亡之河...",
                            "命运预言: 黄金将从天而降,如往昔的丰收。",
                            "命运预言: 你的攻击将穿透所有可能性。",
                            "命运预言: 时间为你停滞,空间向你折叠。",
                            "命运预言: 敌人的死亡已成定数。",
                            "命运预言: 你将看到长河的对岸。",
                            "命运预言: 此刻的你,即是命运本身。"
                        };
                        Main.NewText(prophecies[Main.rand.Next(prophecies.Length)], 200, 160, 255);
                    }
                }
            }

            // ==========================================================
            // [序列1·命运循环] Y键: 5秒后回滚范围内NPC位置 (Boss除外)
            // ==========================================================
            if (currentWheelSequence <= 1 && LotMKeybinds.Wheel_FateLoop.JustPressed)
            {
                if (fateLoopCooldown > 0)
                {
                    Main.NewText($"命运循环冷却中... ({fateLoopCooldown / 60}秒)", 255, 100, 100);
                }
                else if (fateLoopActive)
                {
                    Main.NewText("命运循环已经在生效中。", 255, 200, 100);
                }
                else if (!TryConsumeSpirituality(1000))
                {
                    Main.NewText("灵性不足 (命运循环需 1000 点)", 255, 50, 50);
                }
                else
                {
                    fateLoopCooldown = FATE_LOOP_CD_MAX;
                    fateLoopActive = true;
                    fateLoopExpireTick = Main.GameUpdateCount + (uint)FATE_LOOP_DURATION;
                    fateLoopActiveTimer = FATE_LOOP_DURATION;
                    fateLoopCenter = Player.Center;
                    _loopedNpcStartPos.Clear();

                    // 快照所有范围内NPC的位置
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!npc.friendly && !npc.dontTakeDamage
                            && npc.Distance(fateLoopCenter) < 800f
                            && !_loopedNpcStartPos.ContainsKey(npc.whoAmI))
                        {
                            _loopedNpcStartPos[npc.whoAmI] = npc.position;
                        }
                    }

                    Main.NewText($"【命运循环·展开】 {_loopedNpcStartPos.Count} 个目标的位置已被记录,5秒后回滚到此刻。", 200, 150, 255);
                    Main.NewText("（5秒内的伤害和血量变化将被保留,但位置回到现在）", 180, 180, 220);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item104, Player.position);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, Player.position);

                    if (Main.myPlayer == Player.whoAmI)
                    {
                        // [开场特效] 巨大齿轮领域展开
                        for (int ring = 0; ring < 4; ring++)
                        {
                            float radius = 200 + ring * 200;
                            int count = 80 + ring * 30;
                            for (int k = 0; k < count; k++)
                            {
                                float angle = (k / (float)count) * MathHelper.TwoPi;
                                Vector2 pos = fateLoopCenter + new Vector2((float)System.Math.Cos(angle), (float)System.Math.Sin(angle)) * radius;
                                int t = (ring % 2 == 0) ? DustID.GoldCoin : DustID.PurpleCrystalShard;
                                Dust d = Dust.NewDustPerfect(pos, t,
                                    new Vector2((float)System.Math.Cos(angle), (float)System.Math.Sin(angle)) * 2f, 0, default, 2f);
                                d.noGravity = true;
                                d.fadeIn = 1.5f;
                            }
                        }
                        // 镜头震
                        Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                            Player.Center, new Vector2(0.2f, -0.5f), 10f, 7f, 15, 1000f));
                    }
                }
            }

            // ==========================================================
            // [序列1·主动重启] H键: 主动撤退 - 回到10秒前位置+满血+清debuff
            // ==========================================================
            if (currentWheelSequence <= 1 && LotMKeybinds.Wheel_Restart.JustPressed)
            {
                if (restartManualCooldown > 0)
                {
                    Main.NewText($"主动重启冷却中... ({restartManualCooldown / 60}秒)", 255, 100, 100);
                }
                else if (!TryConsumeSpirituality(3000))
                {
                    Main.NewText("灵性不足 (主动重启需 3000 点)", 255, 50, 50);
                }
                else
                {
                    restartManualCooldown = RESTART_MANUAL_CD_MAX;

                    // 回到10秒前位置
                    if (positionHistoryFilled)
                    {
                        Vector2 pastPos = positionHistory[positionHistoryIdx];
                        if (pastPos != Vector2.Zero)
                        {
                            // 拖影回放
                            if (Main.myPlayer == Player.whoAmI)
                            {
                                for (int i = 0; i < positionHistory.Length; i += 8)
                                {
                                    int idx = (positionHistoryIdx + i) % positionHistory.Length;
                                    Vector2 hp = positionHistory[idx];
                                    if (hp == Vector2.Zero) continue;
                                    Dust d = Dust.NewDustPerfect(hp, DustID.SilverCoin, Vector2.Zero, 0, default, 1.4f);
                                    d.noGravity = true;
                                    d.fadeIn = 1.3f;
                                }
                            }
                            Player.position = pastPos - Player.Size / 2f;
                            Player.velocity = Vector2.Zero;
                        }
                    }

                    // 满血+清debuff
                    Player.statLife = Player.statLifeMax2;
                    Player.HealEffect(Player.statLifeMax2);
                    for (int b = 0; b < Player.MaxBuffs; b++)
                    {
                        int bt = Player.buffType[b];
                        if (bt > 0 && Main.debuff[bt]) Player.DelBuff(b);
                    }
                    Player.immune = true;
                    Player.immuneTime = 120;

                    Main.NewText("【主动重启】你撕开了10秒前的存档,把自己覆盖了回去。", 200, 200, 255);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item104, Player.position);

                    if (Main.myPlayer == Player.whoAmI)
                    {
                        // 中心爆发: 100颗银色粒子
                        for (int k = 0; k < 100; k++)
                        {
                            float ang = (k / 100f) * MathHelper.TwoPi;
                            Dust d = Dust.NewDustPerfect(Player.Center,
                                (k % 3 == 0) ? DustID.GoldCoin : DustID.SilverCoin,
                                new Vector2((float)System.Math.Cos(ang), (float)System.Math.Sin(ang)) * 6f, 0, default, 2f);
                            d.noGravity = true;
                            d.fadeIn = 1.5f;
                        }
                    }
                }
            }

            // =================================================================
            // [学徒·开门] E键: 穿过附近墙壁
            // =================================================================
            if (currentDoorSequence <= 9 && LotMKeybinds.Door_OpenDoor.JustPressed)
            {
                if (doorOpenCooldown > 0)
                {
                    Main.NewText($"开门冷却中... ({doorOpenCooldown / 60f:F1}秒)", 255, 100, 100);
                }
                else if (!TryConsumeSpirituality(10))
                {
                    Main.NewText("灵性不足 (开门需 10 点)", 255, 50, 50);
                }
                else
                {
                    // 朝玩家面向方向(由 Player.direction 决定: 1向右 -1向左) 探测墙壁厚度
                    int dir = Player.direction;
                    // 玩家中心位置 (像素)
                    Vector2 startPos = Player.Center;
                    // 玩家碰撞盒大致是 2 格宽,3 格高. 从玩家边缘开始检测
                    int startTileX = (int)(startPos.X / 16f) + dir; // 玩家面向方向的第一格
                    int startTileY = (int)(startPos.Y / 16f);
                    int wallThickness = 0;
                    int maxScan = 8; // 最多扫描8格墙(超过就视为"山", 不能穿)

                    // 找连续的实心墙壁
                    bool foundWallStart = false;
                    int wallStartX = startTileX;
                    for (int i = 0; i < maxScan; i++)
                    {
                        int tx = startTileX + i * dir;
                        // 检测玩家身高范围内的所有tile (大致4格高)
                        bool blocked = false;
                        for (int dy = -2; dy <= 1; dy++)
                        {
                            int ty = startTileY + dy;
                            if (ty < 0 || ty >= Main.maxTilesY) continue;
                            if (tx < 0 || tx >= Main.maxTilesX) continue;
                            Tile t = Main.tile[tx, ty];
                            if (t != null && t.HasTile && Main.tileSolid[t.TileType] && !Main.tileSolidTop[t.TileType])
                            {
                                blocked = true;
                                break;
                            }
                        }
                        if (blocked)
                        {
                            if (!foundWallStart)
                            {
                                wallStartX = tx;
                                foundWallStart = true;
                            }
                            wallThickness++;
                        }
                        else if (foundWallStart)
                        {
                            // 墙壁结束 - 找到墙的另一边
                            break;
                        }
                    }

                    if (!foundWallStart)
                    {
                        Main.NewText("面前没有可穿越的墙壁。", 255, 200, 100);
                        // 不消耗CD和灵性 - 恢复灵性
                        spiritualityCurrent += 10;
                    }
                    else if (wallThickness > 5)
                    {
                        Main.NewText("墙壁太厚 (>5格), 这不再是\"常规墙壁\",无法穿越。", 255, 100, 100);
                        spiritualityCurrent += 10;
                    }
                    else
                    {
                        // 穿墙: 把玩家传送到墙另一边的位置
                        // 目标 X = wallStartX + wallThickness * dir (墙另一边第一个空气格)
                        int targetTileX = wallStartX + wallThickness * dir;
                        // 目标 Y = 当前tileY (玩家脚下)
                        int targetTileY = startTileY;

                        // 验证目标位置可以站人 - 至少3格高的空气
                        bool targetClear = true;
                        for (int dy = -2; dy <= 1; dy++)
                        {
                            int ty = targetTileY + dy;
                            if (ty < 0 || ty >= Main.maxTilesY) { targetClear = false; break; }
                            if (targetTileX < 0 || targetTileX >= Main.maxTilesX) { targetClear = false; break; }
                            Tile t = Main.tile[targetTileX, targetTileY + dy];
                            if (t != null && t.HasTile && Main.tileSolid[t.TileType] && !Main.tileSolidTop[t.TileType])
                            {
                                targetClear = false;
                                break;
                            }
                        }

                        if (!targetClear)
                        {
                            Main.NewText("墙的另一边也是固体 (不能卡进山里)。", 255, 100, 100);
                            spiritualityCurrent += 10;
                        }
                        else
                        {
                            // === 执行穿墙 ===
                            doorOpenCooldown = DOOR_OPEN_CD_MAX;

                            // 起点效果
                            Vector2 fromPos = Player.Center;

                            // 计算目标像素位置 (居中)
                            Vector2 toPos = new Vector2(
                                targetTileX * 16f + 8f,
                                targetTileY * 16f + 8f
                            );

                            // 传送
                            Player.position = toPos - Player.Size / 2f;
                            Player.velocity = Vector2.Zero;

                            // 短暂无敌防止穿出来被秒
                            Player.immune = true;
                            Player.immuneTime = 30;

                            // === 视觉/音效 ===
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.DoorOpen, fromPos);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, toPos);

                            if (Main.myPlayer == Player.whoAmI)
                            {
                                // 起点门型粒子
                                for (int k = 0; k < 30; k++)
                                {
                                    Dust d = Dust.NewDustPerfect(
                                        fromPos + new Vector2(0, Main.rand.NextFloat(-24, 24)),
                                        DustID.GoldCoin, Vector2.Zero, 0, default, 1.4f);
                                    d.noGravity = true;
                                    d.fadeIn = 1.2f;
                                }
                                // 终点同样效果
                                for (int k = 0; k < 30; k++)
                                {
                                    Dust d = Dust.NewDustPerfect(
                                        toPos + new Vector2(0, Main.rand.NextFloat(-24, 24)),
                                        DustID.YellowTorch, Vector2.Zero, 0, default, 1.4f);
                                    d.noGravity = true;
                                    d.fadeIn = 1.2f;
                                }
                            }

                            Main.NewText($"[开门] 你穿过了 {wallThickness} 格厚的墙壁。", 220, 220, 100);
                        }
                    }
                }
            }

            // =================================================================
            // [戏法大师] 序列8: 切换戏法 + 释放戏法 (专属按键,默认T/R,可改键)
            // =================================================================
            if (currentDoorSequence <= 8)
            {
                // 切换戏法 (循环选择 0-10, 第11个"逃脱"是自动触发不在循环里)
                if (LotMKeybinds.Door_TrickSwitch.JustPressed)
                {
                    selectedTrick = (selectedTrick + 1) % 11; // 只在前11个戏法里循环
                    Main.NewText($"► 戏法: [c/FFE878:{TrickNames[selectedTrick]}] ({selectedTrick + 1}/11)", 220, 220, 120);
                }

                // 释放戏法
                if (LotMKeybinds.Door_TrickCast.JustPressed)
                {
                    if (trickCooldown > 0)
                    {
                        Main.NewText($"戏法冷却中... ({trickCooldown / 60f:F1}秒)", 255, 100, 100);
                    }
                    else
                    {
                        CastTrick(selectedTrick);
                    }
                }
            }

            // =================================================================
            // [占星人·占星术] 序列7: G键消耗灵性获得"预兆"状态15秒
            // =================================================================
            if (currentDoorSequence <= 7 && LotMKeybinds.Door_Astrology.JustPressed)
            {
                if (astrologyCooldown > 0)
                {
                    Main.NewText($"占星术冷却中... ({astrologyCooldown / 60}秒)", 255, 100, 100);
                }
                else if (astrologyActiveTimer > 0)
                {
                    Main.NewText("占卜已经在进行中。", 255, 200, 100);
                }
                else if (!TryConsumeSpirituality(300))
                {
                    Main.NewText("灵性不足 (占星术需 300 点)", 255, 50, 50);
                }
                else
                {
                    astrologyCooldown = ASTROLOGY_CD_MAX;
                    astrologyActiveTimer = ASTROLOGY_DURATION;

                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item104, Player.position);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29, Player.position);

                    // 占星预言文本(随机)
                    string[] omens = {
                        "占星预言: 群星呼唤,你的下次出击必成必中。",
                        "占星预言: 命途流转,你的命运在接下来15秒指向胜利。",
                        "占星预言: 星象大吉,危险将自动远离你。",
                        "占星预言: 黄道辉煌,你的灵性洞察一切。",
                        "占星预言: 紫微帝星,你身周三尺如有神助。",
                        "占星预言: 北斗倾转,逆袭之时已至。"
                    };
                    Main.NewText(omens[Main.rand.Next(omens.Length)], 255, 220, 100);
                    Main.NewText("【占星术·激活】15 秒内 伤害+25% / 暴击+25 / 减伤+10% / 持续回血", 255, 200, 120);

                    if (Main.myPlayer == Player.whoAmI)
                    {
                        // 水晶球开启视觉: 玩家头顶上方出现星象阵
                        for (int k = 0; k < 60; k++)
                        {
                            float ang = (k / 60f) * MathHelper.TwoPi;
                            Vector2 pos = Player.Center + new Vector2((float)System.Math.Cos(ang), (float)System.Math.Sin(ang)) * 120f;
                            Dust d = Dust.NewDustPerfect(pos, DustID.GoldCoin, Vector2.Zero, 0, default, 1.8f);
                            d.noGravity = true;
                            d.fadeIn = 1.5f;
                        }
                        // 中心紫色爆发
                        for (int k = 0; k < 40; k++)
                        {
                            Dust d = Dust.NewDustPerfect(Player.Center, DustID.MagicMirror,
                                Main.rand.NextVector2Circular(5, 5), 0, default, 1.7f);
                            d.noGravity = true;
                            d.fadeIn = 1.4f;
                        }
                    }
                }
            }

            // =================================================================
            // [记录官·普通记录] 序列6
            //   F短按: 循环切换当前选中的普通记录
            //   Shift+F (或长按) : 释放当前选中的普通记录 - 调用蠕动饥饿的能力库
            // =================================================================
            if (currentDoorSequence <= 6 && LotMKeybinds.Door_RecordNormal.JustPressed)
            {
                if (recorderNormalList.Count == 0)
                {
                    Main.NewText("还没有任何普通记录 (击杀有非凡能力的敌人可获得)。", 255, 200, 100);
                }
                else if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift)
                      || Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift))
                {
                    // === Shift+F: 释放当前选中的记录 ===
                    if (recorderCooldown > 0)
                    {
                        Main.NewText($"记录使用冷却中... ({recorderCooldown / 60f:F1}秒)", 255, 100, 100);
                    }
                    else
                    {
                        int idx = recorderSelectedNormal % recorderNormalList.Count;
                        int npcType = recorderNormalList[idx];
                        string npcName = Lang.GetNPCNameValue(npcType);

                        recorderCooldown = RECORDER_CD_MAX;
                        // 不消耗记录(类似蠕动饥饿,记录是"载体",可重复咏唱)

                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item104, Player.position);
                        Main.NewText("「我来到, 我看见, 我记录。」", 220, 200, 130);
                        Main.NewText($"◆ 咏唱: [{npcName}] 的非凡符号 - 你重现了它的能力!", 220, 220, 120);

                        // 执行能力 - 借用蠕动饥饿的能力库
                        Vector2 mousePos = Main.MouseWorld;
                        Vector2 dir = (mousePos - Player.Center).SafeNormalize(Vector2.UnitX);
                        Vector2 shootPos = Player.Center;
                        Vector2 shootVel = dir * 14f;
                        int dmg = 80;  // 普通能力固定基数(简化)

                        if (CreepingHunger.IsSpecialSkillNPC_Static(npcType))
                        {
                            CreepingHunger.UseSpecialSkill(Player, npcType,
                                Player.GetSource_FromThis(), shootPos, shootVel, dmg, 4f);
                        }
                        else
                        {
                            int projType = CreepingHunger.GetProjectileFromNPC_Static(npcType);
                            if (projType != ProjectileID.None)
                            {
                                int p = Projectile.NewProjectile(Player.GetSource_FromThis(),
                                    shootPos, shootVel, projType, dmg, 4f, Main.myPlayer);
                                if (p >= 0 && p < Main.maxProjectiles)
                                {
                                    Main.projectile[p].hostile = false;
                                    Main.projectile[p].friendly = true;
                                    Main.projectile[p].DamageType = DamageClass.Magic;
                                }
                            }
                        }

                        if (Main.myPlayer == Player.whoAmI)
                        {
                            for (int k = 0; k < 30; k++)
                            {
                                float ang = (k / 30f) * MathHelper.TwoPi;
                                Vector2 pos = Player.Center + new Vector2((float)System.Math.Cos(ang), (float)System.Math.Sin(ang)) * 60f;
                                Dust d = Dust.NewDustPerfect(pos, DustID.GoldCoin, Vector2.Zero, 0, default, 1.3f);
                                d.noGravity = true;
                                d.fadeIn = 1.2f;
                            }
                        }
                    }
                }
                else
                {
                    // === 单按F: 切换选中 ===
                    recorderSelectedNormal = (recorderSelectedNormal + 1) % recorderNormalList.Count;
                    int npcType = recorderNormalList[recorderSelectedNormal];
                    string npcName = Lang.GetNPCNameValue(npcType);
                    Main.NewText($"► 选中记录: [c/FFE878:{npcName}] ({recorderSelectedNormal + 1}/{recorderNormalList.Count})", 220, 220, 120);
                }
            }

            // =================================================================
            // [记录官·神性记录] 序列6: C键
            //   单按C: 释放当前选中的神性记录 (神性记录稀有,直接释放)
            //   Shift+C: 切换选中神性记录(目前最多1个)
            // =================================================================
            if (currentDoorSequence <= 6 && LotMKeybinds.Door_RecordDivine.JustPressed)
            {
                if (recorderDivineList.Count == 0)
                {
                    Main.NewText("还没有任何神性记录 (击杀Boss有几率获得)。", 255, 200, 100);
                }
                else if (recorderCooldown > 0)
                {
                    Main.NewText($"记录使用冷却中... ({recorderCooldown / 60f:F1}秒)", 255, 100, 100);
                }
                else
                {
                    int idx = recorderSelectedDivine % recorderDivineList.Count;
                    int npcType = recorderDivineList[idx];
                    string npcName = Lang.GetNPCNameValue(npcType);

                    recorderCooldown = RECORDER_CD_MAX;

                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, Player.position);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item104, Player.position);
                    Main.NewText("【我来到, 我看见, 我记录!】", 255, 200, 100);
                    Main.NewText($"◈ 神性咏唱: 你重现了 [{npcName}] 的非凡神性!", 220, 180, 255);

                    Vector2 mousePos = Main.MouseWorld;
                    Vector2 dir = (mousePos - Player.Center).SafeNormalize(Vector2.UnitX);
                    int dmg = 300; // 神性能力固定高伤害

                    if (CreepingHunger.IsSpecialSkillNPC_Static(npcType))
                    {
                        CreepingHunger.UseSpecialSkill(Player, npcType,
                            Player.GetSource_FromThis(), Player.Center, dir * 16f, dmg, 8f);
                    }
                    else
                    {
                        int projType = CreepingHunger.GetProjectileFromNPC_Static(npcType);
                        if (projType != ProjectileID.None)
                        {
                            int p = Projectile.NewProjectile(Player.GetSource_FromThis(),
                                Player.Center, dir * 16f, projType, dmg, 8f, Main.myPlayer);
                            if (p >= 0 && p < Main.maxProjectiles)
                            {
                                Main.projectile[p].hostile = false;
                                Main.projectile[p].friendly = true;
                                Main.projectile[p].DamageType = DamageClass.Magic;
                            }
                        }
                    }

                    if (Main.myPlayer == Player.whoAmI)
                    {
                        for (int ring = 0; ring < 3; ring++)
                        {
                            int count = 40 + ring * 20;
                            float radius = 60 + ring * 40;
                            for (int k = 0; k < count; k++)
                            {
                                float ang = (k / (float)count) * MathHelper.TwoPi;
                                Vector2 pos = Player.Center + new Vector2((float)System.Math.Cos(ang), (float)System.Math.Sin(ang)) * radius;
                                int t = (ring % 2 == 0) ? DustID.GoldCoin : DustID.PurpleCrystalShard;
                                Dust d = Dust.NewDustPerfect(pos, t, Vector2.Zero, 0, default, 1.8f);
                                d.noGravity = true;
                                d.fadeIn = 1.5f;
                            }
                        }
                        Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                            Player.Center, Vector2.UnitY, 12f, 8f, 20, 1000f));
                    }
                }
            }

            // =================================================================
            // [旅行家·旅行家之门] 序列5: J键, 全图任意位置传送
            //   按J → 自动打开全屏地图 → 在地图上点击 → 传送
            //   再按J = 取消选点
            // =================================================================
            if (currentDoorSequence <= 5 && LotMKeybinds.Door_TravelerGate.JustPressed)
            {
                if (travelerGateCooldown > 0)
                {
                    Main.NewText($"旅行家之门冷却中... ({travelerGateCooldown / 60}秒)", 255, 100, 100);
                }
                else if (travelerGateAwaitingMapClick)
                {
                    // 第二次按 = 取消
                    travelerGateAwaitingMapClick = false;
                    Main.mapFullscreen = false;
                    Main.NewText("【旅行家之门】 已取消传送。", 200, 200, 200);
                }
                else if (Main.myPlayer == Player.whoAmI)
                {
                    // 先打开全屏地图,进入选点等待
                    travelerGateAwaitingMapClick = true;
                    Main.mapFullscreen = true;
                    Main.NewText("【旅行家之门】 在地图上点击任意位置传送 (再按 J 取消)。", 220, 180, 255);
                }
            }

            // === 地图点击监听: 玩家在等待选点状态下点击全屏地图触发传送 ===
            if (travelerGateAwaitingMapClick && Main.myPlayer == Player.whoAmI)
            {
                if (!Main.mapFullscreen)
                {
                    // 玩家关掉了地图,取消选点
                    travelerGateAwaitingMapClick = false;
                }
                else if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    // 把鼠标在全屏地图上的位置转成世界坐标
                    // Main.mapFullscreenScale = 缩放, Main.mapFullscreenPos = 中心(tile坐标)
                    Vector2 mouseScreen = new Vector2(Main.mouseX, Main.mouseY);
                    Vector2 screenCenter = new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f);
                    Vector2 offsetFromCenter = (mouseScreen - screenCenter) / Main.mapFullscreenScale;
                    Vector2 targetTile = Main.mapFullscreenPos + offsetFromCenter;
                    Vector2 target = targetTile * 16f;
                    Vector2 targetTL = target - Player.Size / 2f;

                    // 限制坐标在世界范围内
                    if (targetTile.X < 50 || targetTile.X > Main.maxTilesX - 50
                        || targetTile.Y < 50 || targetTile.Y > Main.maxTilesY - 50)
                    {
                        Main.NewText("目标超出世界范围。", 255, 200, 100);
                        return;
                    }
                    // 卡在固体里?向上找最近的空气格
                    int safeY = (int)targetTile.Y;
                    int safeX = (int)targetTile.X;
                    int findTries = 0;
                    while (findTries < 100 && safeY > 10)
                    {
                        bool blocked = false;
                        for (int dy = -2; dy <= 1; dy++)
                        {
                            Tile t = Main.tile[safeX, safeY + dy];
                            if (t != null && t.HasTile && Main.tileSolid[t.TileType] && !Main.tileSolidTop[t.TileType])
                            { blocked = true; break; }
                        }
                        if (!blocked) break;
                        safeY--;
                        findTries++;
                    }
                    target = new Vector2(safeX * 16f + 8f, safeY * 16f);
                    targetTL = target - Player.Size / 2f;

                    // 消耗灵性
                    if (!TryConsumeSpirituality(500))
                    {
                        Main.NewText("灵性不足 (旅行家之门需 500 点)", 255, 50, 50);
                        travelerGateAwaitingMapClick = false;
                        Main.mapFullscreen = false;
                        return;
                    }

                    travelerGateAwaitingMapClick = false;
                    Main.mapFullscreen = false;  // 关闭地图
                    travelerGateCooldown = TRAVELER_GATE_CD;
                    Vector2 from = Player.Center;

                    // === 起点门 ===
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, from);
                    for (int k = 0; k < 50; k++)
                    {
                        float ang = (k / 50f) * MathHelper.TwoPi;
                        Vector2 p = from + new Vector2((float)System.Math.Cos(ang), (float)System.Math.Sin(ang)) * 40f;
                        Dust d = Dust.NewDustPerfect(p, DustID.PurpleCrystalShard, Vector2.Zero, 0, default, 1.8f);
                        d.noGravity = true;
                        d.fadeIn = 1.5f;
                    }

                    // === 传送 ===
                    Player.Teleport(targetTL, 1);
                    NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null,
                        0, Player.whoAmI, targetTL.X, targetTL.Y, 1);
                    Player.immune = true;
                    Player.immuneTime = 45;

                    // === 终点门 ===
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item6, target);
                    for (int k = 0; k < 50; k++)
                    {
                        float ang = (k / 50f) * MathHelper.TwoPi;
                        Vector2 p = target + new Vector2((float)System.Math.Cos(ang), (float)System.Math.Sin(ang)) * 40f;
                        Dust d = Dust.NewDustPerfect(p, DustID.GoldCoin, Vector2.Zero, 0, default, 1.8f);
                        d.noGravity = true;
                        d.fadeIn = 1.5f;
                    }

                    Main.NewText("【旅行家之门】 你于灵界中穿梭,跨越了整个世界。", 220, 180, 255);
                }
            }

            // =================================================================
            // [旅行家·闪现] 序列5: K键, 朝鼠标方向短距闪现
            // =================================================================
            if (currentDoorSequence <= 5 && LotMKeybinds.Door_Blink.JustPressed)
            {
                if (travelerBlinkCooldown > 0)
                {
                    // 1秒CD,不刷屏提示
                }
                else if (!TryConsumeSpirituality(50))
                {
                    Main.NewText("灵性不足 (闪现需 50 点)", 255, 50, 50);
                }
                else if (Main.myPlayer == Player.whoAmI)
                {
                    Vector2 dir = (Main.MouseWorld - Player.Center).SafeNormalize(Vector2.UnitX);
                    // 闪现30格 = 480像素 (限制不超过鼠标位置)
                    float maxBlink = 480f;
                    float toMouse = Vector2.Distance(Player.Center, Main.MouseWorld);
                    float blinkDist = System.Math.Min(maxBlink, toMouse);

                    Vector2 target = Player.Center + dir * blinkDist;
                    Vector2 targetTL = target - Player.Size / 2f;

                    // 检查终点是否在固体内 - 是则缩短到第一个可行位置
                    int tries = 0;
                    while (Collision.SolidCollision(targetTL, Player.width, Player.height) && tries < 10)
                    {
                        blinkDist *= 0.7f;
                        target = Player.Center + dir * blinkDist;
                        targetTL = target - Player.Size / 2f;
                        tries++;
                    }

                    if (Collision.SolidCollision(targetTL, Player.width, Player.height))
                    {
                        Main.NewText("无法闪现到此处。", 255, 200, 100);
                        spiritualityCurrent += 50; // 退还
                    }
                    else
                    {
                        travelerBlinkCooldown = TRAVELER_BLINK_CD;
                        Vector2 from = Player.Center;

                        // 起点拖尾粒子
                        for (int k = 0; k < 15; k++)
                        {
                            float t = k / 15f;
                            Vector2 p = Vector2.Lerp(from, target, t);
                            Dust d = Dust.NewDustPerfect(p, DustID.PurpleCrystalShard, Vector2.Zero, 0, default, 1.3f);
                            d.noGravity = true;
                            d.fadeIn = 1.1f;
                        }

                        Player.position = targetTL;
                        Player.velocity *= 0.5f;
                        Player.immune = true;
                        Player.immuneTime = 15;

                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, target);
                    }
                }
            }

            // [命运骰子] M键: 序列3+均可
            if (currentWheelSequence <= 3 && LotMKeybinds.Wheel_Dice.JustPressed)
            {
                if (fateDiceCooldown > 0)
                {
                    Main.NewText($"命运骰子冷却中... ({fateDiceCooldown / 60}秒)", 255, 100, 100);
                }
                else if (!TryConsumeSpirituality(200))
                {
                    Main.NewText("灵性不足 (需 200 点)", 255, 50, 50);
                }
                else
                {
                    fateDiceCooldown = FATE_DICE_CD_MAX;
                    int diceResult = Main.rand.Next(1, 7); // 1-6

                    // 公共特效:大量金色粒子+音效
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29, Player.position);
                    for (int i = 0; i < 60; i++)
                    {
                        Dust d = Dust.NewDustPerfect(Player.Center,
                            DustID.GoldCoin, Main.rand.NextVector2Circular(10, 10), 0, default, 2f);
                        d.noGravity = true;
                    }

                    Main.NewText($"骰子落地: {diceResult} 点", 255, 215, 0);

                        switch (diceResult)
                        {
                            case 1: // 厄: 自身倒霉1分钟
                                Player.AddBuff(ModContent.BuffType<ExtremeBadLuckBuff>(), 3600);
                                Main.NewText("命运嘲弄你... 1点!", 200, 50, 50);
                                CombatText.NewText(Player.getRect(), Color.Red, "1", true);
                                break;
                            case 2: // 治愈: 恢复30%生命
                                int healAmount = (int)(Player.statLifeMax2 * 0.30f);
                                Player.statLife += healAmount;
                                if (Player.statLife > Player.statLifeMax2) Player.statLife = Player.statLifeMax2;
                                Player.HealEffect(healAmount, true);
                                Main.NewText("命运眷顾，伤口愈合! 2点!", 100, 255, 100);
                                CombatText.NewText(Player.getRect(), Color.Lime, "2", true);
                                break;
                            case 3: // 灾厄: 周围1500像素内非Boss敌人各受1000真实伤害
                                int hitCount = 0;
                                foreach (NPC npc in Main.ActiveNPCs)
                                {
                                    if (!npc.friendly && !npc.dontTakeDamage && !npc.boss
                                        && npc.Distance(Player.Center) < 1500f)
                                    {
                                        npc.SimpleStrikeNPC(1000, 0, false, 0, DamageClass.Default, true);
                                        npc.netUpdate = true;
                                        CombatText.NewText(npc.getRect(), Color.Crimson, "天罚!", true);
                                        hitCount++;
                                    }
                                }
                                Main.NewText($"命运的天罚降临 ({hitCount} 个敌人)! 3点!", 255, 100, 0);
                                CombatText.NewText(Player.getRect(), Color.Orange, "3", true);
                                break;
                            case 4: // 庇护: 30秒命运庇护(50%伤害减免)
                                fateBlessingActiveTimer = 1800; // 30秒
                                Main.NewText("命运庇护笼罩你 30 秒! 4点!", 255, 215, 0);
                                CombatText.NewText(Player.getRect(), Color.Gold, "4", true);
                                break;
                            case 5: // 滞: 全屏减速敌人15秒
                                foreach (NPC npc in Main.ActiveNPCs)
                                {
                                    if (!npc.friendly && !npc.dontTakeDamage)
                                    {
                                        npc.AddBuff(BuffID.Slow, 900);
                                        npc.AddBuff(BuffID.Confused, 900);
                                        npc.netUpdate = true;
                                    }
                                }
                                Main.NewText("命运凝滞，所有敌人陷入混乱! 5点!", 100, 200, 255);
                                CombatText.NewText(Player.getRect(), Color.Cyan, "5", true);
                                break;
                            case 6: // 大吉: Boss削15%血,无Boss则全屏非精英秒杀
                                bool hitBoss = false;
                                foreach (NPC npc in Main.ActiveNPCs)
                                {
                                    if (npc.boss && !npc.friendly && !npc.dontTakeDamage
                                        && npc.Distance(Player.Center) < 4000f)
                                    {
                                        int bossDmg = (int)(npc.life * 0.15f);
                                        npc.SimpleStrikeNPC(bossDmg, 0, false, 0, DamageClass.Default, true);
                                        npc.netUpdate = true;
                                        CombatText.NewText(npc.getRect(), Color.Purple, "厄运降临!", true);
                                        hitBoss = true;
                                        break;
                                    }
                                }
                                if (!hitBoss)
                                {
                                    foreach (NPC npc in Main.ActiveNPCs)
                                    {
                                        if (!npc.friendly && !npc.dontTakeDamage && !npc.boss
                                            && npc.lifeMax < 2000
                                            && npc.Distance(Player.Center) < 4000f)
                                        {
                                            npc.SimpleStrikeNPC(npc.lifeMax, 0, false, 0, DamageClass.Default, true);
                                            npc.netUpdate = true;
                                            CombatText.NewText(npc.getRect(), Color.Purple, "厄运降临!", true);
                                        }
                                    }
                                }
                                Main.NewText("命运大吉! 6点!", 255, 0, 255);
                                CombatText.NewText(Player.getRect(), Color.Magenta, "6", true);
                                break;
                        }
                    }
                }
            //   错误途径 (Marauder) 完整按键逻辑整合
            // 获取按键状态
            bool isShiftDown = Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift);

            if (LotMKeybinds.Marauder_Parasite.JustPressed)
            {
                // --- [Shift + P] : 领域类技能 (高序列优先) ---
                if (isShiftDown)
                {
                    // 【序列1】 时之虫领域 (Time Clock Domain)
                    if (currentMarauderSequence <= 1)
                    {
                        isTimeClockActive = !isTimeClockActive;
                        isDeceitDomainActive = false; // 开启高级领域时，自动关闭低级领域

                        if (isTimeClockActive)
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item113, Player.position); // 钟声
                            Main.NewText("时间权柄：古老壁钟已降临 (周围敌人极度减速并衰老)。", 0, 255, 255);
                        }
                        else
                        {
                            Main.NewText("壁钟虚影消散。", 150, 150, 150);
                        }
                    }
                    // 【序列3】 欺瞒领域 (Deceit Domain)
                    else if (currentMarauderSequence <= 3)
                    {
                        isDeceitDomainActive = !isDeceitDomainActive;
                        if (isDeceitDomainActive)
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, Player.position);
                            Main.NewText("欺瞒领域：现实的规则已被扭曲 (敌人混乱/弹幕偏转)。", 0, 255, 127);
                        }
                        else
                        {
                            Main.NewText("欺瞒领域：关闭。", 150, 150, 150);
                        }
                    }
                }
                // --- [单按 P] : 寄生技能 (序列4) ---
                else if (currentMarauderSequence <= 4)
                {
                    if (isParasitizing)
                    {
                        // 主动解除寄生
                        isParasitizing = false;
                        Player.velocity = new Vector2(0, -10); // 弹射出来
                        Player.immune = true;
                        Player.immuneTime = 60;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath13, Player.position);
                        Main.NewText("解除寄生状态。", 200, 200, 200);
                    }
                    else
                    {
                        // 尝试寄生鼠标指向的目标
                        int targetIndex = -1;
                        float maxDist = 300f;
                        bool foundPlayer = false; // 标记是否找到了玩家

                        // 1. 优先寻找 NPC
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            NPC n = Main.npc[i];
                            // 必须活着 + 鼠标指向 + 距离内
                            if (n.active && n.getRect().Contains(Main.MouseWorld.ToPoint()) && Player.Distance(n.Center) < maxDist)
                            {
                                targetIndex = i;
                                parasiteIsPlayer = false; // 是NPC
                                break;
                            }
                        }

                        // 2. 如果没找到 NPC，寻找 玩家 (队友)
                        if (targetIndex == -1)
                        {
                            for (int i = 0; i < Main.maxPlayers; i++)
                            {
                                Player p = Main.player[i];
                                // 必须活着 + 不是自己 + 鼠标指向 + 距离内
                                if (p.active && !p.dead && p.whoAmI != Player.whoAmI &&
                                    p.getRect().Contains(Main.MouseWorld.ToPoint()) && Player.Distance(p.Center) < maxDist)
                                {
                                    targetIndex = i;
                                    parasiteIsPlayer = true; // 是玩家
                                    foundPlayer = true;
                                    break;
                                }
                            }
                        }

                        // 3. 执行寄生
                        if (targetIndex != -1)
                        {
                            if (TryConsumeSpirituality(50))
                            {
                                isParasitizing = true;
                                parasiteTargetIndex = targetIndex;
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath13, Player.position);

                                if (foundPlayer)
                                {
                                    // 寄生玩家逻辑
                                    parasiteIsPlayer = true;
                                    parasiteIsTownNPC = false;
                                    Main.NewText($"已寄生于队友 {Main.player[targetIndex].name} 体内 (生命/灵性共享)", 100, 255, 255);
                                }
                                else
                                {
                                    // 寄生 NPC 逻辑
                                    parasiteIsPlayer = false;
                                    parasiteIsTownNPC = Main.npc[targetIndex].townNPC;
                                    if (parasiteIsTownNPC)
                                        Main.NewText($"已寄生于 {Main.npc[targetIndex].FullName} 体内 (浅层寄生/恢复伤势)", 100, 255, 100);
                                    else
                                        Main.NewText($"已深度寄生目标！(控制/生命窃取)", 255, 100, 100);
                                }
                            }
                            else Main.NewText("灵性不足，无法寄生！", 255, 50, 50);
                        }
                        else Main.NewText("未找到可寄生的目标 (NPC或队友)", 150, 150, 150);
                    }
                }
            }

            // ------------------------------------------------------------------
            // 2. O 键逻辑 (窃取 / 偷盗)
            // ------------------------------------------------------------------
            if (LotMKeybinds.Marauder_StealToggle.JustPressed && currentMarauderSequence <= 9)
            {
                // 切换状态 (开变关，关变开)
                isPassiveStealEnabled = !isPassiveStealEnabled;

                // 播放音效提示
                Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);

                // 文字提示
                if (isPassiveStealEnabled)
                {
                    Main.NewText("窃取被动：已开启", 100, 255, 100); // 绿色
                }
                else
                {
                    Main.NewText("窃取被动：已关闭", 200, 200, 200); // 灰色
                }
            }
            if (LotMKeybinds.MarauderSteal.JustPressed)
            {
                if (isShiftDown)
                {
                    // 【序列1】 窃取时间 (Time Theft)
                    if (currentMarauderSequence <= 1)
                    {
                        // 消耗保持 500，冷却时间从 10秒(600) 降低到 8秒(480)
                        if (timeTheftCooldown <= 0 && TryConsumeSpirituality(500))
                        {
                            int targetIdx = -1;
                            // 稍微扩大一点鼠标判定范围，防止点不到怪
                            for (int i = 0; i < Main.maxNPCs; i++)
                            {
                                if (Main.npc[i].active && !Main.npc[i].friendly && Main.npc[i].getRect().Intersects(Terraria.Utils.CenteredRectangle(Main.MouseWorld, new Vector2(50, 50))))
                                {
                                    targetIdx = i;
                                    break;
                                }
                            }

                            if (targetIdx != -1)
                            {
                                NPC target = Main.npc[targetIdx];

                                // === 伤害计算公式修改 ===
                                // 1. 基础伤害：固定 10000 (保证下限)
                                int baseDamage = 10000;

                                // 2. 寿命剥夺：目标当前生命的 15% (原为10%)
                                int lifeStealDamage = (int)(target.life * 0.15f);

                                int totalDamage = baseDamage + lifeStealDamage;

                                // 3. 伤害上限修正
                                if (target.boss)
                                {
                                    // Boss 伤害上限提升至 100,000 (原为 5,000)
                                    // 这样面对几十万血的 Boss 也能一刀切掉一大块
                                    if (totalDamage > 100000) totalDamage = 100000;
                                }
                                else
                                {
                                    // 对小怪：直接造成 5倍当前生命伤害 (确切的秒杀)
                                    totalDamage = target.life * 5;
                                    if (totalDamage < 20000) totalDamage = 20000;
                                }

                                // 造成伤害 (暴击)
                                Player.ApplyDamageToNPC(target, totalDamage, 0, 0, true);

                                // 自身获得强力 Buff
                                Player.AddBuff(BuffID.Panic, 600);    // 加移速
                                Player.AddBuff(BuffID.Swiftness, 600);// 加移速
                                Player.AddBuff(BuffID.ShadowDodge, 300); // 【新增】窃取了时间，获得一次神圣闪避

                                // 视觉特效
                                CombatText.NewText(target.getRect(), Color.Cyan, $"时间剥夺! -{totalDamage}", true);
                                SoundEngine.PlaySound(SoundID.Item14, target.Center); // 爆炸音效
                                for (int k = 0; k < 40; k++)
                                {
                                    Dust d = Dust.NewDustPerfect(target.Center, DustID.Vortex, Main.rand.NextVector2Circular(10, 10), 0, default, 2.5f);
                                    d.noGravity = true;
                                }

                                timeTheftCooldown = 480; // 冷却降低为 8秒
                            }
                        }
                        else if (timeTheftCooldown > 0) Main.NewText($"窃取时间冷却中: {timeTheftCooldown / 60}s", 150, 150, 150);
                    }
                    // 【序列2】 命运窃取 (Fate Theft)
                    else if (currentMarauderSequence <= 2)
                    {
                        if (fateTheftCooldown <= 0 && TryConsumeSpirituality(200))
                        {
                            int targetIdx = -1;
                            for (int i = 0; i < Main.maxNPCs; i++) { if (Main.npc[i].active && !Main.npc[i].friendly && Main.npc[i].getRect().Contains(Main.MouseWorld.ToPoint())) { targetIdx = i; break; } }

                            if (targetIdx != -1)
                            {
                                NPC target = Main.npc[targetIdx];
                                if (!target.boss)
                                {
                                    float myRatio = (float)Player.statLife / Player.statLifeMax2;
                                    float targetRatio = (float)target.life / target.lifeMax;
                                    if (myRatio < targetRatio)
                                    {
                                        int newLife = (int)(targetRatio * Player.statLifeMax2);
                                        int targetNewLife = (int)(myRatio * target.lifeMax);
                                        Player.statLife = newLife; Player.HealEffect(newLife - (int)(myRatio * Player.statLifeMax2));
                                        target.life = targetNewLife;
                                        CombatText.NewText(target.getRect(), Color.Purple, "命运互换!", true);
                                    }
                                    else Main.NewText("你的命运优于目标。", 150, 150, 150);
                                }
                                else
                                {
                                    Player.ApplyDamageToNPC(target, 5000, 0, 0, false);
                                    Player.statLife += 500; Player.HealEffect(500);
                                    CombatText.NewText(target.getRect(), Color.Purple, "窃取未来!", true);
                                }
                                fateTheftCooldown = 1200;
                            }
                        }
                        else if (fateTheftCooldown > 0) Main.NewText($"命运窃取冷却中: {fateTheftCooldown / 60}s", 150, 150, 150);
                    }
                }
                // --- [单按 O] : 基础窃取模式 (序列9) ---
                else if (currentMarauderSequence <= 9)
                {
                    stealMode = !stealMode;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                    if (stealMode) Main.NewText("窃取模式：开启 (商店免费，小心被发现...)", 255, 100, 100);
                    else Main.NewText("窃取模式：关闭", 100, 255, 100);
                }
            }
            if (LotMKeybinds.Marauder_ConceptSteal.JustPressed && currentMarauderSequence <= 9)
            {
                // 只有序列 4 (寄生者) 及以下可以使用
                if (currentMarauderSequence <= 4)
                {
                    if (conceptStealCooldown <= 0 && TryConsumeSpirituality(80))
                    {
                        int targetIdx = -1;
                        // 寻找鼠标指向的 NPC
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            if (Main.npc[i].active && !Main.npc[i].friendly && Main.npc[i].getRect().Contains(Main.MouseWorld.ToPoint()))
                            {
                                targetIdx = i;
                                break;
                            }
                        }

                        if (targetIdx != -1)
                        {
                            NPC n = Main.npc[targetIdx];
                            if (!n.boss)
                            {
                                // 窃取位置 (互换)
                                Vector2 pPos = Player.Center;
                                Player.Teleport(n.Center, 1);
                                n.Center = pPos;
                                CombatText.NewText(Player.getRect(), Color.Orange, "位置窃取!", true);
                                conceptStealCooldown = 180;
                            }
                            else
                            {
                                Main.NewText("位格过高无法窃取位置！", 255, 50, 50);
                            }
                        }
                        else
                        {
                            // 窃取距离 (传送)
                            Player.Teleport(Main.MouseWorld, 1);
                            Main.NewText("窃取距离。", 200, 200, 255);
                            conceptStealCooldown = 120;
                        }
                    }
                    else if (conceptStealCooldown > 0)
                    {
                        Main.NewText($"概念窃取冷却: {conceptStealCooldown / 60}s", 150, 150, 150);
                    }
                }
                else
                {
                    // (可选) 提示序列不够
                    Main.NewText("序列不足 (需序列4)", 150, 150, 150);
                }
            }
            // 太阳途径，歌颂者
            // 赞美太阳 ---
            if (LotMKeybinds.Sun_Sing.JustPressed && currentSunSequence <= 9)
            {
                ToggleSinging();
            }
            if (LotMKeybinds.Sun_Radiance.JustPressed && currentSunSequence <= 8)
            {
                if (currentSunSequence <= 4)
                {
                    if (sunRadianceCooldown <= 0 && TryConsumeSpirituality(100))
                    {

                        Vector2 vel = (Main.MouseWorld - Player.Center).SafeNormalize(Vector2.UnitX) * 25f; // 速度极快

                        Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, vel, ModContent.ProjectileType<Content.Projectiles.Sun.UnshadowedSpear>(), 2000, 5f, Player.whoAmI);

                        SoundEngine.PlaySound(SoundID.Item71, Player.position); // 强力的投掷音效
                    }
                    else if (sunRadianceCooldown > 0)
                    {
                        Main.NewText($"无暗之枪冷却中... ({sunRadianceCooldown / 60}s)", 150, 150, 150);
                    }
                    else
                    {
                        Main.NewText("灵性不足 (需100点)", 255, 50, 50);
                    }
                }
                else
                {
                    CastSunlight();
                }
            }
            if (LotMKeybinds.Sun_HolyLight.JustPressed && currentSunSequence <= 7)
            {
                CastHolyLight();
            }
            // G键：光明之火 (序列7)
            if (LotMKeybinds.Sun_FireOcean.JustPressed && currentSunSequence <= 7)
            {
                // 序列4：阳炎
                if (currentSunSequence <= 4)
                {
                    CastFireOcean(); // 这里面已经包含了序列4和序列7的判定逻辑
                }
                // 序列7：光明之火
                else
                {
                    CastFireOcean();
                }
            }

            // 2. 【新增】新按键：太阳使者 (序列2)
            // 请确保你在 LotMKeybinds.cs 里注册了 Sun_Messenger
            if (LotMKeybinds.Sun_Messenger.JustPressed && currentSunSequence <= 2)
            {
                isSunMessenger = !isSunMessenger;
                if (isSunMessenger)
                {
                    SoundEngine.PlaySound(SoundID.Item119, Player.position); // 神圣火焰音效
                    Main.NewText("化身太阳，播撒光热！", 255, 200, 0);

                    // 变身瞬间清空周围敌对弹幕
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if (Main.projectile[i].active && Main.projectile[i].hostile && Main.projectile[i].Distance(Player.Center) < 500f)
                        {
                            Main.projectile[i].Kill();
                        }
                    }
                }
                else
                {
                    Main.NewText("解除太阳形态。", 255, 255, 200);
                }
            }
            if (LotMKeybinds.Sun_Notarize.JustPressed && currentSunSequence <= 6)
            {
                // 【序列1：纯白之光】 (Pure White Light)
                if (currentSunSequence <= 1)
                {
                    if (notarizeCooldown <= 0 && TryConsumeSpirituality(30000))
                    {
                        notarizeCooldown = 1200; // 20秒冷却

                        Main.NewText("除了纯白与太阳，一切都将分崩离析。", 255, 255, 255);
                        SoundEngine.PlaySound(SoundID.Item29, Player.position);

                        RunSunSuppression(3000f);

                        // 基础伤害 30000 (配合倍率和Projectile内的10倍加成，是对全屏的毁灭性打击)
                        int dmg = (int)(30000 * GetSequenceMultiplier(currentSunSequence));

                        Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, ModContent.ProjectileType<Content.Projectiles.Sun.PureWhiteLight>(), dmg, 0f, Player.whoAmI);
                    }
                    else if (notarizeCooldown > 0) Main.NewText($"纯白之光冷却中... ({notarizeCooldown / 60}s)", 150, 150, 150);
                    else Main.NewText("灵性不足 (需30000点)", 255, 50, 50);
                }
                // 【序列3：正义审判】 (Justice Judgment)
                // 包含序列2的伤害倍率逻辑
                else if (currentSunSequence <= 3)
                {
                    if (notarizeCooldown <= 0 && TryConsumeSpirituality(1500))
                    {
                        notarizeCooldown = 600; // 10秒冷却

                        RunSunSuppression(2000f);

                        // --- 索敌逻辑 ---
                        int targetIdx = -1;
                        float minDst = 600f;

                        // 1. 优先检查鼠标指向的目标
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            NPC n = Main.npc[i];
                            if (n.active)
                            {
                                bool isValidTarget = !n.friendly || n.type == NPCID.Angler;
                                if (isValidTarget && n.getRect().Contains(Main.MouseWorld.ToPoint()))
                                {
                                    targetIdx = i; break;
                                }
                            }
                        }

                        // 2. 如果没指到，找鼠标附近最近的
                        if (targetIdx == -1)
                        {
                            foreach (NPC n in Main.ActiveNPCs)
                            {
                                bool isValidTarget = !n.friendly || n.type == NPCID.Angler;
                                if (isValidTarget && n.Distance(Main.MouseWorld) < minDst)
                                {
                                    minDst = n.Distance(Main.MouseWorld);
                                    targetIdx = n.whoAmI;
                                }
                            }
                        }

                        // --- 执行审判 ---
                        if (targetIdx != -1)
                        {
                            NPC target = Main.npc[targetIdx];
                            Main.NewText($"正义审判：{target.FullName}！", 255, 215, 0);
                            SoundEngine.PlaySound(SoundID.Item122, Player.position);

                            float sunMult = GetSequenceMultiplier(currentSunSequence);
                            int baseDmg = 15000;

                            // 判定是否违反核心正义 (邪恶生物)
                            bool isEvil = NPCID.Sets.Zombies[target.type] || NPCID.Sets.Skeletons[target.type] || target.boss || target.type == NPCID.Angler || target.aiStyle == 22;

                            // 序列2特权：对邪恶生物 5倍伤害，普通 3倍
                            int multiplier = 1;
                            if (currentSunSequence <= 2) multiplier = isEvil ? 5 : 3;

                            int finalDmg = (int)(baseDmg * sunMult * multiplier);

                            Player.ApplyDamageToNPC(target, finalDmg, 0, 0, true);

                            if (!target.townNPC || target.type == NPCID.Angler)
                            {
                                target.AddBuff(BuffID.Ichor, 1200);
                                target.AddBuff(BuffID.BetsysCurse, 1200);
                                target.AddBuff(BuffID.Daybreak, 600);
                                target.AddBuff(BuffID.Confused, 600);
                            }

                            Vector2 spawnPos = target.Center - new Vector2(0, 800);
                            Projectile.NewProjectile(Player.GetSource_FromThis(), spawnPos, new Vector2(0, 30f), ModContent.ProjectileType<Content.Projectiles.Sun.JusticeJudgment>(), finalDmg, 10f, Player.whoAmI);
                        }
                        else
                        {
                            Main.NewText("范围内无审判目标。", 150, 150, 150);
                            spiritualityCurrent += 1500; // 返还灵性
                            notarizeCooldown = 30;
                        }
                    }
                    else if (notarizeCooldown > 0) Main.NewText($"审判冷却中... ({notarizeCooldown / 60}s)", 150, 150, 150);
                    else Main.NewText("灵性不足 (需1500点)", 255, 50, 50);
                }
                else
                {
                    CastNotarize();
                }
            }

            // =================================================================
            // V键：神圣誓约 (序列7) / 神圣契约 (序列3) / 信仰之仆 (序列1)
            // =================================================================
            if (LotMKeybinds.Sun_Oath.JustPressed && currentSunSequence <= 7)
            {
                // 【序列1：信仰之仆】 (Servant of Faith)
                if (currentSunSequence <= 1)
                {
                    if (holyOathCooldown <= 0 && TryConsumeSpirituality(2000))
                    {
                        holyOathCooldown = 3600; // 60秒冷却

                        Main.NewText("狂热的信仰让你无所畏惧！", 255, 215, 0);
                        SoundEngine.PlaySound(SoundID.Item29, Player.position);

                        // 1. 绝对无敌 (5秒)
                        Player.SetImmuneTimeForAllTypes(300);

                        // 2. 强力Buff (20秒)
                        int duration = 1200;

                        // 【修复】BuffID.SolarInferno -> BuffID.SolarShield3 (日耀护盾满充能)
                        Player.AddBuff(BuffID.SolarShield3, duration);

                        Player.AddBuff(BuffID.Wrath, duration);
                        Player.AddBuff(BuffID.Lifeforce, duration);

                        // 3. 视觉：转化为光人
                        for (int i = 0; i < 50; i++)
                        {
                            Dust d = Dust.NewDustPerfect(Player.Center, DustID.WhiteTorch, Main.rand.NextVector2Circular(10, 10), 0, default, 3f);
                            d.noGravity = true;
                        }
                    }
                    else if (holyOathCooldown > 0) Main.NewText($"信仰冷却中... ({holyOathCooldown / 60}s)", 150, 150, 150);
                    else Main.NewText("灵性不足 (需2000点)", 255, 50, 50);
                }
                // 【序列3：神圣契约】 (Holy Contract)
                else if (currentSunSequence <= 3)
                {
                    if (holyOathCooldown <= 0 && spiritualityCurrent >= 1000)
                    {
                        TryConsumeSpirituality(1000);
                        holyOathCooldown = 3600; // 60秒冷却

                        SoundEngine.PlaySound(SoundID.Item29, Player.position);
                        SoundEngine.PlaySound(SoundID.Item4, Player.position);

                        CombatText.NewText(Player.getRect(), Color.Gold, "!!! 神圣契约已签订 !!!", true);

                        int duration = 3600;
                        Player.AddBuff(BuffID.Lifeforce, duration);
                        Player.AddBuff(BuffID.Ironskin, duration);
                        Player.AddBuff(BuffID.Endurance, duration);
                        Player.AddBuff(BuffID.RapidHealing, duration);

                        int heal = Player.statLifeMax2;
                        Player.statLife += heal;
                        Player.HealEffect(heal);

                        for (int i = 0; i < 100; i++)
                        {
                            Vector2 pos = Player.Center + new Vector2(Main.rand.Next(-50, 50), Main.rand.Next(0, 50));
                            Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame, new Vector2(0, -10), 0, default, 3f);
                            d.noGravity = true;

                            Vector2 vel = Main.rand.NextVector2Circular(10, 10);
                            Dust d2 = Dust.NewDustPerfect(Player.Center, DustID.Enchanted_Gold, vel, 0, default, 2f);
                            d2.noGravity = true;
                        }
                        Main.screenPosition += Main.rand.NextVector2Circular(15, 15);
                    }
                    else if (holyOathCooldown > 0) Main.NewText($"契约冷却中... ({holyOathCooldown / 60}s)", 150, 150, 150);
                    else Main.NewText("灵性不足以签订契约 (需1000点)！", 255, 50, 50);
                }
                // 【序列7：神圣誓约】
                else
                {
                    CastHolyOath();
                }
            }
            if (baseDemonessSequence <= 6 && LotMKeybinds.Demoness_Mirror.JustPressed)
            {
                // ★ 修改1：冷却时间大幅缩短 (如果冷却小于等于0 且 灵性足够)
                if (mirrorSubstituteCooldown <= 0 && TryConsumeSpirituality(40)) // 耗蓝降低到40
                {
                    Vector2 targetPos = Main.MouseWorld;

                    // ★ 修改2：传送距离翻倍 (450 -> 900)
                    float maxDistance = 900f;

                    // 限制距离
                    if (Player.Distance(targetPos) > maxDistance)
                    {
                        targetPos = Player.Center + (targetPos - Player.Center).SafeNormalize(Vector2.Zero) * maxDistance;
                    }

                    bool canTeleport = !Collision.SolidCollision(targetPos, Player.width, Player.height);

                    if (canTeleport)
                    {
                        // === 原地生成替身碎片 ===
                        if (Main.myPlayer == Player.whoAmI)
                        {
                            // 确保这里引用的是 DemonessMirrorShard (镜子碎片)
                            int shardType = ModContent.ProjectileType<DemonessMirrorShard>();

                            // 爆发更多碎片 (8 -> 12)
                            for (int i = 0; i < 12; i++)
                            {
                                Vector2 shardVel = Main.rand.NextVector2Circular(10, 10);
                                Projectile.NewProjectile(
                                    Player.GetSource_Misc("MirrorSubstitute"),
                                    Player.Center.X, Player.Center.Y - 10,
                                    shardVel.X, shardVel.Y,
                                    shardType,
                                    50, // 伤害提升
                                    3f,
                                    Player.whoAmI
                                );
                            }
                        }

                        // === 音效与特效 ===
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27, Player.position); // 清脆破碎声
                        for (int i = 0; i < 20; i++)
                            Dust.NewDust(Player.position, Player.width, Player.height, DustID.Glass, 0, 0, 0, default, 1.5f);

                        // === 玩家瞬移 ===
                        Player.Teleport(targetPos, 1);

                        // === 瞬移后处理 ===
                        // 给予较短的无敌帧 (0.5秒)，防止连续使用太无赖
                        Player.SetImmuneTimeForAllTypes(30);

                        // ★ 修改4：冷却时间改为 2秒 (120帧)
                        mirrorSubstituteCooldown = 120;

                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, Player.position);
                    }
                    else
                    {
                        // 如果卡墙，尝试稍微往上提一点再试一次 (优化手感)
                        targetPos.Y -= 16;
                        if (!Collision.SolidCollision(targetPos, Player.width, Player.height))
                        {
                            // 重复上面的传送逻辑 (简写)
                            Player.Teleport(targetPos, 1);
                            mirrorSubstituteCooldown = 120;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, Player.position);
                        }
                        else
                        {
                            // 只有真的完全卡住才提示
                            Main.NewText("无法在该位置重组身体！", 255, 100, 100);
                        }
                    }
                }
            }
            if (LotMKeybinds.Demoness_MirrorSwitch.JustPressed && baseDemonessSequence <= 5)
            {
                ToggleMirrorClone();
            }
            if (baseDemonessSequence <= 5) // 只有痛苦魔女及以上可用
            {
                // 技能1：魔女之发 (按 X)
                if (LotMKeybinds.Demoness_HairAttack.JustPressed)
                {
                    UseDemonessHair();
                }

                // 技能2：蛛丝操控 (按 C)
                if (LotMKeybinds.Demoness_SilkControl.JustPressed)
                {
                    UseSpiderSilk();
                }
            }
            if (baseDemonessSequence <= 4 && LotMKeybinds.Demoness_DespairSkill.JustPressed)
            {
                UseDespairIce();
            }
            if (baseDemonessSequence <= 3 && LotMKeybinds.Demoness_PetrifySkill.JustPressed)
            {
                UsePetrificationGaze();
            }
            if (baseDemonessSequence <= 2 && LotMKeybinds.Demoness_Catastrophe.JustPressed)
            {
                CastCatastrophe();
            }
            if (baseDemonessSequence <= 1 && LotMKeybinds.Demoness_Apocalypse.JustPressed)
            {
                CastApocalypse();
            }
        }
        public void UsePetrificationGaze()
        {
            // 1. 【新增】检查冷却时间
            if (PetrificationGazeCD > 0)
            {
                // 如果CD还没好，直接不执行，也不提示（防止刷屏）
                return;
            }

            int cost = 500;
            // 检查灵性
            if (spiritualityCurrent < cost)
            {
                Main.NewText("灵性不足！(需要 500)", 255, 50, 50);
                return;
            }
            spiritualityCurrent -= cost;

            // 提示文本
            Main.NewText("万物静籁，唯我不朽。", 200, 200, 200);

            // 播放音效
            SoundStyle timeStopSound = new SoundStyle("zhashi/Assets/Sounds/TimeStop")
            {
                Volume = 1.0f,
                Pitch = 0.0f,
                PitchVariance = 0f,
                MaxInstances = 1,
            };
            SoundEngine.PlaySound(timeStopSound, Player.Center);

            // 开启特效
            TimeStopScreenEffect.Activate(Player.Center);

            // 设置特效持续时间 (5秒)
            PetrificationGazeTimer = 300;

            // 【新增】设置冷却时间 (30秒 = 1800帧)
            PetrificationGazeCD = 1800;

            // 技能实际效果
            float range = 1500f;
            foreach (NPC target in Main.ActiveNPCs)
            {
                if (target.friendly || target.dontTakeDamage) continue;
                if (target.Distance(Player.Center) > range) continue;

                target.AddBuff(ModContent.BuffType<Content.Buffs.Debuffs.TimeStagnationBuff>(), 300);

                if (target.boss)
                {
                    target.AddBuff(BuffID.Ichor, 300);
                }

                for (int i = 0; i < 5; i++)
                {
                    int d = Dust.NewDust(target.position, target.width, target.height, DustID.GemDiamond, 0, 0, 100, Color.Gray, 1.0f);
                    Main.dust[d].velocity *= 0.1f;
                    Main.dust[d].noGravity = true;
                }
            }
        }

        // 辅助方法：执行镜面分身
        public void ToggleMirrorClone()
        {
            // 检查是否已经有分身
            bool alreadyHasClone = false;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == ModContent.ProjectileType<Projectiles.Demoness.MirrorCloneProjectile>() && p.owner == Player.whoAmI)
                {
                    p.Kill(); // 再次按下则关闭
                    alreadyHasClone = true;
                    Main.NewText("镜面破碎...", 150, 150, 150);
                }
            }

            if (!alreadyHasClone)
            {
                // --- 修正点：变量名改为 spiritualityCurrent ---
                int cost = 500;
                if (spiritualityCurrent < cost)
                {
                    Main.NewText("灵性不足以维持镜面世界！(需要 500)", 255, 50, 50);
                    return;
                }

                spiritualityCurrent -= cost; 

                // 生成分身
                Projectile.NewProjectile(
                    Player.GetSource_FromThis(),
                    Player.Center,
                    Microsoft.Xna.Framework.Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.Demoness.MirrorCloneProjectile>(),
                    0, // 伤害由分身自己计算，这里填0
                    0,
                    Player.whoAmI,
                    Player.Center.X // ai[0] = 镜面轴X
                );

                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Item28, Player.Center);
                Main.NewText("绝望魔女的倒影已浮现...", 200, 50, 200);
            }
        }
        public void UseDespairIce()
        {
            int cost = 80;
            if (spiritualityCurrent < cost)
            {
                Main.NewText("灵性不足！(需要 80)", 255, 50, 50);
                return;
            }
            spiritualityCurrent -= cost;

            // 向鼠标发射 5 枚黑焰冰晶 (扇形)
            Vector2 target = Main.MouseWorld;
            Vector2 direction = (target - Player.Center).SafeNormalize(Vector2.UnitX);

            int damage = 200 + (int)(Player.GetDamage(DamageClass.Magic).Additive * 50);

            // 发射 5 枚，角度扩散
            for (int i = -2; i <= 2; i++)
            {
                Vector2 velocity = direction.RotatedBy(MathHelper.ToRadians(10 * i)) * 16f;

                Projectile.NewProjectile(
                    Player.GetSource_FromThis(),
                    Player.Center,
                    velocity,
                    ModContent.ProjectileType<Projectiles.Demoness.DespairIceProjectile>(),
                    damage,
                    5f,
                    Player.whoAmI
                );
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item28, Player.Center);
        }
        public void UseDemonessHair()
        {
            // 灵性消耗：例如 50点
            int cost = 200;
            if (spiritualityCurrent < cost)
            {
                Main.NewText("灵性不足！(需要 200)", 255, 50, 50);
                return;
            }
            spiritualityCurrent -= cost;

            // 向鼠标方向发射 3 根头发
            Vector2 target = Main.MouseWorld;
            Vector2 direction = (target - Player.Center).SafeNormalize(Vector2.UnitX);

            int damage = 100 + (int)(Player.GetDamage(DamageClass.Magic).Additive * 20); // 基础伤害100 + 魔法加成

            for (int i = -1; i <= 1; i++) // 发射3根
            {
                Vector2 perturbedSpeed = direction.RotatedBy(MathHelper.ToRadians(10 * i)) * 12f; // 扇形散射

                Projectile.NewProjectile(
                    Player.GetSource_FromThis(),
                    Player.Center,
                    perturbedSpeed,
                    ModContent.ProjectileType<Projectiles.Demoness.DemonessHairProjectile>(),
                    damage,
                    3f,
                    Player.whoAmI
                );
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item39, Player.Center); // 鞭子挥舞声
        }

        public void UseSpiderSilk()
        {
            // 灵性消耗：例如 30点
            int cost = 30;
            if (spiritualityCurrent < cost)
            {
                Main.NewText("灵性不足！(需要 30)", 255, 50, 50);
                return;
            }
            spiritualityCurrent -= cost;

            // 向鼠标方向发射一团蛛丝
            Vector2 target = Main.MouseWorld;
            Vector2 velocity = (target - Player.Center).SafeNormalize(Vector2.UnitX) * 14f;

            int damage = 60 + (int)(Player.GetDamage(DamageClass.Magic).Additive * 10);

            Projectile.NewProjectile(
                Player.GetSource_FromThis(),
                Player.Center,
                velocity,
                ModContent.ProjectileType<Projectiles.Demoness.DemonessSpiderSilkProjectile>(),
                damage,
                0f, // 蛛丝主要靠控制，击退低
                Player.whoAmI
            );
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, Player.Center); // 投掷声
        }

        // 辅助方法：执行奇迹愿望
        private void CastMiracleWish()
        {
            // 消耗 2000 灵性
            if (miracleCooldown <= 0 && TryConsumeSpirituality(2000))
            {
                SoundEngine.PlaySound(SoundID.Item29, Player.position); // 奇迹音效
                miracleCooldown = 3600; // 60秒冷却

                switch (selectedWish)
                {
                    case 0: // 生命
                        Player.statLife = Player.statLifeMax2;
                        Player.HealEffect(Player.statLifeMax2);
                        for (int i = 0; i < Player.MaxBuffs; i++)
                            if (Main.debuff[Player.buffType[i]]) Player.DelBuff(i);
                        Main.NewText("奇迹：生命复苏！", 0, 255, 0);
                        break;

                    case 1: // 毁灭
                        foreach (NPC n in Main.ActiveNPCs)
                        {
                            if (!n.friendly && !n.dontTakeDamage && n.Distance(Player.Center) < 1500f)
                            {
                                Player.ApplyDamageToNPC(n, 10000, 0, 0, false); // 10000 真实伤害
                                                                                // 召唤雷电球特效
                                Projectile.NewProjectile(Player.GetSource_FromThis(), n.Center, Vector2.Zero, ProjectileID.Electrosphere, 1000, 0, Player.whoAmI);
                            }
                        }
                        Main.NewText("奇迹：毁灭天灾！", 255, 0, 0);
                        break;

                    case 2: // 传送
                        waitingForTeleport = true; // 开启地图点击传送状态
                        Main.NewText("奇迹：空间折叠已展开，请打开地图(M)或点击屏幕任意位置传送。", 0, 255, 255);
                        break;

                    case 3: // 昼夜
                        Main.time = 0;
                        Main.dayTime = !Main.dayTime;
                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendData(MessageID.WorldData); // 联机同步时间
                        Main.NewText("奇迹：昼夜更替。", 200, 200, 200);
                        break;

                    // --- 新增效果 ---

                    case 4: // 天气 (Weather)
                        if (Main.raining)
                        {
                            Main.StopRain();
                            Main.NewText("奇迹：云销雨霁。", 255, 255, 0);
                        }
                        else
                        {
                            Main.StartRain();
                            Main.NewText("奇迹：风雨如晦。", 0, 0, 255);
                        }
                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendData(MessageID.WorldData); // 同步天气
                        break;

                    case 5: // 月相 (Moon Phase)
                        Main.moonPhase = (Main.moonPhase + 1) % 8;
                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendData(MessageID.WorldData); // 同步月相
                        string moonText = Main.moonPhase == 0 ? "满月" : "月相变迁";
                        Main.NewText($"奇迹：{moonText} 已至。", 150, 150, 255);
                        break;

                    case 6: // 建筑 (Historical Building)
                            // 移除旧的，生成新的
                        for (int i = 0; i < Main.maxProjectiles; i++)
                        {
                            if (Main.projectile[i].active && Main.projectile[i].owner == Player.whoAmI &&
                                Main.projectile[i].type == ModContent.ProjectileType<Projectiles.HistoricalSceneProjectile>())
                            {
                                Main.projectile[i].Kill();
                            }
                        }
                        Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero,
                            ModContent.ProjectileType<Projectiles.HistoricalSceneProjectile>(), 0, 0, Player.whoAmI);
                        Main.NewText("奇迹：历史的投影降临于此。", 200, 200, 255);
                        break;

                    case 7: // 外貌 (Appearance - 对最近的玩家)
                        int targetIdx = -1;
                        float minDist = 800f;
                        // 寻找最近的玩家（不包括自己）
                        for (int i = 0; i < Main.maxPlayers; i++)
                        {
                            Player p = Main.player[i];
                            if (p.active && !p.dead && i != Player.whoAmI && Player.Distance(p.Center) < minDist)
                            {
                                minDist = Player.Distance(p.Center);
                                targetIdx = i;
                            }
                        }

                        if (targetIdx != -1)
                        {
                            Player target = Main.player[targetIdx];
                            // 随机施加一种变形 Buff (狼人、人鱼、隐身、石化)
                            int[] buffs = { BuffID.Werewolf, BuffID.Merfolk, BuffID.Invisibility, BuffID.Stoned };
                            int selectedBuff = buffs[Main.rand.Next(buffs.Length)];

                            target.AddBuff(selectedBuff, 3600); // 持续1分钟

                            // 发送简单的文字提示
                            string look = selectedBuff == BuffID.Werewolf ? "狼人" : selectedBuff == BuffID.Merfolk ? "人鱼" : "虚无";
                            Main.NewText($"奇迹：{target.name} 的外貌被篡改成了 {look}！", 255, 100, 200);

                            // 特效
                            for (int k = 0; k < 20; k++)
                                Dust.NewDust(target.position, target.width, target.height, DustID.Confetti, 0, 0, 0, default, 1.5f);
                        }
                        else
                        {
                            Main.NewText("附近没有可以修改的目标...", 150, 150, 150);
                            spiritualityCurrent += 2000; // 返还灵性
                            miracleCooldown = 60; // 缩短冷却
                        }
                        break;
                }
            }
            else if (miracleCooldown > 0)
            {
                Main.NewText($"奇迹冷却中: {miracleCooldown / 60}s", 150, 150, 150);
            }
            else
            {
                Main.NewText("灵性不足 (需2000点)！", 255, 50, 50);
            }
        }

        // 辅助方法：打印命运状态
        private void PrintFateState()
        {
            if (fateDisturbanceActive) Main.NewText("命运干扰: 开启", 200, 100, 255);
            else Main.NewText("命运干扰: 关闭", 150, 150, 150);
        }
        public override void PostUpdateBuffs()
        {
            if (currentMoonSequence <= 9)
            {
                if (Player.HasBuff(BuffID.Poisoned)) Player.ClearBuff(BuffID.Poisoned);
                if (Player.HasBuff(BuffID.Venom)) Player.ClearBuff(BuffID.Venom);
            }
        }

        // 8. 辅助
        public float GetSequenceMultiplier(int seq) { if (seq > 9) return 1f; return 1f + (9 - seq) * 0.3f; }
        public bool TryConsumeSpirituality(float amount, bool isMaintenance = false) { if (isCalamityGiant && !isMaintenance) return true; if (spiritualityCurrent >= amount) { spiritualityCurrent -= amount; return true; } return false; }

        // ==============================================================
        // 戏法大师 (序列8): 释放选中的戏法
        // 这些戏法奇特但不强力,符合"马戏团戏法"的设定
        // ==============================================================
        private void CastTrick(int trick)
        {
            // 每个戏法消耗少量灵性
            if (!TryConsumeSpirituality(20))
            {
                Main.NewText("灵性不足 (戏法需 20 点)", 255, 50, 50);
                return;
            }
            trickCooldown = TRICK_CD_MAX;
            Vector2 c = Player.Center;

            switch (trick)
            {
                case 0: // 闪光 - 5格内敌人致盲+混乱
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item25, c);
                    foreach (NPC n in Main.ActiveNPCs)
                    {
                        if (!n.friendly && !n.dontTakeDamage && n.Distance(c) < 80f * 5)
                        {
                            n.AddBuff(BuffID.Blackout, 240);
                            n.AddBuff(BuffID.Confused, 240);
                            n.netUpdate = true;
                        }
                    }
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        for (int k = 0; k < 40; k++)
                        {
                            Dust d = Dust.NewDustPerfect(c, DustID.WhiteTorch,
                                Main.rand.NextVector2Circular(10, 10), 0, default, 2.5f);
                            d.noGravity = true; d.fadeIn = 2f;
                        }
                    }
                    Main.NewText("◆ 闪光! 强光让敌人短暂失明。", 255, 255, 200);
                    break;

                case 1: // 黑幕 - 玩家隐身+周围变暗
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, c);
                    Player.AddBuff(BuffID.Invisibility, 240);
                    foreach (NPC n in Main.ActiveNPCs)
                    {
                        if (!n.friendly && !n.dontTakeDamage && n.Distance(c) < 80f * 6)
                        {
                            n.AddBuff(BuffID.Darkness, 300);
                            n.netUpdate = true;
                        }
                    }
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        for (int k = 0; k < 50; k++)
                        {
                            Dust d = Dust.NewDustPerfect(c + Main.rand.NextVector2Circular(120, 120),
                                DustID.Shadowflame, Vector2.Zero, 100, Color.Black, 1.8f);
                            d.noGravity = true;
                        }
                    }
                    Main.NewText("◆ 黑幕! 你隐入无光的幕布之中。", 150, 120, 180);
                    break;

                case 2: // 转移气体 - 清除自身debuff
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, c);
                    int cleared = 0;
                    for (int b = 0; b < Player.MaxBuffs; b++)
                    {
                        int bt = Player.buffType[b];
                        if (bt > 0 && Main.debuff[bt] && !BuffID.Sets.NurseCannotRemoveDebuff[bt])
                        {
                            Player.DelBuff(b); b--; cleared++;
                        }
                    }
                    Main.NewText($"◆ 转移气体! 清除了 {cleared} 个负面状态。", 150, 255, 150);
                    break;

                case 3: // 巨响 - 范围混乱+吸引仇恨
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item62, c);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, c);
                    foreach (NPC n in Main.ActiveNPCs)
                    {
                        if (!n.friendly && !n.dontTakeDamage && n.Distance(c) < 80f * 12)
                        {
                            n.AddBuff(BuffID.Confused, 120);
                            n.netUpdate = true;
                        }
                    }
                    if (Main.myPlayer == Player.whoAmI)
                        Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                            c, Vector2.UnitY, 8f, 6f, 12, 1000f));
                    Main.NewText("◆ 巨响! 震耳欲聋的声响扰乱了敌人。", 255, 200, 100);
                    break;

                case 4: // 冰冻射线 - 朝鼠标方向10格内直线冰冻
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item28, c);
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        Vector2 dir = (Main.MouseWorld - c).SafeNormalize(Vector2.UnitX);
                        Projectile.NewProjectile(Player.GetSource_FromThis(),
                            c, dir * 14f, ProjectileID.FrostBoltStaff,
                            30, 2f, Main.myPlayer);
                        // 射线视觉
                        for (int i = 0; i < 10; i++)
                        {
                            Vector2 p = c + dir * (i * 16f);
                            Dust d = Dust.NewDustPerfect(p, DustID.IceTorch, Vector2.Zero, 0, default, 1.5f);
                            d.noGravity = true;
                        }
                    }
                    // 命中范围内最近的敌人冰冻
                    foreach (NPC n in Main.ActiveNPCs)
                    {
                        if (!n.friendly && !n.dontTakeDamage && n.Distance(c) < 80f * 10)
                        {
                            n.AddBuff(BuffID.Frostburn, 300);
                            n.AddBuff(BuffID.Chilled, 300);
                            if (!n.boss) n.AddBuff(BuffID.Frozen, 90);
                            n.netUpdate = true;
                            break; // 射线只冻一个
                        }
                    }
                    Main.NewText("◆ 冰冻射线! 一道寒霜射线激射而出。", 150, 220, 255);
                    break;

                case 5: // 电击 - 给下次近战附加感电
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item93, c);
                    electricChargeTimer = 600; // 10秒内近战命中附加感电
                    Main.NewText("◆ 电击! 你的手上缠绕着电流(10秒内近战命中附加感电)。", 200, 220, 255);
                    break;

                case 6: // 造雾 - 15格雾气,敌人迷惑
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, c);
                    foreach (NPC n in Main.ActiveNPCs)
                    {
                        if (!n.friendly && !n.dontTakeDamage && n.Distance(c) < 80f * 15)
                        {
                            n.AddBuff(BuffID.Confused, 360);
                            n.netUpdate = true;
                        }
                    }
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        for (int k = 0; k < 80; k++)
                        {
                            Dust d = Dust.NewDustPerfect(c + Main.rand.NextVector2Circular(240, 200),
                                DustID.Cloud, Vector2.Zero, 80, default, 2f);
                            d.noGravity = true;
                        }
                    }
                    Main.NewText("◆ 造雾! 浓雾笼罩四周,敌人迷失方向。", 200, 200, 210);
                    break;

                case 7: // 刮风 - 面向方向推开敌人
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45, c);
                    int dirX = Player.direction;
                    foreach (NPC n in Main.ActiveNPCs)
                    {
                        if (!n.friendly && !n.dontTakeDamage && !n.boss && n.Distance(c) < 80f * 10)
                        {
                            n.velocity.X += dirX * 18f;
                            n.velocity.Y -= 4f;
                            n.netUpdate = true;
                        }
                    }
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        for (int k = 0; k < 30; k++)
                        {
                            Dust d = Dust.NewDustPerfect(c + new Vector2(0, Main.rand.NextFloat(-60, 60)),
                                DustID.Cloud, new Vector2(dirX * 12f, 0), 0, default, 1.5f);
                            d.noGravity = true;
                        }
                    }
                    Main.NewText("◆ 刮风! 七八级大风将敌人吹飞。", 200, 255, 220);
                    break;

                case 8: // 点火 - 3格内敌人着火
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, c);
                    foreach (NPC n in Main.ActiveNPCs)
                    {
                        if (!n.friendly && !n.dontTakeDamage && n.Distance(c) < 80f * 3)
                        {
                            n.AddBuff(BuffID.OnFire, 300);
                            n.netUpdate = true;
                        }
                    }
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        for (int k = 0; k < 30; k++)
                        {
                            Dust d = Dust.NewDustPerfect(c + Main.rand.NextVector2Circular(48, 48),
                                DustID.Torch, Main.rand.NextVector2Circular(2, 2), 0, default, 1.8f);
                            d.noGravity = true;
                        }
                    }
                    Main.NewText("◆ 点火! 搓动手指点燃了近处的敌人。", 255, 150, 50);
                    break;

                case 9: // 摔倒术 - 6格内敌人减速+混乱(打滑)
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item7, c);
                    foreach (NPC n in Main.ActiveNPCs)
                    {
                        if (!n.friendly && !n.dontTakeDamage && !n.boss && n.Distance(c) < 80f * 6)
                        {
                            n.AddBuff(BuffID.Slow, 300);
                            n.AddBuff(BuffID.Confused, 180);
                            n.velocity.X *= 0.3f;
                            n.netUpdate = true;
                        }
                    }
                    Main.NewText("◆ 摔倒术! 地面如涂油脂,敌人脚下打滑。", 220, 220, 180);
                    break;

                case 10: // 驱物 - 5格内弹幕弹开
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, c);
                    int pushed = 0;
                    for (int p = 0; p < Main.maxProjectiles; p++)
                    {
                        Projectile proj = Main.projectile[p];
                        if (proj.active && proj.hostile && proj.Distance(c) < 80f * 5)
                        {
                            proj.velocity = (proj.Center - c).SafeNormalize(Vector2.UnitY) * 10f;
                            proj.netUpdate = true;
                            pushed++;
                        }
                    }
                    Main.NewText($"◆ 驱物! 推开了 {pushed} 个飞来的弹幕。", 200, 200, 255);
                    break;

                default:
                    Main.NewText("这个戏法暂时无法表演。", 255, 150, 150);
                    spiritualityCurrent += 20; // 退还
                    trickCooldown = 0;
                    break;
            }
        }


        // ==============================================================
        // 命运多面骰: 延迟60帧执行效果(等动画播完)
        // ==============================================================
        public void QueuePolyhedronEffect(int faces, int result)
        {
            polyhedronDelayTimer = 60;
            polyhedronQueuedFaces = faces;
            polyhedronQueuedResult = result;
        }

        private void ExecutePolyhedronEffect()
        {
            int faces = polyhedronQueuedFaces;
            int result = polyhedronQueuedResult;
            polyhedronQueuedFaces = 0;
            polyhedronQueuedResult = 0;
            if (faces == 0) return;

            string title = $"【命运多面骰·D{faces}】 落定: {result} 点";
            Color titleColor;
            switch (faces)
            {
                case 4: titleColor = new Color(100, 160, 255); break;
                case 6: titleColor = new Color(120, 230, 130); break;
                case 8: titleColor = new Color(180, 110, 255); break;
                case 10: titleColor = new Color(255, 215, 80); break;
                case 12: titleColor = new Color(255, 140, 60); break;
                case 20: titleColor = new Color(255, 60, 80); break;
                default: titleColor = Color.White; break;
            }
            Main.NewText(title, titleColor.R, titleColor.G, titleColor.B);

            // 派发到对应面数的效果池
            switch (faces)
            {
                case 4: D4_Effect(result); break;
                case 6: D6_Effect(result); break;
                case 8: D8_Effect(result); break;
                case 10: D10_Effect(result); break;
                case 12: D12_Effect(result); break;
                case 20: D20_Effect(result); break;
            }
        }

        // === D4 四元素之骰 (蓝): 四种小型环境效果 ===
        private void D4_Effect(int r)
        {
            switch (r)
            {
                case 1: // 火: 范围1500内敌人灼烧10秒
                    foreach (NPC n in Main.ActiveNPCs)
                    {
                        if (!n.friendly && !n.dontTakeDamage && n.Distance(Player.Center) < 1500f)
                        {
                            n.AddBuff(BuffID.OnFire3, 600);
                            n.AddBuff(BuffID.Daybreak, 600);
                            n.netUpdate = true;
                        }
                    }
                    Main.NewText("◆ 元素·火 - 全场敌人被神圣火焰灼烧 10 秒", 255, 100, 50);
                    break;
                case 2: // 冰: 全场敌人冰冻5秒
                    foreach (NPC n in Main.ActiveNPCs)
                    {
                        if (!n.friendly && !n.dontTakeDamage && n.Distance(Player.Center) < 1500f && !n.boss)
                        {
                            n.AddBuff(BuffID.Frozen, 300);
                            n.AddBuff(BuffID.Chilled, 600);
                            n.netUpdate = true;
                        }
                        else if (n.boss && n.Distance(Player.Center) < 1500f)
                        {
                            n.AddBuff(BuffID.Chilled, 600); // Boss仅减速
                            n.netUpdate = true;
                        }
                    }
                    Main.NewText("◆ 元素·冰 - 全场凝固 (Boss减速)", 100, 200, 255);
                    break;
                case 3: // 雷: 8道连锁闪电随机击中敌人
                    int strikeCount = 0;
                    foreach (NPC n in Main.ActiveNPCs)
                    {
                        if (strikeCount >= 8) break;
                        if (!n.friendly && !n.dontTakeDamage && n.Distance(Player.Center) < 1500f)
                        {
                            int dmg = n.boss ? (int)(n.lifeMax * 0.03f) : (int)(n.life * 0.5f);
                            if (dmg < 200) dmg = 200;
                            n.SimpleStrikeNPC(dmg, 0, false, 0, DamageClass.Default, true);
                            n.netUpdate = true;
                            CombatText.NewText(n.getRect(), Color.Cyan, $"⚡{dmg}", true);
                            strikeCount++;
                        }
                    }
                    Main.NewText($"◆ 元素·雷 - 八道闪电连锁 ({strikeCount} 个目标)", 100, 200, 255);
                    break;
                case 4: // 毒: 全场剧毒+腐蚀
                    foreach (NPC n in Main.ActiveNPCs)
                    {
                        if (!n.friendly && !n.dontTakeDamage && n.Distance(Player.Center) < 1500f)
                        {
                            n.AddBuff(BuffID.Venom, 900);
                            n.AddBuff(BuffID.Poisoned, 900);
                            n.AddBuff(BuffID.CursedInferno, 900);
                            n.netUpdate = true;
                        }
                    }
                    Main.NewText("◆ 元素·毒 - 全场被诅咒之毒侵蚀 15 秒", 100, 255, 100);
                    break;
            }
        }

        // === D6 经典六元 (绿): 中等增益/治疗 ===
        private void D6_Effect(int r)
        {
            switch (r)
            {
                case 1: // 全队治愈
                    foreach (Player p in Main.player)
                    {
                        if (p.active && !p.dead && p.Distance(Player.Center) < 3000f)
                        {
                            int delta = p.statLifeMax2 - p.statLife;
                            if (delta > 0) { p.statLife = p.statLifeMax2; p.HealEffect(delta, true); }
                        }
                    }
                    Main.NewText("◇ 六元·治愈 - 全队回满生命", 100, 255, 100);
                    break;
                case 2: // 灵性大补
                    spiritualityCurrent = spiritualityMax;
                    Main.NewText("◇ 六元·灵涌 - 灵性回满", 200, 200, 255);
                    break;
                case 3: // 双倍金币掉落 60秒(BattleStrong + 自定义)
                    Player.AddBuff(BuffID.Heartreach, 3600);
                    Player.AddBuff(BuffID.Lifeforce, 3600);
                    Player.AddBuff(BuffID.Endurance, 3600);
                    Main.NewText("◇ 六元·守护 - 60 秒三重防御增益", 200, 200, 100);
                    break;
                case 4: // 攻击狂热 60秒
                    Player.AddBuff(BuffID.Rage, 3600);
                    Player.AddBuff(BuffID.Wrath, 3600);
                    Player.AddBuff(BuffID.AmmoBox, 3600);
                    Main.NewText("◇ 六元·狂热 - 60 秒三重攻击增益", 255, 100, 100);
                    break;
                case 5: // 全队加速
                    foreach (Player p in Main.player)
                    {
                        if (p.active && !p.dead && p.Distance(Player.Center) < 3000f)
                        {
                            p.AddBuff(BuffID.Swiftness, 3600);
                            p.AddBuff(BuffID.Panic, 3600);
                        }
                    }
                    Main.NewText("◇ 六元·疾风 - 全队加速 60 秒", 200, 255, 200);
                    break;
                case 6: // 范围真伤
                    foreach (NPC n in Main.ActiveNPCs)
                    {
                        if (!n.friendly && !n.dontTakeDamage && n.Distance(Player.Center) < 1500f)
                        {
                            int dmg = n.boss ? (int)(n.lifeMax * 0.05f) : (int)(n.life * 0.6f);
                            n.SimpleStrikeNPC(dmg, 0, false, 0, DamageClass.Default, true);
                            n.netUpdate = true;
                        }
                    }
                    Main.NewText("◇ 六元·裁决 - 范围内敌人受重创", 255, 200, 100);
                    break;
            }
        }

        // === D8 命运之骰 (紫): 中等概率事件 ===
        private void D8_Effect(int r)
        {
            switch (r)
            {
                case 1:
                case 2:
                    // 全场敌人受 10% 当前血真伤 + 4 种 Debuff
                    foreach (NPC n in Main.ActiveNPCs)
                    {
                        if (!n.friendly && !n.dontTakeDamage && n.Distance(Player.Center) < 1500f)
                        {
                            int dmg = (int)(n.life * 0.10f);
                            n.SimpleStrikeNPC(dmg, 0, false, 0, DamageClass.Default, true);
                            n.AddBuff(BuffID.Ichor, 1800);
                            n.AddBuff(BuffID.BetsysCurse, 1800);
                            n.AddBuff(BuffID.OnFire3, 1800);
                            n.AddBuff(BuffID.Confused, 1800);
                            n.netUpdate = true;
                        }
                    }
                    Main.NewText("◈ 命运·撕裂 - 全场10%血量真伤+重度厄运", 200, 100, 255);
                    break;
                case 3:
                case 4:
                    // 玩家暴击爆表 30秒
                    Player.AddBuff(BuffID.Wrath, 1800);
                    Player.AddBuff(BuffID.Rage, 1800);
                    Player.AddBuff(BuffID.Sharpened, 1800);
                    Player.AddBuff(BuffID.MagicPower, 1800);
                    Player.AddBuff(BuffID.AmmoBox, 1800);
                    Main.NewText("◈ 命运·精准 - 30 秒全攻击Buff", 255, 180, 100);
                    break;
                case 5:
                case 6:
                    // 全队疾风+无敌3秒
                    foreach (Player p in Main.player)
                        if (p.active && !p.dead && p.Distance(Player.Center) < 3000f)
                        {
                            p.AddBuff(BuffID.Swiftness, 1800);
                            p.AddBuff(BuffID.Panic, 180);
                            p.immune = true;
                            p.immuneTime = 180; // 3秒无敌
                        }
                    Main.NewText("◈ 命运·闪光 - 全队疾速 + 3 秒无敌", 200, 200, 255);
                    break;
                case 7:
                case 8:
                    // Boss削15%, 非Boss秒杀
                    foreach (NPC n in Main.ActiveNPCs)
                    {
                        if (!n.friendly && !n.dontTakeDamage && n.Distance(Player.Center) < 3000f)
                        {
                            int dmg = n.boss ? (int)(n.life * 0.15f) : n.life;
                            n.SimpleStrikeNPC(dmg, 0, false, 0, DamageClass.Default, true);
                            n.netUpdate = true;
                            CombatText.NewText(n.getRect(), Color.Purple, n.boss ? "命运伤痕" : "湮灭", true);
                        }
                    }
                    Main.NewText("◈ 命运·终结 - Boss削15%血，非Boss湮灭", 200, 100, 255);
                    break;
            }
        }

        // === D10 命运十契 (金): 强力 buff + 精英伤害 ===
        private void D10_Effect(int r)
        {
            if (r <= 3)
            {
                Player.AddBuff(BuffID.Endurance, 5400);
                Player.AddBuff(BuffID.Ironskin, 5400);
                Player.AddBuff(BuffID.Lifeforce, 5400);
                Player.AddBuff(BuffID.Regeneration, 5400);
                Player.AddBuff(BuffID.Wrath, 5400);
                Player.AddBuff(BuffID.Rage, 5400);
                Player.AddBuff(BuffID.Swiftness, 5400);
                Player.AddBuff(BuffID.Heartreach, 5400);
                Main.NewText($"♦ 十契·{r} - 90 秒八重强力Buff", 255, 215, 80);
            }
            else if (r <= 7)
            {
                foreach (NPC n in Main.ActiveNPCs)
                {
                    if (!n.friendly && !n.dontTakeDamage && n.Distance(Player.Center) < 2000f)
                    {
                        int dmg = n.boss ? (int)(n.life * 0.20f) : (int)(n.life * 0.80f);
                        n.SimpleStrikeNPC(dmg, 0, false, 0, DamageClass.Default, true);
                        n.netUpdate = true;
                        CombatText.NewText(n.getRect(), Color.Gold, $"-{dmg}", true);
                    }
                }
                Main.NewText($"♦ 十契·{r} - 全场重创 (Boss削20%, 普通80%)", 255, 215, 80);
            }
            else
            {
                foreach (NPC n in Main.ActiveNPCs)
                {
                    if (!n.friendly && !n.dontTakeDamage && n.Distance(Player.Center) < 2500f)
                    {
                        int dmg = n.boss ? (int)(n.life * 0.30f) : n.life;
                        n.SimpleStrikeNPC(dmg, 0, false, 0, DamageClass.Default, true);
                        n.netUpdate = true;
                        CombatText.NewText(n.getRect(), Color.Yellow, n.boss ? "命运刻印" : "蒸发", true);
                    }
                }
                Main.NewText($"♦ 十契·{r} - 命运的判决 (Boss削30%, 非Boss蒸发)", 255, 230, 100);
            }
        }

        // === D12 黄道十二宫 (橙): 每一面对应一个星座的独特效果 ===
        private void D12_Effect(int r)
        {
            string[] zodiacNames = { "白羊", "金牛", "双子", "巨蟹", "狮子", "处女", "天秤", "天蝎", "射手", "摩羯", "水瓶", "双鱼" };
            string sign = zodiacNames[r - 1];
            switch (r)
            {
                case 1: // 白羊·冲锋
                    Player.AddBuff(BuffID.Swiftness, 5400);
                    Player.AddBuff(BuffID.Wrath, 5400);
                    Player.AddBuff(BuffID.Rage, 5400);
                    break;
                case 2: // 金牛·坚韧
                    Player.AddBuff(BuffID.Ironskin, 5400);
                    Player.AddBuff(BuffID.Endurance, 5400);
                    Player.AddBuff(BuffID.Lifeforce, 5400);
                    break;
                case 3: // 双子·分身
                    Player.AddBuff(BuffID.Summoning, 5400);
                    Player.AddBuff(BuffID.BeetleEndurance3, 5400);
                    break;
                case 4: // 巨蟹·庇护
                    foreach (Player p in Main.player)
                        if (p.active && !p.dead && p.Distance(Player.Center) < 3000f)
                        {
                            p.immune = true; p.immuneTime = 360;
                            p.AddBuff(BuffID.Endurance, 3600);
                            p.statLife = p.statLifeMax2;
                            p.HealEffect(0);
                        }
                    break;
                case 5: // 狮子·王威
                    foreach (NPC n in Main.ActiveNPCs)
                        if (!n.friendly && !n.dontTakeDamage && n.Distance(Player.Center) < 2500f)
                        {
                            n.AddBuff(BuffID.Weak, 3600);
                            n.AddBuff(BuffID.BrokenArmor, 3600);
                            n.AddBuff(BuffID.WitheredArmor, 3600);
                            n.AddBuff(BuffID.WitheredWeapon, 3600);
                            n.netUpdate = true;
                        }
                    break;
                case 6: // 处女·净化
                    for (int b = 0; b < Player.MaxBuffs; b++)
                    {
                        int bt = Player.buffType[b];
                        if (bt > 0 && Main.debuff[bt]) Player.DelBuff(b);
                    }
                    foreach (NPC n in Main.ActiveNPCs)
                        if (!n.friendly && !n.dontTakeDamage && n.Distance(Player.Center) < 2500f)
                        {
                            for (int b = 0; b < NPC.maxBuffs; b++) n.buffTime[b] = 0;
                            n.netUpdate = true;
                        }
                    break;
                case 7: // 天秤·平衡
                    NPC closest = null;
                    float minDist = 1000f;
                    foreach (NPC n in Main.ActiveNPCs)
                    {
                        if (n.friendly || n.dontTakeDamage) continue;
                        float d = n.Distance(Player.Center);
                        if (d < minDist) { minDist = d; closest = n; }
                    }
                    if (closest != null && !closest.boss)
                    {
                        int nLife = closest.life;
                        closest.life = Math.Min(Player.statLife, closest.lifeMax);
                        Player.statLife = Math.Min(nLife, Player.statLifeMax2);
                        closest.netUpdate = true;
                    }
                    else { Player.statLife = Player.statLifeMax2; Player.HealEffect(0); }
                    break;
                case 8: // 天蝎·剧毒
                    foreach (NPC n in Main.ActiveNPCs)
                        if (!n.friendly && !n.dontTakeDamage && n.Distance(Player.Center) < 2500f)
                        {
                            n.AddBuff(BuffID.Venom, 5400);
                            n.AddBuff(BuffID.ShadowFlame, 5400);
                            n.AddBuff(BuffID.CursedInferno, 5400);
                            n.netUpdate = true;
                        }
                    break;
                case 9: // 射手·穿透
                    foreach (NPC n in Main.ActiveNPCs)
                        if (!n.friendly && !n.dontTakeDamage && n.Distance(Player.Center) < 2500f)
                        {
                            int dmg = (int)(n.life * 0.20f);
                            n.SimpleStrikeNPC(dmg, 0, false, 0, DamageClass.Default, true);
                            n.netUpdate = true;
                        }
                    break;
                case 10: // 摩羯·攀升
                    Player.AddBuff(BuffID.WellFed3, 7200);
                    Player.AddBuff(BuffID.Wrath, 7200);
                    Player.AddBuff(BuffID.Rage, 7200);
                    Player.AddBuff(BuffID.Endurance, 7200);
                    Player.AddBuff(BuffID.Lifeforce, 7200);
                    Player.AddBuff(BuffID.Ironskin, 7200);
                    Player.AddBuff(BuffID.Regeneration, 7200);
                    Player.AddBuff(BuffID.Swiftness, 7200);
                    Player.AddBuff(BuffID.Heartreach, 7200);
                    Player.AddBuff(BuffID.AmmoBox, 7200);
                    break;
                case 11: // 水瓶·恩泽
                    foreach (Player p in Main.player)
                        if (p.active && !p.dead && p.Distance(Player.Center) < 3000f)
                        {
                            p.statLife = p.statLifeMax2; p.HealEffect(p.statLifeMax2, true);
                            var pmp = p.GetModPlayer<LotMPlayer>();
                            pmp.spiritualityCurrent = pmp.spiritualityMax;
                        }
                    break;
                case 12: // 双鱼·梦境
                    foreach (NPC n in Main.ActiveNPCs)
                        if (!n.friendly && !n.dontTakeDamage && n.Distance(Player.Center) < 2500f)
                        {
                            n.AddBuff(BuffID.Confused, 3600);
                            n.AddBuff(BuffID.Slow, 3600);
                            n.AddBuff(BuffID.Webbed, 600);
                            n.netUpdate = true;
                        }
                    break;
            }
            Main.NewText($"♚ 十二宫·{sign} ({r}) - 星座之力降临", 255, 140, 60);

            // 【连星图视觉特效已在此处被安全移除】
        }

        // === D20 致命二十面 (红): 史诗效果 ===
        private void D20_Effect(int r)
        {
            if (r == 1)
            {
                Player.statLife = Math.Max(1, Player.statLife / 4);
                Player.AddBuff(BuffID.Wrath, 600);
                Player.AddBuff(BuffID.Rage, 600);
                Player.AddBuff(BuffID.AmmoBox, 600);
                Main.NewText("★ 致命暴击·1 - 代价是你自己的血肉，但命运将怒火借给你 10 秒", 255, 50, 50);
            }
            else if (r == 20)
            {
                foreach (NPC n in Main.ActiveNPCs)
                {
                    if (!n.friendly && !n.dontTakeDamage)
                    {
                        int dmg = n.boss ? (int)(n.life * 0.50f) : n.life;
                        n.SimpleStrikeNPC(dmg, 0, false, 0, DamageClass.Default, true);
                        n.netUpdate = true;
                        CombatText.NewText(n.getRect(), Color.Red, n.boss ? "命运一击!" : "终焉", true);
                    }
                }
                foreach (Player p in Main.player)
                {
                    if (p.active && !p.dead && p.Distance(Player.Center) < 6000f)
                    {
                        p.statLife = p.statLifeMax2;
                        p.HealEffect(p.statLifeMax2, true);
                        var pmp = p.GetModPlayer<LotMPlayer>();
                        pmp.spiritualityCurrent = pmp.spiritualityMax;
                    }
                }
                Main.NewText("★★★ 完美二十·20 - 命运裁决:全场敌人受最高判决,全队彻底恢复 ★★★", 255, 60, 80);
            }
            else if (r <= 5)
            {
                Player.statLife = Math.Max(1, Player.statLife - 100);
                Player.AddBuff(BuffID.Wrath, 5400);
                Player.AddBuff(BuffID.Rage, 5400);
                Player.AddBuff(BuffID.Endurance, 5400);
                Player.AddBuff(BuffID.Ironskin, 5400);
                Main.NewText($"★ 二十·{r} - 代价100血,90秒强化", 255, 100, 100);
            }
            else if (r <= 15)
            {
                foreach (NPC n in Main.ActiveNPCs)
                {
                    if (!n.friendly && !n.dontTakeDamage && n.Distance(Player.Center) < 3000f)
                    {
                        int dmg = (int)(n.life * 0.25f);
                        n.SimpleStrikeNPC(dmg, 0, false, 0, DamageClass.Default, true);
                        n.netUpdate = true;
                    }
                }
                Main.NewText($"★ 二十·{r} - 全场25%血量重创", 255, 80, 80);
            }
            else
            {
                foreach (NPC n in Main.ActiveNPCs)
                {
                    if (!n.friendly && !n.dontTakeDamage && n.Distance(Player.Center) < 4000f)
                    {
                        int dmg = n.boss ? (int)(n.life * 0.30f) : n.life;
                        n.SimpleStrikeNPC(dmg, 0, false, 0, DamageClass.Default, true);
                        n.netUpdate = true;
                    }
                }
                Main.NewText($"★ 二十·{r} - Boss削30%, 非Boss湮灭", 255, 60, 60);
            }
        }
        public override void ModifyScreenPosition() { if (shakeTime > 0) { Main.screenPosition += Main.rand.NextVector2Circular(shakePower, shakePower); shakeTime--; }
            if (screenShakeMagnitude > 0f)
            {
                Main.screenPosition += Main.rand.NextVector2Circular(screenShakeMagnitude, screenShakeMagnitude);
                screenShakeMagnitude *= 0.9f; // 逐渐衰减
                if (screenShakeMagnitude < 0.1f) screenShakeMagnitude = 0f;
            }
            // 命运启示: 屏幕浮动 (命运长河水波感)
            if (revelationActiveTimer > 0 && Player.whoAmI == Main.myPlayer)
            {
                Main.screenPosition.Y += (float)System.Math.Sin(Main.GameUpdateCount / 12f) * 4f;
                Main.screenPosition.X += (float)System.Math.Cos(Main.GameUpdateCount / 14f) * 3f;
            }
            base.ModifyScreenPosition(); }
        public override bool CanUseItem(Item item) { if (isFireForm || isGuardianStance || isMoonlightized || isBatSwarm) return false; return base.CanUseItem(item); }
        private void CalculateMaxSpirituality()
        {
            // 基础灵性
            int max = 100;

            // 1. 愚者途径
            if (currentFoolSequence <= 9) max = Math.Max(max, 200);
            if (currentFoolSequence <= 8) max = Math.Max(max, 300);
            if (currentFoolSequence <= 7) max = Math.Max(max, 500);
            if (currentFoolSequence <= 6) max = Math.Max(max, 1000);
            if (currentFoolSequence <= 4) max = Math.Max(max, 5000);
            if (currentFoolSequence <= 3) max = Math.Max(max, 20000);
            if (currentFoolSequence <= 2) max = Math.Max(max, 100000);
            if (currentFoolSequence <= 1) max = Math.Max(max, 200000);

            // 2.月亮途径
            if (currentMoonSequence <= 9) max = Math.Max(max, 150); 
            if (currentMoonSequence <= 8) max = Math.Max(max, 250);
            if (currentMoonSequence <= 7) max = Math.Max(max, 600); 
            if (currentMoonSequence <= 6) max = Math.Max(max, 1000);
            if (currentMoonSequence <= 5) max = Math.Max(max, 2000);
            if (currentMoonSequence <= 4) max = Math.Max(max, 5000);
            if (currentMoonSequence <= 3) max = Math.Max(max, 10000);
            if (currentMoonSequence <= 2) max = Math.Max(max, 50000);
            if (currentMoonSequence <= 1) max = Math.Max(max, 100000);

            // 3.猎人途径
            if (currentHunterSequence <= 9) max = Math.Max(max, 120);
            if (currentHunterSequence <= 8) max = Math.Max(max, 150);
            if (currentHunterSequence <= 7) max = Math.Max(max, 300); 
            if (currentHunterSequence <= 6) max = Math.Max(max, 500);
            if (currentHunterSequence <= 5) max = Math.Max(max, 800);
            if (currentHunterSequence <= 4) max = Math.Max(max, 1500);
            if (currentHunterSequence <= 3) max = Math.Max(max, 3000);
            if (currentHunterSequence <= 2) max = Math.Max(max, 10000); 
            if (currentHunterSequence <= 1) max = Math.Max(max, 100000); 

            //4. 巨人途径
            if (currentSequence <= 9) max = Math.Max(max, 100);
            if (currentSequence <= 8) max = Math.Max(max, 150);
            if (currentSequence <= 7) max = Math.Max(max, 250);
            if (currentSequence <= 6) max = Math.Max(max, 400); 
            if (currentSequence <= 5) max = Math.Max(max, 700);
            if (currentSequence <= 4) max = Math.Max(max, 1200);
            if (currentSequence <= 3) max = Math.Max(max, 3000); 
            if (currentSequence <= 2) max = Math.Max(max, 10000); 
            if (currentSequence <= 1) max = Math.Max(max, 50000);

            //5.错误途径
            if (currentMarauderSequence <= 9) max = Math.Max(max, 100);
            if (currentMarauderSequence <= 8) max = Math.Max(max, 200);
            if (currentMarauderSequence <= 7) max = Math.Max(max, 500);
            if (currentMarauderSequence <= 6) max = Math.Max(max, 1000);
            if (currentMarauderSequence <= 5) max = Math.Max(max, 2000);
            if (currentMarauderSequence <= 4) max = Math.Max(max, 5000);
            if (currentMarauderSequence <= 3) max = Math.Max(max, 10000);
            if (currentMarauderSequence <= 2) max = Math.Max(max, 50000);
            if (currentMarauderSequence <= 1) max = Math.Max(max, 100000);

            //6.太阳途径
            if (currentSunSequence <= 9) max = Math.Max(max, 100);
            if (currentSunSequence <= 8) max = Math.Max(max, 200);
            if (currentSunSequence <= 7) max = Math.Max(max, 500);
            if (currentSunSequence <= 6) max = Math.Max(max, 1000);
            if (currentSunSequence <= 5) max = Math.Max(max, 2000);
            if (currentSunSequence <= 4) max = Math.Max(max, 5000);
            if (currentSunSequence <= 3) max = Math.Max(max, 10000);
            if (currentSunSequence <= 2) max = Math.Max(max, 20000);
            if (currentSunSequence <= 1) max = Math.Max(max, 60000);

            //7.魔女途径
            if (currentDemonessSequence <= 9) max = Math.Max(max, 150);
            if (currentDemonessSequence <= 8) max = Math.Max(max, 250);
            if (currentDemonessSequence <= 7) max = Math.Max(max, 500);
            if (currentDemonessSequence <= 6) max = Math.Max(max, 1000);
            if (currentDemonessSequence <= 5) max = Math.Max(max, 2000);
            if (currentDemonessSequence <= 4) max = Math.Max(max, 5000);
            if (currentDemonessSequence <= 3) max = Math.Max(max, 10000);
            if (currentDemonessSequence <= 2) max = Math.Max(max, 20000);
            if (currentDemonessSequence <= 1) max = Math.Max(max, 60000);

            //8.命运途径
            if (currentWheelSequence <= 9) max = Math.Max(max, 200);
            if (currentWheelSequence <= 8) max = Math.Max(max, 400);
            if (currentWheelSequence <= 7) max = Math.Max(max, 600);
            if (currentWheelSequence <= 6) max = Math.Max(max, 1000);
            if (currentWheelSequence <= 5) max = Math.Max(max, 2000);
            if (currentWheelSequence <= 4) max = Math.Max(max, 5000);
            if (currentWheelSequence <= 3) max = Math.Max(max, 10000);
            if (currentWheelSequence <= 2) max = Math.Max(max, 20000);
            if (currentWheelSequence <= 1) max = Math.Max(max, 60000);

            //9.学徒/门途径 (与愚者一致)
            if (currentDoorSequence <= 9) max = Math.Max(max, 200);
            if (currentDoorSequence <= 8) max = Math.Max(max, 300);
            if (currentDoorSequence <= 7) max = Math.Max(max, 500);
            if (currentDoorSequence <= 6) max = Math.Max(max, 1000);
            if (currentDoorSequence <= 4) max = Math.Max(max, 5000);
            if (currentDoorSequence <= 3) max = Math.Max(max, 20000);
            if (currentDoorSequence <= 2) max = Math.Max(max, 100000);
            if (currentDoorSequence <= 1) max = Math.Max(max, 200000);



            spiritualityMax = max;
        }
        private void HandleSpiritualityRegen()
        {
            spiritualityRegenTimer++;
            if (spiritualityRegenTimer >= 60) // 每秒触发一次
            {
                spiritualityRegenTimer = 0;

                // 基础回复：2 + 1% 最大灵性
                float regen = 2f + (spiritualityMax * 0.01f);

                // [新增] 错误途径特权：更快的灵性回复 (每提升1个序列，回复速度+5%)
                if (currentMarauderSequence <= 9)
                {
                    float marauderBonus = 1f + (9 - currentMarauderSequence) * 0.05f;
                    regen *= marauderBonus;
                }

                // 月亮途径高序列回复加成
                if (currentMoonSequence <= 2) regen += (spiritualityMax * 0.04f);

                spiritualityCurrent += regen;

                // 确保不超过上限
                if (spiritualityCurrent > spiritualityMax) spiritualityCurrent = spiritualityMax;
            }
        }
        private void HandleDawnArmorLogic() { if (dawnArmorBroken) { dawnArmorCooldownTimer--; if (dawnArmorCooldownTimer <= 0) { dawnArmorBroken = false; dawnArmorCurrentHP = MaxDawnArmorHP; Main.NewText("铠甲已重铸", 100, 255, 100); } } else if (!dawnArmorActive && dawnArmorCurrentHP < MaxDawnArmorHP && Main.GameUpdateCount % 2 == 0) dawnArmorCurrentHP++; }
        private void SpawnVisualDust() { for (int i = 0; i < 40; i++) Dust.NewDustPerfect(Player.Center, DustID.GoldFlame, Main.rand.NextVector2Circular(5f, 5f), 100, default, 2.0f).noGravity = true; }
        private void CheckConquerorRitual() { if (currentHunterSequence == 2 && !conquerorRitualComplete && ConquerorSpawnSystem.StopSpawning) { bool enemyExists = false; for (int i = 0; i < Main.maxNPCs; i++) { NPC npc = Main.npc[i]; if (npc.active && !npc.friendly && !npc.townNPC && npc.lifeMax > 5 && !npc.dontTakeDamage) { enemyExists = true; break; } } if (!enemyExists) { conquerorRitualComplete = true; Main.NewText("这片大陆已无敌手... 征服的意志已达成！", 255, 0, 0); SoundEngine.PlaySound(SoundID.Roar, Player.position); } } }

        public override void PostUpdate()
        {
            UpdateParasiteLogic();

            // ----------------------------------------------------
            // 时停特效自动关闭逻辑
            // ----------------------------------------------------
            if (PetrificationGazeTimer > 0)
            {
                PetrificationGazeTimer--; 
                if (PetrificationGazeTimer <= 0 || Player.dead || !Player.active)
                {
                    PetrificationGazeTimer = 0;
                    TimeStopScreenEffect.Deactivate();
                }
            }

            if (canUseWitchBroom &&
                Terraria.GameInput.PlayerInput.Triggers.JustPressed.QuickMount &&
                !Player.mount.Active &&
                !wasMountedBeforeUpdate)
            {
                Player.mount.SetMount(MountID.WitchBroom, Player);
            }

            base.PostUpdate();
        }

        private void UpdateParasiteLogic()
        {
            if (!isParasitizing || parasiteTargetIndex == -1)
                return;
            if (parasiteIsPlayer)
            {
                if (parasiteTargetIndex >= Main.maxPlayers) return; // 防止索引越界
                Player targetPlayer = Main.player[parasiteTargetIndex];

                if (!targetPlayer.active || targetPlayer.dead)
                {
                    EndParasiteState();
                    Main.NewText("宿主已死亡或断开连接。", 255, 50, 50);
                    return;
                }

                Player.Center = targetPlayer.Center;
                Player.velocity = targetPlayer.velocity;
                Player.gfxOffY = 0;
                Player.direction = targetPlayer.direction; // 朝向跟随

                Player.immune = true;
                Player.immuneTime = 2;
                Player.invis = true;
                Player.controlLeft = false; Player.controlRight = false;
                Player.controlUp = false; Player.controlDown = false;
                Player.controlJump = false; Player.controlUseItem = false;

                if (!TryConsumeSpirituality(0.5f / 60f, true))
                {
                    isParasitizing = false;
                    Main.NewText("灵性耗尽，寄生中断！", 255, 50, 50);
                    return;
                }

                Player.lifeRegen += 5;
                targetPlayer.lifeRegen += 5;

                if (Main.GameUpdateCount % 60 == 0)
                {
                    CombatText.NewText(targetPlayer.getRect(), Color.LightGreen, "寄生治疗", false, true);
                }

                return; // 结束方法，不执行下面的 NPC 逻辑
            }
            if (parasiteTargetIndex >= Main.maxNPCs) return;
            NPC target = Main.npc[parasiteTargetIndex];

            // 1. 安全检查
            if (!target.active || target.life <= 0)
            {
                EndParasiteState();
                return;
            }

            // 2. 防脱战 (Boss)
            if (target.boss || target.type == NPCID.EaterofWorldsHead)
            {
                //target.target = Player.whoAmI;
                target.timeLeft = 1000;
            }

            // 3. 锁定位置
            Player.Center = target.Center + new Vector2(0, 2);
            Player.velocity = target.velocity;
            Player.gfxOffY = 0;

            // 4. 自身状态
            Player.immune = true;
            Player.immuneTime = 2;
            Player.invis = true;
            Player.controlLeft = false; Player.controlRight = false;
            Player.controlUp = false; Player.controlDown = false;
            Player.controlJump = false; Player.controlUseItem = false;

            // 5. 消耗与效果
            float cost = parasiteIsTownNPC ? 0.5f : 5.0f;
            if (!TryConsumeSpirituality(cost / 60f, true))
            {
                isParasitizing = false;
                Main.NewText("灵性耗尽，寄生中断！", 255, 50, 50);
                return;
            }

            if (parasiteIsTownNPC)
            {
                // 浅层寄生逻辑
                Player.lifeRegen += 10;

                // 序列3 仪式
                if (currentMarauderSequence == 3)
                {
                    trojanRitualTimer++;
                    if (trojanRitualTimer % 1800 == 0) Main.NewText($"正在编织命运... ({trojanRitualTimer / 60}s / 300s)", 150, 150, 255);
                    if (trojanRitualTimer == TROJAN_RITUAL_TARGET) { Main.NewText("仪式完成！", 0, 255, 255); Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, Player.position); }
                }
            }
            else
            {
                // 深层寄生逻辑 (伤害)
                int frequency = 60;
                if (currentMarauderSequence <= 3) frequency = 45;
                if (currentMarauderSequence <= 2) frequency = 30;
                if (currentMarauderSequence <= 1) frequency = 15;

                if (Main.GameUpdateCount % frequency == 0)
                {
                    int baseDmg = 150;
                    float seqMult = 1f;
                    if (currentMarauderSequence <= 3) seqMult = 3f;
                    if (currentMarauderSequence <= 2) seqMult = 6f;
                    if (currentMarauderSequence <= 1) seqMult = 12f;

                    int finalDamage = (int)(Player.GetDamage(DamageClass.Generic).ApplyTo(baseDmg) * seqMult);

                    if (target.boss)
                    {
                        float percent = 0.001f;
                        if (currentMarauderSequence <= 2) percent = 0.002f;
                        if (currentMarauderSequence <= 1) percent = 0.005f;
                        int bonus = (int)(target.lifeMax * percent);
                        int cap = 2000 * (5 - currentMarauderSequence);
                        if (bonus > cap) bonus = cap;
                        finalDamage += bonus;
                    }

                    bool crit = currentMarauderSequence <= 2;
                    Player.ApplyDamageToNPC(target, finalDamage, 0, 0, crit);

                    int heal = 5 + (4 - currentMarauderSequence) * 5;
                    if (currentMarauderSequence <= 1) heal += finalDamage / 1000;
                    Player.statLife += heal;
                    Player.HealEffect(heal);

                    target.AddBuff(BuffID.Confused, 120);
                    target.AddBuff(BuffID.Slow, 120);
                    if (currentMarauderSequence <= 3) { target.AddBuff(BuffID.ShadowFlame, 120); target.AddBuff(BuffID.Venom, 120); }

                    CombatText.NewText(target.getRect(), Color.MediumPurple, $"-{finalDamage}", false, true);
                }
            }
        }

        public void EndParasiteState()
        {
            isParasitizing = false;
            parasiteTargetIndex = -1;
            Player.invis = false;
            Player.immune = false;
            Player.velocity = new Vector2(0, -5f);
            Main.NewText("你解除了寄生状态。", 200, 200, 255);
        }
        private void ToggleSinging()
        {
            if (!isSinging)
            {
                isSinging = true;
                singTimer = 10440; // 2.54分钟
                singingSoundSlot = SoundEngine.PlaySound(BardSongStyle);

                Main.NewText("赞美太阳！", 255, 215, 0);
            }
            else
            {
                isSinging = false;
                singTimer = 0;
                if (SoundEngine.TryGetActiveSound(singingSoundSlot, out var sound))
                {
                    sound.Stop();
                }

                Main.NewText("歌声渐息...", 200, 200, 200);
            }
        }
        private void CastSunlight()
        {
            // 冷却时间：4秒
            if (sunRadianceCooldown <= 0 && TryConsumeSpirituality(60))
            {
                sunRadianceCooldown = 240;

                SoundEngine.PlaySound(SoundID.Item14, Player.position);
                SoundEngine.PlaySound(SoundID.Item105, Player.position);
                SoundEngine.PlaySound(SoundID.DD2_BetsyFlameBreath, Player.position);
                Main.NewText("烈阳！！！", 255, 200, 0);

                shakeTime = 20;
                shakePower = 8f;
                Vector2 center = Player.Center;

                for (int i = 0; i < 80; i++)
                {
                    Dust d = Dust.NewDustPerfect(center, DustID.GoldFlame, Main.rand.NextVector2Circular(20f, 20f), 0, default, 4.0f);
                    d.noGravity = true;
                }
                for (int i = 0; i < 360; i += 3)
                {
                    Vector2 vel = MathHelper.ToRadians(i).ToRotationVector2() * 25f;
                    Dust.NewDustPerfect(center, DustID.SolarFlare, vel, 0, default, 2.5f).noGravity = true;
                }
                for (int i = 0; i < 60; i++)
                {
                    Vector2 pos = center + new Vector2(Main.rand.NextFloat(-100, 100), 50);
                    Dust d = Dust.NewDustPerfect(pos, DustID.Enchanted_Gold, new Vector2(0, Main.rand.NextFloat(-20, -40)), 0, default, 2.0f);
                    d.fadeIn = 1.5f; d.noGravity = true;
                }
                float maxRadius = 1200f; // 最大范围

                foreach (NPC npc in Main.ActiveNPCs)
                {
                    float distance = npc.Distance(center);

                    if (!npc.friendly && !npc.dontTakeDamage && distance < maxRadius)
                    {
                        int damage = 150;
                        damage = (int)(damage * GetSequenceMultiplier(currentSunSequence));

                        float distanceFactor = 1f - (distance / maxRadius);
                        if (distanceFactor < 0.2f) distanceFactor = 0.2f;
                        damage = (int)(damage * distanceFactor);
                        bool isUndead = IsUndeadCreature(npc);
                        if (isUndead)
                        {
                            damage *= 4;
                            CombatText.NewText(npc.getRect(), Color.Gold, "净化!!!", true);
                            for (int k = 0; k < 10; k++) Dust.NewDust(npc.position, npc.width, npc.height, DustID.GoldFlame, 0, 0, 0, default, 2f);
                        }
                        float knockback = 15f * distanceFactor;
                        Player.ApplyDamageToNPC(npc, damage, knockback, Player.direction, isUndead);

                        npc.AddBuff(BuffID.Confused, 300);
                        npc.AddBuff(BuffID.OnFire3, 300);
                        npc.AddBuff(BuffID.Daybreak, 180);
                        npc.AddBuff(BuffID.Midas, 300);
                    }
                }
            }
            else if (sunRadianceCooldown > 0)
            {
                if (sunRadianceCooldown % 60 == 0) Main.NewText($"冷却: {sunRadianceCooldown/60}s", 150, 150, 150);
            }
            else
            {
                Main.NewText("灵性不足 (需60点)", 255, 50, 50);
            }
        }
        private void CastHolyLight()
        {
            int cost = 80;
            if (currentSunSequence <= 5) cost = 150; // 序列5消耗更高

            if (holyLightCooldown <= 0 && TryConsumeSpirituality(cost))
            {
                holyLightCooldown = 120; // 2秒冷却
                Vector2 targetPos = Main.MouseWorld;

                if (currentSunSequence <= 5)
                {
                    Player.bodyFrame.Y = Player.bodyFrame.Height * 5; // 强制设为一个举手动作(如果有)

                    SoundEngine.PlaySound(SoundID.Item122, Player.position); // 更神圣的声音
                    Main.NewText("神圣之光！", 255, 165, 0);

                    int damage = (int)(800 * GetSequenceMultiplier(currentSunSequence)); // 伤害暴增

                    for (int i = -1; i <= 1; i++)
                    {
                        Vector2 offset = new Vector2(i * 30, 0); // 左右排开
                        Projectile.NewProjectile(Player.GetSource_FromThis(), targetPos + offset - new Vector2(0, 800), new Vector2(0, 25f), ModContent.ProjectileType<Content.Projectiles.Sun.HolyLightBeam>(), damage, 8f, Player.whoAmI);
                    }

                    for (int k = 0; k < 50; k++)
                    {
                        Dust.NewDustPerfect(targetPos + Main.rand.NextVector2Circular(50, 50), DustID.GoldFlame, new Vector2(0, -5), 0, default, 3f).noGravity = true;
                    }
                }
                else // 序列7：普通的圣光
                {
                    Vector2 spawnPos = targetPos - new Vector2(0, 600);
                    int damage = (int)(300 * GetSequenceMultiplier(currentSunSequence));
                    Projectile.NewProjectile(Player.GetSource_FromThis(), spawnPos, new Vector2(0, 20f), ModContent.ProjectileType<Content.Projectiles.Sun.HolyLightBeam>(), damage, 5f, Player.whoAmI);
                    Main.NewText("圣光啊，惩戒这个敌人！", 255, 200, 0);
                }
            }
            else if (holyLightCooldown > 0) Main.NewText("圣光冷却中...", 150, 150, 150);
        }
        private void CastHolyOath()
        {
            if (holyOathCooldown <= 0 && TryConsumeSpirituality(100))
            {
                holyOathCooldown = 1800; // 30秒冷却
                SoundEngine.PlaySound(SoundID.Item29, Player.position);

                int type = Main.rand.Next(3);
                string text = "";

                switch (type)
                {
                    case 0: // 力量
                        text = "我发誓，我将无坚不摧！";
                        Player.AddBuff(BuffID.Wrath, 1200); // 增加伤害
                        Player.AddBuff(BuffID.Titan, 1200); // 增加击退
                        break;
                    case 1: // 敏捷
                        text = "我发誓，我将快如闪电！";
                        Player.AddBuff(BuffID.Swiftness, 1200);
                        Player.AddBuff(BuffID.Panic, 600);
                        break;
                    case 2: // 神圣火焰
                        text = "我发誓，以此火净化污秽！";
                        Player.AddBuff(BuffID.WeaponImbueFire, 1200);
                        Player.AddBuff(BuffID.WeaponImbueIchor, 1200); // 减防模拟神圣穿透
                        break;
                }

                CombatText.NewText(Player.getRect(), Color.Gold, text, true);

                // 特效
                for (int i = 0; i < 30; i++) Dust.NewDust(Player.position, Player.width, Player.height, DustID.Enchanted_Gold, 0, 0, 0, default, 1.5f);
            }
            else if (holyOathCooldown > 0) Main.NewText("誓约冷却中...", 150, 150, 150);
        }

        // 技能：光明之火 (火海)
        private void CastFireOcean()
        {
            if (currentSunSequence <= 4)
            {
                if (fireOceanCooldown <= 0 && TryConsumeSpirituality(500))
                {
                    fireOceanCooldown = 3600; // 60秒冷却 (超级大招)

                    Main.NewText("阳炎！拥抱太阳的恩赐吧！", 255, 69, 0); // 深橙色提示
                    SoundEngine.PlaySound(SoundID.Item14, Player.position); // 爆炸音效

                    Projectile.NewProjectile(Player.GetSource_FromThis(), Main.MouseWorld, Vector2.Zero, ModContent.ProjectileType<Content.Projectiles.Sun.FlaringSun>(), 3000, 10f, Player.whoAmI);
                }
                else if (fireOceanCooldown > 0)
                {
                    Main.NewText($"阳炎积蓄中... ({fireOceanCooldown / 60}s)", 150, 150, 150);
                }
                else
                {
                    Main.NewText("灵性不足 (需500点)", 255, 50, 50);
                }
            }
            else
            {
                if (fireOceanCooldown <= 0 && TryConsumeSpirituality(200))
                {
                    fireOceanCooldown = 1200; // 20秒冷却

                    Main.NewText("感受太阳的磅礴气息吧！", 255, 100, 0);
                    SoundEngine.PlaySound(SoundID.Item34, Player.position); // 火焰喷射音效

                    int count = 10;
                    for (int i = 0; i < count; i++)
                    {

                        Vector2 offset = Main.rand.NextVector2Circular(300, 100);
                        Vector2 vel = new Vector2(Main.rand.NextFloat(-5, 5), Main.rand.NextFloat(-5, -10));

                        int damage = (int)(100 * GetSequenceMultiplier(currentSunSequence));
                        Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center + offset, vel, ModContent.ProjectileType<Content.Projectiles.Sun.SunFireTrap>(), damage, 2f, Player.whoAmI);
                    }
                }
                else if (fireOceanCooldown > 0)
                {
                    Main.NewText($"光之火冷却中... ({fireOceanCooldown / 60}s)", 150, 150, 150);
                }
                else
                {
                    Main.NewText("灵性不足 (需200点)", 255, 50, 50);
                }
            }
        }
        private void ApplyBardBuffs(Player target)
        {
            target.buffImmune[BuffID.Horrified] = true;
            target.buffImmune[BuffID.Silenced] = true;
            target.buffImmune[BuffID.Darkness] = true;
            target.AddBuff(BuffID.Rage, 2);
            target.AddBuff(BuffID.Swiftness, 2);
            if (currentSunSequence <= 8)
            {
                target.AddBuff(BuffID.Warmth, 2);
                target.buffImmune[BuffID.Chilled] = true; // 免疫寒冷
                target.buffImmune[BuffID.Frozen] = true;  // 免疫冰冻
                target.AddBuff(BuffID.Shine, 2); // 发光
                target.buffImmune[BuffID.Blackout] = true; // 免疫黑视
                target.AddBuff(BuffID.Hunter, 2);
            }
            LotMPlayer targetLotM = target.GetModPlayer<LotMPlayer>();
            if (Main.GameUpdateCount % 60 == 0)
            {
                targetLotM.spiritualityCurrent = Math.Min(targetLotM.spiritualityCurrent + 5f, targetLotM.spiritualityMax);
            }
            if (targetLotM.currentSunSequence <= 9)
            {
                target.statDefense += 5;
                target.AddBuff(BuffID.Endurance, 2);
            }
        }
        // === 太阳途径新增：通用的 PVP 序列压制逻辑 ===
        private void RunSunSuppression(float range)
        {
            if (currentSunSequence > 4) return;
            int debuffDuration = 86400; // 一整天

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player target = Main.player[i];
                if (target.active && !target.dead && target.whoAmI != Player.whoAmI)
                {
                    // PVP 判定
                    bool isHostile = Player.hostile && target.hostile && (Player.team == 0 || Player.team != target.team);

                    if (isHostile && target.Distance(Player.Center) < range)
                    {
                        LotMPlayer targetLotM = target.GetModPlayer<LotMPlayer>();

                        // 计算目标的最高序列 (取最小值)
                        int targetBest = 10;
                        targetBest = Math.Min(targetBest, targetLotM.currentSequence);
                        targetBest = Math.Min(targetBest, targetLotM.currentHunterSequence);
                        targetBest = Math.Min(targetBest, targetLotM.currentMoonSequence);
                        targetBest = Math.Min(targetBest, targetLotM.currentFoolSequence);
                        targetBest = Math.Min(targetBest, targetLotM.currentMarauderSequence);
                        targetBest = Math.Min(targetBest, targetLotM.currentSunSequence);
                        targetBest = Math.Min(targetBest, targetLotM.currentDemonessSequence);

                        // 只有对方位格不高于自己时才生效
                        if (targetBest >= currentSunSequence)
                        {
                            int buffType = ModContent.BuffType<Buffs.SunSuppressionDebuff>();

                            if (Main.netMode == NetmodeID.SinglePlayer)
                            {
                                target.AddBuff(buffType, debuffDuration);
                            }
                            else
                            {
                                ModPacket packet = Mod.GetPacket();
                                packet.Write((byte)LotMNetMsg.ApplySunSuppression);
                                packet.Write((byte)target.whoAmI);
                                packet.Write(debuffDuration);
                                packet.Send();
                            }

                            CombatText.NewText(target.getRect(), Color.Gold, "无暗序列压制!", true);
                            for (int k = 0; k < 30; k++)
                                Dust.NewDust(target.position, target.width, target.height, DustID.GoldFlame, 0, 0, 0, default, 2f);
                        }
                    }
                }
            }
        }
        private void CastNotarize()
        {
            int cost = currentSunSequence <= 4 ? 300 : 150;
            int cd = currentSunSequence <= 4 ? 1800 : 1200; // 净化CD稍长(30秒)

            if (notarizeCooldown <= 0 && TryConsumeSpirituality(cost))
            {
                notarizeCooldown = cd;
                SoundEngine.PlaySound(SoundID.Item29, Player.Center);

                float range = currentSunSequence <= 4 ? 2000f : 1000f; // 范围翻倍

                if (currentSunSequence <= 4)
                {
                    RunSunSuppression(range);
                }

                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (p.active && !p.dead && p.Distance(Player.Center) < range)
                    {
                        for (int k = 0; k < p.buffType.Length; k++)
                        {
                            if (p.buffType[k] > 0 && Main.debuff[p.buffType[k]])
                            {
                                p.DelBuff(k);
                                k--;
                            }
                        }

                        // 太阳誓约 (强力Buff)
                        p.AddBuff(BuffID.Wrath, 1200);
                        p.AddBuff(BuffID.Ironskin, 1200);
                        p.AddBuff(BuffID.Regeneration, 1200);
                        p.AddBuff(BuffID.Lifeforce, 1200); // 增加生命上限 (新)
                        CombatText.NewText(p.getRect(), Color.Gold, "净化完毕!", true);
                    }
                }
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.friendly && npc.Distance(Player.Center) < range)
                    {
                        int damage = (int)(200 * GetSequenceMultiplier(currentSunSequence));
                        bool isUnholy = NPCID.Sets.Zombies[npc.type] || NPCID.Sets.Skeletons[npc.type];

                        if (currentSunSequence <= 4 && isUnholy) damage *= 5;
                        else if (isUnholy) damage *= 2;

                        Player.ApplyDamageToNPC(npc, damage, 0f, 0, false);

                        npc.AddBuff(BuffID.Ichor, 600);
                        npc.AddBuff(BuffID.Confused, 180);
                        if (currentSunSequence <= 4) npc.AddBuff(BuffID.Daybreak, 600);
                    }
                }

                Main.NewText(currentSunSequence <= 4 ? "污秽消散！" : "公证完成！", 255, 215, 0);
            }
        }


        // 更新大招释放方法
        public void CastCatastrophe()
        {
            // --- 冷却检查逻辑 ---
            if (catastropheCooldown > 0)
            {
                // 只在本地玩家视角显示，防止联机时给别人发消息
                if (Main.myPlayer == Player.whoAmI)
                {
                    // 转换为秒，保留1位小数，例如: 12.5s
                    float secondsLeft = catastropheCooldown / 60f;
                    Main.NewText($"天灾积蓄中... ({secondsLeft:F1}s)", 150, 150, 150);
                }
                return; // 直接返回，不执行后续技能
            }

            // --- 灵性检查 ---
            if (!TryConsumeSpirituality(5000))
            {
                Main.NewText("灵性不足以引动天灾！(需 5000)", 255, 50, 50);
                return;
            }

            // --- 技能释放逻辑 ---
            catastropheCooldown = 7200; // 120秒冷却

            Main.NewText("天灾降临：世界在你的意志下颤抖！", 148, 0, 211); // 深紫色文本

            int baseDmg = 8000;
            int finalDmg = (int)(Player.GetDamage(DamageClass.Magic).ApplyTo(baseDmg));

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(
                    Player.GetSource_FromThis(),
                    Player.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<CatastropheController>(),
                    finalDmg,
                    0f,
                    Player.whoAmI
                );
            }
        }
        public void CastApocalypse()
        {
            // --- 冷却检查逻辑 ---
            if (apocalypseCooldown > 0)
            {
                if (Main.myPlayer == Player.whoAmI)
                {
                    // 将帧数转换为 分钟:秒 的格式，因为10分钟太长了，只显示秒数不直观
                    int totalSeconds = apocalypseCooldown / 60;
                    int minutes = totalSeconds / 60;
                    int seconds = totalSeconds % 60;

                    // 显示为：末日权柄冷却中... (9分 30秒)
                    Main.NewText($"末日权柄冷却中... ({minutes}分 {seconds}秒)", 150, 150, 150);
                }
                return; // 冷却中直接返回
            }

            // --- 灵性检查 ---
            if (!TryConsumeSpirituality(10000))
            {
                Main.NewText("灵性不足以引动末日！(需 10000)", 255, 50, 50);
                return;
            }

            // --- 技能释放逻辑 ---
            apocalypseCooldown = 36000; // 10分钟 (600秒)

            SoundEngine.PlaySound(SoundID.Item29, Player.Center);
            SoundEngine.PlaySound(SoundID.Item122, Player.Center);

            screenShakeMagnitude = 30f;
            Main.NewText("【末日降临】", 178, 34, 34);
            Main.NewText("旧的时代已经终结，万物归于寂静。", 255, 100, 100);

            // A. 冻结时间
            foreach (NPC target in Main.ActiveNPCs)
            {
                if (target.friendly || target.dontTakeDamage) continue;

                target.velocity = Vector2.Zero;
                target.AddBuff(ModContent.BuffType<global::zhashi.Content.Buffs.Debuffs.TimeStagnationBuff>(), 600);

                target.AddBuff(BuffID.ShadowFlame, 1200);
                target.AddBuff(BuffID.Frostburn2, 1200);
                target.AddBuff(BuffID.Venom, 1200);
                target.AddBuff(BuffID.BetsysCurse, 1200);
                target.AddBuff(BuffID.Ichor, 1200);
            }

            // B. 清除弹幕
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].hostile)
                {
                    Main.projectile[i].Kill();
                }
            }

            // C. 毁灭打击
            foreach (NPC target in Main.ActiveNPCs)
            {
                if (target.friendly || target.dontTakeDamage) continue;

                if (!target.boss || target.life < target.lifeMax * 0.3f)
                {
                    Player.ApplyDamageToNPC(target, 999999, 0, 0, false);
                    for (int k = 0; k < 20; k++)
                        Dust.NewDust(target.position, target.width, target.height, DustID.Granite, 0, 0, 0, default, 2f);
                }
                else
                {
                    int doomDamage = (int)(target.life * 0.1f);
                    if (doomDamage > 50000) doomDamage = 50000;
                    if (doomDamage < 10000) doomDamage = 10000;

                    Player.ApplyDamageToNPC(target, doomDamage, 0, 0, true);
                }
            }

            Player.SetImmuneTimeForAllTypes(600);
        }
        private void SpawnCalamity()
        {
            if (Main.myPlayer != Player.whoAmI) return; // 仅本地执行

            NPC target = null;
            float maxDist = 800f;
            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && !npc.dontTakeDamage && npc.Distance(Player.Center) < maxDist)
                {
                    target = npc;
                    maxDist = npc.Distance(Player.Center);
                }
            }

            if (target != null)
            {
                int choice = Main.rand.Next(3);
                Vector2 spawnPos = target.Center + new Vector2(Main.rand.Next(-100, 100), -400);
                Vector2 velocity = Vector2.Normalize(target.Center - spawnPos) * 15f;

                // 【修复处】统一计算 damage，移除了重复声明
                int damage = (int)(50 * Systems.BalanceSystem.GetWorldTierMultiplier());
                if (Player.luck > 0)
                {
                    damage = (int)(damage * (1f + Player.luck * 0.5f));
                }

                int projType = ProjectileID.Meteor1;

                switch (choice)
                {
                    case 0: projType = ProjectileID.Meteor1; break;
                    case 1: projType = ProjectileID.BallofFire; break;
                    case 2:
                        spawnPos = target.Center;
                        velocity = Vector2.Zero;
                        projType = ProjectileID.Grenade;
                        break;
                }

                Projectile.NewProjectile(Player.GetSource_FromThis(), spawnPos, velocity, projType, damage, 2f, Main.myPlayer);
                CombatText.NewText(target.getRect(), Color.OrangeRed, "灾祸!", true);
            }
        }
        public override bool CanConsumeAmmo(Item weapon, Item ammo)
        {
            // ---------------------------------------------------------
            // A. 厄运判定：如果你正处于“倒霉”状态 (仪式中或喝了假酒)
            // ---------------------------------------------------------
            if (Player.HasBuff(ModContent.BuffType<ExtremeBadLuckBuff>()))
            {
                if (Main.rand.NextFloat() < 0.3f)
                {
                    Player.ConsumeItem(ammo.type); // 手动扣除额外一发
                }

                return true; 
            }

            // ---------------------------------------------------------
            // B. 赢家判定：序列5 动态省弹药逻辑
            // ---------------------------------------------------------
            if (currentWheelSequence <= 5)
            {
                if (Player.luck <= 0)
                {
                    return true; // 必须消耗，无特殊效果
                }
                float saveChance = 0.20f + (Player.luck * 0.1f);

                if (saveChance > 0.6f) saveChance = 0.6f;

                if (Main.rand.NextFloat() < saveChance)
                {
                    if (Main.rand.NextBool(10))
                    {
                        Dust.NewDust(Player.Center, 5, 5, DustID.GoldCoin, 0, -1, 0, default, 0.5f);
                    }
                    return false; // 返回 false = 不消耗弹药
                }
            }
            return base.CanConsumeAmmo(weapon, ammo);
        }
        private void CastPsychicStorm()
        {
            if (spiritualityCurrent < 50)
            {
                CombatText.NewText(Player.getRect(), Color.Red, "灵性不足!", true);
                return;
            }
            spiritualityCurrent -= 50;
            psychicStormCooldown = 300;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item15, Player.position);

            for (int i = 0; i < 40; i++)
            {
                Vector2 speed = Main.rand.NextVector2Circular(12, 12);
                Dust d = Dust.NewDustPerfect(Player.Center, DustID.PurpleCrystalShard, speed, 150, default, 2f);
                d.noGravity = true;
            }

            // 【修复处】使用 stormRadius 替代容易冲突的 radius，统一定义 damage
            bool isDemigod = currentWheelSequence <= 4;
            float stormRadius = isDemigod ? 800f : 450f;
            int stormDamage = (int)((isDemigod ? 200 : 80) * Systems.BalanceSystem.GetWorldTierMultiplier());

            bool hitAny = false;

            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && npc.Distance(Player.Center) < stormRadius)
                {
                    hitAny = true;
                    Player.ApplyDamageToNPC(npc, stormDamage, 0f, 0, crit: Main.rand.Next(100) < Player.GetCritChance(DamageClass.Generic));

                    npc.AddBuff(BuffID.Confused, 240);

                    if (isDemigod)
                    {
                        npc.AddBuff(BuffID.Venom, 600);
                        npc.AddBuff(BuffID.CursedInferno, 600);
                        CombatText.NewText(npc.getRect(), Color.Purple, "疯狂迷失!", true);
                    }
                    else
                    {
                        npc.AddBuff(BuffID.ShadowFlame, 300);
                        CombatText.NewText(npc.getRect(), new Color(147, 112, 219), "混乱!", true);
                    }
                }
            }

            if (hitAny)
            {
                Main.NewText("你引爆了灵性，制造了一场精神风暴！", 180, 100, 255);
            }
            else
            {
                CombatText.NewText(Player.getRect(), Color.Gray, "周围没有目标", true);
            }
        }

        public override void OnRespawn()
        {
            // 复活时将灵性回满，防止刚复活就因为灵性为0而再次暴毙
            spiritualityCurrent = spiritualityMax;

            // 顺便重置所有可能导致扣蓝的状态
            isFireForm = false;
            isMercuryForm = false;
            isCalamityGiant = false;
            isSunMessenger = false;
            isSpiritForm = false;
            isVampireWings = false;
            // ... 其他你需要关闭的状态
        }
        // 判断是否为不死/邪恶/克制生物的通用方法
        public bool IsUndeadCreature(NPC npc)
        {
            if (NPCID.Sets.Zombies[npc.type] || NPCID.Sets.Skeletons[npc.type]) return true;

            if (npc.aiStyle == 22 || npc.aiStyle == 23) return true;

            if (npc.coldDamage) return true;

            if (npc.HitSound == SoundID.NPCHit2) return true;

            int[] extraUndead = {
        // --- 僵尸变种 ---
        NPCID.BloodZombie,       // 血腥僵尸
        NPCID.ZombieMerman,      // 僵尸鱼人
        NPCID.ZombieElf,         // 僵尸精灵
        NPCID.ZombieElfBeard,
        NPCID.ZombieElfGirl,
        NPCID.UndeadMiner,       // 不死矿工
        NPCID.UndeadViking,      // 不死维京人
        NPCID.Nymph,             // 宁芙 (迷失女孩) - 可选

        NPCID.BoneLee,           // 骷髅李
        NPCID.AngryBones,        // 愤怒骷髅
        NPCID.AngryBonesBig,
        NPCID.AngryBonesBigHelmet,
        NPCID.AngryBonesBigMuscle,
        
        NPCID.BlueArmoredBones, NPCID.BlueArmoredBonesMace, NPCID.BlueArmoredBonesNoPants, NPCID.BlueArmoredBonesSword,
        NPCID.HellArmoredBones, NPCID.HellArmoredBonesMace, NPCID.HellArmoredBonesSpikeShield, NPCID.HellArmoredBonesSword,
        NPCID.RustyArmoredBonesFlail, NPCID.RustyArmoredBonesAxe, NPCID.RustyArmoredBonesSword,
        
        NPCID.DarkCaster,        //哪怕是暗黑法师
        NPCID.DiabolistRed,      // 魔教徒 (可能是你说的撒旦骷髅?)
        NPCID.DiabolistWhite,
        NPCID.RaggedCaster,      // 褴褛法师
        NPCID.RaggedCasterOpenCoat,
        NPCID.RedDevil,          // 红魔鬼 (虽然是恶魔，但绝对邪恶)
        NPCID.Vampire,           // 吸血鬼
        NPCID.Mothron            // 蛾怪 (日食)
    };

            if (extraUndead.Contains(npc.type)) return true;

            return false;
        }
    }

    public class ApothecaryCrafting : Terraria.ModLoader.GlobalItem { public override void OnCreated(Terraria.Item item, ItemCreationContext context) { if (context is RecipeItemCreationContext) { Player p = Main.LocalPlayer; if (p != null && p.active && p.GetModPlayer<LotMPlayer>().currentMoonSequence <= 9) { bool isP = item.consumable && (item.buffType > 0 || item.healLife > 0 || item.healMana > 0); if (isP) p.QuickSpawnItem(item.GetSource_FromThis(), item.type, item.stack); } } } }
}