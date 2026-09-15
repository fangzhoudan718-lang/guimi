using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace zhashi.Content.Globals.Flavor
{
    /// <summary>
    /// 小概率替换一条原版城镇 NPC 闲聊，让途径身份自然地进入日常世界。
    /// 仅在本地打开对话时运行，不产生网络状态，也不会覆盖商店或任务按钮。
    /// </summary>
    public class PathwayTownDialogueGlobalNPC : GlobalNPC
    {
        public override void GetChat(NPC npc, ref string chat)
        {
            if (!npc.townNPC || Main.gameMenu || !Main.rand.NextBool(4))
                return;

            LotMPlayer lotm = Main.LocalPlayer.GetModPlayer<LotMPlayer>();
            string key = null;

            if (npc.type == NPCID.Guide && lotm.currentFoolSequence <= 9)
                key = "GuideFool";
            else if (npc.type == NPCID.Nurse && lotm.currentMoonSequence <= 9)
                key = "NurseMoon";
            else if (npc.type == NPCID.ArmsDealer && lotm.currentHunterSequence <= 9)
                key = "ArmsDealerHunter";
            else if (npc.type == NPCID.GoblinTinkerer && lotm.currentMarauderSequence <= 9)
                key = "GoblinError";
            else if (npc.type == NPCID.Wizard && lotm.currentDoorSequence <= 9)
                key = "WizardDoor";
            else if (npc.type == NPCID.TaxCollector && lotm.currentBlackEmperorSequence <= 9)
                key = "TaxCollectorEmperor";
            else if (npc.type == NPCID.Dryad && lotm.currentSunSequence <= 9)
                key = "DryadSun";
            else if (npc.type == NPCID.Clothier && lotm.currentDemonessSequence <= 9)
                key = "ClothierDemoness";
            else if (npc.type == NPCID.DD2Bartender && lotm.currentSequence <= 9)
                key = "TavernkeepGiant";
            else if (npc.type == NPCID.PartyGirl && lotm.currentWheelSequence <= 9)
                key = "PartyGirlWheel";

            if (key != null)
                chat = Language.GetTextValue($"Mods.zhashi.Messages.PathwayTownChat.{key}");
        }
    }
}
