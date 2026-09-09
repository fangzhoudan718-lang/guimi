using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Graphics.Effects;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using zhashi.Content.Items.Accessories; // 引用魔女牌

namespace zhashi.Content.Items.Potions.Demoness
{
    public class ApocalypseDemonessPotion : LotMItem
    {
        public override string Pathway => "Demoness";
        public override int RequiredSequence => 2; // 必须是序列2

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("末日魔女魔药");
            // Tooltip.SetDefault("序列1：末日魔女 (从神)\n" +
            //                  "权柄：终结一切，引发真正的末日。\n" +
            //                  "仪式：在世界即将毁灭的时刻 (月球领主降临) 服用，并吸收绝望。");
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.consumable = true;
            Item.rare = ItemRarityID.Purple; // 紫色 (神级)
            Item.value = Item.sellPrice(platinum: 5);
        }

        public override bool CanUseItem(Player player)
        {
            if (!base.CanUseItem(player)) return false;

            // --- 仪式判定 ---

            // 条件 1: 末日危机 (月球领主必须活着)
            bool apocalypseActive = NPC.AnyNPCs(NPCID.MoonLordCore);

            // 条件 2: 绝望情绪 (玩家生命值必须低于 30%，处于濒死边缘)
            bool despairActive = player.statLife < (player.statLifeMax2 * 0.3f);

            if (!apocalypseActive || !despairActive)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    Main.NewText("仪式未完成！你没有感受到真正的末日与绝望...", 255, 50, 50);
                    if (!apocalypseActive)
                        Main.NewText("提示：必须在【月球领主】降临时服用。", 200, 200, 200);
                    if (!despairActive)
                        Main.NewText("提示：必须在濒死状态 (血量 < 30%) 下服用，感受绝望。", 200, 200, 200);
                }
                return false;
            }

            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);
            Player player = Main.LocalPlayer;

            bool cond1 = NPC.AnyNPCs(NPCID.MoonLordCore);
            bool cond2 = player.statLife < (player.statLifeMax2 * 0.3f);

            string c1 = cond1 ? "00FF00" : "FF0000";
            string c2 = cond2 ? "00FF00" : "FF0000";

            tooltips.Add(new TooltipLine(Mod, "Ritual1", $"[c/{c1}:仪式条件1：月球领主降临 (末日)]"));
            tooltips.Add(new TooltipLine(Mod, "Ritual2", $"[c/{c2}:仪式条件2：生命垂危 < 30% (绝望)]"));
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();
                modPlayer.baseDemonessSequence = 1;
                modPlayer.currentDemonessSequence = 1;

                // 晋升特效：震慑全场
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position);
                Main.NewText("你已化身为末日，即使是外神也将为你颤抖！", 178, 34, 34); // 深红

                // 瞬间回满血 (晋升奖励)
                player.statLife = player.statLifeMax2;
                player.HealEffect(player.statLifeMax2);

                // 清除所有 Debuff
                for (int i = 0; i < Player.MaxBuffs; i++) player.DelBuff(i);
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<DemonessCard>(),
                // 主材料：月亮领主掉落物 (象征位格)
                (ItemID.LunarOre, 100),
                (ItemID.FragmentSolar, 20),
                (ItemID.FragmentVortex, 20),
                (ItemID.FragmentNebula, 20),
                (ItemID.FragmentStardust, 20),
               // 辅助材料：深渊/星界 (星云砖/夜明锭)
                (ItemID.LunarBar, 20), // 修正为 LunarBar
                (ItemID.BottledWater, 1)
            );
        }
    }
}