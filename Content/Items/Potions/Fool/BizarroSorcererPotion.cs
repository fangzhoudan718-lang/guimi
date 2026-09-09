using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using zhashi.Content.Items.Accessories; // 引用愚者牌

namespace zhashi.Content.Items.Potions.Fool
{
    public class BizarroSorcererPotion : LotMItem
    {
        // 设定途径和前置序列 (需要序列5 秘偶大师)
        public override string Pathway => "Fool";
        public override int RequiredSequence => 5;

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
            Item.rare = ItemRarityID.Yellow; // 半神级
            Item.value = Item.sellPrice(platinum: 1);
        }

        // 【核心检查】能否使用 (仪式判定)
        public override bool CanUseItem(Player player)
        {
            // 1. 先检查序列是否达标
            if (!base.CanUseItem(player)) return false;

            // 仪式判定：
            // 1. 谋杀半神：必须已经击败世纪之花 (Plantera)
            // 2. 观众：周围必须有至少 10 个 NPC (城镇NPC或敌人均可)

            bool bossDowned = NPC.downedPlantBoss;

            int audienceCount = 0;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.Distance(player.Center) < 2000f) // 屏幕范围内
                {
                    audienceCount++;
                }
            }

            // 自己不算，至少要有10个"观众"
            if (!bossDowned || audienceCount < 10)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    Main.NewText($"仪式未完成：需击败世纪之花 ({bossDowned}) 且周围有 10 位观众 ({audienceCount})", 255, 50, 50);
                }
                return false;
            }

            return true;
        }

        // 【UI提示】显示仪式进度
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            bool bossDowned = NPC.downedPlantBoss;
            int audienceCount = 0;
            foreach (NPC npc in Main.ActiveNPCs)
                if (npc.Distance(Main.LocalPlayer.Center) < 2000f) audienceCount++;

            string c1 = bossDowned ? "00FF00" : "FF0000";
            // 【修复】这里之前写的是 >=3，已修正为 >=10 以匹配判定逻辑
            string c2 = audienceCount >= 10 ? "00FF00" : "FF0000";

            tooltips.Add(new TooltipLine(Mod, "Ritual1", $"[c/{c1}:仪式条件1：谋杀半神 (击败世纪之花)]"));
            tooltips.Add(new TooltipLine(Mod, "Ritual2", $"[c/{c2}:仪式条件2：观众在场 (周围生物>10) ({audienceCount})]"));
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();
                modPlayer.baseFoolSequence = 4; // 晋升序列4

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, player.position);
                Main.NewText("你分裂了，你重组了，你成为了诡异的主宰...", 128, 0, 128);
                Main.NewText("晋升成功！序列4：诡法师！(半神)", 255, 215, 0);
            }
            return true;
        }

        public override void AddRecipes()
        {
            // 使用智能配方生成器
            CreateDualRecipe(
                ModContent.ItemType<FoolCard>(),

                (ItemID.BottledWater, 1),
                (ItemID.SoulofMight, 10),   // 诡术邪怪主眼 (力量之魂)
                (ItemID.Ectoplasm, 10),     // 灵界掠夺者 (灵气)
                (ItemID.RichMahogany, 5),   // 红毛桦
                (ItemID.Vine, 1),           // 葡萄藤
                (ItemID.Silk, 5)            // 橡皮面具 (丝绸)
            );
        }
    }
}