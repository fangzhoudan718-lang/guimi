using Terraria;
using Terraria.ModLoader;
using zhashi.Content.NPCs;
using zhashi.Content.Items; // 引用 Item 命名空间
using Microsoft.Xna.Framework;

namespace zhashi.Content
{
    public class DogNameCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;
        public override string Command => "dogname";
        public override string Usage => "/dogname 新名字";
        public override string Description => "给你的狗狗改名";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length == 0)
            {
                Main.NewText("❌ 格式错误！请输入: /dogname 新名字", Color.Red);
                return;
            }

            string newName = string.Join(" ", args);
            bool found = false;
            Player player = caller.Player;

            // 1. 修改世界里存在的实体狗
            foreach (NPC npc in Main.npc)
            {
                if (npc.active && npc.ModNPC is DogNPC dog)
                {
                    // 只有主人才能改名
                    if (dog.OwnerName == player.name || string.IsNullOrEmpty(dog.OwnerName))
                    {
                        string oldName = dog.MyName;
                        dog.MyName = newName;
                        dog.OwnerName = player.name; // 改名时顺便确立归属权

                        // 飘字特效
                        CombatText.NewText(npc.getRect(), Color.Green, newName, true);
                        found = true;

                        // 强制同步给服务器
                        if (Main.netMode != Terraria.ID.NetmodeID.SinglePlayer)
                            npc.netUpdate = true;
                    }
                }
            }

            // 2. 【核心修复】同时修改玩家背包里的狗牌物品数据！
            // 否则下次召唤/重进游戏，狗的名字又会变回物品上存的旧名字
            int itemUpdateCount = 0;
            for (int i = 0; i < 58; i++) // 遍历玩家背包
            {
                Item item = player.inventory[i];
                if (item.type == ModContent.ItemType<DogItem>())
                {
                    if (item.ModItem is DogItem dogItem)
                    {
                        dogItem.DogName = newName;
                        itemUpdateCount++;
                    }
                }
            }

            if (found)
            {
                Main.NewText($"✅ 狗狗已改名为 [{newName}]！(同步更新了 {itemUpdateCount} 个狗牌)", Color.Green);
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Chat);
            }
            else
            {
                Main.NewText("❌ 没找到属于你的狗狗！请先把它召唤出来。", Color.Red);
            }
        }
    }
}