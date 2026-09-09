using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using zhashi.Content.Items;

namespace zhashi.Content
{
    public abstract class LotMItem : ModItem
    {
        // ==========================================
        // 1. 基础属性设置
        // ==========================================
        public virtual string Pathway => "None";
        public virtual int RequiredSequence => 0;

        // 【条件1】需持有亵渎石板
        public static readonly Condition HasBlasphemySlate = new Condition(
            Language.GetOrRegister("Mods.zhashi.Conditions.HasBlasphemySlate", () => "需持有亵渎石板"),
            () => Main.LocalPlayer.HasItem(ModContent.ItemType<BlasphemySlate>())
        );

        // 【条件2】需持有水瓶 (用于隐藏配方)
        // 这样向导不会因为水瓶而剧透，但在制作栏旁边会提示“需消耗水瓶”
        public static readonly Condition HasBottledWater = new Condition(
            Language.GetOrRegister("Mods.zhashi.Conditions.HasBottledWater", () => "需消耗水瓶"),
            () => Main.LocalPlayer.HasItem(ItemID.BottledWater)
        );

        // ==========================================
        // 2. 核心配方逻辑 (双配方 + 防剧透)
        // ==========================================
        protected void CreateDualRecipe(int cardItemID, params (int itemID, int count)[] ingredients)
        {
            // 我们需要把参数里的“水瓶”剔除出来，改为“条件消耗”
            // 这样拿着水瓶问向导就不会显示配方了
            List<(int id, int num)> filteredIngredients = new List<(int id, int num)>();
            bool needsWater = false;
            int waterCount = 1;

            foreach (var ing in ingredients)
            {
                if (ing.itemID == ItemID.BottledWater)
                {
                    needsWater = true;
                    waterCount = ing.count; // 记录需要多少水瓶(通常是1)
                }
                else
                {
                    filteredIngredients.Add(ing);
                }
            }

            // =========================================================
            // 配方 A：【持牌者特权】 (有牌 -> 免费制作)
            // =========================================================
            if (cardItemID > 0)
            {
                Recipe rCard = CreateRecipe();

                // 1. 添加除了水瓶以外的材料
                foreach (var ing in filteredIngredients) rCard.AddIngredient(ing.id, ing.num);

                // 2. 添加亵渎之牌 (作为钥匙，向导可见)
                rCard.AddIngredient(cardItemID, 1);
                rCard.AddConsumeItemCallback((Recipe r, int type, ref int amount) => {
                    if (type == cardItemID) amount = 0; // 牌不消耗
                });

                // 3. 处理水瓶 (作为条件 + 手动消耗)
                if (needsWater)
                {
                    rCard.AddCondition(HasBottledWater); // 界面显示条件
                    rCard.AddOnCraftCallback((Recipe r, Item item, List<Item> consumedItems, Item destinationStack) => {
                        // 手动扣除水瓶
                        for (int i = 0; i < waterCount; i++) Main.LocalPlayer.ConsumeItem(ItemID.BottledWater);
                    });
                }

                rCard.AddTile(TileID.Bottles);
                rCard.Register();
            }

            // =========================================================
            // 配方 B：【无牌者代价】 (无牌 + 有石板 -> 消耗石板)
            // =========================================================
            Recipe rSlate = CreateRecipe();

            // 1. 添加除了水瓶以外的材料
            foreach (var ing in filteredIngredients) rSlate.AddIngredient(ing.id, ing.num);

            // 2. 添加条件：必须有石板 (条件向导不可见)
            rSlate.AddCondition(HasBlasphemySlate);

            // 3. 添加条件：必须没有牌 (防止误消耗)
            if (cardItemID > 0)
            {
                rSlate.AddCondition(new Condition(
                    Language.GetOrRegister("Mods.zhashi.Conditions.NoCard", () => "未持有对应亵渎之牌"),
                    () => !Main.LocalPlayer.HasItem(cardItemID)
                ));
            }

            // 4. 处理水瓶 (同上)
            if (needsWater)
            {
                rSlate.AddCondition(HasBottledWater);
            }

            // 5. 消耗逻辑：石板 + 水瓶
            rSlate.AddOnCraftCallback((Recipe r, Item item, List<Item> consumedItems, Item destinationStack) => {
                // 扣除石板
                Main.LocalPlayer.ConsumeItem(ModContent.ItemType<BlasphemySlate>());
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Tink, Main.LocalPlayer.position);

                // 扣除水瓶
                if (needsWater)
                {
                    for (int i = 0; i < waterCount; i++) Main.LocalPlayer.ConsumeItem(ItemID.BottledWater);
                }
            });

            rSlate.AddTile(TileID.Bottles);
            rSlate.Register();
        }

        // ==========================================
        // 3. 其他功能 (保持不变)
        // ==========================================
        public override bool CanUseItem(Player player)
        {
            if (Pathway != "None" && RequiredSequence > 0)
            {
                LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();
                if (!CheckSequenceRequirement(modPlayer)) return false;
            }
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (RequiredSequence > 0 && Pathway != "None")
            {
                Player player = Main.LocalPlayer;
                LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();
                string colorHex = CheckSequenceRequirement(modPlayer) ? "00FF00" : "FF0000";
                string statusText = CheckSequenceRequirement(modPlayer) ? "已满足" : "未满足";
                string pathName = Pathway;
                switch (Pathway)
                {
                    case "Fool": pathName = "愚者"; break;
                    case "Giant": pathName = "巨人/战士"; break;
                    case "Hunter": pathName = "猎人"; break;
                    case "Moon": pathName = "月亮"; break;
                    case "Marauder": pathName = "错误"; break;
                    case "Sun": pathName = "太阳"; break;
                    case "Demoness": pathName = "魔女"; break;
                    case "Wheel": pathName = "命运"; break;
                    case "Door": pathName = "学徒/门"; break;
                }
                tooltips.Add(new TooltipLine(Mod, "SequenceReq", $"[c/{colorHex}:需要 {pathName} 途径 序列 {RequiredSequence} ({statusText})]"));
            }
        }

        private bool CheckSequenceRequirement(LotMPlayer p)
        {
            if (Pathway == "Hunter") return p.currentHunterSequence <= RequiredSequence;
            if (Pathway == "Fool") return p.currentFoolSequence <= RequiredSequence;
            if (Pathway == "Giant") return p.currentSequence <= RequiredSequence;
            if (Pathway == "Moon") return p.baseMoonSequence <= RequiredSequence;
            if (Pathway == "Marauder") return p.currentMarauderSequence <= RequiredSequence;
            if (Pathway == "Sun") return p.baseSunSequence <= RequiredSequence;
            if (Pathway == "Demoness") return p.currentDemonessSequence <= RequiredSequence;
            if (Pathway == "Wheel") return p.currentWheelSequence <= RequiredSequence;
            if (Pathway == "Door") return p.currentDoorSequence <= RequiredSequence;
            return false;
        }
    }
}