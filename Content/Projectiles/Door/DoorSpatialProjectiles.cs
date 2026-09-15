using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Effects.Door;
using zhashi.Content.Globals.Door;
using zhashi.Content.Pathways.Door;

namespace zhashi.Content.Projectiles.Door
{
    /// <summary>
    /// The doorway left behind by a Door pathway ability.
    ///
    /// ai[0]: 1 = pathway door opening (hidden space, scene recording, rituals),
    ///        2 = illusory door opening (Banish: the leaf opens onto another scene),
    ///        3 = door closing (returning from hiding / from exile).
    /// ai[1]: 延迟 tick。放逐要读出“开门 → 目标被吞下 → 关门”的顺序，所以关门门扉先潜伏一会儿。
    /// The opening and closing each carry the vanilla door sound. The projectile is synced, so every
    /// client hears it at the right position, and every doorway uses DoorVisuals.DoorRadii so the
    /// same door always looks the same size.
    /// </summary>
    public class DoorAuthorityVisual : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
            Projectile.timeLeft = 54; Projectile.netImportant = true;
        }
        public override bool? CanDamage() => false;
        private int Style => (int)Projectile.ai[0];
        private bool Closing => Style == 3;
        private DoorTheme Theme => Style == 2 ? DoorTheme.Illusory : DoorVisuals.ThemeForSequence(OwnerSequence());
        private int OwnerSequence()
        {
            if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers) return 4;
            int seq = Main.player[Projectile.owner].GetModPlayer<LotMPlayer>().currentDoorSequence;
            return Math.Clamp(seq, 1, 4);
        }
        public override void AI()
        {
            if (Projectile.ai[1] > 0f)
            {
                // 延迟阶段让两边一起倒数，保证客户端看到的开门/关门时刻一致。
                Projectile.ai[1] -= 1f;
                Projectile.timeLeft = 54;
                return;
            }
            if (Main.dedServ) return;
            if (Projectile.localAI[0]++ == 0)
            {
                if (Closing)
                    SoundEngine.PlaySound(SoundID.DoorClosed with { Volume = 0.52f, Pitch = -0.2f }, Projectile.Center);
                else
                    SoundEngine.PlaySound(SoundID.DoorOpen with { Volume = 0.52f, Pitch = Style == 2 ? -0.4f : -0.2f }, Projectile.Center);
            }
            // Kept deliberately dim: neighbouring vanilla light still reads as the light source.
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.ai[1] > 0f) return false;
            float t = 54 - Projectile.timeLeft;
            float open = Closing
                ? MathHelper.Clamp(Projectile.timeLeft / 34f, 0f, 1f)
                : Math.Min(t / 10f, Projectile.timeLeft / 18f);
            DoorVisuals.Portal(Projectile.Center, DoorVisuals.DoorRadii, DoorVisuals.ThemeColor(Theme), open, t, Theme);
            return false;
        }
    }

    /// <summary>
    /// A visible refuge door follows its caster while the hidden space lasts, then stays where it is
    /// and swings shut. Opening and closing both carry a door sound, so the skill announces itself
    /// without any extra visual noise.
    /// </summary>
    public class DoorRefugeVisual : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2; Projectile.timeLeft = 900;
            Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.netImportant = true;
        }
        public override bool? CanDamage() => false;
        private bool Closing => Projectile.ai[1] > 0.5f;
        public override void AI()
        {
            if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers) { Projectile.Kill(); return; }
            Player owner = Main.player[Projectile.owner];
            bool hidingEnded = !owner.active || owner.dead || owner.GetModPlayer<LotMPlayer>().currentDoorSequence > 4 ||
                (Projectile.ai[0] > 20 && !owner.GetModPlayer<DoorPathwayPlayer>().secretSpaceActive);
            if (!Closing)
            {
                if (!Main.dedServ && Projectile.localAI[0]++ == 0)
                    SoundEngine.PlaySound(SoundID.DoorOpen with { Volume = 0.5f, Pitch = -0.25f }, Projectile.Center);
                if (hidingEnded && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.ai[1] = 1f;                 // swing shut where it stands
                    Projectile.timeLeft = 34;
                    Projectile.netUpdate = true;
                }
                // 隐藏没有固定时长，门就一直立在入口处，直到玩家离开门后空间。
                else if (!hidingEnded && Projectile.timeLeft < 120) Projectile.timeLeft = 120;
            }
            // The initial projectile position IS the fixed entrance. Never overwrite it with an
            // independently arriving player packet. Only authority changes the closing phase.
            if (Closing && !Main.dedServ && Projectile.localAI[1] == 0)
            {
                Projectile.localAI[1] = 1;
                SoundEngine.PlaySound(SoundID.DoorClosed with { Volume = 0.4f, Pitch = -0.25f }, Projectile.Center);
            }
            if (Closing) Projectile.timeLeft = Math.Min(Projectile.timeLeft, 34);
            Projectile.ai[0]++;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            float open = Closing
                ? MathHelper.Clamp(Projectile.timeLeft / 30f, 0f, 1f)
                : Math.Min(1f, Projectile.ai[0] / 16f);
            // Leave only the doorway; the surrounding scene remains unobscured.
            int sequence = Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers
                ? Main.player[Projectile.owner].GetModPlayer<LotMPlayer>().currentDoorSequence : 4;
            DoorTheme theme = DoorVisuals.ThemeForSequence(sequence);
            DoorVisuals.Portal(Projectile.Center, DoorVisuals.DoorRadii, DoorVisuals.ThemeColor(theme), open, Projectile.ai[0], theme, Closing ? 0.7f : 0.35f);
            return false;
        }
    }

    // 撕裂空间：裂缝开在鼠标处，存在十秒；玩家身体碰到裂缝会被随机送到另一条裂缝。
    // 和空间牢笼同一套“另一个世界”表现：固定切口 + 两层按视角偏移的星野。
    public class SpatialRiftProjectile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";
        /// <summary>裂缝存在时间：十秒。</summary>
        public const int LifeTicks = 600;
        /// <summary>判定厚度与玩家接触判定用的半宽。</summary>
        public const float TouchThickness = 26f;
        /// <summary>视差位移上限（世界像素）；贴图遮罩就是按这个值留的余量。</summary>
        private const float ParallaxLimit = 12f;

        private Vector2 Axis => Vector2.UnitX.RotatedBy(Projectile.ai[0]);
        private float HalfLength => Projectile.ai[1];
        private bool Opening => Projectile.timeLeft > LifeTicks - 30;
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 8; Projectile.timeLeft = LifeTicks;
            Projectile.friendly = true; Projectile.penetrate = -1; Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic; Projectile.usesLocalNPCImmunity = true;
            // 开缝那一刀之后，留在裂缝里的目标每半秒还会被余波割一次。
            Projectile.localNPCHitCooldown = 30;
            Projectile.hide = true;
        }
        public override void OnSpawn(IEntitySource source) => Projectile.hide = true;
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
            List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            // 和空间牢笼一样：盖住地形，但留在生物与玩家之下，所以裂缝里的人仍然看得见。
            behindNPCs.Add(index);
        }
        public override bool? CanDamage() => Projectile.timeLeft <= LifeTicks - 8 ? null : false;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float point = 0;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center - Axis * HalfLength, Projectile.Center + Axis * HalfLength, 22, ref point);
        }
        public override void AI()
        {
            if (!OwnerValid(Projectile.owner, 3)) { Projectile.Kill(); return; }
            if (!Main.dedServ && Projectile.timeLeft == LifeTicks - 12)
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.35f, Pitch = -0.2f }, Projectile.Center);
            if (Main.netMode != NetmodeID.MultiplayerClient) TeleportTouchingPlayers();
        }

        /// <summary>玩家身体碰到裂缝：随机换到同一施法者的另一条裂缝。离开所有裂缝后才能再次触发。</summary>
        private void TeleportTouchingPlayers()
        {
            if (Opening) return;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead || player.ghost) continue;
                DoorPathwayPlayer door = player.GetModPlayer<DoorPathwayPlayer>();
                if (door.riftTeleportLocked) continue;
                if (!TouchesPlayer(Projectile, player)) continue;

                Projectile destination = FindOtherRift(Projectile, player);
                if (destination == null) continue;

                Vector2 target = FindSafeSpot(destination.Center, player);
                door.riftTeleportLocked = true;
                player.Teleport(target, 1);
                player.velocity *= 0.35f;
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, player.whoAmI, target.X, target.Y, 1);
            }
        }

        /// <summary>寻找同一位施法者的另一条裂缝。</summary>
        private static Projectile FindOtherRift(Projectile self, Player player)
        {
            Projectile chosen = null;
            int matches = 0;
            foreach (Projectile other in Main.ActiveProjectiles)
            {
                if (other.whoAmI == self.whoAmI || other.type != self.type || other.owner != self.owner) continue;
                if (other.timeLeft <= 20) continue;
                matches++;
                if (Main.rand.Next(matches) == 0) chosen = other;
            }
            return chosen;
        }

        /// <summary>落点优先选裂缝中心；被方块占住时在附近找一个能站人的位置。</summary>
        private static Vector2 FindSafeSpot(Vector2 center, Player player)
        {
            if (!Collision.SolidCollision(center - player.Size * 0.5f, player.width, player.height)) return center;
            for (int ring = 1; ring <= 3; ring++)
                for (int step = 0; step < 8; step++)
                {
                    Vector2 candidate = center + Vector2.UnitX.RotatedBy(step * MathHelper.PiOver4) * (ring * 24f);
                    if (!Collision.SolidCollision(candidate - player.Size * 0.5f, player.width, player.height)) return candidate;
                }
            return center;
        }

        /// <summary>玩家碰撞箱是否碰到裂缝，供传送触发与“离开后才能再次触发”的解锁共用。</summary>
        public static bool TouchesPlayer(Projectile rift, Player player)
        {
            float point = 0;
            Vector2 axis = Vector2.UnitX.RotatedBy(rift.ai[0]);
            float half = rift.ai[1];
            return Collision.CheckAABBvLineCollision(player.Hitbox.TopLeft(), player.Hitbox.Size(),
                rift.Center - axis * half, rift.Center + axis * half, TouchThickness, ref point);
        }

        internal static bool OwnerValid(int owner, int sequence) => owner >= 0 && owner < Main.maxPlayers &&
            // A late-joining client may receive the projectile before the owner's pathway packet.
            // Authority ends invalid casts; clients must not kill them based on temporary defaults.
            (Main.netMode == NetmodeID.MultiplayerClient || (Main.player[owner].active && !Main.player[owner].dead &&
            Main.player[owner].GetModPlayer<LotMPlayer>().currentDoorSequence <= sequence));
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ArmorPenetration += DoorPathwayPlayer.ScaledArmorPenetration(50, OwnerSequence());
            // 第一刀是完整的切割，之后留在裂缝里的只是余波。
            if (!Opening) modifiers.FinalDamage *= 0.25f;
        }

        private int OwnerSequence() => Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers
            ? Main.player[Projectile.owner].GetModPlayer<LotMPlayer>().currentDoorSequence : 3;
        public override bool PreDraw(ref Color lightColor)
        {
            if (!DoorVisuals.Visible(Projectile.Center, HalfLength + 40)) return false;
            float age = LifeTicks - Projectile.timeLeft;
            float fade = Math.Min(1f, age / 12f) * Math.Min(1f, Projectile.timeLeft / 24f);
            // 观察者越偏离裂缝，里面的星野偏得越多；裂缝本身（切边）永远不动。
            Vector2 eye = Main.LocalPlayer.Center - Projectile.Center;
            if (eye.LengthSquared() > 700f * 700f) eye = eye.SafeNormalize(Vector2.Zero) * 700f;
            Vector2 farOffset = -eye * 0.012f;
            Vector2 nearOffset = -eye * 0.026f;
            if (farOffset.Length() > ParallaxLimit) farOffset = farOffset.SafeNormalize(Vector2.Zero) * ParallaxLimit;
            if (nearOffset.Length() > ParallaxLimit) nearOffset = nearOffset.SafeNormalize(Vector2.Zero) * ParallaxLimit;
            float length = HalfLength * 2f;
            DoorVisuals.Rift(DoorDomainLayer.Void, Projectile.Center, length, Projectile.ai[0], fade);
            DoorVisuals.Rift(DoorDomainLayer.Far, Projectile.Center + farOffset, length, Projectile.ai[0], fade);
            DoorVisuals.Rift(DoorDomainLayer.Near, Projectile.Center + nearOffset, length, Projectile.ai[0], fade);
            return false;
        }
    }

    // Gameplay radius and phase use ai; no per-tick packets or client-side NPC mutations.
    public class DoorDomainProjectile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";
        private readonly bool[] affected = new bool[Main.maxNPCs];
        private int Kind => (int)Projectile.ai[0];
        private float Radius => Projectile.ai[1];
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 4; Projectile.timeLeft = 600;
            Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.netImportant = true;
            Projectile.hide = false;
        }
        public override void OnSpawn(IEntitySource source) => Projectile.hide = Kind == 0;
        /// <summary>
        /// 星系背景要盖住地形、但必须留在生物与玩家之下，所以登记进 behindNPCs 这一档。
        /// （以前的 ModSystem.PostDrawTiles 属于瓦片层，启用渲染目标时会连同瓦片一起盖到实体上面。）
        /// </summary>
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
            List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            if (Kind == 0) behindNPCs.Add(index);
        }
        public override bool? CanDamage() => false;
        public override void AI()
        {
            Projectile.hide = Kind == 0;
            int age = (int)Projectile.ai[2]++;
            if (Kind == 0)
            {
                // 永久维持：施法者还在维持就一直是满时限；关闭时缩到 24 tick 让它淡出收束。
                bool maintained = Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers &&
                    Main.player[Projectile.owner].active && !Main.player[Projectile.owner].dead &&
                    Main.player[Projectile.owner].GetModPlayer<DoorPathwayPlayer>().spatialPrisonActive;
                if (maintained) Projectile.timeLeft = 600;
                else if (Projectile.timeLeft > 24) Projectile.timeLeft = 24;
            }
            else Projectile.timeLeft = Math.Min(Projectile.timeLeft, (Kind == 1 ? 240 : 600) - age);
            if (!SpatialRiftProjectile.OwnerValid(Projectile.owner, Kind == 2 ? 1 : Kind == 1 ? 2 : 3))
            { Projectile.Kill(); return; }
            if (age == 0 && !Main.dedServ)
                SoundEngine.PlaySound(SoundID.DoorClosed with { Volume = 0.3f, Pitch = -0.4f }, Projectile.Center);
            if (Kind == 0)
            {
                // 领域收束时把门重新打开，先响后散。
                if (Projectile.localAI[0] == 0f && Projectile.timeLeft <= 30)
                {
                    Projectile.localAI[0] = 1f;
                    if (!Main.dedServ) SoundEngine.PlaySound(SoundID.DoorOpen with { Volume = 0.35f, Pitch = -0.3f }, Projectile.Center);
                }
            }
            if (Main.netMode == NetmodeID.MultiplayerClient || age < 24 || age % 6 != 0) return;
            // 收束阶段不再捕捉新目标。
            if (Kind == 0 && Projectile.timeLeft <= 24) return;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                // “范围内所有生物都出不去”：敌对生物与小动物都算，城镇NPC不动。
                bool trappable = npc.CanBeChasedBy() || NPCID.Sets.CountsAsCritter[npc.type];
                if (!trappable || npc.townNPC || npc.Distance(Projectile.Center) > Radius) continue;
                var spatial = npc.GetGlobalNPC<DoorSpatialGlobalNPC>();
                if (spatial.banishTicks > 0) continue;
                if (Kind == 0)
                {
                    // 每6 tick续一次：已经在圈里的出不去，之后走进来的同样出不去。
                    spatial.Cage(npc, Projectile.Center, Radius, Math.Min(600, Projectile.timeLeft + 6));
                    continue;
                }
                affected[npc.whoAmI] = true;
                bool boss = DoorSpatialGlobalNPC.IsBossBody(npc);
                if (Kind == 1) spatial.Miniaturize(npc, boss ? 300 : 720, Projectile.owner);
                // 时空迷宫对 Boss 同样生效：整条身体被圆环拨着转。
                else spatial.Maze(npc, Projectile.Center, Radius, Math.Min(600, Projectile.timeLeft + 6));
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (!DoorVisuals.Visible(Projectile.Center, Radius + 40)) return false;
            float age = Projectile.ai[2], fade = Math.Min(1, age / 24) * Math.Min(1, Projectile.timeLeft / 24f);
            Color color = Kind == 0 ? DoorVisuals.Gold : Kind == 1 ? DoorVisuals.Cyan : DoorVisuals.Violet;
            if (Kind == 0)
            {
                // 割离领域 = 固定的圆盘切口 + 两层随视角平移的星野。
                // 观察者离领域中心越远，内部星野偏得越多，读起来就像透过一只眼睛看到另一个世界。
                Vector2 eye = Main.LocalPlayer.Center - Projectile.Center;
                if (eye.LengthSquared() > 700f * 700f) eye = eye.SafeNormalize(Vector2.Zero) * 700f;
                DoorVisuals.Domain(DoorDomainLayer.Void, Projectile.Center, Radius, fade);
                DoorVisuals.Domain(DoorDomainLayer.Far, Projectile.Center - eye * 0.026f, Radius, fade);
                DoorVisuals.Domain(DoorDomainLayer.Near, Projectile.Center - eye * 0.068f, Radius, fade);
                // 四扇小门是门途径留下的锚点，压暗到只做结构提示。
                for (int i = 0; i < 4; i++)
                {
                    Vector2 p = Projectile.Center + Vector2.UnitX.RotatedBy(i * MathHelper.PiOver2 + 0.2f) * (Radius - 16f);
                    DoorVisuals.Portal(p, new Vector2(12, 22), color, 0.85f, age, DoorTheme.Wanderer, 0.26f * fade);
                }
                return false;
            }
            if (Kind == 1)
            {
                DoorVisuals.DimensionalEye(Projectile.Center, age, Projectile.timeLeft, fade);
            }
            else
            {
                // One faint, stationary boundary tells players where the field works; no interior fill.
                DoorVisuals.Ring(Projectile.Center, Radius, color, opacity: 0.18f * fade);
                // 时空迷宫：十二扇门围成一个不断旋转的圆环，转速与圈内生物的“钟表指针”一致。
                const int count = 12;
                float spin = age * (MathHelper.TwoPi / 240f);
                for (int i = 0; i < count; i++)
                {
                    Vector2 p = Projectile.Center + Vector2.UnitX.RotatedBy(spin + i * MathHelper.TwoPi / count) * Radius * 0.94f;
                    DoorVisuals.Portal(p, new Vector2(13, 24), color, 0.85f, age, DoorTheme.KeyOfStars, 0.62f * fade);
                }
                DoorVisuals.Ring(Projectile.Center, Radius, color, 0.20f * fade, spin, 1f, 0.38f);
            }
            return false;
        }
    }

    // One replay per recorded figure; profiles are an explicit, conservative NPC ability mapping.
    public class DoorRecordedEcho : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2; Projectile.timeLeft = 56;
            Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.netImportant = true;
        }
        public override bool? CanDamage() => false;
        public override void AI()
        {
            if (!SpatialRiftProjectile.OwnerValid(Projectile.owner, 2)) { Projectile.Kill(); return; }
            Projectile.ai[2]++;
            if (Projectile.ai[2] != 22 || Main.netMode == NetmodeID.MultiplayerClient) return;
            DoorEchoKind kind = DoorEchoCatalog.GetKind((int)Projectile.ai[0]);
            int shots = kind == DoorEchoKind.Volley ? 3 : 1;
            for (int i = 0; i < shots; i++)
            {
                float spread = (i - (shots - 1) / 2f) * 0.16f;
                float speed = kind == DoorEchoKind.Strike ? 24 : kind == DoorEchoKind.Lance ? 28 : 15;
                Vector2 direction = Vector2.UnitX.RotatedBy(Projectile.ai[1] + spread);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, direction * speed,
                    ModContent.ProjectileType<DoorEchoBolt>(), Math.Max(1, Projectile.damage / shots), 5,
                    Projectile.owner, (int)kind, Projectile.ai[0]);
            }
        }
        internal static void DrawFigure(int npcType, Vector2 center, float maxSize, float opacity, float angle)
        {
            if (npcType <= 0 || npcType >= TextureAssets.Npc.Length || !DoorVisuals.Visible(center)) return;
            Main.instance.LoadNPC(npcType);
            var texture = TextureAssets.Npc[npcType].Value;
            int h = texture.Height / Math.Max(1, Main.npcFrameCount[npcType]);
            var frame = new Rectangle(0, 0, texture.Width, h);
            float scale = Math.Min(0.8f, maxSize / Math.Max(texture.Width, h));
            Main.spriteBatch.Draw(texture, center - Main.screenPosition, frame,
                DoorVisuals.Tint(DoorVisuals.Pale, opacity), 0, frame.Size() / 2, scale,
                MathF.Cos(angle) > 0 ? Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally :
                    Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            float fade = Math.Min(1, Projectile.ai[2] / 12f) * Math.Min(1, Projectile.timeLeft / 18f);
            DoorVisuals.Portal(Projectile.Center, new Vector2(21, 36), DoorVisuals.Cyan, fade, Projectile.ai[2],
                DoorTheme.Planeswalker, 0.4f);
            DrawFigure((int)Projectile.ai[0], Projectile.Center, 64, fade * 0.72f, Projectile.ai[1]);
            return false;
        }
    }

    public class DoorEchoBolt : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";
        private DoorEchoKind Kind => (DoorEchoKind)(int)Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 3;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20; Projectile.timeLeft = 75;
            Projectile.friendly = true; Projectile.penetrate = 1; Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Magic; Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        public override void AI()
        {
            if (!SpatialRiftProjectile.OwnerValid(Projectile.owner, 2)) { Projectile.Kill(); return; }
            Projectile.ai[2]++;
            if (Projectile.localAI[0] == 0)
            {
                Projectile.localAI[0] = 1;
                Vector2 center = Projectile.Center;
                if (Kind == DoorEchoKind.Strike) Projectile.width = Projectile.height = 46;
                if (Kind == DoorEchoKind.Lance) Projectile.penetrate = 3;
                Projectile.Center = center;
            }
            int duration = Kind == DoorEchoKind.Strike ? 24 : Kind == DoorEchoKind.Lance ? 42 : 75;
            Projectile.timeLeft = Math.Min(Projectile.timeLeft, Math.Max(1, duration - (int)Projectile.ai[2]));
            if (Kind == DoorEchoKind.Pursuit && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC nearest = null; float distance = 420;
                foreach (NPC npc in Main.ActiveNPCs)
                    if (npc.CanBeChasedBy() && npc.GetGlobalNPC<DoorSpatialGlobalNPC>().banishTicks == 0 &&
                        npc.Distance(Projectile.Center) < distance)
                    { nearest = npc; distance = npc.Distance(Projectile.Center); }
                if (nearest != null)
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity,
                        (nearest.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * 16, 0.09f);
                if ((int)Projectile.ai[2] % 6 == 0) Projectile.netUpdate = true;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (!DoorVisuals.Visible(Projectile.Center)) return false;
            float angle = Projectile.velocity.ToRotation();
            float fade = Math.Min(1, Projectile.timeLeft / 8f);
            if (Kind == DoorEchoKind.Strike || Kind == DoorEchoKind.Pursuit)
            {
                DoorRecordedEcho.DrawFigure((int)Projectile.ai[1], Projectile.Center, 48, 0.72f * fade, angle);
            }
            else if (Kind == DoorEchoKind.Lance)
                DoorVisuals.Tear(Projectile.Center, 44, 10, angle, DoorVisuals.Pale, 0.8f * fade, 1);
            else DoorVisuals.Shard(Projectile.Center, 18, DoorVisuals.Gold, 0.85f * fade, angle);
            return false;
        }
    }



    /// <summary>
    /// 空间破碎：在鼠标处张开一个黑洞。
    /// 阶段一「成形」逐步粉碎整个视口内的可破坏物块；阶段二「牵引」把生物、弹幕、掉落与碎片拽向奇点；
    /// 阶段三「凝聚」事件视界收成一点，随后结算一次巨量伤害。请勿在家中使用。
    /// </summary>
    public class SpaceShatterProjectile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";
        /// <summary>ai[1] 由施法客户端上报、服务端钳制并随弹幕同步，覆盖施法时的整个视口。</summary>
        internal float EffectRadius => MathHelper.Clamp(Projectile.ai[1] > 0f ? Projectile.ai[1] : 1920f, 800f, 6000f);
        private const int FormTicks = 120;      // 2 秒成形
        private const int HoldTicks = 150;      // 2.5 秒持续牵引
        private const int CollapseTicks = 45;   // 0.75 秒凝聚
        public const int TotalTicks = FormTicks + HoldTicks + CollapseTicks;

        private int Age => TotalTicks - Projectile.timeLeft;
        private bool Collapsing => Projectile.timeLeft <= CollapseTicks;
        private long tileScanCursor;
        private int cachedTileSide;
        private int rowPermutationStep;
        private bool ambienceStarted;
        private readonly Dictionary<int, Point> tileDirtyRows = new();
        /// <summary>0→1 的充能进度，牵引强度与震屏幅度都跟着它涨。</summary>
        private float Charge => MathHelper.Clamp(Age / (float)(FormTicks + HoldTicks), 0f, 1f);
        /// <summary>供客户端屏幕空间透镜读取；不参与伤害、牵引或网络判定。</summary>
        internal float VisualDistortionStrength => Collapsing
            ? MathHelper.Clamp(Projectile.timeLeft / (float)CollapseTicks, 0f, 1f)
            : 0.35f + 0.65f * Charge;

        internal void GetVisualState(out float coreRadius, out float tilt, out float opacity)
        {
            float age = Age;
            opacity = Math.Min(1f, Projectile.timeLeft / 18f) * Math.Min(1f, Math.Max(0.2f, age / 12f));
            tilt = MathF.Sin(age * 0.012f) * 0.18f;
            if (!Collapsing)
                coreRadius = MathHelper.Lerp(26f, 92f, Math.Min(1f, age / FormTicks)) * (1f + 0.08f * Charge);
            else
            {
                float collapseProgress = 1f - Projectile.timeLeft / (float)CollapseTicks;
                coreRadius = MathHelper.Lerp(96f, 1.5f, collapseProgress * collapseProgress);
            }
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 4; Projectile.timeLeft = TotalTicks;
            Projectile.friendly = true; Projectile.penetrate = -1; Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic; Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; Projectile.netImportant = true;
        }
        // 伤害只在“凝聚成一点”的那几帧结算，每个目标一次。
        public override bool? CanDamage() =>
            Projectile.timeLeft <= 14 && Projectile.timeLeft >= 4 ? null : false;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 nearest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
            return Vector2.DistanceSquared(nearest, Projectile.Center) <= EffectRadius * EffectRadius;
        }
        public override void AI()
        {
            Projectile.ai[0]++;
            if (!SpatialRiftProjectile.OwnerValid(Projectile.owner, 1)) { Projectile.Kill(); return; }
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (!Collapsing)
                {
                    PullEverything();
                    ShatterVisibleWorld();
                }
                else PullLooseItems();
            }
            ShakeScreen();
            if (Main.dedServ) return;
            StartAmbience();
            if (Projectile.timeLeft == TotalTicks)
                SoundEngine.PlaySound(SoundID.Item117 with { Volume = 0.42f, Pitch = -0.7f }, Projectile.Center);
            if (Projectile.timeLeft == CollapseTicks)
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.6f, Pitch = -0.45f }, Projectile.Center);
        }

        /// <summary>把所有范围内的生物（Boss 也不例外）往奇点里拖，并阻止它们逃出去。</summary>
        private void PullEverything()
        {
            float strength = 3.5f + 13f * Charge;
            float radius = EffectRadius;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active) continue;
                if (npc.GetGlobalNPC<DoorSpatialGlobalNPC>().banishTicks > 0) continue;
                Vector2 offset = Projectile.Center - npc.Center;
                float distance = offset.Length();
                if (distance > radius || distance < 1f) continue;
                Vector2 direction = offset / distance;
                bool boss = DoorSpatialGlobalNPC.IsBossBody(npc);
                // Boss 只吃到部分牵引，免得直接打断分节与传送类 AI。
                float weight = boss ? 0.5f : 1f;
                npc.velocity = Vector2.Lerp(npc.velocity, direction * strength * weight, 0.18f * weight);
                // 位置也轻轻拽一把，保证光靠速度硬扛的目标最终仍会被吸进范围。
                npc.Center += direction * (boss ? 0.6f : 1.6f) * (0.5f + Charge);
                if (Age % 15 == 0) npc.netUpdate = true;
            }
            // 弹幕同样会被吸进去。门途径自己的场地弹幕除外——黑洞、领域、裂隙不该互相拖拽。
            foreach (Projectile other in Main.ActiveProjectiles)
            {
                if (!other.active || other.whoAmI == Projectile.whoAmI) continue;
                if (other.type == Projectile.type) continue;
                if (other.ModProjectile?.Mod == Mod) continue;
                Vector2 offset = Projectile.Center - other.Center;
                float distance = offset.Length();
                if (distance > radius || distance < 1f) continue;
                Vector2 direction = offset / distance;
                other.velocity = Vector2.Lerp(other.velocity, direction * strength * 1.15f, 0.20f);
                // 位置也拽一把，免得高速弹幕直接从引力区穿过去。
                other.Center += direction * 1.4f * (0.5f + Charge);
            }
            PullLooseItems();
        }

        /// <summary>地面掉落物同样沿引力方向加速；抵达事件视界后被彻底吞没。</summary>
        private void PullLooseItems()
        {
            foreach (Item item in Main.ActiveItems)
            {
                Vector2 offset = Projectile.Center - item.Center;
                float distance = offset.Length();
                if (distance > EffectRadius || distance < 1f) continue;
                Vector2 direction = offset / distance;
                float speed = MathHelper.Clamp(distance / 32f, 8f, 42f);
                item.velocity = Vector2.Lerp(item.velocity, direction * speed, 0.16f);
                if (distance < 34f)
                {
                    item.active = false;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncItem, -1, -1, null, item.whoAmI);
                }
                else if (Main.netMode == NetmodeID.Server && Age % 12 == 0)
                    NetMessage.SendData(MessageID.SyncItem, -1, -1, null, item.whoAmI);
            }
        }

        /// <summary>
        /// 分批拆除圆形视口内的可破坏物块。按打散后的行顺序扫描，避免明显的整齐擦除线；
        /// 每次联机同步仍合并成少量横向矩形。掉落被奇点碾碎，抽样方块碎片负责表现吸入过程。
        /// </summary>
        private void ShatterVisibleWorld()
        {
            if (Age < 24) return;
            int radiusTiles = (int)MathF.Ceiling(EffectRadius / 16f);
            int side = radiusTiles * 2 + 1;
            long total = (long)side * side;
            if (cachedTileSide != side)
            {
                cachedTileSide = side;
                rowPermutationStep = Math.Max(1, (int)(side * 0.6180339f));
                while (GreatestCommonDivisor(rowPermutationStep, side) != 1) rowPermutationStep++;
                tileScanCursor = 0;
            }
            if (tileScanCursor >= total) return;

            int ticksRemaining = Math.Max(1, FormTicks + HoldTicks - Age);
            int scanBudget = (int)Math.Clamp((total - tileScanCursor + ticksRemaining - 1) / ticksRemaining, 64L, 2400L);
            int centerX = (int)(Projectile.Center.X / 16f);
            int centerY = (int)(Projectile.Center.Y / 16f);
            int debrisBudget = 9;
            tileDirtyRows.Clear();

            for (int scanned = 0; scanned < scanBudget && tileScanCursor < total; scanned++, tileScanCursor++)
            {
                int logicalRow = (int)(tileScanCursor / side);
                int column = (int)(tileScanCursor % side);
                int shuffledRow = logicalRow * rowPermutationStep % side;
                if ((logicalRow & 1) != 0) column = side - 1 - column;
                int dx = column - radiusTiles;
                int dy = shuffledRow - radiusTiles;
                if ((long)dx * dx + (long)dy * dy > (long)radiusTiles * radiusTiles) continue;

                int x = centerX + dx;
                int y = centerY + dy;
                if (!WorldGen.InWorld(x, y, 12)) continue;
                Tile tile = Framing.GetTileSafely(x, y);
                if (!tile.HasTile) continue;

                ushort oldType = tile.TileType;
                short oldFrameX = tile.TileFrameX;
                short oldFrameY = tile.TileFrameY;
                WorldGen.KillTile(x, y, false, false, true);
                if (Framing.GetTileSafely(x, y).HasTile) continue;

                if (!tileDirtyRows.TryGetValue(y, out Point span)) tileDirtyRows[y] = new Point(x, x);
                else tileDirtyRows[y] = new Point(Math.Min(span.X, x), Math.Max(span.Y, x));

                if (debrisBudget > 0 && (uint)((x * 397) ^ (y * 613)) % 13u == 0u)
                {
                    debrisBudget--;
                    Vector2 position = new Vector2(x * 16 + 8, y * 16 + 8);
                    Vector2 tangent = (Projectile.Center - position).SafeNormalize(Vector2.UnitY)
                        .RotatedBy(MathHelper.PiOver2);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), position, tangent * 2.4f,
                        ModContent.ProjectileType<SpaceShatterDebrisProjectile>(), 0, 0f,
                        Projectile.owner, oldType, oldFrameX, oldFrameY);
                }
            }

            if (Main.netMode == NetmodeID.Server)
                foreach (KeyValuePair<int, Point> row in tileDirtyRows)
                    NetMessage.SendTileSquare(-1, row.Value.X - 2, row.Key - 2,
                        row.Value.Y - row.Value.X + 5, 5);
        }

        private static int GreatestCommonDivisor(int a, int b)
        {
            while (b != 0) { int remainder = a % b; a = b; b = remainder; }
            return Math.Abs(a);
        }

        /// <summary>持续震屏：短脉冲彼此覆盖，方向缓慢旋转，形成不断被拖向奇点的低频颤动。</summary>
        private void ShakeScreen()
        {
            if (Main.dedServ || Main.gameMenu) return;
            float distance = Main.LocalPlayer.Distance(Projectile.Center);
            if (distance > EffectRadius + 1000f || Age % 6 != 0) return;
            float attenuation = 1f - MathHelper.Clamp(distance / (EffectRadius + 1000f), 0f, 0.72f);
            float strength = (2.8f + 5.6f * Charge) * attenuation;
            if (Collapsing) strength *= 1.75f;
            Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                Projectile.Center, Vector2.UnitX.RotatedBy(Age * 0.31f), strength, 3.4f, 10,
                EffectRadius + 1000f, $"DoorSpaceShatter:{Projectile.whoAmI}"));
        }

        private void StartAmbience()
        {
            if (ambienceStarted) return;
            ambienceStarted = true;
            SoundStyle loop = SoundID.DD2_EtherianPortalIdleLoop with
            {
                Volume = 0.26f,
                Pitch = -0.72f,
                PitchVariance = 0f,
                IsLooped = true,
                MaxInstances = 4
            };
            SoundEngine.PlaySound(loop, Projectile.Center, sound =>
            {
                if (!Projectile.active || Projectile.ModProjectile != this) return false;
                sound.Position = Projectile.Center;
                sound.Volume = 0.72f + 0.28f * Charge;
                sound.Pitch = Collapsing
                    ? -0.9f + Projectile.timeLeft / (float)CollapseTicks * 0.18f
                    : -0.72f;
                return true;
            });
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            int sequence = Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers
                ? Main.player[Projectile.owner].GetModPlayer<LotMPlayer>().currentDoorSequence : 1;
            modifiers.ArmorPenetration += DoorPathwayPlayer.ScaledArmorPenetration(60, sequence);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (!DoorVisuals.Visible(Projectile.Center, EffectRadius + 120)) return false;
            float age = Age;
            GetVisualState(out float coreRadius, out float tilt, out float fade);

            // 引力井：两层极暗的柔雾把周围“吃掉”。不是画线，是让那块地方变暗。
            float visualRadius = Math.Min(EffectRadius, 960f);
            DoorVisuals.Haze(Projectile.Center, new Vector2(visualRadius * 1.6f), DoorVisuals.Ink,
                (0.10f + 0.08f * Charge) * fade, tilt);
            DoorVisuals.Haze(Projectile.Center, new Vector2(visualRadius * 0.85f), DoorVisuals.Ink,
                (0.12f + 0.06f * Charge) * fade, tilt);
            // 范围提示：一条压得很暗的倾斜环，只做读数用。
            DoorVisuals.Ring(Projectile.Center, visualRadius, DoorVisuals.Ink, 0.12f * fade, tilt, 1f, 0.42f);

            // 黑洞本体在 EndCapture 阶段绘制，才能真实折射已经完成的世界画面。
            DrawInfallingDust(coreRadius, fade);

            if (!Collapsing)
                DoorVisuals.Crack(Projectile.Center, 90f + 150f * Charge, Charge, DoorVisuals.Ink, 0.38f * fade, 0f);
            else if (Projectile.timeLeft <= 10)
            {
                // 归零之后向外裂开一圈裂纹，作为“巨量伤害”的视觉落点。
                float burst = (10 - Projectile.timeLeft) / 10f;
                DoorVisuals.Crack(Projectile.Center, visualRadius * (0.25f + 0.75f * burst), burst,
                    DoorVisuals.Pale, 0.50f * (1f - burst), 0f);
            }
            return false;
        }

        /// <summary>
        /// 被撕碎的物质沿螺旋线掉进奇点：外圈慢慢转、越往里越快也越暗，
        /// 这是“黑洞正在吞东西”最直观的读数，也是 3D 旋转感的主要来源。
        /// </summary>
        private void DrawInfallingDust(float coreRadius, float fade)
        {
            const int motes = 26;
            for (int i = 0; i < motes; i++)
            {
                float seed = i * 0.6180339f;
                float progress = (Age * (0.008f + 0.006f * Charge) + seed) % 1f;
                float radius = MathHelper.Lerp(Math.Min(EffectRadius, 960f) * 0.55f, coreRadius * 1.35f, progress);
                // 越往里转得越快：角速度随进度上升，读起来是被越拧越紧地卷进去。
                float angle = seed * MathHelper.TwoPi + progress * progress * 3.2f;
                Vector2 position = Projectile.Center + Vector2.UnitX.RotatedBy(angle) * radius;
                DoorVisuals.Mote(position, 7f + 10f * (1f - progress), DoorVisuals.Pale,
                    (1f - progress) * 0.30f * fade, angle);
            }
        }
    }

    /// <summary>空间破碎抽样保留的方块碎片；只作视觉表现，不造成额外伤害或掉落。</summary>
    public class SpaceShatterDebrisProjectile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 12;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }
        public override bool? CanDamage() => false;
        public override void AI()
        {
            Projectile target = null;
            float best = float.MaxValue;
            foreach (Projectile candidate in Main.ActiveProjectiles)
            {
                if (candidate.owner != Projectile.owner || candidate.ModProjectile is not SpaceShatterProjectile) continue;
                float distance = Vector2.DistanceSquared(candidate.Center, Projectile.Center);
                if (distance < best) { best = distance; target = candidate; }
            }
            if (target == null) { Projectile.Kill(); return; }

            float length = MathF.Sqrt(best);
            Vector2 inward = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
            float speed = MathHelper.Clamp(length / 30f, 12f, 58f);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, inward * speed, 0.13f);
            Projectile.rotation += 0.16f + Projectile.velocity.Length() * 0.006f;
            if (length < 30f && Main.netMode != NetmodeID.MultiplayerClient) Projectile.Kill();
        }
        public override bool PreDraw(ref Color lightColor)
        {
            int tileType = (int)Projectile.ai[0];
            if (tileType < 0 || tileType >= TextureAssets.Tile.Length) return false;
            Main.instance.LoadTiles(tileType);
            Texture2D texture = TextureAssets.Tile[tileType].Value;
            int frameX = Math.Clamp((int)Projectile.ai[1], 0, Math.Max(0, texture.Width - 16));
            int frameY = Math.Clamp((int)Projectile.ai[2], 0, Math.Max(0, texture.Height - 16));
            Rectangle source = new Rectangle(frameX, frameY, Math.Min(16, texture.Width), Math.Min(16, texture.Height));
            float fade = Math.Min(1f, Projectile.timeLeft / 18f);
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, source, lightColor * fade,
                Projectile.rotation, source.Size() * 0.5f, 0.86f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
