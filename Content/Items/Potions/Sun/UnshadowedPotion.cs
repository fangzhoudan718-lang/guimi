using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用太阳牌
using zhashi.Content.Items.Materials; // 引用 UnshadowedCross (确保有这个命名空间)

namespace zhashi.Content.Items.Potions.Sun
{
    public class UnshadowedPotion : LotMItem
    {
        // 1. 定义途径和前置序列
        public override string Pathway => "Sun";
        public override int RequiredSequence => 5; // 需要序列5 (光之祭司)

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
            Item.rare = ItemRarityID.Yellow; // 序列4 半神 (黄色)
            Item.value = Item.sellPrice(gold: 20);
        }

        // 2. 显示仪式条件
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            var modPlayer = Main.LocalPlayer.GetModPlayer<LotMPlayer>();

            if (modPlayer.baseSunSequence == 5)
            {
                // 检查审判进度
                bool killReady = modPlayer.judgmentProgress >= LotMPlayer.JUDGMENT_RITUAL_TARGET;
                string killColor = killReady ? "00FF00" : "FF0000";

                tooltips.Add(new TooltipLine(Mod, "RitualKill",
                    $"[c/{killColor}:仪式进度: 审判强敌 {modPlayer.judgmentProgress}/{LotMPlayer.JUDGMENT_RITUAL_TARGET}]"));

                // 检查圣物
                bool hasCross = Main.LocalPlayer.HasItem(ModContent.ItemType<UnshadowedCross>()); // 假设 UnshadowedCross 类已存在
                string crossColor = hasCross ? "00FF00" : "FF0000";

                tooltips.Add(new TooltipLine(Mod, "RitualItem",
                    $"[c/{crossColor}:仪式物品: 需持有【无暗十字】]"));
            }
        }

        // 3. 使用条件检查
        public override bool CanUseItem(Player player)
        {
            // 基类先检查序列
            if (!base.CanUseItem(player)) return false;

            var modPlayer = player.GetModPlayer<LotMPlayer>();

            if (modPlayer.baseSunSequence == 5)
            {
                // 1. 检查圣物
                if (!player.HasItem(ModContent.ItemType<UnshadowedCross>()))
                {
                    if (player.whoAmI == Main.myPlayer)
                        Main.NewText("[晋升失败] 缺少关键圣物：你的背包里没有【无暗十字】。", 255, 50, 50);
                    return false;
                }

                // 2. 检查仪式：审判进度
                if (modPlayer.judgmentProgress < LotMPlayer.JUDGMENT_RITUAL_TARGET)
                {
                    if (player.whoAmI == Main.myPlayer)
                    {
                        Main.NewText($"[晋升失败] 仪式未完成！你需要审判更多强敌 ({modPlayer.judgmentProgress}/{LotMPlayer.JUDGMENT_RITUAL_TARGET})。", 255, 50, 50);
                        Main.NewText("提示：去攻击 Boss 或 血量>2000 的怪物。", 200, 200, 200);
                    }
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
            modPlayer.baseSunSequence = 4;

            // 音效
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29, player.position);

            Main.NewText("情感被剥离又回归，你的身体化作了纯粹的光...", 255, 215, 0); // 金色
            Main.NewText("晋升半神！序列4：无暗者。", 255, 69, 0); // 半神红

            // 半神晋升特效
            for (int i = 0; i < 200; i++)
            {
                Vector2 speed = Main.rand.NextVector2Circular(15, 15);
                Dust.NewDustPerfect(player.Center, DustID.SolarFlare, speed, 0, default, 3f).noGravity = true;
            }

            return true;
        }

        // 5. 双配方支持
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<SunCard>(), // 核心：太阳牌
                (ItemID.BottledWater, 1),
                (ItemID.SolarTablet, 5),   // 太阳石板 (太阳)
                (ItemID.Ruby, 5),          // 红宝石 (热情)
                (ItemID.Topaz, 5),         // 黄宝石 (光辉)
                (ItemID.LavaBucket, 1),    // 岩浆桶 (净化)
                (ItemID.SoulofSight, 5)    // 视域之魂 (无暗)
            );
        }
    }
}