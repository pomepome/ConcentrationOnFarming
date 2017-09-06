using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Minigames;
using StardewValley.Monsters;
using StardewValley.SDKs;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

using static ConcentrationOnFarming.Util;

namespace ConcentrationOnFarming
{
    using SVObject = StardewValley.Object;
    using Player = StardewValley.Farmer;

    public class ModEntry : Mod
    {
        public static readonly string VERSION = "1.0.2";

        Config config = null;

        private uint tickCount;

        private static bool isSpecialKeyPerformed;

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
            
            if(config.CheckUpdate)
            {
                UpdateChecker.CheckUpdate(Monitor);
            }

            GameEvents.UpdateTick += onGameTick;
            GameEvents.SecondUpdateTick += OnUpdate;

            ControlEvents.KeyPressed += OnKeyPressed;
        }

        private void onGameTick(object sender, EventArgs args)
        {
            if(!Context.IsWorldReady || !config.Enabled)
            {
                return;
            }
            Farm farm = Game1.getFarm();
            if(farm == null)
            {
                return;
            }

            Player player = Game1.player;

            Dictionary<Vector2, TerrainFeature> features = player.currentLocation.terrainFeatures;
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
                if(player.CurrentTool is FishingRod)
                {
                    FishingRod rod = (FishingRod)player.CurrentTool;
                    IClickableMenu clickableManu = Game1.activeClickableMenu;
                    if (clickableManu is BobberBar)
                    {
                        BobberBar bar = (BobberBar)clickableManu;

                        if ((float)GetPrivateValue(bar, "scale") <= 0f)
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
                if(CurrentTool is WateringCan)
                {
                    WateringCan wc = (WateringCan)CurrentTool;
                    wc.WaterLeft = wc.waterCanMax;
                }
            }
            if(config.InstantCatchFish)
            {
                if(CurrentTool is FishingRod)
                {
                    FishingRod rod = (FishingRod)CurrentTool;
                    if(rod.isFishing && rod.timeUntilFishingBite > 0)
                    {
                        rod.timeUntilFishingBite = 0;
                    }
                }
            }
            if(config.NoGarbageFishing)
            {
                if (CurrentTool is FishingRod)
                {
                    FishingRod rod = (FishingRod)CurrentTool;

                    int whichFish = (int)GetPrivateValue(rod, "whichFish");

                    if(!IsFish(whichFish))
                    {
                        SetPrivateValue(rod, "whichFish", GetNextFish(player.currentLocation, rod));
                        Vector2 bobber = (Vector2)GetPrivateValue(rod, "bobber");
                        Game1.screenOverlayTempSprites.Clear();
                        Game1.screenOverlayTempSprites.Add(new TemporaryAnimatedSprite(Game1.mouseCursors, new Rectangle(612, 1913, 74, 30), 1500f, 1, 0, Game1.GlobalToLocal(Game1.viewport, bobber + new Vector2(-140f, (float)(-(float)Game1.tileSize * 5 / 2))), false, false, 1f, 0.005f, Color.White, 4f, 0.075f, 0f, 0f, true)
                        {
                            scaleChangeChange = -0.005f,
                            motion = new Vector2(0f, -0.1f),
                            endFunction = new TemporaryAnimatedSprite.endBehavior(rod.startMinigameEndFunction),
                            extraInfoForEndBehavior = (int)GetPrivateValue(rod, "whichFish")
                        });
                    }
                }
            }
            if(config.AutoWateringCrops)
            {
                foreach(Vector2 loc in features.Keys)
                {
                    TerrainFeature feature = features[loc];
                    if(feature is HoeDirt)
                    {
                        ((HoeDirt)feature).state = 1;
                    }
                }
            }
            if (config.InstantGrowTree)
            {
                foreach (Vector2 loc in features.Keys)
                {
                    TerrainFeature feature = features[loc];
                    if (feature is Tree)
                    {
                        Tree tree = (Tree)feature;
                        if (tree.growthStage < 5)
                        {
                            tree.growthStage = 5;
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
        }

        private void OnUpdate(object sender, EventArgs args)
        {
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
                    Game1.player.addItemToInventory(new SVObject(SVObject.prismaticShardIndex,100));
                    Monitor.Log("Special Item Added to your Inventory!", LogLevel.Alert);
                }
            }
        }

        private void OnKeyPressed(object sender, EventArgsKeyPressed e)
        {
            Player player = Game1.player;
            if (e.KeyPressed == Keys.C)
            {
                if(!config.CutNearbyWood)
                {
                    return;
                }
                GameLocation location = player.currentLocation;
                Dictionary<Vector2, TerrainFeature> features = location.terrainFeatures;
                foreach(Vector2 loc in features.Keys)
                {
                    TerrainFeature feature = features[loc];

                    if(feature == null || !(feature is Tree))
                    {
                        continue;
                    }

                    Rectangle bb = ExpandBoundingBox(player.GetBoundingBox(), 1000, 1000);

                    if(feature.getBoundingBox(loc).Intersects(bb) && NullCheck(player.CurrentTool, loc, location))
                    {
                        feature.performToolAction(player.CurrentTool, 0, loc, location);
                    }
                }
            }
            else if(e.KeyPressed == Keys.PageDown)
            {
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
            int clearWaterDistance = (int)Util.GetPrivateValue(rod, "clearWaterDistance");
            Player lastUser = (Player)Util.GetPrivateValue(rod, "lastUser");
            Vector2 bobber = (Vector2)GetPrivateValue(rod, "bobber");
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
        public static bool NullCheck(params object[] objs)
        {
            foreach(object obj in objs)
            {
                if(obj == null)
                {
                    return false;
                }
            }
            return true;
        }

        public static void SetPrivateValue(object obj, string name, object value)
        {
            Type type = obj.GetType();
            type.GetField(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);
        }
        public static object GetPrivateValue(object obj, string name)
        {
            Type type = obj.GetType();
            return type.GetField(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
        }

        public static void KillEnemies(Player player, ModEntry main)
        {
            if(player == null)
            {
                return;
            }

            GameLocation location = player.currentLocation;

            for (int i = location.characters.Count - 1; i >= 0; i--)
            {
                NPC npc = location.characters[i];
                if (npc is Monster)
                {
                    Monster mon = npc as Monster;
                    if (mon.isInvincible())
                    {
                        mon.setInvincibleCountdown(0);
                    }
                    if (mon.missChance > 0)
                    {
                        mon.missChance = 0;
                    }
                    if (mon is RockCrab)
                    {
                        RockCrab rc = mon as RockCrab;
                        if (!(bool)GetPrivateValue(rc, "shellGone"))
                        {
                            SetPrivateValue(rc, "shellGone", true);
                        }
                    }

                    Rectangle bb = mon.GetBoundingBox();

                    int damageDone = mon.takeDamage(mon.maxHealth, 0, 0, false, 0);

                    main.Monitor.Log(string.Format("Slayed Monster:\"{0}\" @[{1},{2}]", mon.name, mon.position.X, mon.position.Y));

                    if (!location.isFarm)
                    {
                        player.checkForQuestComplete(null, 1, 1, null, mon.name, 4, -1);
                    }
                    if(player.leftRing != null)
                    {
                        player.leftRing.onMonsterSlay(mon);
                    }
                    if(player.rightRing != null)
                    {
                        player.rightRing.onMonsterSlay(mon);
                    }
                    if (player.IsMainPlayer && !location.isFarm && (!(mon is GreenSlime) || (mon as GreenSlime).firstGeneration))
                    {
                        Game1.stats.monsterKilled(mon.name);
                    }

                    location.monsterDrop(mon, bb.Center.X, bb.Center.Y);

                    if(!location.isFarm)
                    {
                        player.gainExperience(4, mon.experienceGained);
                    }
                    location.characters.RemoveAt(i);
                    Stats expr_6F8 = Game1.stats;
                    uint monstersKilled = expr_6F8.MonstersKilled;
                    expr_6F8.MonstersKilled = monstersKilled + 1u;

                }
            }
        }
    }
}
