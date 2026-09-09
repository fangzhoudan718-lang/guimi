using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.ItemDropRules;
using zhashi.Content.Items; // 引用你的物品命名空间

namespace zhashi.Content.Globals
{
    public class BlasphemyGlobalNPC : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (npc.boss)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BlasphemySlate>(), 10));
            }
            if (npc.type == NPCID.RedDevil)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BlasphemySlate>(), 100));
            }
            if (npc.type == NPCID.DungeonSpirit)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BlasphemySlate>(), 50));
            }
        }
    }

    public class BlasphemyGlobalItem : GlobalItem
    {
        public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
        {

            if (ItemID.Sets.IsFishingCrate[item.type])
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BlasphemySlate>(), 100));
            }

            if (ItemID.Sets.BossBag[item.type])
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BlasphemySlate>(), 10));
            }
        }
    }
}