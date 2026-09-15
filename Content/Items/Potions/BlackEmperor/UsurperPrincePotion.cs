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
    /// 序列一 · 弑序亲王。这条路的尽头不是更强的拳头，而是「定义」：
    /// 重新定义什么是替身、什么是本体、什么行为叫贿赂、什么人是腐败者，
    /// 再借这份定义把原本的秩序扭曲成有利于自己的样子。
    /// 这一瓶要在三个昼夜里，用自己定下的规矩取代原本的规矩。
    /// </summary>
    public class UsurperPrincePotion : LotMItem
    {
        public override string Pathway => "BlackEmperor";
        public override int RequiredSequence => 2;
        // 同序列统一瓶型：引用愚者途径序列1的瓶型资源。
        public override string Texture => "zhashi/Content/Items/Potions/Fool/AttendantPotion";

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
            Item.rare = ItemRarityID.Red;
            Item.value = Item.buyPrice(gold: 100);
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
            if (be.UsurpRitualComplete) return true;

            if (player.whoAmI == Main.myPlayer && !Main.dedServ)
                Main.NewText("晋升仪式尚未完成：三个昼夜，空手在城里把一条律令走到自然结束。", 255, 70, 70);
            return false;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer) return true;
            LotMPlayer lotm = player.GetModPlayer<LotMPlayer>();
            if (lotm.baseBlackEmperorSequence != 2) return false;

            lotm.baseBlackEmperorSequence = 1;
            lotm.currentBlackEmperorSequence = 1;
            if (Main.netMode == NetmodeID.MultiplayerClient) lotm.SyncPlayer(-1, -1, false);
            // 序列一的那一下：开场即高潮的《弑序》
            PromotionPulse.Raise(player, PromotionPulse.BlackEmperor, 1);
            PromotionPulse.Raise(player, PromotionPulse.BlackEmperor);

            SoundEngine.PlaySound(SoundID.Item104, player.Center);
            Main.NewText("半透明的黑液里倒映出一个黄铜色的扭曲人影，它冲你点了头，然后站到了你的位置上——晋升成功：序列1 弑序亲王！", 210, 170, 120);
            Main.NewText($"能力：定义 (Shift + {KeyName(LotMKeybinds.BlackEmperor_Twist, "/")} 换词条)；扭曲连半神都能拨动。", 220, 180, 140);
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);
            BlackEmperorPlayer be = Main.LocalPlayer.GetModPlayer<BlackEmperorPlayer>();
            string status = be.UsurpRitualComplete ? "[c/00FF00:已完成]" : "[c/FF5555:未完成]";
            tooltips.Add(new TooltipLine(Mod, "BlackEmperorRitual",
                "晋升仪式：以自身的秩序取代原本的秩序。\n" +
                "在三个不同的昼夜，空手站在城镇里，把一条律令维持到自然结束；一天只记一次。\n" +
                status + $" [c/AAAAAA:{be.usurpRitualDays}/{BlackEmperorPlayer.UsurpRitualTarget}]"));
            tooltips.Add(new TooltipLine(Mod, "BlackEmperorIntro",
                "[c/8B5FB0:黑皇帝途径 · 序列1 弑序亲王]\n" +
                "「定义」：你可以说什么是替身、什么是贿赂、什么人算腐败者——\n" +
                "世界会照着你的说法走。同一时间只有一条定义生效。\n" +
                "选择「替身」会制造一具能自行行走的真实替身；致命伤会改判为它死亡。\n" +
                "黑皇帝的控制权柄对 Boss 同样有效，但持续时间与位移幅度会按位格折减。"));
        }

        public override void AddRecipes()
        {
            // 原著主材料「弑序亲王非凡特性」用游戏内等价物替代：这是最后一步，材料也最重。
            CreateDualRecipe(
                ModContent.ItemType<BlackEmperorCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.LunarBar, 30),
                (ItemID.CelestialSigil, 1),
                (ItemID.Compass, 1),
                (ItemID.FragmentSolar, 20),
                (ItemID.FragmentVortex, 20),
                (ItemID.FragmentNebula, 20),
                (ItemID.FragmentStardust, 20),
                (ItemID.PlatinumCoin, 10));
        }
    }
}
