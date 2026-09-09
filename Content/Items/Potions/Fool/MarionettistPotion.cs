using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using zhashi.Content.Items.Accessories; // 引用愚者牌

namespace zhashi.Content.Items.Potions.Fool
{
    public class MarionettistPotion : LotMItem
    {
        // 设定途径和前置序列 (需要序列6 无面人)
        public override string Pathway => "Fool";
        public override int RequiredSequence => 6;

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 30;
            Item.consumable = true;
            Item.rare = ItemRarityID.Pink; // 序列5级别
            Item.value = Item.sellPrice(gold: 20);
        }

        // 【核心检查】
        public override bool CanUseItem(Player player)
        {
            // 1. 先检查序列要求
            if (!base.CanUseItem(player)) return false;

            // 2. 仪式检查：必须在海洋 (模拟美人鱼歌声)
            if (!player.ZoneBeach)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    Main.NewText("仪式未满足：需在海洋环境服食...", 255, 50, 50);
                }
                return false;
            }
            return true;
        }

        // 【UI提示】
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            Player p = Main.LocalPlayer;
            string color = p.ZoneBeach ? "00FF00" : "FF0000";
            string status = p.ZoneBeach ? "满足" : "未满足";

            tooltips.Add(new TooltipLine(Mod, "Ritual", $"[c/{color}:仪式要求：在海洋环境服食 ({status})]"));
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();
                modPlayer.baseFoolSequence = 5; // 晋升序列5

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, player.position);
                Main.NewText("你看见了无数根丝线，它们连接着万物...", 148, 0, 211);
                Main.NewText("晋升成功！序列5：秘偶大师！", 50, 255, 50);
            }
            return true;
        }

        public override void AddRecipes()
        {
            // 使用智能配方生成器
            CreateDualRecipe(
                ModContent.ItemType<FoolCard>(), // 愚者牌

                (ItemID.BottledWater, 1),
                (ItemID.SoulofFlight, 5),   // 古老怨灵粉尘 (飞翔之魂)
                (ItemID.HallowedBar, 5),    // 石像鬼核心 (神圣锭)
                (ItemID.RichMahogany, 5),   // 龙纹树皮 (红毛桦)
                (ItemID.Bone, 10),          // 怨灵残余 (骨头)
                (ItemID.Lens, 2)            // 石像鬼眼睛 (晶状体)
            );
        }
    }
}