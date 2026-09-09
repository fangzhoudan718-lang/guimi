using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用红祭司牌

namespace zhashi.Content.Items.Potions.Hunter
{
    public class ConquerorPotion : LotMItem
    {
        // 1. 定义途径和前置序列
        public override string Pathway => "Hunter";
        public override int RequiredSequence => 2; // 需要序列2 (天气术士)

        public override void SetStaticDefaults()
        {
            // 名字和描述在 HJSON 中
        }

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
            Item.rare = ItemRarityID.Red; // 序列1 红色
            Item.value = Item.sellPrice(gold: 50);
        }

        // 2. 仪式状态显示
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // 先调用基类显示“需要序列2”
            base.ModifyTooltips(tooltips);

            Player player = Main.LocalPlayer;
            LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

            // 只有当玩家是序列2时，才显示详细的仪式条件
            if (modPlayer.baseHunterSequence == 2)
            {
                // 仪式检测
                string ritualColor = modPlayer.conquerorRitualComplete ? "00FF00" : "FF0000";
                string ritualText = modPlayer.conquerorRitualComplete ? "仪式已完成" : "仪式未完成";

                tooltips.Add(new TooltipLine(Mod, "RitualReq", $"[c/{ritualColor}:仪式条件: {ritualText}]"));

                if (!modPlayer.conquerorRitualComplete)
                {
                    tooltips.Add(new TooltipLine(Mod, "RitualHint", "仪式说明: 使用[征服者特性]让世界静默，并确认全图无敌手。"));
                }
            }
        }

        // 3. 使用条件检查
        public override bool CanUseItem(Player player)
        {
            // 基类先检查是否是序列2
            if (!base.CanUseItem(player)) return false;

            LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

            // 额外检查仪式是否完成
            if (modPlayer.baseHunterSequence == 2 && !modPlayer.conquerorRitualComplete)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    Main.NewText("仪式未完成！你还未征服这片大地...", 255, 50, 50);
                }
                return false;
            }

            return true;
        }

        // 4. 晋升逻辑
        public override bool? UseItem(Player player)
        {
            LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

            // 晋升
            modPlayer.baseHunterSequence = 1;

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position);

            if (player.whoAmI == Main.myPlayer)
            {
                Main.NewText("晋升成功！序列1：征服者！", 255, 69, 0); // 橙红色
                Main.NewText("战火已熄，生态恢复...", 100, 255, 100);
            }

            // 恢复刷怪 (这是您特有的系统逻辑，保留)
            ConquerorSpawnSystem.StopSpawning = false;

            return true;
        }

        // 5. 双配方支持
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<RedPriestCard>(), // 核心：红祭司牌
                (ItemID.BottledWater, 1),
                (ModContent.ItemType<Materials.ConquerorCharacteristic>(), 1) // 核心材料：征服者特性                                                            
            );
        }
    }
}