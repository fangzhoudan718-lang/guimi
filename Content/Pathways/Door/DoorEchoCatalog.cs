using Terraria.GameContent;
using Terraria.ID;

namespace zhashi.Content.Pathways.Door
{
    /// <summary>Bounded, friendly actions used by a recorded figure's one-off reenactment.</summary>
    public enum DoorEchoKind
    {
        Strike = 0,
        Volley = 1,
        Lance = 2,
        Pursuit = 3
    }

    /// <summary>
    /// Reenactment preserves a recognizable attack motif, not an NPC's entire AI or power.
    /// The explicit roster follows CreepingHunger.GetProjectileFromNPC_Static and its
    /// special-skill roster, but deliberately never returns their vanilla projectile IDs:
    /// boss projectiles can have hostile side effects or assume their original owner AI.
    /// These four safe actions are a Terraria adaptation of a figure attacking once;
    /// they do not claim to reproduce every recorded creature's full original ability.
    /// </summary>
    public static class DoorEchoCatalog
    {
        public static DoorEchoKind GetKind(int npcType)
        {
            switch (npcType)
            {
                // Spellcasters, directed fire and lasers become one narrow cast.
                case NPCID.FireImp:
                case NPCID.Demon:
                case NPCID.GoblinSorcerer:
                case NPCID.DarkCaster:
                case NPCID.Necromancer:
                case NPCID.NecromancerArmored:
                case NPCID.DiabolistRed:
                case NPCID.DiabolistWhite:
                case NPCID.IceElemental:
                case NPCID.IchorSticker:
                case NPCID.Retinazer:
                case NPCID.Spazmatism:
                case NPCID.CultistBoss:
                case NPCID.Golem:
                case NPCID.RedDevil:
                case NPCID.WallofFlesh:
                case NPCID.Tim:
                case NPCID.MartianSaucer:
                case NPCID.IceGolem:
                    return DoorEchoKind.Lance;

                // Bones, ammunition, feathers, seeds, thrown hammers and falling rain.
                // The volley shares one cast's damage budget rather than multiplying it.
                case NPCID.SkeletonSniper:
                case NPCID.TacticalSkeleton:
                case NPCID.SkeletonCommando:
                case NPCID.SkeletronHead:
                case NPCID.GiantCursedSkull:
                case NPCID.Clown:
                case NPCID.Plantera:
                case NPCID.MourningWood:
                case NPCID.Paladin:
                case NPCID.Harpy:
                case NPCID.AngryNimbus:
                case NPCID.Everscream:
                    return DoorEchoKind.Volley;

                // Homing souls/flames and mobile flying figures retain a pursuit motif.
                case NPCID.RaggedCaster:
                case NPCID.DesertDjinn:
                case NPCID.DukeFishron:
                case NPCID.Wraith:
                case NPCID.BrainofCthulhu:
                case NPCID.QueenBee:
                case NPCID.CaveBat:
                case NPCID.JungleBat:
                case NPCID.Hellbat:
                case NPCID.MeteorHead:
                case NPCID.Drippler:
                case NPCID.EyeofCthulhu:
                case NPCID.WyvernHead:
                case NPCID.Pixie:
                case NPCID.Mothron:
                case NPCID.DemonEye:
                case NPCID.Bee:
                case NPCID.BeeSmall:
                case NPCID.Hornet:
                    return DoorEchoKind.Pursuit;

                // Contact attackers become a short, directed strike. Utility records
                // such as Nurse/Bunny also use this conservative fallback: reenacting
                // a figure must not invoke healing, item drops or NPC spawning scripts.
                case NPCID.Mimic:
                case NPCID.IceMimic:
                case NPCID.ChaosElemental:
                case NPCID.EaterofWorldsHead:
                case NPCID.Nurse:
                case NPCID.Bunny:
                case NPCID.GoldBunny:
                case NPCID.Zombie:
                case NPCID.BaldZombie:
                case NPCID.PincushionZombie:
                case NPCID.SlimedZombie:
                case NPCID.UndeadMiner:
                case NPCID.Medusa:
                case NPCID.Wolf:
                case NPCID.ExplosiveBunny:
                case NPCID.BlueSlime:
                case NPCID.GreenSlime:
                case NPCID.PurpleSlime:
                case NPCID.RedSlime:
                case NPCID.YellowSlime:
                case NPCID.BlackSlime:
                case NPCID.MotherSlime:
                case NPCID.Pinky:
                case NPCID.Shark:
                case NPCID.GiantTortoise:
                case NPCID.Unicorn:
                case NPCID.GraniteGolem:
                case NPCID.DoctorBones:
                case NPCID.Nymph:
                case NPCID.KingSlime:
                case NPCID.TaxCollector:
                    return DoorEchoKind.Strike;
            }

            // Samples are read-only defaults created by tML. Do not instantiate an NPC,
            // run SetDefaults/AI, or mutate this shared object just to classify a record.
            // Unknown mod NPCs get a bounded action from broad movement/contact traits;
            // missing IDs and noncombat samples stay on the least elaborate fallback.
            if (ContentSamples.NpcsByNetId.TryGetValue(npcType, out var sample)
                && sample != null && sample.damage > 0 && sample.noGravity)
                return DoorEchoKind.Pursuit;

            return DoorEchoKind.Strike;
        }

        public static string GetName(int npcType)
        {
            return GetKind(npcType) switch
            {
                DoorEchoKind.Volley => "散射齐射",
                DoorEchoKind.Lance => "定向贯穿",
                DoorEchoKind.Pursuit => "追踪突袭",
                _ => "近身冲击"
            };
        }
    }
}
