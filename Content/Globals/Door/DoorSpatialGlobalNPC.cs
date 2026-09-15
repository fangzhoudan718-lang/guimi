using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using zhashi.Content.Buffs.Door;
using zhashi.Content.Dimensions;
using zhashi.Content.Pathways.Door;
using zhashi.Content.Effects.Door;

namespace zhashi.Content.Globals.Door
{
    /// <summary>门途径空间控制的服务器权威效果，以及三个高序列晋升仪式的击杀判定。</summary>
    public class DoorSpatialGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public int banishTicks, prisonTicks, miniatureTicks, mazeTicks;
        /// <summary>“尚未击败过的 Boss”抵抗放逐时的真硬控；>0 期间该 NPC 的 AI 完全停摆。</summary>
        public int hardStunTicks;
        /// <summary>空间牢笼的圆形领域：圈内生物可以自由行动，但出不去这个圆。</summary>
        public int cageTicks;
        private Vector2 cageCenter;
        private float cageRadius;
        private int banishOwner = -1, pocketOwner = -1, pocketTicks, mazeReverseTicks;
        private Vector2 banishOrigin, prisonAnchor, prisonSafeCenter, pocketAnchor, mazeAnchor, mazeReverseVelocity;
        /// <summary>时空迷宫的圆环半径；圈内生物会绕 mazeAnchor 旋转。</summary>
        private float mazeRadius;
        private bool hadRitualPrison;
        private float drawScale = -1;
        private int fullWidth, fullHeight;
        private const float PrisonRadius = 64f;
        public static bool IsBossBody(NPC npc) => npc.boss || npc.realLife >= 0 ||
            NPCID.Sets.ShouldBeCountedAsBoss[npc.type];

        // ── 已停用的旧“乱流放逐” ────────────────────────────────────────────────
        // 现在的放逐是把目标直接送进灵界（见 SpiritBanishSystem），不再有 20 秒离场与归还门。
        // banishTicks 相关的判定只为旧存档里的残留状态保留，新流程不会再把它设为 >0。
        /// <summary>放逐必须接在已经建立的控制之后；低血量本身不是控制。</summary>
        public bool IsControlledForBanish(NPC npc) => banishTicks == 0 &&
            (prisonTicks > 0 || pocketTicks > 0 || mazeTicks > 0 ||
            npc.HasBuff<SpatialPrisonBuff>() || npc.HasBuff<Content.Buffs.SpiritControlDebuff>() ||
            npc.HasBuff<Content.Buffs.AbyssShacklesDebuff>() || npc.HasBuff<Content.Buffs.Debuffs.TimeStagnationBuff>() ||
            npc.HasBuff(BuffID.Frozen) || npc.HasBuff(BuffID.Webbed));

        /// <summary>
        /// 目标是否还是这位施法者的“玩具”：还在收纳中，或者已经放出但仍处于缩小状态。
        /// 缩小持续时间远长于收纳时间，所以这样判断才能让玩家在整个玩具化期间反复挪动它们。
        /// </summary>
        public bool HasOwnedToy(int owner) => owner >= 0 && owner < Main.maxPlayers &&
            pocketOwner == owner && banishTicks == 0 && (pocketTicks > 0 || miniatureTicks > 0);

        public void Banish(NPC npc, Player owner)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !npc.active || IsBossBody(npc) ||
                owner == null || !owner.active || owner.dead || !IsControlledForBanish(npc)) return;
            banishOwner = owner.whoAmI;
            banishOrigin = npc.Center;
            banishTicks = 1200;
            npc.netUpdate = true;
        }

        public void Prison(NPC npc, int ticks)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || ticks <= 0) return;
            if (prisonTicks == 0) prisonAnchor = prisonSafeCenter = npc.Center;
            prisonTicks = Math.Max(prisonTicks, ticks);
            npc.netUpdate = true;
        }

        public void HardStun(NPC npc, int ticks)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || ticks <= 0) return;
            hardStunTicks = Math.Max(hardStunTicks, ticks);
            npc.velocity = Vector2.Zero;
            npc.netUpdate = true;
        }

        /// <summary>把生物登记进圆形牢笼；领域存在期间会不断续期。</summary>
        public void Cage(NPC npc, Vector2 center, float radius, int ticks)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || ticks <= 0 || radius <= 4f) return;
            if (pocketTicks > 0 || banishTicks > 0) return;
            bool fresh = cageTicks <= 0;
            cageCenter = center;
            cageRadius = radius;
            cageTicks = Math.Max(cageTicks, ticks);
            if (fresh) npc.netUpdate = true;
        }

        /// <summary>割离领域：允许在圆里自由行动，只把越界的目标拉回边界内侧。</summary>
        private void ContainedByDomain(NPC npc)
        {
            if (cageRadius <= 4f || !npc.active) return;
            Vector2 offset = npc.Center - cageCenter;
            float distance = offset.Length();
            // 让整个碰撞箱都留在圆内，而不是只有中心点。
            float limit = Math.Max(24f, cageRadius - Math.Max(npc.width, npc.height) * 0.5f);
            if (distance <= limit) return;

            Vector2 normal = offset.SafeNormalize(Vector2.UnitX);
            Vector2 target = cageCenter + normal * (limit - 1f);
            // Boss 不做瞬移式回拉，改成持续往圆内拽，免得打断分节与传送类 AI。
            npc.Center = IsBossBody(npc) ? Vector2.Lerp(npc.Center, target, 0.45f) : target;
            float outward = Vector2.Dot(npc.velocity, normal);
            if (outward > 0f) npc.velocity -= normal * outward * 1.15f;
            if (Vector2.DistanceSquared(npc.Center, target) > 16f) npc.netUpdate = true;
        }

        /// <summary>硬控整条身体，而不只是鼠标点到的那一节（蠕虫类 Boss）。</summary>
        public static void HardStunBody(NPC npc, int ticks)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            NPC root = SpiritBanishSystem.BanishRoot(npc);
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC segment = Main.npc[i];
                if (!segment.active) continue;
                if (segment != root && segment.realLife != root.whoAmI) continue;
                segment.GetGlobalNPC<DoorSpatialGlobalNPC>().HardStun(segment, ticks);
            }
        }

        public void Miniaturize(NPC npc, int ticks, int owner = -1)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || ticks <= 0 || banishTicks > 0) return;
            if (miniatureTicks == 0 && !IsBossBody(npc))
            {
                pocketTicks = 180;
                pocketOwner = owner >= 0 && owner < Main.maxPlayers ? owner : -1;
                pocketAnchor = npc.Center;
                fullWidth = npc.width; fullHeight = npc.height;
                Vector2 center = npc.Center;
                npc.width = Math.Max(8, (int)(fullWidth * 0.35f));
                npc.height = Math.Max(8, (int)(fullHeight * 0.35f));
                npc.Center = center;
            }
            miniatureTicks = Math.Max(miniatureTicks, ticks);
            npc.netUpdate = true;
        }

        /// <summary>只在服务器释放到非实体位置；缩小与易伤持续时间不会被重新刷新。</summary>
        public bool ReleasePocket(NPC npc, Vector2 destination)
        {
            // 收纳期结束、但仍处于缩小状态时同样允许挪动；pocketOwner 会保留到缩小结束。
            if (Main.netMode == NetmodeID.MultiplayerClient || !npc.active ||
                (pocketTicks <= 0 && miniatureTicks <= 0) ||
                banishTicks > 0 || IsBossBody(npc) ||
                !TryFindSafeCenter(destination, npc.width, npc.height, out Vector2 safe)) return false;
            npc.Center = safe;
            npc.velocity = Vector2.Zero;
            pocketTicks = 0;
            // A released toy must not be snapped back by an older prison or maze anchor.
            prisonAnchor = prisonSafeCenter = mazeAnchor = pocketAnchor = safe;
            mazeReverseTicks = 0;
            npc.netUpdate = true;
            return true;
        }

        /// <summary>时空迷宫：记录圆心与半径，圈内的生物会像钟表指针一样绕圆心转。</summary>
        public void Maze(NPC npc, Vector2 center, float radius, int ticks)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || ticks <= 0 || radius <= 4f) return;
            bool fresh = mazeTicks <= 0;
            mazeAnchor = center;
            mazeRadius = radius;
            mazeTicks = Math.Max(mazeTicks, ticks);
            if (fresh) npc.netUpdate = true;
        }

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter writer)
        {
            bool hasState = banishTicks > 0 || prisonTicks > 0 || miniatureTicks > 0 || mazeTicks > 0 ||
                hardStunTicks > 0 || cageTicks > 0 ||
                pocketTicks > 0 || fullWidth > 0 || npc.HasBuff<SpatialPrisonBuff>();
            bitWriter.WriteBit(hasState);
            if (!hasState) return;
            writer.Write((short)banishTicks); writer.Write((short)prisonTicks);
            writer.Write((short)miniatureTicks); writer.Write((short)mazeTicks);
            writer.Write((short)hardStunTicks);
            writer.Write((short)pocketTicks); writer.Write((short)banishOwner);
            writer.WriteVector2(banishOrigin); writer.WriteVector2(prisonAnchor); writer.WriteVector2(mazeAnchor);
            writer.Write(mazeRadius);
            writer.Write(fullWidth); writer.Write(fullHeight);
            writer.Write((short)pocketOwner); writer.Write((short)mazeReverseTicks);
            writer.WriteVector2(pocketAnchor); writer.WriteVector2(prisonSafeCenter); writer.WriteVector2(mazeReverseVelocity);
            writer.Write((short)cageTicks); writer.Write(cageRadius); writer.WriteVector2(cageCenter);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader reader)
        {
            int previousWidth = fullWidth, previousHeight = fullHeight;
            if (!bitReader.ReadBit())
            {
                banishTicks = prisonTicks = miniatureTicks = mazeTicks = pocketTicks = mazeReverseTicks = 0;
                hardStunTicks = 0;
                cageTicks = 0;
                banishOwner = pocketOwner = -1;
                fullWidth = fullHeight = 0;
                if (previousWidth > 0 && previousHeight > 0)
                { npc.width = previousWidth; npc.height = previousHeight; }
                return;
            }
            banishTicks = reader.ReadInt16(); prisonTicks = reader.ReadInt16();
            miniatureTicks = reader.ReadInt16(); mazeTicks = reader.ReadInt16();
            hardStunTicks = reader.ReadInt16();
            pocketTicks = reader.ReadInt16(); banishOwner = reader.ReadInt16();
            banishOrigin = reader.ReadVector2(); prisonAnchor = reader.ReadVector2(); mazeAnchor = reader.ReadVector2();
            mazeRadius = reader.ReadSingle();
            fullWidth = reader.ReadInt32(); fullHeight = reader.ReadInt32();
            pocketOwner = reader.ReadInt16(); mazeReverseTicks = reader.ReadInt16();
            pocketAnchor = reader.ReadVector2(); prisonSafeCenter = reader.ReadVector2(); mazeReverseVelocity = reader.ReadVector2();
            cageTicks = reader.ReadInt16(); cageRadius = reader.ReadSingle(); cageCenter = reader.ReadVector2();
            if (fullWidth > 0 && fullHeight > 0)
            {
                npc.width = miniatureTicks > 0 ? Math.Max(8, (int)(fullWidth * 0.35f)) : fullWidth;
                npc.height = miniatureTicks > 0 ? Math.Max(8, (int)(fullHeight * 0.35f)) : fullHeight;
            }
            else if (previousWidth > 0 && previousHeight > 0)
            { npc.width = previousWidth; npc.height = previousHeight; }
            // NPC sync already applied authoritative top-left position before ExtraAI. Preserving
            // Center here would shift the resized NPC by half the difference between collision boxes.
        }

        public override bool PreAI(NPC npc)
        {
            if (hardStunTicks > 0)
            {
                // 真硬控：完全跳过 AI，只保留存活时间，避免被当成“离场”而提前消失。
                hardStunTicks--;
                npc.velocity = Vector2.Zero;
                npc.timeLeft = Math.Max(npc.timeLeft, 60);
                return false;
            }

            if (banishTicks > 0)
            {
                npc.velocity = Vector2.Zero;
                npc.timeLeft = Math.Max(npc.timeLeft, 120);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    bool validOwner = banishOwner >= 0 && banishOwner < Main.maxPlayers &&
                        Main.player[banishOwner].active && !Main.player[banishOwner].dead;
                    if (!validOwner || --banishTicks <= 0) ReturnFromBanish(npc, validOwner);
                }
                else if (banishTicks > 1) banishTicks--;
                // Clients keep the target banished at tick 1 until the authoritative return packet.
                return false;
            }

            TickAuthoritative(npc, ref prisonTicks);
            TickAuthoritative(npc, ref mazeTicks);
            TickAuthoritative(npc, ref mazeReverseTicks);
            TickAuthoritative(npc, ref cageTicks);
            if (miniatureTicks > 0)
            {
                if (miniatureTicks > 1) miniatureTicks--;
                else if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 safe = npc.Center;
                    if (fullWidth == 0 || TryFindSafeCenter(npc.Center, fullWidth, fullHeight, out safe))
                    {
                        if (fullWidth > 0)
                        {
                            npc.width = fullWidth; npc.height = fullHeight; npc.Center = safe;
                            fullWidth = fullHeight = 0;
                        }
                        miniatureTicks = 0;
                        // 玩具化结束、体型恢复：解除归属，之后不再算作这位施法者的玩具。
                        pocketOwner = -1;
                        npc.netUpdate = true;
                    }
                    else
                    {
                        miniatureTicks = 30; // Terrain changed: retry safely instead of expanding inside a wall.
                        npc.netUpdate = true;
                    }
                }
            }
            if (pocketTicks > 0 && !IsBossBody(npc))
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    bool ownerGone = pocketOwner >= 0 && (!Main.player[pocketOwner].active || Main.player[pocketOwner].dead);
                    if (ownerGone || pocketTicks <= 1) ReleasePocket(npc, pocketAnchor);
                    else pocketTicks--;
                }
                else if (pocketTicks > 1) pocketTicks--;
                npc.velocity = Vector2.Zero;
                npc.timeLeft = Math.Max(npc.timeLeft, 120);
                if (pocketTicks > 0) return false;
            }

            // Preserve the promotion ritual's control without overwriting its anchor every frame.
            bool ritualPrison = npc.HasBuff<SpatialPrisonBuff>();
            if (ritualPrison && !hadRitualPrison && prisonTicks == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            { prisonAnchor = prisonSafeCenter = npc.Center; npc.netUpdate = true; }
            hadRitualPrison = ritualPrison;
            return true;
        }

        public override void PostAI(NPC npc)
        {
            if (banishTicks > 0 || pocketTicks > 0) return;
            if (cageTicks > 0) ContainedByDomain(npc);
            bool boss = IsBossBody(npc);
            if (miniatureTicks > 0) npc.velocity *= boss ? 0.85f : 0.65f;
            if (mazeTicks > 0)
            {
                // 十二扇门围成的圆环像钟面一样转：圈内的生物（含 Boss）连同身体一起被拨着走。
                // 每个个体旋转同样的角度，所以分节类 Boss 的身体不会被拉断，而是整体转动。
                Vector2 offset = npc.Center - mazeAnchor;
                float limit = Math.Max(24f, mazeRadius - Math.Max(npc.width, npc.height) * 0.5f);
                if (offset.Length() > limit)
                {
                    // 被圆环拦住：拉回边界内侧，并抵消一切向外的速度（重力也算）。
                    Vector2 normal = offset.SafeNormalize(Vector2.UnitY);
                    npc.Center = mazeAnchor + normal * limit;
                    offset = npc.Center - mazeAnchor;
                    float outward = Vector2.Dot(npc.velocity, normal);
                    if (outward > 0f) npc.velocity -= normal * outward;
                }
                // 削弱重力：否则它们会一路沉到圆底贴边打滑，看起来就只是“掉下去了”。
                npc.velocity.Y *= 0.86f;
                float spin = MathHelper.TwoPi / 240f;   // 4 秒一圈
                npc.Center = mazeAnchor + offset.RotatedBy(spin);
                npc.velocity = npc.velocity.RotatedBy(spin);
            }
            if (prisonTicks > 0 || npc.HasBuff<SpatialPrisonBuff>())
            {
                if (boss) npc.velocity *= 0.7f;
                else if (Main.netMode != NetmodeID.MultiplayerClient) ConfineToPocket(npc);
            }
        }

        private static void TickAuthoritative(NPC npc, ref int ticks)
        {
            if (ticks > 1) ticks--;
            else if (ticks == 1 && Main.netMode != NetmodeID.MultiplayerClient)
            { ticks = 0; npc.netUpdate = true; }
        }

        private static bool IsEmptyCenter(Vector2 center, int width, int height)
        {
            if (!float.IsFinite(center.X) || !float.IsFinite(center.Y) || width <= 0 || height <= 0) return false;
            Vector2 top = center - new Vector2(width, height) * 0.5f;
            return top.X >= 16f && top.Y >= 16f && top.X + width <= Main.maxTilesX * 16f - 16f &&
                top.Y + height <= Main.maxTilesY * 16f - 16f && !Collision.SolidCollision(top, width, height);
        }

        private static bool TryFindSafeCenter(Vector2 preferred, int width, int height, out Vector2 safe)
        {
            safe = preferred;
            if (!float.IsFinite(preferred.X) || !float.IsFinite(preferred.Y)) return false;
            if (IsEmptyCenter(preferred, width, height)) return true;
            // Bounded local search; no world scans, no tile removal, no unchecked teleport.
            for (int ring = 1; ring <= 8; ring++)
            for (int direction = 0; direction < 8; direction++)
            {
                Vector2 candidate = preferred + Vector2.UnitY.RotatedBy(direction * MathHelper.PiOver4) * (-ring * 16f);
                if (!IsEmptyCenter(candidate, width, height)) continue;
                safe = candidate;
                return true;
            }
            return false;
        }

        private void ReturnFromBanish(NPC npc, bool ownerAvailable)
        {
            Vector2 destination = banishOrigin;
            bool found = false;
            if (ownerAvailable)
            {
                Player owner = Main.player[banishOwner];
                for (int i = 0; i < 8; i++)
                {
                    Vector2 candidate = owner.Center + Vector2.UnitX.RotatedBy(i * MathHelper.PiOver4) * 180f;
                    if (!IsEmptyCenter(candidate, npc.width, npc.height)) continue;
                    destination = candidate; found = true; break;
                }
            }
            if (!found && !TryFindSafeCenter(banishOrigin, npc.width, npc.height, out destination))
            { banishTicks = 1; return; }
            banishTicks = 0;
            npc.Center = destination;
            npc.velocity = Vector2.Zero;
            pocketTicks = 0; pocketOwner = -1; mazeReverseTicks = 0;
            prisonAnchor = prisonSafeCenter = mazeAnchor = pocketAnchor = destination;
            prisonTicks = Math.Max(prisonTicks, 45); // short contact grace period at the return door
            Projectile.NewProjectile(npc.GetSource_FromAI(), destination, Vector2.Zero,
                ModContent.ProjectileType<Content.Projectiles.Door.DoorAuthorityVisual>(), 0, 0,
                ownerAvailable ? banishOwner : Main.maxPlayers, 3);
            banishOwner = -1;
            npc.netUpdate = true;
        }

        private void ConfineToPocket(NPC npc)
        {
            Vector2 offset = npc.Center - prisonAnchor;
            if (offset.LengthSquared() <= PrisonRadius * PrisonRadius && IsEmptyCenter(npc.Center, npc.width, npc.height))
                prisonSafeCenter = npc.Center;
            else
            {
                Vector2 boundary = prisonAnchor + offset.SafeNormalize(Vector2.UnitX) * PrisonRadius;
                bool found = false;
                Vector2 destination = prisonSafeCenter;
                // Check inward points only: each stays inside the captured region.
                for (int i = 0; i <= 8; i++)
                {
                    Vector2 candidate = Vector2.Lerp(boundary, prisonAnchor, i / 8f);
                    if (!IsEmptyCenter(candidate, npc.width, npc.height)) continue;
                    destination = candidate; found = true; break;
                }
                if (!found) found = IsEmptyCenter(destination, npc.width, npc.height);
                if (found) { npc.Center = destination; prisonSafeCenter = destination; npc.netUpdate = true; }
            }
            Vector2 predicted = npc.Center + npc.velocity - prisonAnchor;
            if (predicted.LengthSquared() > PrisonRadius * PrisonRadius)
            {
                Vector2 normal = (npc.Center - prisonAnchor).SafeNormalize(predicted.SafeNormalize(Vector2.UnitX));
                float outwardSpeed = Vector2.Dot(npc.velocity, normal);
                if (outwardSpeed > 0f) npc.velocity -= normal * outwardSpeed * 1.35f;
            }
        }

        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            if (banishTicks <= 0) return;
            // A target in another space cannot be killed by damage-over-time ticking in this one.
            npc.lifeRegen = Math.Max(0, npc.lifeRegen);
            npc.lifeRegenCount = Math.Max(0, npc.lifeRegenCount);
            damage = 0;
        }

        public override bool CheckDead(NPC npc)
        {
            if (banishTicks <= 0) return true;
            // Also protects against a later GlobalNPC applying DoT after our regen hook.
            if (npc.life <= 0) npc.life = 1;
            return false;
        }

        public override bool CheckActive(NPC npc) => banishTicks == 0 && pocketTicks == 0;
        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot) => banishTicks == 0 && pocketTicks == 0 && prisonTicks == 0;
        public override bool CanHitNPC(NPC npc, NPC target) => banishTicks == 0 && pocketTicks == 0 && prisonTicks == 0;
        public override bool CanBeHitByNPC(NPC npc, NPC attacker) => banishTicks == 0;
        public override bool? CanBeHitByItem(NPC npc, Player player, Item item) => banishTicks > 0 ? false : null;
        public override bool? CanBeHitByProjectile(NPC npc, Projectile projectile) => banishTicks > 0 ? false : null;

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (miniatureTicks > 0) modifiers.FinalDamage *= IsBossBody(npc) ? 1.15f : 1.35f;
            if (mazeTicks > 0) modifiers.ArmorPenetration += 30;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (banishTicks > 0)
            {
                DoorVisuals.Portal(npc.Center, new Vector2(12f, 20f), DoorVisuals.ThemeColor(DoorTheme.Illusory),
                    0.40f, (float)Main.GameUpdateCount, DoorTheme.Illusory);
                return false;
            }
            if (miniatureTicks > 0)
            {
                drawScale = npc.scale;
                npc.scale *= MathHelper.Lerp(1, IsBossBody(npc) ? 0.8f : 0.35f, Math.Min(1, miniatureTicks / 30f));
            }
            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (drawScale >= 0) { npc.scale = drawScale; drawScale = -1; }
            if (!DoorVisuals.Visible(npc.Center)) return;
            if (miniatureTicks > 0)
                DoorVisuals.Box(npc.Center, pocketTicks > 0 ? 22f : 28f, DoorVisuals.Cyan, 0f, pocketTicks > 0 ? 0.24f : 0.12f);
            if (prisonTicks > 0 || npc.HasBuff<SpatialPrisonBuff>())
                DoorVisuals.Box(IsBossBody(npc) ? npc.Center : prisonAnchor,
                    IsBossBody(npc) ? 36f : PrisonRadius, DoorVisuals.Gold, 0f, 0.18f);
            else if (mazeTicks > 0)
                DoorVisuals.Box(npc.Center, 30f, DoorVisuals.Violet, 0.15f, mazeReverseTicks > 0 ? 0.25f : 0.10f);
            if (hardStunTicks > 0)
            {
                // 门扉把目标钉在原地：一圈收紧的封印环和一道空间裂口。
                DoorVisuals.Ring(npc.Center, Math.Max(npc.width, npc.height) * 0.6f + 20f, DoorVisuals.Gold, 0.45f, 0f, 1f, 0.78f);
                DoorVisuals.Crack(npc.Center, Math.Max(28f, npc.height * 0.55f), 0.85f, DoorVisuals.Pale, 0.40f);
            }
        }

        public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers)
        {
            if (miniatureTicks > 0) modifiers.FinalDamage *= IsBossBody(npc) ? 0.75f : 0.45f;
            if (mazeTicks > 0) modifiers.FinalDamage *= 0.7f;
        }

        public override void OnKill(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || npc.lastInteraction < 0 || npc.lastInteraction >= Main.maxPlayers)
                return;

            Player player = Main.player[npc.lastInteraction];
            if (!player.active) return;
            DoorPathwayPlayer door = player.GetModPlayer<DoorPathwayPlayer>();
            LotMPlayer lotm = player.GetModPlayer<LotMPlayer>();

            // 漫游者仪式：在星空或异度空间留下九个互不重复的Boss传说。
            if (lotm.baseDoorSequence == 3 && npc.boss && door.IsBeyondMundaneWorld())
                door.RecordLegend(npc.type);

            // 星之匙仪式：在太空中击败月亮领主，与泰拉瑞亚的“沉重星体”建立联系。
            if (lotm.baseDoorSequence == 2 && npc.type == NPCID.MoonLordCore && player.ZoneSkyHeight)
                door.CompletePulsarRitual();
        }
    }
}
