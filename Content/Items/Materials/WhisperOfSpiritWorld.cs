using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content.Buffs;

namespace zhashi.Content.Items.Materials
{
    public class WhisperOfSpiritWorld : ModItem
    {
        // 【核心修复】借用原版“灵气”的贴图，防止找不到图片报错
        public override string Texture => "Terraria/Images/Item_" + ItemID.Ectoplasm;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("灵界低语");
            // Tooltip.SetDefault("一段来自灵界深处的模糊信息\n使用它来尝试解析'真名'\n警告：如果解析错误，你将付出惨痛代价");
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 99;
            Item.consumable = true;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 45;
            Item.useAnimation = 45;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.buyPrice(gold: 10);
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                // 仪式判定：60% 成功率
                if (Main.rand.NextFloat() < 0.1f)
                {
                    // 成功：获得真名
                    player.QuickSpawnItem(player.GetSource_FromThis(), ModContent.ItemType<TrueNameInscription>());
                    CombatText.NewText(player.getRect(), Color.Gold, "真名解析成功！", true);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, player.position);
                }
                else
                {
                    // 失败：成为月亮的奴仆 (惩罚)
                    CombatText.NewText(player.getRect(), Color.Purple, "你听到了疯狂的呓语...", true);
                    player.AddBuff(BuffID.Confused, 600); // 困惑
                    player.AddBuff(BuffID.MoonLeech, 600); // 月噬
                    player.statLife -= 200; // 扣除生命
                    if (player.statLife <= 0)
                    {
                        player.KillMe(Terraria.DataStructures.PlayerDeathReason.ByCustomReason(player.name + " 成为了月亮的奴仆"), 1000, 0);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath10, player.position);
                }
            }
            return true;
        }

        public override void AddRecipes()
        {
            // 配方：灵气 + 异界材料
            CreateRecipe()
                .AddIngredient(ItemID.Ectoplasm, 5)
                .AddIngredient(ItemID.SoulofNight, 5)
                .AddIngredient(ItemID.SoulofLight, 5)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }

    // 这是成功后获得的材料：真名拓本
    public class TrueNameInscription : ModItem
    {
        // 【核心修复】借用原版“破布”的贴图，防止找不到图片报错
        public override string Texture => "Terraria/Images/Item_" + ItemID.TatteredCloth;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("真名拓本");
            // Tooltip.SetDefault("一段被定义的、与你命运缠绕的特殊信息\n序列3魔药的核心材料");
        }
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 26;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Red;
        }
    }
}