using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content;
using zhashi.Content.Projectiles;

namespace zhashi.Content.Items.Weapons
{
    public class TwilightGiantSword : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 450;
            Item.DamageType = DamageClass.Melee;
            Item.width = 283;
            Item.height = 250;

            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.useTurn = true;
            Item.knockBack = 10;
            Item.value = Item.buyPrice(platinum: 5);
            Item.rare = ItemRarityID.Red;
            Item.UseSound = SoundID.Item71;
            Item.autoReuse = true;

            Item.shoot = ModContent.ProjectileType<TwilightSwordBeam>();
            Item.shootSpeed = 20f;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            TooltipLine warningLine = new TooltipLine(Mod, "DestructWarning", "警告：右键将献祭所有灵性与生命(降至1点)，释放毁灭性风暴！");
            warningLine.OverrideColor = Color.Red;
            tooltips.Add(warningLine);

            tooltips.Add(new TooltipLine(Mod, "Desc", "左键：挥舞并发射穿墙剑气\n右键：灵性决定风暴的伤害与持续时间"));
        }

        public override Vector2? HoldoutOrigin() { return new Vector2(39, 205); }

        public override bool AltFunctionUse(Player player) { return true; }

        public override bool CanUseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();
            if (modPlayer.currentSequence > 2) return false;

            // 右键检测：稍微有一点灵性就能放（哪怕1点），因为这是拼命技
            if (player.altFunctionUse == 2)
            {
                if (modPlayer.spiritualityCurrent <= 0) return false;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // === 右键：黄昏风暴 (绝命一击) ===
            if (player.altFunctionUse == 2)
            {
                var modPlayer = player.GetModPlayer<LotMPlayer>();

                // 1. 获取并清空所有灵性
                float consumedSpirit = modPlayer.spiritualityCurrent;
                modPlayer.spiritualityCurrent = 0;

                // 2. 【新增】强制扣血至 1 点
                if (player.statLife > 1)
                {
                    player.statLife = 1;
                    CombatText.NewText(player.getRect(), Color.DarkRed, "生命献祭!", true);
                    // 播放一个沉闷的扣血声
                    SoundEngine.PlaySound(SoundID.NPCDeath10, player.position);
                }

                // 3. 计算伤害 (基于消耗的灵性)
                // 基准：1000灵性 = 1倍面板伤害
                float multiplier = consumedSpirit / 1000f;
                int finalDamage = (int)(damage * 2 * multiplier);
                if (finalDamage < 1) finalDamage = 1;

                SoundEngine.PlaySound(SoundID.Item117, player.position);
                SoundEngine.PlaySound(SoundID.Item14, player.position);

                // 4. 发射风暴弹幕
                Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero, ModContent.ProjectileType<TwilightStormProjectile>(), finalDamage, 0, player.whoAmI, consumedSpirit);

                // 5. 销毁武器
                Main.NewText("黄昏巨剑已献祭...", 255, 100, 0);
                for (int i = 0; i < 50; i++) Dust.NewDust(player.position, player.width, player.height, DustID.OrangeTorch, 0, 0, 0, default, 2f);
                Item.TurnToAir();

                return false;
            }

            // 左键：剑气
            Vector2 muzzleOffset = Vector2.Normalize(velocity) * 120f;
            if (Collision.CanHit(position, 0, 0, position + muzzleOffset, 0, 0)) position += muzzleOffset;
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);

            return false;
        }

        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.OrangeTorch);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.5f;
            }
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Daybreak, 180);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.LunarBar, 20)
                .AddIngredient(ItemID.FragmentSolar, 20)
                .AddIngredient(ModContent.ItemType<SilverRapier>())
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}