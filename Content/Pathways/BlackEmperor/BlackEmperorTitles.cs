namespace zhashi.Content.Pathways.BlackEmperor
{
    /// <summary>
    /// 黑皇帝的「名义」。称号是这条途径的专属系统：
    /// 只要踏上这条路（哪怕还在序列九），就能靠做成一件事去换一个称呼，
    /// 再把那个称呼戴在身上，领它带来的被动、加成，或者一项主动能力。
    /// </summary>
    public enum BlackEmperorTitle
    {
        Nameless = 0,       // 无名者（默认）
        MushroomKing = 1,   // 蘑菇王
        SeaKing = 2,        // 海王
        DragonSlayer = 3,   // 屠龙者
        Delver = 4,         // 地脉行者
        Tyrant = 5,         // 暴君
        Conqueror = 6,      // 征服者
        Godslayer = 7,      // 弑神者
        Legislator = 8,     // 立法者
        Gambler = 9,        // 赌徒
        Abyssal = 10,       // 深渊行者
        KingSlayer = 11     // 猎王者
    }

    /// <summary>称号的目录：名称、获取条件、效果说明，以及解锁所需的进度。</summary>
    public static class BlackEmperorTitles
    {
        public const int Count = 12;

        // 解锁门槛
        public const int MushroomTarget = 300;
        public const int FishTarget = 100;
        public const int DragonTarget = 10;
        public const int DelveTarget = 2000;
        public const int BloodMoonTarget = 200;
        public const int AbyssTarget = 500;
        public const int KingTarget = 50;
        public const int LawTarget = 50;
        public const int BribeTarget = 30;

        public static string Name(BlackEmperorTitle title) => title switch
        {
            BlackEmperorTitle.MushroomKing => "蘑菇王",
            BlackEmperorTitle.SeaKing => "海王",
            BlackEmperorTitle.DragonSlayer => "屠龙者",
            BlackEmperorTitle.Delver => "地脉行者",
            BlackEmperorTitle.Tyrant => "暴君",
            BlackEmperorTitle.Conqueror => "征服者",
            BlackEmperorTitle.Godslayer => "弑神者",
            BlackEmperorTitle.Legislator => "立法者",
            BlackEmperorTitle.Gambler => "赌徒",
            BlackEmperorTitle.Abyssal => "深渊行者",
            BlackEmperorTitle.KingSlayer => "猎王者",
            _ => "无名者"
        };

        public static string Requirement(BlackEmperorTitle title) => title switch
        {
            BlackEmperorTitle.MushroomKing => $"亲手采下 {MushroomTarget} 朵蘑菇",
            BlackEmperorTitle.SeaKing => $"钓上 {FishTarget} 条鱼（宝匣按三条计）",
            BlackEmperorTitle.DragonSlayer => $"斩杀 {DragonTarget} 条龙",
            BlackEmperorTitle.Delver => $"挖穿 {DelveTarget} 格土石",
            BlackEmperorTitle.Tyrant => $"在血月之下手刃 {BloodMoonTarget} 个敌人",
            BlackEmperorTitle.Conqueror => "打赢一场入侵（哥布林、海盗或火星）",
            BlackEmperorTitle.Godslayer => "击败月亮领主",
            BlackEmperorTitle.Legislator => $"立下 {LawTarget} 条律令",
            BlackEmperorTitle.Gambler => $"行贿 {BribeTarget} 次",
            BlackEmperorTitle.Abyssal => $"在城市地底斩杀 {AbyssTarget} 个敌人",
            BlackEmperorTitle.KingSlayer => $"累计斩杀 {KingTarget} 个 Boss",
            _ => "生来就有的称呼"
        };

        public static string Effect(BlackEmperorTitle title) => title switch
        {
            BlackEmperorTitle.MushroomKing => "周身浮起带伤害的蘑菇孢子，并额外获得减伤与生命",
            BlackEmperorTitle.SeaKing => "水中自由呼吸、游得更快、打得也更重",
            BlackEmperorTitle.DragonSlayer => "伤害提高，并免疫击退",
            BlackEmperorTitle.Delver => "挖掘速度大幅提升，防御提高",
            BlackEmperorTitle.Tyrant => "伤害与暴击大幅提高",
            BlackEmperorTitle.Conqueror => "伤害、移速提高，并免疫击退",
            BlackEmperorTitle.Godslayer => "伤害、防御与减伤全面提升",
            BlackEmperorTitle.Legislator => "律令冷却缩短、持续时间延长，防御 +6",
            BlackEmperorTitle.Gambler => "贿赂更便宜、买来的时间更长，幸运 +0.3",
            BlackEmperorTitle.Abyssal => "身处地下时伤害与防御提高，并免疫黑暗",
            BlackEmperorTitle.KingSlayer => "对 Boss 打得更重，Boss 战里更抗打",
            _ => "全伤害小幅提高"
        };

        /// <summary>这个称号是否带主动能力（Shift + 称号键释放）。</summary>
        public static bool HasActive(BlackEmperorTitle title) => title is
            BlackEmperorTitle.MushroomKing or BlackEmperorTitle.SeaKing or BlackEmperorTitle.DragonSlayer or
            BlackEmperorTitle.Tyrant or BlackEmperorTitle.Godslayer;

        public static string ActiveName(BlackEmperorTitle title) => title switch
        {
            BlackEmperorTitle.MushroomKing => "孢子爆发",
            BlackEmperorTitle.SeaKing => "潮汐",
            BlackEmperorTitle.DragonSlayer => "龙威",
            BlackEmperorTitle.Tyrant => "威压",
            BlackEmperorTitle.Godslayer => "神陨",
            _ => "无"
        };
    }
}
