using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using SubworldLibrary;
using zhashi.Content.Dimensions;

namespace zhashi.Content.Items.Consumables
{
    public class LuckEnhancementRitual : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("转运仪式"); // 1.4.4+ 使用 hjson
            // Tooltip.SetDefault("福生玄黄仙尊...\n福生玄黄天君...\n福生玄黄上帝...\n福生玄黄天尊...\n(长按以迈出步伐)");
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp; // 像举起水晶一样
            Item.value = 0;
            Item.rare = ItemRarityID.Red;
            Item.channel = true; // 允许长按检测
            Item.noUseGraphic = false;
        }

        public override void HoldItem(Player player)
        {
            // 只有在长按鼠标使用时才触发
            if (player.channel)
            {
                // 锁定玩家移动
                player.velocity = Vector2.Zero;

                // 1. 播放音效 (每4秒循环一次，模拟念咒)
                if (Main.GameUpdateCount % 240 == 0)
                {
                    // 建议添加一个 "Incantation.mp3" 到 Assets/Sounds/
                    // SoundEngine.PlaySound(new SoundStyle("zhashi/Assets/Sounds/Incantation"), player.Center);
                    SoundEngine.PlaySound(SoundID.Item103, player.Center); // 暂时用类似暗影魔法的声音替代
                }

                // 2. 生成灰雾特效
                if (Main.rand.NextBool(3))
                {
                    Vector2 dustPos = player.Center + Main.rand.NextVector2Circular(60, 60);
                    Vector2 velocity = (player.Center - dustPos).SafeNormalize(Vector2.Zero) * 2f; // 吸入效果
                    Dust d = Dust.NewDustPerfect(dustPos, DustID.Smoke, velocity, 100, Color.Gray, 2.0f);
                    d.noGravity = true;
                }

                // 3. 屏幕震动，模拟仪式感
                if (Main.GameUpdateCount % 60 == 0)
                {
                    Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(player.Center, Main.rand.NextVector2Circular(1, 1), 2f, 6f, 10));
                }
            }
        }

        public override bool? UseItem(Player player)
        {

            if (player.whoAmI == Main.myPlayer)
            {

                if (!SubworldSystem.IsActive<OriginCastle>())
                {
                    SubworldSystem.Enter<OriginCastle>();
                }
                else
                {
                    SubworldSystem.Exit();
                }
            }
            return true;
        }
    }
}