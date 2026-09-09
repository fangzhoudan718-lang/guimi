using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Microsoft.Xna.Framework;
using zhashi.Content.DamageClasses;

namespace zhashi.Content.Items.RitualItems
{
    public class SacrificialCasket : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 1;

            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;

            Item.rare = ItemRarityID.Pink;
            Item.value = Item.buyPrice(0, 10, 0, 0);
        }

        // 允许手持右键 (用于灵性换血)
        public override bool AltFunctionUse(Player player) => true;

        // 【修改点 1】禁止背包内右键 (取消献祭物品功能)
        public override bool CanRightClick() => false;

        public override bool ConsumeItem(Player player) => false;

        // 【修改点 2】删除了 RightClick 方法 (因为不再需要献祭逻辑)

        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // === 1. 手持右键使用 (灵性 -> 生命) ===
            if (player.altFunctionUse == 2)
            {
                int spiritCost = 50;
                int healAmount = 20;

                if (modPlayer.ConsumePixels(spiritCost))
                {
                    player.statLife += healAmount;
                    if (player.statLife > player.statLifeMax2)
                    {
                        player.statLife = player.statLifeMax2;
                    }

                    // 手动弹出深紫色回血数字
                    CombatText.NewText(player.getRect(), new Color(140, 0, 255), $"+{healAmount}");

                    SoundEngine.PlaySound(SoundID.Item8, player.Center);

                    // 深紫色重构特效
                    for (int i = 0; i < 30; i++)
                    {
                        Vector2 offset = Main.rand.NextVector2Circular(35, 35);
                        Vector2 target = player.Center;
                        Vector2 spawnPos = target + offset;
                        Vector2 dir = Vector2.Normalize(target - spawnPos);

                        Dust d = Dust.NewDustPerfect(spawnPos, DustID.Shadowflame, dir * 4f);
                        d.noGravity = true;
                        d.scale = 1.5f;

                        if (i % 3 == 0)
                        {
                            Dust d2 = Dust.NewDustPerfect(player.Center, DustID.Demonite, Main.rand.NextVector2Circular(2, 2));
                            d2.noGravity = true;
                            d2.scale = 1.2f;
                        }
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            // === 2. 手持左键使用 (生命 -> 灵性) ===
            else
            {
                if (player.statLife > 20)
                {
                    player.statLife -= 20;
                    if (player.statLife <= 0) player.statLife = 1;

                    CombatText.NewText(player.getRect(), Color.Red, "-20 HP");
                    SoundEngine.PlaySound(SoundID.NPCDeath1, player.Center);

                    // LotMPlayer 负责显示紫色 +50
                    modPlayer.AddPixels(50);

                    // 特效：血转灵
                    for (int i = 0; i < 8; i++)
                    {
                        Dust.NewDust(player.position, player.width, player.height, DustID.Blood, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-4, -1), 0, default, 1.5f);
                    }
                    for (int i = 0; i < 30; i++)
                    {
                        Vector2 spawnPos = player.Bottom + new Vector2(Main.rand.NextFloat(-20, 20), -5);
                        int dustType = Main.rand.NextBool() ? DustID.LifeDrain : DustID.Shadowflame;

                        Dust d = Dust.NewDustPerfect(spawnPos, dustType, new Vector2(0, -3f));
                        d.velocity.X += Main.rand.NextFloat(-0.8f, 0.8f);
                        d.velocity += player.velocity * 0.5f;
                        d.noGravity = true;
                        d.scale = Main.rand.NextFloat(1.2f, 2.0f);
                        d.alpha = 50;
                        d.fadeIn = 1.1f;
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips)
        {
            // 【修改点 3】删除了背包献祭的提示
            tooltips.Add(new TooltipLine(Mod, "RitualTip1", "1. [手持左键]: 消耗 20 生命值换取 50 灵性"));
            tooltips.Add(new TooltipLine(Mod, "RitualTip2", "2. [手持右键]: 消耗 50 灵性换取 20 生命值"));
        }
    }
}