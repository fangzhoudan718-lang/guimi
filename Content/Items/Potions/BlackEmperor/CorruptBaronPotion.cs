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
    /// 序列六 · 腐化男爵。看似遵守规则，实际却一直在扭曲它。
    /// 核心是「扭曲」——改写攻击的归属，也改写对手的意图。
    /// </summary>
    public class CorruptBaronPotion : LotMItem
    {
        public override string Pathway => "BlackEmperor";
        public override int RequiredSequence => 7;
        // 同序列统一瓶型：引用愚者途径序列6的瓶型资源。
        public override string Texture => "zhashi/Content/Items/Potions/Fool/FacelessPotion";

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
            Item.rare = ItemRarityID.LightRed;
            Item.value = Item.buyPrice(gold: 5);
        }

        private static string KeyName(ModKeybind keybind, string fallback)
        {
            if (keybind == null) return fallback;
            var keys = keybind.GetAssignedKeys(InputMode.Keyboard);
            return keys.Count > 0 ? string.Join("/", keys) : fallback;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer) return true;
            LotMPlayer lotm = player.GetModPlayer<LotMPlayer>();
            if (lotm.baseBlackEmperorSequence != 7) return false;

            lotm.baseBlackEmperorSequence = 6;
            lotm.currentBlackEmperorSequence = 6;
            if (Main.netMode == NetmodeID.MultiplayerClient) lotm.SyncPlayer(-1, -1, false);
            PromotionPulse.Raise(player, PromotionPulse.BlackEmperor);

            SoundEngine.PlaySound(SoundID.Item3, player.Center);
            Main.NewText("甜美香气里藏着一点腐朽的甜腻，喉咙深处泛起金属味——晋升成功：序列6 腐化男爵！", 190, 130, 190);
            Main.NewText($"能力：扭曲 [{KeyName(LotMKeybinds.BlackEmperor_Twist, "/")}]；契约新增律令「审判」。被动「腐蚀」会慢慢侵蚀你身边的人。", 200, 150, 200);
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);
            tooltips.Add(new TooltipLine(Mod, "BlackEmperorIntro",
                "[c/9A6AB0:黑皇帝途径 · 序列6 腐化男爵]\n" +
                "看似遵守规则，实际却一直在扭曲它。\n" +
                "「扭曲」会改写攻击的归属与对手的意图；\n" +
                "身边的生灵也会被「腐蚀」，一点点变得阴暗。"));
        }

        public override void AddRecipes()
        {
            // 原著主材料「腐烂尸花 / 人脸狒狒的鼻子」用游戏内等价物替代；
            // 腐化与猩红两套世界各写一条，避免某一边做不出来。
            CreateDualRecipe(
                ModContent.ItemType<BlackEmperorCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.RottenChunk, 15),
                (ItemID.Hive, 10),
                (ItemID.ShadowScale, 5),
                (ItemID.Cobweb, 30));

            CreateDualRecipe(
                ModContent.ItemType<BlackEmperorCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.Vertebrae, 15),
                (ItemID.Hive, 10),
                (ItemID.TissueSample, 5),
                (ItemID.Cobweb, 30));
        }
    }
}
