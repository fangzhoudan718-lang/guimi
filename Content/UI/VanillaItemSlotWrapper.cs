using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent; // 【关键引用】
using Terraria.GameInput;
using Terraria.UI;

namespace zhashi.Content.UI
{
    public class VanillaItemSlotWrapper : UIElement
    {
        internal Item Item; // 当前槽位内的物品
        private readonly int _context; // 物品槽上下文 (例如 BankItem, InventoryItem 等)
        private readonly float _scale; // 缩放比例
        internal Func<Item, bool> ValidItemFunc; // 可选：限制能放入的物品规则

        public VanillaItemSlotWrapper(int context = ItemSlot.Context.BankItem, float scale = 1f)
        {
            _context = context;
            _scale = scale;
            Item = new Item();
            Item.SetDefaults(0);
            Width.Set(TextureAssets.InventoryBack.Value.Width * scale, 0f);
            Height.Set(TextureAssets.InventoryBack.Value.Height * scale, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            float oldScale = Main.inventoryScale;
            Main.inventoryScale = _scale;

            Rectangle rectangle = GetDimensions().ToRectangle();

            if (ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface)
            {
                Main.LocalPlayer.mouseInterface = true; // 告诉游戏鼠标在 UI 上

                if (ValidItemFunc == null || ValidItemFunc(Main.mouseItem))
                {
                    ItemSlot.Handle(ref Item, _context);
                }
            }

            ItemSlot.Draw(spriteBatch, ref Item, _context, rectangle.TopLeft());

            if (ContainsPoint(Main.MouseScreen))
            {
                Main.HoverItem = Item.Clone();
                Main.hoverItemName = Item.Name;
            }

            Main.inventoryScale = oldScale;
        }
    }
}