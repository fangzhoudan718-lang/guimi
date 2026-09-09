using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Microsoft.Xna.Framework;
using zhashi.Content; // 引用玩家数据

namespace zhashi.Content.Items.Debug
{
    public class RitualInstaComplete : ModItem
    {
        public override string Texture => "Terraria/Images/Item_" + ItemID.Stopwatch;

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.rare = ItemRarityID.Red; // 红色代表开发者物品
            Item.consumable = false;
            Item.value = 0;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var modPlayer = player.GetModPlayer<LotMPlayer>();

                // ==========================================
                // 1. 所有途径的仪式进度拉满
                // ==========================================

                // [命运途径]
                // 序列4 厄运法师 - 戴着灰色四叶草击败机械骷髅王
                if (modPlayer.misfortuneRitualTimer < LotMPlayer.MISFORTUNE_RITUAL_TARGET)
                {
                    modPlayer.misfortuneRitualTimer = LotMPlayer.MISFORTUNE_RITUAL_TARGET;
                }
                if (!modPlayer.misfortuneMageRitualComplete)
                {
                    modPlayer.misfortuneMageRitualComplete = true;
                }
                // 序列3 怪人 - 三次绝境逆转
                if (modPlayer.anomalyRitualProgress < LotMPlayer.ANOMALY_RITUAL_TARGET)
                {
                    modPlayer.anomalyRitualProgress = LotMPlayer.ANOMALY_RITUAL_TARGET;
                }
                if (!modPlayer.anomalyRitualComplete)
                {
                    modPlayer.anomalyRitualComplete = true;
                }
                // 序列2 先知 - 五次不可定数触发
                if (modPlayer.prophetRitualProgress < LotMPlayer.PROPHET_RITUAL_TARGET)
                {
                    modPlayer.prophetRitualProgress = LotMPlayer.PROPHET_RITUAL_TARGET;
                }
                if (!modPlayer.prophetRitualComplete)
                {
                    modPlayer.prophetRitualComplete = true;
                }
                // 序列1 巨蛇 - 五次启示 + 击败月亮领主
                if (modPlayer.serpentRitualProgress < LotMPlayer.SERPENT_RITUAL_TARGET)
                {
                    modPlayer.serpentRitualProgress = LotMPlayer.SERPENT_RITUAL_TARGET;
                }
                modPlayer.serpentRitualBeatMoonLord = true;
                if (!modPlayer.serpentRitualComplete)
                {
                    modPlayer.serpentRitualComplete = true;
                }

                // [巨人/战士途径]
                if (modPlayer.guardianRitualProgress < LotMPlayer.GUARDIAN_RITUAL_TARGET)
                {
                    modPlayer.guardianRitualProgress = LotMPlayer.GUARDIAN_RITUAL_TARGET;
                }

                // [猎人途径]
                if (modPlayer.demonHunterRitualProgress < LotMPlayer.DEMON_HUNTER_RITUAL_TARGET)
                {
                    modPlayer.demonHunterRitualProgress = LotMPlayer.DEMON_HUNTER_RITUAL_TARGET;
                }
                if (modPlayer.ironBloodRitualProgress < LotMPlayer.IRON_BLOOD_RITUAL_TARGET)
                {
                    modPlayer.ironBloodRitualProgress = LotMPlayer.IRON_BLOOD_RITUAL_TARGET;
                }
                if (!modPlayer.weatherRitualComplete)
                {
                    modPlayer.weatherRitualComplete = true;
                }
                if (!modPlayer.conquerorRitualComplete)
                {
                    modPlayer.conquerorRitualComplete = true;
                }

                // [愚者途径]
                if (modPlayer.attendantRitualProgress < LotMPlayer.ATTENDANT_RITUAL_TARGET)
                {
                    modPlayer.attendantRitualProgress = LotMPlayer.ATTENDANT_RITUAL_TARGET;
                    modPlayer.attendantRitualComplete = true;
                }

                // [错误/盗贼途径]
                if (modPlayer.parasiteRitualProgress < LotMPlayer.PARASITE_RITUAL_TARGET)
                {
                    modPlayer.parasiteRitualProgress = LotMPlayer.PARASITE_RITUAL_TARGET;
                }
                if (modPlayer.mentorRitualProgress < LotMPlayer.MENTOR_RITUAL_TARGET)
                {
                    modPlayer.mentorRitualProgress = LotMPlayer.MENTOR_RITUAL_TARGET;
                }
                if (modPlayer.trojanRitualTimer < LotMPlayer.TROJAN_RITUAL_TARGET)
                {
                    modPlayer.trojanRitualTimer = LotMPlayer.TROJAN_RITUAL_TARGET;
                }
                if (modPlayer.wormRitualTimer < LotMPlayer.WORM_RITUAL_TARGET)
                {
                    modPlayer.wormRitualTimer = LotMPlayer.WORM_RITUAL_TARGET;
                }

                // [魔女/末日途径]
                if (modPlayer.despairRitualCount < 50)
                {
                    modPlayer.despairRitualCount = 50;
                }
                if (modPlayer.afflictionRitualTimer < 54000)
                {
                    modPlayer.afflictionRitualTimer = 54000;
                }
                if (modPlayer.catastropheRitualCount < LotMPlayer.CATASTROPHE_RITUAL_TARGET)
                {
                    modPlayer.catastropheRitualCount = LotMPlayer.CATASTROPHE_RITUAL_TARGET;
                }

                // [太阳途径]
                if (modPlayer.purificationProgress < LotMPlayer.PURIFICATION_RITUAL_TARGET)
                {
                    modPlayer.purificationProgress = LotMPlayer.PURIFICATION_RITUAL_TARGET;
                }
                if (modPlayer.judgmentProgress < LotMPlayer.JUDGMENT_RITUAL_TARGET)
                {
                    modPlayer.judgmentProgress = LotMPlayer.JUDGMENT_RITUAL_TARGET;
                }

                // ==========================================
                // 2. 冷却时间重置
                // ==========================================

                // [命运途径]
                modPlayer.psychicStormCooldown = 0;
                modPlayer.fateBlessingCooldown = 0;
                modPlayer.fateNullifyCooldown = 0;          // 不可定数CD
                modPlayer.fateDiceCooldown = 0;             // 命运骰子CD
                modPlayer.wheelOnHitCooldown = 0;           // OnHit节流
                modPlayer.wordsOfFortuneCooldown = 0;       // 福祸之福CD
                modPlayer.wordsOfMisfortuneCooldown = 0;    // 福祸之祸CD
                modPlayer.revelationCooldown = 0;           // 命运启示CD
                modPlayer.prophecyCooldown = 0;             // (deprecated)
                modPlayer.mercuryDodgeCooldown = 0;         // 水银之躯CD
                modPlayer.fateLoopCooldown = 0;             // 命运循环CD
                modPlayer.restartAutoCooldown = 0;          // 重启自动CD
                modPlayer.restartManualCooldown = 0;        // 重启主动CD
                modPlayer.mercuryPhaseCooldown = 0;         // 水银相位CD

                // [月亮途径]
                modPlayer.paperFigurineCooldown = 0;
                modPlayer.darknessGazeCooldown = 0;
                modPlayer.summonGateCooldown = 0;
                modPlayer.purifyCooldown = 0;
                modPlayer.elixirCooldown = 0;
                modPlayer.abyssShackleCooldown = 0;

                // [太阳途径]
                modPlayer.sunRadianceCooldown = 0;
                modPlayer.holyLightCooldown = 0;
                modPlayer.holyOathCooldown = 0;
                modPlayer.fireOceanCooldown = 0;

                // [通用/其他]
                modPlayer.twilightResurrectionCooldown = 0;
                modPlayer.wormificationCooldown = 0;
                modPlayer.miracleCooldown = 0;
                modPlayer.divinationCooldown = 0;
                modPlayer.damageTransferCooldown = 0;
                modPlayer.glacierCooldown = 0;

                // ==========================================
                // 3. 状态补满
                // ==========================================
                modPlayer.spiritualityCurrent = modPlayer.spiritualityMax;
                modPlayer.borrowUsesDaily = 0;

                // ==========================================
                // 4. 反馈
                // ==========================================
                Main.NewText("★ 开发者指令执行完毕 - 所有仪式已完成 ★", 0, 255, 255);
                Main.NewText("- 命运: 厄运法师 / 怪人 / 先知 仪式 ✓", 255, 215, 100);
                Main.NewText("- 巨人/猎人/愚者/错误/魔女/太阳 全途径仪式 ✓", 200, 200, 255);

                SoundEngine.PlaySound(SoundID.Item4, player.position);
                for (int i = 0; i < 30; i++)
                {
                    Dust.NewDust(player.position, player.width, player.height, DustID.Electric, 0, 0, 0, default, 1.5f);
                }
            }
            return true;
        }

        // 简易配方：1个土块徒手合成
        public override void AddRecipes()
        {

        }
    }
}