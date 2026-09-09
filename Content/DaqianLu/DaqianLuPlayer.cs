using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using zhashi.Content;

namespace zhashi.Content.DaqianLu
{
    public class DaqianLuPlayer : ModPlayer
    {
        // 是否持有大千录 (背包或饰品栏)
        public bool hasDaqianLu = false;

        // 新增：是否戴着铜钱面罩
        public bool isWearingMask = false;

        public int daqianLuCooldown = 0;
        public int painStack = 0;

        public override void ResetEffects()
        {
            hasDaqianLu = false;
            isWearingMask = false; // 每帧重置
        }

        // --- 开局福利逻辑 ---
        public override void ModifyStartingInventory(IReadOnlyDictionary<string, List<Item>> itemsByMod, bool mediumCoreDeath)
        {
            if (Player.name == "李火旺" || Player.name == "LiHuowang")
            {
                List<Item> vanillaItems = itemsByMod["Terraria"];
                Item daqianLu = new Item(ModContent.ItemType<DaqianLuItem>());
                Item mask = new Item(ModContent.ItemType<Items.Armor.CopperCoinMask>());

                vanillaItems.Insert(0, daqianLu);
                Player.armor[3] = mask; // 放入饰品栏第一格
            }
        }

        public override void PostUpdate()
        {
            if (daqianLuCooldown > 0) daqianLuCooldown--;

            // 大千录的特效
            if (hasDaqianLu)
            {
                if (Main.rand.NextBool(20)) Dust.NewDust(Player.position, Player.width, Player.height, DustID.Blood);

                var lotmPlayer = Player.GetModPlayer<LotMPlayer>();
                if (lotmPlayer.sanityCurrent < 20f) Lighting.AddLight(Player.Center, 0.5f, 0f, 0f);
            }
        }

        // 辅助扣血方法
        public void SacrifiseHealth(int amount)
        {
            if (Player.statLife > amount)
            {
                Player.statLife -= amount;
                painStack += amount / 10;
                for (int i = 0; i < 15; i++) Dust.NewDust(Player.position, Player.width, Player.height, DustID.Blood);
            }
        }
    }
}