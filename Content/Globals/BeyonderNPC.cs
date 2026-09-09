using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.DataStructures;
using Terraria.GameContent;
using System.IO;
using zhashi.Content.Configs;
using zhashi.Content.Items;
using zhashi.Content.Items.Accessories;

namespace zhashi.Content.Globals
{
    /// <summary>
    /// 生物超凡化系统:
    /// 每只NPC在生成时,有几率被标记为某途径某序列的超凡者.
    /// 序列越低 = 几率越小 = 越强大.
    /// </summary>
    public class BeyonderNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        // === 超凡数据 ===
        public bool isBeyonder = false;
        public int beyonderSequence = 10; // 10=非超凡, 0-9=对应序列
        public int beyonderPathway = -1;  // 0-7对应 愚者/猎人/月亮/巨人/错误/太阳/魔女/命运
        public int skillCooldown = 0;     // 技能CD计数

        // 途径名
        public static readonly string[] PathwayNames = {
            "愚者", "猎人", "月亮", "巨人/战士", "错误", "太阳", "魔女", "命运"
        };
        // 途径主色 (用于血条 + 名字染色)
        public static readonly Color[] PathwayColors = {
            new Color(255, 220, 100), // 愚者-金
            new Color(120, 200, 80),  // 猎人-绿
            new Color(200, 200, 230), // 月亮-月白
            new Color(220, 100, 80),  // 巨人-赤
            new Color(80, 200, 200),  // 错误-青
            new Color(255, 180, 60),  // 太阳-橘
            new Color(200, 120, 220), // 魔女-紫
            new Color(232, 200, 120)  // 命运-黄铜
        };

        // === 序列生成几率 (千分之单位,方便随机) ===
        // 序列9=100/1000=10% 序列8=50/1000=5% 序列7=10/1000=1% ...
        private static readonly int[] SeqWeights = {
            // seq0,    1,    2,    3,    4,     5,    6,   7,   8,    9
                  1,    5,   10,   50,  100,   500, 1000, 2000, 5000, 10000
        };
        // 这些权重相对总池10万, 9序列是10000=10%

        public override void SetDefaults(NPC npc)
        {
            // 不在SetDefaults中决定超凡 - 因为这里会被一些复制操作调用,会重复roll
            // 在 OnSpawn 中处理
        }

        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (npc.friendly || npc.lifeMax <= 5) return;
            // 排除小动物
            if (NPCID.Sets.CountsAsCritter[npc.type] && npc.type != NPCID.Bunny) return;
            // 蠕虫体节排除(避免每节都roll)
            if (npc.realLife >= 0 && npc.realLife != npc.whoAmI) return;

            // === 配置检查 ===
            var cfg = ModContent.GetInstance<LotMConfig>();
            if (cfg == null || !cfg.EnableBeyonderCreatures) return;

            // 总池 10万
            int roll = Main.rand.Next(100000);
            int cumulative = 0;
            int chosenSeq = -1;
            for (int seq = 0; seq < 10; seq++)
            {
                cumulative += SeqWeights[seq];
                if (roll < cumulative)
                {
                    chosenSeq = seq;
                    break;
                }
            }
            if (chosenSeq < 0) return;

            // === 标记为超凡 ===
            isBeyonder = true;
            beyonderSequence = chosenSeq;
            beyonderPathway = Main.rand.Next(8);

            bool isBoss = npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type];

            // === 强化数值: 倍率按序列指数增长 (诡秘之主原著: 序列0=从神级别) ===
            // 普通NPC vs Boss 用同一套陡峭曲线; Boss原本就强,强化后会变成"位面级威胁"
            float[] hpMult  = { 2000f, 300f, 80f, 30f, 15f, 8f, 5f, 3f, 2f, 1.5f };
            float[] dmgMult = {  500f, 100f, 40f, 15f, 8f,  4f, 3f, 2f, 1.5f, 1.2f };

            float hm = hpMult[chosenSeq];
            float dm = dmgMult[chosenSeq];

            npc.lifeMax = (int)Math.Min(int.MaxValue, npc.lifeMax * hm);
            npc.life = npc.lifeMax;
            npc.damage = (int)Math.Min(int.MaxValue, npc.damage * dm);
            // Boss不动防御(原版已经平衡过),普通NPC才加防御
            if (!isBoss)
                npc.defense = (int)Math.Min(int.MaxValue, npc.defense * (1 + chosenSeq * 0.3f + 2f));

            // 价值翻倍(掉落金币更多)
            npc.value *= (1 + (9 - chosenSeq) * 2);

            // 同步到客户端
            npc.netUpdate = true;

            // === 弹出超凡化提示 (Boss任意序列都提示; 普通NPC仅序列5及以下避免刷屏) ===
            if ((chosenSeq <= 5 || isBoss) && Main.netMode != NetmodeID.Server)
            {
                Color col = PathwayColors[beyonderPathway];
                string msg = chosenSeq <= 2
                    ? $"⚠⚠⚠ 一个 [序列{chosenSeq}·{PathwayNames[beyonderPathway]}] 的 {npc.GivenOrTypeName} 出现了!"
                    : $"⚠ 一个 [序列{chosenSeq}·{PathwayNames[beyonderPathway]}] 的 {npc.GivenOrTypeName} 出现了。";
                Main.NewText(msg, col);
            }
        }

        // === 名字显示前缀 ===
        public override void ModifyTypeName(NPC npc, ref string typeName)
        {
            if (!isBeyonder) return;
            if (beyonderPathway < 0 || beyonderPathway >= PathwayNames.Length) return;
            typeName = $"[序列{beyonderSequence}·{PathwayNames[beyonderPathway]}] {typeName}";
        }

        // === NPC上方绘制 自定义血条 + 标签 ===
        public override void PostDraw(NPC npc, Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!isBeyonder) return;
            if (beyonderPathway < 0 || beyonderPathway >= PathwayColors.Length) return;
            if (Main.dedServ) return;
            // Boss不绘制自定义血条/标签 - 用原版Boss血条
            if (npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type]) return;

            // 计算血条位置 (NPC 上方)
            Vector2 barCenter = new Vector2(
                npc.Center.X - Main.screenPosition.X,
                npc.position.Y - Main.screenPosition.Y - 24f
            );

            // 用Pixel纹理画矩形 (vanilla内置)
            Microsoft.Xna.Framework.Graphics.Texture2D pixel = TextureAssets.MagicPixel.Value;
            float barWidth = 60f;
            float barHeight = 6f;

            // 背景框 (深色)
            spriteBatch.Draw(pixel,
                new Rectangle((int)(barCenter.X - barWidth / 2 - 1), (int)(barCenter.Y - 1), (int)barWidth + 2, (int)barHeight + 2),
                Color.Black * 0.8f);
            // 内部底 (灰色)
            spriteBatch.Draw(pixel,
                new Rectangle((int)(barCenter.X - barWidth / 2), (int)barCenter.Y, (int)barWidth, (int)barHeight),
                Color.DarkSlateGray);
            // 血条本身 (途径色)
            float hpPercent = (float)npc.life / npc.lifeMax;
            if (hpPercent < 0f) hpPercent = 0f;
            if (hpPercent > 1f) hpPercent = 1f;
            Color hpColor = PathwayColors[beyonderPathway];
            spriteBatch.Draw(pixel,
                new Rectangle((int)(barCenter.X - barWidth / 2), (int)barCenter.Y, (int)(barWidth * hpPercent), (int)barHeight),
                hpColor);

            // 序列标签 (序列数字)
            string label = $"S{beyonderSequence} {PathwayNames[beyonderPathway]}";
            Vector2 labelSize = FontAssets.MouseText.Value.MeasureString(label);
            Vector2 labelPos = new Vector2(barCenter.X - labelSize.X / 2, barCenter.Y - labelSize.Y - 2f);

            // 文字阴影 + 主体 (DynamicSpriteFont要用ChatManager)
            Terraria.UI.Chat.ChatManager.DrawColorCodedStringWithShadow(
                spriteBatch, FontAssets.MouseText.Value, label,
                labelPos, hpColor, 0f, Vector2.Zero, Vector2.One);
        }

        // === 联机同步 ===
        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter writer)
        {
            writer.Write(isBeyonder);
            if (isBeyonder)
            {
                writer.Write((byte)beyonderSequence);
                writer.Write((byte)(beyonderPathway + 1)); // +1 避免负数
            }
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader reader)
        {
            isBeyonder = reader.ReadBoolean();
            if (isBeyonder)
            {
                beyonderSequence = reader.ReadByte();
                beyonderPathway = reader.ReadByte() - 1;
            }
        }

        // === 持续属性增强 (每帧调用) ===
        public override void PostAI(NPC npc)
        {
            if (!isBeyonder) return;

            // Boss走简化路线: 只享受血量/伤害强化(已在OnSpawn做了),不释放途径技能/不画粒子
            bool isBoss = npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type];

            // === 视觉效果: 持续发光粒子(客户端,Boss跳过) ===
            if (!isBoss && !Main.dedServ && beyonderPathway >= 0 && beyonderPathway < PathwayColors.Length)
            {
                int rate = beyonderSequence <= 3 ? 1 : (beyonderSequence <= 6 ? 4 : 10);
                if (Main.GameUpdateCount % (uint)rate == 0)
                {
                    Color col = PathwayColors[beyonderPathway];
                    int dustId = beyonderSequence <= 3 ? DustID.GoldCoin : DustID.PurpleCrystalShard;
                    Dust d = Dust.NewDustPerfect(
                        npc.Center + Main.rand.NextVector2Circular(npc.width / 2f, npc.height / 2f),
                        dustId, Vector2.Zero, 0, col, 1.2f);
                    d.noGravity = true;
                    d.fadeIn = 1.3f;
                }
                // 持续光照
                Lighting.AddLight(npc.Center,
                    PathwayColors[beyonderPathway].R / 255f * 0.6f,
                    PathwayColors[beyonderPathway].G / 255f * 0.6f,
                    PathwayColors[beyonderPathway].B / 255f * 0.6f);
            }

            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            // Boss不释放途径技能 (避免和Boss自有AI冲突闪退)
            if (isBoss) return;

            // === 技能自动释放 ===
            if (skillCooldown > 0) skillCooldown--;
            else
            {
                // 离最近玩家不远才考虑释放
                Player target = null;
                float minDist = 1500f;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (!p.active || p.dead) continue;
                    float d = p.Distance(npc.Center);
                    if (d < minDist) { minDist = d; target = p; }
                }
                if (target != null && minDist < 800f)
                {
                    if (CastSkill(npc, target))
                    {
                        // 技能CD: 序列越低,CD越短
                        int[] cdTable = { 60, 90, 120, 180, 240, 300, 360, 420, 480, 600 };
                        skillCooldown = cdTable[beyonderSequence];
                        npc.netUpdate = true;
                    }
                }
            }
        }

        /// <summary>按途径释放对应技能, 返回是否成功释放</summary>
        private bool CastSkill(NPC npc, Player target)
        {
            // 防御性检查 - 防止target死亡时仍触发技能造成异常
            if (target == null || !target.active || target.dead) return false;
            if (npc.life <= 0 || !npc.active) return false;

            Vector2 dir = (target.Center - npc.Center).SafeNormalize(Vector2.UnitX);
            int baseDamage = npc.damage / 2;
            Vector2 origin = npc.Center;

            switch (beyonderPathway)
            {
                case 0: // 愚者: 召唤暗影火苗+混乱
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 v = dir.RotatedBy((i - 1) * 0.25f) * 9f;
                        Projectile.NewProjectile(npc.GetSource_FromAI(), origin, v,
                            ProjectileID.ShadowFlame, baseDamage, 2f, Main.myPlayer);
                    }
                    if (Main.rand.NextBool(3))
                        target.AddBuff(BuffID.Confused, 120);
                    return true;

                case 1: // 猎人: 毒瓶
                    Projectile.NewProjectile(npc.GetSource_FromAI(), origin, dir * 10f,
                        ProjectileID.PoisonSeedPlantera, baseDamage, 3f, Main.myPlayer);
                    target.AddBuff(BuffID.Poisoned, 360);
                    return true;

                case 2: // 月亮: 血族咒+变形
                    Projectile.NewProjectile(npc.GetSource_FromAI(), origin, dir * 11f,
                        ProjectileID.IchorArrow, baseDamage, 4f, Main.myPlayer);
                    target.AddBuff(BuffID.Ichor, 360);
                    target.AddBuff(BuffID.Bleeding, 240);
                    // 自愈
                    npc.life = Math.Min(npc.lifeMax, npc.life + npc.lifeMax / 50);
                    return true;

                case 3: // 巨人/战士: 强力近战 - 强化敌人下次攻击伤害
                    target.AddBuff(BuffID.BrokenArmor, 360);
                    target.AddBuff(BuffID.WeaponImbueIchor, 180); // 伊克尔粘附削玩家防御
                    return true;

                case 4: // 错误: 偷东西 + 混乱
                    if (Main.rand.NextBool(3) && target.statLife > 50)
                    {
                        target.statLife -= 30;
                        npc.life = Math.Min(npc.lifeMax, npc.life + 30);
                        if (Main.myPlayer == target.whoAmI)
                            CombatText.NewText(target.getRect(), Color.Cyan, "-30 (灵性窃取)");
                    }
                    target.AddBuff(BuffID.Confused, 180);
                    return true;

                case 5: // 太阳: 圣光射线 + 自愈
                    for (int i = -1; i <= 1; i++)
                    {
                        Vector2 v = dir.RotatedBy(i * 0.15f) * 14f;
                        Projectile.NewProjectile(npc.GetSource_FromAI(), origin, v,
                            ProjectileID.HallowStar, baseDamage, 5f, Main.myPlayer);
                    }
                    npc.life = Math.Min(npc.lifeMax, npc.life + npc.lifeMax / 30);
                    return true;

                case 6: // 魔女: 三向魔弹
                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 v = dir.RotatedBy((i - 2) * 0.2f) * 10f;
                        Projectile.NewProjectile(npc.GetSource_FromAI(), origin, v,
                            ProjectileID.LostSoulFriendly, baseDamage, 2f, Main.myPlayer);
                    }
                    target.AddBuff(BuffID.Darkness, 360);
                    return true;

                case 7: // 命运: 厄运诅咒 + 概率秒杀
                    target.AddBuff(BuffID.WitheredArmor, 600);
                    target.AddBuff(BuffID.WitheredWeapon, 600);
                    // 序列<=3 概率秒杀 - 用 NetworkText 版避免过时API
                    if (beyonderSequence <= 3 && Main.rand.NextBool(20) && !target.dead && target.statLife > 0)
                    {
                        target.Hurt(
                            PlayerDeathReason.ByCustomReason(
                                Terraria.Localization.NetworkText.FromLiteral($"被序列{beyonderSequence}的命运抹除了。")),
                            target.statLifeMax2 * 2, 0);
                    }
                    return true;
            }
            return false;
        }

        public override void OnKill(NPC npc)
        {
            if (!isBeyonder) return;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            var src = npc.GetSource_Death();
            int seq = beyonderSequence;
            int path = beyonderPathway;

            // ========================================
            // 1. 原版掉落翻倍 - 用 NPCLoader 的方式(更安全)
            //    序列9=2倍, 0=11倍, 通过临时重复触发掉落
            //    简化: 改为直接在尸体处增加额外稀有材料(下面ID白名单)
            // ========================================
            // 序列<=6 额外掉素材
            if (seq <= 6)
            {
                // 战斗药剂/治疗药水等通用资源
                Item.NewItem(src, npc.position, npc.Size, ItemID.HealingPotion, (7 - seq));
                Item.NewItem(src, npc.position, npc.Size, ItemID.ManaPotion, (7 - seq));
            }
            if (seq <= 4)
            {
                // 进阶素材
                Item.NewItem(src, npc.position, npc.Size, ItemID.GreaterHealingPotion, (5 - seq) * 2);
            }
            if (seq <= 2)
            {
                // 顶级药水
                Item.NewItem(src, npc.position, npc.Size, ItemID.SuperHealingPotion, (3 - seq) * 3);
            }

            // ========================================
            // 2. 大幅金币掉落 (核心激励)
            // ========================================
            //   - 序列9: 5金币
            //   - 序列7: 25金币
            //   - 序列5: 80金币
            //   - 序列3: 4铂金
            //   - 序列1: 50铂金
            //   - 序列0: 500铂金
            // 用 ItemID.PlatinumCoin/GoldCoin 直接撒
            int[] platinumByseq = { 500, 50, 20, 10, 4, 2, 1, 0, 0, 0 };
            int[] goldByseq     = {  20, 20, 20, 20, 20, 20, 80, 25, 10, 5 };
            int plat = platinumByseq[seq];
            int gold = goldByseq[seq];
            if (plat > 0)
                Item.NewItem(src, npc.position, npc.Size, ItemID.PlatinumCoin, plat);
            if (gold > 0)
                Item.NewItem(src, npc.position, npc.Size, ItemID.GoldCoin, gold);

            // ========================================
            // 3. 亵渎之牌掉落 (仅序列 2/1/0)
            //    序列2=25%, 1=50%, 0=必掉
            //    根据NPC途径掉对应的牌
            // ========================================
            if (seq <= 2)
            {
                int cardDropChance = seq == 0 ? 1 : (seq == 1 ? 2 : 4); // 1/1, 1/2, 1/4
                if (Main.rand.NextBool(cardDropChance))
                {
                    int cardId = GetCardForPathway(path);
                    if (cardId > 0)
                    {
                        Item.NewItem(src, npc.position, npc.Size, cardId, 1);
                    }
                }
            }

            // ========================================
            // 5. 顶级稀有物 (生命果/红心水晶等额外掉落)
            // ========================================
            if (seq <= 5)
                Item.NewItem(src, npc.position, npc.Size, ItemID.LifeCrystal, (6 - seq));
            if (seq <= 3)
                Item.NewItem(src, npc.position, npc.Size, ItemID.LifeFruit, (4 - seq) * 2);
            if (seq <= 1)
                Item.NewItem(src, npc.position, npc.Size, ItemID.ManaCrystal, (2 - seq) * 3);
        }

        /// <summary>
        /// 根据途径返回该途径对应的 BlasphemyCard ItemID.
        /// 每个途径多张候选牌, 随机选一张.
        /// </summary>
        private static int GetCardForPathway(int pathway)
        {
            try
            {
                // 0=愚者 1=猎人 2=月亮 3=巨人 4=错误 5=太阳 6=魔女 7=命运
                switch (pathway)
                {
                    case 0: // 愚者
                        return ModContent.ItemType<FoolCard>();
                    case 1: // 猎人
                        return ModContent.ItemType<HermitCard>();
                    case 2: // 月亮
                        return ModContent.ItemType<MoonCard>();
                    case 3: // 巨人/战士
                        return ModContent.ItemType<StrengthCard>();
                    case 4: // 错误
                        return ModContent.ItemType<HangedManCard>();
                    case 5: // 太阳
                        return ModContent.ItemType<SunCard>();
                    case 6: // 魔女
                        return ModContent.ItemType<DemonessCard>();
                    case 7: // 命运
                        return ModContent.ItemType<WheelOfFortuneCard>();
                }
            }
            catch { }
            return 0;
        }
    }
}
