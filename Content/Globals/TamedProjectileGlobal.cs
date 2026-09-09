using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using zhashi.Content.Buffs;
using zhashi.Content.NPCs;

namespace zhashi.Content.Globals
{
    public class TamedProjectileGlobal : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public bool isTamedMinionProjectile = false;

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            // 1. 检查来源是否是 NPC (如南瓜王发射镰刀)
            if (source is EntitySource_Parent parentSource && parentSource.Entity is NPC sourceNPC)
            {
                if (sourceNPC.HasBuff(ModContent.BuffType<TamedBuff>()))
                {
                    MarkAsTamed(projectile);
                }
            }
            // 2. 检查来源是否是另一个弹幕 (如镰刀爆炸)
            else if (source is EntitySource_Parent parentProjSource && parentProjSource.Entity is Projectile sourceProj)
            {
                if (sourceProj.TryGetGlobalProjectile(out TamedProjectileGlobal parentGlobal) && parentGlobal.isTamedMinionProjectile)
                {
                    MarkAsTamed(projectile);
                }
            }
        }

        private void MarkAsTamed(Projectile projectile)
        {
            isTamedMinionProjectile = true;
            projectile.friendly = true;
            projectile.hostile = false; // 初始设为非敌对
        }

        // 【关键】每帧强制覆盖，防止 Boss AI 把弹幕改回敌对
        public override void PostAI(Projectile projectile)
        {
            if (isTamedMinionProjectile)
            {
                projectile.friendly = true;
                projectile.hostile = false;
            }
        }

        // 双重保险：弹幕绝不伤害玩家
        public override bool CanHitPlayer(Projectile projectile, Player target)
        {
            if (isTamedMinionProjectile) return false;
            return base.CanHitPlayer(projectile, target);
        }

        // 允许弹幕伤害敌人
        public override bool? CanHitNPC(Projectile projectile, NPC target)
        {
            if (isTamedMinionProjectile)
            {
                if (!target.friendly && !target.HasBuff(ModContent.BuffType<TamedBuff>()))
                    return true;
            }
            return null;
        }
    }
}