using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Minigames;
using StardewValley.Monsters;
using StardewValley.SDKs;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using xTile.Layers;
using xTile.ObjectModel;
using xTile.Tiles;
using static ConcentrationOnFarming.Util;

namespace ConcentrationOnFarming
{
    using SVObject = StardewValley.Object;
    using Player = StardewValley.Farmer;

    public class ModEntry : Mod
    {
        private static string version;

        public static string VERSION
        {
            get { return version; }
        }

        private static Mod instance;
        public static Mod INSTANCE
        {
            get { return instance; }
        }

        Config config = null;

        private bool isFirstTick = true;
        private uint tickCount;

        private static bool isSpecialKeyPerformed;

        public ModEntry()
        {
            instance = this;
        }

        public override void Entry(IModHelper helper)
        {
            config = helper.ReadConfig<Config>();

            if(config.PercentageTreasureHunt < 0)
            {
                config.PercentageTreasureHunt = 0;
            }
            else if(config.PercentageTreasureHunt > 50)
            {
                config.PercentageTreasureHunt = 50;
            }
            helper.WriteConfig(config);
            
            if(!config.Enabled)
            {
                return;
            }

            {
                //Creates version info using manifest.json
                IManifest manifest = helper.ModRegistry.Get(helper.ModRegistry.ModID);
                ISemanticVersion versionInfo = manifest.Version;
                version = string.Format("{0}.{1}.{2}", versionInfo.MajorVersion, versionInfo.MinorVersion, versionInfo.PatchVersion);
            }

            if(config.CheckUpdate)
            {
                UpdateChecker.CheckUpdate(Monitor);
            }

            GameEvents.UpdateTick += OnGameTick;
            GameEvents.SecondUpdateTick += OnUpdate;

            ControlEvents.KeyPressed += OnKeyPressed;
        }

        private void OnGameTick(object sender, EventArgs args)
        {
            if(!Context.IsWorldReady || !config.Enabled)
            {
                return;
            }

            if(isFirstTick)
            {
                isFirstTick = false;
                OutputNames(Helper.DirectoryPath);
            }

            Player player = Game1.player;
            
            Dictionary <Vector2, TerrainFeature> features = player.currentLocation.terrainFeatures;
            Tool CurrentTool = player.CurrentTool;

            IMinigame minigame = Game1.currentMinigame;

            if (config.InfiniteStamina)
            {
                player.Stamina = player.MaxStamina;
            }
            if (config.AutokillEnemies)
            {
                KillEnemies(player, this);
            }
            if(config.AutoSave)
            {
                tickCount = (tickCount + 1) % config.AutoSaveInterval;
                if(tickCount == 0)
                {
                    SaveGame.Save();
                    Monitor.Log("Auto saved!");
                }
            }
            if(config.SkipFishingMinigame)
            {
                if (player.CurrentTool is FishingRod rod)
                {
                    IClickableMenu clickableManu = Game1.activeClickableMenu;
                    if (clickableManu is BobberBar bar)
                    {
                        if (GetPrivateValue<float>(bar, "scale") <= 0f)
                        {
                            int random = new Random().Next() % 100;

                            Monitor.Log("Random value:" + random);

                            if (random < config.PercentageTreasureHunt)
                            {
                                //Treasure caught!
                                Monitor.Log("Treasure Caught!");
                                SetPrivateValue(bar, "treasureCaught", true);
                            }

                        }
                        else
                        {
                            SetPrivateValue(bar, "fadeOut", true);
                            SetPrivateValue(bar, "perfect", true);
                            SetPrivateValue(bar, "distanceFromCatching", 10);
                        }
                    }
                }
            }
            if(config.InfiniteWateringCan)
            {
                if (CurrentTool is WateringCan wc)
                {
                    wc.WaterLeft = wc.waterCanMax;
                }
            }
            if(config.InstantCatchFish)
            {
                if (CurrentTool is FishingRod rod)
                {
                    if (rod.isFishing && rod.timeUntilFishingBite > 0)
                    {
                        rod.timeUntilFishingBite = 0;
                    }
                }
            }
            if(config.NoGarbageFishing)
            {
                if (CurrentTool is FishingRod rod)
                {
                    int whichFish = GetPrivateValue<int>(rod, "whichFish");

                    if (!IsFish(whichFish))
                    {
                        SetPrivateValue(rod, "whichFish", GetNextFish(player.currentLocation, rod));
                        Vector2 bobber = GetPrivateValue<Vector2>(rod, "bobber");
                        Game1.screenOverlayTempSprites.Clear();
                        Game1.screenOverlayTempSprites.Add(new TemporaryAnimatedSprite(Game1.mouseCursors, new Rectangle(612, 1913, 74, 30), 1500f, 1, 0, Game1.GlobalToLocal(Game1.viewport, bobber + new Vector2(-140f, (float)(-(float)Game1.tileSize * 5 / 2))), false, false, 1f, 0.005f, Color.White, 4f, 0.075f, 0f, 0f, true)
                        {
                            scaleChangeChange = -0.005f,
                            motion = new Vector2(0f, -0.1f),
                            endFunction = new TemporaryAnimatedSprite.endBehavior(rod.startMinigameEndFunction),
                            extraInfoForEndBehavior = GetPrivateValue<int>(rod, "whichFish")
                        });
                    }
                }
            }
            if(config.AutoWateringCrops)
            {
                foreach(Vector2 loc in features.Keys)
                {
                    if (features[loc] is HoeDirt feature)
                    {
                        feature.state = 1;
                    }
                }
            }
            if (config.InstantGrowTree)
            {
                foreach (Vector2 loc in features.Keys)
                {
                    if (features[loc] is Tree tree)
                    {
                        if (tree.growthStage < 5)
                        {
                            tree.growthStage = 5;
                            tree.hasSeed = true;
                        }
                    }
                    else if(features[loc] is FruitTree fTree)
                    {
                        if(fTree.growthStage < 4)
                        {
                            fTree.growthStage = 4;
                            fTree.dayUpdate(player.currentLocation, loc);
                        }
                    }
                }
            }
            if(config.FastMachineProcessing)
            {
                foreach (GameLocation location in Game1.locations)
                {
                    Dictionary<Vector2, SVObject> objects = location.Objects;
                    foreach (Vector2 loc in objects.Keys)
                    {
                        SVObject obj = objects[loc];
                        obj.minutesElapsed(1000, player.currentLocation);
                    }
                }
            }
            if(config.MagnetDropItems)
            {
                foreach(Debris debri in player.currentLocation.debris)
                {
                    MagnetDebris(debri, player);
                }
            }
        }

        private void OnUpdate(object sender, EventArgs args)
        {
            if(!Context.IsWorldReady)
            {
                return;
            }
            Player player = Game1.player;
            GameLocation location = player.currentLocation;
            if(!player.IsMainPlayer)
            {
                return;
            }
            if (isSpecialKeyPerformed)
            {
                if (IsAllKeyUp(Keys.P, Keys.U, Keys.N, Keys.Y, Keys.O))
                {
                    isSpecialKeyPerformed = false;
                }
            }
            else
            {
                if (IsAllKeyDown(Keys.P, Keys.U, Keys.N, Keys.Y, Keys.O))
                {
                    isSpecialKeyPerformed = true;
                    Game1.player.addItemToInventory(new SVObject(74, 100));
                    Game1.player.addItemToInventory(new SVObject(394, 100));
                    Monitor.Log("Special Item Added to your Inventory!", LogLevel.Alert);
                }
            }
        }

        

        private void OnKeyPressed(object sender, EventArgsKeyPressed e)
        {
            Player player = Game1.player;
            if (e.KeyPressed == Keys.C)
            {
                if(!config.UseToolsNearby || player.CurrentTool == null)
                {
                    return;
                }

                Rectangle bb = ExpandBoundingBox(player.GetBoundingBox(), 1000, 1000);

                GameLocation location = player.currentLocation;
                Dictionary<Vector2, TerrainFeature> features = location.terrainFeatures;

                if (player.CurrentTool is Axe)
                {
                    foreach (Vector2 loc in features.Keys)
                    {
                        TerrainFeature feature = features[loc];
                        if (feature.getBoundingBox(loc).Intersects(bb) && feature is Tree tree)
                        {
                            tree.health = 0;
                            tree.performToolAction(player.CurrentTool, 0, loc, location);
                        }
                    }

                    Dictionary<Vector2, SVObject> objects = new Dictionary<Vector2, SVObject>(location.Objects);
                    foreach (Vector2 loc in objects.Keys)
                    {
                        SVObject obj = objects[loc];
                        if (obj != null && loc != null && obj.getBoundingBox(loc) != null && obj.getBoundingBox(loc).Intersects(bb) && (obj.Name == "Twig" || obj.name == "Weeds"))
                        {
                            obj.performToolAction(player.CurrentTool);
                            location.removeObject(loc, false);
                            Monitor.Log(string.Format("removed tile: {0}", obj.name));
                        }
                    }

                    if (location is Farm farm)
                    {
                        BreakStumps(player, bb, farm.resourceClumps);
                    }
                    else if (location is Woods woods)
                    {
                        BreakStumps(player, bb, woods.stumps);
                    }
                    else if (location is Forest forest)
                    {
                        if (BreakStump(player, bb, forest.log))
                        {
                            forest.log = null;
                        }
                    }
                }
                else if (player.CurrentTool is Pickaxe)
                {
                    int playerX = player.getStandingX() / Game1.tileSize, playerY = player.getStandingY() / Game1.tileSize; 
                    Dictionary<Vector2, SVObject> objects = new Dictionary<Vector2, SVObject>(location.Objects);
                    foreach (Vector2 loc in objects.Keys)
                    {
                        int num = (int)loc.X / Game1.tileSize;
                        int num2 = (int)loc.Y / Game1.tileSize;

                        SVObject obj = objects[loc];
                        if (obj != null && loc != null && obj.getBoundingBox(loc) != null && obj.getBoundingBox(loc).Intersects(bb) && (obj.name == "Stone"))
                        {
                            location.Objects.Remove(loc);
                            if (!(location is MineShaft))
                            {
                                Game1.createRadialDebris(Game1.currentLocation, 14, num, num2, Game1.random.Next(2, 5), false, -1, false, -1);
                                location.breakStone(obj.parentSheetIndex, num, num2, player, new Random());
                            }
                            else if(location is MineShaft shaft)
                            {
                                if (GetPrivateValue<int>(shaft, "stonesLeftOnThisLevel") == 0)
                                {
                                    return;
                                }
                                shaft.checkStoneForItems(obj.parentSheetIndex, num, num2, player);
                                int stonesLeft = GetPrivateValue<int>(shaft, "stonesLeftOnThisLevel");
                                Monitor.Log("Stones left is " + stonesLeft);
                                if (GetPrivateValue<int>(shaft, "stonesLeftOnThisLevel") == 0)
                                {
                                    Monitor.Log("Creating ladder!");

                                    shaft.createLadderDown(playerX, playerY);
                                }
                            }
                        }
                    }
                    if (location is Farm farm)
                    {
                        BreakStumps(player, bb, farm.resourceClumps);
                    }
                    else if (location is Woods woods)
                    {
                        BreakStumps(player, bb, woods.stumps);
                    }
                    else if (location is MineShaft shaft)
                    {
                        BreakStumps(player, bb, shaft.resourceClumps);
                    }
                }
            }
            else if(e.KeyPressed == Keys.PageDown)
            {
                GameLocation location = player.currentLocation;
                if(location != null && location is MineShaft mineshaft)
                {
                    if(mineshaft.mineLevel == 120)
                    {
                        return;
                    }
                    Game1.enterMine(false, Game1.mine.mineLevel + 1, null);
                    Game1.playSound("stairsdown");
                }
            }

        }

        private Rectangle ExpandBoundingBox(Rectangle parent, int dx, int dy)
        {
            return new Rectangle(parent.X - dx, parent.Y - dy, parent.Width + 2 * dx, parent.Height + 2 * dy);
        }

        private bool IsKeyDown(Keys key)
        {
            return Keyboard.GetState().IsKeyDown(key);
        }

        private bool IsAllKeyDown(params Keys[] keys)
        {
            foreach(Keys key in keys)
            {
                if(!IsKeyDown(key))
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsAllKeyUp(params Keys[] keys)
        {
            foreach (Keys key in keys)
            {
                if (IsKeyDown(key))
                {
                    return false;
                }
            }
            return true;
        }

        private int GetNextFish(GameLocation location, FishingRod rod)
        {
            int clearWaterDistance = GetPrivateValue<int>(rod, "clearWaterDistance");
            Player lastUser = GetPrivateValue<Player>(rod, "lastUser");
            Vector2 bobber = GetPrivateValue<Vector2>(rod, "bobber");
            double num3 = (rod.attachments[0] != null) ? rod.attachments[0].Price / 10f : 0f;

            Rectangle rectangle2 = new Rectangle(location.fishSplashPoint.X * Game1.tileSize, location.fishSplashPoint.Y * Game1.tileSize, Game1.tileSize, Game1.tileSize);
            Rectangle value2 = new Rectangle((int)bobber.X - Game1.tileSize * 5 / 4, (int)bobber.Y - Game1.tileSize * 5 / 4, Game1.tileSize, Game1.tileSize);
            bool flag = rectangle2.Intersects(value2);

            SVObject @object = location.getFish(rod.fishingNibbleAccumulator, (rod.attachments[0] != null) ? rod.attachments[0].ParentSheetIndex : -1, clearWaterDistance + (flag ? 1 : 0), lastUser, num3 + (flag ? 0.4 : 0.0));

            if(!IsFish(@object.ParentSheetIndex))
            {
                return GetNextFish(location, rod);
            }
            return @object.ParentSheetIndex;
        }
        private bool IsFish(int which)
        {
            SVObject @object = new SVObject(which, 1);
            if (@object.Category == -20 || @object.ParentSheetIndex == 152 || @object.ParentSheetIndex == 153 || @object.parentSheetIndex == 157)
            {
                return false;
            }
            return true;
        }
    }

    internal class Util
    {
        private static Dictionary<string, FieldInfo> fieldsCached = new Dictionary<string, FieldInfo>();

        public static void OutputNames(string modPath)
        {
            string output = "";
            List<int> keyArray = ConvertKeys<int>(Game1.bigCraftablesInformation.Keys);
            keyArray.Sort((a, b) => a - b);
            foreach (int i in keyArray)
            {
                string[] array = Game1.bigCraftablesInformation[i].Split(new char[] { '/' });
                string name = array[0];
                if (!string.IsNullOrWhiteSpace(name))
                {
                    output += string.Format("ID:{0} is {1}\r\n", i, name);
                }
            }
            File.WriteAllText(Path.Combine(modPath, "Objects.txt"), output);

            output = "";
            keyArray = ConvertKeys<int>(Game1.objectInformation.Keys);
            keyArray.Sort((a,b)=> a - b);
            foreach (int i in keyArray)
            {
                string[] array = Game1.objectInformation[i].Split(new char[] { '/' });
                string name = array[0];
                if (!string.IsNullOrWhiteSpace(name))
                {
                    output += string.Format("ID:{0} is {1}\r\n", i, name);
                }
            }
            File.WriteAllText(Path.Combine(modPath, "Items.txt"), output);
        }

        public static void MagnetDebris(Debris debri, Player player)
        {
            player.magneticRadius = 1000;

            Rectangle bb = player.GetBoundingBox();

            foreach (Chunk chunk in debri.Chunks)
            {
               chunk.position = new Vector2(bb.Center.X - Game1.tileSize, bb.Center.Y - Game1.tileSize);
            }
        }

        public static bool CanBreakBigObject(Player player, Rectangle bb, ResourceClump clump)
        {
            int num = clump.parentSheetIndex;
            if(!clump.getBoundingBox(clump.tile).Intersects(bb) || player.CurrentTool == null)
            {
                return false;
            }
            Tool tool = player.CurrentTool;
            if(tool is Axe)
            {
                return num == 602 ? tool.upgradeLevel >= 2 : tool.upgradeLevel >= 1;
            }
            else if(tool is Pickaxe)
            {
                if(num != 622)
                {
                    if(num != 672)
                    {
                        switch (num)
                        {
                            case 752:
                            case 754:
                            case 756:
                            case 758:
                                return true;
                        }
                    }
                    else
                    {
                        return tool.upgradeLevel >= 2;
                    }
                }
                else
                {
                    return tool.upgradeLevel >= 3;
                }
            }
            return false;
        }

        public static bool BreakStump(Player player, Rectangle bb, ResourceClump clump)
        {
            if (CanBreakBigObject(player, bb, clump))
            {
                clump.health = 0;
                clump.performToolAction(player.CurrentTool, 1, clump.tile, player.currentLocation);
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void BreakStumps(Player player, Rectangle bb, List<ResourceClump> parentClumps)
        {
            for(int i = parentClumps.Count - 1;i >= 0;i--)
            {
                if(BreakStump(player, bb, parentClumps[i]))
                {
                    parentClumps.RemoveAt(i);
                }
            }
        }

        public static void SetPrivateValue(object obj, string name, object value)
        {
            GetField(obj, name).SetValue(obj, value);
        }
        public static T GetPrivateValue<T>(object obj, string name)
        {
            return (T)GetField(obj, name).GetValue(obj);
        }

        public static void RemoveNulls(IList source)
        {
            for(int i = source.Count - 1;i >= 0;i--)
            {
                if(source[i] == null)
                {
                    source.RemoveAt(i);
                }
            }
        }

        public static FieldInfo GetField(object obj, string name)
        {
            if(obj == null)
            {
                throw new ArgumentNullException("paramater obj cannot be null.");
            }
            Type type = obj.GetType();
            string key = type.FullName + "." + name;
            if(fieldsCached.ContainsKey(key))
            {
                return fieldsCached[key];
            }
            FieldInfo info = type.GetField(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            fieldsCached.Add(key, info);
            return info;
        }

        public static List<K> ConvertKeys<K>(ICollection collection)
        {
            List<K> list = new List<K>();

            foreach(object obj in collection)
            {
                if(obj is K key)
                {
                    list.Add(key);
                }
            }
            return list;
        }

        public static void KillEnemies(Player player, ModEntry main)
        {
            if (player == null)
            {
                return;
            }

            GameLocation location = player.currentLocation;

            for (int i = location.characters.Count - 1; i >= 0; i--)
            {
                NPC npc = location.characters[i];
                if (npc is Monster mon)
                {
                    if (mon.isInvincible())
                    {
                        mon.setInvincibleCountdown(0);
                    }
                    if (mon.missChance > 0)
                    {
                        mon.missChance = 0;
                    }

                    Rectangle bb = mon.GetBoundingBox();

                    int damageDone = mon.takeDamage(mon.maxHealth, 0, 0, false, 0);

                    main.Monitor.Log(string.Format("Slayed Monster:\"{0}\" @['{1}',{2},{3}]", mon.name, location.Name, mon.position.X, mon.position.Y));

                    if (!location.isFarm)
                    {
                        player.checkForQuestComplete(null, 1, 1, null, mon.name, 4, -1);
                    }
                    if (player.leftRing != null)
                    {
                        player.leftRing.onMonsterSlay(mon);
                    }
                    if (player.rightRing != null)
                    {
                        player.rightRing.onMonsterSlay(mon);
                    }
                    if (player.IsMainPlayer && !location.isFarm && (!(mon is GreenSlime) || (mon as GreenSlime).firstGeneration))
                    {
                        Game1.stats.monsterKilled(mon.name);
                    }

                    location.monsterDrop(mon, bb.Center.X, bb.Center.Y);

                    if (!location.isFarm)
                    {
                        player.gainExperience(4, mon.experienceGained);
                    }
                    location.characters.RemoveAt(i);
                    Stats expr_6F8 = Game1.stats;
                    uint monstersKilled = expr_6F8.MonstersKilled;
                    expr_6F8.MonstersKilled = monstersKilled + 1u;

                    main.Monitor.Log(string.Format("Kill Count of monster:'{0} is {1}'", mon.name, Game1.stats.getMonstersKilled(mon.name)));
                }
            }
        }
    }
}
