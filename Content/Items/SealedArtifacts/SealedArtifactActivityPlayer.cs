using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace zhashi.Content.Items.SealedArtifacts
{
    public class SealedArtifactActivityPlayer : ModPlayer
    {
        public float Activity { get; private set; }
        private bool warnedRestless;
        private bool warnedDangerous;
        public override void SaveData(TagCompound tag) => tag["sealedArtifactActivity"] = Activity;
        public override void LoadData(TagCompound tag) => Activity = tag.GetFloat("sealedArtifactActivity");

        public override void PostUpdate()
        {
            bool equipped = HasEquipped(ModContent.ItemType<MisfortuneDie>());
            Activity = equipped
                ? MathHelper.Clamp(Activity + 0.01f, 0f, 100f)
                : Math.Max(0f, Activity - 0.025f);

            // Clients mirror the deterministic gauge for responsive tooltips; consequences remain server-owned.
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            if (Activity >= 35f && !warnedRestless)
            {
                warnedRestless = true;
                Notify("Mods.zhashi.Messages.SealedArtifact.Restless", new Color(210, 190, 145));
            }
            if (Activity >= 70f && !warnedDangerous)
            {
                warnedDangerous = true;
                Notify("Mods.zhashi.Messages.SealedArtifact.Dangerous", new Color(225, 120, 115));
            }
            if (equipped && Activity >= 100f) TriggerOutbreak();

            if (Activity < 25f) warnedRestless = false;
            if (Activity < 55f) warnedDangerous = false;
        }

        public bool SuppressWithCharacteristic()
        {
            if (Activity < 15f) return false;
            Activity = Math.Max(0f, Activity - 45f);
            warnedDangerous = Activity >= 70f;
            Notify("Mods.zhashi.Messages.SealedArtifact.Suppressed", new Color(145, 220, 190));
            return true;
        }

        private void TriggerOutbreak()
        {
            Activity = 45f;
            warnedRestless = true;
            warnedDangerous = false;
            Player.AddBuff(BuffID.Confused, 240);
            Player.AddBuff(BuffID.Slow, 300);

            if (!Player.dead)
            {
                Vector2 position = Player.Center + Main.rand.NextVector2CircularEdge(420f, 220f);
                NPC.NewNPC(Player.GetSource_Misc("MisfortuneDieOutbreak"), (int)position.X, (int)position.Y, NPCID.Wraith);
            }
            Notify("Mods.zhashi.Messages.SealedArtifact.Outbreak", new Color(210, 90, 120));
        }

        private bool HasEquipped(int type)
        {
            foreach (Item item in Player.armor)
                if (item.type == type) return true;
            return false;
        }

        private void Notify(string key, Color color)
        {
            if (Main.netMode == NetmodeID.Server)
                ChatHelper.SendChatMessageToClient(NetworkText.FromKey(key), color, Player.whoAmI);
            else if (Player.whoAmI == Main.myPlayer)
                Main.NewText(Language.GetTextValue(key), color);
        }
    }

    public class SealedArtifactActivityGlobalItem : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type != ModContent.ItemType<MisfortuneDie>()) return;
            float activity = Main.LocalPlayer.GetModPlayer<SealedArtifactActivityPlayer>().Activity;
            tooltips.Add(new TooltipLine(Mod, "ArtifactActivity", Language.GetTextValue("Mods.zhashi.Messages.SealedArtifact.TooltipActivity", (int)activity)) { OverrideColor = new Color(205, 155, 175) });
            tooltips.Add(new TooltipLine(Mod, "ArtifactContainment", Language.GetTextValue("Mods.zhashi.Messages.SealedArtifact.TooltipContainment")) { OverrideColor = new Color(135, 205, 185) });
        }
    }
}
