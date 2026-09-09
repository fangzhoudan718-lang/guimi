using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content; // 引用玩家数据

namespace zhashi.Content.Items.Debug
{
    public class ResurrectionReset : ModItem
    {
        // 【关键】借用原版“金表”的贴图，防止蓝屏，也符合“重置时间”的意境
        public override string Texture => "Terraria/Images/Item_" + ItemID.GoldWatch;

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.useStyle = ItemUseStyleID.HoldUp; // 举起动作
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.rare = ItemRarityID.Red; // 红色稀有度，代表开发者物品
            Item.consumable = false; // 无限使用
            Item.value = 0;
        }

        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 核心逻辑：直接将冷却时间归零
            modPlayer.twilightResurrectionCooldown = 0;

            // 提示信息
            Main.NewText("【测试】黄昏重生冷却已重置！随时准备赴死。", 0, 255, 0);

            // 播放一个魔法音效
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item37, player.position); // 类似附魔的声音

            return true;
        }

        // 不写 AddRecipes，这样只能通过 Cheat Sheet / Hero's Mod 拿取，普通玩家无法合成
    }
}
