using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content;

namespace zhashi.Content.Items.Vanities
{
    // [AutoloadEquip] 特性会自动寻找对应的装备贴图 Monocle_Face.png
    [AutoloadEquip(EquipType.Face)]
    public class Monocle : ModItem // 改为继承 ModItem，移除 LotM 逻辑限制
    {
        private static int leftFaceSlot = -1;

        public override void Load()
        {
            leftFaceSlot = EquipLoader.AddEquipTexture(Mod, Texture + "_FaceLeft",
                EquipType.Face, this, "MonocleLeft");
        }

        public override void Unload() => leftFaceSlot = -1;

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.accessory = true; // 设为饰品槽位可佩戴
            Item.vanity = true;    // 标记为时装物品，不提供战斗属性

            Item.rare = ItemRarityID.Blue;
            Item.value = Item.buyPrice(gold: 1);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (!hideVisual)
                ApplyOccasionalLeftEye(player);
        }

        public override void UpdateVanity(Player player) => ApplyOccasionalLeftEye(player);

        private static void ApplyOccasionalLeftEye(Player player)
        {
            if (leftFaceSlot < 0 || player.GetModPlayer<LotMPlayer>().currentMarauderSequence > 9)
                return;

            MonocleFlavorPlayer flavor = player.GetModPlayer<MonocleFlavorPlayer>();
            flavor.RollSideOnce();
            if (!flavor.wearOnLeft)
                return;

            player.face = leftFaceSlot;
            if (player.whoAmI == Main.myPlayer && !flavor.noticedSwitch)
            {
                flavor.noticedSwitch = true;
                Main.NewText(Terraria.Localization.Language.GetTextValue(
                    "Mods.zhashi.Messages.Flavor.MonocleOtherSide"), 160, 150, 185);
            }
        }

        public override void AddRecipes()
        {
            // 任何人都可以直接在工作台用 1 个玻璃制作
            CreateRecipe()
                .AddIngredient(ItemID.Glass, 1)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }

    public class MonocleFlavorPlayer : ModPlayer
    {
        public bool sideRolled;
        public bool wearOnLeft;
        public bool noticedSwitch;

        public override void OnEnterWorld()
        {
            sideRolled = false;
            wearOnLeft = false;
            noticedSwitch = false;
        }

        public void RollSideOnce()
        {
            if (sideRolled)
                return;
            sideRolled = true;
            wearOnLeft = Main.rand.NextBool(12);
        }
    }
}
