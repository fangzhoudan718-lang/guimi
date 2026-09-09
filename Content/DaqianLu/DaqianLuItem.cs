using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using zhashi.Content; // 确保引用了你原本的LotMPlayer所在的命名空间

namespace zhashi.Content.DaqianLu
{
    public class DaqianLuItem : ModItem
    {
        public override void SetStaticDefaults()
        {
            // 这里可以添加一段符合原著的描述
            // Tooltip.SetDefault("这根本不是什么秘籍，这是来自地狱的账本。\n装备后按 [大千录技能键] 划开血肉施展神通。");
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 36;
            Item.accessory = true; // 设为饰品
            Item.rare = ItemRarityID.Quest; // 任务级稀有度（红色或橙色），代表其唯一性
            Item.value = Item.sellPrice(0, 0, 0, 0); // 原著中这东西没法用金钱衡量
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 获取你定义的独立逻辑类（我们在上一步创建的）
            var dqPlayer = player.GetModPlayer<DaqianLuPlayer>();
            dqPlayer.hasDaqianLu = true; // 激活大千录持有状态

            // 极致还原：装备大千录后，由于其邪异性，玩家的自然回血速度会受到抑制
            player.lifeRegenTime = 0;
            player.lifeRegen -= 2;
        }
        public override void UpdateInventory(Player player)
        {
            // 检测你的专属快捷键
            if (DaqianLuKeybinds.UseDaqianLu.JustPressed && player.GetModPlayer<DaqianLuPlayer>().hasDaqianLu)
            {
                DaqianLuActions.ExecuteSkillWithDice(player);
            }
        }

        // 修改鼠标悬停时的提示颜色，使其看起来更诡异
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            foreach (TooltipLine line in tooltips)
            {
                if (line.Name == "ItemName")
                {
                    line.OverrideColor = Color.DarkRed; // 名字显示为深红色
                }
            }
        }

        // 不要任何合成配方：此处留空或不写 AddRecipes
    }
}