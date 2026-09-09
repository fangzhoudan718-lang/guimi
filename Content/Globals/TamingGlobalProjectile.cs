using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Buffs;

namespace zhashi.Content.Globals
{
    public class TamingGlobalProjectile : GlobalProjectile
    {
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!ProjectileID.Sets.IsAWhip[projectile.type])
                return;

            Player player = Main.player[projectile.owner];
            if (player == null || !player.active) return;

            LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

            if (modPlayer.currentMoonSequence <= 8)
            {

            }
        }

        private void TryTameNPC(NPC target)
        {

            if (!target.boss && target.active && !target.friendly && target.lifeMax > 5 && target.life <= target.lifeMax * 0.3f && !target.HasBuff(ModContent.BuffType<TamedBuff>()))
            {
                // 给予驯服 Buff (持续 3 分钟 = 10800 帧)
                target.AddBuff(ModContent.BuffType<TamedBuff>(), 10800);

                // 稍微回点血，防止它刚被驯服就被流弹打死
                int heal = target.lifeMax / 3;
                target.life += heal;
                if (target.life > target.lifeMax) target.life = target.lifeMax;
                target.HealEffect(heal);

                // 视觉提示
                CombatText.NewText(target.getRect(), Color.HotPink, "驯服成功!", true);

                // 粒子特效
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDust(target.position, target.width, target.height, DustID.HeartCrystal, 0, -2, 0, default, 1.5f);
                }
            }
            else if (!target.boss && !target.friendly && !target.HasBuff(ModContent.BuffType<TamedBuff>()))
            {
                // 提示玩家削血 (每 15 次提示一次，防止刷屏)
                if (Main.rand.NextBool(15))
                    CombatText.NewText(target.getRect(), Color.Gray, "需削弱至30%血量...", false);
            }
        }
    }
}