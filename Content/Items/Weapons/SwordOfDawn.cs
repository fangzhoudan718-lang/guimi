using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using zhashi.Content;
using zhashi.Content.Projectiles;

namespace zhashi.Content.Items.Weapons
{
    public class SwordOfDawn : ModItem
    {
        private bool castingSpell = false;

        public override void SetDefaults()
        {
            Item.damage = 72;
            Item.DamageType = DamageClass.Melee;
            Item.width = 60;
            Item.height = 60;
            Item.scale = 1.5f;
            Item.useTime = 35;
            Item.useAnimation = 35;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 7;
            Item.rare = ItemRarityID.Pink;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = 0;

            Item.consumable = false;
            Item.shoot = ProjectileID.None;
        }

        public override bool AltFunctionUse(Player player)
        {
            return true;
        }

        public override bool CanUseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();
            if (modPlayer.currentSequence > 6) return false;

            // ===如果是右键点击 (光之风暴)===
            if (player.altFunctionUse == 2)
            {
                // 【修改点】大招消耗从 100 提升到 400
                if (!modPlayer.TryConsumeSpirituality(400))
                {
                    return false; // 灵性不足
                }

                castingSpell = true;

                Item.useStyle = ItemUseStyleID.Thrust;
                Item.noUseGraphic = true;
                Item.useTime = 40;
                Item.useAnimation = 40;
                Item.shoot = ModContent.ProjectileType<LightStormController>();
                Item.shootSpeed = 0f;
                Item.consumable = true;
            }
            // ===如果是左键点击 (普通挥砍)===
            else
            {
                castingSpell = false;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noUseGraphic = false;
                Item.useTime = 35;
                Item.useAnimation = 35;
                Item.UseSound = SoundID.Item1;
                Item.shoot = ProjectileID.None;
                Item.consumable = false;
            }
            return true;
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (castingSpell)
            {
                position = Main.MouseWorld;
                damage = (int)(damage * 0.8f);
                SoundEngine.PlaySound(SoundID.Shatter, player.position);
                SoundEngine.PlaySound(SoundID.Item122, player.position);
            }
        }

        public override void OnConsumeItem(Player player)
        {
            Main.NewText("晨曦之剑崩解为纯粹的光芒...", 255, 215, 0);

            for (int i = 0; i < 30; i++)
            {
                Dust.NewDust(player.position, player.width, player.height, DustID.GoldFlame, 0, 0, 100, default, 2f);
            }
        }

        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            if (!castingSpell && Main.rand.NextBool(3))
            {
                Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.Enchanted_Gold);
            }
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, 180);
            for (int i = 0; i < 10; i++)
            {
                Vector2 speed = Main.rand.NextVector2Circular(3f, 3f);
                Dust d = Dust.NewDustPerfect(target.Center, DustID.GoldFlame, speed, 100, default, 1.5f);
                d.noGravity = true;
            }
        }

        public override void AddRecipes()
        {
            LocalizedText text = Language.GetOrRegister(Mod, "Conditions.IsSequence6", () => "需 序列6: 黎明骑士");
            Condition dawnCondition = new Condition(text, () => Main.LocalPlayer.GetModPlayer<LotMPlayer>().currentSequence <= 6);
            CreateRecipe()
                .AddIngredient(ItemID.Gel, 1)
                .AddCondition(dawnCondition)
                .Register();
        }
    }
}