using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.DataStructures;
using zhashi.Content.Buffs;
using zhashi.Content;

namespace zhashi.Content.NPCs
{
    public class TamingGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public int attackCooldown = 0;
        public int originalDamage = -1;
        public int ownerIndex = -1;

        public override void ResetEffects(NPC npc)
        {
            // 留空
        }

        // ==========================================================
        // 1. 【子代继承修复】 自动驯服 Boss 召唤出来的子代
        // ==========================================================
        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            if (source is EntitySource_Parent parentSource && parentSource.Entity is NPC parentNPC)
            {
                // 如果“爸爸”有驯服 Buff，儿子也自动继承
                if (parentNPC.HasBuff(ModContent.BuffType<TamedBuff>()))
                {
                    npc.AddBuff(ModContent.BuffType<TamedBuff>(), 36000);
                    if (parentNPC.TryGetGlobalNPC(out TamingGlobalNPC parentGlobal))
                    {
                        this.ownerIndex = parentGlobal.ownerIndex;
                    }
                    npc.friendly = true;
                    npc.dontTakeDamageFromHostiles = false;
                }
            }
        }

        // ==========================================================
        // 2. 【防伤人修复】 禁止伤害玩家
        // ==========================================================
        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            if (npc.HasBuff(ModContent.BuffType<TamedBuff>()))
            {
                return false;
            }
            return base.CanHitPlayer(npc, target, ref cooldownSlot);
        }

        public override bool PreAI(NPC npc)
        {
            // ==========================================================
            // 3. 【崩溃修复】 状态清理逻辑 (核心修复点)
            // ==========================================================
            // 如果怪物不再拥有驯服 Buff (过期或被清除)，必须立刻重置状态！
            // 否则原版 AI 会因为 friendly=true 或 target=-1 而崩溃 (IndexOutOfRangeException)
            if (!npc.HasBuff(ModContent.BuffType<TamedBuff>()))
            {
                // 如果 ownerIndex != -1 说明它之前是被驯服的
                if (ownerIndex != -1)
                {
                    ownerIndex = -1; // 清除主人标记

                    // 如果不是城镇NPC，强制改回敌对
                    if (!npc.townNPC && !NPCID.Sets.ActsLikeTownNPC[npc.type])
                    {
                        npc.friendly = false;
                        npc.dontTakeDamageFromHostiles = true;

                        // 【关键】重置 Target 为 255 (无效但安全)，迫使原版 AI 重新索敌
                        // 防止原版 AI 使用残留的 -1 去读取 Main.player 数组导致崩溃
                        npc.target = 255;
                    }
                }
                return true; // 安全返回，执行原版 AI
            }

            // ==========================================================
            // 以下为驯服状态下的逻辑
            // ==========================================================

            // 1. 主人检查
            if (ownerIndex < 0 || ownerIndex >= Main.maxPlayers)
            {
                npc.velocity.X *= 0.9f;
                return false;
            }

            Player owner = Main.player[ownerIndex];
            if (!owner.active || owner.dead)
            {
                npc.DelBuff(npc.FindBuffIndex(ModContent.BuffType<TamedBuff>()));
                return true;
            }

            // 2. 强力续命 (防止消失)
            npc.timeLeft = 1000;

            // 3. 属性增强与续杯
            var modPlayer = owner.GetModPlayer<LotMPlayer>();
            if (modPlayer.currentMoonSequence <= 7)
            {
                int buffIndex = npc.FindBuffIndex(ModContent.BuffType<TamedBuff>());
                if (buffIndex != -1) npc.buffTime[buffIndex] = 30000;
            }

            if (originalDamage == -1) originalDamage = npc.damage;

            // 4. 强制阵营修正
            npc.friendly = true;
            npc.dontTakeDamageFromHostiles = false;
            npc.target = -1; // 移除对玩家仇恨

            // 伤害加成
            float damageMult = 1.0f;
            if (modPlayer.currentMoonSequence <= 7) damageMult = 1.5f;
            if (npc.damage < npc.defDamage * damageMult) npc.damage = (int)(npc.defDamage * damageMult);
            if (npc.damage < 10) npc.damage = 10;

            // --- 5. 移动控制逻辑 ---
            float distToOwner = npc.Distance(owner.Center);

            // [防丢传送] 距离太远直接拉回来
            if (distToOwner > 2000f)
            {
                npc.Center = owner.Center;
                npc.velocity = Vector2.Zero;
                npc.netUpdate = true;
            }

            // 寻找敌人
            NPC targetEnemy = FindClosestEnemy(npc, owner);
            if (targetEnemy != null)
            {
                npc.target = targetEnemy.whoAmI;
            }

            // --- 6. AI 接管判定 ---
            // 包含 3 (Fighter) 和 26 (Unicorn)，这是之前报错的根源
            int[] overrideStyles = { 3, 1, 26, 14, 2, 8, 5, 44, 10 };

            bool shouldOverrideAI = false;
            foreach (int style in overrideStyles)
            {
                if (npc.aiStyle == style) { shouldOverrideAI = true; break; }
            }

            if (shouldOverrideAI)
            {
                if (targetEnemy != null)
                {
                    MoveTowards(npc, targetEnemy.Center, 7f, 15f);
                    CheckCollisionDamage(npc, targetEnemy);
                }
                else
                {
                    // 回到主人身边
                    if (distToOwner > 200f) MoveTowards(npc, owner.Center, 6f, 20f);
                    else
                    {
                        npc.velocity.X *= 0.9f;
                        if (npc.noGravity) npc.velocity.Y *= 0.9f;

                        if (Math.Abs(npc.Center.X - owner.Center.X) > 20)
                            npc.direction = npc.spriteDirection = (owner.Center.X > npc.Center.X) ? 1 : -1;
                    }
                }
                return false; // 阻断原版 AI
            }

            // 对于 Boss (aiStyle 0 等)，我们允许原版 AI 运行，但在 PostAI 中强制修正阵营
            return true;
        }

        // ==========================================================
        // 4. 【下陷与Boss修复】 PostAI 强制修正
        // ==========================================================
        public override void PostAI(NPC npc)
        {
            if (npc.HasBuff(ModContent.BuffType<TamedBuff>()))
            {
                npc.friendly = true;

                // 【核心修复：沉入地下问题】
                // 只有当它是 Boss，或者原本就是飞行/穿墙单位时，才允许穿墙
                if (npc.boss || npc.noGravity)
                {
                    npc.noTileCollide = true;
                }
                else
                {
                    // 地面单位必须检测碰撞，否则会掉出世界
                    npc.noTileCollide = false;
                }
            }
        }

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            binaryWriter.Write(ownerIndex);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            ownerIndex = binaryReader.ReadInt32();
        }

        // --- 辅助方法 ---

        private void MoveTowards(NPC npc, Vector2 targetPos, float speedBase, float inertia)
        {
            float speed = speedBase;
            if (npc.noGravity) speed *= 1.2f;

            Vector2 direction = targetPos - npc.Center;

            // [修复] 如果是地面单位，不要直接飞向目标，而是要在地面跑
            if (!npc.noGravity)
            {
                float xDir = (direction.X > 0) ? 1 : -1;
                float xSpeedTarget = xDir * speed;
                npc.velocity.X = (npc.velocity.X * (inertia - 1) + xSpeedTarget) / inertia;

                // [简易跳跃逻辑]
                bool blocked = npc.collideX;
                bool targetAbove = targetPos.Y < npc.Center.Y - 32;
                if (blocked && targetAbove && npc.velocity.Y == 0)
                {
                    npc.velocity.Y = -7f;
                }
            }
            else
            {
                // 飞行单位直接飞
                if (direction != Vector2.Zero)
                {
                    direction.Normalize();
                    direction *= speed;
                    npc.velocity = (npc.velocity * (inertia - 1) + direction) / inertia;
                }
            }

            if (npc.velocity.X != 0)
                npc.direction = npc.spriteDirection = (npc.velocity.X > 0) ? 1 : -1;
        }

        private void CheckCollisionDamage(NPC me, NPC target)
        {
            if (attackCooldown > 0) { attackCooldown--; return; }

            if (me.getRect().Intersects(target.getRect()))
            {
                int damage = me.damage;
                target.SimpleStrikeNPC(damage, 0, false, 0f, DamageClass.Generic, false, 0f, true);

                if (me.noGravity) me.velocity *= -0.5f;
                else me.velocity.X *= -0.5f;

                attackCooldown = 30;
            }
        }

        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (npc.HasBuff(ModContent.BuffType<TamedBuff>()))
            {
                drawColor = Color.Lerp(drawColor, Color.HotPink, 0.5f);
            }
        }

        public override bool CanHitNPC(NPC npc, NPC target)
        {
            if (npc.HasBuff(ModContent.BuffType<TamedBuff>()) && !target.friendly && !target.HasBuff(ModContent.BuffType<TamedBuff>()))
                return true;
            return base.CanHitNPC(npc, target);
        }

        public override void ModifyHitNPC(NPC npc, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (npc.HasBuff(ModContent.BuffType<TamedBuff>()))
            {
                modifiers.ArmorPenetration += 15;
            }
        }

        private NPC FindClosestEnemy(NPC me, Player owner)
        {
            NPC closest = null;
            float minDist = 1500f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];
                if (other.active && !other.friendly && other.whoAmI != me.whoAmI && other.lifeMax > 5 && !other.dontTakeDamage && !other.HasBuff(ModContent.BuffType<TamedBuff>()))
                {
                    float dist = Vector2.Distance(me.Center, other.Center);
                    if (dist < minDist) { minDist = dist; closest = other; }
                }
            }
            return closest;
        }
    }
}