using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Projectiles;

namespace zhashi.Content.NPCs.Bosses.Aurmir
{
    // 【关键修复 1】必须删除或注释掉这一行！防止它去乱找图片
    // [AutoloadBossHead] 
    public class Aurmir : ModNPC
    {
        // 使用月球领主核心贴图
        public override string Texture => "Terraria/Images/NPC_398";

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1; // 月球领主只有1帧(3个部件分别绘制，这里核心算1帧)

            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.TrailCacheLength[NPC.type] = 6;
            NPCID.Sets.TrailingMode[NPC.type] = 0;

            // 【关键修复 2】手动指定地图头像为原版月球领主 (ID: 39)
            NPCID.Sets.BossHeadTextures[Type] = 39;
        }

        public override void SetDefaults()
        {
            NPC.width = 160;
            NPC.height = 220;

            NPC.damage = 200;
            NPC.defense = 100;
            NPC.lifeMax = 300000;
            NPC.HitSound = SoundID.NPCHit41;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.value = Item.buyPrice(platinum: 30);
            NPC.boss = true;
            NPC.npcSlots = 50f;
            NPC.timeLeft = NPC.activeTime * 30;
        }

        // === 核心绘制：染成橘色 ===
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = TextureAssets.Npc[NPC.type].Value;
            Vector2 origin = new Vector2(texture.Width / 2, texture.Height / 2);

            // 定义黄昏颜色 (橘色)
            Color twilightColor = new Color(255, 140, 0, 255);

            // 1. 绘制残影
            if (NPC.velocity.Length() > 5f || NPC.ai[2] == 2)
            {
                for (int i = 0; i < NPC.oldPos.Length; i += 2)
                {
                    Vector2 drawPos = NPC.oldPos[i] - screenPos + origin + new Vector2(0, NPC.gfxOffY);
                    Color trailColor = twilightColor * ((float)(NPC.oldPos.Length - i) / NPC.oldPos.Length) * 0.5f;
                    spriteBatch.Draw(texture, drawPos, null, trailColor, NPC.rotation, origin, NPC.scale, NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
                }
            }

            // 2. 绘制本体
            spriteBatch.Draw(texture, NPC.Center - screenPos, null, twilightColor, NPC.rotation, origin, NPC.scale, NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);

            return false;
        }

        // === 动画控制 ===
        public override void FindFrame(int frameHeight)
        {
            NPC.frame.Y = 0;
            NPC.spriteDirection = NPC.direction;
        }

        // === AI 逻辑 ===
        public override void AI()
        {
            if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
                NPC.TargetClosest(true);
            Player player = Main.player[NPC.target];

            // 处决时不脱战
            if (player.dead && NPC.ai[0] != 5) { NPC.velocity.Y -= 0.5f; NPC.EncourageDespawn(10); return; }

            if (NPC.ai[0] != 5) CheckPhase(player);

            switch ((int)NPC.ai[0])
            {
                case 0: AI_Hover(player); break;
                case 1: AI_ShootOrbs(player); break;
                case 2: AI_SkyRain(player); break;
                case 3: AI_TwilightSlash(player); break;
                case 4: AI_TwilightRage(player); break;
                case 5: AI_Punishment(player); break;
            }

            Lighting.AddLight(NPC.Center, 1.5f, 0.8f, 0f);
        }

        // 神罚触发器
        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (projectile.type == ModContent.ProjectileType<TwilightStormProjectile>())
            {
                if (NPC.ai[0] != 5)
                {
                    modifiers.FinalDamage *= 0;
                    NPC.ai[0] = 5; NPC.ai[1] = 0; NPC.dontTakeDamage = true;
                    SoundEngine.PlaySound(SoundID.Item29, NPC.position);
                }
            }
        }

        // 阶段检测
        private void CheckPhase(Player player)
        {
            if (NPC.ai[2] == 0 && NPC.life < NPC.lifeMax * 0.6f) { NPC.ai[2] = 1; NPC.ai[0] = 0; NPC.ai[1] = 0; SoundEngine.PlaySound(SoundID.Roar, NPC.position); Main.NewText("奥尔米尔举起了晨曦之剑！", 255, 215, 0); }
            else if (NPC.ai[2] == 1 && NPC.life < NPC.lifeMax * 0.2f) { NPC.ai[2] = 2; NPC.ai[0] = 4; NPC.ai[1] = 0; SoundEngine.PlaySound(SoundID.ForceRoar, NPC.position); Main.NewText("黄昏已至，万物衰败……", 255, 69, 0); }
        }

        // 神罚 AI
        private void AI_Punishment(Player player)
        {
            NPC.ai[1]++;
            if (NPC.ai[1] == 1)
            {
                for (int i = 0; i < Main.maxProjectiles; i++) { Projectile p = Main.projectile[i]; if (p.active && p.type == ModContent.ProjectileType<TwilightStormProjectile>() && p.owner == player.whoAmI) { p.Kill(); for (int j = 0; j < 10; j++) Dust.NewDust(p.position, p.width, p.height, DustID.Smoke, 0, 0, 0, default, 2f); } }
                if (Main.netMode != NetmodeID.Server) { Main.NewText("奥尔米尔：就凭这也想窃取黄昏的权柄？", 255, 120, 0); Main.NewText("奥尔米尔：凡人，让你见识一下什么才是真正的神性！", 255, 0, 0); }
            }
            if (NPC.ai[1] < 90) { Vector2 targetPos = player.Center - new Vector2(0, 800); Vector2 flyDir = targetPos - NPC.Center; if (flyDir.Length() > 20f) { flyDir.Normalize(); NPC.velocity = flyDir * 40f; } else { NPC.velocity = Vector2.Zero; NPC.Center = targetPos; } NPC.dontTakeDamage = true; }
            else if (NPC.ai[1] < 150) { NPC.velocity = Vector2.Zero; if (NPC.ai[1] == 90) SoundEngine.PlaySound(SoundID.ForceRoar, NPC.position); if (Main.rand.NextBool(2)) { Vector2 pos = NPC.Center + Main.rand.NextVector2Circular(200, 200); Dust d = Dust.NewDustPerfect(pos, DustID.OrangeTorch, (NPC.Center - pos) * 0.1f, 0, default, 3f); d.noGravity = true; } }
            else if (NPC.ai[1] == 150) { Projectile.NewProjectile(NPC.GetSource_FromAI(), player.Center, Vector2.Zero, ModContent.ProjectileType<AurmirBossStorm>(), 0, 0f, Main.myPlayer); SoundEngine.PlaySound(SoundID.Item119, player.position); player.KillMe(PlayerDeathReason.ByCustomReason(player.name + " 因僭越神权而被黄昏吞噬。"), 999999, 0); }
            else if (NPC.ai[1] > 200) { NPC.velocity.Y -= 0.5f; NPC.EncourageDespawn(10); NPC.dontTakeDamage = false; }
        }

        // 0. 悬停
        private void AI_Hover(Player player)
        {
            Vector2 targetPos = player.Center - new Vector2(0, 350); float speed = (NPC.ai[2] >= 1) ? 12f : 8f; Vector2 dir = (targetPos - NPC.Center); if (dir.Length() > speed) dir = dir.SafeNormalize(Vector2.Zero) * speed; NPC.velocity = (NPC.velocity * 20f + dir) / 21f;
            NPC.ai[1]++; int cooldown = 60; if (NPC.ai[1] > cooldown) { NPC.ai[1] = 0; int rand = Main.rand.Next(100); if (NPC.ai[2] == 0) { if (rand < 70) NPC.ai[0] = 1; else NPC.ai[0] = 2; } else { if (rand < 40) NPC.ai[0] = 1; else if (rand < 70) NPC.ai[0] = 2; else NPC.ai[0] = 3; } }
        }

        // 1. 连发法球
        private void AI_ShootOrbs(Player player)
        {
            NPC.velocity *= 0.9f; NPC.ai[1]++; NPC.rotation = (player.Center - NPC.Center).X * 0.02f;
            if (NPC.ai[1] % 5 == 0) { SoundEngine.PlaySound(SoundID.Item20, NPC.position); if (Main.netMode != NetmodeID.MultiplayerClient) { Vector2 dir = (player.Center - NPC.Center).SafeNormalize(Vector2.Zero).RotatedByRandom(MathHelper.ToRadians(20)); dir *= 12f; Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, dir, ModContent.ProjectileType<AurmirTwilightOrb>(), 70, 0f, Main.myPlayer, player.whoAmI); } }
            if (NPC.ai[1] > 90) { NPC.ai[0] = 0; NPC.ai[1] = 0; }
        }

        // 2. 光雨
        private void AI_SkyRain(Player player)
        {
            NPC.ai[1]++; if (NPC.ai[1] % 3 == 0) { SoundEngine.PlaySound(SoundID.Item12, NPC.position); if (Main.netMode != NetmodeID.MultiplayerClient) { Vector2 spawnPos = player.Center + new Vector2(Main.rand.Next(-1000, 1000), -800); Vector2 vel = new Vector2(0, 25f); Projectile.NewProjectile(NPC.GetSource_FromAI(), spawnPos, vel, ModContent.ProjectileType<AurmirSkyLance>(), 80, 0f, Main.myPlayer); } }
            if (NPC.ai[1] > 120) { NPC.ai[0] = 0; NPC.ai[1] = 0; }
        }

        // 3. 瞬移斩
        private void AI_TwilightSlash(Player player)
        {
            NPC.ai[1]++; if (NPC.ai[1] == 1) { SoundEngine.PlaySound(SoundID.Item6, NPC.position); for (int i = 0; i < 30; i++) Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GoldFlame, 0, 0, 0, default, 2f); Vector2 offset = new Vector2(Main.rand.NextBool() ? -500 : 500, -300); NPC.Center = player.Center + offset; NPC.velocity = Vector2.Zero; NPC.alpha = 255; }
            if (NPC.ai[1] == 30) { NPC.alpha = 0; SoundEngine.PlaySound(SoundID.Item71, NPC.position); if (Main.netMode != NetmodeID.MultiplayerClient) { Vector2 dir = (player.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 15f; Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, dir, ModContent.ProjectileType<AurmirBossStorm>(), 100, 0f, Main.myPlayer); Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, dir.RotatedBy(0.4), ModContent.ProjectileType<AurmirBossStorm>(), 100, 0f, Main.myPlayer); Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, dir.RotatedBy(-0.4), ModContent.ProjectileType<AurmirBossStorm>(), 100, 0f, Main.myPlayer); } }
            if (NPC.ai[1] > 60) { NPC.ai[0] = 0; NPC.ai[1] = 0; }
        }

        // 4. 狂暴
        private void AI_TwilightRage(Player player)
        {
            NPC.ai[1]++; Vector2 targetPos = player.Center + new Vector2(0, -400); Vector2 flyDir = (targetPos - NPC.Center).SafeNormalize(Vector2.Zero); NPC.velocity = (NPC.velocity * 20f + flyDir * 20f) / 21f;
            if (NPC.ai[1] % 15 == 0) { if (Main.netMode != NetmodeID.MultiplayerClient) { Vector2 spawnPos = player.Center + new Vector2(Main.rand.Next(-800, 800), -800); Projectile.NewProjectile(NPC.GetSource_FromAI(), spawnPos, new Vector2(0, 30f), ModContent.ProjectileType<AurmirSkyLance>(), 80, 0f, Main.myPlayer); } }
            if (NPC.ai[1] % 30 == 0) { SoundEngine.PlaySound(SoundID.Item20, NPC.position); if (Main.netMode != NetmodeID.MultiplayerClient) { Vector2 dir = (player.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 10f; for (int i = 0; i < 3; i++) { Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, dir.RotatedBy(MathHelper.ToRadians(i * 120 + NPC.ai[1])), ModContent.ProjectileType<AurmirTwilightOrb>(), 70, 0f, Main.myPlayer, player.whoAmI); } } }
            if (player.Distance(NPC.Center) < 500f) { player.AddBuff(BuffID.WitheredArmor, 2); player.AddBuff(BuffID.Obstructed, 2); if (NPC.ai[1] % 30 == 0) player.Hurt(PlayerDeathReason.ByCustomReason("衰败"), 5, 0); }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) { npcLoot.Add(ItemDropRule.Common(ItemID.LunarBar, 1, 20, 30)); }
    }
}