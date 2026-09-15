using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using zhashi.Content.Items.Materials;

namespace zhashi.Content.Items.Door.Curios
{
    internal static class DoorCurioUtils
    {
        public static void AddCharacteristic(Recipe recipe, int amount = 1)
            => recipe.AddIngredient(ModContent.ItemType<SpiritEssence>(), amount);

        public static bool TryFindPassage(Player player, Vector2 direction, float range, bool requireWall, out Vector2 destination)
        {
            destination = player.position;
            direction = direction.SafeNormalize(new Vector2(player.direction, 0f));
            bool crossedWall = false;

            for (float distance = 12f; distance <= range; distance += 8f)
            {
                Vector2 candidate = player.position + direction * distance;
                if (candidate.X < 16f || candidate.Y < 16f ||
                    candidate.X + player.width > Main.maxTilesX * 16f - 16f ||
                    candidate.Y + player.height > Main.maxTilesY * 16f - 16f)
                    continue;
                bool blocked = Collision.SolidCollision(candidate, player.width, player.height);
                if (blocked)
                {
                    crossedWall = true;
                    continue;
                }

                if ((!requireWall || crossedWall) && !Collision.LavaCollision(candidate, player.width, player.height))
                {
                    destination = candidate;
                    return true;
                }
            }
            return false;
        }

        public static bool TryFindBlinkDestination(Player player, NPC target, out Vector2 destination)
        {
            destination = player.position;
            float preferredSide = player.Center.X <= target.Center.X ? 1f : -1f;
            float baseSeparation = target.width * 0.5f + player.width * 0.5f + 12f;
            float floorAlignedY = target.Bottom.Y - player.height;
            float[] sides = { preferredSide, -preferredSide };
            float[] extraDistances = { 0f, 8f, 16f, 24f, 32f };
            float[] upwardOffsets = { 0f, 8f, 16f, 24f, 32f, 48f };

            foreach (float side in sides)
            {
                foreach (float extraDistance in extraDistances)
                {
                    foreach (float upwardOffset in upwardOffsets)
                    {
                        Vector2 candidate = new Vector2(
                            target.Center.X + side * (baseSeparation + extraDistance) - player.width * 0.5f,
                            floorAlignedY - upwardOffset);

                        if (candidate.X < 16f || candidate.Y < 16f ||
                            candidate.X + player.width > Main.maxTilesX * 16f - 16f ||
                            candidate.Y + player.height > Main.maxTilesY * 16f - 16f)
                            continue;
                        if (Collision.SolidCollision(candidate, player.width, player.height) ||
                            Collision.LavaCollision(candidate, player.width, player.height))
                            continue;

                        destination = candidate;
                        return true;
                    }
                }
            }

            // 狭窄地形下最后尝试目标正上方，仍然必须通过完整的实体方块与熔岩检查。
            Vector2 aboveTarget = new Vector2(target.Center.X - player.width * 0.5f,
                target.Top.Y - player.height - 12f);
            if (aboveTarget.X >= 16f && aboveTarget.Y >= 16f &&
                aboveTarget.X + player.width <= Main.maxTilesX * 16f - 16f &&
                aboveTarget.Y + player.height <= Main.maxTilesY * 16f - 16f &&
                !Collision.SolidCollision(aboveTarget, player.width, player.height) &&
                !Collision.LavaCollision(aboveTarget, player.width, player.height))
            {
                destination = aboveTarget;
                return true;
            }

            return false;
        }

        public static void TeleportLocalPlayer(Player player, Vector2 destination)
        {
            player.Teleport(destination, 1);
            player.GetModPlayer<Pathways.Door.DoorPathwayPlayer>().NoteTeleportUsed();
            player.immune = true;
            player.immuneTime = Math.Max(player.immuneTime, 20);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, player.whoAmI,
                    destination.X, destination.Y, 1);
        }

        public static void Feedback(Player player, LocalizedText text, Color color)
        {
            if (player.whoAmI == Main.myPlayer)
                CombatText.NewText(player.getRect(), color, text.Value);
        }

        public static void Feedback(Player player, string text, Color color)
        {
            if (player.whoAmI == Main.myPlayer)
                CombatText.NewText(player.getRect(), color, text);
        }
    }

    /// <summary>
    /// 所有主动门途径神奇物品统一从灵性池支付代价。服务端不重复扣除，
    /// 玩家资源会由现有 PlayerSync 流程同步，避免多人游戏一次使用扣两次。
    /// </summary>
    public abstract class SpiritualityCurioItem : ModItem
    {
        public abstract float SpiritualityCost { get; }

        public override bool CanUseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer)
                return true;

            LotMPlayer lotm = player.GetModPlayer<LotMPlayer>();
            if (lotm.TryConsumeSpirituality(SpiritualityCost))
                return true;

            DoorCurioUtils.Feedback(player,
                Language.GetTextValue("Mods.zhashi.Messages.DoorCurioNotEnoughSpirituality", SpiritualityCost),
                new Color(170, 125, 205));
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "DoorCurioSpiritualityCost",
                Language.GetTextValue("Mods.zhashi.Messages.DoorCurioSpiritualityCost", SpiritualityCost))
            {
                OverrideColor = new Color(178, 145, 220)
            });
        }
    }

    /// <summary>序列九「开门」的弱化造物：只够穿过一道不太厚的墙。</summary>
    public class WallpassingKey : SpiritualityCurioItem
    {
        public static LocalizedText NoPassageText { get; private set; }
        public override float SpiritualityCost => 8f;

        private Vector2 preparedDestination;
        private bool hasPreparedDestination;

        public override void SetStaticDefaults() => NoPassageText = this.GetLocalization(nameof(NoPassageText));

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 34;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 75;
            Item.useAnimation = 75;
            Item.UseSound = SoundID.Item8;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(gold: 1);
            Item.noMelee = true;
        }

        public override bool CanUseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                Vector2 direction = Main.MouseWorld - player.Center;
                hasPreparedDestination = DoorCurioUtils.TryFindPassage(player, direction, 160f, true,
                    out preparedDestination);
                if (!hasPreparedDestination)
                {
                    DoorCurioUtils.Feedback(player, NoPassageText, new Color(150, 135, 190));
                    return false;
                }
            }
            return base.CanUseItem(player);
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer) return true;
            if (!hasPreparedDestination || Collision.SolidCollision(preparedDestination, player.width, player.height))
            {
                DoorCurioUtils.Feedback(player, NoPassageText, new Color(150, 135, 190));
                return false;
            }
            DoorCurioUtils.TeleportLocalPlayer(player, preparedDestination);
            hasPreparedDestination = false;
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            DoorCurioUtils.AddCharacteristic(recipe, 1);
            recipe.AddIngredient(ItemID.GoldenKey).AddIngredient(ItemID.FallenStar, 3)
                .AddTile(TileID.Anvils).Register();
        }
    }

    /// <summary>一只与私人存钱罐空间相连的小袋子。</summary>
    public class SpatialPocket : SpiritualityCurioItem
    {
        public override float SpiritualityCost => 4f;

        public override void SetDefaults()
        {
            // 复用原版钱袋的完整开箱、关箱和多人同步流程，避免直接写 chest=-2 导致卡 UI。
            Item.CloneDefaults(ItemID.MoneyTrough);
            Item.mana = 0;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.sellPrice(gold: 2);
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            DoorCurioUtils.AddCharacteristic(recipe, 2);
            recipe.AddIngredient(ItemID.Silk, 12).AddIngredient(ItemID.PiggyBank)
                .AddIngredient(ItemID.FallenStar, 5).AddTile(TileID.Loom).Register();
        }
    }

    /// <summary>把戏法大师的元素戏法压进一根小手杖。</summary>
    public class TricksterWand : SpiritualityCurioItem
    {
        public override float SpiritualityCost => 3f;

        private static readonly int[] Bolts =
        {
            ProjectileID.AmethystBolt, ProjectileID.TopazBolt, ProjectileID.SapphireBolt,
            ProjectileID.EmeraldBolt, ProjectileID.RubyBolt, ProjectileID.DiamondBolt
        };

        public override void SetStaticDefaults()
        {
            // 对角线手杖按原版法杖姿势旋转，握点落在下端手柄而非贴图中心。
            Item.staff[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 32;
            Item.damage = 24;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 0;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.knockBack = 2f;
            Item.shootSpeed = 9f;
            Item.shoot = ProjectileID.AmethystBolt;
            Item.UseSound = SoundID.Item8;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.sellPrice(gold: 2);
        }

        public override Vector2? HoldoutOffset() => new Vector2(-4f, 0f);

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int bolt = Bolts[Main.rand.Next(Bolts.Length)];
            float turn = Main.rand.NextFloat(-0.10f, 0.10f);
            Projectile.NewProjectile(source, position, velocity.RotatedBy(turn), bolt, damage, knockback, player.whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            DoorCurioUtils.AddCharacteristic(recipe, 1);
            recipe.AddIngredient(ItemID.WandofSparking).AddIngredient(ItemID.FallenStar, 5)
                .AddIngredient(ItemID.Amethyst).AddIngredient(ItemID.Topaz).AddTile(TileID.Anvils).Register();
        }
    }

    public class AstrologerCompass : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 3);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.dangerSense = true;
            player.findTreasure = true;
            player.detectCreature = true;
            player.luck += 0.05f;
            player.GetCritChance(DamageClass.Generic) += 4f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            DoorCurioUtils.AddCharacteristic(recipe, 2);
            recipe.AddIngredient(ItemID.Compass).AddIngredient(ItemID.Lens, 3)
                .AddIngredient(ItemID.FallenStar, 8).AddTile(TileID.TinkerersWorkbench).Register();
        }
    }

    /// <summary>命中后留下一段记录，短暂延迟后重现部分伤害。</summary>
    public class RecordingQuill : SpiritualityCurioItem
    {
        public override float SpiritualityCost => 6f;

        public override void SetStaticDefaults()
        {
            // 羽笔同样是从笔杆末端握持的施法媒介。
            Item.staff[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 32;
            Item.damage = 30;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 0;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 27;
            Item.useAnimation = 27;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.knockBack = 2.5f;
            Item.shootSpeed = 11f;
            Item.shoot = ModContent.ProjectileType<RecordingQuillProjectile>();
            Item.UseSound = SoundID.Item9;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 4);
        }

        public override Vector2? HoldoutOffset() => new Vector2(-4f, 0f);

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            DoorCurioUtils.AddCharacteristic(recipe, 2);
            recipe.AddIngredient(ItemID.Feather, 6).AddIngredient(ItemID.Book)
                .AddIngredient(ItemID.ManaCrystal).AddTile(TileID.Bookcases).Register();
        }
    }

    public class TravelerBoots : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 26;
            Item.accessory = true;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 4);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.moveSpeed += 0.14f;
            player.maxRunSpeed += 0.7f;
            player.jumpSpeedBoost += 1.2f;
            player.noFallDmg = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            DoorCurioUtils.AddCharacteristic(recipe, 2);
            recipe.AddIngredient(ItemID.HermesBoots).AddIngredient(ItemID.Silk, 8)
                .AddIngredient(ItemID.FallenStar, 6).AddTile(TileID.TinkerersWorkbench).Register();
        }
    }

    /// <summary>命中后把使用者挪到目标另一侧，模拟一次很短的闪现。</summary>
    public class BlinkDagger : SpiritualityCurioItem
    {
        public override float SpiritualityCost => 8f;

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 34;
            Item.damage = 38;
            Item.DamageType = DamageClass.Melee;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 24;
            Item.useAnimation = 24;
            Item.useTurn = true;
            Item.knockBack = 3.5f;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.LightRed;
            Item.value = Item.sellPrice(gold: 5);
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 近战命中由持有者客户端确认传送，再用原版 TeleportEntity 同步，
            // 可避免专服上 OnHitNPC 执行时机不一致造成“命中但不闪现”。
            if (player.whoAmI != Main.myPlayer ||
                !DoorCurioUtils.TryFindBlinkDestination(player, target, out Vector2 destination))
                return;

            Vector2 oldPosition = player.position;
            DoorCurioUtils.TeleportLocalPlayer(player, destination);
            player.velocity = Vector2.Zero;
            SoundEngine.PlaySound(SoundID.Item8, player.Center);
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(oldPosition, player.width, player.height, DustID.PurpleTorch,
                    Main.rand.NextFloat(-1.2f, 1.2f), Main.rand.NextFloat(-1.2f, 1.2f), 120, default, 0.85f);
                Dust.NewDust(player.position, player.width, player.height, DustID.PurpleTorch,
                    Main.rand.NextFloat(-1.2f, 1.2f), Main.rand.NextFloat(-1.2f, 1.2f), 120, default, 0.85f);
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            DoorCurioUtils.AddCharacteristic(recipe, 3);
            recipe.AddIngredient(ItemID.MagicDagger).AddIngredient(ItemID.SoulofLight, 8)
                .AddIngredient(ItemID.FallenStar, 8).AddTile(TileID.MythrilAnvil).Register();
        }
    }

    /// <summary>把目标短暂推离现实边缘；Boss只受到空间推力。</summary>
    public class BanishmentBell : SpiritualityCurioItem
    {
        public override float SpiritualityCost => 25f;

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 90;
            Item.useAnimation = 90;
            Item.mana = 0;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<BanishmentPulseProjectile>();
            Item.shootSpeed = 0f;
            Item.UseSound = SoundID.Item35;
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.sellPrice(gold: 6);
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            DoorCurioUtils.AddCharacteristic(recipe, 3);
            recipe.AddIngredient(ItemID.Bell).AddIngredient(ItemID.SoulofNight, 10)
                .AddIngredient(ItemID.Bone, 20).AddTile(TileID.MythrilAnvil).Register();
        }
    }

    public class MirrorDoorShard : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 34;
            Item.accessory = true;
            Item.rare = ItemRarityID.LightRed;
            Item.value = Item.sellPrice(gold: 5);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.endurance += 0.06f;
            player.longInvince = true;
            player.aggro -= 250;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            DoorCurioUtils.AddCharacteristic(recipe, 2);
            recipe.AddIngredient(ItemID.MagicMirror).AddIngredient(ItemID.Glass, 20)
                .AddIngredient(ItemID.SoulofLight, 6).AddTile(TileID.TinkerersWorkbench).Register();
        }
    }

    public class StarThreadSpool : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true;
            Item.rare = ItemRarityID.LightRed;
            Item.value = Item.sellPrice(gold: 5);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetAttackSpeed(DamageClass.Magic) += 0.08f;
            player.GetDamage(DamageClass.Magic) += 0.08f;
            player.GetCritChance(DamageClass.Magic) += 4f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            DoorCurioUtils.AddCharacteristic(recipe, 2);
            recipe.AddIngredient(ItemID.WhiteString).AddIngredient(ItemID.Cobweb, 30)
                .AddIngredient(ItemID.FallenStar, 10).AddTile(TileID.TinkerersWorkbench).Register();
        }
    }

    /// <summary>第一次使用记录脚下位置，第二次使用返回；记录不跨世界保存。</summary>
    public class DoorwayChalk : SpiritualityCurioItem
    {
        public override float SpiritualityCost => 10f;
        public static LocalizedText MarkedText { get; private set; }
        public static LocalizedText ReturnedText { get; private set; }
        public static LocalizedText BlockedText { get; private set; }

        public override void SetStaticDefaults()
        {
            MarkedText = this.GetLocalization(nameof(MarkedText));
            ReturnedText = this.GetLocalization(nameof(ReturnedText));
            BlockedText = this.GetLocalization(nameof(BlockedText));
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 45;
            Item.useAnimation = 45;
            Item.UseSound = SoundID.Item8;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 3);
            Item.noMelee = true;
        }

        public override bool CanUseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                DoorCurioPlayer curio = player.GetModPlayer<DoorCurioPlayer>();
                if (curio.hasChalkMark && curio.chalkWorldId == Main.worldID &&
                    Collision.SolidCollision(curio.chalkMark, player.width, player.height))
                {
                    curio.hasChalkMark = false;
                    DoorCurioUtils.Feedback(player, BlockedText, new Color(210, 120, 150));
                    return false;
                }
            }
            return base.CanUseItem(player);
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer) return true;
            DoorCurioPlayer curio = player.GetModPlayer<DoorCurioPlayer>();
            if (!curio.hasChalkMark || curio.chalkWorldId != Main.worldID)
            {
                curio.hasChalkMark = true;
                curio.chalkWorldId = Main.worldID;
                curio.chalkMark = player.position;
                DoorCurioUtils.Feedback(player, MarkedText, new Color(170, 150, 220));
                return true;
            }

            DoorCurioUtils.TeleportLocalPlayer(player, curio.chalkMark);
            curio.hasChalkMark = false;
            DoorCurioUtils.Feedback(player, ReturnedText, new Color(170, 150, 220));
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            DoorCurioUtils.AddCharacteristic(recipe, 2);
            recipe.AddIngredient(ItemID.StoneBlock, 20).AddIngredient(ItemID.Bone, 5)
                .AddIngredient(ItemID.FallenStar, 5).AddTile(TileID.Anvils).Register();
        }
    }

    public class PocketDoorLantern : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 34;
            Item.accessory = true;
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.sellPrice(gold: 6);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.nightVision = true;
            player.dangerSense = true;
            player.aggro -= 200;
            if (!Main.dedServ)
                Lighting.AddLight(player.Center, new Vector3(0.18f, 0.12f, 0.30f));
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            DoorCurioUtils.AddCharacteristic(recipe, 2);
            recipe.AddIngredient(ItemID.HeartLantern).AddIngredient(ItemID.Glass, 12)
                .AddIngredient(ItemID.SoulofNight, 6).AddTile(TileID.MythrilAnvil).Register();
        }
    }

    public class DoorCurioPlayer : ModPlayer
    {
        public bool hasChalkMark;
        public Vector2 chalkMark;
        public int chalkWorldId = -1;

        public override void OnEnterWorld()
        {
            hasChalkMark = false;
            chalkWorldId = -1;
        }
    }

    public class RecordingQuillProjectile : ModProjectile
    {
        public override string Texture => "zhashi/Content/Items/Door/Curios/RecordingQuill";

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.scale = 0.78f;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Lighting.AddLight(Projectile.Center, new Vector3(0.08f, 0.05f, 0.15f));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
                ModContent.ProjectileType<RecordedEchoProjectile>(), Math.Max(1, damageDone / 3), 0f,
                Projectile.owner, target.whoAmI);
        }
    }

    /// <summary>
    /// 隐形的服务端裁定脉冲。由物品射击流程自然同步到服务器，确保放逐铃在单机和联机均只结算一次。
    /// </summary>
    public class BanishmentPulseProjectile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 3;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;
        public override bool PreDraw(ref Color lightColor) => false;

        public override void AI()
        {
            if (Projectile.ai[0] != 0f)
                return;

            Projectile.ai[0] = 1f;
            if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers && Main.player[Projectile.owner].active)
                Projectile.Center = Main.player[Projectile.owner].Center;

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy() || npc.Distance(Projectile.Center) > 300f)
                    continue;

                Vector2 away = (npc.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                npc.velocity += away * (npc.boss ? 2.5f : 8f);
                npc.AddBuff(BuffID.Slow, npc.boss ? 60 : 180);
                if (!npc.boss)
                    npc.AddBuff(BuffID.Confused, 150);
                npc.netUpdate = true;
            }
        }
    }

    public class RecordedEchoProjectile : ModProjectile
    {
        public override string Texture => "zhashi/Content/Items/Door/Curios/RecordingQuill";

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 28;
            Projectile.scale = 0.65f;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            int targetIndex = (int)Projectile.ai[0];
            if (targetIndex < 0 || targetIndex >= Main.maxNPCs || !Main.npc[targetIndex].active)
            {
                Projectile.Kill();
                return;
            }

            NPC target = Main.npc[targetIndex];
            Projectile.Center = target.Center + new Vector2(0f, -target.height * 0.6f - 12f);
            Projectile.rotation += 0.12f;
            Projectile.Opacity = MathHelper.Clamp(Projectile.timeLeft / 12f, 0f, 1f);

            if (Projectile.timeLeft == 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int direction = target.direction == 0 ? 1 : target.direction;
                if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers && Main.player[Projectile.owner].active)
                    direction = target.Center.X >= Main.player[Projectile.owner].Center.X ? 1 : -1;
                target.SimpleStrikeNPC(Projectile.damage, direction, false, 0f, DamageClass.Magic, false);
                target.netUpdate = true;
            }
        }
    }
}
