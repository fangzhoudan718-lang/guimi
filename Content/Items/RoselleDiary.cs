using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.UI; // 引用 UI 系统

namespace zhashi.Content.Items
{
    public class RoselleDiary : LotMItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("罗塞尔的随记");
            // Tooltip.SetDefault("这上面用一种奇怪的符号写满了文字...\n<右键点击阅读>");
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 32;
            Item.maxStack = 1;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp; // 举起书本的动作
            Item.consumable = false; // 不消耗
            Item.rare = ItemRarityID.Orange; // 橙色品质
            Item.value = 0;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                // 调用 UI 系统切换显示状态
                ModContent.GetInstance<RoselleDiaryUISystem>().ToggleUI();
            }
            return true;
        }

        // 允许右键点击使用
        public override bool AltFunctionUse(Player player) => true;
    }
}