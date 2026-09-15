using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using SubworldLibrary;
using zhashi.Content.Items.Materials;

namespace zhashi.Content.Dimensions
{
    public class SpiritWorldGlobalNPC : GlobalNPC
    {
        // 只有在灵界内的怪物才会应用这个类
        public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
        {
            return SubworldSystem.IsActive<SpiritWorld>();
        }

        public override void SetDefaults(NPC npc)
        {
            // 排除城镇NPC和友好生物
            if (!npc.friendly && !npc.townNPC)
            {
                float lifeScale = NPC.downedPlantBoss ? 1.35f : (Main.hardMode ? 1.2f : 1f);
                npc.lifeMax = (int)(npc.lifeMax * lifeScale);
                npc.life = npc.lifeMax;
                npc.damage = (int)(npc.damage * (NPC.downedPlantBoss ? 1.2f : 1f));
                npc.knockBackResist *= 0.8f;
                npc.value = 0f;
            }
        }

        // 修改战利品掉落 (更高级的掉落翻倍)
        public override void OnKill(NPC npc)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && !npc.friendly && !npc.townNPC)
            {
                if (NPC.downedPlantBoss && Main.rand.NextBool(5))
                    Item.NewItem(npc.GetSource_Death(), npc.getRect(), ItemID.Ectoplasm);
                else if (!NPC.downedPlantBoss && Main.rand.NextBool(8))
                    Item.NewItem(npc.GetSource_Death(), npc.getRect(), ItemID.FallenStar);
                if (NPC.downedPlantBoss && ((npc.boss && Main.rand.NextBool(2)) || Main.rand.NextBool(50)))
                    Item.NewItem(npc.GetSource_Death(), npc.getRect(), ModContent.ItemType<WhisperOfSpiritWorld>());
            }
        }
    }
}
