using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Blocks.MusicBoxes; // 引用物块所在的 Blocks 命名空间

namespace zhashi.Content.Items.Placeable.MusicBoxes
{
    public class MysteryMusicBox1 : ModItem
    {
        public override void SetStaticDefaults()
        {
            // 允许在微光转化为普通音乐盒
            ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.MusicBox;
            ItemID.Sets.CanGetPrefixes[Type] = false;
        }

        public override void SetDefaults()
        {
            // 1. 先调用辅助方法设置基础属性（这会将长宽设为默认的24）
            Item.DefaultToMusicBox(ModContent.TileType<MysteryMusicBox1Tile>(), 0);

            // 2. ★★★ 覆盖尺寸设置 ★★★
            Item.width = 21;
            Item.height = 32;

            // 3. 其他属性
            Item.value = Item.sellPrice(0, 2, 0, 0);
        }
    }
}