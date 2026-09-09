using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio; // 【核心修复】必须引用这个才能使用 SoundEngine
using Microsoft.Xna.Framework;
using zhashi.Content;

namespace zhashi.Content.Items.Materials
{
    public class WeatherRune : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 99;
            Item.rare = ItemRarityID.LightRed;
            Item.value = Item.sellPrice(0, 10, 0, 0);
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 30;      // 加快一点速度，方便连续使用
            Item.useAnimation = 30;
            Item.consumable = false;
            Item.autoReuse = true;  // 允许按住连续使用
        }

        public override bool CanUseItem(Player player)
        {
            LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();
            // 只有 序列3：战争主教 或更高级 可以使用
            if (modPlayer.currentHunterSequence <= 3)
            {
                return true;
            }
            return false;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

                // --- 仪式逻辑 ---
                // 只有在序列3且仪式未完成时才计算
                if (modPlayer.currentHunterSequence == 3 && !modPlayer.weatherRitualComplete)
                {
                    modPlayer.weatherRitualCount++;
                    modPlayer.weatherRitualTimer = 300; // 重置倒计时为5秒 (300帧)，保证只要连续点就不会断

                    // 视觉特效
                    CombatText.NewText(player.getRect(), Color.Cyan, $"{modPlayer.weatherRitualCount}/10", true);
                    SoundEngine.PlaySound(SoundID.Item4, player.position);

                    if (modPlayer.weatherRitualCount >= 10)
                    {
                        modPlayer.weatherRitualComplete = true;
                        Main.NewText("天气符文产生了强烈的共鸣！晋升仪式条件已满足！", 0, 255, 255);
                        SoundEngine.PlaySound(SoundID.Item29, player.position);
                    }
                }
                // ----------------

                // 原有的天气切换功能
                if (Main.raining)
                {
                    Main.StopRain();
                    if (modPlayer.weatherRitualComplete) Main.NewText("风雨平息...", 100, 255, 255);
                }
                else
                {
                    Main.StartRain();
                    if (modPlayer.weatherRitualComplete) Main.NewText("风暴降临！", 0, 200, 255);
                }
            }
            return true;
        }
        public override void AddRecipes()
        {
            CreateRecipe()
                // 核心材料：飞翔之魂 (象征对天空和天气的掌控)
                .AddIngredient(ItemID.SoulofFlight, 5)
                // 象征材料：雨云块 (直接对应降雨效果)
                .AddIngredient(ItemID.RainCloud, 20)
                // 魔法材料：蓝宝石 (象征水属性的魔力引导)
                .AddIngredient(ItemID.Sapphire, 3)
                // 载体材料：金锭 (作为符文的贵金属载体)
                .AddIngredient(ItemID.GoldBar, 5)
                // 制作环境：秘银砧/山铜砧 (困难模式工艺)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}