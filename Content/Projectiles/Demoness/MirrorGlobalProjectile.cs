using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Projectiles.Demoness;

namespace zhashi.Content.Projectiles.Demoness
{
    // 1. 定义一张“出生证明”，用来标记这是镜像生成的弹幕
    public class EntitySource_MirrorClone : IEntitySource
    {
        public string Context => "MirrorClone";
    }

    public class MirrorGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            // --- 第一道防线：递归检查 ---
            // 如果这个弹幕拿着“镜像出生证明”，说明它是分身发射的，直接放行，不要再处理
            if (source is EntitySource_MirrorClone) return;

            // 检查玩家有效性
            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers) return;
            Player player = Main.player[projectile.owner];
            if (!player.active || player.dead) return;

            // 检查是否开启了分身
            int cloneType = ModContent.ProjectileType<MirrorCloneProjectile>();
            if (player.ownedProjectileCounts[cloneType] <= 0) return;

            // --- 第二道防线：类型严格过滤 ---
            // 我们只允许“脱手”的弹幕（子弹、箭、魔法球）。
            // 任何需要“拿在手里”或者“连接着玩家”的东西统统不要！
            if (IsHeldOrAttachedProjectile(projectile)) return;

            // 排除召唤物、哨兵、宠物、钩爪
            if (projectile.minion || projectile.sentry || projectile.minionSlots > 0) return;
            if (projectile.aiStyle == 7) return; // 钩爪
            if (projectile.type == cloneType) return; // 不复制分身自己

            // 寻找分身位置
            Projectile clone = null;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == cloneType && p.owner == player.whoAmI)
                {
                    clone = p;
                    break;
                }
            }
            if (clone == null) return;

            // --- 生成镜像弹幕 ---

            // 1. 计算相对位置
            Vector2 offset = projectile.Center - player.Center;

            // 2. 镜像位置：以分身为中心，X轴反转
            Vector2 mirrorPos = clone.Center + new Vector2(-offset.X, offset.Y);

            // 3. 镜像速度：X轴反转
            Vector2 mirrorVel = new Vector2(-projectile.velocity.X, projectile.velocity.Y);

            // 4. 发射！(带上出生证明)
            int newProjIndex = Projectile.NewProjectile(
                new EntitySource_MirrorClone(), // <--- 关键！防止无限循环
                mirrorPos,
                mirrorVel,
                projectile.type,
                projectile.damage,
                projectile.knockBack,
                projectile.owner,
                projectile.ai[0],
                projectile.ai[1]
            );

            if (newProjIndex < Main.maxProjectiles)
            {
                Projectile newProj = Main.projectile[newProjIndex];
                // 修正朝向
                newProj.direction = -projectile.direction;
                newProj.spriteDirection = -projectile.spriteDirection;

                // 稍微推开一点，防止在分身内部生成卡住
                newProj.position += newProj.velocity * 2f;
            }
        }

        // 严格的黑名单：所有“不脱手”的弹幕
        private bool IsHeldOrAttachedProjectile(Projectile p)
        {
            // 161: 短剑 (崩溃之源)
            // 19: 长矛
            // 20: 钻头/电锯
            // 99: 悠悠球
            // 15: 连枷 (球连着链子)
            // 13: 钩爪枪
            // 75: 弧光剑类
            // 190: 天顶剑 (Zenith)
            return p.aiStyle == 161 || p.aiStyle == 19 || p.aiStyle == 20 ||
                   p.aiStyle == 99 || p.aiStyle == 15 || p.aiStyle == 13 ||
                   p.aiStyle == 75 || p.aiStyle == 190;
        }
    }
}