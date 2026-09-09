using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.UI;
using zhashi.Content.UI.Profiles;

namespace zhashi.Content.Items.Story
{
    public class KleinsDiary : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("克莱恩的日记"); 
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.rare = ItemRarityID.Blue;
            Item.consumable = false;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var uiSystem = ModContent.GetInstance<GalgameUISystem>();

                // ★★★ 核心修复2：防重置逻辑 ★★★
                // 如果日记已经打开了 (IsManualMode 为 true)，则关闭它，而不是重置它
                if (uiSystem.DialogueUI.IsManualMode)
                {
                    uiSystem.DialogueUI.Close();
                    return true;
                }

                // 如果没打开，则新建并打开
                var diaryProfile = new KleinsDiaryProfile();
                diaryProfile.ResetPage();
                uiSystem.DialogueUI.SetCustomProfile(diaryProfile);
                uiSystem.DialogueUI.Show();
            }
            return true;
        }
    }
}