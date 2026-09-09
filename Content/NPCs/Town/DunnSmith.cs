using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Bestiary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics; // 必须引用这个才能画图
using zhashi.Content.UI;
using zhashi.Content.Items.Weapons;
using zhashi.Content.Projectiles.Ammo;

namespace zhashi.Content.NPCs.Town
{
    [AutoloadHead]
    public class DunnSmith : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 25;
            NPCID.Sets.ExtraFramesCount[NPC.type] = 9;
            NPCID.Sets.AttackFrameCount[NPC.type] = 4;
            NPCID.Sets.DangerDetectRange[NPC.type] = 700;

            // 必须设为 1 (射击模式)，保证他会做开枪动作
            NPCID.Sets.AttackType[NPC.type] = 1;

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
            NPC.aiStyle = 7;
            NPC.damage = 48;
            NPC.defense = 15;
            NPC.lifeMax = 250;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.5f;
            AnimationType = NPCID.Guide;
        }

        public override bool CanTownNPCSpawn(int numTownNPCs)
        {
            return true;
        }

        public override string GetChat()
        {
            return "有什么事吗？";
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
        // 攻击逻辑 (功能)
        // ======================================================

        public override void TownNPCAttackStrength(ref int damage, ref float knockback)
        {
            damage = 48;
            knockback = 4f;
        }

        public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
        {
            cooldown = 45;
            randExtraCooldown = 15;
        }

        public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
        {
            projType = ModContent.ProjectileType<ExorcismBulletProjectile>();
            attackDelay = 1;
        }

        public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset)
        {
            multiplier = 14f;
            randomOffset = 0.2f;
        }

        public override void TownNPCAttackShoot(ref bool inBetweenShots)
        {
            // 射击间隔
        }

        // ======================================================
        // ★★★ 视觉部分：暴力强制绘制 (PostDraw) ★★★
        // ======================================================

        // 我们删除了 DrawTownAttackGun，改用 PostDraw 自己画！
        // 这样就绝对不会报错了，而且一定能看到枪。
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // 1. 获取当前帧索引
            int frameHeight = NPC.frame.Height;
            // 防止除以0错误，虽然一般不会发生
            if (frameHeight == 0) return;

            int currentFrame = NPC.frame.Y / frameHeight;

            // 2. 判断是否处于攻击动作
            // 向导AI (Guide) 的攻击帧通常是最后几帧 (20~24)
            if (currentFrame >= 21 && currentFrame <= 24)
            {
                // 加载枪的贴图
                Texture2D texture = ModContent.Request<Texture2D>("zhashi/Content/Items/Weapons/NighthawkPistol").Value;

                // 计算位置：基于NPC中心
                // offsetX: 向前伸出的距离 (根据朝向)
                float offsetX = 14f * NPC.spriteDirection;
                // offsetY: 高度微调 (正数向下)
                float offsetY = 2f;

                Vector2 drawPos = NPC.Center + new Vector2(offsetX, offsetY) - screenPos;
                drawPos.Y += NPC.gfxOffY; // 加上这个防止NPC上下跳动时枪不跟着动

                // 处理翻转 (如果NPC朝左，枪也要翻转)
                SpriteEffects effects = NPC.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                // 贴图中心点
                Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);

                // 画出来！
                spriteBatch.Draw(texture, drawPos, null, drawColor, 0f, origin, 0.8f, effects, 0f);
            }
        }
    }
}