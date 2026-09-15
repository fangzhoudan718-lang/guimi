using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories;
using zhashi.Content.Pathways.Door;

namespace zhashi.Content.Items.Potions.Door
{
    public abstract class HighSequenceDoorPotion : LotMItem
    {
        public override string Pathway => "Door";
        protected abstract int NewSequence { get; }
        // 本模组的魔药瓶按序列统一造型：直接引用愚者途径同序列资源，
        // 不复制或移动PNG，避免重复资源及路径迁移造成的失效。
        public override string Texture => NewSequence switch
        {
            4 => "zhashi/Content/Items/Potions/Fool/BizarroSorcererPotion",
            3 => "zhashi/Content/Items/Potions/Fool/ScholarOfYorePotion",
            2 => "zhashi/Content/Items/Potions/Fool/MiracleInvokerPotion",
            1 => "zhashi/Content/Items/Potions/Fool/AttendantPotion",
            _ => "zhashi/Content/Items/Potions/Door/TravelerPotion"
        };
        protected abstract string SequenceName { get; }
        protected abstract string RitualText { get; }
        protected abstract bool RitualComplete(DoorPathwayPlayer door);
        protected abstract string AbilitySummary { get; }
        protected virtual bool StartRitualAttempt(Player player, DoorPathwayPlayer door) => false;

        public override void SetDefaults()
        {
            Item.width = 24; Item.height = 32;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 17; Item.useTime = 17; Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 30; Item.consumable = true;
            Item.rare = NewSequence == 4 ? ItemRarityID.Yellow : NewSequence == 3 ? ItemRarityID.Lime : NewSequence == 2 ? ItemRarityID.Cyan : ItemRarityID.Red;
            Item.value = Item.sellPrice(gold: NewSequence == 4 ? 20 : NewSequence == 3 ? 35 : NewSequence == 2 ? 60 : 100);
        }

        public override bool CanUseItem(Player player)
        {
            if (!base.CanUseItem(player)) return false;
            if (!RitualComplete(player.GetModPlayer<DoorPathwayPlayer>()))
            {
                DoorPathwayPlayer door = player.GetModPlayer<DoorPathwayPlayer>();
                if (player.whoAmI == Main.myPlayer && !StartRitualAttempt(player, door))
                    Main.NewText("晋升仪式尚未完成。", 255, 70, 70);
                return false;
            }
            return true;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer) return true;
            LotMPlayer lotm = player.GetModPlayer<LotMPlayer>();
            if (lotm.baseDoorSequence != RequiredSequence) return false;
            lotm.baseDoorSequence = NewSequence;
            lotm.currentDoorSequence = NewSequence;
            if (Main.netMode == NetmodeID.MultiplayerClient) lotm.SyncPlayer(-1, -1, false);
            PromotionPulse.Raise(player, PromotionPulse.Door);
            SoundEngine.PlaySound(SoundID.Item104, player.Center);
            Main.NewText($"晋升成功：序列{NewSequence} {SequenceName}！", 232, 200, 120);
            Main.NewText(AbilitySummary, 210, 185, 255);
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);
            DoorPathwayPlayer door = Main.LocalPlayer.GetModPlayer<DoorPathwayPlayer>();
            string status = RitualComplete(door) ? "[c/00FF00:已完成]" : "[c/FF5555:未完成]";
            tooltips.Add(new TooltipLine(Mod, "DoorRitual", $"{RitualText} {status}"));
            tooltips.Add(new TooltipLine(Mod, "DoorAbilities", AbilitySummary));
        }
    }

    public class SecretsSorcererPotion : HighSequenceDoorPotion
    {
        public override int RequiredSequence => 5;
        protected override int NewSequence => 4;
        protected override string SequenceName => "秘法师";
        protected override string RitualText => "仪式：将敌对Boss削弱至20%生命以下并靠近它，尝试使用本魔药启动封印；第一次不消耗魔药，仪式完成后再次饮用晋升。";
        protected override string AbilitySummary => $"能力：空间隐藏 [{LotMKeybinds.GetBindingText(LotMKeybinds.Door_SecretSpace)}] / 放逐 [{LotMKeybinds.GetBindingText(LotMKeybinds.Door_Banish)}] / 强化闪现";
        protected override bool RitualComplete(DoorPathwayPlayer door) => door.secretSealRitualComplete;
        protected override bool StartRitualAttempt(Player player, DoorPathwayPlayer door)
        {
            door.RequestSecretSealRitual();
            return true;
        }
        public override void AddRecipes() => CreateDualRecipe(ModContent.ItemType<DoorCard>(),
            (ItemID.BottledWater, 1), (ItemID.CrystalBall, 1), (ItemID.CrystalShard, 20),
            (ItemID.Ectoplasm, 10), (ItemID.SoulofFlight, 10), (ItemID.HallowedBar, 10));
    }

    public class WandererPotion : HighSequenceDoorPotion
    {
        public override int RequiredSequence => 4;
        protected override int NewSequence => 3;
        protected override string SequenceName => "漫游者";
        protected override string RitualText => "仪式：分别进入灵界、血月高空和任意四柱影响区，记录三份星界信息衍生的危险场景。";
        protected override string AbilitySummary => $"能力：空间牢笼 [{LotMKeybinds.GetBindingText(LotMKeybinds.Door_SpatialPrison)}] / 撕裂空间 [{LotMKeybinds.GetBindingText(LotMKeybinds.Door_SpaceTear)}] / 漫游";
        protected override bool RitualComplete(DoorPathwayPlayer door) => door.WandererRitualComplete;
        public override void AddRecipes() => CreateDualRecipe(ModContent.ItemType<DoorCard>(),
            (ItemID.BottledWater, 1), (ItemID.Ectoplasm, 20), (ItemID.FragmentVortex, 10),
            (ItemID.FragmentNebula, 10), (ItemID.MartianConduitPlating, 30), (ItemID.SoulofFlight, 20));
    }

    public class PlaneswalkerPotion : HighSequenceDoorPotion
    {
        public override int RequiredSequence => 3;
        protected override int NewSequence => 2;
        protected override string SequenceName => "旅法师";
        protected override string RitualText => "仪式：在太空层或异度空间击败九种不同Boss，在星球之外留下九段传说。";
        protected override string AbilitySummary => $"能力：维度之视 [{LotMKeybinds.GetBindingText(LotMKeybinds.Door_DimensionalSight)}] / 人物再现 [{LotMKeybinds.GetBindingText(LotMKeybinds.Door_Reenact)}]\n场景记录 [Shift + {LotMKeybinds.GetBindingText(LotMKeybinds.Door_Reenact)}] / 返回场景 [Ctrl + {LotMKeybinds.GetBindingText(LotMKeybinds.Door_Reenact)}]（限当前世界/空间）";
        protected override bool RitualComplete(DoorPathwayPlayer door) => door.TravelerRitualComplete;
        public override void AddRecipes() => CreateDualRecipe(ModContent.ItemType<DoorCard>(),
            (ItemID.BottledWater, 1), (ItemID.LunarBar, 15), (ItemID.FragmentVortex, 15),
            (ItemID.FragmentNebula, 15), (ItemID.FragmentStardust, 15), (ItemID.FragmentSolar, 15),
            (ItemID.Ectoplasm, 20));
    }

    public class KeyOfStarsPotion : HighSequenceDoorPotion
    {
        public override int RequiredSequence => 2;
        protected override int NewSequence => 1;
        protected override string SequenceName => "星之匙";
        protected override string RitualText => "仪式：在太空层击败月亮领主，与旋转并发出信号的沉重星体建立神秘学联系。";
        protected override string AbilitySummary => $"能力：时空迷宫 [{LotMKeybinds.GetBindingText(LotMKeybinds.Door_TimeSpaceMaze)}] / 空间破碎 [{LotMKeybinds.GetBindingText(LotMKeybinds.Door_SpaceShatter)}] / 定位与封印权柄\n警告：空间破碎会摧毁屏幕内的可破坏物块，请勿在家中使用。";
        protected override bool RitualComplete(DoorPathwayPlayer door) => door.starKeyPulsarRitualComplete;
        public override void AddRecipes() => CreateDualRecipe(ModContent.ItemType<DoorCard>(),
            (ItemID.BottledWater, 1), (ItemID.LunarBar, 30), (ItemID.CelestialSigil, 1),
            (ItemID.Compass, 1), (ItemID.FragmentVortex, 20), (ItemID.FragmentNebula, 20),
            (ItemID.FragmentStardust, 20), (ItemID.FragmentSolar, 20));
    }
}
