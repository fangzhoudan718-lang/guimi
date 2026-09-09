using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace zhashi.Content.Projectiles
{
    public class HandOfGodAirCannon : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        // 本地变量：存储同步过来的镐力
        private int playerMaxPickPower = 0;

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            float chargeTime = Projectile.ai[0];

            // 初始化逻辑
            if (Projectile.localAI[0] == 0)
            {
                // 1. 体型
                Projectile.scale = 1f + (chargeTime / 120f);

                int oldWidth = Projectile.width;
                int oldHeight = Projectile.height;
                Projectile.width = (int)(40 * Projectile.scale);
                Projectile.height = (int)(40 * Projectile.scale);
                Projectile.position.X -= (Projectile.width - oldWidth) / 2f;
                Projectile.position.Y -= (Projectile.height - oldHeight) / 2f;

                // 2. 射程
                Projectile.timeLeft = (int)(30 + chargeTime * 2.5f);

                // =========================================================
                // 【★联机同步修复：直接读取 AI[1]★】
                // 不再扫描背包，而是直接读取发射时传入的数值。
                // 这个数值会自动同步到服务器端。
                // =========================================================
                playerMaxPickPower = (int)Projectile.ai[1];

                Projectile.localAI[0] = 1;
            }

            // 3. 执行破坏
            if (chargeTime > 180)
            {
                DestroyTiles();
            }

            // 4. 特效
            int dustCount = (int)(2 * Projectile.scale);
            for (int i = 0; i < dustCount; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 2, Projectile.height / 2), DustID.Cloud, -Projectile.velocity * 0.1f, 100, default, 1f * Projectile.scale);
                d.noGravity = true;

                if (chargeTime > 180 && Main.rand.NextBool(3))
                {
                    Dust d2 = Dust.NewDustPerfect(Projectile.Center, DustID.GoldCoin, Vector2.Zero, 0, default, 0.6f * Projectile.scale);
                    d2.noGravity = true;
                }
            }
        }

        private void DestroyTiles()
        {
            // 破坏方块的代码只能在服务器(或单人)运行，客户端只负责看特效
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            int radius = (int)(2 * Projectile.scale);

            Vector2 center = Projectile.Center;
            int minX = (int)(center.X / 16f) - radius;
            int maxX = (int)(center.X / 16f) + radius;
            int minY = (int)(center.Y / 16f) - radius;
            int maxY = (int)(center.Y / 16f) + radius;

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY) continue;

                    Vector2 tilePos = new Vector2(x * 16 + 8, y * 16 + 8);
                    if (Vector2.Distance(center, tilePos) > radius * 16) continue;

                    Tile tile = Main.tile[x, y];
                    if (tile.HasTile)
                    {
                        // 使用同步过来的 playerMaxPickPower 进行判断
                        int requiredPower = GetRequiredPickaxePower(tile.TileType);

                        if (playerMaxPickPower >= requiredPower)
                        {
                            WorldGen.KillTile(x, y, false, false, false);

                            // 服务器通知所有客户端：这里方块没了
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendTileSquare(-1, x, y, 1);
                        }
                        else
                        {
                            // 挖掘失败特效 (仅服务器发包太麻烦，这里简化处理，反正客户端自己也会跑AI看得到特效)
                            if (Main.rand.NextBool(20))
                            {
                                // 这里主要是给服务器端逻辑用，实际视觉效果靠客户端的AI部分
                            }
                        }
                    }
                }
            }
        }

        private int GetRequiredPickaxePower(int tileType)
        {
            if (tileType == TileID.LihzahrdBrick || tileType == TileID.LihzahrdAltar || tileType == TileID.LihzahrdFurnace) return 210;
            if (tileType == TileID.Chlorophyte) return 200;
            if (tileType == TileID.BlueDungeonBrick || tileType == TileID.GreenDungeonBrick || tileType == TileID.PinkDungeonBrick) return 110;
            if (tileType == TileID.Ebonstone || tileType == TileID.Crimstone || tileType == TileID.Pearlstone || tileType == TileID.Hellstone || tileType == TileID.Obsidian) return 65;
            if (tileType == TileID.Meteorite || tileType == TileID.Demonite || tileType == TileID.Crimtane) return 55;
            if (tileType == TileID.DemonAltar) return 9999;
            return 0;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(Projectile.Center, Projectile.velocity, 5f * Projectile.scale, 5f, 20, 1000f));
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
        }
    }
}