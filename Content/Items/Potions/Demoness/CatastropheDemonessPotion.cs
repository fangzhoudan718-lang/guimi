using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using zhashi.Content.Items.Accessories; // 引用魔女牌
using zhashi.Content.Systems; // 引用系统

namespace zhashi.Content.Items.Potions.Demoness
{
    public class CatastropheDemonessPotion : LotMItem
    {
        public override string Pathway => "Demoness";
        public override int RequiredSequence => 3; // 必须先成为序列3不老魔女

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("灾难魔女魔药");
            // Tooltip.SetDefault("序列2：灾难魔女 (天使位格)\n" +
            //                  "能力：掌控天灾，引发寒潮、瘟疫与飓风，与自然灾害融为一体。\n" +
            //                  "仪式：在这一刻，你就是天灾。");
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.consumable = true;
            Item.rare = ItemRarityID.Red; // 红色品质 (天使)
            Item.value = Item.sellPrice(platinum: 1); // 极其昂贵
        }

        // --- 核心检查：能否使用 ---
        public override bool CanUseItem(Player player)
        {
            // 1. 基类检查序列等级 (是否是序列3)
            if (!base.CanUseItem(player)) return false;

            // 2. 仪式判定逻辑
            // 判定 A: 环境必须恶劣 (暴雨、沙尘暴、暴雪)
            bool weatherCondition = Main.raining || player.ZoneSandstorm || player.ZoneSnow;

            // 判定 B: 必须处于大型威胁中 (Boss存活 或 入侵事件 或 血月/日食)
            bool threatCondition = false;
            if (Main.invasionType > 0 || Main.bloodMoon || Main.eclipse) threatCondition = true;
            else
            {
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (npc.boss)
                    {
                        threatCondition = true;
                        break;
                    }
                }
            }

            // 检查
            if (!weatherCondition || !threatCondition)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    Main.NewText("仪式未完成！魔药不仅需要材料，还需要环境的共鸣...", 255, 50, 50);

                    if (!weatherCondition)
                        Main.NewText("提示：缺少灾难性的天气 (暴雨/暴雪/沙尘暴)。", 200, 200, 200);

                    if (!threatCondition)
                        Main.NewText("提示：缺少宏大的毁灭现场 (需在 Boss战 或 大规模入侵事件 中)。", 200, 200, 200);
                }
                return false;
            }

            return true;
        }

        // --- UI提示：显示仪式条件 ---
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            Player player = Main.LocalPlayer;

            // 实时检测条件状态
            bool weather = Main.raining || player.ZoneSandstorm || player.ZoneSnow;
            bool threat = (Main.invasionType > 0 || Main.bloodMoon || Main.eclipse);
            if (!threat) { foreach (NPC n in Main.ActiveNPCs) if (n.boss) { threat = true; break; } }

            string c1 = weather ? "00FF00" : "FF0000";
            string c2 = threat ? "00FF00" : "FF0000";
            string s1 = weather ? "(达成)" : "(未达成)";
            string s2 = threat ? "(达成)" : "(未达成)";

            tooltips.Add(new TooltipLine(Mod, "Ritual1", $"[c/{c1}:仪式条件1：灾难天气 {s1}]"));
            tooltips.Add(new TooltipLine(Mod, "Ritual2", $"[c/{c2}:仪式条件2：毁灭现场(Boss/事件) {s2}]"));
        }

        // --- 晋升逻辑 ---
        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

                // 晋升为序列2
                modPlayer.baseDemonessSequence = 2;
                modPlayer.currentDemonessSequence = 2;

                // 播放震撼音效
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.Center);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, player.Center); // 雷声

                Main.NewText("你的存在即是毁灭本身。", 255, 0, 0);
                Main.NewText("晋升成功！序列2：灾难魔女！", 255, 0, 255);

                // 晋升特效：强制引发更大的灾难 (比如直接满风速)
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Main.windSpeedCurrent = 1.0f; // 狂风
                    if (!Main.raining) Main.StartRain(); // 暴雨
                    if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.WorldData);
                }
            }
            return true;
        }

        // --- 配方注册 ---
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<DemonessCard>(), // 核心：魔女牌

                // 主材料：对应月亮领主后的力量
                (ItemID.FragmentNebula, 10),  // 星云碎片 (魔法/神秘)
                (ItemID.FragmentSolar, 10),   // 日耀碎片 (毁灭力量)

                // 辅助材料：灾难的象征
                (ItemID.RainCloud, 50),       // 洪水/雨云
                (ItemID.ChlorophyteBar, 5),   // 丰饶大地 (叶绿锭)
                (ItemID.Ectoplasm, 10),       // 灵体/镜中世界
                (ItemID.CursedFlame, 20)      // 灾难死者/诅咒
            );
        }
    }
}