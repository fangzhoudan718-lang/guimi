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
    /// 序列二 · 熵之公爵。手里握着的是「混乱」的一部分，也是「利用」的一部分：
    /// 熵让交战越久的一切越随机、越无法把控，直到某个东西陷入寂灭；
    /// 利用则被强化到能把重力这条规矩直接拧松。
    /// 这一瓶要先把一个至少十人的城镇握在手里，再从内部推它一把。
    /// </summary>
    public class EntropyDukePotion : LotMItem
    {
        public override string Pathway => "BlackEmperor";
        public override int RequiredSequence => 3;
        // 同序列统一瓶型：引用愚者途径序列2的瓶型资源。
        public override string Texture => "zhashi/Content/Items/Potions/Fool/MiracleInvokerPotion";

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
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.buyPrice(gold: 60);
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
            if (be.EntropyRitualComplete) return true;

            if (player.whoAmI == Main.myPlayer && !Main.dedServ)
                Main.NewText("晋升仪式尚未完成：先把十位居民握在手里，再在城里引爆满层的熵。", 255, 70, 70);
            return false;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer) return true;
            LotMPlayer lotm = player.GetModPlayer<LotMPlayer>();
            if (lotm.baseBlackEmperorSequence != 3) return false;

            lotm.baseBlackEmperorSequence = 2;
            lotm.currentBlackEmperorSequence = 2;
            if (Main.netMode == NetmodeID.MultiplayerClient) lotm.SyncPlayer(-1, -1, false);
            PromotionPulse.Raise(player, PromotionPulse.BlackEmperor);

            SoundEngine.PlaySound(SoundID.Item104, player.Center);
            Main.NewText("透明液体里那些黑色文字与符号慢慢缠成一顶冠冕，冠冕底下什么都没有——晋升成功：序列2 熵之公爵！", 170, 140, 220);
            Main.NewText($"能力：兑现 [{KeyName(LotMKeybinds.BlackEmperor_Entropy, ";")}]；被动「熵」与「极致利用」生效。", 190, 160, 230);
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);
            BlackEmperorPlayer be = Main.LocalPlayer.GetModPlayer<BlackEmperorPlayer>();
            string status = be.EntropyRitualComplete ? "[c/00FF00:已完成]" : "[c/FF5555:未完成]";
            tooltips.Add(new TooltipLine(Mod, "BlackEmperorRitual",
                "晋升仪式：在乱世之夜，站在城镇里引爆一次满层的熵。\n" +
                "熵要叠到 " + BlackEmperorPlayer.EntropyMaxStacks + " 层（交战中每三秒一层，只涨不掉）；\n" +
                "站在城镇里；当晚是血月、入侵、霜月、南瓜月或日食。三项齐了按兑现键，一次即成。\n" +
                status));
            tooltips.Add(new TooltipLine(Mod, "BlackEmperorIntro",
                "[c/8B5FB0:黑皇帝途径 · 序列2 熵之公爵]\n" +
                "「熵」让交战拖得越久，一切越随机、越无法把控，直到某个东西陷入寂灭；\n" +
                "「兑现」把攒下的乱一次性掷出去，换来伤害与一个谁也说不准的结果，\n" +
                "代价是接下来五秒里灵性回复只有一半。"));
        }

        public override void AddRecipes()
        {
            // 原著主材料「所罗门的指节」用游戏内等价物替代。
            CreateDualRecipe(
                ModContent.ItemType<BlackEmperorCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.LunarBar, 15),
                (ItemID.FragmentSolar, 15),
                (ItemID.FragmentVortex, 15),
                (ItemID.FragmentNebula, 15),
                (ItemID.FragmentStardust, 15),
                (ItemID.Ectoplasm, 20),
                (ItemID.Diamond, 1));
        }
    }
}
