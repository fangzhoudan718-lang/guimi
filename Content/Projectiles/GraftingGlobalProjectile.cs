using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content;

namespace zhashi.Content.Projectiles
{
    public class GraftingGlobalProjectile : GlobalProjectile
    {
        public override void AI(Projectile projectile)
        {
            // 仅处理敌对弹幕
            if (projectile.hostile && projectile.active)
            {
                // 遍历所有玩家
                foreach (Player p in Main.ActivePlayers)
                {
                    LotMPlayer modPlayer = p.GetModPlayer<LotMPlayer>();

                    // 如果玩家开启了 嫁接模式 1 (空间嫁接/反弹)
                    if (modPlayer.currentFoolSequence <= 1 && modPlayer.graftingMode == 1)
                    {
                        // 距离检测 (例如 300 像素内)
                        if (projectile.Distance(p.Center) < 300f)
                        {
                            // 嫁接：修改归属权，反弹回去！
                            projectile.hostile = false;
                            projectile.friendly = true;
                            projectile.owner = p.whoAmI;
                            projectile.velocity = -projectile.velocity * 2f; // 加速反弹

                            // 特效
                            for (int i = 0; i < 5; i++) Dust.NewDust(projectile.position, projectile.width, projectile.height, Terraria.ID.DustID.Vortex, 0, 0, 0, default, 1.5f);
                        }
                    }
                }
            }
        }
    }
}