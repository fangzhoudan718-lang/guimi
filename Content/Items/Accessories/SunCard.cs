using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework; // 用于颜色
using zhashi.Content; // 引用 LotMPlayer
using zhashi.Content.Items;

namespace zhashi.Content.Items.Accessories
{
    public class SunCard : BlasphemyCardBase
    {
        public override void SafeSetDefaults()
        {
            Item.width = 28;
            Item.height = 34;
            Item.accessory = true;
            Item.rare = ItemRarityID.Red; // 红色稀有度
            Item.value = Item.sellPrice(platinum: 5);
            Item.maxStack = 1;
        }

        public override void SafeUpdateAccessory(Player player, LotMPlayer mp, bool hideVisual)
        {
            // 1. 核心标记
            mp.isSunCardEquipped = true;

            // 2. 基础效果：光明之子
            // 提供极强的光照 (亮度 1.5, 颜色 纯白/金)
            Lighting.AddLight(player.Center, 1.5f, 1.4f, 1.0f);

            // 免疫黑暗类 Debuff
            player.buffImmune[BuffID.Darkness] = true;
            player.buffImmune[BuffID.Blackout] = true;
            player.buffImmune[BuffID.Obstructed] = true;

            // 3. 途径专属加成 (需太阳途径序列5及以上)
            // 假设变量名为 baseSunSequence，请确保你在 LotMPlayer 中定义了它
            if (mp.baseSunSequence <= 9)
            {
                // === [无暗之域] ===
                // 类似于红祭司的伤害光环，但对亡灵有加成
                if (player.whoAmI == Main.myPlayer)
                {
                    // 范围稍大一点，代表阳光普照
                    float range = 450f;
                    int baseDmg = (int)(60 * player.GetDamage(DamageClass.Generic).Multiplicative);

                    // 每 0.5 秒造成一次伤害
                    if (Main.GameUpdateCount % 30 == 0)
                    {
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            NPC target = Main.npc[i];
                            if (target.active && !target.friendly && !target.dontTakeDamage && target.lifeMax > 5)
                            {
                                float distance = player.Distance(target.Center);
                                if (distance < range)
                                {
                                    int finalDmg = baseDmg;

                                    // 核心特色：对亡灵/邪恶生物造成 200% 伤害
                                    // 检查是否是骷髅、僵尸、或者具有亡灵属性的怪
                                    if (IsUndeadOrEvil(target))
                                    {
                                        finalDmg *= 2;
                                        // 额外点燃效果：神圣之火 (OnFire3)
                                        target.AddBuff(BuffID.OnFire3, 120);
                                    }

                                    // 施加普通燃烧
                                    target.AddBuff(BuffID.Daybreak, 60);

                                    // 造成伤害
                                    player.ApplyDamageToNPC(target, finalDmg, 0f, 0, false);

                                    // 视觉特效：生成金色粒子
                                    Dust.NewDust(target.position, target.width, target.height, DustID.GoldFlame, 0, 0, 0, default, 1.5f);
                                }
                            }
                        }
                    }
                }

                // === [光之祭司] ===
                // 极高的生命回复 (太阳途径特色)
                player.lifeRegen += 10;

                // === [神圣契约] ===
                // 增加防御力和免伤
                player.statDefense += 20;
                player.endurance += 0.15f;

                // === [纯净体质] ===
                // 免疫常见持续伤害
                player.buffImmune[BuffID.Poisoned] = true;
                player.buffImmune[BuffID.Venom] = true;
                player.buffImmune[BuffID.Bleeding] = true;
                player.buffImmune[BuffID.OnFire] = true; // 既然玩火，自然不怕火
            }
        }

        // 辅助函数：判断是否为亡灵/邪恶生物
        private bool IsUndeadOrEvil(NPC npc)
        {
            // 泰拉瑞亚原版并没有统一的 IsUndead 属性暴露给 ModItem，
            // 但我们可以通过 AI 样式或特定 ID 列表来判断，或者检查 soundHit
            // 这里简单判断一下常见的 (骷髅/僵尸类通常 HitSound 是 Bone 或 Rotten)

            if (npc.aiStyle == 3 || npc.aiStyle == -3) return true; // 僵尸类 AI
            if (NPCID.Sets.Skeletons[npc.type]) return true; // 骷髅集合

            // 也可以根据 HitSound 判断
            if (npc.HitSound == SoundID.NPCHit2) return true; // 骨头声

            return false;
        }
    }
}