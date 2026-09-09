using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用恋人牌

namespace zhashi.Content.Items.Potions.Marauder
{
    public class WormOfTimePotion : LotMItem
    {
        public override string Pathway => "Marauder";
        public override int RequiredSequence => 2; // 需要序列2

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 99;
            Item.consumable = true;
            // 严格保留您原代码的设定 (红色稀有度，1铂金)
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(platinum: 1);
        }

        // === 修改 1：仪式可视化 ===
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            var mp = Main.LocalPlayer.GetModPlayer<LotMPlayer>();

            // 只有序列2才显示进度
            if (mp.baseMarauderSequence == 2)
            {
                // 计算分钟数
                int currentMin = mp.wormRitualTimer / 3600;
                int targetMin = LotMPlayer.WORM_RITUAL_TARGET / 3600;

                string color = (mp.wormRitualTimer >= LotMPlayer.WORM_RITUAL_TARGET) ? "00FF00" : "FF0000";

                tooltips.Add(new TooltipLine(Mod, "Ritual",
                    $"[c/{color}:仪式进度: 城镇混乱 {currentMin} / {targetMin} 分钟]"));

                tooltips.Add(new TooltipLine(Mod, "RitualDesc",
                    "仪式要求：在城镇中开启【欺瞒领域】，让时间陷入混乱。"));
            }
        }

        public override bool CanUseItem(Player player)
        {
            if (!base.CanUseItem(player)) return false;

            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 严格保留您原代码的仪式检查逻辑
            if (modPlayer.wormRitualTimer < LotMPlayer.WORM_RITUAL_TARGET)
            {
                int minLeft = (LotMPlayer.WORM_RITUAL_TARGET - modPlayer.wormRitualTimer) / 3600;
                Main.NewText($"仪式未完成：这座城市还未完全陷入时光的混乱... (还需约 {minLeft + 1} 分钟)", 200, 50, 50);
                return false;
            }
            return true;
        }

        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 晋升逻辑
            modPlayer.baseMarauderSequence = 1;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position);

            Main.NewText("你看见了斑驳的石壁，听见了古老的钟声，你成为了时间的寄生者...", 0, 255, 255);
            Main.NewText("晋升成功：序列1 时之虫！", 255, 215, 0);

            // === 严格保留：您要求的属性直接提升 ===
            // 虽然通常建议写在ResetEffects里，但既然要求不改，这里原样保留
            modPlayer.spiritualityMax += 5000;
            player.statLifeMax2 += 1000;

            return true;
        }

        // === 修改 2：配方优化 (双配方) ===
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<LoversCard>(), // 核心：恋人牌
                (ItemID.BottledWater, 1),
                (ItemID.LunarBar, 5),          // 夜明锭
                (ItemID.FragmentStardust, 10), // 星尘碎片
                (ItemID.Timer1Second, 1),      // 1秒计时器
                (ItemID.Book, 1),              // 书
                (ItemID.Ectoplasm, 5)          // 灵气
            );
        }
    }
}