using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用太阳牌

namespace zhashi.Content.Items.Potions.Sun
{
    public class WhiteAngelPotion : LotMItem
    {
        // 1. 定义途径和前置序列
        public override string Pathway => "Sun";
        public override int RequiredSequence => 2; // 需要序列2 (逐光者)

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 40;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 30;
            Item.consumable = true;
            Item.rare = ItemRarityID.Purple; // 序列1 天使之王 (紫色)
            Item.value = Item.sellPrice(platinum: 20);
        }

        // 2. 显示仪式条件
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            var modPlayer = Main.LocalPlayer.GetModPlayer<LotMPlayer>();

            if (modPlayer.baseSunSequence == 2)
            {
                // 环境与条件检测 (实时)
                bool isNoon = Main.dayTime && Main.time >= 27000 && Main.time <= 36000;

                int townNPCs = 0;
                for (int i = 0; i < Main.maxNPCs; i++) { if (Main.npc[i].active && Main.npc[i].townNPC) townNPCs++; }
                bool hasFollowers = townNPCs >= 15;

                bool hasSymbol = Main.LocalPlayer.HasItem(ItemID.TerraBlade) || Main.LocalPlayer.HasItem(ItemID.LastPrism) || Main.LocalPlayer.HasItem(ItemID.Meowmere);

                string condColor = (isNoon && hasFollowers && hasSymbol) ? "00FF00" : "FF0000";

                string timeText = isNoon ? "正午" : "需正午(12:00-14:00)";
                string npcText = hasFollowers ? "信徒充足" : $"信徒不足({townNPCs}/15)";
                string itemText = hasSymbol ? "圣物" : "缺秩序圣物(泰拉刃等)";

                tooltips.Add(new TooltipLine(Mod, "RitualCond",
                    $"[c/{condColor}:仪式条件: {timeText} + {npcText} + {itemText}]"));
            }
        }

        // 3. 使用条件检查
        public override bool CanUseItem(Player player)
        {
            // 基类先检查序列
            if (!base.CanUseItem(player)) return false;

            var modPlayer = player.GetModPlayer<LotMPlayer>();

            if (modPlayer.baseSunSequence == 2)
            {
                bool isNoon = Main.dayTime && Main.time >= 27000 && Main.time <= 36000;

                int townNPCs = 0;
                for (int i = 0; i < Main.maxNPCs; i++) { if (Main.npc[i].active && Main.npc[i].townNPC) townNPCs++; }
                bool hasFollowers = townNPCs >= 15;

                bool hasSymbol = player.HasItem(ItemID.TerraBlade) || player.HasItem(ItemID.LastPrism) || player.HasItem(ItemID.Meowmere);

                if (!isNoon || !hasFollowers || !hasSymbol)
                {
                    if (player.whoAmI == Main.myPlayer)
                        Main.NewText("仪式条件未满足：需在正午时刻、拥有至少15名信徒(NPC)，并手持秩序圣物。", 255, 50, 50);
                    return false;
                }
            }
            return true;
        }

        // 4. 晋升逻辑
        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 晋升
            modPlayer.baseSunSequence = 1;

            // 音效
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29, player.position);

            Main.NewText("百万信徒的祈祷声汇聚成洪流...", 255, 255, 255);
            Main.NewText("你化身为纯白的秩序，神圣之国降临人间！", 255, 255, 0); // 神圣金
            Main.NewText("晋升天使之王！序列1：纯白天使。", 255, 255, 255);

            // 天使之王晋升特效：纯白圣光
            for (int i = 0; i < 300; i++)
            {
                Vector2 speed = Main.rand.NextVector2Circular(30, 30);
                Dust d = Dust.NewDustPerfect(player.Center, DustID.WhiteTorch, speed, 0, default, 5f);
                d.noGravity = true;
            }

            return true;
        }

        // 5. 双配方支持
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<SunCard>(), // 核心：太阳牌
                (ItemID.BottledWater, 1),
                (ItemID.FragmentSolar, 50),     // 日耀
                (ItemID.EmpressButterfly, 1),   // 七彩草蛉 (纯洁神性)
                (ItemID.HallowedBar, 50),       // 神圣锭
                (ItemID.LunarBar, 50),          // 夜明锭 (月亮领主后材料)
                (ItemID.LihzahrdPowerCell, 5),  // 能源
                (ItemID.SoulofLight, 50),       // 光
                (ItemID.Ectoplasm, 30)          // 灵
            );
        }
    }
}