using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using System.IO;
using zhashi.Content;
using zhashi.Content.UI;
using zhashi.Content.Buffs;
using zhashi.Content.Systems;
using zhashi.Content.Dimensions;
using zhashi.Content.Pathways.Door;

namespace zhashi
{
    // 网络消息类型枚举
    public enum LotMNetMsg : byte
    {
        PlayerSync = 0,
        ApplySunSuppression = 1,
        SyncFavorability = 2,
        StoryDataSync = 3,
        WheelStateSync = 4,      // 命运途径状态同步(独立包,避免污染原PlayerSync)
        ProphetActiveSkill = 5,  // 先知主动技能广播(对NPC的命运操作)
        RequestWeatherToggle = 6,
        RequestConquerorToggle = 7,
        DoorAbilityRequest = 8,
        DoorStateSync = 9,
        DoorDebugResetRequest = 10,
        SpiritBanishPlayer = 11  // 服务器要求某个玩家走进灵界(放逐玩家)
        , BlackEmperorStateSync = 12  // 黑皇帝：契约状态同步
        , BlackEmperorAbilityRequest = 13  // 黑皇帝：主动技能请求(服务器裁定世界改动)
        , PromotionPulse = 14  // 晋升的动静：一声雷 + 一记震屏，广播给所有人
    }

    public class zhashi : Mod
    {
        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            // 读取消息类型
            LotMNetMsg msgType = (LotMNetMsg)reader.ReadByte();

            switch (msgType)
            {
                // =================================================================
                // Case 0: 战斗玩家数据同步 (修复读取不足错误)
                // =================================================================
                case LotMNetMsg.PlayerSync:
                    byte playernumber = reader.ReadByte();

                    // 【重要修复】即使玩家不活跃，也不能直接 return，必须把数据读完！
                    // 我们先获取 ModPlayer，如果 playernumber 合法，GetModPlayer 总是安全的
                    LotMPlayer modPlayer = null;
                    if (playernumber < Main.maxPlayers)
                    {
                        modPlayer = Main.player[playernumber].GetModPlayer<LotMPlayer>();
                    }

                    // --- [0] 读取基础序列等级 (8个 int) ---
                    int baseSeq = reader.ReadInt32();
                    int baseMarauder = reader.ReadInt32();
                    int baseFool = reader.ReadInt32();
                    int baseHunter = reader.ReadInt32();
                    int baseMoon = reader.ReadInt32();
                    int baseSun = reader.ReadInt32();
                    int baseDemoness = reader.ReadInt32();
                    int baseWheel = reader.ReadInt32();          // <修复> 命运基础序列
                    int baseDoor = reader.ReadInt32();           // 学徒/门基础序列
                    int baseBlackEmperor = reader.ReadInt32();    // 黑皇帝基础序列

                    // --- [1] 读取当前序列等级 (8个 int) + 灵性 (1个 float) ---
                    int currSeq = reader.ReadInt32();
                    int currMarauder = reader.ReadInt32();
                    int currFool = reader.ReadInt32();
                    int currHunter = reader.ReadInt32();
                    int currMoon = reader.ReadInt32();
                    int currSun = reader.ReadInt32();
                    int currDemoness = reader.ReadInt32();
                    int currWheel = reader.ReadInt32();           // <修复> 命运当前序列
                    int currDoor = reader.ReadInt32();            // 学徒/门当前序列
                    int currBlackEmperor = reader.ReadInt32();     // 黑皇帝当前序列
                    float spiritCurr = reader.ReadSingle();

                    // --- [2] 读取寄生与仪式状态 ---
                    bool isParasitizing = reader.ReadBoolean();
                    int parasiteTarget = reader.ReadInt32();
                    bool parasiteNPC = reader.ReadBoolean();
                    bool parasitePlayer = reader.ReadBoolean();
                    int purifyProg = reader.ReadInt32();
                    int judgeProg = reader.ReadInt32();
                    int ironProg = reader.ReadInt32();
                    int despairCount = reader.ReadInt32();
                    int afflictTimer = reader.ReadInt32();

                    // --- [2.1] 完整晋升仪式状态 ---
                    int guardianProg = reader.ReadInt32();
                    int demonHunterProg = reader.ReadInt32();
                    int weatherCount = reader.ReadInt32();
                    int weatherTimer = reader.ReadInt32();
                    bool weatherComplete = reader.ReadBoolean();
                    bool conquerorComplete = reader.ReadBoolean();
                    int attendantProg = reader.ReadInt32();
                    bool attendantComplete = reader.ReadBoolean();
                    int parasiteRitual = reader.ReadInt32();
                    int mentorRitual = reader.ReadInt32();
                    int trojanRitual = reader.ReadInt32();
                    int wormRitual = reader.ReadInt32();
                    int catastropheRitual = reader.ReadInt32();
                    int misfortuneRitual = reader.ReadInt32();
                    bool misfortuneMageComplete = reader.ReadBoolean();
                    int anomalyRitual = reader.ReadInt32();
                    bool anomalyComplete = reader.ReadBoolean();
                    int prophetRitual = reader.ReadInt32();
                    bool prophetComplete = reader.ReadBoolean();
                    int serpentRitual = reader.ReadInt32();
                    bool serpentBeatMoonLord = reader.ReadBoolean();
                    bool serpentComplete = reader.ReadBoolean();

                    // --- [3] 核心资源 ---
                    int spiritWorms = reader.ReadInt32();

                    // --- [4] 愚者途径状态 ---
                    bool spiritVis = reader.ReadBoolean();
                    bool spiritForm = reader.ReadBoolean();
                    int graftMode = reader.ReadInt32();
                    int threadTarget = reader.ReadInt32();
                    bool realmActive = reader.ReadBoolean();

                    // --- [5] 错误途径状态 ---
                    bool deceitDom = reader.ReadBoolean();
                    bool timeClock = reader.ReadBoolean();

                    // --- [6] 月亮途径状态 ---
                    bool taming = reader.ReadBoolean();
                    bool vWings = reader.ReadBoolean();
                    bool batSwarm = reader.ReadBoolean();
                    bool moonLight = reader.ReadBoolean();
                    bool fullMoon = reader.ReadBoolean();
                    bool createDom = reader.ReadBoolean();

                    // --- [7] 猎人途径状态 ---
                    bool fireForm = reader.ReadBoolean();
                    bool calGiant = reader.ReadBoolean();
                    bool flameCloak = reader.ReadBoolean();

                    // --- [8] 巨人/战士途径状态 ---
                    bool guardStance = reader.ReadBoolean();
                    bool mercForm = reader.ReadBoolean();
                    bool dawnArmor = reader.ReadBoolean();

                    // --- [9] 太阳途径状态 ---
                    bool singing = reader.ReadBoolean();
                    bool sunMsg = reader.ReadBoolean();

                    // --- [10] 其他/魔女 (顺序与 SyncPlayer 写入一致) ---
                    bool apocalypseForm = reader.ReadBoolean();
                    bool disasterForm = reader.ReadBoolean();
                    bool passSteal = reader.ReadBoolean();

                    // --- [11] 理智与其他会影响玩法/表现的开关 ---
                    float sanity = reader.ReadSingle();
                    bool losingControl = reader.ReadBoolean();
                    bool faceless = reader.ReadBoolean();
                    bool borrowingPower = reader.ReadBoolean();
                    bool fateDisturbance = reader.ReadBoolean();
                    bool trojanResurrection = reader.ReadBoolean();
                    bool stealModeActive = reader.ReadBoolean();
                    bool cleansingSlash = reader.ReadBoolean();
                    bool afflictionDemoness = reader.ReadBoolean();
                    bool mirrorClone = reader.ReadBoolean();
                    bool misfortuneDomain = reader.ReadBoolean();
                    bool prophecyMark = reader.ReadBoolean();
                    bool fateLoop = reader.ReadBoolean();
                    float fateLoopX = reader.ReadSingle();
                    float fateLoopY = reader.ReadSingle();
                    bool dawnArmorBroken = reader.ReadBoolean();
                    int dawnArmorHP = reader.ReadInt32();
                    bool armyOfOne = reader.ReadBoolean();

                    // 客户端只能提交自己的玩家状态，防止伪造其他玩家的晋升/资源。
                    if (Main.netMode == NetmodeID.Server && playernumber != whoAmI)
                        modPlayer = null;

                    if (modPlayer != null)
                    {
                        modPlayer.baseSequence = baseSeq;
                        modPlayer.baseMarauderSequence = baseMarauder;
                        modPlayer.baseFoolSequence = baseFool;
                        modPlayer.baseHunterSequence = baseHunter;
                        modPlayer.baseMoonSequence = baseMoon;
                        modPlayer.baseSunSequence = baseSun;
                        modPlayer.baseDemonessSequence = baseDemoness;
                        modPlayer.baseWheelSequence = baseWheel;       // <修复>
                        modPlayer.baseDoorSequence = baseDoor;          // 学徒/门
                        modPlayer.baseBlackEmperorSequence = baseBlackEmperor;  // 黑皇帝

                        modPlayer.currentSequence = currSeq;
                        modPlayer.currentMarauderSequence = currMarauder;
                        modPlayer.currentFoolSequence = currFool;
                        modPlayer.currentHunterSequence = currHunter;
                        modPlayer.currentMoonSequence = currMoon;
                        modPlayer.currentSunSequence = currSun;
                        modPlayer.currentDemonessSequence = currDemoness;
                        modPlayer.currentWheelSequence = currWheel;    // <修复>
                        modPlayer.currentDoorSequence = currDoor;       // 学徒/门
                        modPlayer.currentBlackEmperorSequence = currBlackEmperor; // 黑皇帝
                        modPlayer.spiritualityCurrent = spiritCurr;

                        modPlayer.isParasitizing = isParasitizing;
                        modPlayer.parasiteTargetIndex = parasiteTarget;
                        modPlayer.parasiteIsTownNPC = parasiteNPC;
                        modPlayer.parasiteIsPlayer = parasitePlayer;
                        modPlayer.purificationProgress = purifyProg;
                        modPlayer.judgmentProgress = judgeProg;
                        modPlayer.ironBloodRitualProgress = ironProg;
                        modPlayer.despairRitualCount = despairCount;
                        modPlayer.afflictionRitualTimer = afflictTimer;
                        modPlayer.guardianRitualProgress = guardianProg;
                        modPlayer.demonHunterRitualProgress = demonHunterProg;
                        modPlayer.weatherRitualCount = weatherCount;
                        modPlayer.weatherRitualTimer = weatherTimer;
                        modPlayer.weatherRitualComplete = weatherComplete;
                        modPlayer.conquerorRitualComplete = conquerorComplete;
                        modPlayer.attendantRitualProgress = attendantProg;
                        modPlayer.attendantRitualComplete = attendantComplete;
                        modPlayer.parasiteRitualProgress = parasiteRitual;
                        modPlayer.mentorRitualProgress = mentorRitual;
                        modPlayer.trojanRitualTimer = trojanRitual;
                        modPlayer.wormRitualTimer = wormRitual;
                        modPlayer.catastropheRitualCount = catastropheRitual;
                        modPlayer.misfortuneRitualTimer = misfortuneRitual;
                        modPlayer.misfortuneMageRitualComplete = misfortuneMageComplete;
                        modPlayer.anomalyRitualProgress = anomalyRitual;
                        modPlayer.anomalyRitualComplete = anomalyComplete;
                        modPlayer.prophetRitualProgress = prophetRitual;
                        modPlayer.prophetRitualComplete = prophetComplete;
                        modPlayer.serpentRitualProgress = serpentRitual;
                        modPlayer.serpentRitualBeatMoonLord = serpentBeatMoonLord;
                        modPlayer.serpentRitualComplete = serpentComplete;

                        modPlayer.spiritWorms = spiritWorms;

                        modPlayer.isSpiritVisionActive = spiritVis;
                        modPlayer.isSpiritForm = spiritForm;
                        modPlayer.graftingMode = graftMode;
                        modPlayer.spiritThreadTargetIndex = threadTarget;
                        modPlayer.isRealmOfMysteriesActive = realmActive;  // <修复:之前丢弃了>

                        modPlayer.isDeceitDomainActive = deceitDom;
                        modPlayer.isTimeClockActive = timeClock;

                        modPlayer.isTamingActive = taming;
                        modPlayer.isVampireWings = vWings;
                        modPlayer.isBatSwarm = batSwarm;
                        modPlayer.isMoonlightized = moonLight;
                        modPlayer.isFullMoonActive = fullMoon;
                        modPlayer.isCreationDomain = createDom;

                        modPlayer.isFireForm = fireForm;
                        modPlayer.isCalamityGiant = calGiant;
                        modPlayer.isFlameCloakActive = flameCloak;

                        modPlayer.isGuardianStance = guardStance;
                        modPlayer.isMercuryForm = mercForm;
                        modPlayer.dawnArmorActive = dawnArmor;

                        modPlayer.isSinging = singing;
                        modPlayer.isSunMessenger = sunMsg;

                        modPlayer.isApocalypseForm = apocalypseForm;   // <修复>
                        modPlayer.isDisasterForm = disasterForm;       // <修复>

                        modPlayer.isPassiveStealEnabled = passSteal;

                        modPlayer.sanityCurrent = sanity;
                        modPlayer.isLosingControl = losingControl;
                        modPlayer.isFacelessActive = faceless;
                        modPlayer.isBorrowingPower = borrowingPower;
                        modPlayer.fateDisturbanceActive = fateDisturbance;
                        modPlayer.isTrojanResurrection = trojanResurrection;
                        modPlayer.stealMode = stealModeActive;
                        modPlayer.isCleansingSlash = cleansingSlash;
                        modPlayer.isAfflictionDemoness = afflictionDemoness;
                        modPlayer.mirrorCloneActive = mirrorClone;
                        modPlayer.isMisfortuneDomainActive = misfortuneDomain;
                        modPlayer.prophecyMarked = prophecyMark;
                        modPlayer.fateLoopActive = fateLoop;
                        modPlayer.fateLoopCenter = new Microsoft.Xna.Framework.Vector2(fateLoopX, fateLoopY);
                        modPlayer.dawnArmorBroken = dawnArmorBroken;
                        modPlayer.dawnArmorCurrentHP = dawnArmorHP;
                        modPlayer.isArmyOfOne = armyOfOne;

                        // 如果是服务器收到包，转发给其他客户端
                        if (Main.netMode == NetmodeID.Server)
                            modPlayer.SyncPlayer(-1, whoAmI, false);
                    }
                    break;

                case LotMNetMsg.RequestWeatherToggle:
                    if (Main.netMode == NetmodeID.Server && whoAmI >= 0 && whoAmI < Main.maxPlayers)
                    {
                        Player requester = Main.player[whoAmI];
                        if (requester.active &&
                            requester.GetModPlayer<LotMPlayer>().currentHunterSequence <= 3 &&
                            requester.HeldItem.type == ModContent.ItemType<Content.Items.Materials.WeatherRune>())
                        {
                            if (Main.raining) Main.StopRain();
                            else Main.StartRain();
                            NetMessage.SendData(MessageID.WorldData);
                        }
                    }
                    break;

                case LotMNetMsg.RequestConquerorToggle:
                    if (Main.netMode == NetmodeID.Server && whoAmI >= 0 && whoAmI < Main.maxPlayers)
                    {
                        Player requester = Main.player[whoAmI];
                        if (requester.active &&
                            requester.GetModPlayer<LotMPlayer>().currentHunterSequence <= 2 &&
                            requester.HeldItem.type == ModContent.ItemType<Content.Items.Materials.ConquerorCharacteristic>())
                        {
                            ConquerorSpawnSystem.StopSpawning = !ConquerorSpawnSystem.StopSpawning;
                            ConquerorSpawnSystem.StopSpawningOwner = ConquerorSpawnSystem.StopSpawning ? whoAmI : -1;
                            NetMessage.SendData(MessageID.WorldData);
                        }
                    }
                    break;

                case LotMNetMsg.DoorAbilityRequest:
                    DoorPathwayPlayer.ReceiveAbilityRequest(reader, whoAmI);
                    break;

                case LotMNetMsg.DoorStateSync:
                    DoorPathwayPlayer.ReceiveState(reader, whoAmI);
                    break;

                case LotMNetMsg.SpiritBanishPlayer:
                    SpiritBanishSystem.ReceivePlayerBanish(reader, whoAmI);
                    break;

                case LotMNetMsg.BlackEmperorStateSync:
                    Content.Pathways.BlackEmperor.BlackEmperorPlayer.ReceiveLaw(reader, whoAmI);
                    break;

                case LotMNetMsg.BlackEmperorAbilityRequest:
                    Content.Pathways.BlackEmperor.BlackEmperorPlayer.ReceiveAbilityRequest(reader, whoAmI);
                    break;

                case LotMNetMsg.PromotionPulse:
                    Content.PromotionPulse.Receive(reader, whoAmI);
                    break;

                case LotMNetMsg.DoorDebugResetRequest:
                    if (Main.netMode == NetmodeID.Server && whoAmI >= 0 && whoAmI < Main.maxPlayers)
                    {
                        Player requester = Main.player[whoAmI];
                        if (requester.active && requester.HeldItem.ModItem is Content.Items.Debug.RitualInstaComplete)
                        {
                            DoorPathwayPlayer door = requester.GetModPlayer<DoorPathwayPlayer>();
                            door.CompleteAllRitualsForDebug();
                            door.ResetAllCooldowns();
                            door.SyncState();
                        }
                    }
                    break;

                // =================================================================
                // Case 1: 太阳压制 (未变动)
                // =================================================================
                case LotMNetMsg.ApplySunSuppression:
                    int targetWho = reader.ReadByte();
                    int duration = reader.ReadInt32();
                    if (Main.netMode == NetmodeID.Server)
                    {
                        if (targetWho >= 0 && targetWho < Main.maxPlayers)
                        {
                            Player target = Main.player[targetWho];
                            if (target.active) target.AddBuff(ModContent.BuffType<SunSuppressionDebuff>(), duration);
                        }
                    }
                    break;

                // =================================================================
                // Case 2: 好感度 (未变动)
                // =================================================================
                case LotMNetMsg.SyncFavorability:
                    int npcID = reader.ReadInt32();
                    int score = reader.ReadInt32();
                    if (!FavorabilitySystem.favorabilityScores.ContainsKey(npcID))
                        FavorabilitySystem.favorabilityScores[npcID] = 0;
                    FavorabilitySystem.favorabilityScores[npcID] = score;

                    if (Main.netMode == NetmodeID.Server)
                    {
                        ModPacket packet = GetPacket();
                        packet.Write((byte)LotMNetMsg.SyncFavorability);
                        packet.Write(npcID);
                        packet.Write(score);
                        packet.Send(-1, whoAmI);
                    }
                    break;

                // =================================================================
                // Case 3: 剧情数据同步 (修复读取不足错误)
                // =================================================================
                case LotMNetMsg.StoryDataSync:
                    byte storyPlayerNum = reader.ReadByte();

                    LotMStoryPlayer storyPlayer = null;
                    if (storyPlayerNum < Main.maxPlayers)
                    {
                        storyPlayer = Main.player[storyPlayerNum].GetModPlayer<LotMStoryPlayer>();
                    }

                    // 必须先读完所有数据
                    int sStage = reader.ReadInt32();
                    int sDays = reader.ReadInt32();
                    bool sDaily = reader.ReadBoolean();
                    bool sComp = reader.ReadBoolean();
                    int sType = reader.ReadInt32();
                    int sTarget = reader.ReadInt32();
                    int sReq = reader.ReadInt32();
                    int sCurr = reader.ReadInt32();
                    bool sPotion = reader.ReadBoolean();

                    // 再赋值
                    if (storyPlayer != null)
                    {
                        storyPlayer.StoryStage = sStage;
                        storyPlayer.DaysSinceJoined = sDays;
                        storyPlayer.HasDailyQuest = sDaily;
                        storyPlayer.QuestCompletedToday = sComp;
                        storyPlayer.QuestType = sType;
                        storyPlayer.QuestTargetID = sTarget;
                        storyPlayer.QuestRequiredAmount = sReq;
                        storyPlayer.QuestCurrentAmount = sCurr;
                        storyPlayer.HasReceivedStarterPotion = sPotion;

                        if (Main.netMode == NetmodeID.Server)
                            storyPlayer.SyncPlayer(-1, whoAmI, false);
                    }
                    break;
            }
        }
    }
}
