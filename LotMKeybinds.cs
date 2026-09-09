using Terraria.ModLoader;

namespace zhashi
{
    public class LotMKeybinds : ModSystem
    {
        // ===================================================
        // 1. 变量声明 (在这里定义所有按键)
        // ===================================================

        // --- 月亮途径 (Moon) ---
        public static ModKeybind Moon_Wings { get; private set; }
        public static ModKeybind Moon_BatSwarm { get; private set; }
        public static ModKeybind Moon_PaperFigurine { get; private set; }
        public static ModKeybind Moon_Gaze { get; private set; }
        public static ModKeybind Moon_Shackles { get; private set; }
        public static ModKeybind Moon_Grenade { get; private set; }
        public static ModKeybind Moon_Elixir { get; private set; }
        public static ModKeybind Moon_Moonlight { get; private set; }
        public static ModKeybind Moon_FullMoon { get; private set; }
        public static ModKeybind Moon_SummonGate { get; private set; }
        public static ModKeybind Moon_Tame { get; private set; }

        // --- 猎人途径 (Hunter/Red Priest) ---
        public static ModKeybind RP_Transformation { get; private set; }
        public static ModKeybind RP_Flash { get; private set; }
        public static ModKeybind RP_Bomb { get; private set; }
        public static ModKeybind RP_Cloak { get; private set; }
        public static ModKeybind RP_Slash { get; private set; }
        public static ModKeybind RP_Enchant { get; private set; }
        public static ModKeybind RP_Skill { get; private set; }
        public static ModKeybind RP_Army { get; private set; }
        public static ModKeybind RP_Weather { get; private set; }
        public static ModKeybind RP_Glacier { get; private set; }

        // --- 巨人/战士途径 (Giant/Warrior) ---
        public static ModKeybind Giant_Mercury { get; private set; }
        public static ModKeybind Giant_Armor { get; private set; }
        public static ModKeybind Giant_Guardian { get; private set; }

        // --- 愚者途径 (Fool) [新增] ---
        public static ModKeybind Fool_SpiritVision { get; private set; } // 灵视 (C)
        public static ModKeybind Fool_Divination { get; private set; }   // 占卜 (J)
        public static ModKeybind Fool_FlameJump { get; private set; }    // 火焰跳跃 (F)
        public static ModKeybind Fool_Faceless { get; private set; }     // 无面伪装 (V)
        public static ModKeybind Fool_Distort { get; private set; }      // 干扰直觉 (G)
        public static ModKeybind Fool_Threads { get; private set; }      // 灵体之线 (Z)
        public static ModKeybind Fool_Swap { get; private set; }         // 秘偶互换 (T)
        public static ModKeybind Fool_Control { get; private set; }      // 控灵 (R)
        public static ModKeybind Fool_History { get; private set; }      // 历史投影 (Y)
        public static ModKeybind Fool_Borrow { get; private set; }       // 昨日重现 (U)
        public static ModKeybind Fool_Miracle { get; private set; }      // 奇迹愿望 (V - 序列2)
        public static ModKeybind Fool_Grafting { get; private set; }     // 嫁接 (G - 序列1)
        public static ModKeybind Fool_SpiritForm { get; private set; }   // 灵肉转化 (V - 序列1)
        public static ModKeybind Fool_RealmSwitch { get; private set; } // 新增：诡秘之境开关

        // --- 错误途径 (Fool) [新增] ---
        public static ModKeybind Marauder_StealToggle { get; private set; }
        public static ModKeybind Marauder_Parasite { get; private set; }
        public static ModKeybind MarauderSteal { get; private set; }
        public static ModKeybind Marauder_ConceptSteal { get; private set; }

        // 新增：太阳途径技能按键
        public static ModKeybind Sun_Sing { get; private set; }
        public static ModKeybind Sun_Radiance { get; private set; }
        public static ModKeybind Sun_HolyLight { get; private set; } // C键：召唤圣光
        public static ModKeybind Sun_Oath { get; private set; }    // V键：神圣誓约
        public static ModKeybind Sun_FireOcean { get; private set; }// G键：光明之火
        public static ModKeybind Sun_Notarize { get; private set; } // 公证人技能键
        public static ModKeybind Sun_Messenger { get; private set; } // 公证人技能键

        // --- 魔女途径 ---
        public static ModKeybind Demoness_Mirror { get; private set; }   // 核心技能：镜子替身
        public static ModKeybind Demoness_MirrorSwitch { get; private set; }
        public static ModKeybind Demoness_HairAttack { get; private set; }   // 头发攻击
        public static ModKeybind Demoness_SilkControl { get; private set; }  // 蛛丝控制
        public static ModKeybind Demoness_DespairSkill { get; private set; }
        public static ModKeybind Demoness_PetrifySkill { get; private set; }
        public static ModKeybind Demoness_Catastrophe { get; private set; }
        public static ModKeybind Demoness_Apocalypse { get; private set; }

        // --- 命运途径 ---
        public static ModKeybind Wheel_PsychicStorm { get; private set; }
        public static ModKeybind Wheel_Domain { get; private set; }
        public static ModKeybind Wheel_Blessing { get; private set; }
        public static ModKeybind Wheel_Dice { get; private set; }              // 序列3 命运骰子
        public static ModKeybind Wheel_WordsOfFortune { get; private set; }    // 序列2 福祸之言-福(给友军)
        public static ModKeybind Wheel_WordsOfMisfortune { get; private set; } // 序列2 福祸之言-祸(给敌人)
        public static ModKeybind Wheel_Revelation { get; private set; }        // 序列2 命运启示
        public static ModKeybind Wheel_FateLoop { get; private set; }          // 序列1 命运循环
        public static ModKeybind Wheel_Restart { get; private set; }           // 序列1 主动重启

        // --- 学徒/门途径 ---
        public static ModKeybind Door_OpenDoor { get; private set; }           // 序列9 开门(穿墙)

        // --- 学徒/门途径 序列8 戏法大师 ---
        public static ModKeybind Door_TrickSwitch { get; private set; }        // 切换戏法
        public static ModKeybind Door_TrickCast { get; private set; }          // 释放戏法

        // --- 学徒/门途径 序列7 占星人 ---
        public static ModKeybind Door_Astrology { get; private set; }          // 占星术

        // --- 学徒/门途径 序列6 记录官 ---
        public static ModKeybind Door_RecordNormal { get; private set; }       // 使用普通记录
        public static ModKeybind Door_RecordDivine { get; private set; }       // 使用神性记录

        // --- 学徒/门途径 序列5 旅行家 ---
        public static ModKeybind Door_TravelerGate { get; private set; }       // 旅行家之门(远距传送)
        public static ModKeybind Door_Blink { get; private set; }              // 闪现(短距)
        // ===================================================
        // 2. 注册按键 (Load)
        // ===================================================
        public override void Load()
        {
            // Moon
            Moon_Wings = KeybindLoader.RegisterKeybind(Mod, "月亮: 黑暗之翼", "F");
            Moon_BatSwarm = KeybindLoader.RegisterKeybind(Mod, "月亮: 蝙蝠化身", "F");
            Moon_PaperFigurine = KeybindLoader.RegisterKeybind(Mod, "月亮: 纸人替身", "J");
            Moon_Gaze = KeybindLoader.RegisterKeybind(Mod, "月亮: 黑暗凝视", "J");
            Moon_Shackles = KeybindLoader.RegisterKeybind(Mod, "月亮: 深渊枷锁", "G");
            Moon_Grenade = KeybindLoader.RegisterKeybind(Mod, "月亮: 炼金手雷", "X");
            Moon_Elixir = KeybindLoader.RegisterKeybind(Mod, "月亮: 生命灵液", "V");
            Moon_Moonlight = KeybindLoader.RegisterKeybind(Mod, "月亮: 月光化", "Z");
            Moon_FullMoon = KeybindLoader.RegisterKeybind(Mod, "月亮: 满月/创生", "C");
            Moon_SummonGate = KeybindLoader.RegisterKeybind(Mod, "月亮: 召唤之门", "K");
            Moon_Tame = KeybindLoader.RegisterKeybind(Mod, "月亮: 驯兽", "T");

            // Hunter
            RP_Transformation = KeybindLoader.RegisterKeybind(Mod, "猎人: 形态切换", "Z");
            RP_Flash = KeybindLoader.RegisterKeybind(Mod, "猎人: 火焰闪现", "F");
            RP_Bomb = KeybindLoader.RegisterKeybind(Mod, "猎人: 炸弹", "X");
            RP_Cloak = KeybindLoader.RegisterKeybind(Mod, "猎人: 火焰披风", "C");
            RP_Slash = KeybindLoader.RegisterKeybind(Mod, "猎人: 收割斩击", "G");
            RP_Enchant = KeybindLoader.RegisterKeybind(Mod, "猎人: 武器附魔", "V");
            RP_Skill = KeybindLoader.RegisterKeybind(Mod, "猎人: 蓄力火球", "Q");
            RP_Army = KeybindLoader.RegisterKeybind(Mod, "猎人: 集众", "Z");
            RP_Weather = KeybindLoader.RegisterKeybind(Mod, "猎人: 天气操控", "P");
            RP_Glacier = KeybindLoader.RegisterKeybind(Mod, "猎人: 冰河世纪", "X");

            // Giant
            Giant_Mercury = KeybindLoader.RegisterKeybind(Mod, "巨人: 水银化", "C");
            Giant_Armor = KeybindLoader.RegisterKeybind(Mod, "巨人: 晨曦之铠", "X");
            Giant_Guardian = KeybindLoader.RegisterKeybind(Mod, "巨人: 守护姿态", "Z");

            // Fool [新增]
            Fool_SpiritVision = KeybindLoader.RegisterKeybind(Mod, "愚者: 灵视开关", "C");
            Fool_Divination = KeybindLoader.RegisterKeybind(Mod, "愚者,命运: 占卜术", "J");
            Fool_FlameJump = KeybindLoader.RegisterKeybind(Mod, "愚者: 火焰跳跃", "F");
            Fool_Faceless = KeybindLoader.RegisterKeybind(Mod, "愚者: 无面伪装", "V");
            Fool_Distort = KeybindLoader.RegisterKeybind(Mod, "愚者: 干扰直觉", "G");
            Fool_Threads = KeybindLoader.RegisterKeybind(Mod, "愚者: 灵体之线", "Z");
            Fool_Swap = KeybindLoader.RegisterKeybind(Mod, "愚者: 秘偶互换", "T");
            Fool_Control = KeybindLoader.RegisterKeybind(Mod, "愚者: 控灵/麻痹", "R");
            Fool_History = KeybindLoader.RegisterKeybind(Mod, "愚者: 历史投影", "Y");
            Fool_Borrow = KeybindLoader.RegisterKeybind(Mod, "愚者: 昨日重现", "U");
            Fool_Miracle = KeybindLoader.RegisterKeybind(Mod, "愚者: 奇迹愿望", "V"); // 默认也设为 V
            Fool_Grafting = KeybindLoader.RegisterKeybind(Mod, "愚者: 嫁接", "G");    // 默认设为 G
            Fool_SpiritForm = KeybindLoader.RegisterKeybind(Mod, "愚者: 灵肉转化", "V"); // 默认设为 V
            Fool_RealmSwitch = KeybindLoader.RegisterKeybind(Mod, "愚者: 诡秘之境开关", "L");

            // Marauder [新增]
            Marauder_StealToggle = KeybindLoader.RegisterKeybind(Mod, "错误: 窃取被动开关", "I");
            Marauder_ConceptSteal = KeybindLoader.RegisterKeybind(Mod, "错误: 概念窃取 (位置/距离)", "K");
            MarauderSteal = KeybindLoader.RegisterKeybind(Mod, "错误: 偷窃", "O");
            Marauder_Parasite = KeybindLoader.RegisterKeybind(Mod, "错误: 寄生", "P");

            // Sun [新增]
            Sun_Sing = KeybindLoader.RegisterKeybind(Mod, "太阳：歌颂/赞美", "Z"); // 默认 Z 键
            Sun_Radiance = KeybindLoader.RegisterKeybind(Mod, "太阳：日照/光之术", "X");
            Sun_HolyLight = KeybindLoader.RegisterKeybind(Mod, "太阳：召唤圣光", "C");
            Sun_Oath = KeybindLoader.RegisterKeybind(Mod, "太阳：神圣誓约", "V");
            Sun_FireOcean = KeybindLoader.RegisterKeybind(Mod, "太阳：光明之火", "G");
            Sun_Notarize = KeybindLoader.RegisterKeybind(Mod, "太阳：公证", "J");
            Sun_Messenger = KeybindLoader.RegisterKeybind(Mod, "太阳: 太阳使者", "P");

            // Demoness
            Demoness_Mirror = KeybindLoader.RegisterKeybind(Mod, "魔女: 镜子替身", "Q");
            Demoness_MirrorSwitch = KeybindLoader.RegisterKeybind(Mod, "魔女: 镜子分身", "Z"); // 默认按 Z 键
            Demoness_HairAttack = KeybindLoader.RegisterKeybind(Mod, "魔女: 头发攻击", "X");
            Demoness_SilkControl = KeybindLoader.RegisterKeybind(Mod, "魔女: 蛛丝控制", "C");
            Demoness_DespairSkill = KeybindLoader.RegisterKeybind(Mod, "魔女: 黑焱冰晶", "V");
            Demoness_PetrifySkill = KeybindLoader.RegisterKeybind(Mod, "魔女: 时间石化", "G");
            Demoness_Catastrophe = KeybindLoader.RegisterKeybind(Mod, "魔女:天灾 ", "V");
            Demoness_Apocalypse = KeybindLoader.RegisterKeybind(Mod, "魔女:毁灭 ", "B");

            // Wheel
            Wheel_PsychicStorm = KeybindLoader.RegisterKeybind(Mod, "命运：精神风暴", "N");
            Wheel_Domain = KeybindLoader.RegisterKeybind(Mod, "命运：灾祸光环/厄运领域", "V");
            Wheel_Blessing = KeybindLoader.RegisterKeybind(Mod, "命运：命运赐福", "B");
            Wheel_Dice = KeybindLoader.RegisterKeybind(Mod, "命运：投掷命运骰子", "M");
            Wheel_WordsOfFortune = KeybindLoader.RegisterKeybind(Mod, "命运：福祸之言-福（给友军）", "K");
            Wheel_WordsOfMisfortune = KeybindLoader.RegisterKeybind(Mod, "命运：福祸之言-祸（给敌人）", "L");
            Wheel_Revelation = KeybindLoader.RegisterKeybind(Mod, "命运：命运启示", "U");
            Wheel_FateLoop = KeybindLoader.RegisterKeybind(Mod, "命运：命运循环（巨蛇）", "Y");
            Wheel_Restart = KeybindLoader.RegisterKeybind(Mod, "命运：重启循环（巨蛇）", "H");

            // Door (学徒/门)
            Door_OpenDoor = KeybindLoader.RegisterKeybind(Mod, "学徒：开门（穿墙）", "E");

            // Door 序列8 戏法大师 (默认键可与其他途径重复,玩家可在控件更改)
            Door_TrickSwitch = KeybindLoader.RegisterKeybind(Mod, "戏法大师：切换戏法", "T");
            Door_TrickCast = KeybindLoader.RegisterKeybind(Mod, "戏法大师：释放戏法", "R");

            // Door 序列7 占星人
            Door_Astrology = KeybindLoader.RegisterKeybind(Mod, "占星人：占星术", "G");

            // Door 序列6 记录官
            Door_RecordNormal = KeybindLoader.RegisterKeybind(Mod, "记录官：使用普通记录", "F");
            Door_RecordDivine = KeybindLoader.RegisterKeybind(Mod, "记录官：使用神性记录", "C");

            // Door 序列5 旅行家
            Door_TravelerGate = KeybindLoader.RegisterKeybind(Mod, "旅行家：旅行家之门(传送)", "J");
            Door_Blink = KeybindLoader.RegisterKeybind(Mod, "旅行家：闪现", "K");
        }

        // ===================================================
        // 3. 卸载按键 (Unload)
        // ===================================================
        public override void Unload()
        {
            // Moon
            Moon_Wings = null;
            Moon_BatSwarm = null;
            Moon_PaperFigurine = null;
            Moon_Gaze = null;
            Moon_Shackles = null;
            Moon_Grenade = null;
            Moon_Elixir = null;
            Moon_Moonlight = null;
            Moon_FullMoon = null;
            Moon_SummonGate = null;
            Moon_Tame = null;

            // Hunter
            RP_Transformation = null;
            RP_Flash = null;
            RP_Bomb = null;
            RP_Cloak = null;
            RP_Slash = null;
            RP_Enchant = null;
            RP_Skill = null;
            RP_Army = null;
            RP_Weather = null;
            RP_Glacier = null;

            // Giant
            Giant_Mercury = null;
            Giant_Armor = null;
            Giant_Guardian = null;

            // Fool [新增]
            Fool_SpiritVision = null;
            Fool_Divination = null;
            Fool_FlameJump = null;
            Fool_Faceless = null;
            Fool_Distort = null;
            Fool_Threads = null;
            Fool_Swap = null;
            Fool_Control = null;
            Fool_History = null;
            Fool_Borrow = null;
            Fool_Miracle = null;
            Fool_Grafting = null;
            Fool_SpiritForm = null;

            // Marauder
            Marauder_StealToggle = null;
            MarauderSteal = null;
            Marauder_Parasite = null;
            Marauder_ConceptSteal = null;

            // Sun
            Sun_Sing = null;
            Sun_Radiance = null;
            Sun_HolyLight = null;
            Sun_Oath = null;
            Sun_FireOcean = null;
            Sun_Notarize = null;
            Sun_Messenger = null;

            //魔女
            Demoness_Mirror = null;
            Demoness_MirrorSwitch = null;
            Demoness_HairAttack = null;
            Demoness_SilkControl = null;
            Demoness_DespairSkill = null;
            Demoness_PetrifySkill = null;
            Demoness_Catastrophe = null;
            Demoness_Apocalypse = null;

            //命运
            Wheel_PsychicStorm = null;
            Wheel_Domain = null;
            Wheel_Blessing = null;
            Wheel_Dice = null;
            Wheel_WordsOfFortune = null;
            Wheel_WordsOfMisfortune = null;
            Wheel_Revelation = null;
            Wheel_FateLoop = null;
            Wheel_Restart = null;

            // Door
            Door_OpenDoor = null;
            Door_TrickSwitch = null;
            Door_TrickCast = null;
            Door_Astrology = null;
            Door_RecordNormal = null;
            Door_RecordDivine = null;
            Door_TravelerGate = null;
            Door_Blink = null;
        }
    }
}