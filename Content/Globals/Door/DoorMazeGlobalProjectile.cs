using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using zhashi.Content.Projectiles.Door;

namespace zhashi.Content.Globals.Door
{
    /// <summary>
    /// 时空迷宫的圆环同样拨动弹幕：圈内飞行的弹幕会跟着“钟表指针”一起绕圆心转。
    ///
    /// 门途径自己的弹幕（领域、黑洞、裂隙、门扉）是场地本身，自转只会看起来坏掉，所以整组跳过；
    /// 其余弹幕——原版的、其它模组的、敌我的——都会被卷进去。
    /// </summary>
    public class DoorMazeGlobalProjectile : GlobalProjectile
    {
        public override void PostAI(Projectile projectile)
        {
            // 本模组的弹幕不动：它们是“场地”，不是被卷进来的东西。
            if (projectile.ModProjectile?.Mod == Mod) return;
            if (!projectile.active || projectile.velocity == Vector2.Zero) return;

            int domainType = ModContent.ProjectileType<DoorDomainProjectile>();
            foreach (Projectile domain in Main.ActiveProjectiles)
            {
                if (!domain.active || domain.type != domainType || (int)domain.ai[0] != 2) continue;
                float radius = domain.ai[1];
                Vector2 offset = projectile.Center - domain.Center;
                if (offset.LengthSquared() < 1f || offset.LengthSquared() > radius * radius) continue;

                float spin = MathHelper.TwoPi / 240f;
                projectile.Center = domain.Center + offset.RotatedBy(spin);
                projectile.velocity = projectile.velocity.RotatedBy(spin);
                return;
            }
        }
    }
}
