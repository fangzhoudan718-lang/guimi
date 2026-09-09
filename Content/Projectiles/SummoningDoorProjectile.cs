using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Buffs;
using zhashi.Content.NPCs;

namespace zhashi.Content.Projectiles
{
    public class SummoningDoorProjectile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.MoonlordTurret;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 180;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.scale = 0.1f;
        }

        public override void AI()
        {
            // 贴图动画
            if (++Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }
            }

            // 淡入与缩放
            if (Projectile.alpha > 0) Projectile.alpha -= 10;
            Projectile.scale = MathHelper.Lerp(Projectile.scale, 1.5f, 0.1f);
            Projectile.rotation += 0.02f;

            // 氛围粒子
            if (Main.rand.NextBool(3))
            {
                Vector2 dustPos = Projectile.Center + Main.rand.NextVector2Circular(40, 40);
                int dustID = Main.rand.NextBool() ? DustID.BlueCrystalShard : DustID.DungeonSpirit;
                Dust d = Dust.NewDustPerfect(dustPos, dustID, Vector2.Zero, 150, default, 1.5f);
                d.noGravity = true;
                d.velocity *= 0.5f;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                SpawnSummon();
            }
        }

        private void SpawnSummon()
        {
            Vector2 spawnPos = Projectile.Center;
            SoundEngine.PlaySound(SoundID.Item119, spawnPos);

            int roll = Main.rand.Next(100);
            int npcId = -1;
            bool isHostile = false; // 标记是否为敌对生物

            // =================================================
            // 1. 神话级 (1% 概率) - 召唤失败，BOSS 降临 (敌对)
            // =================================================
            if (roll < 1)
            {
                Main.NewText("【警告】召唤仪式失控！古老的存在被激怒了！", 255, 0, 0); // 红色警告
                SoundEngine.PlaySound(SoundID.Roar, spawnPos);

                // 随机选择 Boss
                int[] godTier = { NPCID.MoonLordCore, NPCID.DukeFishron, NPCID.HallowBoss };
                int type = godTier[Main.rand.Next(godTier.Length)];

                npcId = NPC.NewNPC(Projectile.GetSource_FromThis(), (int)spawnPos.X, (int)spawnPos.Y, type);

                // 标记为敌对
                isHostile = true;

                // 特效
                for (int i = 0; i < 100; i++)
                    Dust.NewDust(spawnPos, 100, 100, DustID.LunarOre, 0, 0, 0, default, 3f);
            }
            // =================================================
            // 2. 灾厄级 (4% 概率) - 召唤异变，强怪降临 (敌对)
            // =================================================
            else if (roll < 5)
            {
                Main.NewText("警告：灵界生物冲破了束缚！", 255, 100, 100);
                SoundEngine.PlaySound(SoundID.ForceRoar, spawnPos);

                // 南瓜王、冰雪女王、蛾怪、火星飞碟
                int[] disasterTier = { NPCID.Pumpking, NPCID.IceQueen, NPCID.Mothron, NPCID.MartianSaucer };
                int type = disasterTier[Main.rand.Next(disasterTier.Length)];

                npcId = NPC.NewNPC(Projectile.GetSource_FromThis(), (int)spawnPos.X, (int)spawnPos.Y, type);

                // 标记为敌对
                isHostile = true;

                for (int i = 0; i < 50; i++)
                    Dust.NewDust(spawnPos, 100, 100, DustID.Blood, 0, 0, 0, default, 2f);
            }
            // =================================================
            // 3. 传说级 (15% 概率) - 契约达成 (友好 - 驯服)
            // =================================================
            else if (roll < 20)
            {
                Main.NewText("契约达成：召唤了高位灵界生物。", 255, 215, 0);

                int[] eliteTier = {
                    NPCID.WyvernHead, NPCID.SolarCrawltipedeHead, NPCID.NebulaBeast,
                    NPCID.VortexHornetQueen, NPCID.DeadlySphere, NPCID.Nailhead
                };
                int type = eliteTier[Main.rand.Next(eliteTier.Length)];

                npcId = NPC.NewNPC(Projectile.GetSource_FromThis(), (int)spawnPos.X, (int)spawnPos.Y, type);

                if (npcId != Main.maxNPCs)
                {
                    NPC summon = Main.npc[npcId];
                    // 3倍属性
                    summon.damage = (int)(summon.damage * 3f);
                    summon.lifeMax = (int)(summon.lifeMax * 3f);
                    summon.life = summon.lifeMax;
                    summon.defense += 60;

                    // 只有这里加 Buff
                    summon.AddBuff(ModContent.BuffType<TamedBuff>(), 36000);
                }
            }
            // =================================================
            // 4. 史诗级 (80% 概率) - 召唤成功 (友好 - 驯服)
            // =================================================
            else
            {
                Main.NewText("召唤成功。", 150, 150, 255);

                int[] normalTier = {
                    NPCID.Paladin, NPCID.RedDevil, NPCID.Necromancer, NPCID.GiantTortoise,
                    NPCID.DiabolistRed, NPCID.SkeletonSniper, NPCID.SkeletonCommando, NPCID.BrainScrambler
                };
                int type = normalTier[Main.rand.Next(normalTier.Length)];

                npcId = NPC.NewNPC(Projectile.GetSource_FromThis(), (int)spawnPos.X, (int)spawnPos.Y, type);

                if (npcId != Main.maxNPCs)
                {
                    NPC summon = Main.npc[npcId];
                    // 1.5倍属性
                    summon.damage = (int)(summon.damage * 1.5f);
                    summon.lifeMax = (int)(summon.lifeMax * 1.5f);
                    summon.life = summon.lifeMax;

                    // 只有这里加 Buff
                    summon.AddBuff(ModContent.BuffType<TamedBuff>(), 18000);
                }
            }

            // ==========================================================
            // 后处理
            // ==========================================================
            if (npcId != -1 && npcId != Main.maxNPCs)
            {
                NPC summon = Main.npc[npcId];

                // 如果是【敌对】生物（召唤失败）
                if (isHostile)
                {
                    // 确保它是敌对的
                    summon.friendly = false;
                    // 并且立刻把目标锁定为召唤者，防止它发呆
                    summon.target = Projectile.owner;
                }
                // 如果是【友好】生物（召唤成功）
                else
                {
                    // 确保它是友好的
                    summon.friendly = true;

                    // 绑定主人索引 (用于随从跟随逻辑)
                    if (summon.TryGetGlobalNPC(out TamingGlobalNPC globalNPC))
                    {
                        globalNPC.ownerIndex = Projectile.owner;
                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npcId);
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[ProjectileID.MoonlordTurret].Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle sourceRect = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = sourceRect.Size() / 2f;

            Main.EntitySpriteDraw(texture, drawPosition, sourceRect, Color.White * 0.9f, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, drawPosition, sourceRect, new Color(100, 255, 255, 0) * 0.5f, Projectile.rotation, origin, Projectile.scale * 1.1f, SpriteEffects.None, 0);

            return false;
        }
    }
}