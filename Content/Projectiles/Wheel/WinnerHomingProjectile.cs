using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Terraria.DataStructures; // 【修复】正确引用 IEntitySource
using zhashi.Content;

namespace zhashi.Content.Projectiles.Wheel // 你的路径可能是在 Wheel 文件夹下，注意命名空间一致性
{
    public class WinnerHomingProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool isHomingBullet = false;

        // 【修复】移除了错误的命名空间前缀
        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            if (projectile.owner == Main.myPlayer && (projectile.DamageType == DamageClass.Ranged || projectile.DamageType == DamageClass.Magic))
            {
                Player player = Main.player[projectile.owner];
                var modPlayer = player.GetModPlayer<LotMPlayer>();

                if (modPlayer.currentWheelSequence <= 5 && player.luck > 0 && Main.rand.NextFloat() < 0.2f + (player.luck * 0.1f))
                {
                    isHomingBullet = true;
                    // 【修复】原版没有 tintGray，我们改一下透明度或者别的来标记
                    // 这里不做视觉修改，或者你可以生成一个特效
                    // projectile.alpha = 100; 
                }
            }
        }

        public override void AI(Projectile projectile)
        {
            if (isHomingBullet && projectile.friendly)
            {
                float homingRange = 400f;
                float homingSpeed = 10f;

                NPC target = null;
                foreach (NPC npc in Main.npc)
                {
                    if (npc.active && !npc.friendly && !npc.dontTakeDamage && npc.Distance(projectile.Center) < homingRange)
                    {
                        target = npc;
                        homingRange = npc.Distance(projectile.Center);
                    }
                }

                if (target != null)
                {
                    Vector2 direction = Vector2.Normalize(target.Center - projectile.Center);
                    // 平滑转向
                    projectile.velocity = Vector2.Lerp(projectile.velocity, direction * projectile.velocity.Length(), homingSpeed / 60f);
                }
            }
        }
    }
}