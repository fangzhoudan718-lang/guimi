using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content;
using zhashi.Content.Items.Accessories; // 引用力量牌

namespace zhashi.Content.Items.Potions
{
    // 1. 继承 LotMItem
    public class DemonHunterPotion : LotMItem
    {
        // 2. 设定途径和前置序列
        public override string Pathway => "Giant";
        public override int RequiredSequence => 5; // 需要是序列5守护者才能喝

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.UseSound = SoundID.Item3;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.maxStack = 99;
            Item.consumable = true;
            Item.rare = ItemRarityID.Yellow; // 黄色稀有度
            Item.value = Item.buyPrice(gold: 50);
        }

        // 3. 重写 ModifyTooltips：显示仪式进度
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // 先调用基类的逻辑 (显示红色的"需要序列5")
            base.ModifyTooltips(tooltips);

            var modPlayer = Main.LocalPlayer.GetModPlayer<LotMPlayer>();

            // 只有当玩家是序列5时，才显示仪式进度，避免干扰其他玩家
            if (modPlayer.baseSequence == 5)
            {
                string statusColor = (modPlayer.demonHunterRitualProgress >= LotMPlayer.DEMON_HUNTER_RITUAL_TARGET) ? "00FF00" : "FF0000";
                string progressText = $"[c/{statusColor}:仪式进度: {modPlayer.demonHunterRitualProgress} / {LotMPlayer.DEMON_HUNTER_RITUAL_TARGET}]";

                tooltips.Add(new TooltipLine(Mod, "RitualDesc", "晋升仪式: 独自猎杀 10 只红魔鬼(Red Devil)。"));
                tooltips.Add(new TooltipLine(Mod, "RitualProgress", progressText));
            }
        }

        // 4. 重写 CanUseItem：增加仪式未完成的拦截
        public override bool CanUseItem(Player player)
        {
            // 先让基类检查是否是序列5 (代码复用！)
            if (!base.CanUseItem(player)) return false;

            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 额外检查仪式进度
            if (modPlayer.baseSequence == 5)
            {
                if (modPlayer.demonHunterRitualProgress < LotMPlayer.DEMON_HUNTER_RITUAL_TARGET)
                {
                    if (player.whoAmI == Main.myPlayer)
                    {
                        Main.NewText($"你还未证明你是恶魔的克星... (进度: {modPlayer.demonHunterRitualProgress}/{LotMPlayer.DEMON_HUNTER_RITUAL_TARGET})", 255, 50, 50);
                    }
                    return false;
                }
            }
            return true;
        }

        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            if (modPlayer.baseSequence == 5)
            {
                modPlayer.baseSequence = 4;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position);

                Main.NewText("你的双眼看穿了万物的弱点，你已不再是凡人...", 255, 0, 255);
                Main.NewText("晋升成功：序列4 猎魔者 (半神)！", 255, 0, 255);
                Main.NewText("获得能力：【猎魔之眼】(永久显示敌人，暴击大幅提升)", 255, 255, 255);
                Main.NewText("获得能力：【弱点看破】(攻击削弱敌人防御)", 255, 255, 255);
                return true;
            }
            // 不需要再写 else if (baseSequence > 5) 了，因为 CanUseItem 已经拦住了
            return true;
        }

        // =========================================================
        // 【重点修改】使用智能配方生成器
        // =========================================================
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<StrengthCard>(),

                (ItemID.BottledWater, 1),
                (ItemID.BeetleHusk, 5),  // 甲虫壳
                (ItemID.Ectoplasm, 10),  // 灵气
                (ItemID.SoulofNight, 15) // 暗影之魂
            );
        }
    }
}