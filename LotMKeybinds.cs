using Terraria.ModLoader;

using Terraria.GameInput;
using Microsoft.Xna.Framework.Input;

namespace zhashi
{
    public class LotMKeybinds : ModSystem
    {
        public static string GetBindingText(ModKeybind keybind, string fallback = "未绑定")
        {
            if (keybind == null) return fallback;
            var keyboard = keybind.GetAssignedKeys(InputMode.Keyboard);
            var gamepad = keybind.GetAssignedKeys(InputMode.XBoxGamepad);
            if (keyboard.Count > 0 && gamepad.Count > 0) return $"{string.Join("/", keyboard)} · {string.Join("/", gamepad)}";
            if (keyboard.Count > 0) return string.Join("/", keyboard);
            if (gamepad.Count > 0) return string.Join("/", gamepad);
            return "未绑定";
        }
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

        // --- 门途径高序列（序列4-1）---
        public static ModKeybind Door_SecretSpace { get; private set; }        // 秘法师：空间隐藏
        public static ModKeybind Door_Banish { get; private set; }             // 秘法师：放逐
        public static ModKeybind Door_SpatialPrison { get; private set; }      // 漫游者：空间牢笼/仪式封印
        public static ModKeybind Door_SpaceTear { get; private set; }           // 漫游者：撕裂空间
        public static ModKeybind Door_DimensionalSight { get; private set; }   // 旅法师：维度之视
        public static ModKeybind Door_Reenact { get; private set; }            // 旅法师：再现
        public static ModKeybind Door_TimeSpaceMaze { get; private set; }      // 星之匙：时空迷宫
        public static ModKeybind Door_SpaceShatter { get; private set; }       // 星之匙：空间破碎

        // --- 黑皇帝途径 (BlackEmperor) ---
        public static ModKeybind BlackEmperor_Contract { get; private set; }   // 契约 / 律令
        public static ModKeybind BlackEmperor_Bribe { get; private set; }      // 贿赂
        public static ModKeybind BlackEmperor_Twist { get; private set; }      // 扭曲与定义
        public static ModKeybind BlackEmperor_Entropy { get; private set; }    // 熵之公爵：兑现
        public static ModKeybind BlackEmperor_Lawless { get; private set; }     // 野蛮人：无法之地
        public static ModKeybind BlackEmperor_Unstoppable { get; private set; } // 野蛮人：硬闯
        public static ModKeybind BlackEmperor_Chaos { get; private set; }       // 混乱导师：混乱场
        public static ModKeybind BlackEmperor_Gift { get; private set; }        // 堕落伯爵：赠予
        public static ModKeybind BlackEmperor_Amplify { get; private set; }     // 堕落伯爵：放大
        public static ModKeybind BlackEmperor_Rage { get; private set; }        // 狂乱法师：狂乱
        public static ModKeybind BlackEmperor_Title { get; private set; }       // 名义：切换称号 / Shift 释放称号能力
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
            Door_OpenDoor = KeybindLoader.RegisterKeybind(Mod, "门：开门（穿墙）", "E");

            // Door 序列8 戏法大师 (默认键可与其他途径重复,玩家可在控件更改)
            Door_TrickSwitch = KeybindLoader.RegisterKeybind(Mod, "门：切换戏法", "T");
            Door_TrickCast = KeybindLoader.RegisterKeybind(Mod, "门：释放戏法", "R");

            // Door 序列7 占星人
            Door_Astrology = KeybindLoader.RegisterKeybind(Mod, "门：占星术", "G");

            // Door 序列6 记录官
            Door_RecordNormal = KeybindLoader.RegisterKeybind(Mod, "门：使用普通记录", "F");
            Door_RecordDivine = KeybindLoader.RegisterKeybind(Mod, "门：使用神性记录", "C");

            // Door 序列5 旅行家
            Door_TravelerGate = KeybindLoader.RegisterKeybind(Mod, "门：旅行家之门（传送）", "J");
            Door_Blink = KeybindLoader.RegisterKeybind(Mod, "门：闪现", "K");
            Door_SecretSpace = KeybindLoader.RegisterKeybind(Mod, "门：空间隐藏", "Z");
            Door_Banish = KeybindLoader.RegisterKeybind(Mod, "门：放逐", "X");
            Door_SpatialPrison = KeybindLoader.RegisterKeybind(Mod, "门：空间牢笼", "Q");
            Door_SpaceTear = KeybindLoader.RegisterKeybind(Mod, "门：撕裂空间", "V");
            Door_DimensionalSight = KeybindLoader.RegisterKeybind(Mod, "门：维度之视", "B");
            Door_Reenact = KeybindLoader.RegisterKeybind(Mod, "门：再现", "N");
            Door_TimeSpaceMaze = KeybindLoader.RegisterKeybind(Mod, "门：时空迷宫", "M");
            Door_SpaceShatter = KeybindLoader.RegisterKeybind(Mod, "门：空间破碎", "H");

            // ===================================================
            // 黑皇帝途径（律师 → 弑序亲王）
            // 字母键已被现有途径占满，默认取标点区，零冲突；玩家可在「控件」里自行改键。
            // 用 Keys 枚举重载注册，避免字符串默认值的解析歧义。
            // ===================================================
            BlackEmperor_Contract = KeybindLoader.RegisterKeybind(Mod, "黑皇帝：契约", Keys.OemComma);
            BlackEmperor_Bribe = KeybindLoader.RegisterKeybind(Mod, "黑皇帝：贿赂", Keys.OemPeriod);
            BlackEmperor_Twist = KeybindLoader.RegisterKeybind(Mod, "黑皇帝：扭曲与定义", Keys.OemQuestion);
            BlackEmperor_Entropy = KeybindLoader.RegisterKeybind(Mod, "黑皇帝：兑现", Keys.OemSemicolon);
            BlackEmperor_Lawless = KeybindLoader.RegisterKeybind(Mod, "黑皇帝：无法之地", Keys.OemOpenBrackets);
            BlackEmperor_Unstoppable = KeybindLoader.RegisterKeybind(Mod, "黑皇帝：硬闯", Keys.OemCloseBrackets);
            BlackEmperor_Chaos = KeybindLoader.RegisterKeybind(Mod, "黑皇帝：混乱场", Keys.OemPipe);
            BlackEmperor_Gift = KeybindLoader.RegisterKeybind(Mod, "黑皇帝：赠予", Keys.OemQuotes);
            BlackEmperor_Amplify = KeybindLoader.RegisterKeybind(Mod, "黑皇帝：放大", Keys.OemPlus);
            BlackEmperor_Rage = KeybindLoader.RegisterKeybind(Mod, "黑皇帝：狂乱", Keys.OemMinus);
            BlackEmperor_Title = KeybindLoader.RegisterKeybind(Mod, "黑皇帝：名义", Keys.OemTilde);
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
            Door_SecretSpace = null;
            Door_Banish = null;
            Door_SpatialPrison = null;
            Door_SpaceTear = null;
            Door_DimensionalSight = null;
            Door_Reenact = null;
            Door_TimeSpaceMaze = null;
            Door_SpaceShatter = null;

            // BlackEmperor
            BlackEmperor_Contract = null;
            BlackEmperor_Bribe = null;
            BlackEmperor_Twist = null;
            BlackEmperor_Entropy = null;
            BlackEmperor_Lawless = null;
            BlackEmperor_Unstoppable = null;
            BlackEmperor_Chaos = null;
            BlackEmperor_Gift = null;
            BlackEmperor_Amplify = null;
            BlackEmperor_Rage = null;
            BlackEmperor_Title = null;
        }
    }
}
