using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using System.Collections.Generic;
using zhashi.Content; // 引用 LotMPlayer
using zhashi.Content.NPCs.Town;
// 引用所有魔药
using zhashi.Content.Items.Potions;
using zhashi.Content.Items.Potions.Fool;
using zhashi.Content.Items.Potions.Marauder;
using zhashi.Content.Items.Potions.Sun;
using zhashi.Content.Items.Potions.Moon;
using zhashi.Content.Items.Potions.Hunter;
using zhashi.Content.Items.Potions.Demoness;


namespace zhashi.Content.UI.Profiles
{
    public class OldNeilProfile : GalgameProfile
    {
        public override string HeadTexture => "zhashi/Content/UI/Portraits/OldNeil_Head";
        public override string StandingTexture => "zhashi/Content/UI/Portraits/OldNeil_Standing";

        public override float OffsetX => 0f;
        public override float OffsetY => 100f;

        public override string GetDialogue(NPC npc)
        {
            var story = Main.LocalPlayer.GetModPlayer<LotMStoryPlayer>();
            var lotmPlayer = Main.LocalPlayer.GetModPlayer<LotMPlayer>();

            // ★★★ 修复点1：将 0 改为 10 ★★★
            // 如果还没领过魔药 且 还是凡人(序列10)
            if (!story.HasReceivedStarterPotion && lotmPlayer.currentSequence == 10)
            {
                return "邓恩跟我说了，你是一个很有潜力的新人。\n既然加入了值夜者，你就需要选择一条非凡途径。\n不用担心，教会虽然主要掌握‘黑夜’途径，但我们也有其他途径的配方和材料。\n你想好要成为什么样的非凡者了吗？";
            }

            List<string> chats = new List<string>
            {
                "记得，‘扮演法’是消化的关键...哦对了，你看到我的咖啡豆了吗？",
                "如果你的魔药消化完了，记得来找我，或许我能给你提供下一个序列的线索...当然，这需要贡献点。",
                "这一周的报销单又被驳回了...女神啊，这日子没法过了。",
                "如果你在灵界看到了奇怪的东西，千万不要回应，立刻断开灵视，明白吗？",
                "想学古赫密斯语吗？我可以教你，只要你帮我分担一点手写工作。"
            };
            return chats[Main.rand.Next(chats.Count)];
        }

        public override void SetupButtons(NPC npc, GalgameDialogueUI ui)
        {
            var story = Main.LocalPlayer.GetModPlayer<LotMStoryPlayer>();
            var lotmPlayer = Main.LocalPlayer.GetModPlayer<LotMPlayer>();

            // 只有当玩家是“序列10 (凡人)”时才显示选项
            if (!story.HasReceivedStarterPotion && lotmPlayer.currentSequence == 10)
            {
                // 1. 愚者 (占卜家)
                ui.AddButton("我想成为【占卜家】(愚者途径)", () => {
                    GivePotion(npc, ui, story,
                        ModContent.ItemType<SeerPotion>(),
                        "【占卜家】...不错的选择。这是罗塞尔大帝曾经选择的途径。\n虽然前期缺乏攻击手段，但在神秘学辅助上非常强力。\n小心耳边的低语，不要迷失在灵界中。"
                    );
                });

                // 2. 错误 (偷盗者)
                ui.AddButton("我想成为【偷盗者】(错误途径)", () => {
                    GivePotion(npc, ui, story,
                        ModContent.ItemType<MarauderPotion>(),
                        "【偷盗者】？这可是很少见的途径。\n这一途径的非凡者擅长窃取和欺诈...嘿，先把你的手从我的咖啡罐上拿开！\n希望你能用这股力量做正确的事。"
                    );
                });

                // 3. 太阳 (歌颂者)
                ui.AddButton("我想成为【歌颂者】(太阳途径)", () => {
                    GivePotion(npc, ui, story,
                        ModContent.ItemType<BardPotion>(),
                        "【歌颂者】...赞美太阳？咳咳，在黑夜教会里选这个确实有点...特别。\n不过，净化死灵和驱散阴影的能力在面对很多怪物时都非常管用。\n拿着吧，愿光芒指引你。"
                    );
                });

                // 4. 月亮 (药师)
                ui.AddButton("我想成为【药师】(月亮途径)", () => {
                    GivePotion(npc, ui, story,
                        ModContent.ItemType<ApothecaryPotion>(),
                        "【药师】？太好了！\n以后我们可以一起调配药剂了，我也能省下不少去医院的钱。\n这不仅能治疗伤口，还能驯化一些特定的神奇生物。"
                    );
                });

                // 5. 巨人 (战士)
                ui.AddButton("我想成为【战士】(巨人途径)", () => {
                    GivePotion(npc, ui, story,
                        ModContent.ItemType<WarriorPotion>(),
                        "【战士】...简单，直接，粗暴。\n如果你喜欢冲在最前面保护队友，那这就是最适合你的途径。\n不过别忘了，非凡者依靠的不只是肌肉，还有脑子。"
                    );
                });

                // 6. 红祭司 (猎人)
                ui.AddButton("我想成为【猎人】(红祭司途径)", () => {
                    GivePotion(npc, ui, story,
                        ModContent.ItemType<HunterPotion>(),
                        "【猎人】？这也是个擅长战斗的途径，尤其是陷阱和追踪。\n只要你别像那个安德森一样到处挑衅惹事就行...\n控制好你的火焰，别把查尼斯门给烧了。"
                    );
                });

                // 7. 魔女
                ui.AddButton("我想成为【刺客】(魔女途径)", () => {
                    GivePotion(npc, ui, story,
                        ModContent.ItemType<AssassinPotion>(),
                        "【刺客】？（他的表情有些古怪，但还是笑呵呵的）\n...很强的途径。"
                    );
                });
            }

            // 通用离开按钮
            ui.AddButton("离开", () => {
                Main.LocalPlayer.SetTalkNPC(-1);
                ui.Close();
            });
        }

        private void GivePotion(NPC npc, GalgameDialogueUI ui, LotMStoryPlayer story, int itemID, string reactionText)
        {
            // 1. 给予物品
            Main.LocalPlayer.QuickSpawnItem(npc.GetSource_GiftOrReward(), itemID);

            // 2. 标记已领取
            story.HasReceivedStarterPotion = true;

            // 3. 更新 UI 文本
            ui.SetDialogueText(reactionText);

            // 4. 清除按钮，只留离开
            ui.ClearButtons();
            ui.AddButton("谢谢你，老尼尔 (离开)", () => {
                Main.LocalPlayer.SetTalkNPC(-1);
                ui.Close();
            });

            Main.NewText("你获得了对应的序列9魔药！饮用它以开启非凡之路。", 255, 215, 0);
        }
    }
}