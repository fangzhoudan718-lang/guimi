using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Globals
{
    /// <summary>
    /// 命运启示 — 时间感放缓:
    /// 任何玩家进入"命运启示"状态(revelationActiveTimer > 0)且 NPC 在其范围内,
    /// 该 NPC 速度被衰减为 50%, 动画帧也减半.
    /// 不依赖 buff 也不要新贴图.
    /// </summary>
    public class RevelationSlowGlobalNPC : GlobalNPC
    {
        // 启示影响范围 (像素)
        private const float REVELATION_RANGE = 3000f;

        // 判定是否有任意玩家处于启示状态且在范围内
        private static bool ShouldSlow(NPC npc)
        {
            if (npc.friendly || npc.dontTakeDamage) return false;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.active || p.dead) continue;
                var mp = p.GetModPlayer<LotMPlayer>();
                if (mp.revelationActiveTimer > 0 && npc.Distance(p.Center) < REVELATION_RANGE)
                    return true;
            }
            return false;
        }

        public override bool PreAI(NPC npc)
        {
            if (!ShouldSlow(npc)) return true;

            // 对蠕虫/Boss等复杂AI: 每2帧只跑一次AI, 实现50%慢动作
            // 普通小怪也走这条路径(同样50%减速)
            // 注意: 不能直接 velocity *= 0.5, 否则会持续叠加导致停滞
            if (Main.GameUpdateCount % 2 != 0)
            {
                // 偶数帧抵消一次本帧的位移, 等价于约50%速度
                npc.position -= npc.velocity * 0.5f;
                // 动画也放慢
                npc.frameCounter *= 0.5;
                return false; // 跳过AI
            }
            return true; // 奇数帧正常跑AI
        }

        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (ShouldSlow(npc))
            {
                // 微微金紫色化, 表示"被命运注视"
                drawColor.R = (byte)System.Math.Min(255, drawColor.R + 30);
                drawColor.G = (byte)System.Math.Max(0, drawColor.G - 20);
                drawColor.B = (byte)System.Math.Min(255, drawColor.B + 40);
            }
        }
    }

    /// <summary>
    /// 启示期间敌方弹幕减速 + 命运反弹.
    /// 改用 PostAI: 在 AI 跑完后修改 position, 避免 AI 内部重置 velocity 抵消我们的修改.
    /// </summary>
    public class RevelationSlowGlobalProjectile : GlobalProjectile
    {
        public override void PostAI(Projectile projectile)
        {
            if (projectile.friendly || !projectile.hostile) return;

            // 找出范围内是否有玩家处于启示状态
            Player closestRevPlayer = null;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.active || p.dead) continue;
                var mp = p.GetModPlayer<LotMPlayer>();
                if (mp.revelationActiveTimer > 0 && projectile.Distance(p.Center) < 3000f)
                {
                    closestRevPlayer = p;
                    break;
                }
            }
            if (closestRevPlayer == null) return;

            // [命运反弹] 距离玩家 < 200像素时反弹回敌人
            if (projectile.Distance(closestRevPlayer.Center) < 200f && !projectile.GetGlobalProjectile<RevelationReflectMarker>().reflected)
            {
                projectile.velocity = -projectile.velocity * 1.2f; // 反向并加速20%
                projectile.hostile = false;
                projectile.friendly = true;
                projectile.GetGlobalProjectile<RevelationReflectMarker>().reflected = true;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item104, projectile.position);
                for (int k = 0; k < 15; k++)
                {
                    Dust d = Dust.NewDustPerfect(projectile.Center,
                        DustID.GoldCoin, Main.rand.NextVector2Circular(4, 4), 0, default, 1.5f);
                    d.noGravity = true;
                }
                return;
            }

            // [减速] 每偶数帧把本帧的位移退回 50%, 等同于 50% 减速 (PostAI 这时改 position 有效)
            if (Main.GameUpdateCount % 2 == 0)
            {
                projectile.position -= projectile.velocity * 0.5f;
            }
        }
    }

    /// <summary>
    /// 标记弹幕是否已被命运反弹过(避免重复反弹).
    /// </summary>
    public class RevelationReflectMarker : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public bool reflected = false;
    }
}
