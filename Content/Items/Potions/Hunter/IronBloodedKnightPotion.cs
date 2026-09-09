using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用红祭司牌

namespace zhashi.Content.Items.Potions.Hunter
{
    public class IronBloodedKnightPotion : LotMItem
    {
        // 1. 定义途径和前置序列
        public override string Pathway => "Hunter";
        public override int RequiredSequence => 5; // 需要序列5 (收割者)

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
            Item.rare = ItemRarityID.Yellow; // 序列4 半神 (黄色)
            Item.value = Item.buyPrice(gold: 50);
        }

        // 2. 显示仪式进度
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            var modPlayer = Main.LocalPlayer.GetModPlayer<LotMPlayer>();

            // 只有序列5才显示进度
            if (modPlayer.baseHunterSequence == 5)
            {
                string statusColor = (modPlayer.ironBloodRitualProgress >= LotMPlayer.IRON_BLOOD_RITUAL_TARGET) ? "00FF00" : "FF0000";
                string progressText = $"[c/{statusColor}:征服进度: {modPlayer.ironBloodRitualProgress} / {LotMPlayer.IRON_BLOOD_RITUAL_TARGET}]";

                tooltips.Add(new TooltipLine(Mod, "RitualDesc", "晋升仪式: 在至少有5名随从的情况下，无死亡累计击杀100个敌人。"));
                tooltips.Add(new TooltipLine(Mod, "RitualProgress", progressText));
            }
        }

        // 3. 使用条件检查
        public override bool CanUseItem(Player player)
        {
            // 基类先检查是否是序列5
            if (!base.CanUseItem(player)) return false;

            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 额外检查仪式是否完成
            if (modPlayer.baseHunterSequence == 5 && modPlayer.ironBloodRitualProgress < LotMPlayer.IRON_BLOOD_RITUAL_TARGET)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    Main.NewText("你的军队还未经历足够的铁与血...", 255, 50, 50);
                    Main.NewText("提示：需要保持5名随从并不死击杀100个敌人。", 200, 200, 200);
                }
                return false;
            }

            return true;
        }

        // 4. 晋升逻辑
        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 晋升
            modPlayer.baseHunterSequence = 4;

            // 音效与文本
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position);

            Main.NewText("火焰与钢铁在你体内重铸！", 255, 69, 0); // 熔岩橙
            Main.NewText("晋升成功：序列4 铁血骑士！", 255, 215, 0); // 金色
            Main.NewText("能力：【钢铁之躯】 | 【火焰化】(K) | 【集众】(L)", 255, 255, 255);

            return true;
        }

        // 5. 双配方支持
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<RedPriestCard>(), // 核心：红祭司牌
                (ItemID.BottledWater, 1),
                (ItemID.BeetleHusk, 10),    // 甲虫外壳 (坚硬防御)
                (ItemID.HellstoneBar, 10),  // 狱岩锭 (火焰与钢铁)
                (ItemID.SoulofMight, 5),    // 力量之魂 (力量)
                (ItemID.SoulofSight, 5)     // 视域之魂 (统御)
            );
        }
    }
}