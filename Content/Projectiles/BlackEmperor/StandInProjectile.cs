using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Pathways.BlackEmperor;

namespace zhashi.Content.Projectiles.BlackEmperor
{
    /// <summary>
    /// 「定义·替身」的实体。它不是贴在玩家身上的残影，而是会在地面上自行行走、
    /// 跳过障碍并跟随本体的另一具身体。移动由服务器裁定，外观取自所属玩家。
    /// </summary>
    public class StandInProjectile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Ghost";

        private Player visualClone;
        private int visualRefresh;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 42;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.damage = 0;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.netImportant = true;
            Projectile.timeLeft = 2;
        }

        public override void AI()
        {
            if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers)
            {
                Projectile.Kill();
                return;
            }

            Player owner = Main.player[Projectile.owner];
            BlackEmperorPlayer emperor = owner.GetModPlayer<BlackEmperorPlayer>();
            if (!owner.active)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            // 客户端只表现服务器已经生成的替身，避免状态包与弹幕包到达顺序不同导致误杀。
            if (Main.netMode != NetmodeID.MultiplayerClient && (owner.dead || emperor.Sequence > 1 ||
                emperor.definitionMode != (int)BlackEmperorDefinition.StandIn || emperor.standInCooldown > 0))
            {
                Projectile.Kill();
                return;
            }
            Projectile.ai[0]++;

            // 跟丢时直接在本体身后重新落脚，避免替身卡在卸载区块里。
            Vector2 toOwner = owner.Center - Projectile.Center;
            if (Math.Abs(toOwner.X) > 1200f || Math.Abs(toOwner.Y) > 700f)
            {
                Projectile.Bottom = owner.Bottom + new Vector2(-owner.direction * 72f, 0f);
                Projectile.velocity = Vector2.Zero;
                Projectile.netUpdate = true;
                return;
            }

            // 近处会自行踱步，远处才追上玩家；看起来是一具独立身体，而非拴在身后的宠物。
            float idleOffset = (float)Math.Sin(Projectile.ai[0] / 95f) * 34f;
            float desiredX = owner.Center.X - owner.direction * 78f + idleOffset;
            float deltaX = desiredX - Projectile.Center.X;
            float targetSpeed = Math.Abs(deltaX) > 34f ? MathHelper.Clamp(deltaX * 0.055f, -5.2f, 5.2f) : 0f;
            Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, targetSpeed, targetSpeed == 0f ? 0.12f : 0.18f);

            if (Math.Abs(Projectile.velocity.X) > 0.12f)
                Projectile.direction = Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;
            else
                Projectile.direction = Projectile.spriteDirection = owner.direction;

            Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.38f, 10f);

            // 玩家已经跳上高台时，替身会主动寻找一次跳跃机会。
            bool grounded = Math.Abs(Projectile.velocity.Y) < 0.08f;
            if (grounded && owner.Bottom.Y < Projectile.Bottom.Y - 48f && Math.Abs(deltaX) > 28f)
                Projectile.velocity.Y = -6.8f;

            if (Main.netMode != NetmodeID.MultiplayerClient && (int)Projectile.ai[0] % 30 == 0)
                Projectile.netUpdate = true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // 撞墙时像玩家一样跳过去，而不是穿墙或原地抖动。
            if (Math.Abs(oldVelocity.X) > 0.2f && Math.Abs(Projectile.velocity.X) < 0.05f)
            {
                Projectile.velocity.X = oldVelocity.X * 0.35f;
                Projectile.velocity.Y = -7.2f;
            }
            else if (oldVelocity.Y > 0f)
            {
                Projectile.velocity.Y = 0f;
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active) return false;

            // SerializedClone 会完整保留盔甲、染料和外观派生字段；每秒刷新一次即可，避免逐帧分配。
            if (visualClone == null || visualRefresh-- <= 0)
            {
                visualClone = owner.SerializedClone();
                visualRefresh = 60;
            }

            visualClone.position = Projectile.position;
            visualClone.velocity = Projectile.velocity;
            visualClone.direction = Projectile.direction;
            visualClone.itemAnimation = 0;
            visualClone.itemTime = 0;
            visualClone.itemRotation = 0f;
            visualClone.fullRotation = 0f;
            visualClone.gfxOffY = 0f;
            visualClone.mount.Dismount(visualClone);
            visualClone.HeldItem.noUseGraphic = true;
            visualClone.PlayerFrame();

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            try
            {
                using var current = new Main.CurrentPlayerOverride(visualClone);
                Main.PlayerRenderer.DrawPlayer(Main.Camera, visualClone, visualClone.position, 0f,
                    visualClone.fullRotationOrigin, 0f);
            }
            finally
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            }

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ || Projectile.ai[1] < 1f) return;

            SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.55f, Pitch = -0.25f }, Projectile.Center);
            for (int i = 0; i < 24; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(3.2f, 4.2f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(12f, 20f),
                    DustID.PurpleCrystalShard, velocity, 105, new Color(125, 84, 145), 1.05f);
                dust.noGravity = true;
            }
        }
    }
}
