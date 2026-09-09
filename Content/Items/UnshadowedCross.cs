using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content;

namespace zhashi.Content.Items
{
    public class UnshadowedCross : LotMItem
    {
        public override void SetStaticDefaults()
        {
            // 名称和描述由 Hjson 提供
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item29;
            Item.maxStack = 1;
            Item.consumable = false;   // 不消耗
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(gold: 10);
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();
                bool loweredAny = false;
                string msg = "";

                // 修改局部函数：操作 baseSeq 而不是 currentSeq
                void DowngradePathway(ref int baseSeq, ref int currentSeq, string pathwayName)
                {
                    // 如果不是凡人 (序列 < 10)
                    if (baseSeq < 10)
                    {
                        baseSeq++; // 修改存档变量 (永久生效)
                        currentSeq = baseSeq; // 同步修改当前变量 (让效果立刻显现)
                        loweredAny = true;

                        if (baseSeq >= 10)
                        {
                            baseSeq = 10;
                            currentSeq = 10;
                            msg += $"{pathwayName}途径特性已完全析出，回归平凡。\n";
                        }
                        else
                        {
                            msg += $"{pathwayName}途径序列降低为：序列{baseSeq}。\n";
                        }
                    }
                }

                // 检查并降低所有途径 (传入 base 和 current)
                DowngradePathway(ref modPlayer.baseSequence, ref modPlayer.currentSequence, "巨人");
                DowngradePathway(ref modPlayer.baseHunterSequence, ref modPlayer.currentHunterSequence, "红祭司");
                DowngradePathway(ref modPlayer.baseMoonSequence, ref modPlayer.currentMoonSequence, "月亮");
                DowngradePathway(ref modPlayer.baseFoolSequence, ref modPlayer.currentFoolSequence, "愚者");
                DowngradePathway(ref modPlayer.baseMarauderSequence, ref modPlayer.currentMarauderSequence, "错误");
                DowngradePathway(ref modPlayer.baseSunSequence, ref modPlayer.currentSunSequence, "太阳");
                DowngradePathway(ref modPlayer.baseDemonessSequence, ref modPlayer.currentDemonessSequence, "魔女");
                DowngradePathway(ref modPlayer.baseWheelSequence, ref modPlayer.currentWheelSequence, "命运");

                // 如果有变化
                if (loweredAny)
                {
                    // 视觉与文字提示
                    CombatText.NewText(player.getRect(), Color.Gold, "特性析出", true);
                    if (!string.IsNullOrEmpty(msg)) Main.NewText(msg.TrimEnd(), 255, 215, 0);

                    // 播放特效
                    for (int i = 0; i < 60; i++)
                    {
                        Vector2 speed = Main.rand.NextVector2Circular(3f, 3f);
                        Dust d = Dust.NewDustPerfect(player.Center, DustID.GoldFlame, speed, 0, default, 2.5f);
                        d.noGravity = true;
                    }

                    // 检查是否彻底变成凡人 (所有 base 都回到了 10)
                    // 注意：IsBeyonder 通常是检查 current，这里可以直接检查 IsBeyonder，因为上面已经同步了 current
                    if (!modPlayer.IsBeyonder)
                    {
                        modPlayer.spiritualityCurrent = 100; // 重置灵性
                        Main.NewText("你已彻底洗净了非凡特性，变回了普通人。", 200, 200, 200);
                    }
                }
                else
                {
                    Main.NewText("你已经是凡人了，没有任何非凡特性可以析出。", 150, 150, 150);
                }
            }
            return true;
        }
    }
}