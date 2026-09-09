using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.CameraModifiers;
using Microsoft.Xna.Framework;
using System;
using zhashi.Content;

namespace zhashi.Content.DaqianLu
{
    public static class DaqianLuActions
    {
        // ==========================================
        // 核心入口：掷骰子判定
        // ==========================================
        public static void ExecuteSkillWithDice(Player player)
        {
            var dqPlayer = player.GetModPlayer<DaqianLuPlayer>();
            var lotmPlayer = player.GetModPlayer<LotMPlayer>();

            // 1. 检查装备
            if (!dqPlayer.hasDaqianLu) return;

            // 2. 检查冷却 (新增功能)
            if (dqPlayer.daqianLuCooldown > 0)
            {
                // 如果在冷却中按键，提示还剩多少秒，或者直接不理会
                // float secondsLeft = dqPlayer.daqianLuCooldown / 60f;
                // CombatText.NewText(player.getRect(), Color.Gray, $"{secondsLeft:F1}s", true);
                return;
            }

            // 3. 掷骰子
            int diceRoll = Main.rand.Next(1, 101);
            float greedFactor = Main.rand.NextFloat(0.05f, 0.25f);
            SoundEngine.PlaySound(SoundID.Item1, player.position);

            // 设置 10秒 冷却 (600帧)
            dqPlayer.daqianLuCooldown = 600;

            // --- 绝境爆发 (HP < 20%) ---
            if (player.statLife < player.statLifeMax2 * 0.2f)
            {
                HandleDesperateStrike(player, diceRoll);
                return;
            }

            // ==========================================
            // 特殊数字判定 (彩蛋)
            // ==========================================
            if (diceRoll == 100)
            {
                HandleAscension(player, lotmPlayer);
                return;
            }
            if (diceRoll == 1)
            {
                HandleDoom(player, lotmPlayer);
                return;
            }

            if (diceRoll == 88)
            {
                Main.NewText($"[大千录] 🎲 {diceRoll} [发财] 地上长金子了！", Color.Gold);
                SpawnVisuals(player, 1f, Color.Gold);
                CombatText.NewText(player.getRect(), Color.Gold, "金币 +20", true);
                for (int i = 0; i < 20; i++)
                    Item.NewItem(player.GetSource_FromThis(), player.getRect(), ItemID.GoldCoin, 1);
                return;
            }

            if (diceRoll == 44)
            {
                ApplySacrificeDamage(player, player.statLife - 10, "试图理解死亡...");
                Main.NewText($"[大千录] 🎲 {diceRoll} [死] 周围敌人受到 4444 真实伤害", Color.Gray);
                CombatText.NewText(player.getRect(), Color.Gray, "HP -99% / 伤害 4444", true);
                SpawnVisuals(player, 3f, Color.Black);
                foreach (NPC npc in Main.npc)
                {
                    if (npc.active && !npc.friendly && npc.Distance(player.Center) < 800f)
                        npc.StrikeNPC(npc.CalculateHitInfo(4444, 0, true));
                }
                return;
            }

            if (diceRoll == 66)
            {
                Main.NewText($"[大千录] 🎲 {diceRoll} [大顺] 全属性大幅提升 (60秒)", Color.LimeGreen);
                CombatText.NewText(player.getRect(), Color.LimeGreen, "全能 Buff 获取!", true);
                // 给予强力Buff
                player.AddBuff(BuffID.Regeneration, 3600);
                player.AddBuff(BuffID.Ironskin, 3600);
                player.AddBuff(BuffID.Swiftness, 3600);
                player.AddBuff(BuffID.Wrath, 3600);
                return;
            }

            // ==========================================
            // 常规区间判定
            // ==========================================

            if (diceRoll >= 90) // [90-99] 上上签
            {
                int cost = (int)(player.statLifeMax2 * 0.05f);
                // 上上签：伤害倍率 3.0，并且给 10% 伤害加成 Buff (愤怒)
                ProcessSkill(player, lotmPlayer, diceRoll, cost, "天道垂青", Color.Cyan, 2.5f, SkillType.Divine, 3.0f);
            }
            else if (diceRoll >= 60) // [60-89] 上签
            {
                int cost = (int)(player.statLifeMax2 * greedFactor);
                SkillType type = (SkillType)Main.rand.Next(0, 3);
                // 上签：伤害倍率 1.5，并且给 基础防御 Buff (铁皮)
                ProcessSkill(player, lotmPlayer, diceRoll, cost, "以此换之", Color.OrangeRed, 1.5f, type, 1.5f);
            }
            else if (diceRoll >= 30) // [30-59] 中平
            {
                int cost = (int)(player.statLifeMax2 * greedFactor);
                // 中平：伤害倍率 1.0，给 回血 Buff (再生)
                ProcessSkill(player, lotmPlayer, diceRoll, cost, "平平无奇", Color.IndianRed, 1.0f, SkillType.Flesh, 1.0f);
            }
            else if (diceRoll >= 10) // [10-29] 下签
            {
                int cost = (int)(player.statLifeMax2 * 0.15f);
                // 下签：伤害减半，没有 Buff
                ProcessSkill(player, lotmPlayer, diceRoll, cost, "有点亏了", Color.Gray, 0.8f, SkillType.Flesh, 0.5f);
            }
            else // [<10] 大凶
            {
                HandleBacklash(player, lotmPlayer, diceRoll);
            }
        }

        // ==========================================
        // 流派枚举
        // ==========================================
        private enum SkillType { Flesh, Mind, Curse, Divine }

        // ==========================================
        // 通用技能处理核心
        // ==========================================
        private static void ProcessSkill(Player player, LotMPlayer lotm, int roll, int hpCost, string msg, Color color, float intensity, SkillType type, float damageMult = 1.0f)
        {
            // 1. 献祭
            if (!ApplySacrificeDamage(player, hpCost, "把自己献祭给了虚无...")) return;

            // 2. 基础伤害计算
            int weaponDmg = player.GetWeaponDamage(player.HeldItem);
            int sacrificeBonus = (int)(hpCost * 15 * damageMult);
            int totalDamage = weaponDmg + sacrificeBonus;

            lotm.sanityCurrent -= 5f;

            // ==========================================
            // 新增功能：属性加成 Buff (持续 15秒 = 900帧)
            // ==========================================
            string buffText = "";
            if (roll >= 90)
            {
                player.AddBuff(BuffID.Wrath, 900); // 增加 10% 伤害
                player.AddBuff(BuffID.Endurance, 900); // 减少 10% 承伤
                buffText = " | 加成: 伤害+10% / 减伤+10%";
            }
            else if (roll >= 60)
            {
                player.AddBuff(BuffID.Ironskin, 900); // 增加防御
                buffText = " | 加成: 防御+8";
            }
            else if (roll >= 30)
            {
                player.AddBuff(BuffID.Regeneration, 900); // 增加回血
                buffText = " | 加成: 生命再生";
            }

            // 3. 视觉与文本优化 (无星号)
            SpawnVisuals(player, intensity, color);

            string typeName = type switch
            {
                SkillType.Flesh => "[血肉法]",
                SkillType.Mind => "[神识法]",
                SkillType.Curse => "[诅咒法]",
                SkillType.Divine => "[神通]",
                _ => ""
            };

            // 左下角：清晰显示 点数、类型、Buff效果
            Main.NewText($"[大千录] 🎲 {roll} {typeName} {msg}{buffText}", color);

            // 头顶飘字：简洁数值
            CombatText.NewText(player.getRect(), color, $"耗血 {hpCost} / 增伤 {sacrificeBonus}", true);

            // 4. 执行伤害效果
            switch (type)
            {
                case SkillType.Flesh:
                    DoAreaDamage(player, totalDamage, 400f * intensity, DustID.Blood);
                    break;

                case SkillType.Mind:
                    int mindDmg = (int)(totalDamage * 0.7f);
                    DoAreaDamage(player, mindDmg, 600f * intensity, DustID.MagicMirror, (target) => {
                        target.AddBuff(BuffID.Confused, 600);
                        if (lotm.spiritualityCurrent < lotm.spiritualityMax)
                            lotm.spiritualityCurrent += 10;
                    });
                    break;

                case SkillType.Curse:
                    int curseDmg = (int)(totalDamage * 0.8f);
                    DoAreaDamage(player, curseDmg, 500f * intensity, DustID.Shadowflame, (target) => {
                        target.AddBuff(BuffID.ShadowFlame, 600);
                        target.AddBuff(BuffID.Ichor, 600);
                        target.AddBuff(BuffID.CursedInferno, 600);
                    });
                    break;

                case SkillType.Divine:
                    DoAreaDamage(player, totalDamage * 2, 1000f, DustID.GoldCoin, (target) => {
                        target.AddBuff(BuffID.Midas, 1200);
                        target.AddBuff(BuffID.Slow, 600);
                    });
                    break;
            }
        }

        // ==========================================
        // 辅助逻辑
        // ==========================================
        private static void DoAreaDamage(Player player, int damage, float radius, int dustType, Action<NPC> extraEffect = null)
        {
            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && npc.Distance(player.Center) < radius)
                {
                    npc.StrikeNPC(npc.CalculateHitInfo(damage, 0, false, 8f));
                    for (int i = 0; i < 5; i++) Dust.NewDust(npc.position, npc.width, npc.height, dustType);
                    extraEffect?.Invoke(npc);
                }
            }
        }

        private static void HandleAscension(Player player, LotMPlayer lotm)
        {
            SpawnVisuals(player, 5f, Color.White);
            SoundEngine.PlaySound(SoundID.Item29, player.position);

            Main.NewText($"[大千录] 🎲 100 [羽化] 妈！我分清了！我没病！！", Color.Cyan);
            CombatText.NewText(player.getRect(), Color.Cyan, "羽化登仙！", true);

            player.statLife = player.statLifeMax2;
            player.HealEffect(player.statLifeMax2);
            lotm.sanityCurrent = 0;

            // 成仙没有CD，或者CD重置
            player.GetModPlayer<DaqianLuPlayer>().daqianLuCooldown = 0;

            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && npc.Distance(player.Center) < 2000f)
                {
                    npc.StrikeNPC(npc.CalculateHitInfo(99999, 0, true));
                    Dust.NewDust(npc.position, npc.width, npc.height, DustID.Electric);
                }
            }
        }

        private static void HandleDoom(Player player, LotMPlayer lotm)
        {
            if (player.statLife > 1) player.statLife = 1;
            Main.NewText($"[大千录] 🎲 1 [死局] 骰子是活的...它在笑...", Color.Red);
            CombatText.NewText(player.getRect(), Color.Red, "走火入魔...", true);

            SpawnVisuals(player, 4f, Color.Black);
            lotm.sanityCurrent = 0;
            player.AddBuff(BuffID.Obstructed, 180);
            player.AddBuff(BuffID.MoonLeech, 600);
        }

        private static void HandleBacklash(Player player, LotMPlayer lotm, int roll)
        {
            ApplySacrificeDamage(player, 80, "被邪祟吞噬");
            Main.NewText($"[大千录] 🎲 {roll} [大凶] 坐忘道来了！", Color.Purple);
            CombatText.NewText(player.getRect(), Color.Purple, "遭遇反噬!", true);

            SpawnVisuals(player, 2f, Color.Purple);
            lotm.sanityCurrent -= 20f;
            player.AddBuff(BuffID.Confused, 300);
            player.AddBuff(BuffID.Darkness, 600);
        }

        private static void HandleDesperateStrike(Player player, int roll)
        {
            if (roll >= 20)
            {
                int sacrifice = player.statLife - 1;
                int damageBonus = sacrifice * 100 + 5000;
                player.statLife = 1;
                SpawnVisuals(player, 4f, Color.DarkRed);

                Main.NewText($"[大千录] 🎲 {roll} [回光返照] 还没完呢！！", Color.Red);
                CombatText.NewText(player.getRect(), Color.Red, $"绝命一击: {damageBonus}", true);

                DoAreaDamage(player, damageBonus, 1000f, DustID.LifeDrain);
                player.immune = true; player.immuneTime = 120;
            }
            else
            {
                player.KillMe(PlayerDeathReason.ByCustomReason(player.name + " 没能挺过最后一口气..."), 9999, 0);
            }
        }

        private static bool ApplySacrificeDamage(Player player, int damage, string deathMessage)
        {
            if (damage <= 0) return true;
            if (player.statLife <= damage)
            {
                player.statLife = 0;
                SoundEngine.PlaySound(SoundID.NPCDeath1, player.position);
                player.KillMe(PlayerDeathReason.ByCustomReason(player.name + " " + deathMessage), damage, 0);
                return false;
            }
            else
            {
                player.statLife -= damage;
                return true;
            }
        }

        private static void SpawnVisuals(Player player, float intensity, Color color)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                PunchCameraModifier modifier = new PunchCameraModifier(player.Center, (Main.rand.NextFloat() * 6.28f).ToRotationVector2(), 10f * intensity, 6f, 20, 1000f, "DaqianLuShake");
                Main.instance.CameraModifiers.Add(modifier);
            }
            if (intensity > 2f) SoundEngine.PlaySound(SoundID.Roar, player.position);
            else SoundEngine.PlaySound(SoundID.NPCDeath1, player.position);

            int goreCount = (int)(5 * intensity);
            for (int i = 0; i < goreCount; i++) Gore.NewGore(player.GetSource_FromThis(), player.Center, Main.rand.NextVector2Circular(5, 5), 99);

            for (int i = 0; i < 20 * intensity; i++)
            {
                Dust d = Dust.NewDustPerfect(player.Center, DustID.Blood, Main.rand.NextVector2Circular(8, 8) * intensity, 0, color, 1.5f);
                d.noGravity = true;
            }
        }
    }
}