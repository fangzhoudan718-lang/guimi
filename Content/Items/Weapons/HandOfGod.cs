using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Projectiles;

namespace zhashi.Content.Items.Weapons
{
    public class HandOfGod : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 8000;
            Item.DamageType = DamageClass.Melee;
            Item.width = 60;
            Item.height = 60;
            Item.scale = 1f;

            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;

            Item.knockBack = 20f;
            Item.value = Item.sellPrice(platinum: 5);
            Item.rare = ItemRarityID.Red;
            Item.UseSound = SoundID.Item71;

            // 默认属性
            Item.channel = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<HandOfGodProjectile>();
            Item.shootSpeed = 1f;
        }

        public override bool AltFunctionUse(Player player)
        {
            return true;
        }

        public override bool CanUseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 负面效果检测
            if (modPlayer.currentSequence > 1)
            {
                player.statLife -= 500;
                if (player.statLife <= 0)
                    player.KillMe(PlayerDeathReason.ByCustomReason(player.name + " 崩溃了。"), 9999, 0);
                return false;
            }

            // =========================================================
            // 【核心修复：动态切换武器属性】
            // 我们在开火前，直接篡改武器的属性，让引擎认为这是合法的
            // =========================================================
            if (player.altFunctionUse == 2)
            {
                // 【右键模式】变成一把蓄力法杖
                Item.noUseGraphic = true;
                Item.channel = true;        // 开启蓄力
                Item.autoReuse = false;     // 关闭连发 (蓄力必须关连发)
                Item.useTime = 20;
                Item.useAnimation = 20;

                // ★最关键的一步★：告诉引擎，我现在射的是蓄力弹幕！
                Item.shoot = ModContent.ProjectileType<HandOfGodCharge>();
                Item.shootSpeed = 1f; // 给个速度，虽然我们会重写位置，但这能保证 projectile.velocity 不为0
            }
            else
            {
                // 【左键模式】变成一把挥舞大剑
                Item.noUseGraphic = true;
                Item.channel = false;       // 关闭蓄力
                Item.autoReuse = true;      // 开启连发
                Item.useTime = 30;
                Item.useAnimation = 30;

                // 告诉引擎，我现在射的是大剑！
                Item.shoot = ModContent.ProjectileType<HandOfGodProjectile>();
                Item.shootSpeed = 1f;
            }

            // 限制弹幕数量，防止重叠
            return player.ownedProjectileCounts[Item.shoot] < 1;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 因为我们在 CanUseItem 里已经把 Item.shoot 改对了
            // 所以这里直接 return true，让游戏引擎帮我们生成弹幕
            // 这样能最大程度保证 channel 逻辑的稳定性
            return true;
        }
    }
}