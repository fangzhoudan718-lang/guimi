using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Items
{
    public class FireEnchantGlobalItem : GlobalItem
    {
        public override bool InstancePerEntity => true;

        // === 1. 增加伤害和攻速 ===
        public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();
            if (modPlayer.isFireEnchanted && item.damage > 0)
            {
                // 基础增加 20% 伤害
                float damageBonus = 0.20f;
                // 序列越高加成越高
                if (modPlayer.currentHunterSequence <= 5) damageBonus += 0.20f;

                damage += damageBonus;
            }
        }

        // === 2. 增加攻速 (针对已有火属性的武器) ===
        public override float UseSpeedMultiplier(Item item, Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();
            if (modPlayer.isFireEnchanted)
            {
                // 如果武器本身能造成燃烧，或者名字里带"Fire/Flame/Hell/Magma"，攻速+30%
                // 这里简单判断：如果它是近战且造成火伤(泰拉瑞亚没直接属性，我们简单提升所有近战攻速)
                if (item.DamageType == DamageClass.Melee)
                {
                    return 1.3f;
                }
            }
            return 1.0f;
        }

        // === 3. 视觉特效：武器冒火 ===
        public override void MeleeEffects(Item item, Player player, Rectangle hitbox)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();
            if (modPlayer.isFireEnchanted)
            {
                if (Main.rand.NextBool(2))
                {
                    int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.Torch);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= 0.5f;
                    // 序列5以上变成狱火(蓝火)
                    if (modPlayer.currentHunterSequence <= 5) Main.dust[dust].type = DustID.BlueTorch;
                }
            }
        }

        // === 4. 命中施加燃烧 ===
        public override void OnHitNPC(Item item, Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();
            if (modPlayer.isFireEnchanted)
            {
                target.AddBuff(BuffID.OnFire, 300);
                if (modPlayer.currentHunterSequence <= 5)
                {
                    target.AddBuff(BuffID.OnFire3, 300); // 狱火
                    target.AddBuff(BuffID.Oiled, 300); // 油性
                }
            }
        }
    }
}