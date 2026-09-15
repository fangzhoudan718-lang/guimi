using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Globals
{
    /// <summary>
    /// 命运启示期间, 玩家发射的所有非检测/警报类弹幕都会获得轻度追踪能力.
    /// (代表"你看到了击中敌人的最佳轨迹")
    /// </summary>
    public class RevelationHomingGlobalProjectile : GlobalProjectile
    {
        public override void PostAI(Projectile projectile)
        {
            // 只处理玩家友方弹幕
            if (!projectile.friendly || projectile.hostile) return;
            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers) return;

            Player owner = Main.player[projectile.owner];
            if (!owner.active || owner.dead) return;

            var mp = owner.GetModPlayer<LotMPlayer>();
            if (mp.revelationActiveTimer <= 0) return;

            // 排除一些不适合追踪的弹幕类型 (绳子/锚/钩子/灯笼/钓鱼线等)
            if (projectile.aiStyle == 7 // 抓钩
                || projectile.aiStyle == 13 // 绳子
                || projectile.aiStyle == 15 // 锤子
                || projectile.aiStyle == 61 // 钓鱼浮标
                || projectile.bobber) return;

            // 排除静止的弹幕 (火焰/光环类)
            if (projectile.velocity.LengthSquared() < 0.1f) return;

            // 找最近敌人
            NPC target = null;
            float minDist = 600f; // 启示提供较强的追踪范围
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.friendly || npc.dontTakeDamage) continue;
                if (npc.CanBeChasedBy(projectile))
                {
                    float d = projectile.Distance(npc.Center);
                    if (d < minDist)
                    {
                        minDist = d;
                        target = npc;
                    }
                }
            }

            if (target == null) return;

            // 轻度引导: 当前速度向目标方向偏移 (保留弹幕原有速度大小)
            Vector2 toTarget = (target.Center - projectile.Center).SafeNormalize(Vector2.Zero);
            float speed = projectile.velocity.Length();
            projectile.velocity = Vector2.Lerp(projectile.velocity.SafeNormalize(Vector2.Zero), toTarget, 0.08f) * speed;
        }
    }
}
