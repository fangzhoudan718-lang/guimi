using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using zhashi.Content.Items.Accessories;
using zhashi.Content.Projectiles.Demoness; // 引用分身弹幕

namespace zhashi.Content.Items.Potions.Demoness
{
    public class UnagingDemonessPotion : LotMItem
    {

        public override string Pathway => "Demoness";
        public override int RequiredSequence => 4; // 前置序列4

        public override void SetDefaults()
        {
            Item.width = 20; Item.height = 30;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 17; Item.useTime = 17; Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 30; Item.consumable = true;
            Item.rare = ItemRarityID.Cyan; // 序列3 青色/光辉品质
            Item.value = Item.sellPrice(platinum: 1);
        }

        public override bool CanUseItem(Player player)
        {
            if (!base.CanUseItem(player)) return false;

            // --- 仪式检查：与镜中倒影融合 ---
            // 1. 遍历所有弹幕，寻找属于玩家的镜面分身
            Projectile clone = null;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && p.type == ModContent.ProjectileType<MirrorCloneProjectile>())
                {
                    clone = p;
                    break;
                }
            }

            // 2. 如果没开分身，或者距离太远
            if (clone == null)
            {
                if (player.whoAmI == Main.myPlayer)
                    Main.NewText("仪式未完成：你需要先召唤镜中倒影 (按Z键)。", 255, 50, 50);
                return false;
            }

            if (Vector2.Distance(player.Center, clone.Center) > 40f)
            {
                if (player.whoAmI == Main.myPlayer)
                    Main.NewText("仪式未完成：你需要走向镜中倒影，与其合二为一。", 255, 50, 50);
                return false;
            }

            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);
            tooltips.Add(new TooltipLine(Mod, "Ritual", "[c/FF00FF:仪式要求：召唤镜面分身并与其重叠融合]"));
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();
                modPlayer.baseDemonessSequence = 3;

                // 播放神秘音效
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item119, player.Center); // 魔法音效
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Shatter, player.Center); // 镜子破碎声，象征融合

                Main.NewText("镜面破碎，时光在你身上凝固...", 0, 255, 255);
                Main.NewText("晋升成功！序列3：不老魔女！", 0, 255, 255);

                // 融合后关闭分身，增加仪式感
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.owner == player.whoAmI && p.type == ModContent.ProjectileType<MirrorCloneProjectile>())
                    {
                        p.Kill();
                        break;
                    }
                }
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<DemonessCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.Ectoplasm, 5),      // 主材料：灵气
                (ItemID.HallowedBar, 5),    // 永恒之金
                (ItemID.MagicMirror, 1),    // 镜中核心 (消耗一个魔镜)
                (ItemID.LifeFruit, 1),      // 青春头发 (消耗一个生命果)
                (ItemID.CrystalShard, 10)   // 辅助
            );
        }
    }
}