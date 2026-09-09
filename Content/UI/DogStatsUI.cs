using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria.ID;
using zhashi.Content.NPCs;
using System;

namespace zhashi.Content.UI
{
    public class DogStatsUI : UIState
    {
        public static bool Visible = false;
        public static DogNPC TargetDog = null;

        private UIPanel panel;
        private UIText statsText;
        private VanillaItemSlotWrapper[] equipSlots = new VanillaItemSlotWrapper[3];

        private int _textUpdateTimer = 0;

        public override void OnInitialize()
        {
            panel = new UIPanel();
            panel.Width.Set(360, 0);
            panel.Height.Set(330, 0);
            panel.HAlign = 0.5f;
            panel.VAlign = 0.5f;
            panel.BackgroundColor = new Color(33, 43, 79) * 0.9f;
            Append(panel);

            var renameHint = new UIText("提示: 聊天栏输入 /dogname 新名字 改名", 0.75f);
            renameHint.HAlign = 0.5f;
            renameHint.Top.Set(10, 0);
            renameHint.TextColor = Color.LightGray;
            panel.Append(renameHint);

            for (int i = 0; i < 3; i++)
            {
                equipSlots[i] = new VanillaItemSlotWrapper(ItemSlot.Context.ChestItem, 0.85f);
                equipSlots[i].Left.Set(20 + i * 50, 0);
                equipSlots[i].Top.Set(40, 0);
                equipSlots[i].Item = new Item();
                equipSlots[i].Item.SetDefaults(0);
                equipSlots[i].ValidItemFunc = item => item.IsAir || item.defense > 0 || item.accessory;
                panel.Append(equipSlots[i]);
            }

            var slotHint = new UIText("装备栏 (防具/配饰)", 0.7f);
            slotHint.Left.Set(180, 0);
            slotHint.Top.Set(45, 0);
            panel.Append(slotHint);

            statsText = new UIText("等待数据...", 0.8f);
            statsText.Top.Set(95, 0);
            statsText.Left.Set(10, 0);
            panel.Append(statsText);

            var closeButton = new UITextPanel<string>("保存并关闭");
            closeButton.Width.Set(120, 0);
            closeButton.Height.Set(30, 0);
            closeButton.HAlign = 0.5f;
            closeButton.Top.Set(280, 0);
            closeButton.OnLeftClick += (evt, element) => { CloseMenu(); };
            panel.Append(closeButton);
        }

        public void OpenMenu(DogNPC dog)
        {
            if (dog == null || !dog.NPC.active) return;
            TargetDog = dog;
            Visible = true;
            Main.playerInventory = true;

            // 打开时，把狗身上的装备复制到 UI 里
            for (int i = 0; i < 3; i++)
            {
                if (dog.DogInventory[i] != null && !dog.DogInventory[i].IsAir)
                    equipSlots[i].Item = dog.DogInventory[i].Clone();
                else
                {
                    equipSlots[i].Item = new Item();
                    equipSlots[i].Item.SetDefaults(0);
                }
            }
        }

        public void CloseMenu()
        {
            // 关闭时最后保存一次，并同步网络
            if (TargetDog != null && TargetDog.NPC.active)
            {
                SyncInventoryToDog();
                if (Main.netMode != NetmodeID.SinglePlayer) TargetDog.NPC.netUpdate = true;
            }
            Visible = false; TargetDog = null; Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuClose);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // 【核心修复】这里必须用 TargetDog.NPC.active
            if (TargetDog == null || !TargetDog.NPC.active) return;

            // 1. 数据同步：从 NPC -> UI
            for (int i = 0; i < 3; i++)
            {
                if (TargetDog.DogInventory[i] == null) TargetDog.DogInventory[i] = new Item();
                equipSlots[i].Item = TargetDog.DogInventory[i];
            }

            // 2. 执行绘制
            base.Draw(spriteBatch);

            // 3. 数据同步：UI -> NPC
            for (int i = 0; i < 3; i++)
            {
                TargetDog.DogInventory[i] = equipSlots[i].Item;
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (!Visible) return;
            if (TargetDog == null || !TargetDog.NPC.active || Main.LocalPlayer.Distance(TargetDog.NPC.Center) > 1000f) { CloseMenu(); return; }

            base.Update(gameTime);

            // 【核心修复】实时同步
            // 每一帧都把 UI 里的装备覆盖到狗身上
            // 这样 DogNPC.AI() 计算属性时，就能立刻读到新装备了
            SyncInventoryToDog();

            if (ContainsPoint(Main.MouseScreen)) Main.LocalPlayer.mouseInterface = true;

            _textUpdateTimer++;
            if (_textUpdateTimer > 15) { _textUpdateTimer = 0; UpdateText(); }
        }

        // 辅助方法：把 UI 物品槽的数据写入狗的背包
        private void SyncInventoryToDog()
        {
            if (TargetDog != null && TargetDog.NPC.active)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (equipSlots[i].Item != null)
                        TargetDog.DogInventory[i] = equipSlots[i].Item.Clone();
                    else
                        TargetDog.DogInventory[i] = new Item();
                }
            }
        }

        private void UpdateText()
        {
            if (TargetDog == null) return;

            string status = TargetDog.isStaying ? "[c/FF0000:停留 (炮台模式)]" : "[c/00FF00:跟随 (战斗模式)]";
            int finalDef = TargetDog.NPC.defDefense; // 现在这里能读到实时防御力了
            int regen = TargetDog.CalculatedRegen;
            int dmg = TargetDog.NPC.damage;
            int atkSpeedPct = (int)(TargetDog.CalculatedAttackSpeed * 100);
            string nameDisplay = TargetDog.MyName ?? "狗狗";

            string pathwayName = GetPathwayName(TargetDog.currentPathway);
            string abilityInfo = TargetDog.GetAbilityDescription();
            string seqColor = "FFFFFF";

            switch (TargetDog.currentPathway)
            {
                case 1: seqColor = "DAA520"; break;
                case 2: seqColor = "FF4500"; break;
                case 3: seqColor = "DC143C"; break;
                case 4: seqColor = "800080"; break;
                case 5: seqColor = "C0C0C0"; break;
                case 6: seqColor = "FFD700"; break;
            }

            string seqDisplay = TargetDog.currentSequence >= 10
                ? "[c/888888:凡狗 (未开启非凡之路)]"
                : $"[c/{seqColor}:{pathwayName} 途径 - 序列 {TargetDog.currentSequence}]";

            string text = $"名称: [c/FFA500:{nameDisplay}]\n" +
                          $"{seqDisplay}\n" +
                          $"生命: {TargetDog.NPC.life} / {TargetDog.NPC.lifeMax}\n" +
                          $"防御: {finalDef} (装备加成)\n" +
                          $"攻击: {dmg}   攻速: {atkSpeedPct}%\n" +
                          $"回复: {regen} 点/秒\n" +
                          $"状态: {status}\n" +
                          $"[c/00FFFF:{abilityInfo}]";

            statsText.SetText(text);
        }

        private string GetPathwayName(int id)
        {
            switch (id) { case 1: return "巨人"; case 2: return "猎人"; case 3: return "月亮"; case 4: return "愚者"; case 5: return "错误"; case 6: return "太阳"; default: return "未知"; }
        }
    }
}