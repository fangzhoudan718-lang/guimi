using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;
using Terraria.ModLoader;
using Terraria.GameContent.UI.Elements;

namespace zhashi.Content.UI
{
    // 这一行 class 定义绝对不能少！
    public class RoselleDiaryUI : UIState
    {
        private UIElement dragableArea;
        private UIImage diaryImage;

        public override void OnInitialize()
        {
            // === 1. 填入你图片的实际像素大小 ===
            float width = 1495f;
            float height = 800f;

            dragableArea = new UIElement();
            dragableArea.Width.Set(width, 0f);
            dragableArea.Height.Set(height, 0f);

            // === 2. 强制居中算法 ===
            // 原理：将锚点定在屏幕正中心 (0.5f)，然后向左上方移动图片尺寸的一半
            dragableArea.Left.Set(-width / 2f, 0.5f);
            dragableArea.Top.Set(-height / 2f, 0.5f);

            // 必须将 HAlign/VAlign 设为 0，防止冲突
            dragableArea.HAlign = 0f;
            dragableArea.VAlign = 0f;

            Append(dragableArea);

            // === 3. 加载图片 ===
            // 路径指向 Content/UI/RoselleDiaryPage
            var texture = ModContent.Request<Texture2D>("zhashi/Content/UI/RoselleDiaryPage");

            diaryImage = new UIImage(texture);
            diaryImage.Width.Set(0, 1f);   // 填满容器
            diaryImage.Height.Set(0, 1f);  // 填满容器

            // 点击关闭
            diaryImage.OnLeftClick += (evt, element) => {
                ModContent.GetInstance<RoselleDiaryUISystem>().CloseUI();
            };

            dragableArea.Append(diaryImage);
        }
    }
}