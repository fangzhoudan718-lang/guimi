using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using SubworldLibrary;

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
                // ==== 生物加强 ====
                npc.lifeMax = (int)(npc.lifeMax * 2.5f); // 血量 2.5 倍
                // ★★★ 新增：由 maxLife 变化后，需要补满当前血量 ★★★
                npc.life = npc.lifeMax;

                npc.damage = (int)(npc.damage * 1.5f);   // 伤害 1.5 倍
                npc.defense = (int)(npc.defense * 1.2f); // 防御 1.2 倍
                npc.knockBackResist *= 0.5f;             // 更难被击退

                // ==== 战利品翻倍 (钱币) ====
                npc.value *= 2;
            }
        }

        // 修改战利品掉落 (更高级的掉落翻倍)
        public override void OnKill(NPC npc)
        {
            // 简单的额外掉落逻辑：
            // 如果你在灵界击杀怪物，有几率额外掉落一些特定的东西
            if (!npc.friendly && !npc.townNPC)
            {
                // 举例：必掉一个金币作为“灵界奖励”
                Item.NewItem(npc.GetSource_Death(), npc.getRect(), ItemID.GoldCoin, 1);
            }
        }
    }
}