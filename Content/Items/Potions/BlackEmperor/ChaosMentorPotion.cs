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
    /// 序列五 · 混乱导师。提升威严与体质，让周围的生灵不自觉放低身段；
    /// 「混乱」让攻击难以落到自己身上、让敌人一再选错，「扭曲」则连概念都能改写。
    /// 这一瓶必须先在天黑之后、于城市地底压住三个夜晚，才能喝下去。
    /// </summary>
    public class ChaosMentorPotion : LotMItem
    {
        public override string Pathway => "BlackEmperor";
        public override int RequiredSequence => 6;
        // 同序列统一瓶型：引用愚者途径序列5的瓶型资源。
        public override string Texture => "zhashi/Content/Items/Potions/Fool/MarionettistPotion";

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
            Item.rare = ItemRarityID.LightPurple;
            Item.value = Item.buyPrice(gold: 8);
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
            if (be.chaosRitualComplete) return true;

            if (player.whoAmI == Main.myPlayer && !Main.dedServ)
                Main.NewText("晋升仪式尚未完成：先让城市的地底连着三个夜晚都听你的。", 255, 70, 70);
            return false;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer) return true;
            LotMPlayer lotm = player.GetModPlayer<LotMPlayer>();
            if (lotm.baseBlackEmperorSequence != 6) return false;

            lotm.baseBlackEmperorSequence = 5;
            lotm.currentBlackEmperorSequence = 5;
            if (Main.netMode == NetmodeID.MultiplayerClient) lotm.SyncPlayer(-1, -1, false);
            PromotionPulse.Raise(player, PromotionPulse.BlackEmperor);

            SoundEngine.PlaySound(SoundID.Item104, player.Center);
            Main.NewText("自生漩涡的黑液时而扩散时而收缩，喝下去的一瞬，周围的一切都安静了——晋升成功：序列5 混乱导师！", 170, 150, 220);
            Main.NewText($"能力：混乱场 [{KeyName(LotMKeybinds.BlackEmperor_Chaos, "\\")}]；扭曲升级为「扭曲概念」，Shift + {KeyName(LotMKeybinds.BlackEmperor_Twist, "/")} 使用。", 190, 165, 230);
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);
            BlackEmperorPlayer be = Main.LocalPlayer.GetModPlayer<BlackEmperorPlayer>();
            string status = be.chaosRitualComplete ? "[c/00FF00:已完成]" : $"[c/FF5555:未完成 {be.chaosRitualNights}/{BlackEmperorPlayer.ChaosRitualNightTarget} 夜]";
            tooltips.Add(new TooltipLine(Mod, "BlackEmperorRitual",
                "晋升仪式：城镇里住着至少五个人时，连续三个夜晚在城市地底各自了结足够多的敌人。\n" +
                "夜里动了城里的人，连夜的账就作废。\n" +
                status));
            tooltips.Add(new TooltipLine(Mod, "BlackEmperorIntro",
                "[c/8A6AB0:黑皇帝途径 · 序列5 混乱导师]\n" +
                "威严与体质一同提升，周围的生灵不自觉放低身段。\n" +
                "「混乱场」把一块地里的距离与敌我全部拨乱；\n" +
                "「扭曲概念」让被绑住的目标替你分担伤害与诅咒。"));
        }

        public override void AddRecipes()
        {
            // 原著主材料「妖精火花 / 森林之子的脑袋」用游戏内等价物替代；
            // 金锭一档同时写白金，避免某一边世界做不出来。
            CreateDualRecipe(
                ModContent.ItemType<BlackEmperorCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.MeteoriteBar, 12),
                (ItemID.Hellstone, 20),
                (ItemID.PixieDust, 10),
                (ItemID.GoldBar, 5),
                (ItemID.Obsidian, 15));

            CreateDualRecipe(
                ModContent.ItemType<BlackEmperorCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.MeteoriteBar, 12),
                (ItemID.Hellstone, 20),
                (ItemID.PixieDust, 10),
                (ItemID.PlatinumBar, 5),
                (ItemID.Obsidian, 15));
        }
    }
}
