using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using zhashi.Content.Dimensions;
using System;
using Terraria.Localization;

namespace zhashi.Content.Items
{
    public class SpiritWorldKey : ModItem
    {
        // 引导时间 (180帧 = 3秒)
        private const int ChannelTime = 180;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("灵界之匙");
            // Tooltip... (已在hjson中设置)
        }

        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 34;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.rare = ItemRarityID.Red;
            Item.channel = true;
            Item.noUseGraphic = true;
            Item.autoReuse = false;
            Item.UseSound = SoundID.Item103;
        }

        public override void HoldItem(Player player)
        {
            if (player.channel)
            {
                // 1. 强力定身 & 悬浮感
                player.velocity *= 0.05f; // 稍微给一点点滑行感，不像0.01那么死板
                player.fallStart = (int)(player.position.Y / 16f);
                if (!player.mount.Active) player.velocity.Y *= 0.5f;

                // 2. 初始化/更新计时器
                if (player.itemTime == 0)
                {
                    player.itemTime = ChannelTime;
                    player.itemAnimation = ChannelTime;
                }

                // 进度 (0.0 -> 1.0)
                float progress = 1f - (float)player.itemAnimation / ChannelTime;

                // 3. 屏幕震动：随着虚空降临，空间不稳定性增加
                if (Main.myPlayer == player.whoAmI && progress > 0.3f)
                {
                    float shake = (progress - 0.3f) * 6f; // 震动稍微加强
                    Main.screenPosition += Main.rand.NextVector2Circular(shake, shake);
                }

                // 4. 环境光照：深邃的虚空紫 -> 猩红的献祭红
                // 颜色更暗淡，更具压迫感
                Color startColor = new Color(60, 0, 90); // 深紫
                Color endColor = new Color(180, 0, 0);   // 暗红
                Color lightColor = Color.Lerp(startColor, endColor, progress * progress);
                Lighting.AddLight(player.Center, lightColor.ToVector3() * 1.2f * progress);

                // ================== ★★★ 深色系特效逻辑 ★★★ ==================

                Vector2 center = player.Center;
                float time = Main.GameUpdateCount * 0.05f;

                // --- 层级 A: 虚空法阵 (暗影焰与花岗岩) ---
                if (progress > 0.1f)
                {
                    float radius = 80f * progress;

                    // 外圈：暗影焰 (深紫色火焰)
                    if (Main.GameUpdateCount % 2 == 0) // 减少一点生成频率防止太乱
                    {
                        Vector2 posOut = center + Vector2.UnitX.RotatedBy(time * 2.5f) * radius;
                        Dust d1 = Dust.NewDustPerfect(posOut, DustID.Shadowflame, Vector2.Zero);
                        d1.noGravity = true;
                        d1.scale = 1.6f;
                    }

                    // 内圈：花岗岩 (深蓝/黑色粒子) - 逆时针
                    Vector2 posIn = center + Vector2.UnitX.RotatedBy(-time * 4f) * (radius * 0.6f);
                    Dust d2 = Dust.NewDustPerfect(posIn, DustID.Granite, Vector2.Zero);
                    d2.noGravity = true;
                    d2.scale = 1.3f;
                }

                // --- 层级 B: 灵魂剥离 (黑洞吸入) ---
                // 粒子从周围被强行吸入体内
                int particleCount = (int)(2 + progress * 6);
                for (int i = 0; i < particleCount; i++)
                {
                    float angle = MathHelper.ToRadians(Main.rand.Next(360));
                    float dist = 90f + Main.rand.NextFloat(60f);
                    Vector2 spawnPos = center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * dist;

                    // 前期：腐化/恶魔粒子 (暗紫)
                    // 后期：混入大量血液粒子 (暗红)
                    int dustType = DustID.Demonite;
                    if (progress > 0.6f && Main.rand.NextBool()) dustType = DustID.Blood;

                    Dust d = Dust.NewDustPerfect(spawnPos, dustType, Vector2.Zero);
                    // 计算吸入速度：进度越快吸得越狠
                    d.velocity = (center - spawnPos) * (0.06f + progress * 0.12f);
                    d.noGravity = true;
                    d.scale = 1.2f + progress; // 粒子越接近完成越大
                }

                // --- 层级 C: 溢出的黑暗 (脚底烟雾) ---
                if (Main.rand.NextBool(3))
                {
                    Vector2 mistPos = player.Bottom + new Vector2(Main.rand.NextFloat(-25, 25), 0);
                    // 使用烟雾粒子并染黑
                    Dust mist = Dust.NewDustPerfect(mistPos, DustID.Smoke, new Vector2(0, -1f - progress * 3f));
                    mist.color = Color.Black * 0.8f; // 黑色烟雾
                    mist.alpha = 100;
                    mist.scale = 2f;
                    mist.noGravity = true;
                }

                // 5. 仪式完成
                if (player.itemAnimation <= 2)
                {
                    DoTeleport(player);
                    player.channel = false;
                    player.itemAnimation = 0;
                    player.itemTime = 0;
                }
            }
            else
            {
                player.itemAnimation = 0;
                player.itemTime = 0;
            }
        }

        private void DoTeleport(Player player)
        {
            if (player.whoAmI != Main.myPlayer) return;

            // === 献祭代价 ===
            player.statMana = 0;
            player.manaRegenDelay = 120;
            int sacrificeAmount = player.statLife - 1;
            if (sacrificeAmount > 0)
            {
                player.statLife = 1;
                // 用深红色显示扣血
                CombatText.NewText(player.getRect(), new Color(139, 0, 0), "灵性献祭...", true);
                SoundEngine.PlaySound(SoundID.NPCDeath10, player.Center);
            }

            // === ★★★ 终极爆发：深渊降临 ★★★ ===

            SoundEngine.PlaySound(SoundID.Item119, player.Center);
            SoundEngine.PlaySound(SoundID.Item14, player.Center);

            Vector2 center = player.Center;

            // 1. 黑暗光柱 (Shadowflame)
            // 不再是明亮的钻石，而是深紫色的暗影焰柱
            for (int k = -60; k < 60; k++)
            {
                Vector2 pos = center + new Vector2(Main.rand.NextFloat(-15, 15), k * 8);
                Dust d = Dust.NewDustPerfect(pos, DustID.Shadowflame, new Vector2(0, -8), 0, default, 2.5f);
                d.noGravity = true;
                d.velocity.X *= 0.5f; // 限制水平扩散，保持柱状
            }

            // 2. 虚空冲击波 (Granite + Blood)
            for (int i = 0; i < 360; i += 4)
            {
                float angle = MathHelper.ToRadians(i);
                Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));

                // 深蓝黑色冲击波 (速度快)
                Dust d1 = Dust.NewDustPerfect(center, DustID.Granite, dir * 18f, 0, default, 2.5f);
                d1.noGravity = true;

                // 暗红血色冲击波 (速度稍慢，滞留感)
                Dust d2 = Dust.NewDustPerfect(center, DustID.Blood, dir * 10f, 0, default, 2.2f);
                d2.noGravity = true;
            }

            // 3. 执行传送
            if (SubworldSystem.IsActive<SpiritWorld>())
            {
                SubworldSystem.Exit();
            }
            else
            {
                SubworldSystem.Enter<SpiritWorld>();
            }
        }

        public override void AddRecipes()
        {
            // 配方保持不变
            CreateRecipe()
                .AddIngredient(ItemID.DirtBlock, 20)
                .AddIngredient(ItemID.SandBlock, 20)
                .AddIngredient(ItemID.SnowBlock, 20)
                .AddIngredient(ItemID.MudBlock, 20)
                .AddIngredient(ItemID.AshBlock, 20)
                .AddIngredient(ItemID.EbonstoneBlock, 20)
                .AddIngredient(ItemID.PearlstoneBlock, 20)
                .AddTile(TileID.DemonAltar)
                .Register();

            CreateRecipe()
                .AddIngredient(ItemID.DirtBlock, 20)
                .AddIngredient(ItemID.SandBlock, 20)
                .AddIngredient(ItemID.SnowBlock, 20)
                .AddIngredient(ItemID.MudBlock, 20)
                .AddIngredient(ItemID.AshBlock, 20)
                .AddIngredient(ItemID.CrimstoneBlock, 20)
                .AddIngredient(ItemID.PearlstoneBlock, 20)
                .AddTile(TileID.DemonAltar)
                .Register();
        }
    }
}