using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Audio; // 引用音效核心库
using ReLogic.Content; // 引用资源加载库

namespace zhashi.Content.Systems
{
    public class AudioPreloader : ModSystem
    {
        public override void Load()
        {
            // 1. 只有客户端才需要加载声音，服务器(dedServ)不需要，否则会报错
            if (!Main.dedServ)
            {
                // 2. 强制预加载“赞美太阳”的歌声
                // AssetRequestMode.ImmediateLoad = "别等了，现在立刻马上给我加载进内存"
                // 这里的路径必须和你 LotMPlayer.cs 里写的 SoundStyle 路径完全一致（不需要加 .ogg 后缀）

                ModContent.Request<SoundEffect>("zhashi/Assets/Sounds/BardSong", AssetRequestMode.ImmediateLoad);

                // 如果你以后还有其他大音效（比如核爆、长吟唱），也可以复制上面这行加在这里
            }
        }
    }
}