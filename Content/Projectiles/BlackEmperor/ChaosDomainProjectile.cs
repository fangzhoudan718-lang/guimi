using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Effects.BlackEmperor;
using zhashi.Content.Pathways.BlackEmperor;

namespace zhashi.Content.Projectiles.BlackEmperor
{
    /// <summary>
    /// 混乱场的载体：一块以施法者为圆心的割离领域。
    /// 它只负责把「另一个世界」画在瓦片之上、生物与玩家之下（所以被困在里面的人仍然看得见），
    /// 以及收束时的淡出；场内发生了什么——谁认错了距离、哪道弹幕换了主人——
    /// 全部由 BlackEmperorPlayer 在服务器上裁定。
    /// </summary>
    public class ChaosDomainProjectile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";
        private float Radius => Projectile.ai[0];
        /// <summary>
        /// 0 = 混乱场（跟着施法者）；1 = 规则领域（钉在律令落点，只画一圈边界）；
        /// 2 = 狂乱波（原地向外推开、推到尽头就散的一圈波动）。
        /// </summary>
        private int Kind => (int)Projectile.ai[1];
        private const float ParallaxLimit = 12f;
        private const float RageWaveMaxRadius = 520f;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 4;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.hide = true;
        }

        /// <summary>
        /// 挂在 behindNPCs 这一档：盖住地形，但留在生物与玩家下面。
        /// （写在 ModSystem.PostDrawTiles 里会连同瓦片一起盖到实体上面。）
        /// </summary>
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
            List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
            => behindNPCs.Add(index);

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.hide = true;
            if (Kind == 2)
            {
                // 狂乱波：一圈从脚下推开、推到尽头就散的波动，不需要维护也不需要圆心。
                Projectile.ai[0] += 26f;
                if (Projectile.ai[0] >= RageWaveMaxRadius) Projectile.Kill();
                return;
            }
            if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers) { Projectile.Kill(); return; }
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                if (Projectile.timeLeft > 24) Projectile.timeLeft = 24;
                return;
            }

            BlackEmperorPlayer be = owner.GetModPlayer<BlackEmperorPlayer>();
            bool maintained;
            if (Kind == 1)
            {
                // 规则领域钉在律令落下的地方：律令还在，这块地就还在。
                Projectile.Center = be.lawAnchor;
                maintained = be.activeLaw != BlackEmperorLaw.None;
            }
            else
            {
                // 混乱场的圆心永远跟着施法者：玩家就站在这个世界的正中央。
                Projectile.Center = owner.Center;
                maintained = be.chaosFieldActive;
            }
            Projectile.velocity = Vector2.Zero;

            if (maintained) Projectile.timeLeft = 600;
            else if (Projectile.timeLeft > 24) Projectile.timeLeft = 24;

            // 收束时先响一声，再慢慢暗下去。
            if (Projectile.localAI[1] == 0f && Projectile.timeLeft <= 24)
            {
                Projectile.localAI[1] = 1f;
                if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Item104 with { Volume = 0.28f, Pitch = -0.35f }, Projectile.Center);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!BlackEmperorVisuals.IsOnScreen(Projectile.Center, Radius + 60f)) return false;
            float age = ++Projectile.localAI[0];
            float fade = Math.Min(1f, age / 20f) * Math.Min(1f, Projectile.timeLeft / 24f);

            if (Kind == 1)
            {
                // 规则领域只留一圈边界：看得见「规矩在这里被改写」，但不遮住任何东西。
                float pulse = 0.30f + 0.06f * (float)Math.Sin(age * 0.05f);
                BlackEmperorVisuals.Boundary(Projectile.Center, Radius, pulse * fade, age * 0.002f);
                return false;
            }

            if (Kind == 2)
            {
                // 推得越远越淡，散尽的瞬间正好收尾。
                float progress = MathHelper.Clamp(Radius / RageWaveMaxRadius, 0f, 1f);
                BlackEmperorVisuals.Boundary(Projectile.Center, Radius, (1f - progress) * 0.75f, age * 0.01f);
                return false;
            }

            // 站在正中央的人没有视差可看，于是让内景自己慢慢转：两层反向转，读起来就是深度。
            float farSpin = age * 0.0035f;
            float nearSpin = -age * 0.008f;

            // 从旁边看过去的人则得到和空间牢笼一样的视差：离圆心越远，内景偏得越多。
            Vector2 eye = Main.LocalPlayer.Center - Projectile.Center;
            if (eye.LengthSquared() > 700f * 700f) eye = eye.SafeNormalize(Vector2.Zero) * 700f;
            Vector2 farOffset = -eye * 0.026f;
            Vector2 nearOffset = -eye * 0.062f;
            if (farOffset.Length() > ParallaxLimit) farOffset = farOffset.SafeNormalize(Vector2.Zero) * ParallaxLimit;
            if (nearOffset.Length() > ParallaxLimit) nearOffset = nearOffset.SafeNormalize(Vector2.Zero) * ParallaxLimit;

            BlackEmperorVisuals.Domain(ChaosDomainLayer.Void, Projectile.Center, Radius, fade, 0f);
            BlackEmperorVisuals.Domain(ChaosDomainLayer.Far, Projectile.Center + farOffset, Radius, fade, farSpin);
            BlackEmperorVisuals.Domain(ChaosDomainLayer.Near, Projectile.Center + nearOffset, Radius, fade, nearSpin);
            BlackEmperorVisuals.Boundary(Projectile.Center, Radius, 0.45f * fade, nearSpin * 0.5f);
            return false;
        }
    }
}
