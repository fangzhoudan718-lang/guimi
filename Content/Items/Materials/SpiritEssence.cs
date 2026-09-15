using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.SealedArtifacts;

namespace zhashi.Content.Items.Materials
{
    /// <summary>
    /// 学徒途径非凡特性：门途径的超凡者陨落时，从体内析出的非凡特性。
    ///
    /// 只有门途径的非凡者死去才会掉落，而且序列越高凝得越多：
    /// 序列九 1 枚 → 序列一 9 枚。它是灵性恢复药水的主料之一，
    /// 另一条路是拿墓碑（也就是说，凡人用自己的死也能换来同样的东西）。
    /// </summary>
    public class SpiritEssence : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.maxStack = 999;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = Item.sellPrice(silver: 50);
            Item.consumable = true;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.UseSound = SoundID.Item4;
        }

        public override bool CanUseItem(Player player) => player.GetModPlayer<SealedArtifactActivityPlayer>().Activity >= 15f;

        public override bool? UseItem(Player player) => player.GetModPlayer<SealedArtifactActivityPlayer>().SuppressWithCharacteristic();

    }
}
