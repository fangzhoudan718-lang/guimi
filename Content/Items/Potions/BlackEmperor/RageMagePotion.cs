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
    /// 序列三 · 狂乱法师。一部分「阶层」权柄，一部分「混乱」权柄。
    /// 名义让下位者抬不起头，狂乱则让双方的状态一起乱掉——连施法者也不知道会变成什么。
    /// 这一瓶必须在三场仪式里活下来，才喝得下去。
    /// </summary>
    public class RageMagePotion : LotMItem
    {
        public override string Pathway => "BlackEmperor";
        public override int RequiredSequence => 4;
        // 同序列统一瓶型：引用愚者途径序列3的瓶型资源。
        public override string Texture => "zhashi/Content/Items/Potions/Fool/ScholarOfYorePotion";

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
            Item.rare = ItemRarityID.Lime;
            Item.value = Item.buyPrice(gold: 35);
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
            if (be.RageRitualComplete) return true;

            if (player.whoAmI == Main.myPlayer && !Main.dedServ)
                Main.NewText("晋升仪式尚未完成：在三场战斗里动过狂乱，并且活着走完它们。", 255, 70, 70);
            return false;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer) return true;
            LotMPlayer lotm = player.GetModPlayer<LotMPlayer>();
            if (lotm.baseBlackEmperorSequence != 4) return false;

            lotm.baseBlackEmperorSequence = 3;
            lotm.currentBlackEmperorSequence = 3;
            if (Main.netMode == NetmodeID.MultiplayerClient) lotm.SyncPlayer(-1, -1, false);
            PromotionPulse.Raise(player, PromotionPulse.BlackEmperor);

            SoundEngine.PlaySound(SoundID.Item104, player.Center);
            Main.NewText("被漆黑黏液分隔成三份的赤红岩浆在杯里翻涌，喝下去时连呼吸的节奏都乱了——晋升成功：序列3 狂乱法师！", 190, 120, 210);
            Main.NewText($"能力：狂乱 [{KeyName(LotMKeybinds.BlackEmperor_Rage, "-")}]；被动「名义」会随世界状态改变，扭曲也升级为无法停下。", 200, 150, 230);
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);
            BlackEmperorPlayer be = Main.LocalPlayer.GetModPlayer<BlackEmperorPlayer>();
            int done = be.RageRitualProgress;
            string status = done >= BlackEmperorPlayer.RageRitualTarget
                ? "[c/00FF00:已完成]"
                : $"[c/FF5555:未完成 · 已活过 {done}/{BlackEmperorPlayer.RageRitualTarget} 场]";
            tooltips.Add(new TooltipLine(Mod, "BlackEmperorRitual",
                "晋升仪式：挑三场足以牵动天使层面力量的战斗，在其中亲手扬起狂乱，\n" +
                "再一头雾水地活到最后。中途死过一次，那一场就作废。\n" +
                status));
            tooltips.Add(new TooltipLine(Mod, "BlackEmperorIntro",
                "[c/8B5FB0:黑皇帝途径 · 序列3 狂乱法师]\n" +
                "威严来自名义，来自阶位，也来自实力——你开口，灵体也要发抖。\n" +
                "「狂乱」扬起一阵谁也算不准的波动：你自己的状态往好处乱，\n" +
                "周围敌人的状态往坏处乱，具体变成什么样子，你自己也不知道。"));
        }

        public override void AddRecipes()
        {
            // 原著主材料「黑山君主的心脏 / 狂野人猿的尾巴」用游戏内等价物替代。
            CreateDualRecipe(
                ModContent.ItemType<BlackEmperorCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.FragmentVortex, 10),
                (ItemID.FragmentNebula, 10),
                (ItemID.MartianConduitPlating, 30),
                (ItemID.SoulofFlight, 20),
                (ItemID.Ectoplasm, 20));
        }
    }
}
