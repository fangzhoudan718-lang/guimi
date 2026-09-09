using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content;

namespace zhashi.Content.Items.Materials
{
    public class ConquerorCharacteristic : ModItem
    {
        // 请确保你有贴图：zhashi/Content/Items/Materials/ConquerorCharacteristic.png
        // 如果没有，请暂时解开下面这行的注释借用贴图：
        // public override string Texture => "Terraria/Images/Item_" + ItemID.RedPotion;

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 1;
            Item.value = 0; // 无价
            Item.rare = ItemRarityID.Red;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.consumable = false; // 不消耗，可反复开关
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // 显示当前开关状态
            string status = ConquerorSpawnSystem.StopSpawning ? "[c/FF0000:已开启 (生物停止繁衍)]" : "[c/00FF00:未开启 (正常生态)]";
            tooltips.Add(new TooltipLine(Mod, "Status", $"世界状态: {status}"));

            // 显示仪式进度
            var player = Main.LocalPlayer.GetModPlayer<LotMPlayer>();
            if (player.currentHunterSequence == 2) // 只有荣耀者能看到仪式
            {
                string ritualStatus = player.conquerorRitualComplete ? "[c/00FF00:已完成 (全图肃清)]" : "[c/FF0000:未完成 (仍有敌意存活)]";
                tooltips.Add(new TooltipLine(Mod, "RitualDesc", "晋升仪式: 开启此特性停止刷新，然后杀光地图上所有敌对生物。"));
                tooltips.Add(new TooltipLine(Mod, "RitualStatus", ritualStatus));
            }
        }

        public override bool? UseItem(Player player)
        {
            // 切换开关
            ConquerorSpawnSystem.StopSpawning = !ConquerorSpawnSystem.StopSpawning;

            if (ConquerorSpawnSystem.StopSpawning)
            {
                Main.NewText("征服者的威压笼罩了世界，万物噤声...", 255, 50, 50);
                SoundEngine.PlaySound(SoundID.ForceRoar, player.position); // 恐怖吼叫
            }
            else
            {
                Main.NewText("威压消散，生命开始重新躁动。", 100, 255, 100);
            }

            return true;
        }

        public override void AddRecipes()
        {
            // "所有可入住NPC掉落物" 的概念配方
            CreateRecipe()
                // 1. 核心：向导与服装商的命 (允许杀害NPC)
                .AddIngredient(ItemID.GuideVoodooDoll, 1)
                .AddIngredient(ItemID.ClothierVoodooDoll, 1)
                // 2. 财富：商人/税收官
                .AddIngredient(ItemID.PlatinumCoin, 5)
                // 3. 快乐：派对女孩
                .AddIngredient(ItemID.Confetti, 100)
                // 4. 酒精：酒馆老板
                .AddIngredient(ItemID.Ale, 5)
                // 5. 军火：军火商
                .AddIngredient(ItemID.Minishark, 1)
                // 6. 机械：机械师
                .AddIngredient(ItemID.Wrench, 1)
                // 7. 自然：树妖 (净化粉)
                .AddIngredient(ItemID.PurificationPowder, 50)
                // 8. 染料：染料商
                .AddIngredient(ItemID.DyeVat, 1)
                // 9. 魔法：巫师 (水晶球)
                .AddIngredient(ItemID.CrystalBall, 1)
                // 在 恶魔祭坛/猩红祭坛 合成
                .AddTile(TileID.DemonAltar)
                .Register();
        }
    }
}