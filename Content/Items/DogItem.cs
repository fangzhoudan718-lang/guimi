using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Localization;
using zhashi.Content.NPCs;
using Terraria.Audio; // 引用音效

namespace zhashi.Content.Items
{
    public class DogItem : ModItem
    {
        // 这里的变量充当“存档”的作用
        public string DogName = "旺财";
        public int DogPathway = 0;
        public int DogSequence = 10;
        public int BonusMaxLife = 0;
        public Item[] Equipment;

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.maxStack = 1; // 唯一
            Item.value = Item.sellPrice(0, 1, 0, 0);
            Item.rare = ItemRarityID.Blue;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = false; // 不消耗
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 10)
                .AddCondition(new Condition(
                    Language.GetOrRegister("Mods.zhashi.Conditions.DogNotOwned", () => "未拥有/未召唤"),
                    () => !Main.LocalPlayer.HasItem(Type) && !IsDogActive(Main.LocalPlayer)
                ))
                .Register();
        }

        // ========================================================
        // 核心修复部分
        // ========================================================
        public override bool? UseItem(Player player)
        {
            // 1. 检查狗是否活着 (本地检查)
            NPC existingDog = GetActiveDog(player);

            // ------------------------------------------------
            // 情况 A：狗还活着 -> 传送 (无需复活)
            // ------------------------------------------------
            if (existingDog != null)
            {
                // 传送逻辑建议只在本地或服务端执行一次同步，通常直接修改位置即可
                existingDog.Center = player.Center;
                existingDog.velocity = Vector2.Zero;
                if (existingDog.ModNPC is DogNPC dogScript)
                {
                    dogScript.attackState = 0;
                    dogScript.isStaying = false;
                    // 同步状态变化
                    if (Main.netMode != NetmodeID.SinglePlayer)
                        existingDog.netUpdate = true;
                }

                // 仅本地玩家显示提示
                if (player.whoAmI == Main.myPlayer)
                {
                    Main.NewText($"{GetDogNameDisplay()} 回到了你身边！", 100, 255, 100);
                    SoundEngine.PlaySound(SoundID.Item6, player.position); // 传送音效
                }
                return true;
            }

            // ------------------------------------------------
            // 情况 B：狗死了 -> 消耗材料复活
            // ------------------------------------------------

            // 检查材料 (仅客户端检查提示，服务端也需要检查防止作弊，这里简化为通用逻辑)
            int boneCount = player.CountItem(ItemID.Bone);
            int fleshCount = player.CountItem(ItemID.RottenChunk);

            // 如果材料不足
            if (boneCount < 100 || fleshCount < 100)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    Main.NewText($"无法复活！缺少复活材料：", Color.Red);
                    if (boneCount < 100) Main.NewText($"- 骨头: {boneCount}/100", Color.Red);
                    if (fleshCount < 100) Main.NewText($"- 腐肉: {fleshCount}/100", Color.Red);
                }
                return true;
            }

            // 消耗材料 (全端执行，保证库存同步)
            // 注意：ConsumeItem 在联机客户端调用会自动同步消耗
            for (int i = 0; i < 100; i++) player.ConsumeItem(ItemID.Bone);
            for (int i = 0; i < 100; i++) player.ConsumeItem(ItemID.RottenChunk);

            // --- 召唤新狗 (仅限 服务端 或 单人模式) ---
            // 客户端绝对不能调用 NewNPC，否则会生成一个只有自己能看见且马上消失的假NPC
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int spawnX = (int)player.Center.X;
                int spawnY = (int)player.Bottom.Y - 40;

                int index = NPC.NewNPC(player.GetSource_ItemUse(Item), spawnX, spawnY, ModContent.NPCType<DogNPC>());

                if (index >= 0 && index < Main.maxNPCs && Main.npc[index].ModNPC is DogNPC newDog)
                {
                    // 注入数据
                    newDog.MyName = this.DogName;
                    newDog.currentPathway = this.DogPathway;
                    newDog.currentSequence = this.DogSequence;
                    newDog.BonusMaxHP = this.BonusMaxLife;
                    newDog.OwnerName = player.name;

                    // 恢复装备
                    if (this.Equipment != null)
                    {
                        newDog.DogInventory = new Item[3];
                        for (int i = 0; i < 3; i++)
                        {
                            if (this.Equipment[i] != null) newDog.DogInventory[i] = this.Equipment[i].Clone();
                            else newDog.DogInventory[i] = new Item();
                        }
                    }

                    // 初始化血量
                    newDog.NPC.lifeMax = 300 + newDog.BonusMaxHP + (newDog.currentSequence < 10 ? (10 - newDog.currentSequence) * 30 : 0);
                    newDog.NPC.life = newDog.NPC.lifeMax;
                    newDog.isInitialized = true;

                    // 【关键】强制网络同步
                    // 告诉所有客户端：这里有个NPC，并且它的自定义数据（ModNPC数据）变了
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, index); // 同步基础NPC
                        newDog.NPC.netUpdate = true; // 触发 ModNPC.SendExtraAI 同步自定义数据
                    }
                }
            }

            // --- 客户端视觉反馈 ---
            if (player.whoAmI == Main.myPlayer)
            {
                Main.NewText($"{this.DogName} 在血肉与白骨的献祭中重生了！", 200, 50, 255);
                SoundEngine.PlaySound(SoundID.Item29, player.position); // 复活音效
            }

            return true;
        }

        // --- 信息显示 ---
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            NPC liveDog = GetActiveDog(Main.LocalPlayer);
            DogNPC dogScript = liveDog?.ModNPC as DogNPC;

            // 优先显示活狗数据，否则显示物品存档数据
            string displayName = (dogScript != null) ? dogScript.MyName : this.DogName;
            int pathway = (dogScript != null) ? dogScript.currentPathway : this.DogPathway;
            int sequence = (dogScript != null) ? dogScript.currentSequence : this.DogSequence;

            string status = (dogScript != null)
                ? "[c/00FF00:存活 (点击传送)]"
                : "[c/FF0000:死亡 (需100骨头+100腐肉复活)]";

            string pathwayName = "凡狗";
            switch (pathway) { case 1: pathwayName = "巨人"; break; case 2: pathwayName = "猎人"; break; case 3: pathwayName = "月亮"; break; case 4: pathwayName = "愚者"; break; case 5: pathwayName = "错误"; break; case 6: pathwayName = "太阳"; break; }
            string seqText = sequence < 10 ? $"[{pathwayName} 序列{sequence}]" : "(未开启途径)";

            tooltips.Add(new TooltipLine(Mod, "DogStatus", $"状态: {status}"));
            tooltips.Add(new TooltipLine(Mod, "DogInfo", $"[c/FFA500:{displayName}] {seqText}"));
            tooltips.Add(new TooltipLine(Mod, "Lore", "据说这是霍尔伯爵买猎狗时的附赠品。") { OverrideColor = Color.Gray });
        }

        private string GetDogNameDisplay()
        {
            NPC liveDog = GetActiveDog(Main.LocalPlayer);
            if (liveDog != null && liveDog.ModNPC is DogNPC script) return script.MyName;
            return DogName;
        }

        private NPC GetActiveDog(Player player)
        {
            foreach (var n in Main.npc)
            {
                if (n.active && n.type == ModContent.NPCType<DogNPC>() && n.ModNPC is DogNPC dog)
                {
                    if (dog.OwnerName == player.name) return n;
                }
            }
            return null;
        }
        private bool IsDogActive(Player player) => GetActiveDog(player) != null;

        // --- 存档读写 (保证退出游戏名字不丢) ---
        public override void SaveData(TagCompound tag)
        {
            tag["DogName"] = DogName; // 保存名字
            tag["DogPathway"] = DogPathway;
            tag["DogSequence"] = DogSequence;
            tag["BonusMaxLife"] = BonusMaxLife;
            if (Equipment != null)
            {
                for (int i = 0; i < Equipment.Length; i++)
                {
                    if (Equipment[i] == null) Equipment[i] = new Item();
                    tag[$"Equip_{i}"] = ItemIO.Save(Equipment[i]);
                }
            }
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.ContainsKey("DogName")) DogName = tag.GetString("DogName");
            if (tag.ContainsKey("DogPathway")) DogPathway = tag.GetInt("DogPathway");
            if (tag.ContainsKey("DogSequence")) DogSequence = tag.GetInt("DogSequence");
            if (tag.ContainsKey("BonusMaxLife")) BonusMaxLife = tag.GetInt("BonusMaxLife");
            Equipment = new Item[3];
            for (int i = 0; i < 3; i++)
            {
                if (tag.ContainsKey($"Equip_{i}")) Equipment[i] = ItemIO.Load(tag.GetCompound($"Equip_{i}"));
                else { Equipment[i] = new Item(); Equipment[i].SetDefaults(0); }
            }
        }
    }
}