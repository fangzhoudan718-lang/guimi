using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

// 注意：这个类故意放在 zhashi.Content 而不是它所在的文件夹名。
// 魔药散落在 zhashi.Content.Items.Potions.* 下面，放在父命名空间里，
// 它们就不必为了这一句「晋升的动静」再加一条 using。
namespace zhashi.Content
{
    /// <summary>
    /// 晋升那一刻的动静。
    ///
    /// 门途径与黑皇帝途径：一声雷 + 一记屏幕震动。
    /// 愚者途径序列一：换成《愚者的帷幕》——随模组自己合成的原创纯音乐，
    /// 整首谱写在 SourceAssets/Music/Build-FoolsVeil.py 里，重跑一次就能改。
    ///
    /// 魔药是在玩家自己的客户端上喝下去的，所以这里把这件事交给服务器转播一次，
    /// 让同一局里的所有人都在同一刻听见、看见；离得远的人只是脚下一颤。
    /// </summary>
    public class PromotionPulse : ModSystem
    {
        /// <summary>门途径。</summary>
        public const byte Door = 0;
        /// <summary>黑皇帝途径。</summary>
        public const byte BlackEmperor = 1;
        /// <summary>愚者途径（序列一有专属落幕曲）。</summary>
        public const byte Fool = 2;

        /// <summary>落幕曲的资源名：Assets/Music/FoolsVeil.mp3（或 .ogg）。</summary>
        public const string FoolsVeilPath = "Assets/Music/FoolsVeil";
        /// <summary>落幕曲响多久（tick）：曲子约 96 秒，这里给 100 秒。</summary>
        public const int FoolsVeilDuration = 100 * 60;

        /// <summary>黑皇帝序列一的曲子：《弑序》，资源名 Assets/Music/UsurperTheme.mp3（或 .ogg）。</summary>
        public const string UsurperThemePath = "Assets/Music/UsurperTheme";
        /// <summary>《弑序》响多久（tick）：曲子 30 秒 + 余响，这里给 34 秒。</summary>
        public const int UsurperThemeDuration = 34 * 60;

        private static PromotionPulse instance;
        private static int foolsVeilSlot = -1;
        private static int foolsVeilTicks;
        private static int usurperSlot = -1;
        private static int usurperTicks;
        private static bool warnedMissingSong;

        /// <summary>落幕曲的音乐槽位；文件不在时是 -1。</summary>
        public static int FoolsVeilSlot => foolsVeilSlot;
        /// <summary>落幕曲是否正在播放。</summary>
        public static bool FoolsVeilPlaying => foolsVeilTicks > 0 && foolsVeilSlot >= 0;
        /// <summary>《弑序》的音乐槽位；文件不在时是 -1。</summary>
        public static int UsurperThemeSlot => usurperSlot;
        /// <summary>《弑序》是否正在播放。</summary>
        public static bool UsurperThemePlaying => usurperTicks > 0 && usurperSlot >= 0;
        /// <summary>有没有哪首晋升曲正在播。</summary>
        public static bool AnyPromotionMusicPlaying => FoolsVeilPlaying || UsurperThemePlaying;

        public override void OnModLoad()
        {
            instance = this;

            // 找不到音频也不影响加载，只是那一下没有音乐——免得少了文件就整个模组起不来。
            if (ModContent.HasAsset("zhashi/" + FoolsVeilPath))
                foolsVeilSlot = MusicLoader.GetMusicSlot(Mod, FoolsVeilPath);
            else
                Mod.Logger.Info("没有找到 Assets/Music/FoolsVeil（.mp3 / .ogg），愚者序列一的落幕曲将不会播放。");

            if (ModContent.HasAsset("zhashi/" + UsurperThemePath))
                usurperSlot = MusicLoader.GetMusicSlot(Mod, UsurperThemePath);
            else
                Mod.Logger.Info("没有找到 Assets/Music/UsurperTheme（.mp3 / .ogg），黑皇帝序列一的曲子将不会播放。");
        }

        public override void Unload()
        {
            instance = null;
            foolsVeilSlot = -1;
            foolsVeilTicks = 0;
            usurperSlot = -1;
            usurperTicks = 0;
        }

        /// <summary>落幕曲的倒计时。</summary>
        public override void PostUpdateEverything()
        {
            if (foolsVeilTicks > 0) foolsVeilTicks--;
            if (usurperTicks > 0) usurperTicks--;
        }

        /// <summary>让本机立刻开始放《愚者的帷幕》。</summary>
        public static void StartFoolsVeil()
        {
            if (Main.dedServ) return;
            if (foolsVeilSlot < 0)
            {
                if (!warnedMissingSong)
                {
                    warnedMissingSong = true;
                    Main.NewText("[诡秘] 缺少 Assets/Music/FoolsVeil 音频，这次的落幕曲放不出来。", 200, 170, 220);
                }
                return;
            }

            foolsVeilTicks = FoolsVeilDuration;
            Main.newMusic = foolsVeilSlot;
        }

        /// <summary>让本机立刻开始放《弑序》——开场第一拍就是高潮。</summary>
        public static void StartUsurperTheme()
        {
            if (Main.dedServ) return;
            if (usurperSlot < 0)
            {
                if (!warnedMissingSong)
                {
                    warnedMissingSong = true;
                    Main.NewText("[诡秘] 缺少 Assets/Music/UsurperTheme 音频，这次的曲子放不出来。", 200, 170, 220);
                }
                return;
            }

            usurperTicks = UsurperThemeDuration;
            Main.newMusic = usurperSlot;
        }

        /// <summary>喝下魔药、序列改完之后调用这一句。</summary>
        public static void Raise(Player player, byte kind, int sequence = 0)
        {
            if (instance == null || player == null || !player.active) return;

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                // 自己那一下也由服务器广播回来，免得本地响两次、别人却什么都没听到。
                Send(player.whoAmI, kind, sequence);
                return;
            }

            Play(player, kind, sequence);
            if (Main.netMode == NetmodeID.Server) Send(player.whoAmI, kind, sequence);
        }

        private static void Send(int playerIndex, byte kind, int sequence)
        {
            if (instance == null) return;
            ModPacket packet = instance.Mod.GetPacket();
            packet.Write((byte)LotMNetMsg.PromotionPulse);
            packet.Write((byte)playerIndex);
            packet.Write(kind);
            packet.Write((byte)sequence);
            packet.Send();
        }

        public static void Receive(BinaryReader reader, int whoAmI)
        {
            int index = reader.ReadByte();
            byte kind = reader.ReadByte();
            byte sequence = reader.ReadByte();
            if (index < 0 || index >= Main.maxPlayers) return;

            if (Main.netMode == NetmodeID.Server)
            {
                // 服务器只负责转播：让所有人（包括刚喝下药的那位）同时听到。
                ModPacket packet = instance.Mod.GetPacket();
                packet.Write((byte)LotMNetMsg.PromotionPulse);
                packet.Write((byte)index);
                packet.Write(kind);
                packet.Write(sequence);
                packet.Send(-1, -1);
                return;
            }

            Play(Main.player[index], kind, sequence);
        }

        /// <summary>真的去响、去晃；无头服务器上什么都不做。</summary>
        public static void Play(Player player, byte kind, int sequence = 0)
        {
            if (Main.dedServ) return;

            // 愚者走到序列一：不放雷，改放那首落幕曲。
            if (kind == Fool && sequence == 1)
            {
                StartFoolsVeil();
                Main.NewText("—— 『愚者的帷幕』", 200, 170, 220);
                return;
            }

            // 黑皇帝走到序列一：放《弑序》，开场第一拍就是高潮；雷声让位给音乐，
            // 只留那记震动，让画面也跟着砸一下。
            if (kind == BlackEmperor && sequence == 1)
            {
                StartUsurperTheme();
                Main.NewText("—— 『弑序』", 220, 180, 140);
                AddShake(player, 9f, 26);
                return;
            }

            SoundEngine.PlaySound(
                SoundID.Thunder with { Volume = 0.85f, Pitch = kind == BlackEmperor ? -0.25f : 0.15f },
                player.Center);

            AddShake(player, 7.5f, 20);
        }

        /// <summary>屏幕震动：离得越远晃得越轻，远处的人只是脚下一颤，不抢画面。</summary>
        private static void AddShake(Player player, float strength, int frames)
        {
            float distance = Vector2.Distance(Main.LocalPlayer.Center, player.Center);
            float falloff = MathHelper.Clamp(1f - distance / 3200f, 0.25f, 1f);
            Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                player.Center, Vector2.UnitY, strength * falloff, 2.6f, frames, 3200f, "LotMPromotion"));
        }
    }

    /// <summary>
    /// 落幕曲播放期间把当前音乐压住，交给《愚者的帷幕》。
    /// 优先级给到最高的一档：这一下就是要盖过包括 Boss 曲在内的一切。
    /// </summary>
    public class FoolsVeilScene : ModSceneEffect
    {
        public override int Music => PromotionPulse.FoolsVeilPlaying
            ? PromotionPulse.FoolsVeilSlot
            : PromotionPulse.UsurperThemeSlot;
        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;
        public override bool IsSceneEffectActive(Player player) => PromotionPulse.AnyPromotionMusicPlaying;
    }
}
