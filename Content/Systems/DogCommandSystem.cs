using Terraria.ModLoader;
using Terraria;
using zhashi.Content.NPCs; // 【关键修改】引用 zhashi

namespace zhashi.Content.Systems // 【关键修改】命名空间改成 zhashi
{
    public class DogCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;

        public override string Command => "name";

        public override string Usage => "/name <新名字>";

        public override string Description => "给最近的狗狗改名";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length == 0)
            {
                caller.Reply("请输入名字！例如: /name 小黑");
                return;
            }

            string newName = args[0];

            // 寻找最近的狗
            DogNPC closestDog = null;
            float minDst = 500f;

            foreach (NPC n in Main.npc)
            {
                // 这里 ModNPC is DogNPC 会自动识别上面引用的 zhashi.Content.NPCs.DogNPC
                if (n.active && n.ModNPC is DogNPC dog && n.Distance(caller.Player.Center) < minDst)
                {
                    closestDog = dog;
                    minDst = n.Distance(caller.Player.Center);
                }
            }

            if (closestDog != null)
            {
                closestDog.MyName = newName;
                caller.Reply($"狗狗的名字已修改为: {newName}");
            }
            else
            {
                caller.Reply("附近没有找到你的狗（请靠近它）");
            }
        }
    }
}