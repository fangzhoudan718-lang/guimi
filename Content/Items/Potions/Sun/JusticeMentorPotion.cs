using System.Collections.Generic;
using Microsoft.Xna.Framework; // 【核心修复】必须添加这个引用才能使用 Vector2
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories;

namespace zhashi.Content.Items.Potions.Sun
{
    public class JusticeMentorPotion : LotMItem
    {
        public override string Pathway => "Sun";
        public override int RequiredSequence => 4;

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 36;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 30;
            Item.consumable = true;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(platinum: 1);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            var modPlayer = Main.LocalPlayer.GetModPlayer<LotMPlayer>();

            if (modPlayer.baseSunSequence == 4)
            {
                bool killReady = modPlayer.judgmentProgress >= LotMPlayer.JUDGMENT_RITUAL_TARGET;
                string killColor = killReady ? "00FF00" : "FF0000";

                tooltips.Add(new TooltipLine(Mod, "RitualKill",
                    $"[c/{killColor}:仪式进度: 审判强敌 {modPlayer.judgmentProgress}/{LotMPlayer.JUDGMENT_RITUAL_TARGET}]"));

                bool isDay = Main.dayTime;
                bool hasEmblem = Main.LocalPlayer.HasItem(ItemID.AvengerEmblem);
                bool isClean = !Main.LocalPlayer.HasBuff(BuffID.Bleeding) && !Main.LocalPlayer.HasBuff(BuffID.Poisoned);

                string condColor = (isDay && hasEmblem && isClean) ? "00FF00" : "FF0000";
                string dayText = isDay ? "白天" : "需白天";
                string itemText = hasEmblem ? "徽章" : "需复仇者徽章";
                string cleanText = isClean ? "纯净" : "有污秽Debuff";

                tooltips.Add(new TooltipLine(Mod, "RitualEnv",
                    $"[c/{condColor}:仪式条件: {dayText} + {itemText} + {cleanText}]"));
            }
        }

        public override bool CanUseItem(Player player)
        {
            if (!base.CanUseItem(player)) return false;

            var modPlayer = player.GetModPlayer<LotMPlayer>();

            if (modPlayer.baseSunSequence == 4)
            {
                if (modPlayer.judgmentProgress < LotMPlayer.JUDGMENT_RITUAL_TARGET)
                {
                    if (player.whoAmI == Main.myPlayer)
                        Main.NewText($"仪式未完成：你需要审判更多的罪恶 ({modPlayer.judgmentProgress}/{LotMPlayer.JUDGMENT_RITUAL_TARGET})。", 255, 50, 50);
                    return false;
                }

                bool isDay = Main.dayTime;
                bool hasEmblem = player.HasItem(ItemID.AvengerEmblem);
                bool isClean = !player.HasBuff(BuffID.Bleeding) && !player.HasBuff(BuffID.Poisoned);

                if (!isDay || !hasEmblem || !isClean)
                {
                    if (player.whoAmI == Main.myPlayer)
                        Main.NewText("仪式条件未满足：需在白天的纯净状态下，手持徽章见证正义。", 255, 50, 50);
                    return false;
                }
            }
            return true;
        }

        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            modPlayer.baseSunSequence = 3;

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29, player.position);

            Main.NewText("你确立了自己的秩序，与世界签订了契约...", 255, 215, 0);
            Main.NewText("晋升圣者！序列3：正义导师。", 255, 215, 0);

            // 【此处需要 Vector2，现在有了 using Microsoft.Xna.Framework 就不会报错了】
            for (int i = 0; i < 100; i++)
            {
                Vector2 speed = new Vector2(0, -10f).RotatedByRandom(0.5f);
                Dust d = Dust.NewDustPerfect(player.Center, DustID.GoldFlame, speed, 0, default, 4f);
                d.noGravity = true;
            }

            return true;
        }

        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<SunCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.FragmentSolar, 15),
                (ItemID.Ectoplasm, 10),
                (ItemID.LifeFruit, 5),
                (ItemID.SporeSac, 1),
                (ItemID.HallowedBar, 20)
            );
        }
    }
}