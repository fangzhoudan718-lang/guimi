using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace zhashi.Content.UI.Profiles
{
    public class KleinsDiaryProfile : GalgameProfile
    {
        // 设为null，配合 GalgameDialogueUI 自动隐藏图片并拓宽文本框
        public override string HeadTexture => null;
        public override string StandingTexture => null;

        private int pageIndex = 0;

        // 确保这里有完整的 6 页内容
        private readonly string[] diaryPages = new string[]
        {
            "【5月29日】\n\n韦尔奇找到了我，神神秘秘地说他得到了一本第四纪图铎王朝时期的古老笔记。\n他邀请我和娜娅一起参与解密。作为历史系的学生，我无法拒绝这种诱惑。",
            "【6月20日】\n\n简直不可思议！我们成功解读了笔记的第一页。\n虽然内容晦涩，但这确实是安提格努斯家族留下的东西。\n这是一个伟大的发现！",
            "【6月21日 - 25日】\n\n事情开始变得不对劲了... 笔记里反复出现奇怪的词汇：\n“黑皇帝”、“图铎王朝”、“霍纳奇斯山主峰”...\n每次解读这些内容，我都感觉精神压力巨大，甚至开始出现幻听。",
            "【6月26日】\n\n（字迹变得非常混乱，难以辨认）\n\n我在哪？我干了什么？\n记忆...缺失了...有什么东西在看着我...\n我不该看那本笔记的，那是亵渎！那是污染！",
            "【6月27日】\n\n（这一页是一片空白，只有纸张被疯狂抓挠的痕迹，似乎暗示着日记的主人已经处于精神崩溃的边缘...）",
            "[c/FF0000:【6月28日】]\n\n（页面上有一个触目惊心的血手印）\n\n[c/FF0000:所有人都会死，包括我。]"
        };

        public override string GetDialogue(NPC npc)
        {
            if (pageIndex < 0) pageIndex = 0;
            if (pageIndex >= diaryPages.Length) pageIndex = diaryPages.Length - 1;
            return diaryPages[pageIndex];
        }

        public override void SetupButtons(NPC npc, GalgameDialogueUI ui)
        {
            // 上一页
            if (pageIndex > 0)
            {
                ui.AddButton("<- 上一页", () => {
                    pageIndex--;
                    ui.SetDialogueText(GetDialogue(npc));
                    ui.ClearButtons();
                    SetupButtons(npc, ui);
                });
            }

            // 下一页 (只要索引小于 5，就会显示下一页)
            if (pageIndex < diaryPages.Length - 1)
            {
                ui.AddButton("下一页 ->", () => {
                    pageIndex++;
                    ui.SetDialogueText(GetDialogue(npc));
                    ui.ClearButtons();
                    SetupButtons(npc, ui);
                });
            }
            else
            {
                // 关闭
                ui.AddButton("合上日记", () => {
                    pageIndex = 0; // 重置页码
                    ui.Close();    // 调用我们修复后的 Close 方法
                });
            }
        }

        public void ResetPage()
        {
            pageIndex = 0;
        }
    }
}