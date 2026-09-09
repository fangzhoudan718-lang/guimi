using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using zhashi.Content;
using zhashi.Content.NPCs.Bosses.Aurmir; // 引用 Boss 命名空间

namespace zhashi.Content.Items.Materials
{
    public class ExtraordinaryBlood : ModItem
    {
        // 依然使用你的贴图 (或者红宝石)
        // public override string Texture => "Terraria/Images/Item_" + ItemID.Ruby; 
        // 如果你有 ExtraordinaryBlood.png，就把上面那行删掉或注释掉

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.maxStack = 99;
            Item.value = 0; // 无价
            Item.rare = ItemRarityID.Red; // 红色稀有度

            // 召唤物属性
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = true; // 消耗品
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var player = Main.LocalPlayer;
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 基础描述
            TooltipLine line = new TooltipLine(Mod, "BloodDesc", "蕴含着一丝古老神性的鲜血。");
            line.OverrideColor = Color.Red;
            tooltips.Add(line);

            // 仪式说明
            string timeStatus = (!Main.dayTime && Main.time < 16200) ? "[c/00FF00:符合]" : "[c/FF0000:不符合]"; // 16200 ticks = 4.5小时 (7:30PM - 12:00AM)
            string seqStatus = (modPlayer.currentSequence <= 2) ? "[c/00FF00:符合]" : "[c/FF0000:不符合]";
            string cdStatus = (modPlayer.twilightResurrectionCooldown <= 0) ? "[c/00FF00:就绪]" : $"[c/FF0000:冷却中({modPlayer.twilightResurrectionCooldown / 60}s)]";

            tooltips.Add(new TooltipLine(Mod, "Ritual1", $"召唤条件 1: 黄昏与夜晚初期 (7:30 PM - 12:00 AM) {timeStatus}"));
            tooltips.Add(new TooltipLine(Mod, "Ritual2", $"召唤条件 2: 序列2 荣耀者及以上 {seqStatus}"));
            tooltips.Add(new TooltipLine(Mod, "RitualWarning", "警告：使用将清空生命值以触发[黄昏重生]。"));
            tooltips.Add(new TooltipLine(Mod, "RitualCheck", $"重生能力状态: {cdStatus}"));
        }

        public override bool CanUseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 1. 检查 Boss 是否已经存在
            if (NPC.AnyNPCs(ModContent.NPCType<Aurmir>())) return false;

            // 2. 检查序列 (必须是荣耀者)
            if (modPlayer.currentSequence > 2)
            {
                if (player.whoAmI == Main.myPlayer) Main.NewText("你的位格不足以通过鲜血沟通古神。", 255, 50, 50);
                return false;
            }

            // 3. 检查时间 (必须是晚上，且在前半夜)
            // Main.dayTime = false 代表晚上 (7:30 PM 开始)
            // Main.time 是从 7:30 PM 开始计时的 tick 数
            // 我们限制在 7:30 PM 到 12:00 AM 之间 (4.5 小时 = 16200 ticks)
            if (Main.dayTime || Main.time > 16200)
            {
                if (player.whoAmI == Main.myPlayer) Main.NewText("必须在黄昏或夜色初降时进行仪式...", 200, 100, 0); // 橘色提示
                return false;
            }

            return true;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var modPlayer = player.GetModPlayer<LotMPlayer>();

                // 播放献祭音效
                SoundEngine.PlaySound(SoundID.NPCDeath10, player.position);

                // === 核心机制：致死打击 ===
                // 这行代码会扣除 99999 血，强制触发 LotMPlayer 中的 PreKill 逻辑
                // 如果你的[黄昏重生]在冷却中，你就会真的死掉！
                // 如果[黄昏重生]就绪，你会被PreKill拦截，回满血，并触发重生特效
                player.KillMe(PlayerDeathReason.ByCustomReason(player.name + " 献祭自身以唤醒巨人王..."), 99999, 0);

                // 只有当玩家由于PreKill逻辑没有死（也就是复活成功了），才召唤Boss
                // 我们可以通过检测玩家是否还活着来判断
                if (!player.dead)
                {
                    SoundEngine.PlaySound(SoundID.Roar, player.position);

                    // 召唤 Boss
                    int type = ModContent.NPCType<Aurmir>();
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        NPC.SpawnOnPlayer(player.whoAmI, type);
                    }
                    else
                    {
                        NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: type);
                    }

                    Main.NewText("古老的号角声在黄昏中回荡...", 255, 100, 0);
                    Main.NewText("巨人王·奥尔米尔 降临！", 255, 0, 0);
                }
            }
            return true;
        }

        public override void AddRecipes()
        {
            // 之前的配方：凝胶
            // 只有序列9能做，这里保留作为获取途径
            Condition seqCondition = new Condition(
                Language.GetOrRegister(Mod, "Conditions.IsSequence9", () => "需序列9及以上"),
                () => Main.LocalPlayer.GetModPlayer<LotMPlayer>().currentSequence <= 9
            );

            CreateRecipe()
                .AddIngredient(ItemID.Gel, 1)
                .AddCondition(seqCondition)
                .Register();
        }
    }
}