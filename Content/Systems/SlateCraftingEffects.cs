using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures; // 【核心修复】必须添加这一行
using zhashi.Content.Items;

namespace zhashi.Content.Systems
{
    public class SlateCraftingEffects : GlobalItem
    {
        public override void OnCreated(Item item, ItemCreationContext context)
        {
            // 检查是否是合成产生的
            if (context is RecipeItemCreationContext recipeContext)
            {
                // 检查合成配方中是否使用了亵渎石板
                foreach (Item ingredient in recipeContext.Recipe.requiredItem)
                {
                    if (ingredient.type == ModContent.ItemType<BlasphemySlate>())
                    {
                        // === 触发破碎特效 ===
                        Player player = Main.LocalPlayer;

                        // 1. 播放清脆的破碎声
                        SoundEngine.PlaySound(SoundID.Shatter, player.position);

                        // 2. 生成石块粒子
                        for (int i = 0; i < 20; i++)
                        {
                            Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.Stone, 0, 0, 0, default, 1.2f);
                            d.velocity *= 1.5f;
                            d.noGravity = false;
                        }

                        // 3. 生成魔法烟雾
                        for (int i = 0; i < 10; i++)
                        {
                            Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.Smoke, 0, 0, 100, Microsoft.Xna.Framework.Color.Gray, 1.5f);
                            d.velocity *= 0.5f;
                        }

                        break; // 只要检测到一个石板就跳出
                    }
                }
            }
        }
    }
}