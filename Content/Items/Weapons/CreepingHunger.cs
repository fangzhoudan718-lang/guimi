using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO; // 联机同步需要
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Audio;
using Terraria.Localization;
using zhashi.Content.UI;

namespace zhashi.Content.Items.Weapons
{
    public class CreepingHunger : ModItem
    {
        // === 数据存储 ===
        public int currentSoulNPC = 0;
        private List<int> storedSouls = new List<int>();

        // === 饥饿系统变量 ===
        public bool isFed = false;
        private double lastDayCheck = 0;

        private const int MAX_SOULS = 5;

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.autoReuse = true;

            // 基础伤害 (肉山前)
            Item.damage = 15;

            Item.DamageType = DamageClass.Magic;
            Item.knockBack = 4f;
            Item.mana = 15;
            Item.rare = ItemRarityID.Expert; // 专家稀有度

            Item.shoot = ProjectileID.PurificationPowder;
            Item.shootSpeed = 14f;

            if (storedSouls.Count == 0) storedSouls.Add(0);
        }

        // =========================================================
        // 1. 动态面板成长系统
        // =========================================================
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            // 随着进度提升基础伤害
            if (NPC.downedBoss1) damage.Base = 25; // 克眼后
            if (NPC.downedBoss3) damage.Base = 35; // 骷髅王后
            if (Main.hardMode) damage.Base = 60;   // 肉山后
            if (NPC.downedMechBossAny) damage.Base = 80; // 机械一王后
            if (NPC.downedPlantBoss) damage.Base = 120;  // 花后
            if (NPC.downedMoonlord) damage.Base = 200;   // 月后

            // [灾厄联动] 如果击败了灾厄Boss，还可以继续提升
            if (ModLoader.TryGetMod("CalamityMod", out Mod cal))
            {
                if ((bool)cal.Call("Downed", "providence")) damage.Base = 300; // 亵渎天神后
                if ((bool)cal.Call("Downed", "devourerofgods")) damage.Base = 450; // 神吞后
                if ((bool)cal.Call("Downed", "yharon")) damage.Base = 600; // 犽戎后
            }
        }

        // =========================================================
        // 2. 环境限制 & 使用判定
        // =========================================================
        public override bool CanUseItem(Player player)
        {
            // 限制：蘑菇地禁用
            if (player.ZoneGlowshroom)
            {
                if (player.whoAmI == Main.myPlayer && Main.mouseLeft && !Main.mouseRight)
                {
                    CombatText.NewText(player.getRect(), Color.Blue, "蘑菇孢子抑制了它...", true);
                }
                return false;
            }

            // 限制：正在使用轮盘或右键时禁用攻击
            if (CreepingHungerWheelState.Visible || player.altFunctionUse == 2) return false;

            return true;
        }

        // =========================================================
        // 3. 放牧与管理系统
        // =========================================================

        public bool IsSupportedNPC(int npcId)
        {
            return GetProjectileFromNPC(npcId) != ProjectileID.None || IsSpecialSkillNPC(npcId);
        }

        // 让记录官等其他系统也能复用本能力库的识别
        public static bool IsSupportedNPC_Static(int npcId)
        {
            return GetProjectileFromNPC_Static(npcId) != ProjectileID.None || IsSpecialSkillNPC_Static(npcId);
        }

        public static int GetProjectileFromNPC_Static(int npcId)
        {
            // 直接照搬原版 GetProjectileFromNPC 的 case 列表
            switch (npcId)
            {
                // --- 基础弹幕 ---
                case NPCID.FireImp: return ProjectileID.BallofFire;
                case NPCID.Demon: return ProjectileID.DemonScythe;
                case NPCID.GoblinSorcerer: return ProjectileID.WaterBolt;
                case NPCID.DarkCaster: return ProjectileID.WaterBolt;
                case NPCID.Necromancer: return ProjectileID.ShadowBeamFriendly;
                case NPCID.NecromancerArmored: return ProjectileID.ShadowBeamFriendly;
                case NPCID.RaggedCaster: return ProjectileID.LostSoulFriendly;
                case NPCID.DiabolistRed: return ProjectileID.InfernoFriendlyBolt;
                case NPCID.DiabolistWhite: return ProjectileID.InfernoFriendlyBolt;
                case NPCID.SkeletonSniper: return ProjectileID.SniperBullet;
                case NPCID.TacticalSkeleton: return ProjectileID.ExplosiveBullet;
                case NPCID.IceElemental: return ProjectileID.FrostBlastFriendly;
                case NPCID.IchorSticker: return ProjectileID.GoldenShowerFriendly;
                case NPCID.Mimic: return ProjectileID.GoldCoin;
                case NPCID.IceMimic: return ProjectileID.FrostBoltSword;
                case NPCID.DesertDjinn: return ProjectileID.SpiritFlame;
                case NPCID.SkeletronHead: return ProjectileID.Skull;
                case NPCID.Retinazer: return ProjectileID.DeathLaser;
                case NPCID.Spazmatism: return ProjectileID.CursedFlameFriendly;
                case NPCID.DukeFishron: return ProjectileID.Typhoon;
                case NPCID.CultistBoss: return ProjectileID.CultistBossFireBall;
                case NPCID.GiantCursedSkull: return ProjectileID.Skull;
                case NPCID.Clown: return ProjectileID.HappyBomb;
                case NPCID.Plantera: return ProjectileID.SeedPlantera;
                case NPCID.Golem: return ProjectileID.Fireball;
                case NPCID.RedDevil: return ProjectileID.UnholyTridentFriendly;
                case NPCID.SkeletonCommando: return ProjectileID.RocketSkeleton;
                case NPCID.MourningWood: return ProjectileID.GreekFire1;
                default: return ProjectileID.None;
            }
        }

        public static bool IsSpecialSkillNPC_Static(int npcId)
        {
            switch (npcId)
            {
                // 原著机制
                case NPCID.ChaosElemental:
                case NPCID.Paladin:
                case NPCID.Wraith:
                case NPCID.Nurse:
                case NPCID.BrainofCthulhu:
                case NPCID.QueenBee:
                case NPCID.EaterofWorldsHead:
                case NPCID.WallofFlesh:
                // 趣味/功能
                case NPCID.Bunny:
                case NPCID.GoldBunny:
                case NPCID.Zombie:
                case NPCID.BaldZombie:
                case NPCID.PincushionZombie:
                case NPCID.SlimedZombie:
                case NPCID.UndeadMiner:
                case NPCID.Harpy:
                case NPCID.Medusa:
                case NPCID.Wolf:
                // 实用功能
                case NPCID.ExplosiveBunny:
                case NPCID.BlueSlime:
                case NPCID.GreenSlime:
                case NPCID.PurpleSlime:
                case NPCID.RedSlime:
                case NPCID.YellowSlime:
                case NPCID.BlackSlime:
                case NPCID.MotherSlime:
                case NPCID.Pinky:
                case NPCID.CaveBat:
                case NPCID.JungleBat:
                case NPCID.Hellbat:
                case NPCID.Shark:
                case NPCID.GiantTortoise:
                case NPCID.Unicorn:
                case NPCID.GraniteGolem:
                case NPCID.MeteorHead:
                case NPCID.DoctorBones:
                case NPCID.Drippler:
                case NPCID.Nymph:
                // 强力技能
                case NPCID.KingSlime:
                case NPCID.EyeofCthulhu:
                case NPCID.Tim:
                case NPCID.TaxCollector:
                case NPCID.WyvernHead:
                case NPCID.AngryNimbus:
                case NPCID.MartianSaucer:
                case NPCID.Everscream:
                case NPCID.IceGolem:
                case NPCID.Pixie:
                case NPCID.Mothron:
                    return true;
                default:
                    return false;
            }
        }

        public void AddSoul(int npcId)
        {
            if (storedSouls.Contains(npcId)) return;

            string npcName = Lang.GetNPCNameValue(npcId);

            if (storedSouls.Count <= MAX_SOULS)
            {
                storedSouls.Add(npcId);
                Main.NewText(NetworkText.FromLiteral($"捕获灵魂: {npcName}"), new Color(180, 80, 255));
                SoundEngine.PlaySound(SoundID.NPCDeath6, Main.LocalPlayer.Center);
            }
            else
            {
                Main.NewText(NetworkText.FromLiteral($"五指已满！请先在轮盘(右键)中释放一个灵魂。"), Color.Red);
            }
        }

        public void RemoveSoul(int npcId)
        {
            if (npcId == 0) return;
            if (storedSouls.Contains(npcId))
            {
                storedSouls.Remove(npcId);
                if (currentSoulNPC == npcId) currentSoulNPC = 0;

                string npcName = Lang.GetNPCNameValue(npcId);
                Main.NewText(NetworkText.FromLiteral($"已将 {npcName} 的灵魂放逐到灵界。"), Color.Gray);
                SoundEngine.PlaySound(SoundID.NPCDeath59, Main.LocalPlayer.Center);
            }
        }

        public List<int> GetSouls() => storedSouls;

        // =========================================================
        // 4. 饥饿机制
        // =========================================================

        public void Feed()
        {
            if (!isFed)
            {
                isFed = true;
                SoundEngine.PlaySound(SoundID.Item2, Main.LocalPlayer.Center);
                CombatText.NewText(Main.LocalPlayer.getRect(), Color.Green, "饥饿已平息", true);
            }
        }

        private void CheckDailyReset()
        {
            if (Main.dayTime && Main.time < 3600 && lastDayCheck > 3600)
            {
                isFed = false;
                CombatText.NewText(Main.LocalPlayer.getRect(), Color.Red, "它饿了...", true);
            }
            lastDayCheck = Main.time;
        }

        // =========================================================
        // 5. 交互与攻击逻辑
        // =========================================================

        public override bool AltFunctionUse(Player player) => true;

        public override void HoldItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer) return;

            CheckDailyReset();

            if (Main.mouseRight)
            {
                var uiSystem = ModContent.GetInstance<CreepingHungerUISystem>();
                if (!CreepingHungerWheelState.Visible)
                {
                    uiSystem.OpenWheel(this);
                    SoundEngine.PlaySound(SoundID.MenuOpen);
                }
                player.bodyFrame.Y = player.bodyFrame.Height * 3;
            }
            else
            {
                if (CreepingHungerWheelState.Visible)
                {
                    var uiSystem = ModContent.GetInstance<CreepingHungerUISystem>();
                    int selected = uiSystem.GetState().GetSelectedSoulNPCID();
                    uiSystem.CloseWheel();
                    SoundEngine.PlaySound(SoundID.MenuClose);

                    if (selected != -1 && storedSouls.Contains(selected))
                    {
                        currentSoulNPC = selected;
                        string name = (selected == 0) ? "无" : Lang.GetNPCNameValue(selected);
                        Main.NewText(NetworkText.FromLiteral($"拟态切换: {name}"), Color.Gold);
                    }
                }
            }

            if (!isFed)
            {
                if (Main.rand.NextBool(60))
                {
                    Dust.NewDust(player.position, player.width, player.height, DustID.Blood);
                }
            }
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2) return false;

            if (currentSoulNPC == 0)
            {
                Projectile.NewProjectile(source, position, velocity, ProjectileID.PurificationPowder, 0, 0, player.whoAmI);
                return false;
            }

            // === 饥饿反噬 ===
            if (!isFed)
            {
                player.statLife -= 5;
                CombatText.NewText(player.getRect(), Color.Red, "-5 (祭品)");
                if (player.statLife <= 0)
                {
                    player.KillMe(PlayerDeathReason.ByCustomReason(NetworkText.FromLiteral(player.name + " 被贪婪的手套吞噬了灵魂。")), 1000, 0);
                    return false;
                }
                for (int i = 0; i < 5; i++) Dust.NewDust(position, 10, 10, DustID.Blood);
            }

            // === 技能释放 ===
            if (IsSpecialSkillNPC(currentSoulNPC))
            {
                UseSpecialSkill(player, currentSoulNPC, source, position, velocity, damage, knockback);
                return false;
            }

            int projType = GetProjectileFromNPC(currentSoulNPC);
            if (projType != ProjectileID.None)
            {
                Vector2 finalVelocity = velocity;
                // 弹幕速度修正
                if (projType == ProjectileID.SniperBullet || projType == ProjectileID.ExplosiveBullet || projType == ProjectileID.PinkLaser)
                    finalVelocity *= 2.5f;
                // 红恶魔三叉戟等需要更快的速度
                if (projType == ProjectileID.UnholyTridentFriendly || projType == ProjectileID.RocketSkeleton)
                    finalVelocity *= 1.5f;

                int p = Projectile.NewProjectile(source, position, finalVelocity, projType, damage, knockback, player.whoAmI);
                Main.projectile[p].hostile = false;
                Main.projectile[p].friendly = true;
                Main.projectile[p].DamageType = DamageClass.Magic;

                SoundEngine.PlaySound(SoundID.Item8, position);
            }
            else
            {
                Main.NewText(NetworkText.FromLiteral("这个灵魂似乎失去了力量..."), Color.Gray);
            }

            return false;
        }

        // =========================================================
        // 6. 技能库 (完整列表)
        // =========================================================

        private bool IsSpecialSkillNPC(int npcId)
        {
            // === 【灾厄联动判定 (Calamity Support)】 ===
            // 弱引用：只有在检测到灾厄Mod时才会运行
            if (ModLoader.TryGetMod("CalamityMod", out Mod cal))
            {
                if (cal.TryFind<ModNPC>("SupremeCalamitas", out var scal) && npcId == scal.Type) return true;
                if (cal.TryFind<ModNPC>("DevourerofGodsHead", out var dog) && npcId == dog.Type) return true;
                if (cal.TryFind<ModNPC>("Yharon", out var yharon) && npcId == yharon.Type) return true;
                if (cal.TryFind<ModNPC>("Cryogen", out var cryogen) && npcId == cryogen.Type) return true;
                if (cal.TryFind<ModNPC>("DesertScourgeHead", out var ds) && npcId == ds.Type) return true;
            }
            // ==========================================

            switch (npcId)
            {
                // 原著机制
                case NPCID.ChaosElemental:
                case NPCID.Paladin:
                case NPCID.Wraith:
                case NPCID.Nurse:
                case NPCID.BrainofCthulhu:
                case NPCID.QueenBee:
                case NPCID.EaterofWorldsHead:
                case NPCID.WallofFlesh:

                // 趣味/功能
                case NPCID.Bunny:
                case NPCID.GoldBunny:
                case NPCID.Zombie:
                case NPCID.BaldZombie:
                case NPCID.PincushionZombie:
                case NPCID.SlimedZombie:
                case NPCID.UndeadMiner:
                case NPCID.Harpy:
                case NPCID.Medusa:
                case NPCID.Wolf: // 【新增】狼人

                // 实用功能
                case NPCID.ExplosiveBunny:
                case NPCID.BlueSlime:
                case NPCID.GreenSlime:
                case NPCID.PurpleSlime:
                case NPCID.RedSlime:
                case NPCID.YellowSlime:
                case NPCID.BlackSlime:
                case NPCID.MotherSlime:
                case NPCID.Pinky:
                case NPCID.CaveBat:
                case NPCID.JungleBat:
                case NPCID.Hellbat:
                case NPCID.Shark:
                case NPCID.GiantTortoise:
                case NPCID.Unicorn:
                case NPCID.GraniteGolem:
                case NPCID.MeteorHead:
                case NPCID.DoctorBones:
                case NPCID.Drippler:
                case NPCID.Nymph:

                // 强力技能
                case NPCID.KingSlime:
                case NPCID.EyeofCthulhu:
                case NPCID.Tim:
                case NPCID.TaxCollector:
                case NPCID.WyvernHead:
                case NPCID.AngryNimbus:
                case NPCID.MartianSaucer:
                case NPCID.Everscream:
                case NPCID.IceGolem:
                case NPCID.Pixie:
                case NPCID.Mothron:
                    return true;
                default:
                    return false;
            }
        }

        private int GetProjectileFromNPC(int npcId)
        {
            switch (npcId)
            {
                // --- 基础弹幕 ---
                case NPCID.FireImp: return ProjectileID.BallofFire;
                case NPCID.Demon: return ProjectileID.DemonScythe;
                case NPCID.GoblinSorcerer: return ProjectileID.WaterBolt;
                case NPCID.DarkCaster: return ProjectileID.WaterBolt;
                case NPCID.Necromancer: return ProjectileID.ShadowBeamFriendly;
                case NPCID.NecromancerArmored: return ProjectileID.ShadowBeamFriendly;
                case NPCID.RaggedCaster: return ProjectileID.LostSoulFriendly;
                case NPCID.DiabolistRed: return ProjectileID.InfernoFriendlyBolt;
                case NPCID.DiabolistWhite: return ProjectileID.InfernoFriendlyBolt;
                case NPCID.SkeletonSniper: return ProjectileID.SniperBullet;
                case NPCID.TacticalSkeleton: return ProjectileID.ExplosiveBullet;
                case NPCID.IceElemental: return ProjectileID.FrostBlastFriendly;
                case NPCID.IchorSticker: return ProjectileID.GoldenShowerFriendly;
                case NPCID.Mimic: return ProjectileID.GoldCoin;
                case NPCID.IceMimic: return ProjectileID.FrostBoltSword;
                case NPCID.DesertDjinn: return ProjectileID.SpiritFlame;
                case NPCID.SkeletronHead: return ProjectileID.Skull;
                case NPCID.Retinazer: return ProjectileID.DeathLaser;
                case NPCID.Spazmatism: return ProjectileID.CursedFlameFriendly;
                case NPCID.DukeFishron: return ProjectileID.Typhoon;
                case NPCID.CultistBoss: return ProjectileID.CultistBossFireBall;

                // --- 进阶/新增弹幕 ---
                case NPCID.Pumpking: return ProjectileID.FlamingScythe;
                case NPCID.IceQueen: return ProjectileID.FrostWave;
                case NPCID.PirateCaptain: return ProjectileID.CannonballFriendly;
                case NPCID.GoblinSummoner: return ProjectileID.ShadowFlame;
                case NPCID.RuneWizard: return ProjectileID.RuneBlast;
                case NPCID.Gastropod: return ProjectileID.PinkLaser;
                case NPCID.BlackRecluse: return ProjectileID.WebSpit;
                case NPCID.GiantCursedSkull: return ProjectileID.Skull;
                case NPCID.Clown: return ProjectileID.HappyBomb;

                // --- 【本次新增 9 个弹幕型灵魂】 ---
                case NPCID.Plantera: return ProjectileID.SeedPlantera; // 世纪之花
                case NPCID.Golem: return ProjectileID.Fireball; // 石巨人
                case NPCID.RedDevil: return ProjectileID.UnholyTridentFriendly; // 红恶魔
                case NPCID.SkeletonCommando: return ProjectileID.RocketSkeleton; // 骷髅突击手
                case NPCID.BigMimicHallow: return ProjectileID.CrystalStorm; // 神圣宝箱怪
                case NPCID.BigMimicCorruption: return ProjectileID.CursedFlameFriendly; // 腐化宝箱怪
                case NPCID.BigMimicCrimson: return ProjectileID.IchorSplash; // 猩红宝箱怪
                case NPCID.MourningWood: return ProjectileID.GreekFire1; // 哀木
                case NPCID.SantaNK1: return ProjectileID.Present; // 圣诞坦克

                default: return ProjectileID.None;
            }
        }

        public static void UseSpecialSkill(Player player, int npcId, IEntitySource source, Vector2 position, Vector2 velocity, int damage, float knockback)
        {
            // === 【灾厄联动技能 (Calamity Skills)】 ===
            if (ModLoader.TryGetMod("CalamityMod", out Mod cal))
            {
                // 1. 至尊灾厄 (SCal)
                if (cal.TryFind<ModNPC>("SupremeCalamitas", out var scal) && npcId == scal.Type)
                {
                    SoundEngine.PlaySound(SoundID.Item14, player.Center);
                    int projType = ProjectileID.InfernoFriendlyBolt;
                    if (cal.TryFind<ModProjectile>("BrimstoneHellblastFriendly", out var brimProj)) projType = brimProj.Type;

                    for (int i = -2; i <= 2; i++)
                    {
                        Vector2 perturbedSpeed = velocity.RotatedBy(MathHelper.ToRadians(15 * i));
                        Projectile.NewProjectile(source, position, perturbedSpeed * 1.5f, projType, damage * 5, knockback, player.whoAmI);
                    }
                    return;
                }

                // 2. 神明吞噬者 (DoG)
                if (cal.TryFind<ModNPC>("DevourerofGodsHead", out var dog) && npcId == dog.Type)
                {
                    SoundEngine.PlaySound(SoundID.Item122, player.Center);
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 laserVel = velocity * (2f + i * 0.5f);
                        Projectile.NewProjectile(source, position, laserVel, ProjectileID.LastPrismLaser, damage * 4, knockback, player.whoAmI, 0, 0);
                    }
                    return;
                }

                // 3. 犽戎 (Yharon)
                if (cal.TryFind<ModNPC>("Yharon", out var yharon) && npcId == yharon.Type)
                {
                    SoundEngine.PlaySound(SoundID.Item74, player.Center);
                    for (int i = -1; i <= 1; i += 2)
                    {
                        Projectile.NewProjectile(source, position, velocity.RotatedBy(i * 0.5f), ProjectileID.Typhoon, damage * 3, knockback, player.whoAmI);
                    }
                    player.AddBuff(BuffID.Wrath, 1200);
                    return;
                }

                // 4. 极地之灵 (Cryogen)
                if (cal.TryFind<ModNPC>("Cryogen", out var cryogen) && npcId == cryogen.Type)
                {
                    SoundEngine.PlaySound(SoundID.Item28, player.Center);
                    for (int i = 0; i < 12; i++)
                    {
                        Vector2 shootDir = Vector2.UnitX.RotatedBy(MathHelper.ToRadians(30 * i)) * 10f;
                        Projectile.NewProjectile(source, player.Center, shootDir, ProjectileID.FrostBlastFriendly, damage, knockback, player.whoAmI);
                    }
                    return;
                }

                // 5. 荒漠灾虫 (Desert Scourge)
                if (cal.TryFind<ModNPC>("DesertScourgeHead", out var ds) && npcId == ds.Type)
                {
                    SoundEngine.PlaySound(SoundID.Item1, player.Center);
                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 vel = new Vector2(velocity.X + Main.rand.NextFloat(-2, 2), velocity.Y + Main.rand.NextFloat(-2, 2));
                        Projectile.NewProjectile(source, position, vel, ProjectileID.SandBallFalling, damage, knockback, player.whoAmI);
                    }
                    return;
                }
            }

            switch (npcId)
            {
                // === 【本次新增 - 狼人技能】 ===
                case NPCID.Wolf:
                    {
                        SoundEngine.PlaySound(SoundID.Roar, player.Center);
                        player.AddBuff(BuffID.Werewolf, 1800); // 30秒狼人变身
                        CombatText.NewText(player.getRect(), Color.Gray, "野性回归!");
                        // 视觉特效
                        for (int i = 0; i < 15; i++) Dust.NewDust(player.position, player.width, player.height, DustID.SilverCoin, 0, 0, 0, default, 1.5f);
                    }
                    break;

                // === 原版特殊技能 ===

                case NPCID.Mothron:
                    {
                        Vector2 dashDir = (Main.MouseWorld - player.Center).SafeNormalize(Vector2.Zero);
                        player.velocity = dashDir * 30f;
                        player.SetImmuneTimeForAllTypes(60);
                        SoundEngine.PlaySound(SoundID.Roar, player.Center);
                        for (int i = 0; i < 30; i++) Dust.NewDustPerfect(player.Center, DustID.SolarFlare, dashDir * -Main.rand.NextFloat(4f), 0, default, 2f);
                        foreach (NPC target in Main.ActiveNPCs)
                        {
                            if (target.active && !target.friendly && target.Distance(player.Center) < 200f)
                            {
                                player.ApplyDamageToNPC(target, damage * 4, 20f, 0, false);
                            }
                        }
                    }
                    break;

                case NPCID.KingSlime:
                    {
                        Vector2 teleportPos = Main.MouseWorld - new Vector2(0, 300);
                        if (!Collision.SolidCollision(teleportPos, player.width, player.height))
                        {
                            player.Teleport(teleportPos, 1);
                            player.velocity.Y = 25f;
                            player.AddBuff(BuffID.Featherfall, 120);
                            SoundEngine.PlaySound(SoundID.Item1, player.Center);
                            for (int i = 0; i < 20; i++) Dust.NewDust(player.position, player.width, player.height, DustID.t_Slime, 0, 0, 100, default, 2f);
                        }
                        else { CombatText.NewText(player.getRect(), Color.Red, "空间不足!"); }
                    }
                    break;

                case NPCID.EyeofCthulhu:
                    {
                        SoundEngine.PlaySound(SoundID.NPCDeath1, player.Center);
                        for (int i = 0; i < 4; i++)
                        {
                            Vector2 spawnVel = Main.rand.NextVector2Circular(8f, 8f);
                            int p = Projectile.NewProjectile(source, player.Center, spawnVel, ProjectileID.DemonScythe, damage / 2, knockback, player.whoAmI);
                            Main.projectile[p].DamageType = DamageClass.Magic;
                        }
                    }
                    break;

                case NPCID.Tim:
                    {
                        if (player.statLife > 20)
                        {
                            player.statLife -= 20;
                            player.statMana += 100;
                            player.manaRegenDelay = 0;
                            SoundEngine.PlaySound(SoundID.Item29, player.Center);
                            CombatText.NewText(player.getRect(), Color.Blue, "+100 Mana");
                            CombatText.NewText(player.getRect(), Color.Red, "-20 HP");
                            player.AddBuff(BuffID.MagicPower, 1800);
                        }
                    }
                    break;

                case NPCID.TaxCollector:
                    {
                        SoundEngine.PlaySound(SoundID.Coins, player.Center);
                        int coinAmount = Main.rand.Next(1, 6);
                        player.QuickSpawnItem(source, ItemID.SilverCoin, coinAmount);
                        CombatText.NewText(player.getRect(), Color.Yellow, "征税!");
                    }
                    break;

                case NPCID.WyvernHead:
                    {
                        player.AddBuff(BuffID.Featherfall, 1200);
                        player.AddBuff(BuffID.Swiftness, 1200);
                        player.wingTime = player.wingTimeMax;
                        SoundEngine.PlaySound(SoundID.Item24, player.Center);
                        for (int i = 0; i < 30; i++) Dust.NewDust(player.position, player.width, player.height, DustID.Cloud, 0, 0, 0, default, 1.5f);
                    }
                    break;

                case NPCID.AngryNimbus:
                    {
                        Vector2 cloudPos = Main.MouseWorld;
                        cloudPos.Y -= 200;
                        for (int i = -1; i <= 1; i += 2)
                        {
                            Projectile.NewProjectile(source, cloudPos + new Vector2(i * 30, 0), Vector2.Zero, ProjectileID.RainCloudRaining, damage, 0, player.whoAmI);
                        }
                        SoundEngine.PlaySound(SoundID.Item21, player.Center);
                    }
                    break;

                case NPCID.MartianSaucer:
                    {
                        Vector2 spawnPos = new Vector2(Main.MouseWorld.X, player.Center.Y - 600);
                        Vector2 laserVelocity = new Vector2(0, 25f);
                        Projectile.NewProjectile(source, spawnPos, laserVelocity, ProjectileID.MartianWalkerLaser, damage * 3, knockback * 2, player.whoAmI);
                        SoundEngine.PlaySound(SoundID.Item91, player.Center);
                    }
                    break;

                case NPCID.Everscream:
                    {
                        SoundEngine.PlaySound(SoundID.Item28, player.Center);
                        for (int i = 0; i < 8; i++)
                        {
                            Vector2 needleVel = Vector2.UnitX.RotatedBy(MathHelper.ToRadians(45 * i)) * 12f;
                            Projectile.NewProjectile(source, player.Center, needleVel, ProjectileID.PineNeedleFriendly, damage, knockback, player.whoAmI);
                        }
                    }
                    break;

                case NPCID.IceGolem:
                    {
                        SoundEngine.PlaySound(SoundID.Item30, player.Center);
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            NPC npc = Main.npc[i];
                            if (npc.active && !npc.friendly && npc.Distance(player.Center) < 400f)
                            {
                                npc.AddBuff(BuffID.Frostburn, 300);
                                npc.AddBuff(BuffID.Chilled, 300);
                                Dust.NewDust(npc.position, npc.width, npc.height, DustID.IceTorch);
                            }
                        }
                    }
                    break;

                case NPCID.Pixie:
                    {
                        player.AddBuff(BuffID.DryadsWard, 600);
                        player.AddBuff(BuffID.Shine, 1200);
                        SoundEngine.PlaySound(SoundID.Item4, player.Center);
                        for (int i = 0; i < 20; i++) Dust.NewDust(player.position, player.width, player.height, DustID.Pixie, 0, 0, 0, default, 1.5f);
                    }
                    break;

                case NPCID.ExplosiveBunny:
                    {
                        SoundEngine.PlaySound(SoundID.Item14, player.Center);
                        for (int i = 0; i < 50; i++) Dust.NewDust(player.position, player.width, player.height, DustID.Smoke, 0, 0, 100, default, 2f);
                        for (int i = 0; i < 30; i++) Dust.NewDust(player.position, player.width, player.height, DustID.Torch, 0, 0, 100, default, 3f);

                        foreach (NPC blastTarget in Main.ActiveNPCs)
                        {
                            if (blastTarget.active && !blastTarget.friendly && blastTarget.Distance(player.Center) < 300f)
                            {
                                player.ApplyDamageToNPC(blastTarget, damage * 5, 10f, 0, false);
                            }
                        }
                        player.Hurt(PlayerDeathReason.ByCustomReason(NetworkText.FromLiteral($"{player.name} 像兔子一样爆炸了。")), 50, 0);
                    }
                    break;

                case NPCID.BlueSlime:
                case NPCID.GreenSlime:
                case NPCID.PurpleSlime:
                case NPCID.RedSlime:
                case NPCID.YellowSlime:
                case NPCID.BlackSlime:
                case NPCID.MotherSlime:
                    if (player.statLife > 10)
                    {
                        player.statLife -= 10;
                        CombatText.NewText(player.getRect(), Color.Red, "-10 HP");
                        int gelAmount = Main.rand.Next(5, 11);
                        player.QuickSpawnItem(source, ItemID.Gel, gelAmount);
                        SoundEngine.PlaySound(SoundID.Item1, player.Center);
                    }
                    else
                    {
                        CombatText.NewText(player.getRect(), Color.Red, "生命不足!");
                    }
                    break;

                case NPCID.Pinky:
                    if (player.statLife > 20)
                    {
                        player.statLife -= 20;
                        CombatText.NewText(player.getRect(), Color.Red, "-20 HP");
                        player.QuickSpawnItem(source, ItemID.PinkGel, Main.rand.Next(1, 4));
                        SoundEngine.PlaySound(SoundID.Item1, player.Center);
                    }
                    else
                    {
                        CombatText.NewText(player.getRect(), Color.Red, "生命不足!");
                    }
                    break;

                case NPCID.CaveBat:
                case NPCID.JungleBat:
                case NPCID.Hellbat:
                    player.AddBuff(BuffID.Hunter, 1800);
                    SoundEngine.PlaySound(SoundID.Item4, player.Center);
                    for (int i = 0; i < 20; i++) Dust.NewDust(player.position, player.width, player.height, DustID.Electric, 0, 0, 100, default, 0.5f);
                    break;

                case NPCID.Shark:
                    player.AddBuff(BuffID.Gills, 3600);
                    player.AddBuff(BuffID.Flipper, 3600);
                    SoundEngine.PlaySound(SoundID.Item3, player.Center);
                    break;

                case NPCID.GiantTortoise:
                    player.AddBuff(BuffID.Ironskin, 600);
                    player.AddBuff(BuffID.Endurance, 600);
                    player.AddBuff(BuffID.Thorns, 600);
                    player.AddBuff(BuffID.Slow, 600);
                    SoundEngine.PlaySound(SoundID.Item50, player.Center);
                    break;

                case NPCID.Unicorn:
                    Vector2 unicornDir = (Main.MouseWorld - player.Center).SafeNormalize(Vector2.Zero);
                    player.velocity = unicornDir * 25f;
                    player.SetImmuneTimeForAllTypes(40);
                    SoundEngine.PlaySound(SoundID.Item74, player.Center);
                    for (int i = 0; i < 30; i++) Dust.NewDustPerfect(player.Center, DustID.RainbowMk2, unicornDir * -Main.rand.NextFloat(2f), 0, Color.White, 1.5f);
                    break;

                case NPCID.GraniteGolem:
                    player.AddBuff(BuffID.ObsidianSkin, 600);
                    player.AddBuff(BuffID.Ironskin, 1200);
                    CombatText.NewText(player.getRect(), Color.Gray, "坚如磐石");
                    break;

                case NPCID.MeteorHead:
                    player.AddBuff(BuffID.ObsidianSkin, 3600);
                    SoundEngine.PlaySound(SoundID.Item20, player.Center);
                    break;

                case NPCID.DoctorBones:
                    player.AddBuff(BuffID.Mining, 3600);
                    CombatText.NewText(player.getRect(), Color.Yellow, "考古时间!");
                    break;

                case NPCID.Drippler:
                    {
                        Vector2 skyPos = Main.MouseWorld;
                        skyPos.Y = player.position.Y - 400;
                        for (int i = 0; i < 3; i++)
                        {
                            Vector2 fallSpeed = new Vector2(Main.rand.NextFloat(-2f, 2f), 10f);
                            Projectile.NewProjectile(source, skyPos + new Vector2(Main.rand.Next(-50, 50), 0), fallSpeed, ProjectileID.BloodRain, damage, knockback, player.whoAmI);
                        }
                    }
                    break;

                case NPCID.Nymph:
                    player.AddBuff(BuffID.Invisibility, 1200);
                    player.AddBuff(BuffID.Calm, 1200);
                    SoundEngine.PlaySound(SoundID.Item8, player.Center);
                    break;

                case NPCID.Bunny:
                case NPCID.GoldBunny:
                    player.velocity.Y = -18f;
                    player.AddBuff(BuffID.Featherfall, 240);
                    SoundEngine.PlaySound(SoundID.DoubleJump, player.Center);
                    for (int i = 0; i < 15; i++)
                        Dust.NewDust(player.position, player.width, player.height, DustID.Cloud, 0, 2, 100, default, 1.5f);
                    break;

                case NPCID.Zombie:
                case NPCID.BaldZombie:
                case NPCID.PincushionZombie:
                case NPCID.SlimedZombie:
                    player.AddBuff(BuffID.Ironskin, 1200);
                    SoundEngine.PlaySound(SoundID.Item2, player.Center);
                    for (int i = 0; i < 10; i++) Dust.NewDust(player.position, player.width, player.height, DustID.Stone);
                    break;

                case NPCID.UndeadMiner:
                    player.AddBuff(BuffID.Spelunker, 3600);
                    player.AddBuff(BuffID.NightOwl, 3600);
                    player.AddBuff(BuffID.Shine, 3600);
                    SoundEngine.PlaySound(SoundID.Item29, player.Center);
                    CombatText.NewText(player.getRect(), Color.Yellow, "洞察开启");
                    break;

                case NPCID.Harpy:
                    player.AddBuff(BuffID.Featherfall, 1200);
                    SoundEngine.PlaySound(SoundID.Item32, player.Center);
                    for (int i = 0; i < 5; i++) Dust.NewDust(player.Bottom, player.width, 4, DustID.Harpy);
                    break;

                case NPCID.Medusa:
                    {
                        SoundEngine.PlaySound(SoundID.Item103, player.Center);
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            NPC npc = Main.npc[i];
                            if (npc.active && !npc.friendly && !npc.boss && npc.Distance(player.Center) < 600f)
                            {
                                player.ApplyDamageToNPC(npc, damage * 2, 0f, 0, false);
                                npc.AddBuff(BuffID.Confused, 300);
                                npc.AddBuff(BuffID.Slow, 300);
                                for (float f = 0; f < 1f; f += 0.1f)
                                    Dust.NewDust(Vector2.Lerp(player.Center, npc.Center, f), 1, 1, DustID.Stone);
                            }
                        }
                    }
                    break;

                case NPCID.ChaosElemental:
                    {
                        Vector2 target = Main.MouseWorld;
                        if (Vector2.Distance(player.Center, target) < 900 && !Collision.SolidCollision(target - player.Size / 2, player.width, player.height))
                        {
                            player.Teleport(target, 1);
                            NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, player.whoAmI, target.X, target.Y, 1);
                            SoundEngine.PlaySound(SoundID.Item6, player.Center);
                            player.SetImmuneTimeForAllTypes(30);
                            for (int i = 0; i < 10; i++)
                                Dust.NewDust(player.position, player.width, player.height, DustID.Cloud, 0, 0, 100, default, 1.5f);
                        }
                        else
                        {
                            CombatText.NewText(player.getRect(), Color.Gray, "灵界穿梭失败");
                        }
                    }
                    break;

                case NPCID.Paladin:
                    {
                        player.AddBuff(BuffID.Endurance, 600);
                        player.AddBuff(BuffID.Ironskin, 600);
                        SoundEngine.PlaySound(SoundID.Item29, player.Center);
                        for (int i = 0; i < 30; i++)
                        {
                            Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 30f) * 40f;
                            Dust.NewDustPerfect(player.Center + offset, DustID.GoldFlame, Vector2.Zero).noGravity = true;
                        }
                    }
                    break;

                case NPCID.Nurse:
                    if (player.statMana > 60)
                    {
                        player.statMana -= 60;
                        player.manaRegenDelay = 120;
                        int heal = 40;
                        player.Heal(heal);
                        SoundEngine.PlaySound(SoundID.Item4, player.Center);
                        CombatText.NewText(player.getRect(), Color.Green, $"+{heal}");
                    }
                    else
                    {
                        CombatText.NewText(player.getRect(), Color.Blue, "魔力不足");
                    }
                    break;

                case NPCID.Wraith:
                    player.AddBuff(BuffID.Invisibility, 240);
                    player.AddBuff(BuffID.ShadowDodge, 240);
                    SoundEngine.PlaySound(SoundID.Item8, player.Center);
                    break;

                case NPCID.BrainofCthulhu:
                    {
                        SoundEngine.PlaySound(SoundID.Item103, player.Center);
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            NPC npc = Main.npc[i];
                            if (npc.active && !npc.friendly && npc.Distance(player.Center) < 800f)
                            {
                                player.ApplyDamageToNPC(npc, damage, 0f, 0, false);
                                npc.AddBuff(BuffID.Confused, 180);
                                for (float f = 0; f < 1f; f += 0.05f)
                                    Dust.NewDustPerfect(Vector2.Lerp(player.Center, npc.Center, f), DustID.Crimson, Vector2.Zero).noGravity = true;
                            }
                        }
                    }
                    break;

                case NPCID.EaterofWorldsHead:
                    Projectile.NewProjectile(source, position, velocity, ProjectileID.CorruptSpray, damage, knockback, player.whoAmI);
                    player.AddBuff(BuffID.Thorns, 600);
                    break;

                case NPCID.QueenBee:
                    {
                        SoundEngine.PlaySound(SoundID.Item97, player.Center);
                        for (int i = 0; i < 5; i++)
                        {
                            Vector2 perturbedSpeed = velocity.RotatedByRandom(MathHelper.ToRadians(30));
                            int p = Projectile.NewProjectile(source, position, perturbedSpeed * 0.8f, ProjectileID.Bee, damage / 2, knockback, player.whoAmI);
                            Main.projectile[p].DamageType = DamageClass.Magic;
                        }
                    }
                    break;

                case NPCID.WallofFlesh:
                    if (player.statMana > 50)
                    {
                        player.statMana -= 50;
                        Projectile.NewProjectile(source, position, velocity * 2f, ProjectileID.PurpleLaser, damage * 2, knockback, player.whoAmI);
                        SoundEngine.PlaySound(SoundID.Item33, player.Center);
                    }
                    break;
            }
        }

        // =========================================================
        // 7. 数据保存与同步 (支持联机)
        // =========================================================

        public override void SaveData(TagCompound tag)
        {
            tag["souls"] = storedSouls;
            tag["current"] = currentSoulNPC;
            tag["isFed"] = isFed;
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.ContainsKey("souls"))
                storedSouls = tag.GetList<int>("souls") as List<int>;

            if (tag.ContainsKey("current"))
                currentSoulNPC = tag.GetInt("current");

            if (tag.ContainsKey("isFed"))
                isFed = tag.GetBool("isFed");

            if (storedSouls == null) storedSouls = new List<int>();
            if (storedSouls.Count == 0) storedSouls.Add(0);
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(currentSoulNPC);
            writer.Write(isFed);
            writer.Write(storedSouls.Count);
            foreach (int soulId in storedSouls)
            {
                writer.Write(soulId);
            }
        }

        public override void NetReceive(BinaryReader reader)
        {
            currentSoulNPC = reader.ReadInt32();
            isFed = reader.ReadBoolean();
            int count = reader.ReadInt32();
            storedSouls.Clear();
            for (int i = 0; i < count; i++)
            {
                int soulId = reader.ReadInt32();
                storedSouls.Add(soulId);
            }
        }
    }
}