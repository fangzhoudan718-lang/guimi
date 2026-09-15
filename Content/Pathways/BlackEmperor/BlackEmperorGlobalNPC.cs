using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System;
using System.IO;
using Microsoft.Xna.Framework;
using zhashi.Content.Buffs.BlackEmperor;

namespace zhashi.Content.Pathways.BlackEmperor
{
    /// <summary>贿赂的四种模式，对应原著里贿赂者的四个核心能力。</summary>
    public enum BlackEmperorBribeMode { Weaken = 0, Charm = 1, Arrogant = 2, Link = 3 }

    /// <summary>
    /// 黑皇帝挂在 NPC 身上的状态：定罪、以及序列七「贿赂者」的四种贿赂。
    /// 后续序列的腐蚀层数、熵层数也加在这里，用 SendExtraAI/ReceiveExtraAI 同步。
    /// </summary>
    public class BlackEmperorGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        /// <summary>被审判律令定罪的标记。</summary>
        public bool Condemned;

        // ── 序列六 腐化男爵 ──────────────────────────────────────
        /// <summary>「腐蚀」光环叠在目标身上的「阴暗」层数。</summary>
        public int corruptionStacks;
        /// <summary>光环续期计时：站在腐化男爵附近就不断被顶住，离开之后层数才开始回落。</summary>
        public int corruptionRefresh;
        /// <summary>「扭曲」的残余：意图被改写，这段时间里它的行动与攻击都不再指向你。</summary>
        public int twistedTicks;
        /// <summary>序列三之后的扭曲更狠：它停不下来了。</summary>
        public bool twistedUnstoppable;
        /// <summary>狂乱：这段时间里它不分敌我，只攻击离自己最近的那个。</summary>
        public int rageInfightTicks;

        public const int MaxCorruptionStacks = 12;
        private int corruptionDecay;

        // ── 序列五 混乱导师 ──────────────────────────────────────
        /// <summary>混乱场里的误判：距离与敌我都会失去意义。</summary>
        public int chaosTicks;
        /// <summary>被「扭曲概念」绑住时，替哪位玩家分担的目标。</summary>
        public int conceptOwner = -1;
        public int conceptTicks;
        private int chaosSteerCooldown;
        /// <summary>城镇 NPC 最近被哪位玩家打过：用来判断它是不是死在玩家手里。</summary>
        private int townHitByPlayer = -1;
        private int townHitTimer;

        // ── 序列四 堕落伯爵 ──────────────────────────────────────
        /// <summary>赠予·消极怠工：打人变轻、动作变慢。</summary>
        public int slackTicks;
        /// <summary>赠予·贪婪急切：眼里只剩钱，连你都懒得打。</summary>
        public int greedTicks;
        /// <summary>赠予·丧失斗志：不敢靠近你，也更容易被打穿。</summary>
        public int despairTicks;
        /// <summary>放大·束缚：被隔空抱住，动不了。</summary>
        public int boundTicks;
        /// <summary>规则领域：站在律令划下的地方，出手更轻、挨打更痛。</summary>
        public int ruleTicks;
        private int greedDropCooldown;

        public const int BoundDuration = 3 * 60;


        // ── 贿赂状态 ──────────────────────────────────────────────
        /// <summary>魅惑：变成友方，替你打它的同类。</summary>
        public int charmedTicks;
        /// <summary>狂妄：转而攻击同类，但仍然会打你。</summary>
        public int arrogantTicks;
        /// <summary>削弱：对你造成的伤害大幅降低。</summary>
        public int weakenedTicks;
        /// <summary>关联：替某位玩家分担一半伤害。</summary>
        public int linkedOwner = -1;
        public int linkedTicks;

        private int allyStrikeCooldown;

        public bool IsBribed => charmedTicks > 0 || arrogantTicks > 0 || weakenedTicks > 0 || linkedTicks > 0;
        public bool TurnedAgainstAllies => charmedTicks > 0 || arrogantTicks > 0;
        public bool IsCorrupted => corruptionStacks > 0;
        public bool IsTwisted => twistedTicks > 0;

        /// <summary>施加一次贿赂。Boss 同样吃到原效果，但控制持续时间会按位格折减。</summary>
        public void ApplyBribe(NPC npc, BlackEmperorBribeMode mode, int ticks, int owner, bool boss)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !npc.active || ticks <= 0) return;
            int controlTicks = boss ? Math.Max(30, ticks / 4) : ticks;
            int supportTicks = boss ? Math.Max(60, ticks / 2) : ticks;
            switch (mode)
            {
                case BlackEmperorBribeMode.Weaken:
                    weakenedTicks = Math.Max(weakenedTicks, supportTicks);
                    break;
                case BlackEmperorBribeMode.Charm:
                    charmedTicks = Math.Max(charmedTicks, controlTicks);
                    break;
                case BlackEmperorBribeMode.Arrogant:
                    arrogantTicks = Math.Max(arrogantTicks, controlTicks);
                    break;
                case BlackEmperorBribeMode.Link:
                    linkedOwner = owner;
                    linkedTicks = Math.Max(linkedTicks, supportTicks);
                    break;
            }
            npc.netUpdate = true;
        }

        public override void PostAI(NPC npc)
        {
            if (charmedTicks > 0) charmedTicks--;
            if (arrogantTicks > 0) arrogantTicks--;
            if (weakenedTicks > 0) weakenedTicks--;
            if (linkedTicks > 0)
            {
                linkedTicks--;
                if (linkedTicks == 0) linkedOwner = -1;
            }
            if (allyStrikeCooldown > 0) allyStrikeCooldown--;

            if (twistedTicks > 0) twistedTicks--;
            if (rageInfightTicks > 0) rageInfightTicks--;
            if (chaosTicks > 0) chaosTicks--;
            if (conceptTicks > 0) conceptTicks--;
            if (slackTicks > 0) slackTicks--;
            if (greedTicks > 0) greedTicks--;
            if (despairTicks > 0) despairTicks--;
            if (boundTicks > 0) boundTicks--;
            if (ruleTicks > 0) ruleTicks--;
            if (greedDropCooldown > 0) greedDropCooldown--;
            if (chaosSteerCooldown > 0) chaosSteerCooldown--;
            if (townHitTimer > 0)
            {
                townHitTimer--;
                if (townHitTimer == 0) townHitByPlayer = -1;
            }
            TickCorruption(npc);
            TickChaos(npc);

            // 被买通的家伙转头去打自己人。只在服务器裁定，客户端跟着位置同步。
            if (Main.netMode != NetmodeID.MultiplayerClient && TurnedAgainstAllies)
                DriveAgainstAllies(npc);

            // 被扭曲的意图：它认定「远离你」才是前进。Boss 也会被拨动，但幅度更小。
            if (Main.netMode != NetmodeID.MultiplayerClient && twistedTicks > 0 && !TurnedAgainstAllies)
                DriveTwistedIntent(npc);

            if (Main.netMode != NetmodeID.MultiplayerClient) TickEarlGifts(npc);
            if (Main.netMode != NetmodeID.MultiplayerClient && rageInfightTicks > 0) DriveInfighting(npc);
        }

        /// <summary>
        /// 「阴暗」层数的涨落。玩家那侧只负责往里加层，这里负责续图标与回落，
        /// 两边都在服务器上跑，客户端只解读结果。
        /// </summary>
        private void TickCorruption(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            if (corruptionRefresh > 0)
            {
                corruptionRefresh--;
            }
            else if (corruptionStacks > 0 && ++corruptionDecay >= 45)
            {
                corruptionDecay = 0;
                corruptionStacks--;
                npc.netUpdate = true;
            }

            int buffType = ModContent.BuffType<CorruptedDebuff>();
            int index = npc.FindBuffIndex(buffType);

            if (corruptionStacks <= 0)
            {
                corruptionDecay = 0;
                if (index >= 0) npc.DelBuff(index);
                return;
            }

            // 图标跟着层数走：层数还在，图标就不能掉。
            if (index < 0) npc.AddBuff(buffType, 150);
            else if (npc.buffTime[index] < 60) npc.AddBuff(buffType, 150);
        }

        /// <summary>
        /// 混乱场里的误判：它算错了距离，也算错了敌我。
        /// 拨动只在服务器上做，客户端跟着位置同步走。
        /// </summary>
        private void TickChaos(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (chaosTicks <= 0) return;

            // 三分之一的时间把同伴错认成你——这就是「选择错误」。
            if (!TurnedAgainstAllies && Main.rand.NextBool(3)) DriveAgainstAllies(npc);

            if (chaosSteerCooldown > 0) return;
            chaosSteerCooldown = Main.rand.Next(30, 75);
            // 距离失去意义：它会突然朝错误的方向迈一步。
            Vector2 mistaken = new Vector2(-npc.velocity.X * 0.6f + Main.rand.NextFloat(-1.2f, 1.2f),
                npc.velocity.Y + Main.rand.NextFloat(-0.8f, 0.8f));
            npc.velocity = BlackEmperorMath.IsBoss(npc)
                ? Vector2.Lerp(npc.velocity, mistaken, 0.22f)
                : mistaken;
            npc.netUpdate = true;
        }

        /// <summary>标记：某个玩家刚刚对这位城镇 NPC 动过手。</summary>
        public void MarkTownHit(int playerIndex)
        {
            townHitByPlayer = playerIndex;
            townHitTimer = 10 * 60;
        }

        public int TownHitByPlayer => townHitTimer > 0 ? townHitByPlayer : -1;

        /// <summary>堕落伯爵的赠予：Boss 同样收到指定礼物，但控制持续时间会按位格折减。</summary>
        public void ApplyGift(BlackEmperorGiftMode mode, int ticks, bool boss)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || ticks <= 0) return;
            int appliedTicks = boss ? Math.Max(45, ticks / 3) : ticks;
            switch (mode)
            {
                case BlackEmperorGiftMode.Slack:
                    slackTicks = Math.Max(slackTicks, appliedTicks);
                    break;
                case BlackEmperorGiftMode.Greed:
                    greedTicks = Math.Max(greedTicks, appliedTicks);
                    break;
                default:
                    despairTicks = Math.Max(despairTicks, appliedTicks);
                    break;
            }
        }

        /// <summary>赠予留下的行为：怠工动作发沉，被抱住就动不了，失去斗志就往后退。</summary>
        private void TickEarlGifts(NPC npc)
        {
            if (slackTicks > 0) npc.velocity *= BlackEmperorMath.IsBoss(npc) ? 0.95f : 0.86f;

            if (boundTicks > 0)
            {
                if (BlackEmperorMath.IsBoss(npc)) npc.velocity *= 0.85f;
                else npc.velocity = Vector2.Zero;
            }

            if (despairTicks <= 0) return;

            Player nearest = null;
            float best = float.MaxValue;
            foreach (Player player in Main.ActivePlayers)
            {
                if (player.dead || player.ghost) continue;
                float distance = player.Distance(npc.Center);
                if (distance < best) { best = distance; nearest = player; }
            }
            if (nearest == null || best > 520f) return;

            Vector2 away = (npc.Center - nearest.Center).SafeNormalize(Vector2.UnitX);
            float despairPull = BlackEmperorMath.IsBoss(npc) ? 0.055f : 0.16f;
            npc.velocity = Vector2.Lerp(npc.velocity, new Vector2(away.X * 2.2f, npc.velocity.Y), despairPull);
            npc.direction = npc.spriteDirection = away.X >= 0 ? 1 : -1;
        }

        /// <summary>贪婪急切：一被打就从口袋里掉钱。</summary>
        public override void HitEffect(NPC npc, NPC.HitInfo hit)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (greedTicks <= 0 || greedDropCooldown > 0 || !npc.active) return;
            greedDropCooldown = 45;
            Item.NewItem(npc.GetSource_FromThis(), npc.getRect(), ItemID.SilverCoin, Main.rand.Next(1, 3));
        }


        /// <summary>
        /// 狂乱：它已经分不清敌我了，只会扑向离自己最近的那个——其他敌人、Boss，
        /// 甚至自己人都在名单上。Boss 的行为树太复杂，只轻轻带一下它的速度，
        /// 把真正的重量留给接触伤害。
        /// </summary>
        private void DriveInfighting(NPC npc)
        {
            NPC target = null;
            float best = 900f;
            foreach (NPC other in Main.ActiveNPCs)
            {
                if (other.whoAmI == npc.whoAmI || !other.active) continue;
                if (other.townNPC || other.friendly) continue;   // 城里的人不在名单上
                if (NPCID.Sets.CountsAsCritter[other.type]) continue;   // 小动物不值得它分心
                float distance = other.Distance(npc.Center);
                if (distance < best) { best = distance; target = other; }
            }
            if (target == null) return;

            Vector2 direction = (target.Center - npc.Center).SafeNormalize(Vector2.UnitX);
            float pull = BlackEmperorMath.IsBoss(npc) ? 0.05f : 0.16f;
            npc.velocity = Vector2.Lerp(npc.velocity, direction * 3.4f, pull);
            npc.direction = npc.spriteDirection = direction.X >= 0 ? 1 : -1;

            float reach = (npc.width + target.width) * 0.5f + 16f;
            if (allyStrikeCooldown > 0 || npc.Distance(target.Center) > reach) return;

            // 谁先动手都算：Boss 一巴掌下去，旁边的小怪会直接没了。
            int damage = Math.Max(1, npc.damage > 0 ? npc.damage : 12);
            target.SimpleStrikeNPC(damage, direction.X >= 0 ? 1 : -1, false, 4f, DamageClass.Generic, false);
            allyStrikeCooldown = 30;
            npc.netUpdate = true;
        }

        /// <summary>被扭曲的意图：把它往背离玩家的方向推，制造一段「打不着你」的窗口。</summary>
        private void DriveTwistedIntent(NPC npc)
        {
            Player nearest = null;
            float best = float.MaxValue;
            foreach (Player player in Main.ActivePlayers)
            {
                if (player.dead || player.ghost) continue;
                float distance = player.Distance(npc.Center);
                if (distance < best) { best = distance; nearest = player; }
            }
            if (nearest == null) return;

            Vector2 away = (npc.Center - nearest.Center).SafeNormalize(Vector2.UnitX);
            float twistPull = BlackEmperorMath.IsBoss(npc) ? 0.05f : 0.18f;
            npc.velocity = Vector2.Lerp(npc.velocity, new Vector2(away.X * 2.4f, npc.velocity.Y), twistPull);
            npc.direction = npc.spriteDirection = away.X >= 0 ? 1 : -1;

            // 被狂乱法师扭过意图的目标连停都停不下来：地面上永远留着一份速度。
            if (twistedUnstoppable && npc.velocity.X * npc.direction < 1.6f)
                npc.velocity.X = 1.6f * npc.direction;
        }

        private void DriveAgainstAllies(NPC npc)
        {
            NPC target = null;
            float best = 460f;
            foreach (NPC other in Main.ActiveNPCs)
            {
                if (other.whoAmI == npc.whoAmI || !BlackEmperorPlayer.CanAffectEnemy(other)) continue;
                var otherState = other.GetGlobalNPC<BlackEmperorGlobalNPC>();
                // 不打同样被买通的人。
                if (otherState.TurnedAgainstAllies) continue;
                float distance = other.Distance(npc.Center);
                if (distance < best) { best = distance; target = other; }
            }
            if (target == null) return;

            Vector2 direction = (target.Center - npc.Center).SafeNormalize(Vector2.UnitX);
            float allegiancePull = BlackEmperorMath.IsBoss(npc) ? 0.04f : 0.15f;
            npc.velocity = Vector2.Lerp(npc.velocity, direction * 4.5f, allegiancePull);
            npc.direction = npc.spriteDirection = direction.X >= 0 ? 1 : -1;

            float reach = (npc.width + target.width) * 0.5f + 14f;
            if (allyStrikeCooldown <= 0 && npc.Distance(target.Center) < reach)
            {
                int damage = Math.Max(1, npc.damage > 0 ? npc.damage : 10);
                target.SimpleStrikeNPC(damage, direction.X >= 0 ? 1 : -1, false, 4f, DamageClass.Generic, false);
                allyStrikeCooldown = 30;
                npc.netUpdate = true;
            }
        }

        /// <summary>魅惑之后它不再攻击玩家。</summary>
        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
            => charmedTicks <= 0 && greedTicks <= 0;   // 贪婪急切时它眼里只有钱

        /// <summary>被削弱的目标打人变轻。</summary>
        public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers)
        {
            bool boss = BlackEmperorMath.IsBoss(npc);
            if (weakenedTicks > 0) modifiers.FinalDamage *= boss ? 0.88f : 0.7f;
            // 阴暗：心灵被腐蚀得越久，出手越是迟疑。
            if (corruptionStacks > 0) modifiers.FinalDamage *= 1f - (boss ? 0.01f : 0.025f) * corruptionStacks;
            // 扭曲：它以为自己在打你，其实打偏了。
            if (twistedTicks > 0) modifiers.FinalDamage *= boss ? 0.92f : 0.8f;
            // 混乱场：它算不清距离，出手自然容易落偏。
            if (chaosTicks > 0) modifiers.FinalDamage *= boss ? 0.90f : 0.75f;
            // 赠予·消极怠工 / 规则领域：出手发沉，规矩也帮着压它一头。
            if (slackTicks > 0) modifiers.FinalDamage *= boss ? 0.86f : 0.65f;
            if (ruleTicks > 0) modifiers.FinalDamage *= boss ? 0.94f : 0.85f;
            // 狂乱：它正忙着跟自己人算账，对你的手就轻了。
            if (rageInfightTicks > 0) modifiers.FinalDamage *= boss ? 0.88f : 0.7f;
        }

        /// <summary>丧失斗志与规则领域都会让它更容易被打穿。</summary>
        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            bool boss = BlackEmperorMath.IsBoss(npc);
            if (despairTicks > 0) modifiers.FinalDamage *= boss ? 1.10f : 1.25f;
            if (ruleTicks > 0) modifiers.FinalDamage *= boss ? 1.06f : 1.15f;
        }

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter writer)
        {
            bool hasState = IsBribed || Condemned || IsCorrupted || IsTwisted || chaosTicks > 0 || conceptTicks > 0 ||
                slackTicks > 0 || greedTicks > 0 || despairTicks > 0 || boundTicks > 0 || ruleTicks > 0 ||
                rageInfightTicks > 0;
            bitWriter.WriteBit(hasState);
            if (!hasState) return;
            writer.Write((short)charmedTicks);
            writer.Write((short)arrogantTicks);
            writer.Write((short)weakenedTicks);
            writer.Write((short)linkedTicks);
            writer.Write((short)linkedOwner);
            writer.Write(Condemned);
            writer.Write((short)corruptionStacks);
            writer.Write((short)twistedTicks);
            writer.Write(twistedUnstoppable);
            writer.Write((short)chaosTicks);
            writer.Write((short)conceptTicks);
            writer.Write((short)conceptOwner);
            writer.Write((short)slackTicks);
            writer.Write((short)greedTicks);
            writer.Write((short)despairTicks);
            writer.Write((short)boundTicks);
            writer.Write((short)ruleTicks);
            writer.Write((short)rageInfightTicks);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader reader)
        {
            if (!bitReader.ReadBit())
            {
                charmedTicks = arrogantTicks = weakenedTicks = linkedTicks = 0;
                linkedOwner = -1;
                Condemned = false;
                corruptionStacks = twistedTicks = 0;
                chaosTicks = conceptTicks = 0;
                conceptOwner = -1;
                twistedUnstoppable = false;
                slackTicks = greedTicks = despairTicks = boundTicks = ruleTicks = 0;
                rageInfightTicks = 0;
                return;
            }
            charmedTicks = reader.ReadInt16();
            arrogantTicks = reader.ReadInt16();
            weakenedTicks = reader.ReadInt16();
            linkedTicks = reader.ReadInt16();
            linkedOwner = reader.ReadInt16();
            Condemned = reader.ReadBoolean();
            corruptionStacks = reader.ReadInt16();
            twistedTicks = reader.ReadInt16();
            twistedUnstoppable = reader.ReadBoolean();
            chaosTicks = reader.ReadInt16();
            conceptTicks = reader.ReadInt16();
            conceptOwner = reader.ReadInt16();
            slackTicks = reader.ReadInt16();
            greedTicks = reader.ReadInt16();
            despairTicks = reader.ReadInt16();
            boundTicks = reader.ReadInt16();
            ruleTicks = reader.ReadInt16();
            rageInfightTicks = reader.ReadInt16();
        }

        /// <summary>玩家直接攻击城镇 NPC：记一笔，用来判断它是死在谁手里。</summary>
        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            if (npc.townNPC) npc.GetGlobalNPC<BlackEmperorGlobalNPC>().MarkTownHit(player.whoAmI);
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (!npc.townNPC) return;
            int owner = projectile.owner;
            if (owner < 0 || owner >= Main.maxPlayers) return;
            npc.GetGlobalNPC<BlackEmperorGlobalNPC>().MarkTownHit(owner);
        }

        public override void OnKill(NPC npc)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient) RecordRitualKill(npc);
            if (!Condemned) return;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            // 定罪：额外掉落一份金币，序列越高越多。
            int coins = npc.boss ? 50 : 5;
            Item.NewItem(npc.GetSource_Death(), npc.getRect(), ItemID.GoldCoin, coins);
        }

        /// <summary>龙类判定：原版那几条龙 + 名字里带「龙」的（灾厄的龙也算）。</summary>
        private static bool IsDragon(NPC npc)
        {
            if (npc.type == NPCID.WyvernHead) return true;
            string name = Lang.GetNPCNameValue(npc.type);
            return !string.IsNullOrEmpty(name) && name.Contains("龙");
        }

        /// <summary>序列五「城市地底的秩序」的记账：谁的最后一击，就记在谁头上。</summary>
        private void RecordRitualKill(NPC npc)
        {
            int who = npc.townNPC ? TownHitByPlayer : npc.lastInteraction;
            if (who < 0 || who >= Main.maxPlayers) return;
            Player player = Main.player[who];
            if (!player.active) return;
            BlackEmperorPlayer be = player.GetModPlayer<BlackEmperorPlayer>();
            if (npc.townNPC) be.BreakChaosRitual("城里有人的血沾到了你手上");
            else if (!player.dead)
            {
                be.RecordUndergroundKill(npc);
                // 序列三的仪式只认 Boss：这一场动过狂乱、而且活着走完，才算一场。
                if (BlackEmperorMath.IsBoss(npc))
                {
                    be.RecordRageBoss(npc.type);
                    be.CountKingKill();
                }
                // 名义的进度：血月里的杀戮、地底的狩猎、以及龙。
                if (Main.bloodMoon) be.CountBloodMoonKill();
                if (player.ZoneRockLayerHeight || player.ZoneUnderworldHeight) be.CountAbyssKill();
                if (IsDragon(npc)) be.CountDragonSlain();
            }
        }
    }

}
