using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用恋人牌

namespace zhashi.Content.Items.Potions.Marauder
{
    public class MentorPotion : LotMItem
    {
        // 定义途径和前置序列
        public override string Pathway => "Marauder";
        public override int RequiredSequence => 4; // 需要序列4(寄生者)才能服用

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 99;
            Item.consumable = true;
            Item.rare = ItemRarityID.Lime; // 序列3 圣者 (青柠色)
            Item.value = Item.sellPrice(gold: 20);
        }

        // 1. 显示仪式进度
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            var mp = Main.LocalPlayer.GetModPlayer<LotMPlayer>();
            // 只有当前是序列4时才显示仪式，避免干扰
            if (mp.baseMarauderSequence == 4)
            {
                // 动态变色：完成变绿，未完成变红
                string color = (mp.mentorRitualProgress >= LotMPlayer.MENTOR_RITUAL_TARGET) ? "00FF00" : "FF0000";

                tooltips.Add(new TooltipLine(Mod, "Ritual",
                    $"[c/{color}:仪式进度: 误导 {mp.mentorRitualProgress} / {LotMPlayer.MENTOR_RITUAL_TARGET} 个冤魂]"));

                tooltips.Add(new TooltipLine(Mod, "RitualDesc",
                    "仪式要求：利用“混乱”能力误导并致死强大的敌人。"));
            }
        }

        // 2. 使用检查：增加仪式判断
        public override bool CanUseItem(Player player)
        {
            // 先让基类检查是否是序列4
            if (!base.CanUseItem(player)) return false;

            var mp = player.GetModPlayer<LotMPlayer>();

            // 检查仪式是否完成
            if (mp.baseMarauderSequence == 4 && mp.mentorRitualProgress < LotMPlayer.MENTOR_RITUAL_TARGET)
            {
                int left = LotMPlayer.MENTOR_RITUAL_TARGET - mp.mentorRitualProgress;
                // 使用你原设定的颜色 (0, 255, 127) 或标准警告红
                Main.NewText($"仪式未完成：还需误导并致死 {left} 个强大的灵魂...", 255, 50, 50);
                return false;
            }
            return true;
        }

        // 3. 晋升逻辑
        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 晋升
            modPlayer.baseMarauderSequence = 3;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position);

            Main.NewText("你感觉思维变得冰冷而滑腻，规则在你眼中出现了无数漏洞...", 0, 255, 127);
            Main.NewText("晋升成功：序列3 欺瞒导师！", 255, 215, 0);

            return true;
        }

        // 4. 配方优化
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<LoversCard>(), // 核心：恋人牌
                (ItemID.BottledWater, 1),
                (ItemID.Ectoplasm, 10),      // 灵气/冤魂
                (ItemID.PutridScent, 1),     // 腐臭
                (ItemID.VialofVenom, 5),     // 毒液
                (ItemID.JungleSpores, 20),   // 孢子
                (ItemID.GoldBar, 5)          // 黄金
            );
        }
    }
}