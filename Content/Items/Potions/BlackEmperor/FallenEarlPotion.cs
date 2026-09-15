using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories;
using zhashi.Content.Pathways.BlackEmperor;

namespace zhashi.Content.Items.Potions.BlackEmperor
{
    /// <summary>
    /// 序列四 · 堕落伯爵。半神这一步不靠蛮力，靠的是找出规则里的漏洞：
    /// 把负面状态「赠予」出去，把一件事「放大」，把有利的状态「利用」到极致。
    /// 这一瓶必须先在城里拉拢七个人，并推行过一项政策，才喝得下去。
    /// </summary>
    public class FallenEarlPotion : LotMItem
    {
        public override string Pathway => "BlackEmperor";
        public override int RequiredSequence => 5;
        // 同序列统一瓶型：引用愚者途径序列4的瓶型资源。
        public override string Texture => "zhashi/Content/Items/Potions/Fool/BizarroSorcererPotion";

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
            Item.rare = ItemRarityID.Yellow;
            Item.value = Item.buyPrice(gold: 20);
        }

        private static string KeyName(ModKeybind keybind, string fallback)
        {
            if (keybind == null) return fallback;
            var keys = keybind.GetAssignedKeys(InputMode.Keyboard);
            return keys.Count > 0 ? string.Join("/", keys) : fallback;
        }

        public override bool CanUseItem(Player player)
        {
            if (!base.CanUseItem(player)) return false;
            BlackEmperorPlayer be = player.GetModPlayer<BlackEmperorPlayer>();
            // 用会同步的计数而不是名单：名单留在服务器上，客户端拿不到。
            if (be.CorruptedTownCount >= BlackEmperorPlayer.CorruptedTownTarget && be.policyImplemented) return true;

            if (player.whoAmI == Main.myPlayer && !Main.dedServ)
                Main.NewText("晋升仪式尚未完成：先在城里拉拢七个同僚，再推行一项政策。", 255, 70, 70);
            return false;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer) return true;
            LotMPlayer lotm = player.GetModPlayer<LotMPlayer>();
            if (lotm.baseBlackEmperorSequence != 5) return false;

            lotm.baseBlackEmperorSequence = 4;
            lotm.currentBlackEmperorSequence = 4;
            if (Main.netMode == NetmodeID.MultiplayerClient) lotm.SyncPlayer(-1, -1, false);
            PromotionPulse.Raise(player, PromotionPulse.BlackEmperor);

            SoundEngine.PlaySound(SoundID.Item104, player.Center);
            Main.NewText("黑色液体核心处凝出半实质的眼珠，腐烂的脓水在杯壁上慢慢往下淌——晋升成功：序列4 堕落伯爵！", 150, 110, 190);
            Main.NewText($"能力：赠予 [{KeyName(LotMKeybinds.BlackEmperor_Gift, "'")}]、放大 [{KeyName(LotMKeybinds.BlackEmperor_Amplify, "=")}]；被动「利用」与规则领域同时生效。", 180, 150, 220);
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);
            BlackEmperorPlayer be = Main.LocalPlayer.GetModPlayer<BlackEmperorPlayer>();
            int gathered = be.CorruptedTownCount;
            string status = gathered >= BlackEmperorPlayer.CorruptedTownTarget && be.policyImplemented
                ? "[c/00FF00:已完成]"
                : $"[c/FF5555:未完成 · 已拉拢 {gathered}/{BlackEmperorPlayer.CorruptedTownTarget}" +
                  (be.policyImplemented ? " · 政策已推行]" : " · 政策未推行]");
            tooltips.Add(new TooltipLine(Mod, "BlackEmperorRitual",
                "晋升仪式：成为一个国家的中高层。\n" +
                "用钱拉拢七位不同的城镇居民，再在城里把一条律令维持到自然结束，让那项政策真正落地。\n" +
                status));
            tooltips.Add(new TooltipLine(Mod, "BlackEmperorIntro",
                "[c/8B5FB0:黑皇帝途径 · 序列4 堕落伯爵]\n" +
                "任何事情都有规则，而半神擅于找它们的漏洞。\n" +
                "「赠予」把负面状态直接送出去，「放大」把一次普通的行为变成处决或束缚，\n" +
                "「利用」则把有利的状态拖得更久——比如让「离开大地」变成滞空。"));
        }

        public override void AddRecipes()
        {
            // 原著主材料「林地巨妖的眼珠 / 猫脸鬼影的舌头」用游戏内等价物替代。
            CreateDualRecipe(
                ModContent.ItemType<BlackEmperorCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.HallowedBar, 15),
                (ItemID.Ectoplasm, 20),
                (ItemID.SoulofNight, 15),
                (ItemID.PlatinumCoin, 3));
        }
    }
}
