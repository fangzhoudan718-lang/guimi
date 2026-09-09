using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Projectiles.Demoness
{
    public class MirrorCloneProjectile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Ghost"; // 隐形贴图

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 42;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 7200;
            Projectile.ignoreWater = true;

            Projectile.damage = 30;
            Projectile.knockBack = 2f;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];

            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            // 1. 镜像移动
            float mirrorAxisX = Projectile.ai[0];
            float distFromAxis = owner.Center.X - mirrorAxisX;
            Vector2 targetPos = new Vector2(mirrorAxisX - distFromAxis, owner.Center.Y);

            Projectile.Center = targetPos;
            Projectile.velocity = Vector2.Zero;

            // 2. 记录方向
            Projectile.direction = -owner.direction;
            Projectile.spriteDirection = -owner.direction;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            // ====================================================
            // 1. 保存现场
            // ====================================================
            int oldDir = owner.direction;
            float oldItemRot = owner.itemRotation;
            Vector2 oldItemLoc = owner.itemLocation;
            bool oldNoUseGraphic = owner.HeldItem.noUseGraphic;
            int oldItemAnimation = owner.itemAnimation; // 保存动画状态

            // ====================================================
            // 2. 设置分身状态 (取消武器与动作)
            // ====================================================
            owner.direction = Projectile.direction;

            // ---【彻底取消武器镜像】---
            // 1. 强制隐藏武器：无论拿什么，分身手里都是空的
            owner.HeldItem.noUseGraphic = true;

            // 2. 强制重置手臂动作：
            // 让分身看起来只是在移动，不再尝试举起看不见的武器
            // 这样就彻底避免了“手臂指向奇怪方向”的问题
            owner.itemAnimation = 0;
            owner.itemRotation = 0;

            // ====================================================
            // 3. 绘制
            // ====================================================
            try
            {
                Vector2 drawPos = Projectile.position + new Vector2(0, owner.gfxOffY);
                Main.PlayerRenderer.DrawPlayer(Main.Camera, owner, drawPos, 0f, owner.fullRotationOrigin, 0f);
            }
            catch { }

            // ====================================================
            // 4. 还原现场 (全部还原)
            // ====================================================
            owner.direction = oldDir;
            owner.itemRotation = oldItemRot;
            owner.itemLocation = oldItemLoc;
            owner.HeldItem.noUseGraphic = oldNoUseGraphic;
            owner.itemAnimation = oldItemAnimation; // 这一步非常重要，否则本体会无法攻击

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Buffs.Curse.AfflictionCurseBuff>(), 300);
        }
    }
}