using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories;
using zhashi.Content.Items.Materials;

namespace zhashi.Content.Items.Potions.Moon
{
    public class SummoningMasterPotion : LotMItem
    {
        public override string Pathway => "Moon";
        public override int RequiredSequence => 4;

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 34;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 30;
            Item.consumable = true;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: 50);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);
            tooltips.Add(new TooltipLine(Mod, "RitualHint", "[c/B0C4DE:提示: 需解析灵界生物的真名(真名拓本)方可调配此药剂]"));
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var modPlayer = player.GetModPlayer<LotMPlayer>();

                modPlayer.baseMoonSequence = 3;

                CombatText.NewText(player.getRect(), Color.MediumPurple, "晋升：召唤大师", true);
                Main.NewText("你听到了灵界万千生物的呼唤，你是它们的主宰...", 200, 100, 255);

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item119, player.position);

                for (int i = 0; i < 60; i++)
                {
                    Vector2 speed = Main.rand.NextVector2Circular(8, 8);
                    // 【修复点】使用 DungeonSpirit (地牢幽魂) 粒子
                    Dust d = Dust.NewDustPerfect(player.Center, DustID.DungeonSpirit, speed, 0, default, 1.5f);
                    d.noGravity = true;

                    if (i % 3 == 0)
                    {
                        // 【修复点】这里重复使用 DungeonSpirit 或其他有效ID，避免报错
                        Dust d2 = Dust.NewDustPerfect(player.Center, DustID.DungeonSpirit, speed * 1.2f, 0, default, 1.5f);
                        d2.noGravity = true;
                    }
                }
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<MoonCard>(),
                (ItemID.BottledWater, 1),
                (ModContent.ItemType<TrueNameInscription>(), 1),
                (ItemID.Ectoplasm, 10),
                (ItemID.SpectreBar, 5)
            );
        }
    }
}