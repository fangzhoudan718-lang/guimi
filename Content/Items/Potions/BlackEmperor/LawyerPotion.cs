using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories;
using zhashi.Content.Pathways.BlackEmperor;

namespace zhashi.Content.Items.Potions.BlackEmperor
{
    /// <summary>序列九 · 律师。黑皇帝途径的起点，解锁契约系统。</summary>
    public class LawyerPotion : LotMItem
    {
        public override string Pathway => "BlackEmperor";
        public override int RequiredSequence => 10;
        // 本模组的魔药瓶按「同序列统一瓶型」：直接引用愚者途径序列9的瓶型资源，
        // 不复制或移动PNG，避免重复资源，也避免路径迁移造成失效。
        public override string Texture => "zhashi/Content/Items/Potions/Fool/SeerPotion";

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
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.buyPrice(silver: 20);
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer) return true;
            LotMPlayer lotm = player.GetModPlayer<LotMPlayer>();
            if (lotm.baseBlackEmperorSequence != 10) return false;

            lotm.baseBlackEmperorSequence = 9;
            lotm.currentBlackEmperorSequence = 9;
            if (Main.netMode == NetmodeID.MultiplayerClient) lotm.SyncPlayer(-1, -1, false);
            PromotionPulse.Raise(player, PromotionPulse.BlackEmperor);

            SoundEngine.PlaySound(SoundID.Item104, player.Center);
            Main.NewText("黏稠的黑液滑过喉咙，空气都染上了颜色——晋升成功：序列9 律师！", 210, 170, 120);
            Main.NewText("能力：契约 [%key%]。给自己立一条律令，守住有奖励，违约会反噬。", 190, 165, 135);
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);
            tooltips.Add(new TooltipLine(Mod, "BlackEmperorIntro",
                "[c/D2AA78:黑皇帝途径 · 序列9 律师]\n" +
                "拥有出色的口才与思辨，能扭曲或引导目标的思维；\n" +
                "擅长发现规则的漏洞，靠秩序打击对手。\n" +
                "[c/AAAAAA:主材料：书、羽毛、木材、凝胶（游戏内等价物）]"));
        }

        public override void AddRecipes()
        {
            // 原著主材料「智慧果 / 迷宫鹦鹉的舌头」用游戏内等价物替代，不新增材料。
            CreateDualRecipe(
                ModContent.ItemType<BlackEmperorCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.Book, 1),
                (ItemID.Feather, 10),
                (ItemID.Wood, 20),
                (ItemID.Gel, 10));
        }
    }
}

