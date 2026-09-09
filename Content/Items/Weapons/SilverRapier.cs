using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content;

namespace zhashi.Content.Items.Weapons
{
    public class SilverRapier : ModItem
    {
        // 寿命计时器
        public int lifeTime = 0;

        public override void SetDefaults()
        {
            Item.damage = 180;
            Item.DamageType = DamageClass.Melee;
            Item.width = 50;
            Item.height = 50;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 8;
            Item.useAnimation = 8;
            Item.useTurn = true;
            Item.knockBack = 4;
            Item.value = 0;
            Item.rare = ItemRarityID.Cyan;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.None;

            // 【删除】这行代码会导致报错，必须删除！
            // Item.altFunctionUse = true; 
        }

        // 物品栏内自动销毁逻辑 (10秒后消失)
        public override void UpdateInventory(Player player)
        {
            lifeTime++;
            if (lifeTime >= 600) // 600帧 = 10秒
            {
                Item.TurnToAir(); // 销毁物品
                Main.NewText("银白细剑消散了...", 192, 192, 192);
                SoundEngine.PlaySound(SoundID.Shatter, player.position);
            }
        }

        public override void OnCreated(ItemCreationContext context)
        {
            if (Main.gameMenu || Main.LocalPlayer == null || !Main.LocalPlayer.active)
                return;

            var player = Main.LocalPlayer;

            if (player.TryGetModPlayer<LotMPlayer>(out var modPlayer))
            {
                int cost = (int)(modPlayer.spiritualityCurrent / 2);
                if (cost < 50) cost = 50;

                if (modPlayer.spiritualityCurrent >= cost)
                {
                    modPlayer.spiritualityCurrent -= cost;
                    Main.NewText($"具现银白细剑消耗了 {cost} 点灵性！", 0, 255, 255);
                }
                else
                {
                    Main.NewText("灵性不足以维持具现！", 255, 50, 50);
                    Item.TurnToAir();
                }
            }
        }

        public override bool CanUseItem(Player player)
        {
            if (player.TryGetModPlayer<LotMPlayer>(out var modPlayer))
            {
                // 1. 严格限制只有巨人途径序列3及以上可以使用
                if (modPlayer.currentSequence > 3)
                {
                    return false;
                }

                // 2. 计算技能消耗
                int cost = 0;
                if (player.altFunctionUse == 2) cost = 1000; // 右键消耗
                else cost = 100; // 左键消耗

                // 3. 检查灵性
                if (modPlayer.spiritualityCurrent < cost)
                {
                    SoundEngine.PlaySound(SoundID.Shatter, player.position);
                    Main.NewText("灵性枯竭... 银白细剑崩解了！", 255, 50, 50);
                    for (int i = 0; i < 30; i++) Dust.NewDust(player.position, player.width, player.height, DustID.Silver, 0, 0, 0, default, 2f);
                    Item.TurnToAir();
                    return false;
                }

                modPlayer.TryConsumeSpirituality(cost);

                // --- 右键功能：瞬移 ---
                if (player.altFunctionUse == 2)
                {
                    Vector2 targetPos = Main.MouseWorld;
                    if (player.Distance(targetPos) < 1000f && Collision.CanHit(player.position, player.width, player.height, targetPos, player.width, player.height))
                    {
                        for (int i = 0; i < 20; i++) Dust.NewDust(player.position, player.width, player.height, DustID.Silver, 0, 0, 0, default, 2f);
                        player.Teleport(targetPos, 1);
                        for (int i = 0; i < 30; i++) { Vector2 speed = Main.rand.NextVector2Circular(5f, 5f); Dust.NewDustPerfect(player.Center, DustID.Silver, speed, 0, default, 2f); }
                        SoundEngine.PlaySound(SoundID.Item71, player.position);

                        foreach (NPC npc in Main.npc)
                        {
                            if (npc.active && !npc.friendly && npc.Distance(player.Center) < 200f)
                            {
                                player.ApplyDamageToNPC(npc, Item.damage * 2, 5f, player.direction, false);
                                npc.AddBuff(BuffID.Frostburn, 300);
                            }
                        }
                    }
                    else return false;
                }
                return true;
            }
            return false;
        }

        // 这个方法才是控制能否右键的关键，不需要在 SetDefaults 里写属性
        public override bool AltFunctionUse(Player player) { return true; }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.DirtBlock, 1)
                // 【配方可见性限制】只有序列3及以上的巨人途径玩家可见
                .AddCondition(new Condition("需序列3 银骑士 或更强", () => Main.LocalPlayer.GetModPlayer<LotMPlayer>().currentSequence <= 3))
                .Register();
        }
    }
}