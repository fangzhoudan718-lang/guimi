using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Bestiary;
using zhashi.Content.UI; // 引用UI系统

namespace zhashi.Content.NPCs.Town
{
    [AutoloadHead]
    public class OldNeil : ModNPC
    {
        public override void SetStaticDefaults()
        {
            // 税收官有25帧
            Main.npcFrameCount[NPC.type] = 25;

            NPCID.Sets.ExtraFramesCount[NPC.type] = 9;
            NPCID.Sets.AttackFrameCount[NPC.type] = 4;
            NPCID.Sets.DangerDetectRange[NPC.type] = 700;

            // 攻击方式：2 = 魔法 (老尼尔是窥秘人，用魔法攻击很合适)
            // 或者你可以暂时设为 0 (挥舞手杖) 如果你想让他用物理攻击
            NPCID.Sets.AttackType[NPC.type] = 2;

            NPCID.Sets.AttackTime[NPC.type] = 90;
            NPCID.Sets.AttackAverageChance[NPC.type] = 30;
            NPCID.Sets.HatOffsetY[NPC.type] = 4;
        }

        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.friendly = true;
            NPC.width = 18;
            NPC.height = 40;
            NPC.aiStyle = 7; // 城镇NPC AI
            NPC.damage = 10;
            NPC.defense = 15;
            NPC.lifeMax = 250;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.5f;

            // ★★★ 核心：模仿税收官的动作 ★★★
            // 只要你的贴图帧数顺序和税收官一样，他就会驼背走路
            AnimationType = NPCID.TaxCollector;
        }

        public override bool CanTownNPCSpawn(int numTownNPCs)
        {
            // 这里可以写：如果 StoryStage >= 2 且 邓恩在场，则出现
            // 暂时先返回 true 方便调试
            return true;
        }

        public override string GetChat()
        {
            return "有什么事吗？我现在很忙，还有一堆报销单要填。";
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = "交谈";
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            if (firstButton)
            {
                ModContent.GetInstance<GalgameUISystem>().OpenUI();
            }
        }

        // ======================================================
        // 攻击逻辑 (老尼尔使用魔法)
        // ======================================================

        public override void TownNPCAttackStrength(ref int damage, ref float knockback)
        {
            damage = 20;
            knockback = 4f;
        }

        public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
        {
            cooldown = 30;
            randExtraCooldown = 30;
        }

        public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
        {
            // 暂时用 宝石法杖的弹幕，以后可以换成自定义的“窥秘之眼”或者“魔法飞弹”
            projType = ProjectileID.DiamondBolt;
            attackDelay = 1;
        }

        public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset)
        {
            multiplier = 12f;
            randomOffset = 2f;
        }

        // 魔法攻击不需要 DrawTownAttackGun
        public override void TownNPCAttackMagic(ref float auraLightMultiplier)
        {
            auraLightMultiplier = 1f;
        }
    }
}