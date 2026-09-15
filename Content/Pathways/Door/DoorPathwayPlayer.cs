using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using zhashi.Content.Buffs.Door;
using zhashi.Content.Dimensions;
using zhashi.Content.Items.Weapons;
using zhashi.Content.Projectiles.Door;
using zhashi.Content.Systems;
using zhashi.Content.Globals.Door;

namespace zhashi.Content.Pathways.Door
{
    public enum DoorAbility : byte
    {
        SecretSpace,
        Banish,
        SpatialPrison,
        SpaceTear,
        DimensionalSight,
        Reenact,
        TimeSpaceMaze,
        SpaceShatter,
        SecretSealRitual,
        RecordScene,
        ReturnScene
    }

    /// <summary>
    /// 门途径序列4-1。战斗效果由服务器裁定；客户端只提交按键与瞄准位置。
    /// 星门、星匣、封印领域使用自制缓存纹理与分阶段动画。
    /// </summary>
    public class DoorPathwayPlayer : ModPlayer
    {
        private static readonly int[] TrickDustTypes = { DustID.Torch, DustID.IceTorch, DustID.Electric, DustID.Cloud };
        // ── 神性副作用：空间的排斥 ────────────────────────────────
        /// <summary>距离上次「跳」过空间过了多久（tick）。</summary>
        public int teleportSilenceTicks;
        /// <summary>当前被空间排斥的强度：0 = 正常，0.8 = 移速只剩两成。</summary>
        public float spaceRepelStrength;
        private Vector2 curseLastCenter;

        /// <summary>多久不用瞬移就会被压到最慢。</summary>
        public const int CurseGraceTicks = 30 * 60;
        /// <summary>从正常压到最慢要多久。</summary>
        public const int CurseRampTicks = 8 * 60;
        /// <summary>用过瞬移之后，慢慢退回正常要多久。</summary>
        public const int CurseReliefTicks = 12 * 60;
        /// <summary>最慢时损失多少移速（0.8 = 慢 80%）。</summary>
        public const float CurseMaxSlow = 0.8f;

        /// <summary>
        /// 空间的排斥：门后世界不喜欢一直停在原地的你。
        /// 一段时间不用瞬移就会被压慢，用过之后随时间慢慢退回正常。
        /// 判定方式是「位置有没有突然跳一大段」，所以任何跳法都算，也包括别的手段的传送。
        /// </summary>
        private void UpdateSpaceRepel()
        {
            // 位置突然跳一大段（远处的传送门之类）也算用过瞬移；
            // 门槛放到 120 像素，正常跑跳与冲刺够不到，传送够得到。
            if (Vector2.Distance(Player.Center, curseLastCenter) > 120f) teleportSilenceTicks = 0;
            curseLastCenter = Player.Center;

            if (teleportSilenceTicks < int.MaxValue) teleportSilenceTicks++;

            if (teleportSilenceTicks > CurseGraceTicks)
            {
                spaceRepelStrength = Math.Min(CurseMaxSlow,
                    spaceRepelStrength + CurseMaxSlow / CurseRampTicks);
            }
            else
            {
                spaceRepelStrength = Math.Max(0f,
                    spaceRepelStrength - CurseMaxSlow / CurseReliefTicks);
            }
        }

        /// <summary>用过一次瞬移类技能：空间的排斥从此开始慢慢消退。</summary>
        public void NoteTeleportUsed() => teleportSilenceTicks = 0;

        public bool secretSealRitualComplete;
        public byte wandererSceneMask;
        public List<int> travelerLegendBosses = new();
        public bool starKeyPulsarRitualComplete;

        // 空间隐藏是“维持型”状态：没有固定时长，只要不移动、不攻击就能一直留在门后。
        public bool secretSpaceActive;
        public int secretSpaceElapsed;
        private int secretSpaceSettleTimer;
        public Vector2 hiddenSpaceAnchor;
        /// <summary>割离领域是否正在维持；没有时限，关掉或灵性枯竭才结束。</summary>
        public bool spatialPrisonActive;
        /// <summary>刚被裂缝传送过：离开所有裂缝前不再触发，免得两个裂口来回弹。</summary>
        public bool riftTeleportLocked;
        private int exitGraceTimer;
        public int secretSpaceCooldown;
        public int banishCooldown;
        public int spatialPrisonCooldown;
        public int spaceTearCooldown;
        public int dimensionalSightCooldown;
        public int reenactCooldown;
        public int mazeCooldown;
        public int shatterCooldown;
        public bool cardSpatialEfficiency;
        private int feedbackCooldown;
        public bool hasRecordedScene;
        public Vector2 recordedScene;
        private string recordedSceneWorld;
        private string SceneWorld => Main.worldID + ":" + (SubworldSystem.Current?.FullName ?? "Main");

        public override void OnEnterWorld()
        {
            hasRecordedScene = false; recordedSceneWorld = null;
            secretSpaceActive = false;
            secretSpaceElapsed = secretSpaceSettleTimer = 0;
            spatialPrisonActive = false;
            riftTeleportLocked = false;
            exitGraceTimer = 0;
        }

        public const int WandererSceneTarget = 3;

        /// <summary>
        /// 门途径技能强度随序列成长（序列1为最高，序列4为基准）。
        /// 伤害与穿甲这类硬数值统一乘这个系数，避免每个技能各写一套硬编码数字。
        /// </summary>
        public static float SequencePower(int sequence)
        {
            if (sequence <= 1) return 4.2f;   // 星之匙
            if (sequence == 2) return 2.6f;   // 旅法师
            if (sequence == 3) return 1.6f;   // 漫游者
            return 1.0f;                      // 秘法师
        }

        /// <summary>按序列放大后的技能伤害。</summary>
        public static int ScaledDamage(int baseDamage, int sequence)
            => Math.Max(1, (int)MathF.Round(baseDamage * SequencePower(sequence)));

        /// <summary>按序列放大后的穿甲。</summary>
        public static int ScaledArmorPenetration(int baseValue, int sequence)
            => (int)MathF.Round(baseValue * SequencePower(sequence));
        public const int TravelerLegendTarget = 9;

        public override void ResetEffects() => cardSpatialEfficiency = false;

        private int SceneCount => ((wandererSceneMask & 1) != 0 ? 1 : 0) +
                                  ((wandererSceneMask & 2) != 0 ? 1 : 0) +
                                  ((wandererSceneMask & 4) != 0 ? 1 : 0);
        public int WandererSceneCount => SceneCount;
        public int TravelerLegendCount => travelerLegendBosses.Count;
        public bool WandererRitualComplete => SceneCount >= WandererSceneTarget;
        public bool TravelerRitualComplete => travelerLegendBosses.Count >= TravelerLegendTarget;

        public override void SaveData(TagCompound tag)
        {
            tag["DoorSecretSealRitual"] = secretSealRitualComplete;
            tag["DoorWandererScenes"] = (int)wandererSceneMask;
            tag["DoorTravelerLegends"] = travelerLegendBosses;
            tag["DoorStarKeyPulsar"] = starKeyPulsarRitualComplete;
        }

        public override void LoadData(TagCompound tag)
        {
            secretSealRitualComplete = tag.GetBool("DoorSecretSealRitual");
            wandererSceneMask = (byte)tag.GetInt("DoorWandererScenes");
            travelerLegendBosses = tag.ContainsKey("DoorTravelerLegends")
                ? tag.GetList<int>("DoorTravelerLegends").Distinct().Take(TravelerLegendTarget).ToList()
                : new List<int>();
            starKeyPulsarRitualComplete = tag.GetBool("DoorStarKeyPulsar");
        }

        public override void CopyClientState(ModPlayer targetCopy)
        {
            DoorPathwayPlayer clone = (DoorPathwayPlayer)targetCopy;
            clone.secretSealRitualComplete = secretSealRitualComplete;
            clone.wandererSceneMask = wandererSceneMask;
            clone.travelerLegendBosses = new List<int>(travelerLegendBosses);
            clone.starKeyPulsarRitualComplete = starKeyPulsarRitualComplete;
            clone.secretSpaceActive = secretSpaceActive;
        }

        public override void SendClientChanges(ModPlayer clientPlayer)
        {
            DoorPathwayPlayer old = (DoorPathwayPlayer)clientPlayer;
            if (old.secretSealRitualComplete != secretSealRitualComplete ||
                old.wandererSceneMask != wandererSceneMask ||
                old.starKeyPulsarRitualComplete != starKeyPulsarRitualComplete ||
                !old.travelerLegendBosses.SequenceEqual(travelerLegendBosses))
                SyncState();
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) => SyncState(toWho, fromWho);

        public void SyncState(int toWho = -1, int fromWho = -1)
        {
            if (Main.netMode == NetmodeID.SinglePlayer) return;
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)LotMNetMsg.DoorStateSync);
            packet.Write((byte)Player.whoAmI);
            packet.Write(secretSealRitualComplete);
            packet.Write(wandererSceneMask);
            packet.Write((byte)Math.Min(travelerLegendBosses.Count, TravelerLegendTarget));
            for (int i = 0; i < travelerLegendBosses.Count && i < TravelerLegendTarget; i++) packet.Write(travelerLegendBosses[i]);
            packet.Write(starKeyPulsarRitualComplete);
            packet.Write(secretSpaceActive);
            packet.Write((short)Math.Clamp(secretSpaceCooldown, 0, short.MaxValue));
            packet.Write((short)Math.Clamp(banishCooldown, 0, short.MaxValue));
            packet.Write((short)Math.Clamp(spatialPrisonCooldown, 0, short.MaxValue));
            packet.Write((short)Math.Clamp(spaceTearCooldown, 0, short.MaxValue));
            packet.Write((short)Math.Clamp(dimensionalSightCooldown, 0, short.MaxValue));
            packet.Write((short)Math.Clamp(reenactCooldown, 0, short.MaxValue));
            packet.Write((short)Math.Clamp(mazeCooldown, 0, short.MaxValue));
            packet.Write((short)Math.Clamp(shatterCooldown, 0, short.MaxValue));
            packet.Write(hasRecordedScene); packet.WriteVector2(recordedScene);
            packet.WriteVector2(hiddenSpaceAnchor);
            packet.Write(spatialPrisonActive);
            packet.Write((short)exitGraceTimer);
            LotMPlayer lotm = Player.GetModPlayer<LotMPlayer>();
            int normalCount = Math.Min(lotm.recorderNormalList.Count, LotMPlayer.RECORDER_NORMAL_MAX);
            packet.Write((byte)normalCount);
            for (int i = 0; i < normalCount; i++) packet.Write(lotm.recorderNormalList[i]);
            int divineCount = Math.Min(lotm.recorderDivineList.Count, lotm.GetRecorderDivineMax());
            packet.Write((byte)divineCount);
            for (int i = 0; i < divineCount; i++) packet.Write(lotm.recorderDivineList[i]);
            packet.Send(toWho, fromWho);
        }

        public static void ReceiveState(BinaryReader reader, int whoAmI)
        {
            byte playerIndex = reader.ReadByte();
            bool seal = reader.ReadBoolean();
            byte scenes = reader.ReadByte();
            int wireLegendCount = reader.ReadByte();
            List<int> legends = new();
            for (int i = 0; i < wireLegendCount; i++)
            {
                int npcType = reader.ReadInt32();
                if (i < TravelerLegendTarget) legends.Add(npcType);
            }
            bool pulsar = reader.ReadBoolean();
            bool secretHidden = reader.ReadBoolean();
            int secretCooldown = reader.ReadInt16();
            int banishCd = reader.ReadInt16();
            int prisonCd = reader.ReadInt16();
            int tearCd = reader.ReadInt16();
            int sightCd = reader.ReadInt16();
            int reenactCd = reader.ReadInt16();
            int mazeCd = reader.ReadInt16();
            int shatterCd = reader.ReadInt16();
            bool hasScene = reader.ReadBoolean(); Vector2 scenePosition = reader.ReadVector2();
            Vector2 hiddenAnchor = reader.ReadVector2();
            bool prisonActive = reader.ReadBoolean();
            int exitGrace = reader.ReadInt16();
            int wireNormalCount = reader.ReadByte();
            List<int> normalRecords = new();
            for (int i = 0; i < wireNormalCount; i++)
            {
                int npcType = reader.ReadInt32();
                if (i < LotMPlayer.RECORDER_NORMAL_MAX) normalRecords.Add(npcType);
            }
            int wireDivineCount = reader.ReadByte();
            List<int> divineRecords = new();
            for (int i = 0; i < wireDivineCount; i++)
            {
                int npcType = reader.ReadInt32();
                if (i < 20) divineRecords.Add(npcType);
            }

            if (playerIndex >= Main.maxPlayers || (Main.netMode == NetmodeID.Server && playerIndex != whoAmI)) return;
            DoorPathwayPlayer door = Main.player[playerIndex].GetModPlayer<DoorPathwayPlayer>();
            door.secretSealRitualComplete = seal;
            door.wandererSceneMask = (byte)(scenes & 7);
            door.travelerLegendBosses = legends.Distinct().ToList();
            door.starKeyPulsarRitualComplete = pulsar;
            // 客户端只负责提交存档型进度；技能计时由服务器裁定，不能被状态包重置。
            if (Main.netMode != NetmodeID.Server)
            {
                door.secretSpaceActive = secretHidden;
                door.secretSpaceCooldown = Math.Max(0, secretCooldown);
                door.banishCooldown = Math.Max(0, banishCd);
                door.spatialPrisonCooldown = Math.Max(0, prisonCd);
                door.spaceTearCooldown = Math.Max(0, tearCd);
                door.dimensionalSightCooldown = Math.Max(0, sightCd);
                door.reenactCooldown = Math.Max(0, reenactCd);
                door.mazeCooldown = Math.Max(0, mazeCd);
                door.shatterCooldown = Math.Max(0, shatterCd);
                door.hasRecordedScene = hasScene; door.recordedScene = scenePosition;
                door.hiddenSpaceAnchor = hiddenAnchor;
                door.spatialPrisonActive = prisonActive;
                door.exitGraceTimer = Math.Clamp(exitGrace, 0, 30);
            }
            LotMPlayer lotm = Main.player[playerIndex].GetModPlayer<LotMPlayer>();
            lotm.recorderNormalList = normalRecords.Distinct().Take(LotMPlayer.RECORDER_NORMAL_MAX).ToList();
            // 不按接收端“当前序列”截断：加入世界时两个ModPlayer同步包的先后顺序不固定，
            // 否则高序列角色可能在序列包抵达前被错误削成1条神性记录。
            lotm.recorderDivineList = divineRecords.Distinct().Take(20).ToList();
            if (Main.netMode == NetmodeID.Server) door.SyncState(-1, whoAmI);
        }

        public override void PostUpdate()
        {
            LotMPlayer lotm = Player.GetModPlayer<LotMPlayer>();
            TickCooldowns();
            if (secretSpaceActive)
            {
                if (Player.dead || lotm.currentDoorSequence > 4) EndSecretSpace();
                else if (secretSpaceSettleTimer > 0)
                {
                    // 刚跨进门后：让残余惯性自然停下，并让入口对齐到真正停稳的位置。
                    secretSpaceSettleTimer--;
                    Player.velocity.X *= 0.9f;
                    hiddenSpaceAnchor = Player.Center;
                }
                else if (Player.controlUseItem || Player.controlUseTile ||
                    Player.controlLeft || Player.controlRight || Player.controlUp || Player.controlDown ||
                    Player.controlJump || Player.controlMount || Player.controlHook ||
                    Vector2.DistanceSquared(Player.Center, hiddenSpaceAnchor) > 24f * 24f)
                {
                    // 移动、攻击或与场景交互都会把本体拉回现实。
                    EndSecretSpace();
                }
            }

            if (spatialPrisonActive)
            {
                if (Player.dead || lotm.currentDoorSequence > 3) EndSpatialPrison();
                // 永久维持的代价：每秒 1000 灵性，付不出来领域就自行闭合。
                else if (!lotm.TryConsumeSpirituality(1000f / 60f, true))
                {
                    EndSpatialPrison();
                    Feedback("灵性枯竭，割离领域闭合并归还现实。");
                }
            }
            // 离开所有裂缝之前不再触发传送。
            if (riftTeleportLocked && !TouchingAnyRift()) riftTeleportLocked = false;

            // 漫游者仪式：只使用门途径玩家可以独立抵达的场景，避免被其他途径的专属钥匙卡死。
            // 灵界、血月高空、四柱影响区分别代表灵界漫游、危险星空和星界灾变。
            if (lotm.baseDoorSequence == 4)
            {
                byte bit = 0;
                if (SubworldSystem.IsActive<SpiritWorld>()) bit = 1;
                else if (Player.ZoneSkyHeight && Main.bloodMoon) bit = 2;
                else if (Player.ZoneTowerSolar || Player.ZoneTowerVortex ||
                         Player.ZoneTowerNebula || Player.ZoneTowerStardust) bit = 4;
                if (bit != 0 && (wandererSceneMask & bit) == 0)
                {
                    wandererSceneMask |= bit;
                    if (Player.whoAmI == Main.myPlayer)
                        Main.NewText($"【漫游者仪式】已记录危险场景 {SceneCount}/{WandererSceneTarget}", 180, 130, 255);
                    SyncState();
                }
            }

            if (Main.netMode != NetmodeID.Server && lotm.currentDoorSequence <= 9)
                EmitSequenceVisuals(lotm.currentDoorSequence);
        }

        private void EmitSequenceVisuals(int sequence)
        {
            // Passive presence stays sparse: a few quiet motes instead of a permanent particle halo.
            // High-sequence visuals belong to actual abilities.
            if (sequence <= 4) return;
            ulong tick = Main.GameUpdateCount + (ulong)(Player.whoAmI * 3);
            Dust dust;
            if (sequence == 9 && Player.velocity.LengthSquared() > 4f && tick % 14 == 0)
            {
                dust = Dust.NewDustPerfect(Player.Bottom + new Vector2(Main.rand.NextFloat(-9, 9), -2), DustID.GoldCoin, new Vector2(0, -0.4f), 140, default, 0.6f);
                dust.noGravity = true;
            }
            else if (sequence == 8 && tick % 12 == 0)
            {
                int type = TrickDustTypes[Main.rand.Next(TrickDustTypes.Length)];
                dust = Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2Circular(20, 26), type, Main.rand.NextVector2Circular(0.6f, 0.6f), 140, default, 0.65f);
                dust.noGravity = true;
            }
            else if (sequence == 7 && tick % 11 == 0)
            {
                float angle = (float)tick * 0.055f;
                Vector2 orbit = new Vector2((float)Math.Cos(angle) * 32f, (float)Math.Sin(angle) * 12f - 34f);
                dust = Dust.NewDustPerfect(Player.Center + orbit, DustID.GoldCoin, Vector2.Zero, 110, default, 0.75f);
                dust.noGravity = true;
            }
            else if (sequence == 6 && tick % 12 == 0)
            {
                Vector2 glyph = new Vector2(Main.rand.NextFloat(-28, 28), Main.rand.NextFloat(-42, 16));
                dust = Dust.NewDustPerfect(Player.Center + glyph, DustID.Enchanted_Gold, -glyph.SafeNormalize(Vector2.Zero) * 0.25f, 130, default, 0.7f);
                dust.noGravity = true;
            }
            else if (sequence == 5 && tick % 14 == 0)
            {
                dust = Dust.NewDustPerfect(Player.Center - Player.velocity * 2f + Main.rand.NextVector2Circular(16, 28), DustID.PurpleCrystalShard, -Player.velocity * 0.10f, 150, default, 0.7f);
                dust.noGravity = true;
            }
            else if (sequence == 4 && tick % 22 == 0)
            {
                // A short vertical trace behind the player hints at the illusory door; no bright halo.
                for (int i = 0; i < 2; i++)
                {
                    Vector2 rim = new Vector2(Main.rand.NextFloat(-14f, 14f), Main.rand.NextFloat(-40f, 40f));
                    dust = Dust.NewDustPerfect(Player.Center - Player.velocity * 1.2f + rim, DustID.PurpleCrystalShard,
                        -Player.velocity * 0.08f, 160, default, 0.6f);
                    dust.noGravity = true;
                }
            }
            else if (sequence == 3 && tick % 12 == 0)
            {
                dust = Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2Circular(26, 40), DustID.Vortex, -Player.velocity * 0.08f, 150, default, 0.75f);
                dust.noGravity = true;
                Lighting.AddLight(Player.Center, 0.06f, 0.04f, 0.11f);
            }
            else if (sequence == 2 && tick % 10 == 0)
            {
                float angle = (float)tick * 0.09f;
                Vector2 symbol = new Vector2((float)Math.Cos(angle) * 38f, (float)Math.Sin(angle * 2f) * 28f);
                dust = Dust.NewDustPerfect(Player.Center + symbol, DustID.MagicMirror, Vector2.Zero, 130, default, 0.7f);
                dust.noGravity = true;
                Lighting.AddLight(Player.Center, 0.08f, 0.06f, 0.13f);
            }
            else if (sequence <= 1 && tick % 14 == 0)
            {
                // Two star-worms orbit the fixed point; the third slot is spared for real abilities.
                for (int i = 0; i < 2; i++)
                {
                    float angle = (float)tick * 0.075f + MathHelper.TwoPi * i / 2f;
                    Vector2 orbit = new Vector2((float)Math.Cos(angle) * 50f, (float)Math.Sin(angle) * 30f);
                    dust = Dust.NewDustPerfect(Player.Center + orbit, i == 0 ? DustID.GoldCoin : DustID.Vortex, Vector2.Zero, 120, default, 0.8f);
                    dust.noGravity = true;
                }
                Lighting.AddLight(Player.Center, 0.13f, 0.10f, 0.19f);
            }
        }

        private void TickCooldowns()
        {
            if (feedbackCooldown > 0) feedbackCooldown--;
            if (exitGraceTimer > 0) exitGraceTimer--;
            if (secretSpaceActive) secretSpaceElapsed++;
            if (secretSpaceCooldown > 0) secretSpaceCooldown--;
            if (banishCooldown > 0) banishCooldown--;
            if (spatialPrisonCooldown > 0) spatialPrisonCooldown--;
            if (spaceTearCooldown > 0) spaceTearCooldown--;
            if (dimensionalSightCooldown > 0) dimensionalSightCooldown--;
            if (reenactCooldown > 0) reenactCooldown--;
            if (mazeCooldown > 0) mazeCooldown--;
            if (shatterCooldown > 0) shatterCooldown--;
        }

        public override void PostUpdateEquips()
        {
            // 神性副作用：空间的排斥（一段时间不用瞬移就会被压慢）
            UpdateSpaceRepel();

            LotMPlayer lotm = Player.GetModPlayer<LotMPlayer>();
            int seq = lotm.currentDoorSequence;
            // 取「实际生效」的世界倍率：动态世界等级 ×（位格压制的阶段上限）
            float world = BalanceSystem.GetEffectiveWorldMultiplier();

            if (seq <= 4)
            {
                Player.statLifeMax2 += (int)(300 * world);
                Player.statManaMax2 += 300;
                Player.statDefense += 25;
                Player.GetDamage(DamageClass.Generic) += 0.20f;
                Player.GetDamage(DamageClass.Magic) += 0.25f;
                Player.GetAttackSpeed(DamageClass.Generic) += 0.15f;
                Player.endurance += 0.10f;
                Player.aggro -= 800;
                Player.armorEffectDrawShadow = true; // “幻化”后的门后残影
                Player.buffImmune[BuffID.Confused] = true;
                Player.buffImmune[BuffID.Blackout] = true;
            }
            if (seq <= 3)
            {
                Player.statLifeMax2 += (int)(600 * world);
                Player.statManaMax2 += 500;
                Player.statDefense += 25;
                Player.GetDamage(DamageClass.Generic) += 0.30f;
                Player.GetAttackSpeed(DamageClass.Generic) += 0.20f;
                Player.endurance += 0.12f;
                Player.noFallDmg = true;
                Player.noKnockback = true;
                Player.ignoreWater = true;
                Player.accFlipper = true;
                Player.gills = true;
            }
            if (seq <= 2)
            {
                Player.statLifeMax2 += (int)(1000 * world);
                Player.statManaMax2 += 1000;
                Player.statDefense += 40;
                Player.GetDamage(DamageClass.Generic) += 0.40f;
                Player.GetCritChance(DamageClass.Generic) += 25;
                Player.endurance += 0.15f;
                Player.moveSpeed += 0.45f;
            }
            if (seq <= 1)
            {
                Player.statLifeMax2 += (int)(2500 * world);
                Player.statManaMax2 += 1800;
                Player.statDefense += 80;
                Player.GetDamage(DamageClass.Generic) += 0.80f;
                Player.GetCritChance(DamageClass.Generic) += 40;
                Player.GetAttackSpeed(DamageClass.Generic) += 0.35f;
                Player.endurance += 0.20f;
                Player.noKnockback = true;
                Player.buffImmune[BuffID.ChaosState] = true;
                Player.buffImmune[BuffID.Frozen] = true;
                Player.buffImmune[BuffID.Webbed] = true;
            }

            if (secretSpaceActive)
            {
                Player.invis = true;
                Player.aggro -= 3000;
                Player.endurance += 0.25f;
                Player.moveSpeed += 0.35f;
            }
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (Player.whoAmI != Main.myPlayer || Player.dead || Player.GetModPlayer<LotMPlayer>().currentDoorSequence > 5) return;
            if (secretSpaceActive && (LotMKeybinds.Door_OpenDoor.JustPressed || LotMKeybinds.Door_TrickCast.JustPressed ||
                LotMKeybinds.Door_Astrology.JustPressed || LotMKeybinds.Door_RecordNormal.JustPressed ||
                LotMKeybinds.Door_RecordDivine.JustPressed || LotMKeybinds.Door_TravelerGate.JustPressed || LotMKeybinds.Door_Blink.JustPressed))
                RequestAbility(DoorAbility.SecretSpace);
            if (LotMKeybinds.Door_SecretSpace.JustPressed) RequestAbility(DoorAbility.SecretSpace);
            if (LotMKeybinds.Door_Banish.JustPressed) RequestAbility(DoorAbility.Banish);
            if (LotMKeybinds.Door_SpatialPrison.JustPressed) RequestAbility(DoorAbility.SpatialPrison);
            if (LotMKeybinds.Door_SpaceTear.JustPressed) RequestAbility(DoorAbility.SpaceTear);
            if (LotMKeybinds.Door_DimensionalSight.JustPressed) RequestAbility(DoorAbility.DimensionalSight);
            if (LotMKeybinds.Door_Reenact.JustPressed)
            {
                bool shift = Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift);
                bool control = Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) || Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightControl);
                RequestAbility(shift ? DoorAbility.RecordScene : control ? DoorAbility.ReturnScene : DoorAbility.Reenact);
            }
            if (LotMKeybinds.Door_TimeSpaceMaze.JustPressed) RequestAbility(DoorAbility.TimeSpaceMaze);
            if (LotMKeybinds.Door_SpaceShatter.JustPressed) RequestAbility(DoorAbility.SpaceShatter);
        }

        public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot) => !secretSpaceActive && exitGraceTimer <= 0;
        public override bool CanBeHitByProjectile(Projectile projectile) => !secretSpaceActive && exitGraceTimer <= 0;

        private void RequestAbility(DoorAbility ability)
        {
            float viewportRadius = CurrentViewportWorldDiagonal();
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write((byte)LotMNetMsg.DoorAbilityRequest);
            packet.Write((byte)ability);
                packet.WriteVector2(Main.MouseWorld);
                packet.Write((short)Player.GetModPlayer<LotMPlayer>().recorderSelectedNormal);
                packet.Write((short)Player.GetModPlayer<LotMPlayer>().recorderSelectedDivine);
                packet.Write((ushort)Math.Clamp((int)MathF.Ceiling(viewportRadius), 800, 6000));
                packet.Send();
            }
            else ExecuteAbility(ability, Main.MouseWorld, viewportRadius);
        }

        private static float CurrentViewportWorldDiagonal()
        {
            float zoomX = Math.Max(0.35f, Math.Abs(Main.GameViewMatrix.Zoom.X));
            float zoomY = Math.Max(0.35f, Math.Abs(Main.GameViewMatrix.Zoom.Y));
            float width = Main.screenWidth / zoomX;
            float height = Main.screenHeight / zoomY;
            return MathHelper.Clamp(MathF.Sqrt(width * width + height * height), 800f, 6000f);
        }

        public void RequestSecretSealRitual() => RequestAbility(DoorAbility.SecretSealRitual);

        public static void ReceiveAbilityRequest(BinaryReader reader, int whoAmI)
        {
            DoorAbility ability = (DoorAbility)reader.ReadByte();
            Vector2 target = reader.ReadVector2();
            int normalIndex = reader.ReadInt16();
            int divineIndex = reader.ReadInt16();
            float viewportRadius = Math.Clamp((int)reader.ReadUInt16(), 800, 6000);
            if (Main.netMode != NetmodeID.Server || whoAmI < 0 || whoAmI >= Main.maxPlayers || !Main.player[whoAmI].active) return;
            Player player = Main.player[whoAmI];
            if (player.dead || !Enum.IsDefined(typeof(DoorAbility), ability)) return;
            LotMPlayer lotm = player.GetModPlayer<LotMPlayer>();
            lotm.recorderSelectedNormal = Math.Clamp(normalIndex, 0, Math.Max(0, lotm.recorderNormalList.Count - 1));
            lotm.recorderSelectedDivine = Math.Clamp(divineIndex, 0, Math.Max(0, lotm.recorderDivineList.Count - 1));
            if (!float.IsFinite(target.X) || !float.IsFinite(target.Y)) target = player.Center;
            target.X = MathHelper.Clamp(target.X, 16f, Main.maxTilesX * 16f - 16f);
            target.Y = MathHelper.Clamp(target.Y, 16f, Main.maxTilesY * 16f - 16f);
            player.GetModPlayer<DoorPathwayPlayer>().ExecuteAbility(ability, target, viewportRadius);
        }

        private NPC FindTarget(Vector2 point, float maxRange, bool allowBoss = true)
        {
            NPC best = null;
            float bestDistance = 260f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy() || (!allowBoss && npc.boss) || npc.Distance(Player.Center) > maxRange) continue;
                float d = npc.Distance(point);
                if (d < bestDistance) { bestDistance = d; best = npc; }
            }
            return best;
        }

        /// <summary>
        /// 放逐也能作用在敌对玩家身上。为避免误伤，只有双方都开着PvP时才会被选中。
        /// </summary>
        private Player FindPlayerTarget(Vector2 point, float maxRange)
        {
            if (Main.netMode == NetmodeID.SinglePlayer || !Player.hostile) return null;
            Player best = null;
            float bestDistance = 260f;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player other = Main.player[i];
                if (i == Player.whoAmI || !other.active || other.dead || !other.hostile) continue;
                if (other.Distance(Player.Center) > maxRange) continue;
                float distance = other.Distance(point);
                if (distance < bestDistance) { bestDistance = distance; best = other; }
            }
            return best;
        }

        private bool Spend(LotMPlayer lotm, float cost, int cooldown)
        {
            if (cooldown > 0) { Feedback($"能力尚在恢复：{cooldown / 60f:F1}秒。"); return false; }
            if (!lotm.TryConsumeSpirituality(AdjustedCost(cost)))
            { Feedback($"灵性不足，需要{AdjustedCost(cost):F0}点。"); return false; }
            return true;
        }

        private void Feedback(string message)
        {
            if (feedbackCooldown > 0) return;
            feedbackCooldown = 45;
            Announce(message);
        }

        private int SkillDamage(int damage) => (int)Player.GetDamage(DamageClass.Magic).ApplyTo(damage);

        private void CreateDomain(Vector2 position, int kind, float radius, int ticks)
        {
            // A reset item must not turn persistent fields into an unbounded projectile factory.
            foreach (Projectile old in Main.ActiveProjectiles)
                if (old.owner == Player.whoAmI && old.type == ModContent.ProjectileType<DoorDomainProjectile>() && (int)old.ai[0] == kind) old.Kill();
            int id = Projectile.NewProjectile(Player.GetSource_FromThis(), position, Vector2.Zero,
                ModContent.ProjectileType<DoorDomainProjectile>(), 0, 0, Player.whoAmI, kind, radius);
            if (id < Main.maxProjectiles) { Main.projectile[id].timeLeft = ticks; Main.projectile[id].netUpdate = true; }
        }

        private float AdjustedCost(float cost) => cardSpatialEfficiency ? cost * 0.8f : cost;
        private void Refund(LotMPlayer lotm, float cost) => lotm.spiritualityCurrent = Math.Min(lotm.spiritualityMax, lotm.spiritualityCurrent + AdjustedCost(cost));

        private void ExecuteAbility(DoorAbility ability, Vector2 target, float viewportRadius)
        {
            LotMPlayer lotm = Player.GetModPlayer<LotMPlayer>();
            int seq = lotm.currentDoorSequence;
            if (seq > 5 || !Player.active || Player.dead || Main.netMode == NetmodeID.MultiplayerClient) return;
            if (!float.IsFinite(target.X) || !float.IsFinite(target.Y)) return;
            Vector2 offset = target - Player.Center;
            if (offset.LengthSquared() > 1200 * 1200) target = Player.Center + offset.SafeNormalize(Vector2.UnitX) * 1200;
            target.X = MathHelper.Clamp(target.X, 32, Main.maxTilesX * 16 - 32);
            target.Y = MathHelper.Clamp(target.Y, 32, Main.maxTilesY * 16 - 32);
            if (ability != DoorAbility.SecretSpace && secretSpaceActive) EndSecretSpace();

            switch (ability)
            {
                case DoorAbility.RecordScene when seq <= 2:
                    if (Collision.SolidCollision(Player.position, Player.width, Player.height))
                    { Feedback("当前场景被实体方块占据，无法记录。"); return; }
                    recordedScene = Player.position; recordedSceneWorld = SceneWorld; hasRecordedScene = true;
                    SpawnDoorVisual(Player.Center, 1);
                    Feedback("【场景记录】已记下当前位置；Ctrl+再现键回到此处，仅在本次世界中有效。");
                    break;

                case DoorAbility.ReturnScene when seq <= 2:
                    if (!hasRecordedScene || recordedSceneWorld != SceneWorld ||
                        !float.IsFinite(recordedScene.X) || !float.IsFinite(recordedScene.Y) ||
                        recordedScene.X < 32 || recordedScene.Y < 32 ||
                        recordedScene.X + Player.width > Main.maxTilesX * 16 - 32 || recordedScene.Y + Player.height > Main.maxTilesY * 16 - 32 ||
                        Collision.SolidCollision(recordedScene, Player.width, Player.height))
                    { Feedback("记录的场景不存在于当前空间，或落点已被方块阻塞，请重新记录。"); return; }
                    if (!Spend(lotm, 700, reenactCooldown)) return;
                    SpawnDoorVisual(Player.Center, 1);
                    Player.Teleport(recordedScene, 1);
                    NoteTeleportUsed();
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, Player.whoAmI, recordedScene.X, recordedScene.Y, 1);
                    SpawnDoorVisual(Player.Center, 1);
                    reenactCooldown = 12 * 60;
                    break;

                case DoorAbility.SecretSealRitual when lotm.baseDoorSequence == 5:
                {
                    NPC npc = null;
                    float nearest = 800f;
                    foreach (NPC candidate in Main.ActiveNPCs)
                    {
                        float distance = candidate.Distance(Player.Center);
                        if (candidate.CanBeChasedBy() && candidate.boss &&
                            candidate.life <= candidate.lifeMax * 0.20f && distance < nearest)
                        {
                            npc = candidate;
                            nearest = distance;
                        }
                    }
                    if (npc == null)
                    {
                        Announce("【秘法师仪式】需在附近锁定一个生命低于20%、对你有敌意的Boss，再使用秘法师魔药启动封印。");
                        return;
                    }
                    npc.AddBuff(ModContent.BuffType<SpatialPrisonBuff>(), 3 * 60);
                    npc.netUpdate = true;
                    secretSealRitualComplete = true;
                    SpawnDoorVisual(npc.Center, 1);
                    Announce("【秘法师仪式完成】魔药化为封印媒介，敌对半神的投影已被封入隐藏空间；再次饮用即可晋升。");
                    break;
                }

                case DoorAbility.SecretSpace when seq <= 4:
                    if (secretSpaceActive) { EndSecretSpace(); break; }
                    if (!Spend(lotm, 300, secretSpaceCooldown)) return;
                    secretSpaceActive = true;
                    NoteTeleportUsed();
                    secretSpaceElapsed = 0;
                    secretSpaceSettleTimer = 20;
                    hiddenSpaceAnchor = Player.Center;
                    Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero,
                        ModContent.ProjectileType<DoorRefugeVisual>(), 0, 0, Player.whoAmI);
                    Announce("【空间隐藏】门后空间会一直维持：只要不移动、不攻击、不使用技能就能无限隐匿；再次按下按键返回现实。");
                    break;

                case DoorAbility.Banish when seq <= 4:
                {
                    if (!SpiritBanishSystem.CanBanishFromHere)
                    { Feedback("身处灵界时无法再开启放逐之门。"); return; }

                    NPC npc = FindTarget(target, 1200f);
                    Player victim = npc == null ? FindPlayerTarget(target, 1200f) : null;
                    if (npc == null && victim == null)
                    { Feedback("将鼠标指向1200像素以内的敌人；对玩家需要双方都开启PvP。"); return; }
                    if (!Spend(lotm, 450, banishCooldown)) return;
                    banishCooldown = 18 * 60;

                    Vector2 doorway = npc != null ? npc.Center : victim.Center;
                    SpawnDoorVisual(doorway, 2);                       // 开门（DoorOpen）

                    if (npc != null)
                    {
                        NPC root = SpiritBanishSystem.BanishRoot(npc);
                        bool boss = DoorSpatialGlobalNPC.IsBossBody(root);
                        if (boss && !BossDefeatRegistry.IsDefeated(root.type))
                        {
                            // 还没打赢过的Boss是“第一次面对”的位格：放逐不成立，只能硬控两秒。
                            DoorSpatialGlobalNPC.HardStunBody(root, 120);
                            SpawnDoorVisual(doorway, 2, 60f);          // 门扉反复开合却咬不住它
                            SpawnDoorVisual(doorway, 3, 120f);         // 硬控结束，关门（DoorClosed）
                            Announce("【放逐抵抗】尚未击败过的Boss以位格硬抗乱流，被门扉硬控2秒；它无法被放逐。");
                            break;
                        }
                        SpawnDoorVisual(doorway, 3, 26f);              // 吞下目标后再关门（DoorClosed）
                        bool moved = SpiritBanishSystem.Banish(root, Player);
                        Announce(moved
                            ? $"【放逐】{root.GivenOrTypeName} 被门扉丢进灵界，主世界再没有它的踪迹。"
                            : "【放逐】灵界无法接收这个目标。");
                        break;
                    }

                    SpawnDoorVisual(doorway, 3, 26f);
                    SpiritBanishSystem.SendPlayerToSpiritWorld(victim.whoAmI);
                    Announce($"【放逐】{victim.name} 被门扉送往灵界。");
                    break;
                }

                case DoorAbility.SpatialPrison:
                {
                    if (seq > 3) return;
                    if (spatialPrisonActive)
                    {
                        EndSpatialPrison();
                        Announce("【空间牢笼】割离领域散去，被切开的那块空间归还原位。");
                        break;
                    }
                    if (!Spend(lotm, 500, spatialPrisonCooldown)) return;
                    // 500×500 的圆形割离领域，中心落在鼠标所指的位置，一直维持到主动关闭或灵性枯竭。
                    CreateDomain(target, 0, 250, 600);
                    spatialPrisonActive = true;
                    spatialPrisonCooldown = 18 * 60;
                    Announce("【空间牢笼】现实中切下一块直径500像素的领域，圈内生物可以行动但出不去；每秒消耗1000灵性，再次按键关闭。");
                    break;
                }

                case DoorAbility.SpaceTear when seq <= 3:
                {
                    if (!Spend(lotm, 500, spaceTearCooldown)) return;
                    // 裂缝开在鼠标所指的位置，长轴与“玩家→鼠标”方向垂直，像一道横切空间的裂口。
                    Vector2 riftDirection = (target - Player.Center).SafeNormalize(Vector2.UnitX);
                    int riftDamage = ScaledDamage(500, seq);
                    Projectile.NewProjectile(Player.GetSource_FromThis(), target, Vector2.Zero,
                        ModContent.ProjectileType<SpatialRiftProjectile>(), SkillDamage(riftDamage), 8f, Player.whoAmI,
                        riftDirection.ToRotation() + MathHelper.PiOver2, seq <= 1 ? 190 : seq == 2 ? 155 : 120);
                    spaceTearCooldown = 2 * 60;
                    break;
                }

                case DoorAbility.DimensionalSight when seq <= 2:
                {
                    int released = 0;
                    bool hasToy = false;
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        var spatial = npc.GetGlobalNPC<DoorSpatialGlobalNPC>();
                        if (!spatial.HasOwnedToy(Player.whoAmI)) continue;
                        hasToy = true;
                        Vector2 destination = target + new Vector2((released % 5 - 2) * 24, -(released / 5) * 24);
                        if (spatial.ReleasePocket(npc, destination)) released++;
                    }
                    if (hasToy)
                    {
                        // Close the capture field so releasing cannot immediately collect new enemies.
                        foreach (Projectile old in Main.ActiveProjectiles)
                            if (old.owner == Player.whoAmI && old.type == ModContent.ProjectileType<DoorDomainProjectile>() && old.ai[0] == 1) old.Kill();
                        if (released > 0) SpawnDoorVisual(target, 3);
                        Feedback(released > 0 ? $"【维度之视】已把{released}个玩具挪到鼠标处，缩小与虚弱效果继续；不追加消耗。" : "那里被方块占住，换个空地再按一次。");
                        break;
                    }
                    if (!Spend(lotm, 500, dimensionalSightCooldown)) return;
                    CreateDomain(target, 1, seq <= 1 ? 340 : 280, 240);
                    dimensionalSightCooldown = 13 * 60;
                    Announce("【维度之视】鼠标处张开维度竖瞳；普通敌人先被收进空间口袋、随后持续缩小，再次按键可把玩具挪到鼠标处。Boss仅被弱化。");
                    break;
                }

                case DoorAbility.Reenact when seq <= 2:
                {
                    bool divine = lotm.recorderDivineList.Count > 0;
                    List<int> records = divine ? lotm.recorderDivineList : lotm.recorderNormalList;
                    if (records.Count == 0) { Feedback("再现需要至少一条普通或神性记录。"); return; }
                    if (!Spend(lotm, 700, reenactCooldown)) return;
                    int selected = Math.Clamp(divine ? lotm.recorderSelectedDivine : lotm.recorderSelectedNormal, 0, records.Count - 1);
                    int echoes = Math.Min(seq <= 1 ? 3 : 2, records.Count);
                    foreach (Projectile old in Main.ActiveProjectiles)
                        if (old.owner == Player.whoAmI && old.type == ModContent.ProjectileType<DoorRecordedEcho>()) old.Kill();
                    for (int i = 0; i < echoes; i++)
                    {
                        Vector2 position = Player.Center + new Vector2((i - (echoes - 1) / 2f) * 62, -48);
                        float angle = (target - position).SafeNormalize(Vector2.UnitX).ToRotation();
                        Projectile.NewProjectile(Player.GetSource_FromThis(), position, Vector2.Zero,
                            ModContent.ProjectileType<DoorRecordedEcho>(), SkillDamage(ScaledDamage(700, seq) / echoes), 0, Player.whoAmI,
                            records[(selected + i) % records.Count], angle);
                    }
                    reenactCooldown = 12 * 60;
                    Announce($"【再现】{echoes}位记录投影各发动一次对应动作，首位：{DoorEchoCatalog.GetName(records[selected])}。");
                    break;
                }

                case DoorAbility.TimeSpaceMaze when seq <= 1:
                    if (!Spend(lotm, 10000, mazeCooldown)) return;
                    CreateDomain(target, 2, 480, 600);
                    mazeCooldown = 26 * 60;
                    Announce("【时空迷宫】十二扇门围成圆环，圈内所有生物——包括Boss——都会像钟表指针一样被拨着绕圆心旋转。");
                    break;

                case DoorAbility.SpaceShatter when seq <= 1:
                    if (!Spend(lotm, 50000, shatterCooldown)) return;
                    foreach (Projectile old in Main.ActiveProjectiles)
                        if (old.owner == Player.whoAmI && old.type == ModContent.ProjectileType<SpaceShatterProjectile>()) old.Kill();
                    Projectile.NewProjectile(Player.GetSource_FromThis(), target, Vector2.Zero,
                        ModContent.ProjectileType<SpaceShatterProjectile>(), SkillDamage(ScaledDamage(4500, seq)), 14f,
                        Player.whoAmI, 0f, MathHelper.Clamp(viewportRadius, 800f, 6000f));
                    shatterCooldown = 120 * 60;
                    Announce("【空间破碎】整个屏幕内的生物、弹幕与物块都被粉碎并拖向奇点。请勿在家中使用。");
                    break;

                default:
                    return;
            }

            if (Main.netMode == NetmodeID.Server)
            {
                lotm.SyncPlayer(-1, -1, false);
                SyncState();
            }
        }

        /// <summary>
        /// 退出空间隐藏：门在原地合上，本体回到现实并获得 0.5 秒回归保护。
        /// 隐藏时长不设上限，所以冷却从“退出”这一刻才开始计算。
        /// </summary>
        private void EndSecretSpace()
        {
            if (!secretSpaceActive) return;
            secretSpaceActive = false;
            secretSpaceSettleTimer = 0;
            // Stepping back into the world grants a moment of grace: the body is arriving
            // from "behind the door", so it does not eat a contact hit on the same frame.
            exitGraceTimer = 30;
            secretSpaceCooldown = 22 * 60;
            if (Main.netMode != NetmodeID.MultiplayerClient) SyncState();
        }

        /// <summary>关闭割离领域：圆盘自己收束淡出，这里只翻状态。</summary>
        private void EndSpatialPrison()
        {
            if (!spatialPrisonActive) return;
            spatialPrisonActive = false;
            if (Main.netMode != NetmodeID.MultiplayerClient) SyncState();
        }

        /// <summary>玩家是否还踩在某条裂缝里；离开之前不会再次被传送，免得两个裂口互弹。</summary>
        private bool TouchingAnyRift()
        {
            foreach (Projectile rift in Main.ActiveProjectiles)
            {
                if (rift.type != ModContent.ProjectileType<SpatialRiftProjectile>()) continue;
                if (SpatialRiftProjectile.TouchesPlayer(rift, Player)) return true;
            }
            return false;
        }

        private void Announce(string message)
        {
            if (Main.netMode == NetmodeID.Server)
                Terraria.Chat.ChatHelper.SendChatMessageToClient(Terraria.Localization.NetworkText.FromLiteral(message), new Color(215, 185, 255), Player.whoAmI);
            else Main.NewText(message, 215, 185, 255);
        }

        private void SpawnDoorVisual(Vector2 position, int style)
            => SpawnDoorVisual(position, style, 0f);

        /// <summary>delayTicks 让“开门 → 目标被吞下 → 关门”成为读得出来的一串动作。</summary>
        private void SpawnDoorVisual(Vector2 position, int style, float delayTicks)
        {
            Projectile.NewProjectile(Player.GetSource_FromThis(), position, Vector2.Zero,
                ModContent.ProjectileType<DoorAuthorityVisual>(), 0, 0f, Player.whoAmI, style, delayTicks);
        }

        public bool IsBeyondMundaneWorld() => Player.ZoneSkyHeight || SubworldSystem.Current != null;

        public void CompleteAllRitualsForDebug()
        {
            secretSealRitualComplete = true;
            wandererSceneMask = 7;
            travelerLegendBosses = new List<int>
            {
                NPCID.KingSlime, NPCID.EyeofCthulhu, NPCID.EaterofWorldsHead,
                NPCID.BrainofCthulhu, NPCID.QueenBee, NPCID.SkeletronHead,
                NPCID.WallofFlesh, NPCID.Retinazer, NPCID.Spazmatism
            };
            starKeyPulsarRitualComplete = true;
        }

        public void ResetAllCooldowns()
        {
            spatialPrisonActive = false;
            secretSpaceCooldown = 0;
            banishCooldown = 0;
            spatialPrisonCooldown = 0;
            spaceTearCooldown = 0;
            dimensionalSightCooldown = 0;
            reenactCooldown = 0;
            mazeCooldown = 0;
            shatterCooldown = 0;
        }

        public void RecordLegend(int npcType)
        {
            if (travelerLegendBosses.Contains(npcType) || travelerLegendBosses.Count >= TravelerLegendTarget) return;
            travelerLegendBosses.Add(npcType);
            Announce($"【旅法师仪式】你在星空之外留下传说 {travelerLegendBosses.Count}/{TravelerLegendTarget}");
            SyncState();
        }

        public void CompletePulsarRitual()
        {
            if (starKeyPulsarRitualComplete) return;
            starKeyPulsarRitualComplete = true;
            Announce("【星之匙仪式完成】你与不断旋转、发出信号的沉重星体建立了联系。");
            SyncState();
        }
    }
}
