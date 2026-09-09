using Terraria;
using Terraria.DataStructures; // 【核心修复】必须加上这一行！
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Microsoft.Xna.Framework; // 用于 Color
using zhashi.Content.Items.Weapons;

namespace zhashi.Content.Globals
{
    public class CreepingHungerWorldSystem : ModSystem
    {
        public static bool hasDroppedCreepingHunger = false;

        public override void OnWorldLoad()
        {
            hasDroppedCreepingHunger = false;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag["HasDroppedGlove"] = hasDroppedCreepingHunger;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            hasDroppedCreepingHunger = tag.GetBool("HasDroppedGlove");
        }
    }
    public class CreepingHungerItemSource : GlobalItem
    {
        public override void OnSpawn(Item item, IEntitySource source)
        {
            if (source is EntitySource_TileBreak tileSource)
            {
                if (!CreepingHungerWorldSystem.hasDroppedCreepingHunger)
                {
                    bool isOrbDrop = item.type == ItemID.Musket || item.type == ItemID.TheUndertaker ||
                                     item.type == ItemID.ShadowOrb || item.type == ItemID.CrimsonHeart ||
                                     item.type == ItemID.Vilethorn || item.type == ItemID.TheRottedFork;

                    if (isOrbDrop)
                    {
                        if (Main.rand.NextBool(3))
                        {
                            int number = Item.NewItem(source, item.getRect(), ModContent.ItemType<CreepingHunger>());
                            CreepingHungerWorldSystem.hasDroppedCreepingHunger = true;
                            if (Main.netMode != NetmodeID.Server)
                            {
                                CombatText.NewText(item.getRect(), new Color(180, 80, 255), "灵界的馈赠...", true);
                            }
                        }
                    }
                }
            }
        }
    }
}