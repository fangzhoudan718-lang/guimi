using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Audio;
using Terraria.ModLoader;
using Terraria.Chat;            // 用于联机发送消息
using Terraria.Localization;    // 用于文本本地化
using zhashi.Content;

namespace zhashi.Content.NPCs
{
    public class HunterRitualGlobalNPC : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            // 1. 排除无效目标 (友方、雕像怪、血量太少的怪、假人)
            if (npc.friendly || npc.SpawnedFromStatue || npc.lifeMax < 5 || npc.type == NPCID.TargetDummy)
                return;

            // 2. 寻找击杀者 (lastInteraction 在联机中主要由服务器掌握)
            if (npc.lastInteraction == -1)
                return;

            Player killer = Main.player[npc.lastInteraction];

            if (!killer.active || killer.dead)
                return;

            // 3. 检查仪式条件
            LotMPlayer modPlayer = killer.GetModPlayer<LotMPlayer>();

            // 只有猎人途径序列 5 需要这个仪式
            if (modPlayer.currentHunterSequence == 5)
            {
                // 必须有至少 5 个随从
                if (killer.numMinions >= 5)
                {
                    if (modPlayer.ironBloodRitualProgress < LotMPlayer.IRON_BLOOD_RITUAL_TARGET)
                    {
                        // --- 核心修改：增加进度 ---
                        modPlayer.ironBloodRitualProgress++;

                        // --- 核心修改：联机同步 ---
                        if (Main.netMode == NetmodeID.Server)
                        {
                            // 如果是服务器，必须手动调用 SyncPlayer 把数据发给那个玩家
                            // 这里的 toWho: killer.whoAmI 表示只发给杀怪的那个玩家 (节省带宽)
                            // 前提：你的 LotMPlayer.cs 里的 SyncPlayer 方法必须包含 ironBloodRitualProgress 的读写！
                            modPlayer.SyncPlayer(killer.whoAmI, -1, false);
                        }

                        // --- 视觉反馈 ---
                        // CombatText 在服务器端调用会自动广播给周围玩家，所以不需要特殊处理
                        CombatText.NewText(killer.getRect(), new Color(255, 50, 50), "+1 铁血", false, false);

                        // --- 仪式完成提示 ---
                        if (modPlayer.ironBloodRitualProgress == LotMPlayer.IRON_BLOOD_RITUAL_TARGET)
                        {
                            string msg = "铁血仪式条件已达成！你可以晋升了！";
                            Color msgColor = new Color(50, 255, 50);

                            if (Main.netMode == NetmodeID.Server)
                            {
                                // 服务器端发送消息给所有人 (或者只给 killer)
                                ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(msg), msgColor);
                            }
                            else
                            {
                                // 单人模式直接显示
                                Main.NewText(msg, 50, 255, 50);
                            }

                            // 播放音效
                            SoundEngine.PlaySound(SoundID.MaxMana, killer.position);
                        }
                    }
                }
            }
        }
    }
}