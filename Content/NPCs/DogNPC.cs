using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.GameContent;
using Terraria.Audio;
using zhashi.Content.Items;
using zhashi.Content.UI;
using zhashi.Content.Projectiles;
using zhashi.Content.Projectiles.Weapons.Fool;
using zhashi.Content.Items.Weapons.Fool;
using zhashi.Content; // 这一行是为了能读取 LotMPlayer

// 引入所有魔药命名空间
using zhashi.Content.Items.Potions;
using zhashi.Content.Items.Potions.Moon;
using zhashi.Content.Items.Potions.Hunter;
using zhashi.Content.Items.Potions.Fool;
using zhashi.Content.Items.Potions.Marauder;
using zhashi.Content.Items.Potions.Sun; // 引用太阳魔药
using zhashi.Content.Projectiles.Sun;   // 引用太阳弹幕

namespace zhashi.Content.NPCs
{
    public class DogNPC : ModNPC
    {
        public string MyName = "旺财";
        public int BonusMaxHP = 0;
        public Item[] DogInventory = new Item[3];

        public int CalculatedRegen = 0;
        public float CalculatedAttackSpeed = 1.0f;

        public string OwnerName = "";
        public bool isStaying = false;
        public int attackState = 0;
        public int stateTimer = 0;
        public int attackCooldown = 0;

        private int idleTimer = 0;
        private int regenTimer = 0;

        // 非凡系统变量
        public int currentPathway = 0;
        public int currentSequence = 10;
        public int skillTimer = 0;
        private int inAirTimer = 0;

        // 初始化标记
        public bool isInitialized = false;

        private const int FRAME_HEIGHT = 40;
        private const int CUT_TOP = 0;
        private const int CUT_BOTTOM = 0;
        private const int DASH_TIME_LIMIT = 40;
        private const float DASH_SPEED = 24f;

        // 基础冷却时间 18 (约0.3秒)，攻击频率较快
        private const int BASE_COOLDOWN = 18;

        private static readonly SoundStyle DogBarkSound = new SoundStyle("Terraria/Sounds/Zombie_50")
        {
            Volume = 0.7f,
            PitchVariance = 0.1f,
            MaxInstances = 3
        };

        public override void SetStaticDefaults() { Main.npcFrameCount[NPC.type] = 13; }

        public override void SetDefaults()
        {
            NPC.width = 30; NPC.height = 30;
            NPC.damage = 60; NPC.defense = 20; NPC.lifeMax = 300;
            NPC.HitSound = SoundID.NPCHit6; NPC.DeathSound = SoundID.NPCDeath1;
            NPC.friendly = true; NPC.townNPC = true; NPC.aiStyle = -1; // 自定义AI
            NPC.CheckActive(); NPC.knockBackResist = 0f;

            if (DogInventory == null || DogInventory.Length < 3)
            {
                DogInventory = new Item[3];
                for (int i = 0; i < 3; i++) { DogInventory[i] = new Item(); DogInventory[i].SetDefaults(0); }
            }
        }

        // ==========================================
        // 核心修复：死亡时最后一次同步数据到物品
        // ==========================================
        public override void OnKill()
        {
            Player owner = null;
            if (!string.IsNullOrEmpty(OwnerName))
            {
                foreach (var p in Main.player) { if (p.active && p.name == OwnerName) { owner = p; break; } }
            }
            if (owner == null) owner = Main.LocalPlayer;

            // 死前强制同步，防止数据丢失
            if (owner != null && owner.active) SyncDataToItem(owner);
        }

        public override void AI()
        {
            NPC.timeLeft = 2;
            NPC.GivenName = MyName;
            Player owner = Main.LocalPlayer;

            // 1. 实时数据备份
            if (Main.time % 60 == 0 && owner.active && !owner.dead)
            {
                SyncDataToItem(owner);
            }

            bool isMyDog = string.IsNullOrEmpty(OwnerName) || OwnerName == owner.name;
            if (!isMyDog)
            {
                NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, 0f, 0.1f);
                NPC.noGravity = false; NPC.noTileCollide = false;
                attackState = 0;
                return;
            }



            // 防掉出世界
            if (NPC.position.Y > (Main.maxTilesY - 50) * 16)
            {
                NPC.Center = owner.Center - new Vector2(0, 50);
                NPC.velocity = Vector2.Zero;
                attackState = 0;
            }

            UpdateStats();

            // 生命恢复
            if (CalculatedRegen > 0 && NPC.life < NPC.lifeMax)
            {
                regenTimer++;
                int rate = 60 / CalculatedRegen;
                if (rate < 1) rate = 1;
                if (regenTimer >= rate) { NPC.life++; regenTimer = 0; if (Main.rand.NextBool(rate * 2)) CombatText.NewText(NPC.getRect(), CombatText.HealLife, "+1"); }
            }

            // 停留逻辑
            if (isStaying)
            {
                // 1. 强制停止移动 (且防止被推走)
                attackState = 0;
                NPC.velocity = Vector2.Zero;
                NPC.position = NPC.oldPosition;

                // 2. 执行炮台 AI (仅在开启序列后生效，否则只是发呆)
                if (currentSequence < 10)
                {
                    PerformTurretAI(owner);
                }

                return; // 阻止后续的跟随/移动逻辑
            }

            // 距离过远传送
            if (NPC.Distance(owner.Center) > 1500f) { attackState = 0; NPC.Center = owner.Center - new Vector2(0, 50); NPC.velocity = Vector2.Zero; for (int i = 0; i < 20; i++) Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, 0, 0, 0, default, 1.5f); return; }

            if (currentPathway == 5 && currentSequence <= 1)
            {
                bool clockExists = false;
                // 遍历寻找属于我的时钟
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<DogTimeClock>() && (int)p.ai[0] == NPC.whoAmI)
                    {
                        clockExists = true;
                        break;
                    }
                }
                // 如果没找到，就补一个 (只生一次，永不消失)
                if (!clockExists)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<DogTimeClock>(), NPC.damage, 0f, owner.whoAmI, NPC.whoAmI);
                }
            }

            UpdateSunInferno();

            // 核心移动与战斗

            PerformCombatAndMovement(owner);
        }

        // ==========================================
        // 数据同步
        // ==========================================
        private void SyncDataToItem(Player player)
        {
            for (int i = 0; i < 58; i++)
            {
                Item item = player.inventory[i];
                if (item.type == ModContent.ItemType<DogItem>() && item.ModItem is DogItem dogItem)
                {
                    dogItem.DogName = MyName;
                    dogItem.DogPathway = currentPathway;
                    dogItem.DogSequence = currentSequence;
                    dogItem.BonusMaxLife = BonusMaxHP;

                    if (dogItem.Equipment == null) dogItem.Equipment = new Item[3];
                    for (int j = 0; j < 3; j++)
                    {
                        if (DogInventory[j] != null) dogItem.Equipment[j] = DogInventory[j].Clone();
                        else dogItem.Equipment[j] = new Item();
                    }
                    break; // 找到一个就够了
                }
            }
        }

        private void UpdateStats()
        {
            int baseDefense = 20; int baseDamage = 50; int regenRate = 0; float attackSpeed = 1.0f;

            if (currentSequence < 10) { int seqBoost = (10 - currentSequence) * 8; baseDamage += seqBoost * 2; baseDefense += seqBoost; NPC.lifeMax = 300 + BonusMaxHP + (seqBoost * 30); }

            for (int i = 0; i < 3; i++)
            {
                Item item = DogInventory[i];
                if (item != null && !item.IsAir)
                {
                    baseDefense += item.defense;
                    if (item.type == ItemID.FeralClaws) attackSpeed += 0.12f;
                    if (item.type == ItemID.PowerGlove) attackSpeed += 0.12f;
                    if (item.type == ItemID.MechanicalGlove) { attackSpeed += 0.12f; baseDamage += 10; }
                    if (item.type == ItemID.FireGauntlet) { attackSpeed += 0.12f; baseDamage += 10; }
                    if (item.type == ItemID.SunStone || item.type == ItemID.MoonStone || item.type == ItemID.CelestialStone || item.type == ItemID.CelestialShell) attackSpeed += 0.1f;
                    if (item.type == ItemID.BandofRegeneration) regenRate += 2;
                    if (item.type == ItemID.CharmofMyths) regenRate += 4;
                    if (item.type == ItemID.ShinyStone && Math.Abs(NPC.velocity.X) < 0.1f) regenRate += 10;
                    if (item.type == ItemID.WarriorEmblem) baseDamage += 10;
                    if (item.type == ItemID.AvengerEmblem) baseDamage += 15;
                    if (item.type == ItemID.DestroyerEmblem) baseDamage += 15;
                }
            }
            NPC.defDefense = baseDefense; NPC.damage = baseDamage; CalculatedRegen = regenRate; CalculatedAttackSpeed = attackSpeed;
        }
        private float GetAttackRange()
        {
            float baseRange = 1000f; // 基础范围 (序列9及无序列)

            // 每提升一级序列，范围增加 150 像素
            // 序列 9: 1150
            // 序列 1: 2350 (超远距离)
            if (currentSequence < 10)
            {
                baseRange += (10 - currentSequence) * 150f;
            }

            return baseRange;
        }

        private void PerformCombatAndMovement(Player owner)
        {
            // 冲刺状态逻辑
            if (attackState > 0)
            {
                stateTimer--; NPC.noGravity = true; NPC.noTileCollide = true; NPC.dontTakeDamage = true;
                if (attackState == 1) // 冲
                {
                    int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Cloud, -NPC.velocity.X * 0.5f, -NPC.velocity.Y * 0.5f, 0, default, 1.2f); Main.dust[d].noGravity = true;
                    Rectangle myRect = NPC.getRect(); myRect.Inflate(15, 15);
                    foreach (NPC target in Main.ActiveNPCs) { if (target.CanBeChasedBy() && target.getRect().Intersects(myRect)) { NPC.HitInfo hit = new NPC.HitInfo(); hit.Damage = NPC.damage; hit.Knockback = 5f; hit.HitDirection = NPC.direction; target.StrikeNPC(hit); TryStealFromNPC(target, owner); stateTimer = 0; } }
                    if (stateTimer <= 0) { attackState = 2; stateTimer = DASH_TIME_LIMIT; Vector2 backDir = (owner.Center - NPC.Center).SafeNormalize(Vector2.Zero); NPC.velocity = backDir * DASH_SPEED; NPC.netUpdate = true; }
                    return;
                }
                if (attackState == 2) // 返
                {
                    int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, -NPC.velocity.X * 0.5f, -NPC.velocity.Y * 0.5f, 0, default, 1.2f); Main.dust[d].noGravity = true;
                    float distToOwner = NPC.Distance(owner.Center);
                    if (distToOwner < 100f || stateTimer <= 0) { attackState = 0; NPC.velocity *= 0.2f; NPC.noGravity = false; NPC.noTileCollide = false; NPC.dontTakeDamage = false; NPC.netUpdate = true; }
                    else { Vector2 backDir = (owner.Center - NPC.Center).SafeNormalize(Vector2.Zero); NPC.velocity = Vector2.Lerp(NPC.velocity, backDir * DASH_SPEED, 0.2f); }
                    return;
                }
            }

            // 正常状态
            NPC.noGravity = false; NPC.noTileCollide = false; NPC.dontTakeDamage = false;
            bool stuckInWall = Collision.SolidCollision(NPC.position, NPC.width, NPC.height);
            if (attackCooldown > 0) attackCooldown--;

            float range = GetAttackRange();
            NPC targetEnemy = FindTarget(range, false);

            Vector2 targetPos = owner.Center; bool chasingEnemy = false;
            if (targetEnemy != null)
            {
                float distToTarget = NPC.Distance(targetEnemy.Center);

                // 【修改重点 1】距离近 (小于 200) -> 强制近战冲撞
                if (distToTarget < 200f)
                {
                    if (attackCooldown <= 0)
                    {
                        StartAttack(targetEnemy, CalculatedAttackSpeed);
                        return; // 冲撞时不再执行后续移动
                    }
                }
                // 【修改重点 2】距离适中 (200 到 800) -> 尝试释放技能
                else if (distToTarget < 800f)
                {
                    // 在这里调用技能，实现“远了就丢技能”的效果
                    HandleDogSkills(owner);
                }

                // 保持原有的追击移动逻辑
                bool canReach = Collision.CanHit(NPC.position, NPC.width, NPC.height, targetEnemy.position, targetEnemy.width, targetEnemy.height);
                if (distToTarget < 800f && (canReach || stuckInWall)) { targetPos = targetEnemy.Center; chasingEnemy = true; }
            }

            float distToDest = NPC.Distance(targetPos);
            bool shouldFly = stuckInWall || distToDest > (chasingEnemy ? 800f : 300f) || (owner.velocity.Y != 0 && distToDest > 80f);
            float stopDist = chasingEnemy ? 50f : 120f;

            // 停止移动
            if (distToDest < stopDist && !stuckInWall)
            {
                NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, 0f, 0.15f);
                if (!chasingEnemy) { idleTimer++; if (idleTimer > 800) { if (Main.rand.NextBool(3)) { SoundEngine.PlaySound(DogBarkSound, NPC.position); CombatText.NewText(NPC.getRect(), Color.White, "汪!", true); } idleTimer = 0; } }
                return;
            }
            float moveSpeed = (distToDest > 600f) ? 14f : 8f;

            // 执行移动
            if (shouldFly) MoveToFly(targetPos, 12f); else MoveToGround(targetPos, moveSpeed);
        }

        private void StartAttack(NPC target, float atkSpeed)
        {
            attackState = 1;
            stateTimer = DASH_TIME_LIMIT;
            Vector2 dashDir = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero);
            NPC.velocity = dashDir * DASH_SPEED;
            attackCooldown = (int)(BASE_COOLDOWN / atkSpeed);
            SoundEngine.PlaySound(SoundID.NPCHit6 with { Pitch = -0.4f, Volume = 0.8f }, NPC.position);
            NPC.netUpdate = true;
        }

        private void MoveToFly(Vector2 target, float maxSpeed) { NPC.noGravity = true; NPC.noTileCollide = true; Vector2 desiredVelocity = (target - NPC.Center).SafeNormalize(Vector2.Zero) * maxSpeed; NPC.velocity = Vector2.Lerp(NPC.velocity, desiredVelocity, 0.15f); if (NPC.velocity.Length() > 0.5f && Main.rand.NextBool(2)) { for (int i = 0; i < 3; i++) { int d = Dust.NewDust(NPC.BottomLeft - new Vector2(0, 4), NPC.width, 8, DustID.Cloud, 0, 0, 100, default, Main.rand.NextFloat(0.8f, 1.2f)); Main.dust[d].velocity = NPC.velocity * -0.2f + Main.rand.NextVector2Circular(1f, 1f); Main.dust[d].noGravity = true; } } }
        private void MoveToGround(Vector2 target, float maxSpeed) { NPC.noGravity = false; NPC.noTileCollide = false; float distX = target.X - NPC.Center.X; float dirX = Math.Sign(distX); float targetVelX = dirX * maxSpeed; NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, targetVelX, 0.08f); if (NPC.velocity.Y == 0 && Math.Abs(NPC.velocity.X) > 0.5f) { int dustCount = Main.rand.Next(1, 3); for (int i = 0; i < dustCount; i++) { Vector2 footPos = NPC.BottomLeft + new Vector2(Main.rand.Next(NPC.width), 0); int d = Dust.NewDust(footPos, 4, 4, DustID.Cloud, -NPC.velocity.X * 0.2f, 0f, 100, default, Main.rand.NextFloat(1.0f, 1.5f)); Main.dust[d].noGravity = true; Main.dust[d].velocity *= 0.2f; } } if (NPC.velocity.Y < 0) { for (int i = 0; i < 5; i++) { Vector2 dustPos = NPC.position + new Vector2(0, NPC.height - 10); int d = Dust.NewDust(dustPos, NPC.width, 10, DustID.Cloud, 0f, 0f, 80, default, Main.rand.NextFloat(1.2f, 1.8f)); Main.dust[d].velocity *= 0.1f; Main.dust[d].velocity.Y += 0.5f; Main.dust[d].noGravity = true; } } if (NPC.velocity.Y <= 0) { float oldY = NPC.position.Y; Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY); if (NPC.position.Y < oldY && NPC.velocity.Y < -0.5f) { for (int i = 0; i < 35; i++) { int d = Dust.NewDust(NPC.BottomLeft, NPC.width, 10, DustID.Cloud, NPC.velocity.X * 0.2f, -1.5f, 90, default, Main.rand.NextFloat(1.2f, 2.2f)); Main.dust[d].velocity = Main.rand.NextVector2Circular(3f, 3f); Main.dust[d].velocity.Y -= 3f; Main.dust[d].noGravity = true; } } } }

        private void PerformTurretAI(Player owner)
        {
            // 1. 索敌 (视野内，1000像素范围)
            float range = GetAttackRange();
            NPC target = FindTarget(range, true);

            if (target != null)
            {
                // 让狗面向敌人
                NPC.direction = NPC.spriteDirection = (target.Center.X > NPC.Center.X) ? -1 : 1;

                // 2. 计算射速 (随序列等级大幅提升)
                // 序列9: 90帧 (约1.5秒一发)
                // 序列1: 10帧 (约0.16秒一发，机关枪模式)
                int baseCooldown = 90;
                int reductionPerSeq = 9;
                int fireRate = baseCooldown - (10 - currentSequence) * reductionPerSeq;

                // 应用攻速装备加成 (CalculatedAttackSpeed 在 UpdateStats 中已计算)
                fireRate = (int)(fireRate / CalculatedAttackSpeed);
                if (fireRate < 8) fireRate = 8; // 设置一个射速上限，防止鬼畜

                skillTimer++;
                if (skillTimer >= fireRate)
                {
                    skillTimer = 0;
                    CastTurretSkill(target, owner);
                }
            }
            else
            {
                // 待机时稍微冒点特效表示正在警戒
                if (Main.rand.NextBool(60))
                {
                    int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Enchanted_Gold, 0, -2);
                    Main.dust[d].noGravity = true;
                }
            }
        }

        // ==========================================
        // 新增：炮台技能发射逻辑 (根据途径选择弹幕)
        // ==========================================
        private void CastTurretSkill(NPC target, Player owner)
        {
            Vector2 shootPos = NPC.Center;
            // 预判瞄准
            Vector2 shootVel = (target.Center - shootPos).SafeNormalize(Vector2.Zero);
            bool skillUsed = false;

            // 根据途径和序列选择弹幕
            switch (currentPathway)
            {
                case 1: // 巨人 (光之斩击/剑气)
                    {
                        shootVel *= 14f;
                        int projType = ProjectileID.SwordBeam; // 默认剑气
                        if (currentSequence <= 6) projType = ModContent.ProjectileType<DawnBladeProjectile>(); // 黎明之剑

                        // 高序列特效：爆炸震荡 (序列3及以上)
                        if (currentSequence <= 3)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), target.Center, Vector2.Zero, ProjectileID.DD2ExplosiveTrapT3Explosion, NPC.damage * 3, 8f, Main.myPlayer);
                            SoundEngine.PlaySound(SoundID.Item14, NPC.Center);
                        }
                        else
                        {
                            // === 【位置修正】 ===
                            // 1. 向前偏移 40 像素，防止从身体里发出或打到脚下
                            // 2. 向上偏移 16 像素，确保离地一定高度
                            Vector2 spawnOffset = Vector2.Normalize(shootVel) * 40f;
                            spawnOffset.Y -= 16f;

                            int p = Projectile.NewProjectile(NPC.GetSource_FromAI(), shootPos + spawnOffset, shootVel, projType, NPC.damage, 4f, Main.myPlayer);

                            // === 【启用狗模式】 ===
                            // 如果是光之斩击，设置 ai[1] = 1 通知它变小且直线飞行
                            if (projType == ModContent.ProjectileType<DawnBladeProjectile>())
                            {
                                Main.projectile[p].ai[1] = 1;
                                Main.projectile[p].netUpdate = true;
                            }

                            SoundEngine.PlaySound(SoundID.Item71, NPC.Center);
                        }
                        skillUsed = true;
                    }
                    break;

                case 2:
                    if (target != null)
                    {
                        // 【修改重点】序列 2: 天气术士 (闪电 - 精准打击)
                        if (currentSequence <= 2)
                        {
                            // 直接在敌人位置生成闪电，无需飞行
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), target.Center, Vector2.Zero, ModContent.ProjectileType<WeatherStrikeLightning>(), NPC.damage * 4, 10f, Main.myPlayer);
                            SoundEngine.PlaySound(SoundID.Item122, target.Center); // 雷声
                            skillUsed = true;
                        }
                        // 【修改重点】序列 3: 战争主教 (改为 直线高爆火球，无抛物线)
                        else if (currentSequence <= 3)
                        {
                            // 使用 Fireball 替代 Bomb，并给予极高速度 (25f) 模拟直线炮击
                            Vector2 dir = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 25f;
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, dir, ModContent.ProjectileType<PyromaniacFireball>(), NPC.damage * 3, 6f, Main.myPlayer);
                            SoundEngine.PlaySound(SoundID.Item14, NPC.Center); // 爆炸声
                            skillUsed = true;
                        }
                        // 序列 6: 纵火家 (普通火球)
                        else if (currentSequence <= 6)
                        {
                            Vector2 dir = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 12f;
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, dir, ModContent.ProjectileType<PyromaniacFireball>(), NPC.damage * 2, 3f, Main.myPlayer);
                            SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                            skillUsed = true;
                        }
                        // 低序列: 小火球
                        else
                        {
                            Vector2 dir = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 10f;
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, dir, ProjectileID.BallofFire, NPC.damage, 2f, Main.myPlayer);
                            skillUsed = true;
                        }
                    }
                    break;

                case 3: // 月亮 (深渊/药剂)
                    {
                        if (currentSequence <= 3 && target != null)
                        {
                            // 1. 根据序列计算弹幕数量
                            int count = 3; // 序列3：3发
                            if (currentSequence <= 2) count = 5; // 序列2：5发
                            if (currentSequence <= 1) count = 8; // 序列1：8发 (火力覆盖)

                            // 2. 循环生成弹幕
                            for (int i = 0; i < count; i++)
                            {
                                // 随机生成在目标头顶上方高处
                                Vector2 spawnPos = target.Center + new Vector2(Main.rand.NextFloat(-200, 200), -600f);

                                // 设定落点：以目标为中心，随机散布在周围区域 (大范围打击)
                                Vector2 aimPos = target.Center + Main.rand.NextVector2Circular(150, 150);

                                // 计算速度向量 (飞向落点)
                                Vector2 velocity = (aimPos - spawnPos).SafeNormalize(Vector2.Zero) * 20f;

                                // 发射月耀 (ID 645)
                                // ai[1] 参数控制它的垂直目标高度 (aimPos.Y)
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), spawnPos, velocity, ProjectileID.LunarFlare, NPC.damage * 2, 6f, owner.whoAmI, 0, aimPos.Y);
                            }

                            SoundEngine.PlaySound(SoundID.Item122, NPC.Center); // 播放魔法音效
                            skillUsed = true;
                        }
                        else if (currentSequence <= 6) // 魔药教授：剧毒/治疗瓶
                        {
                            // 如果主人受伤了，有 33% 的几率扔治疗瓶，而不是毒瓶
                            if (owner.statLife < owner.statLifeMax2 && Main.rand.NextBool(3))
                            {
                                // 计算飞向主人的方向
                                Vector2 toOwner = (owner.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 14f;

                                // 发射治疗球 (复用 DogHealingOrb，序列6治疗量设为 30)
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, toOwner, ModContent.ProjectileType<DogHealingOrb>(), 30, 0f, owner.whoAmI);

                                SoundEngine.PlaySound(SoundID.Item29, NPC.Center); // 魔法音效
                            }
                            else
                            {
                                // 正常攻击：投掷炼金手雷 (剧毒) - 直线投掷
                                shootVel *= 14f; // 速度稍快一点，更适合直线
                                int p = Projectile.NewProjectile(NPC.GetSource_FromAI(), shootPos, shootVel, ModContent.ProjectileType<AlchemicalGrenade>(), NPC.damage, 4f, owner.whoAmI);

                                // 【核心修改】设置为直线模式
                                Main.projectile[p].ai[1] = 1;
                                Main.projectile[p].netUpdate = true;

                                SoundEngine.PlaySound(SoundID.Item106, NPC.Center); // 扔瓶子音效
                            }
                            skillUsed = true;
                        }
                        else // 药师：治疗能力 (序列 7, 8, 9)
                        {
                            Vector2 toOwner = (owner.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 12f;

                            // 设定固定治疗量 (随序列提升)
                            int healAmount = 10;
                            if (currentSequence <= 8) healAmount = 15;
                            if (currentSequence <= 7) healAmount = 20;

                            // 发射自定义治疗球
                            // 注意：这里传入 healAmount 作为弹幕伤害，我们的自定义弹幕会把它当作治疗量
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, toOwner, ModContent.ProjectileType<DogHealingOrb>(), healAmount, 0f, owner.whoAmI);

                            // 狗自己也回一点血
                            if (NPC.life < NPC.lifeMax) NPC.life += 2;

                            skillUsed = true;
                        }
                    }
                    break;

                case 4: // 愚者 (空气炮/纸牌)
                    {
                        if (currentSequence <= 3) // 古代学者：空气炮
                        {
                            shootVel *= 20f;
                            int type = ModContent.ProjectileType<AirBulletProjectile>();
                            if (type <= 0) type = ProjectileID.BulletHighVelocity;
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), shootPos, shootVel, type, NPC.damage * 2, 8f, Main.myPlayer);
                            SoundEngine.PlaySound(SoundID.Item11, NPC.Center);
                        }
                        else // 纸牌投掷
                        {
                            shootVel *= 15f;
                            int dmg = currentSequence <= 6 ? (int)(NPC.damage * 1.5f) : NPC.damage;
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), shootPos, shootVel, ModContent.ProjectileType<PaperCardProjectile>(), dmg, 2f, Main.myPlayer);
                            SoundEngine.PlaySound(SoundID.Item1, NPC.Center);
                        }
                        skillUsed = true;
                    }
                    break;

                case 5: // 错误 (窃取/时钟)
                    if (target == null) break;
                    {
                        // 1. 通用被动
                        TryStealFromNPC(target, owner);

                        if (Main.GameUpdateCount % 10 == 0)
                            SoundEngine.PlaySound(SoundID.Item93, target.Center);

                        // === 【核心修改】序列 1: 时之虫 (时之钟领域) ===
                        if (currentSequence <= 1)
                        {
                            // 检查是否已经有时钟
                            bool clockFound = false;
                            for (int i = 0; i < Main.maxProjectiles; i++)
                            {
                                Projectile p = Main.projectile[i];
                                if (p.active && p.owner == owner.whoAmI && p.type == ModContent.ProjectileType<DogTimeClock>() && (int)p.ai[0] == NPC.whoAmI)
                                {
                                    p.timeLeft = 60; // 续命
                                    clockFound = true;
                                    break;
                                }
                            }

                            // 如果没有，生成一个新的
                            if (!clockFound)
                            {
                                // 【关键修正】速度参数必须是 Vector2.Zero！
                                // 这样它生成时就是静止的，然后被 AI 锁定在身上
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<DogTimeClock>(), NPC.damage, 0f, owner.whoAmI, NPC.whoAmI);

                                SoundEngine.PlaySound(SoundID.Item113, NPC.Center);
                            }

                            skillUsed = true;
                        }
                        // === 序列 3: 斩杀 (保持不变) ===
                        else if (currentSequence <= 3)
                        {
                            float executeThreshold = target.boss ? 0.03f : 0.15f;
                            if (target.life < (target.lifeMax * executeThreshold))
                            {
                                int killDmg = 999999;
                                target.StrikeNPC(new NPC.HitInfo { Damage = killDmg, Crit = true });
                                HealOwner(owner, 50);
                                for (int i = 0; i < 20; i++) Dust.NewDust(target.position, target.width, target.height, DustID.PurpleCrystalShard, 0, 0, 0, default, 2f);
                            }
                            else
                            {
                                int heavyDmg = NPC.damage * 5;
                                target.StrikeNPC(new NPC.HitInfo { Damage = heavyDmg, Knockback = 2f });
                                HealOwner(owner, heavyDmg / 10);
                            }
                            skillUsed = true;
                        }
                        // === 序列 9-4: 窃取攻击 (保持不变) ===
                        else
                        {
                            int dmg = (int)(NPC.damage * (currentSequence <= 6 ? 1.5f : 1.0f));
                            target.StrikeNPC(new NPC.HitInfo { Damage = dmg, Knockback = 2f });

                            if (currentSequence <= 6) HealOwner(owner, 10);

                            target.velocity *= 0.6f;
                            target.AddBuff(BuffID.Slow, 180);
                            owner.AddBuff(BuffID.Swiftness, 180);
                        }

                        // 视觉特效：有时钟时不显示闪电
                        if (currentSequence > 1)
                        {
                            for (int i = 0; i < 12; i++)
                            {
                                Vector2 pos = Vector2.Lerp(NPC.Center, target.Center, i / 12f);
                                pos += Main.rand.NextVector2Circular(5, 5);
                                Dust d = Dust.NewDustPerfect(pos, DustID.Electric, Vector2.Zero, 0, default, 1.2f);
                                d.noGravity = true;
                            }
                        }

                        skillUsed = true;
                    }
                    break;
                case 6: // 太阳 (光束/火焰/神圣)
                    if (target != null)
                    {
                        // 序列 4+: 阳炎 / 审判
                        if (currentSequence <= 4)
                        {
                            Vector2 spawnPos = target.Center + new Vector2(0, -400);
                            Vector2 vel = new Vector2(0, 15f);
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), spawnPos, vel, ModContent.ProjectileType<JusticeJudgment>(), NPC.damage * 4, 6f, Main.myPlayer);
                            SoundEngine.PlaySound(SoundID.Item29, target.Center);
                            skillUsed = true;
                        }
                        // 序列 6: 太阳神官 (神圣之光)
                        else if (currentSequence <= 6)
                        {
                            Vector2 dir = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 12f;
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, dir, ModContent.ProjectileType<HolyLightBeam>(), NPC.damage * 2, 2f, Main.myPlayer);
                            SoundEngine.PlaySound(SoundID.Item72, NPC.Center);
                            skillUsed = true;
                        }
                        // 序列 7: 公证人 (神圣公证 - 削弱敌人)
                        // 【修复：直接接 else if，不要在前面加 else】
                        else if (currentSequence <= 7)
                        {
                            Vector2 dir = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 14f;
                            // 黄金雨 (Golden Shower)
                            int p = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, dir, ProjectileID.GoldenShowerFriendly, NPC.damage, 0f, Main.myPlayer);
                            Main.projectile[p].timeLeft = 600;
                            SoundEngine.PlaySound(SoundID.Item13, NPC.Center);
                            skillUsed = true;
                        }
                        // 序列 8 & 9 (如果距离远，偶尔丢个火球)
                        // 【修复：这是最后的 else，处理所有剩余情况】
                        else
                        {
                            Vector2 dir = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 10f;
                            // 魔法飞弹
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, dir, ProjectileID.BallofFire, NPC.damage, 1f, Main.myPlayer);
                            skillUsed = true;
                        }
                    }
                    break;

            }

            if (skillUsed && Main.rand.NextBool(5)) // 偶尔叫一声
            {
                CombatText.NewText(NPC.getRect(), Color.Cyan, "汪!", true);
            }
        }
        private void UpdateSunInferno()
        {
            // 仅太阳途径 (ID: 6) 且已开启序列 (<= 9) 生效
            if (currentPathway == 6 && currentSequence <= 9)
            {
                // 1. 视觉特效：双重旋转火圈 (模拟狱火药水)
                if (Main.netMode != NetmodeID.Server) // 仅客户端绘制
                {
                    float radius = 80f; // 火圈半径
                    float rotSpeed = 0.15f; // 旋转速度
                    float angle = Main.GameUpdateCount * rotSpeed;

                    // 内圈：金色神圣之火
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 offset = (angle + i * MathHelper.Pi).ToRotationVector2() * radius;
                        // 随机微调位置，让火焰看起来在跳动
                        offset += Main.rand.NextVector2Circular(5, 5);

                        int d = Dust.NewDust(NPC.Center + offset, 0, 0, DustID.GoldFlame, 0, 0, 100, default, 1.5f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].velocity = Vector2.Zero; // 粒子固定在轨道上
                    }

                    // 外圈：普通烈焰 (稍微慢一点，反向旋转，增加层次感)
                    float angle2 = Main.GameUpdateCount * (rotSpeed * -0.8f);
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 offset = (angle2 + i * MathHelper.Pi).ToRotationVector2() * (radius + 25f);
                        int d = Dust.NewDust(NPC.Center + offset, 0, 0, DustID.Torch, 0, 0, 100, default, 1.5f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].velocity = Vector2.Zero;
                    }
                }

                // 2. 伤害逻辑：烧伤周围敌人 (每 15 帧 / 0.25秒 判定一次)
                if (Main.GameUpdateCount % 15 == 0)
                {
                    float range = 130f; // 伤害范围略大于视觉范围
                    foreach (NPC target in Main.ActiveNPCs)
                    {
                        // 排除友方、自己、无法选中的目标
                        if (target.CanBeChasedBy() && target.Distance(NPC.Center) < range)
                        {
                            // 基础伤害随序列提升
                            // 序列9: 20点, 序列1: 100点 (每级+10)
                            int dmg = 20 + (9 - currentSequence) * 10;

                            // 对不死/邪恶生物伤害翻倍
                            if (NPCID.Sets.Zombies[target.type] || NPCID.Sets.Skeletons[target.type])
                            {
                                dmg *= 2;
                            }

                            // 施加“狱火”Debuff (OnFire3 是泰拉瑞亚里更强的火)
                            target.AddBuff(BuffID.OnFire3, 180); // 持续3秒

                            // 造成无敌帧独立的伤害
                            NPC.HitInfo hit = new NPC.HitInfo();
                            hit.Damage = dmg;
                            hit.Knockback = 2f; // 轻微击退，防止怪贴脸
                            hit.HitDirection = (target.Center.X > NPC.Center.X) ? 1 : -1;
                            target.StrikeNPC(hit);
                        }
                    }
                }

                // 3. 自身照明
                Lighting.AddLight(NPC.Center, 0.8f, 0.6f, 0.2f);
            }
        }
        private void HandleDogSkills(Player owner)
        {
            if (currentSequence >= 10) return;
            skillTimer++;
            int cooldown = 600 - (10 - currentSequence) * 50; if (cooldown < 120) cooldown = 120;
            if (skillTimer > cooldown)
            {
                bool skillUsed = false; float range = GetAttackRange();
                NPC target = FindTarget(range, true);

                float dist = target != null ? target.Distance(NPC.Center) : 9999f;
                switch (currentPathway)
                {
                    case 1: if (currentSequence <= 3) { if (target != null && dist < 300f) { Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ProjectileID.DD2ExplosiveTrapT3Explosion, NPC.damage * 3, 8f, Main.myPlayer); SoundEngine.PlaySound(SoundID.Item14, NPC.Center); skillUsed = true; } } else if (currentSequence <= 6) { if (target != null && dist < 400f) { Vector2 dir = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 12f; Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, dir, ModContent.ProjectileType<DawnBladeProjectile>(), NPC.damage * 2, 4f, Main.myPlayer); SoundEngine.PlaySound(SoundID.Item71, NPC.Center); skillUsed = true; } } else { owner.AddBuff(BuffID.Ironskin, 600); owner.AddBuff(BuffID.Endurance, 600); CombatText.NewText(NPC.getRect(), Color.Gold, "守护!", true); skillUsed = true; } break;
                    case 2: // 猎人途径
                        {
                            // 【核心修复】在这里直接定义 shootVel，防止报错
                            // 确保这一行在 case 2 的大括号里面
                            Vector2 shootVel = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero);

                            // 序列 2: 天气术士 (闪电 - 精准打击)
                            if (currentSequence <= 2)
                            {
                                // 闪电直接生成在目标头顶，无需飞行速度
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), target.Center, Vector2.Zero, ModContent.ProjectileType<WeatherStrikeLightning>(), NPC.damage * 4, 10f, Main.myPlayer);
                                SoundEngine.PlaySound(SoundID.Item122, target.Center);
                                skillUsed = true;
                            }
                            // 序列 3: 战争主教 (炸弹 -> 改为 直线高爆火球)
                            else if (currentSequence <= 3)
                            {
                                // 设为直线高速 (25f)
                                shootVel *= 25f;
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, shootVel, ModContent.ProjectileType<PyromaniacFireball>(), NPC.damage * 3, 6f, Main.myPlayer);
                                SoundEngine.PlaySound(SoundID.Item14, NPC.Center);
                                skillUsed = true;
                            }
                            // 序列 6: 纵火家 (普通火球)
                            else if (currentSequence <= 6)
                            {
                                shootVel *= 12f;
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, shootVel, ModContent.ProjectileType<PyromaniacFireball>(), NPC.damage * 2, 3f, Main.myPlayer);
                                SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                                skillUsed = true;
                            }
                            // 低序列: 小火球
                            else
                            {
                                shootVel *= 10f;
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, shootVel, ProjectileID.BallofFire, NPC.damage, 2f, Main.myPlayer);
                                skillUsed = true;
                            }
                        }
                        break;
                    case 3: if (currentSequence <= 3 && target != null) { Vector2 dir = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 15f; Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, dir, ModContent.ProjectileType<AbyssShackleProjectile>(), NPC.damage, 0f, Main.myPlayer); skillUsed = true; } else if (currentSequence <= 6) { if (target != null && dist < 400f) { Vector2 dir = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 12f; Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, dir, ModContent.ProjectileType<AlchemicalGrenade>(), NPC.damage, 4f, Main.myPlayer); skillUsed = true; } else { HealOwner(owner, 30); skillUsed = true; } } else { HealOwner(owner, 15); skillUsed = true; } break;
                    case 4: if (target != null)
                        {
                            if (currentSequence <= 3) { Vector2 dir = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 20f; int type = ModContent.ProjectileType<AirBulletProjectile>(); if (type <= 0) type = ProjectileID.BulletHighVelocity; Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, dir, type, NPC.damage * 2, 8f, Main.myPlayer); SoundEngine.PlaySound(SoundID.Item11, NPC.Center); skillUsed = true; }
                            else if (currentSequence <= 6) { Vector2 dir = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 15f; Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, dir, ModContent.ProjectileType<PaperCardProjectile>(), (int)(NPC.damage * 1.5f), 2f, Main.myPlayer); skillUsed = true; }
                            else
                            {
                                Vector2 dir = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 13f;

                                // 2. 使用 mod 自定义的纸牌弹幕 (PaperCardProjectile)
                                // 之前这里写的是 ProjectileID.Shuriken (手里剑)，太小看不清
                                int projType = ModContent.ProjectileType<PaperCardProjectile>();

                                // 3. 发射弹幕
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, dir, projType, NPC.damage, 1f, Main.myPlayer);

                                // 4. 【新增】加上投掷音效，让你听到它攻击
                                SoundEngine.PlaySound(SoundID.Item1, NPC.Center);

                                skillUsed = true;
                            }
                        }
                        break;
                    case 5: if (target != null) { if (currentSequence <= 3) { int stealDmg = target.lifeMax / 20; if (stealDmg > NPC.damage * 5) stealDmg = NPC.damage * 5; target.StrikeNPC(new NPC.HitInfo { Damage = stealDmg, Knockback = 0f }); HealOwner(owner, stealDmg / 5); CombatText.NewText(target.getRect(), Color.Purple, "命运窃取!", true); skillUsed = true; } else if (currentSequence <= 6) { target.StrikeNPC(new NPC.HitInfo { Damage = (int)(NPC.damage * 1.5f), Knockback = 2f }); HealOwner(owner, 10); skillUsed = true; } else { target.AddBuff(BuffID.Slow, 300); owner.AddBuff(BuffID.Swiftness, 300); CombatText.NewText(target.getRect(), Color.Silver, "速度窃取!", true); skillUsed = true; } } break;
                    case 6: // 太阳
                        // 序列 9: 歌颂者 (勇气赞歌 - 攻防一体 + 震慑)
                        if (currentSequence >= 9)
                        {
                            // 1. 强力 Buff：怒气(加攻) + 铁皮(加防)
                            owner.AddBuff(BuffID.Wrath, 600);
                            owner.AddBuff(BuffID.Ironskin, 600);

                            // 2. 歌颂震慑：每隔几秒吼一声，把附近的敌人震退/眩晕
                            if (Main.GameUpdateCount % 180 == 0) // 每3秒
                            {
                                bool anyEnemyHit = false;
                                foreach (NPC targetNPC in Main.ActiveNPCs)
                                {
                                    // 范围 300 像素内的敌人
                                    if (targetNPC.CanBeChasedBy() && targetNPC.Distance(NPC.Center) < 300f)
                                    {
                                        targetNPC.AddBuff(BuffID.Confused, 120); // 混乱2秒
                                        targetNPC.velocity += (targetNPC.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 5f; // 击退
                                        anyEnemyHit = true;
                                    }
                                }
                                if (anyEnemyHit)
                                {
                                    SoundEngine.PlaySound(SoundID.Item4, NPC.Center); // 吼叫声
                                    CombatText.NewText(NPC.getRect(), Color.Gold, "勇气!", true);
                                    // 生成一圈金色音符特效
                                    for (int i = 0; i < 20; i++)
                                    {
                                        Vector2 v = Main.rand.NextVector2Circular(5, 5);
                                        Dust.NewDustPerfect(NPC.Center, DustID.Enchanted_Gold, v, 0, default, 1.5f).noGravity = true;
                                    }
                                }
                            }
                            skillUsed = true;
                        }

                        // 序列 8: 祈光人 (神圣光环 - 烧怪 + 强力回血)
                        if (currentSequence <= 8)
                        {
                            // 1. 快速治疗 (Rapid Healing) 而不是普通再生
                            owner.AddBuff(BuffID.RapidHealing, 600);

                            // 2. 灼热光环：每秒对周围敌人造成伤害
                            if (Main.GameUpdateCount % 60 == 0)
                            {
                                foreach (NPC targetNPC in Main.ActiveNPCs)
                                {
                                    if (targetNPC.CanBeChasedBy() && targetNPC.Distance(NPC.Center) < 250f)
                                    {
                                        // 直接扣血，不触发无敌帧 (模拟环境伤害)
                                        targetNPC.life -= 15;
                                        targetNPC.HitEffect(0, 15);
                                        if (targetNPC.life <= 0) targetNPC.checkDead();

                                        // 视觉特效：身上着火
                                        for (int i = 0; i < 5; i++) Dust.NewDust(targetNPC.position, targetNPC.width, targetNPC.height, DustID.GoldFlame);
                                    }
                                }
                            }

                            // 视觉：狗身边一直有光圈
                            if (Main.rand.NextBool(5))
                            {
                                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GoldFlame, 0, -1, 150, default, 0.8f);
                            }
                        }
                        // 序列 5: 无暗者 (净化/范围伤害)
                        else if (currentSequence <= 5)
                        {
                            // 以狗为中心产生光爆 (Unshadowed Domain 简化版)
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<FlaringSun>(), NPC.damage * 3, 8f, Main.myPlayer);

                            // 净化主人 Debuff (简单实现)
                            if (owner.buffType.Length > 0)
                            {
                                // 这里简单清除一个负面Buff，实际需要更复杂的判断
                                owner.ClearBuff(BuffID.Poisoned);
                                owner.ClearBuff(BuffID.OnFire);
                            }
                            SoundEngine.PlaySound(SoundID.Item29, NPC.Center);
                            skillUsed = true;
                        }
                        // 中序列: 召唤光之矛
                        else
                        {
                            if (target != null)
                            {
                                Vector2 dir = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 15f;
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, dir, ModContent.ProjectileType<UnshadowedSpear>(), NPC.damage * 2, 4f, Main.myPlayer);
                                skillUsed = true;
                            }
                        }
                        break;
                }
                if (skillUsed) skillTimer = Main.rand.Next(-100, 50);
            }
        }
        private void TryStealFromNPC(NPC target, Player owner)
        {
            // 只有错误途径 (5) 且开启了序列 (9-1) 才生效
            if (currentPathway != 5 || currentSequence >= 10) return;

            // 计算概率：
            // 基础概率：普通怪 5%，Boss 0.5%
            // 成长概率：每提升 1 级序列，增加 1% (Boss 增加 0.1%)
            // 序列 9: 5% (普通)
            // 序列 1: 13% (普通)
            float chance = (target.boss ? 0.005f : 0.05f) + (9 - currentSequence) * (target.boss ? 0.001f : 0.01f);

            // 限制一下最高概率 (20%)，防止太离谱
            if (chance > 0.2f) chance = 0.2f;

            if (Main.rand.NextFloat() < chance)
            {
                // 构造掉落请求
                var dropInfo = new Terraria.GameContent.ItemDropRules.DropAttemptInfo
                {
                    player = owner,
                    npc = target,
                    IsExpertMode = Main.expertMode,
                    IsMasterMode = Main.masterMode,
                    IsInSimulation = false,
                    rng = Main.rand
                };

                // 尝试执行掉落 (不扣除怪物血量，直接“偷”出来)
                Main.ItemDropSolver.TryDropping(dropInfo);

                // 视觉反馈
                CombatText.NewText(target.getRect(), Color.Gold, "窃取!", true);
                for (int i = 0; i < 5; i++)
                    Dust.NewDust(target.position, target.width, target.height, DustID.GoldCoin, 0, 0, 0, default, 1.2f);
            }
        }

        private NPC FindTarget(float range, bool checkLineOfSight) { NPC target = null; float minDistance = range; foreach (NPC npc in Main.ActiveNPCs) { if (npc.CanBeChasedBy() && npc.Distance(NPC.Center) < minDistance) { if (checkLineOfSight && !Collision.CanHit(NPC.position, NPC.width, NPC.height, npc.position, npc.width, npc.height)) continue; minDistance = npc.Distance(NPC.Center); target = npc; } } return target; }
        private void HealOwner(Player owner, int amount) { foreach (Player p in Main.ActivePlayers) { if (!p.dead && p.Distance(NPC.Center) < 800f && p.statLife < p.statLifeMax2) { p.statLife += amount; p.HealEffect(amount); for (int i = 0; i < 10; i++) Dust.NewDustPerfect(p.Center + Main.rand.NextVector2Circular(30, 30), DustID.Terra, new Vector2(0, -2), 0, default, 1.5f).noGravity = true; } } }

        // ==========================================
        // UI 和 信息显示辅助方法
        // ==========================================
        public string GetPathwayName(int id)
        {
            switch (id)
            {
                case 1: return "巨人";
                case 2: return "猎人";
                case 3: return "月亮";
                case 4: return "愚者";
                case 5: return "错误";
                case 6: return "太阳";
                default: return "未知";
            }
        }

        public string GetAbilityDescription()
        {
            if (currentSequence >= 10) return "无能力 (请喂食序列9魔药)";
            string skillName = "未知";
            switch (currentPathway)
            {
                case 1:
                    if (currentSequence <= 3) skillName = "银骑士：荣耀震荡 (大范围击退)";
                    else if (currentSequence <= 6) skillName = "黎明骑士：光之斩击 (破防)";
                    else skillName = "战士：守护咆哮 (加防)";
                    break;
                case 2:
                    if (currentSequence <= 3) skillName = "战争主教：阳炎爆破 (毁灭)";
                    else if (currentSequence <= 6) skillName = "纵火家：压缩火球 (爆炸)";
                    else skillName = "猎人：火球术 (远程)";
                    break;
                case 3:
                    if (currentSequence <= 3) skillName = "召唤大师：月华领域)";
                    else if (currentSequence <= 6) skillName = "魔药教授：剧毒/治疗瓶 (投掷)";
                    else skillName = "药师：生命光环 (治疗)";
                    break;
                case 4:
                    if (currentSequence <= 3) skillName = "古代学者：空气炮 (穿透)";
                    else if (currentSequence <= 6) skillName = "无面人：强力射击 (击退)";
                    else skillName = "占卜家：纸牌投掷 (远程)";
                    break;
                case 5:
                    if (currentSequence <= 1)
                    {
                        skillName = "时之虫：时之钟 (时间领域)";
                    }
                    else if (currentSequence <= 3)
                    {
                        skillName = "欺瞒导师：命运窃取 (斩杀)";
                    }
                    else if (currentSequence <= 6)
                    {
                        skillName = "腐化男爵：生命窃取 (吸血)";
                    }
                    else
                    {
                        skillName = "偷盗者：速度窃取 (减速)";
                    }
                    break;
                case 6:
                    if (currentSequence <= 3) skillName = "正义导师：神圣审判 (毁灭)";
                    else if (currentSequence <= 6) skillName = "光之祭司：净化之光 (激光)";
                    else if (currentSequence <= 7) skillName = "公证人：神圣公证 (削弱/灵液)"; // 新增
                    else if (currentSequence <= 8) skillName = "祈光人：灼热光环 (烧怪)"; // 新增
                    else skillName = "歌颂者：勇气赞歌 (攻防Buff/震慑)"; // 新增
                    break;
            }
            return $"当前技能: {skillName}";
        }

        public override void ModifyTypeName(ref string typeName)
        {
            string seqTitle = currentSequence < 10 ? $" [{GetPathwayName(currentPathway)} 序列{currentSequence}]" : "";
            if (!string.IsNullOrEmpty(OwnerName)) typeName = $"{MyName}{seqTitle} <{OwnerName}>"; else typeName = $"{MyName}{seqTitle}";
        }

        // ==========================================
        // 存档与加载
        // ==========================================
        public override void SaveData(TagCompound tag) { tag["DogName"] = MyName ?? "旺财"; tag["OwnerName"] = OwnerName ?? ""; tag["BonusHP"] = BonusMaxHP; tag["IsStaying"] = isStaying; tag["DogPathway"] = currentPathway; tag["DogSequence"] = currentSequence; if (DogInventory == null) DogInventory = new Item[3]; for (int i = 0; i < 3; i++) { if (DogInventory[i] == null) DogInventory[i] = new Item(); tag[$"Inv_{i}"] = ItemIO.Save(DogInventory[i]); } }
        public override void LoadData(TagCompound tag) { try { if (tag.ContainsKey("DogName")) MyName = tag.GetString("DogName"); if (tag.ContainsKey("OwnerName")) OwnerName = tag.GetString("OwnerName"); else OwnerName = ""; if (tag.ContainsKey("BonusHP")) { BonusMaxHP = tag.GetInt("BonusHP"); NPC.lifeMax = 300 + BonusMaxHP; NPC.life = NPC.lifeMax; } if (tag.ContainsKey("IsStaying")) isStaying = tag.GetBool("IsStaying"); if (tag.ContainsKey("DogPathway")) currentPathway = tag.GetInt("DogPathway"); if (tag.ContainsKey("DogSequence")) currentSequence = tag.GetInt("DogSequence"); DogInventory = new Item[3]; for (int i = 0; i < 3; i++) { if (tag.ContainsKey($"Inv_{i}")) DogInventory[i] = ItemIO.Load(tag.GetCompound($"Inv_{i}")); else { DogInventory[i] = new Item(); DogInventory[i].SetDefaults(0); } } isInitialized = true; } catch { MyName = "旺财(重置)"; OwnerName = ""; DogInventory = new Item[3]; for (int i = 0; i < 3; i++) { DogInventory[i] = new Item(); DogInventory[i].SetDefaults(0); } } }
        public override void SendExtraAI(BinaryWriter writer) { writer.Write(isStaying); writer.Write(attackState); writer.Write(stateTimer); writer.Write(OwnerName ?? ""); writer.Write(currentPathway); writer.Write(currentSequence); writer.Write(MyName ?? "旺财"); }
        public override void ReceiveExtraAI(BinaryReader reader) { isStaying = reader.ReadBoolean(); attackState = reader.ReadInt32(); stateTimer = reader.ReadInt32(); OwnerName = reader.ReadString(); currentPathway = reader.ReadInt32(); currentSequence = reader.ReadInt32(); MyName = reader.ReadString(); }

        // ==========================================
        // 绘制与动画
        // ==========================================
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = TextureAssets.Npc[NPC.type].Value; Vector2 drawPos = NPC.Center - screenPos; drawPos.Y += NPC.gfxOffY - 1f; drawPos.X = (float)Math.Floor(drawPos.X); drawPos.Y = (float)Math.Floor(drawPos.Y);
            int currentFrameIndex = NPC.frame.Y / FRAME_HEIGHT; Rectangle finalFrame = new Rectangle(0, currentFrameIndex * FRAME_HEIGHT, texture.Width, FRAME_HEIGHT);
            int currentCutTop = CUT_TOP; int currentCutBottom = CUT_BOTTOM;
            if (currentFrameIndex == 10) currentCutTop += 2;
            finalFrame.Y += currentCutTop; finalFrame.Height = FRAME_HEIGHT - currentCutTop - currentCutBottom;
            Vector2 origin = finalFrame.Size() / 2f; origin.X = (float)Math.Floor(origin.X); origin.Y = (float)Math.Floor(origin.Y);
            SpriteEffects effects = NPC.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            if (attackState > 0) { for (int i = 1; i < 5; i++) { Vector2 afterImgPos = drawPos - NPC.velocity * i * 0.25f; Color afterImgColor = drawColor * (0.5f - i * 0.1f); spriteBatch.Draw(texture, afterImgPos, finalFrame, afterImgColor, NPC.rotation, origin, NPC.scale, effects, 0f); } }
            spriteBatch.End(); spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            spriteBatch.Draw(texture, drawPos, finalFrame, drawColor, NPC.rotation, origin, NPC.scale, effects, 0f);
            spriteBatch.End(); spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }

        public override void FindFrame(int frameHeight)
        {
            int h = FRAME_HEIGHT; if (NPC.velocity.X > 0.1f) NPC.spriteDirection = -1; else if (NPC.velocity.X < -0.1f) NPC.spriteDirection = 1;
            bool isFlying = NPC.noGravity; if (NPC.velocity.Y != 0) inAirTimer++; else inAirTimer = 0; bool isJumpingReal = (inAirTimer > 4 || Math.Abs(NPC.velocity.Y) > 2.5f) && !isFlying;
            if (attackState > 0 || isFlying || isJumpingReal) { NPC.frame.Y = 10 * h; }
            else if (isStaying) { NPC.frameCounter++; if (NPC.frameCounter < 20) NPC.frame.Y = 8 * h; else if (NPC.frameCounter < 40) NPC.frame.Y = 9 * h; else NPC.frameCounter = 0; }
            else if (Math.Abs(NPC.velocity.X) < 0.1f && Math.Abs(NPC.velocity.Y) < 0.1f) { if (NPC.ai[0] == 0 && NPC.HasValidTarget && NPC.Distance(Main.npc[NPC.target].Center) < 100f) { NPC.frameCounter++; if (NPC.frameCounter < 10) NPC.frame.Y = 11 * h; else if (NPC.frameCounter < 20) NPC.frame.Y = 12 * h; else NPC.frameCounter = 0; } else { NPC.frame.Y = 8 * h; NPC.frameCounter = 0; } }
            else { float animationSpeed = Math.Abs(NPC.velocity.X) * 0.2f; if (animationSpeed < 0.2f) animationSpeed = 0.2f; NPC.frameCounter += animationSpeed; int totalWalkFrames = 8; int frameSwitchSpeed = 6; int current = (int)NPC.frameCounter % (totalWalkFrames * frameSwitchSpeed); NPC.frame.Y = (current / frameSwitchSpeed) * h; }
        }

        public override bool CanChat() => true;
        public override string GetChat()
        {
            Player player = Main.LocalPlayer;
            // 获取玩家的 LotMPlayer 实例，用来判断玩家的途径
            // 注意：如果你的 LotMPlayer 不在 zhashi.Content 命名空间下，请修改前面的引用
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            List<string> chat = new List<string>();

            // 1. 基础对话 (保留原来的忠诚设定)
            string ownerNameDisplay = string.IsNullOrEmpty(OwnerName) ? "你" : OwnerName;
            chat.Add($"{MyName} 忠诚地守护着 {ownerNameDisplay}。");
            chat.Add("汪！(它摇了摇尾巴，但眼神异常深邃)");

            // 2. 诡秘之主风格 - 通用台词 (苏西/怪物风格)
            chat.Add("不要直视星空...即使是一只狗也知道这一点。");
            chat.Add("这块骨头...有“观众”途径的暗示，我不能吃，这不符合礼仪。");
            chat.Add("你也听到了吗？那些呓语...有时候我会对着空气狂吠，是因为那里真的有东西。");
            chat.Add("作为一只非凡生物，我必须时刻警惕扮演法的反噬。汪！");
            chat.Add("小心你的背后，灵界生物总是喜欢盯着强者的影子。");
            chat.Add("如果我失控了...请在我变成怪物之前杀了我。");

            // 3. 基于 NPC 自身途径的台词 (根据当前喂食的魔药)
            switch (currentPathway)
            {
                case 0: // 凡人/未开启
                    chat.Add("我渴望力量...汪！我是说，有魔药吗？");
                    break;
                case 1: // 巨人 (战士)
                    chat.Add("我感觉我的皮毛像钢铁一样坚硬。");
                    chat.Add("黎明的剑光在呼唤我...");
                    break;
                case 2: // 猎人
                    chat.Add("好热...我的血液在燃烧！想打架吗？");
                    chat.Add("这里需要一场陷阱...或者一场爆炸。");
                    break;
                case 3: // 月亮 (药师)
                    chat.Add("生命是宝贵的，但有时候死亡也是一种药剂。");
                    chat.Add("嗷呜——！(绯红之月...它在看着我)");
                    break;
                case 4: // 愚者 (占卜家)
                    chat.Add("我的灵性直觉告诉我，今天不宜出门...除了散步。");
                    chat.Add("命运...就像我手中的狗绳，看似松弛，实则被紧握。");
                    break;
                case 5: // 错误 (偷盗者)
                    chat.Add("你的背包里刚才是不是少了一块肉？别看我，我不知道。");
                    chat.Add("如果时间可以被偷走，我想偷走等待晚饭的那段时间。");
                    break;
                case 6: // 太阳
                    chat.Add("赞美太阳！哪怕是在地底，我也能感受到祂的光辉。");
                    chat.Add("你的影子...很干净，这很好。");
                    chat.Add("我要净化这块腐肉再吃...这是仪式感。");
                    break;
            }

            // 4. 基于玩家途径的彩蛋对话
            if (modPlayer.currentFoolSequence <= 9) // 玩家是占卜家
            {
                chat.Add("你的身上有灵之虫的味道...闻起来像是高维度的美味，但我不敢咬。");
                chat.Add("赞美愚者！虽然我不知道祂是谁，但顺从强者的灵性直觉准没错。");
            }
            if (modPlayer.currentHunterSequence <= 9) // 玩家是猎人
            {
                chat.Add("好烫！你的气场太暴躁了，这会烧焦我柔顺的毛发！");
            }
            if (modPlayer.currentMarauderSequence <= 9) // 玩家是偷盗者
            {
                chat.Add("我的项圈呢？刚才还在的！把你手里的东西放下！");
                chat.Add("刚才有一瞬间，我感觉我的思维被偷走了一秒钟...是你干的吗？");
            }

            // 随机返回一句
            return Main.rand.Next(chat);
        }
        public override void SetChatButtons(ref string button, ref string button2) { button = isStaying ? "指令: 跟随" : "指令: 停留"; button2 = "查看属性 / 背包"; }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            Player player = Main.LocalPlayer;
            if (OwnerName != "" && OwnerName != player.name) { Main.npcChatText = $"{MyName} 似乎不听你的话，它的主人是 {OwnerName}。"; return; }
            if (firstButton) { isStaying = !isStaying; attackState = 0; NPC.velocity = Vector2.Zero; NPC.target = -1; NPC.netUpdate = true; Main.npcChatText = isStaying ? $"{MyName} 坐下了。" : $"{MyName} 站起来了。"; }
            else
            {
                Item item = player.HeldItem; bool handled = false;
                if (!item.IsAir)
                {
                    int potionPathway = 0; int potionSequence = 0;
                    if (item.type == ModContent.ItemType<WarriorPotion>()) { potionPathway = 1; potionSequence = 9; }
                    else if (item.type == ModContent.ItemType<PugilistPotion>()) { potionPathway = 1; potionSequence = 8; }
                    else if (item.type == ModContent.ItemType<WeaponMasterPotion>()) { potionPathway = 1; potionSequence = 7; }
                    else if (item.type == ModContent.ItemType<DawnKnightPotion>()) { potionPathway = 1; potionSequence = 6; }
                    else if (item.type == ModContent.ItemType<GuardianPotion>()) { potionPathway = 1; potionSequence = 5; }
                    else if (item.type == ModContent.ItemType<DemonHunterPotion>()) { potionPathway = 1; potionSequence = 4; }
                    else if (item.type == ModContent.ItemType<SilverKnightPotion>()) { potionPathway = 1; potionSequence = 3; }
                    else if (item.type == ModContent.ItemType<GloryPotion>()) { potionPathway = 1; potionSequence = 2; }
                    else if (item.type == ModContent.ItemType<HunterPotion>()) { potionPathway = 2; potionSequence = 9; }
                    else if (item.type == ModContent.ItemType<ProvokerPotion>()) { potionPathway = 2; potionSequence = 8; }
                    else if (item.type == ModContent.ItemType<PyromaniacPotion>()) { potionPathway = 2; potionSequence = 7; }
                    else if (item.type == ModContent.ItemType<ConspiratorPotion>()) { potionPathway = 2; potionSequence = 6; }
                    else if (item.type == ModContent.ItemType<ReaperPotion>()) { potionPathway = 2; potionSequence = 5; }
                    else if (item.type == ModContent.ItemType<IronBloodedKnightPotion>()) { potionPathway = 2; potionSequence = 4; }
                    else if (item.type == ModContent.ItemType<WarBishopPotion>()) { potionPathway = 2; potionSequence = 3; }
                    else if (item.type == ModContent.ItemType<WeatherWarlockPotion>()) { potionPathway = 2; potionSequence = 2; }
                    else if (item.type == ModContent.ItemType<ConquerorPotion>()) { potionPathway = 2; potionSequence = 1; }
                    else if (item.type == ModContent.ItemType<ApothecaryPotion>()) { potionPathway = 3; potionSequence = 9; }
                    else if (item.type == ModContent.ItemType<BeastTamerPotion>()) { potionPathway = 3; potionSequence = 8; }
                    else if (item.type == ModContent.ItemType<VampirePotion>()) { potionPathway = 3; potionSequence = 7; }
                    else if (item.type == ModContent.ItemType<PotionsProfessorPotion>()) { potionPathway = 3; potionSequence = 6; }
                    else if (item.type == ModContent.ItemType<ScarletScholarPotion>()) { potionPathway = 3; potionSequence = 5; }
                    else if (item.type == ModContent.ItemType<WitchKingPotion>()) { potionPathway = 3; potionSequence = 4; }
                    else if (item.type == ModContent.ItemType<SummoningMasterPotion>()) { potionPathway = 3; potionSequence = 3; }
                    else if (item.type == ModContent.ItemType<LifeGiverPotion>()) { potionPathway = 3; potionSequence = 2; }
                    else if (item.type == ModContent.ItemType<BeautyGoddessPotion>()) { potionPathway = 3; potionSequence = 1; }
                    else if (item.type == ModContent.ItemType<SeerPotion>()) { potionPathway = 4; potionSequence = 9; }
                    else if (item.type == ModContent.ItemType<ClownPotion>()) { potionPathway = 4; potionSequence = 8; }
                    else if (item.type == ModContent.ItemType<MagicianPotion>()) { potionPathway = 4; potionSequence = 7; }
                    else if (item.type == ModContent.ItemType<FacelessPotion>()) { potionPathway = 4; potionSequence = 6; }
                    else if (item.type == ModContent.ItemType<MarionettistPotion>()) { potionPathway = 4; potionSequence = 5; }
                    else if (item.type == ModContent.ItemType<BizarroSorcererPotion>()) { potionPathway = 4; potionSequence = 4; }
                    else if (item.type == ModContent.ItemType<ScholarOfYorePotion>()) { potionPathway = 4; potionSequence = 3; }
                    else if (item.type == ModContent.ItemType<MiracleInvokerPotion>()) { potionPathway = 4; potionSequence = 2; }
                    else if (item.type == ModContent.ItemType<AttendantPotion>()) { potionPathway = 4; potionSequence = 1; }
                    else if (item.type == ModContent.ItemType<MarauderPotion>()) { potionPathway = 5; potionSequence = 9; }
                    else if (item.type == ModContent.ItemType<SwindlerPotion>()) { potionPathway = 5; potionSequence = 8; }
                    else if (item.type == ModContent.ItemType<CryptologistPotion>()) { potionPathway = 5; potionSequence = 7; }
                    else if (item.type == ModContent.ItemType<PrometheusPotion>()) { potionPathway = 5; potionSequence = 6; }
                    else if (item.type == ModContent.ItemType<DreamStealerPotion>()) { potionPathway = 5; potionSequence = 5; }
                    else if (item.type == ModContent.ItemType<ParasitePotion>()) { potionPathway = 5; potionSequence = 4; }
                    else if (item.type == ModContent.ItemType<MentorPotion>()) { potionPathway = 5; potionSequence = 3; }
                    else if (item.type == ModContent.ItemType<TrojanHorsePotion>()) { potionPathway = 5; potionSequence = 2; }
                    else if (item.type == ModContent.ItemType<WormOfTimePotion>()) { potionPathway = 5; potionSequence = 1; }
                    // === 太阳途径 (ID: 6) ===
                    else if (item.type == ModContent.ItemType<BardPotion>()) { potionPathway = 6; potionSequence = 9; }
                    else if (item.type == ModContent.ItemType<LightSupplicantPotion>()) { potionPathway = 6; potionSequence = 8; }
                    else if (item.type == ModContent.ItemType<SolarHighPriestPotion>()) { potionPathway = 6; potionSequence = 7; }
                    else if (item.type == ModContent.ItemType<NotaryPotion>()) { potionPathway = 6; potionSequence = 6; }
                    else if (item.type == ModContent.ItemType<PriestPotion>()) { potionPathway = 6; potionSequence = 5; }
                    else if (item.type == ModContent.ItemType<UnshadowedPotion>()) { potionPathway = 6; potionSequence = 4; }
                    else if (item.type == ModContent.ItemType<JusticeMentorPotion>()) { potionPathway = 6; potionSequence = 3; }
                    else if (item.type == ModContent.ItemType<LightSeekerPotion>()) { potionPathway = 6; potionSequence = 2; }
                    else if (item.type == ModContent.ItemType<WhiteAngelPotion>()) { potionPathway = 6; potionSequence = 1; }

                    if (potionPathway > 0)
                    {
                        if (currentSequence == 10)
                        {
                            if (potionSequence == 9)
                            {
                                item.stack--; currentPathway = potionPathway; currentSequence = 9;
                                SoundEngine.PlaySound(SoundID.Item4, NPC.position);
                                Main.npcChatText = $"{MyName} 开启了非凡之路！(途径: {GetPathwayName(currentPathway)} 序列9)";
                                SpawnPromoteEffects(); handled = true;
                            }
                            else { Main.npcChatText = $"{MyName} 身体承受不住高阶魔药！(需从序列9开始)"; handled = true; }
                        }
                        else
                        {
                            if (potionPathway == currentPathway)
                            {
                                if (potionSequence == currentSequence - 1)
                                {
                                    item.stack--; currentSequence = potionSequence;
                                    SoundEngine.PlaySound(SoundID.Item4, NPC.position);
                                    Main.npcChatText = $"{MyName} 晋升成功！(目前: {GetPathwayName(currentPathway)} 序列{currentSequence})";
                                    SpawnPromoteEffects(); handled = true;
                                }
                                else if (potionSequence >= currentSequence) { Main.npcChatText = $"{MyName} 已经消化过该魔药了，喝了浪费。"; handled = true; }
                                else { Main.npcChatText = $"{MyName} 灵性不稳，无法跳级！(需按顺序服用)"; handled = true; }
                            }
                            else { Main.npcChatText = $"{MyName} 无法承受不同途径的冲突！(只能走 {GetPathwayName(currentPathway)} 途径)"; handled = true; }
                        }
                    }
                }

                if (!handled)
                {
                    if (item.type == ItemID.LifeCrystal) { item.stack--; BonusMaxHP += 20; NPC.lifeMax += 20; NPC.life += 20; SoundEngine.PlaySound(SoundID.Item2, NPC.position); Main.npcChatText = $"{MyName} 吞下了生命水晶，感觉身体更结实了！(血量上限 +20)"; handled = true; }
                    else if (item.type == ItemID.LifeFruit) { item.stack--; BonusMaxHP += 10; NPC.lifeMax += 10; NPC.life += 10; SoundEngine.PlaySound(SoundID.Item2, NPC.position); Main.npcChatText = $"{MyName} 吃掉了生命果，充满了自然之力！(血量上限 +10)"; handled = true; }
                    else if (item.buffType > 0 && item.consumable) { NPC.AddBuff(item.buffType, 3600); item.stack--; SoundEngine.PlaySound(SoundID.Item3, NPC.position); Main.npcChatText = $"{MyName} 喝下了药剂，感觉充满了力量！"; handled = true; }
                    else if (item.type == ModContent.ItemType<UnshadowedCross>())
                    {
                        // 只有当狗是序列者 (序列 < 10) 时才生效
                        if (currentSequence < 10)
                        {
                            currentSequence++; // 序列数值+1，代表等级降低 (如 1 -> 2)

                            string msg = $"{MyName} 感到体内的非凡特性正在析出... (降为序列{currentSequence})";

                            // 如果降到了 10，彻底重置
                            if (currentSequence >= 10)
                            {
                                currentSequence = 10;
                                currentPathway = 0; // 0 代表凡人/无途径 (假设你用0表示无)
                                msg = $"{MyName} 彻底洗净了非凡特性，变回了一只普通的修勾。";
                            }

                            // 更新对话框文本
                            Main.npcChatText = msg;

                            // 播放音效 (神圣的声音)
                            SoundEngine.PlaySound(SoundID.Item29, NPC.position);

                            // 生成一些金色粒子特效
                            for (int i = 0; i < 30; i++)
                            {
                                Vector2 speed = Main.rand.NextVector2Circular(3f, 3f);
                                Dust.NewDustPerfect(NPC.Center, DustID.GoldFlame, speed, 100, default, 1.5f).noGravity = true;
                            }
                        }
                        else
                        {
                            Main.npcChatText = $"{MyName} 歪着头看着你：汪？(它已经是凡狗了，没有特性可以析出)";
                        }

                        // 【重要】标记为已处理，但不消耗物品 (不写 item.stack--)
                        handled = true;
                    }


                }

                if (handled && string.IsNullOrEmpty(OwnerName)) OwnerName = player.name;
                if (!handled) { if (DogStatsUI.Visible && DogStatsUI.TargetDog == this) ModContent.GetInstance<DogUISystem>().dogStatsUI.CloseMenu(); else ModContent.GetInstance<DogUISystem>().dogStatsUI.OpenMenu(this); }
            }
        }
        private void SpawnPromoteEffects()
        {
            for (int k = 0; k < 50; k++)
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Enchanted_Gold, 0, -2, 0, default, 2f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 1.5f;
            }
        }
    }
}