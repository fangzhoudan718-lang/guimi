using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using zhashi.Content;

namespace zhashi.Content.Items
{
    public class LotMWeaponInfo : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.damage <= 0) return;

            var player = Main.LocalPlayer;
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            if (!modPlayer.IsBeyonder) return;

            // 1. 计算加成
            float bonusPercent = 0f;
            string sequenceName = "";
            Color titleColor = Color.White;
            string statsText = "";

            bool isMelee = item.CountsAsClass(DamageClass.Melee);

            if (modPlayer.currentSequence == 9)
            {
                sequenceName = "【序列9：战士加成】";
                titleColor = Color.LightSkyBlue;
                if (isMelee) { bonusPercent = 0.10f; statsText = "+10% 伤害\n+5% 暴击率"; }
                else statsText = "(非近战武器无加成)";
            }
            else if (modPlayer.currentSequence == 8)
            {
                sequenceName = "【序列8：格斗家加成】";
                titleColor = Color.LightGreen;
                if (isMelee) { bonusPercent = 0.20f; statsText = "+20% 伤害\n+15% 攻速\n+5% 暴击率"; }
                else statsText = "(非近战武器无加成)";
            }
            else if (modPlayer.currentSequence == 7)
            {
                sequenceName = "【序列7：武器大师加成】";
                titleColor = Color.Orange;
                // 序7通用10% + 若近战序9序8的20%
                float baseBonus = 0.10f;
                if (isMelee) baseBonus += 0.20f;
                bonusPercent = baseBonus;
                statsText = $"+{(int)(bonusPercent * 100)}% 伤害\n+20% 暴击率\n+5 护甲穿透";
            }
            // === 【新增】序列6显示逻辑 ===
            else if (modPlayer.currentSequence == 6)
            {
                sequenceName = "【序列6：黎明骑士加成】";
                titleColor = Color.Gold; // 金色传说！

                // 序6通用: 序7(10%) + 序6(15%) = 25% (所有武器都有)
                float baseBonus = 0.25f;

                // 如果是近战，还要加上序9(10%) + 序8(10%) = 额外20%
                if (isMelee) baseBonus += 0.20f;

                bonusPercent = baseBonus;

                statsText = $"+{(int)(bonusPercent * 100)}% 伤害\n+20% 暴击率\n+5 护甲穿透\n光环照明";
            }

            // 2. 修改原本伤害行 (显示 20 + 5)
            TooltipLine damageLine = tooltips.Find(x => x.Name == "Damage" && x.Mod == "Terraria");

            if (damageLine != null && bonusPercent > 0)
            {
                int baseDamage = item.damage;
                int extraDamage = (int)(baseDamage * bonusPercent);
                int displayTotal = baseDamage + extraDamage;

                string[] splitText = damageLine.Text.Split(' ');
                string damageTypeWord = splitText.Length > 1 ? splitText[1] : "伤害";

                // 结果显示为： 25 (20 + 5) 近战伤害
                damageLine.Text = $"{displayTotal} ({baseDamage} + {extraDamage}) {damageTypeWord}";
                damageLine.OverrideColor = Color.Yellow; // 让这行字变黄，提示有加成
            }

            // 3. 添加底部说明
            if (sequenceName != "")
            {
                tooltips.Add(new TooltipLine(Mod, "LotM_Title", sequenceName) { OverrideColor = titleColor });
                tooltips.Add(new TooltipLine(Mod, "LotM_Stats", statsText) { OverrideColor = Color.White });
            }
        }
    }
}