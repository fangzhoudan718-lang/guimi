using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID; // 确保引用
using Terraria.ModLoader;
using zhashi.Content;

namespace zhashi.Content.Projectiles
{
    public class CreationDomainProjectile : ModProjectile
    {
        // 复用贴图，记得确保 FullMoonCircle.png 存在
        public override string Texture => "zhashi/Content/Projectiles/FullMoonCircle";

        public override void SetDefaults()
        {
            Projectile.width = 200;
            Projectile.height = 200;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.scale = 0.1f;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            if (!player.active || player.dead) { Projectile.Kill(); return; }
            LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

            if (!modPlayer.isCreationDomain) { Projectile.Kill(); return; }

            Projectile.timeLeft = 2;
            Projectile.Center = player.Bottom + new Vector2(0, -2f);

            // 动画：变大、淡入
            if (Projectile.alpha > 50) Projectile.alpha -= 10;
            Projectile.scale = MathHelper.Lerp(Projectile.scale, 1.5f, 0.1f);

            // 【核心修改】移除了旋转代码，使其固定
            // Projectile.rotation += 0.01f; 

            // --- 领域效果 ---
            if (Main.GameUpdateCount % 30 == 0)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && !npc.friendly && npc.Distance(player.Center) < 600f)
                    {
                        int damage = (int)(player.statLifeMax2 * 0.05f);
                        npc.SimpleStrikeNPC(damage, 0, false, 0f, DamageClass.Magic, true);

                        Dust d = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.LifeDrain, 0, 0, 0, default, 1.5f);
                        d.velocity = (player.Center - npc.Center).SafeNormalize(Vector2.Zero) * 10f;
                        d.noGravity = true;
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() / 2f;

            Color color = Color.LightGreen * 0.8f;
            color.A = 100;

            Main.EntitySpriteDraw(texture, drawPosition, null, color, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}