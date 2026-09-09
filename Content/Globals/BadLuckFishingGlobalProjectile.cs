using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Globals
{
    /// <summary>
    /// 命运途径特殊机制 - 厄运钓鱼:
    /// 当玩家身上有"灰色四叶草(厄运之骰)"或运气 < -0.5 时, 钓鱼会有几率钓出血月专属怪物.
    /// </summary>
    public class BadLuckFishingGlobalProjectile : GlobalProjectile
    {
        // 血月专属生物列表(钓鱼相关)
        // 真正的"血月才掉的鱼"主要是 ZombieMerman(僵尸人鱼) 和 WanderingEye(游荡眼球鱼);
        // GoblinShark(猪鲨)是血月相关.
        private static readonly int[] BloodMoonFishNPCs = new int[]
        {
            NPCID.ZombieMerman,        // 僵尸人鱼
            NPCID.WanderingEye,        // 游荡眼球鱼
            NPCID.GoblinShark,         // 猪鲨
            NPCID.BloodEelHead,        // 血鳗
        };

        // 标记本浮标是否已经判定过(避免连续触发)
        public override bool InstancePerEntity => true;
        public bool checkedBadLuck = false;

        public override void PostAI(Projectile projectile)
        {
            // 仅处理钓鱼浮标 (aiStyle 61 == 浮标 AI)
            if (!projectile.bobber) return;
            // ai[0] = 1 表示已经"咬钩" (有鱼上钩); ai[1] != 0 == 上钩物品id, 0=未咬
            // 我们要在它咬钩前介入: 判断玩家是否极低运气, 是的话改为生成血月NPC

            if (projectile.ai[0] != 1) return; // 不是咬钩状态
            if (checkedBadLuck) return;
            checkedBadLuck = true;

            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers) return;
            Player p = Main.player[projectile.owner];
            if (!p.active || p.dead) return;

            // 判定厄运: 玩家持有"灰色四叶草(MisfortuneDie)" 或 luck < -0.3
            var mp = p.GetModPlayer<LotMPlayer>();
            bool hasGray = mp.hasGrayClover;
            bool veryUnlucky = p.luck <= -0.3f;
            if (!hasGray && !veryUnlucky) return;

            // 触发几率: 持灰色四叶草30%; luck <= -0.5 时翻倍
            float chance = hasGray ? 0.30f : 0.15f;
            if (p.luck <= -0.5f) chance *= 2f;
            if (Main.rand.NextFloat() >= chance) return;

            // 把咬钩状态改为"无物品", 然后在浮标位置生成血月怪
            int npcType = BloodMoonFishNPCs[Main.rand.Next(BloodMoonFishNPCs.Length)];
            Vector2 spawnPos = projectile.position;
            // 让"咬钩"作废 - 立刻收线/作废这次咬钩
            projectile.ai[0] = 0;
            projectile.ai[1] = 0;

            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                int npcIdx = NPC.NewNPC(projectile.GetSource_FromAI(), (int)spawnPos.X, (int)spawnPos.Y, npcType);
                if (npcIdx < Main.maxNPCs)
                {
                    Main.NewText("[厄运] 一个不该被钓出的东西跃出水面...", 180, 30, 30);
                    CombatText.NewText(p.getRect(), Color.DarkRed, "命运的恶意!", true);
                    // 喷溅水花特效
                    for (int k = 0; k < 30; k++)
                    {
                        Dust d = Dust.NewDustPerfect(spawnPos,
                            DustID.Blood, Main.rand.NextVector2Circular(6, 6), 0, default, 1.6f);
                        d.noGravity = false;
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item54, spawnPos);
                }
            }
            else if (Main.netMode == NetmodeID.Server)
            {
                int npcIdx = NPC.NewNPC(projectile.GetSource_FromAI(), (int)spawnPos.X, (int)spawnPos.Y, npcType);
                if (npcIdx < Main.maxNPCs)
                {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npcIdx);
                }
            }
        }
    }
}
