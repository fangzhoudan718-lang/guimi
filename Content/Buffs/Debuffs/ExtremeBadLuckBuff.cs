using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Terraria.Localization;
using Terraria.Audio;
using Terraria.DataStructures;

namespace zhashi.Content.Buffs.Debuffs
{
    public class ExtremeBadLuckBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = false;
            BuffID.Sets.LongerExpertDebuff[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // =========================
            // 下面全是由于运气差导致的物理后果
            // 不管是不是赢家，只要有这个Buff就必定触发！
            // =========================

            // 1. 属性小幅削弱 (精神不振，手感变差)
            player.statDefense -= 5; // 稍微扣一点护甲
            player.endurance -= 0.1f; // 受到的伤害增加10% 
            player.GetDamage(DamageClass.Generic) *= 0.85f; // 伤害降低15% 
            player.pickSpeed += 0.2f; // 挖掘速度变慢20% 

            // 2. 视觉折磨 (瞎眼)
            player.AddBuff(BuffID.Darkness, 2);
            player.AddBuff(BuffID.Blackout, 2);
            if (Main.rand.NextBool(5))
                Dust.NewDust(player.position, player.width, player.height, DustID.Smoke, 0, -2, 100, Color.Black, 2f);

            // 3. 漏财
            if (player.velocity.Length() > 0.1f && Main.rand.NextBool(180))
            {
                int coinIndex = Item.NewItem(player.GetSource_Buff(buffIndex), player.Center, ItemID.SilverCoin, 1);
                if (Main.item[coinIndex].active) Main.item[coinIndex].velocity = Main.rand.NextVector2Circular(5f, 5f);
                CombatText.NewText(player.getRect(), Color.Gray, "漏财!", true);
            }

            // 4. 平地摔
            if (player.velocity.Y == 0 && System.Math.Abs(player.velocity.X) > 2f && Main.rand.NextBool(300))
            {
                player.velocity.X = 0;
                player.AddBuff(BuffID.Dazed, 60);
                CombatText.NewText(player.getRect(), Color.Gray, "狠狠摔倒!", true);
                player.Hurt(PlayerDeathReason.ByCustomReason(NetworkText.FromLiteral(player.name + " 摔了个狗吃屎")), 30, 0);
            }

            // 5. 天灾 (雷劈/流星)
            if (player.whoAmI == Main.myPlayer && !Main.dayTime && player.ZoneOverworldHeight && Main.rand.NextBool(300))
            {
                Vector2 predictPos = player.Center + (player.velocity * 30f);
                Vector2 spawnPos = predictPos + new Vector2(Main.rand.Next(-20, 20), -600);
                Vector2 velocity = Vector2.Normalize(predictPos - spawnPos) * 25f;

                int p = Projectile.NewProjectile(player.GetSource_Buff(buffIndex), spawnPos, velocity, ProjectileID.FallingStar, 400, 10f, Main.myPlayer);
                Main.projectile[p].hostile = true;
                Main.projectile[p].friendly = false;
                Main.projectile[p].tileCollide = false;

                CombatText.NewText(player.getRect(), Color.OrangeRed, "死神来了!", true);
            }

            // 6. 武器反噬 (炸膛 / 割伤)
            if (player.itemAnimation > 0 && !player.HeldItem.IsAir && player.HeldItem.damage > 0)
            {
                if (Main.rand.NextBool(60))
                {
                    // 强制打断攻击动作
                    player.itemAnimation = 0;
                    player.itemTime = 0;

                    Item heldItem = player.HeldItem;

                    // 判断是否为近战武器
                    if (heldItem.CountsAsClass(DamageClass.Melee))
                    {
                        // --- 近战武器：意外割伤 / 武器脱手 ---
                        player.AddBuff(BuffID.Bleeding, 180); // 赋予3秒流血状态代替诅咒

                        // 伤害很低：只有面板的 20%，并且限制在 10 ~ 30 点伤害之间
                        int selfDamage = (int)(heldItem.damage * 0.2f);
                        if (selfDamage < 10) selfDamage = 10;
                        if (selfDamage > 30) selfDamage = 30;

                        player.Hurt(PlayerDeathReason.ByCustomReason(NetworkText.FromLiteral(player.name + " 挥舞时意外割伤了自己")), selfDamage, 0);

                        SoundEngine.PlaySound(SoundID.NPCHit1, player.position); // 肉体被击中的声音
                        CombatText.NewText(player.getRect(), Color.Red, "意外割伤!", true);
                    }
                    else
                    {
                        // --- 非近战武器 (远程/魔法等)：严重炸膛 ---
                        player.AddBuff(BuffID.Cursed, 120); // 诅咒状态

                        int selfDamage = (int)(heldItem.damage * 3.0f);
                        if (selfDamage < 50) selfDamage = 50;

                        player.Hurt(PlayerDeathReason.ByCustomReason(NetworkText.FromLiteral(player.name + " 倒霉到武器自爆")), selfDamage, 0);

                        SoundEngine.PlaySound(SoundID.Item14, player.position); // 爆炸音效
                        for (int i = 0; i < 15; i++) Dust.NewDust(player.position, player.width, player.height, DustID.Torch, 0, 0, 100, default, 2f);
                        CombatText.NewText(player.getRect(), Color.Red, "严重炸膛!", true);
                    }
                }
            }
        }
    }
}