using Terraria;
using Terraria.ID;
using Terraria.Localization; // 用于处理文字
using Terraria.ModLoader;
using zhashi.Content;        // 引用 LotMPlayer

namespace zhashi.Content.Systems
{
    public class SunRecipes : ModSystem
    {
        public override void AddRecipes()
        {
            // 定义一个严格的条件：必须是序列8或更强
            Condition isLightSupplicant = new Condition(
                // 1. 条件描述 (鼠标悬停在“合成条件”上时显示的文字)
                Language.GetOrRegister("Mods.zhashi.Conditions.IsLightSupplicant"),
                // 2. 判断逻辑 (返回 true 才能合成)
                () => Main.LocalPlayer.GetModPlayer<LotMPlayer>().currentSunSequence <= 8
            );

            // 注册配方
            Recipe.Create(ItemID.HolyWater, 5) // 产出 5 瓶圣水
                .AddIngredient(ItemID.BottledWater, 5) // 消耗 5 瓶水
                .AddIngredient(ItemID.SilverCoin, 1)   // 消耗 1 银币 (仪式触媒)
                .AddCondition(Condition.NearWater)     // 条件：在水源旁 (原著设定)
                .AddCondition(isLightSupplicant)       // 条件：必须是祈光人
                .Register();
        }
    }
}