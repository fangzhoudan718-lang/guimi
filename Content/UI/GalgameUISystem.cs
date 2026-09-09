using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using zhashi.Content.UI.Profiles;
using zhashi.Content.Items;

namespace zhashi.Content.UI
{
    [Autoload(Side = ModSide.Client)]
    public class GalgameUISystem : ModSystem
    {
        internal UserInterface galgameInterface;
        internal GalgameDialogueUI galgameUI;

        // 公开属性
        public GalgameDialogueUI DialogueUI => galgameUI;

        public static bool visible = false;
        public static bool isTalkingToArrodes = false;

        public override void Load()
        {
            galgameUI = new GalgameDialogueUI();
            galgameUI.Activate();
            galgameInterface = new UserInterface();
            galgameInterface.SetState(galgameUI);
        }

        public static void OpenArrodesUI()
        {
            if (visible && isTalkingToArrodes)
            {
                visible = false;
                isTalkingToArrodes = false;
            }
            else
            {
                visible = true;
                isTalkingToArrodes = true;
                Main.npcChatText = "";
            }
        }

        public override void UpdateUI(GameTime gameTime)
        {
            bool shouldShow = CheckIfShouldShowUI();

            if (shouldShow)
            {
                if (!visible) OpenUI();
                galgameInterface?.Update(gameTime);
            }
            else
            {
                if (visible) CloseUI();
            }
        }

        private bool CheckIfShouldShowUI()
        {
            // 1. 优先检查手动模式（日记）
            if (galgameUI.IsManualMode)
            {
                if (Main.playerInventory) return false;
                return true;
            }

            // 2. 阿罗德斯
            if (isTalkingToArrodes)
            {
                if (Main.LocalPlayer.controlLeft || Main.LocalPlayer.controlRight ||
                    Main.LocalPlayer.controlDown || Main.LocalPlayer.controlUp ||
                    Main.playerInventory)
                {
                    return false;
                }
                return true;
            }

            // 3. 原版/Mod NPC
            int talkNPC = Main.LocalPlayer.talkNPC;
            if (talkNPC != -1)
            {
                if (Main.InGuideCraftMenu || Main.npcShop != 0) return false;

                int npcType = Main.npc[talkNPC].type;

                if (npcType == NPCID.Nurse ||
                    npcType == NPCID.Guide ||
                    npcType == NPCID.Dryad ||
                    npcType == NPCID.Angler ||
                    // 加上邓恩·史密斯 (注意括号要成对)
                    npcType == ModContent.NPCType<Content.NPCs.Town.DunnSmith>() || 
                    npcType == ModContent.NPCType<Content.NPCs.Town.OldNeil>()
                   )
                {
                    return true;
                }
            }
            return false;
        }

        public void OpenUI()
        {
            visible = true;
            Main.npcChatText = "";
        }

        public void CloseUI()
        {
            visible = false;
            isTalkingToArrodes = false;
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int dialogIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: NPC / Sign Dialog"));

            if (dialogIndex != -1)
            {
                if (CheckIfShouldShowUI())
                {
                    layers.RemoveAt(dialogIndex);
                    layers.Insert(dialogIndex, new LegacyGameInterfaceLayer(
                        "zhashi: GalgameUI",
                        delegate
                        {
                            visible = true;
                            galgameInterface.Draw(Main.spriteBatch, new GameTime());
                            return true;
                        },
                        InterfaceScaleType.UI)
                    );
                }
            }
        }
    }
}