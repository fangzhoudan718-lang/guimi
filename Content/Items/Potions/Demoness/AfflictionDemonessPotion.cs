using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic; // 必须引用这个，用于ModifyTooltips的List
using zhashi.Content.Items.Accessories; // 引用魔女牌

namespace zhashi.Content.Items.Potions.Demoness
{
    public class AfflictionDemonessPotion : LotMItem
    {
        public override string Pathway => "Demoness";
        public override int RequiredSequence => 6; // 必须先成为序列6欢愉魔女

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("痛苦魔女魔药");
            // Tooltip.SetDefault("序列5：痛苦魔女\n" +
            //                  "能力：散播瘟疫与诅咒，魅惑众生，发丝化作利刃。\n" +
            //                  "仪式：在活火块(Living Fire)上坚持15分钟不离开。");
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 30;
            Item.consumable = true;
            Item.rare = ItemRarityID.Pink; // 序列5 粉色品质
            Item.value = Item.sellPrice(gold: 10);
        }

        // --- 核心检查：能否使用 ---
        public override bool CanUseItem(Player player)
        {
            // 1. 基类检查序列等级 (是否是序列6)
            if (!base.CanUseItem(player)) return false;

            LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

            // 2. 仪式检查：必须站够时间 (3600帧 = 1分钟，如果是15分钟则是54000帧)
            // 这里为了跟您的代码保持一致，我先用 3600 (1分钟)。
            // 如果要还原原著，建议改回 54000。
            int targetTime = 3600;

            if (modPlayer.afflictionRitualTimer < targetTime)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    int mins = modPlayer.afflictionRitualTimer / 3600; // 这里的显示可能一直是0，因为还没到1分钟
                    // 建议改为显示百分比或者秒数
                    Main.NewText($"仪式未完成！你需要忍受烈火灼烧。（当前进度：{modPlayer.afflictionRitualTimer}/{targetTime}）", 255, 50, 50);
                }
                return false;
            }

            return true;
        }

        // --- UI提示：显示仪式进度 ---
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            Player player = Main.LocalPlayer;
            LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

            int timer = modPlayer.afflictionRitualTimer;
            int targetTime = 3600; // 目标时间 (1分钟)

            bool complete = timer >= targetTime;
            if (timer > targetTime) timer = targetTime;

            string color = complete ? "00FF00" : "FF0000"; // 完成变绿，未完成变红
            string status = complete ? " (已完成)" : "";

            // 将帧数转换为秒数显示，更直观
            int seconds = timer / 60;
            int targetSeconds = targetTime / 60;

            tooltips.Add(new TooltipLine(Mod, "Ritual", $"[c/{color}:仪式要求：忍受烈火 ({seconds}/{targetSeconds}秒){status}]"));
        }

        // --- 晋升逻辑 ---
        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

                modPlayer.baseDemonessSequence = 5;
                Main.NewText("痛苦不仅是折磨，更是武器...", 200, 0, 200);
                Main.NewText("晋升成功！序列5：痛苦魔女！", 255, 0, 255);

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item119, player.Center);
            }
            return true;
        }

        // --- 配方注册 (修正语法) ---
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<DemonessCard>(), // 核心：魔女牌

                (ItemID.BottledWater, 1), // 水瓶
                (ItemID.Stinger, 10),     // 毒刺
                (ItemID.Vertebrae, 50),   // 椎骨
                (ItemID.Vine, 10)         // 藤蔓
            );
        }
    }
}