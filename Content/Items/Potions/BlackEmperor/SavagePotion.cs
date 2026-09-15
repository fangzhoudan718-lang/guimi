using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories;
using zhashi.Content.Pathways.BlackEmperor;

namespace zhashi.Content.Items.Potions.BlackEmperor
{
    /// <summary>序列八 · 野蛮人。可怕的力量与体魄，法律管不到的地方就用力量解决。</summary>
    public class SavagePotion : LotMItem
    {
        public override string Pathway => "BlackEmperor";
        public override int RequiredSequence => 9;
        // 同序列统一瓶型：引用愚者途径序列8的瓶型资源。
        public override string Texture => "zhashi/Content/Items/Potions/Fool/ClownPotion";

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
            Item.rare = ItemRarityID.Green;
            Item.value = Item.buyPrice(gold: 1);
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer) return true;
            LotMPlayer lotm = player.GetModPlayer<LotMPlayer>();
            if (lotm.baseBlackEmperorSequence != 9) return false;

            lotm.baseBlackEmperorSequence = 8;
            lotm.currentBlackEmperorSequence = 8;
            if (Main.netMode == NetmodeID.MultiplayerClient) lotm.SyncPlayer(-1, -1, false);
            PromotionPulse.Raise(player, PromotionPulse.BlackEmperor);

            SoundEngine.PlaySound(SoundID.Roar, player.Center);
            Main.NewText("半透明的黑液里浮沉着微缩的器官，喉咙里泛起铁锈味——晋升成功：序列8 野蛮人！", 200, 130, 90);
            Main.NewText("能力：无法之地 [%lawless%] / 硬闯 [%unstoppable%]；被动体魄与「以力破法」。", 200, 150, 110);
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);
            tooltips.Add(new TooltipLine(Mod, "BlackEmperorIntro",
                "[c/C08A58:黑皇帝途径 · 序列8 野蛮人]\n" +
                "可怕的力量、极强的体魄，以及出类拔萃的精神抵抗。\n" +
                "不能依靠法律的时候，就用力量解决。"));
        }

        public override void AddRecipes()
        {
            // 原著主材料「狂化草 / 大地犀牛的实心独角结晶」用游戏内等价物替代。
            // 两种世界各写一条，避免铁/铅互斥导致某一边做不出来。
            CreateDualRecipe(
                ModContent.ItemType<BlackEmperorCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.IronBar, 15),
                (ItemID.Gel, 50),
                (ItemID.JungleSpores, 5),
                (ItemID.Bone, 10));

            CreateDualRecipe(
                ModContent.ItemType<BlackEmperorCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.LeadBar, 15),
                (ItemID.Gel, 50),
                (ItemID.JungleSpores, 5),
                (ItemID.Bone, 10));
        }
    }
}
