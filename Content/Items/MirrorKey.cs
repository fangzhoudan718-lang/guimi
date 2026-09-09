using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using SubworldLibrary;
using zhashi.Content.Dimensions;
using Terraria.Localization;
using zhashi.Content.Systems;

namespace zhashi.Content.Items
{
    public class MirrorKey : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("镜子密钥");
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 26;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.rare = ItemRarityID.Pink;
            Item.UseSound = SoundID.Item29;
            Item.consumable = false;
        }

        // ★★★ 核心修复：正确的序列判定逻辑 ★★★
        public override bool CanUseItem(Player player)
        {
            var p = player.GetModPlayer<LotMPlayer>();

            // 1. 必须是魔女途径 (序列 9~0)
            // 错误逻辑修正：之前写 ==0 导致神被禁，普通人(10)反而通过
            // 现在改为：如果序列等级 >= 10 (普通人)，则禁止使用
            if (p.currentDemonessSequence >= 10)
            {
                if (player.whoAmI == Main.myPlayer)
                    Main.NewText("你没有魔女的灵性，无法通过镜面...", 200, 50, 50);
                return false;
            }

            // 2. 必须没有其他途径 (防止数据残留或双修导致的BUG)
            // 检查逻辑：如果其他途径的序列号 < 10 (说明是序列9~0)，则视为拥有该途径
            if (p.currentHunterSequence < 10 ||
                p.currentFoolSequence < 10 ||
                p.currentMarauderSequence < 10 ||
                p.currentSunSequence < 10 ||
                p.currentMoonSequence < 10) // 补全所有途径
            {
                if (player.whoAmI == Main.myPlayer)
                    Main.NewText("杂乱的非凡特性干扰了镜面通道！", 255, 100, 100);
                return false;
            }

            return true;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                if (SubworldSystem.Current == null)
                {
                    Main.NewText("镜面破碎，通道开启...", 100, 200, 255);
                    SubworldSystem.Enter<MirrorWorld>();
                }
                else if (SubworldSystem.IsActive<MirrorWorld>())
                {
                    Main.NewText("回归现实...", 200, 200, 200);
                    SubworldSystem.Exit();
                }
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Glass, 20)
                .AddIngredient(ItemID.SoulofNight, 5)
                .AddIngredient(ItemID.CrystalShard, 3)
                .AddTile(TileID.MythrilAnvil)
                .AddCondition(new Condition("需要魔女途径序列5 (痛苦魔女)", () =>
                {
                    var p = Main.LocalPlayer.GetModPlayer<LotMPlayer>();
                    // 配方条件：序列号必须 <= 5 (代表序列5、4...0)
                    // 10 <= 5 为 false，所以普通人看不到配方
                    return p.currentDemonessSequence <= 5;
                }))
                .Register();
        }
    }
}