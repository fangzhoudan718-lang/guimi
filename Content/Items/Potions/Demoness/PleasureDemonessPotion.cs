using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用魔女牌
using zhashi.Content.UI;                // ★核心引用：用于读取好感度系统

namespace zhashi.Content.Items.Potions.Demoness
{
    public class PleasureDemonessPotion : LotMItem
    {
        public override string Pathway => "Demoness";
        public override int RequiredSequence => 7; // 只有序列7女巫可以服用

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("欢愉魔女魔药");
            // Tooltip.SetDefault("序列6：欢愉魔女\n获得操控欲望、疾病与蛛丝的能力，掌握镜子替身之术。\n仪式要求：与特定NPC建立极深的羁绊(好感度100%)。");
        }

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
            Item.rare = ItemRarityID.Orange; // 序列6 橙色稀有度
            Item.value = Item.sellPrice(gold: 3);
            Item.buffType = BuffID.WellFed;
            Item.buffTime = 300;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

                // 1. 【状态检查】防止重复晋升
                if (modPlayer.baseDemonessSequence <= 6)
                {
                    Main.NewText("你已经掌握了欢愉的权柄（序列6或更高），无需再次服用。", 255, 255, 0);
                    return true; // 不消耗物品（或者返回 true 消耗，看你喜好）
                }

                // 2. 【前置检查】必须是序列7
                if (!modPlayer.IsBeyonder || modPlayer.baseDemonessSequence != 7)
                {
                    Main.NewText("你不是女巫（序列7），无法越级晋升！", 255, 50, 50);
                    return true;
                }

                // 3. 【仪式逻辑】检查好感度
                bool ritualConditionMet = false;

                // 目标 NPC ID 列表
                int[] requiredNPCs = {
                    NPCID.Nurse,    // 护士
                    NPCID.Dryad,    // 树妖
                    NPCID.Guide,    // 向导
                    NPCID.Angler    // 渔夫
                };

                // 遍历检查
                foreach (int npcId in requiredNPCs)
                {
                    // ★ 核心修复：直接从 FavorabilitySystem 获取好感度
                    int currentScore = FavorabilitySystem.GetFavorability(npcId);

                    // 调试信息（正式发布可注释掉）
                    // Main.NewText($"仪式检查 - NPC ID: {npcId}, 当前好感: {currentScore}", 200, 200, 200);

                    if (currentScore >= 100)
                    {
                        ritualConditionMet = true;
                        Main.NewText($"仪式完成！你与 [NPC {npcId}] 的羁绊成为了锚。", 0, 255, 0);
                        break; // 只要有一个达标即可
                    }
                }

                // 仪式失败判定
                if (!ritualConditionMet)
                {
                    Main.NewText("仪式失败：你并未真正享受欢愉...", 255, 50, 50);
                    Main.NewText("提示：需将 护士/树妖/向导/渔夫 中任一NPC的好感度提升至100。", 255, 100, 100);
                    return true; // 消耗魔药作为惩罚
                }

                // 4. 【执行晋升】
                modPlayer.baseDemonessSequence = 6; // 修改数据

                // 播放音效与特效
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, player.position); // 喝水声
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103, player.position); // 魔法声

                // 5. 【成功反馈】
                Main.NewText("你感到无数虚幻的蛛丝缠绕在指尖...", 255, 105, 180); // 亮粉色提示
                Main.NewText("晋升成功！序列6：欢愉魔女！", 255, 20, 147); // 深粉色确认
            }
            return true;
        }

        // 魔药配方 (AddRecipes)
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<DemonessCard>(), // 亵渎之牌：魔女
                (ItemID.BottledWater, 1),  // 纯水
                (ItemID.Lens, 2),          // 魅欲女妖的眼睛 -> 晶状体
                (ItemID.SpiderFang, 1),    // 寡妇巨蛛丝腺 -> 蜘蛛牙
                (ItemID.TatteredCloth, 1), // 魅欲女妖毛发 -> 破布
                (ItemID.StinkPotion, 1),   // 费内波特苍蝇粉 -> 臭味药水
                (ItemID.DarkShard, 1)      // 木乃伊骨灰 -> 暗黑碎片
            );
        }
    }
}