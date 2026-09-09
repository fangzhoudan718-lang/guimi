using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用太阳牌

namespace zhashi.Content.Items.Potions.Sun
{
    public class PriestPotion : LotMItem
    {
        // 1. 定义途径和前置序列
        public override string Pathway => "Sun";
        public override int RequiredSequence => 6; // 需要序列6 (公证人)

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
            Item.rare = ItemRarityID.LightRed; // 序列5 浅红
            Item.value = Item.sellPrice(gold: 10);
        }

        // 2. 显示仪式进度 (净化不死生物)
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            var modPlayer = Main.LocalPlayer.GetModPlayer<LotMPlayer>();

            // 只有序列6才显示进度
            if (modPlayer.baseSunSequence == 6)
            {
                // 计算进度
                string statusColor = (modPlayer.purificationProgress >= LotMPlayer.PURIFICATION_RITUAL_TARGET) ? "00FF00" : "FF0000";

                tooltips.Add(new TooltipLine(Mod, "RitualDesc", "晋升仪式: 净化大量不死生物 (僵尸/骷髅/亡灵)。"));
                tooltips.Add(new TooltipLine(Mod, "RitualProgress",
                    $"[c/{statusColor}:净化进度: {modPlayer.purificationProgress} / {LotMPlayer.PURIFICATION_RITUAL_TARGET}]"));
            }
        }

        // 3. 使用条件检查 (基类已含序列检查，这里主要做仪式拦截)
        public override bool CanUseItem(Player player)
        {
            if (!base.CanUseItem(player)) return false;

            var modPlayer = player.GetModPlayer<LotMPlayer>();

            if (modPlayer.baseSunSequence == 6)
            {
                if (modPlayer.purificationProgress < LotMPlayer.PURIFICATION_RITUAL_TARGET)
                {
                    if (player.whoAmI == Main.myPlayer)
                    {
                        Main.NewText($"仪式未完成！你需要净化更多的不死生物 ({modPlayer.purificationProgress}/{LotMPlayer.PURIFICATION_RITUAL_TARGET})。", 255, 50, 50);
                        Main.NewText("提示：去击杀僵尸、骷髅或亡灵类怪物。", 200, 200, 200);
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
            modPlayer.baseSunSequence = 5;

            // 音效
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29, player.position);

            Main.NewText("你在无尽的战斗中领悟了光的真谛...", 255, 215, 0); // 太阳金
            Main.NewText("晋升成功！序列5：光之祭司。", 255, 215, 0);

            // 金色粒子特效
            for (int i = 0; i < 100; i++)
            {
                Dust d = Dust.NewDustPerfect(player.Center, DustID.GoldFlame, Main.rand.NextVector2Circular(10, 10), 0, default, 3f);
                d.noGravity = true;
            }

            return true;
        }

        // 5. 双配方支持
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<SunCard>(), // 核心：太阳牌
                (ItemID.BottledWater, 1),
                (ItemID.SunStone, 1),      // 太阳石 (核心)
                (ItemID.Diamond, 5),       // 钻石 (纯净)
                (ItemID.HallowedBar, 10),  // 神圣锭
                (ItemID.SoulofLight, 10),  // 光之魂
                (ItemID.PixieDust, 20)     // 妖精尘
            );
        }
    }
}