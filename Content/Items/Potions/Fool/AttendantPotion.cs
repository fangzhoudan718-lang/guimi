using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using zhashi.Content.Items.Accessories; // 引用愚者牌

namespace zhashi.Content.Items.Potions.Fool
{
    public class AttendantPotion : LotMItem
    {
        // 设定途径和前置序列 (需要序列2 奇迹师)
        public override string Pathway => "Fool";
        public override int RequiredSequence => 2;

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 30;
            Item.consumable = true;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.sellPrice(platinum: 50);
        }

        // 【核心检查】能否使用 (仪式进度检查)
        public override bool CanUseItem(Player player)
        {
            // 1. 先调用基类检查序列要求
            if (!base.CanUseItem(player)) return false;

            LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

            // 2. 检查仪式：只要标记完成，或者当前计数>=10，都算通过
            bool conditionMet = modPlayer.attendantRitualComplete || modPlayer.attendantRitualProgress >= 10;

            if (!conditionMet)
            {
                // 可选：添加一条提示告诉玩家为什么不能用
                if (player.whoAmI == Main.myPlayer)
                {
                    Main.NewText($"仪式未完成：需转化 10 个城镇NPC (当前: {modPlayer.attendantRitualProgress}/10)", 255, 50, 50);
                }
                return false;
            }

            return true;
        }

        // 【UI提示】显示仪式进度
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            Player player = Main.LocalPlayer;
            LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

            int count = modPlayer.attendantRitualProgress;
            bool complete = modPlayer.attendantRitualComplete || count >= 10;

            if (count > 10) count = 10; // 显示上限锁定为10

            string color = complete ? "00FF00" : "FF0000"; // 完成变绿，未完成变红
            string status = complete ? " (已完成)" : "";

            tooltips.Add(new TooltipLine(Mod, "Ritual", $"[c/{color}:仪式要求：转化10个城镇NPC ({count}/10){status}]"));
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();
                
                // 晋升逻辑
                modPlayer.baseFoolSequence = 1;

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, player.position);
                Main.NewText("城镇在你的意志下起舞，诡秘的帷幕已然拉开...", 148, 0, 211);
                Main.NewText("晋升成功！序列1：诡秘侍者！(天使之王)", 255, 0, 255);
            }
            return true;
        }

        public override void AddRecipes()
        {
            // 使用智能配方生成器
            // 自动注册：
            // 1. 消耗石板的配方
            // 2. 持有愚者牌(不消耗石板)的配方
            CreateDualRecipe(
                ModContent.ItemType<FoolCard>(), 

                (ItemID.BottledWater, 1),
                (ItemID.LunarBar, 20),      // 夜明锭
                (ItemID.Ectoplasm, 5),      // 灵气
                (ItemID.SoulofFlight, 5),   // 飞翔之魂
                (ItemID.SoulofLight, 5),    // 光明之魂
                (ItemID.SoulofNight, 5),    // 暗影之魂
                (ItemID.UnicornHorn, 1),    // 独角兽角
                (ItemID.PixieDust, 10),     // 精灵尘
                (ItemID.CrystalShard, 5),   // 水晶碎块
                (ItemID.CursedFlame, 5),    // 咒火
                (ItemID.Ichor, 5)           // 灵液
            );
        }
    }
}