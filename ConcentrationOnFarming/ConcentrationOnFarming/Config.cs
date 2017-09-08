using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConcentrationOnFarming
{
    public class Config
    {
        public bool Enabled { get; set; } = true;
        public bool CheckUpdate { get; set; } = true;
        public bool InfiniteStamina { get; set; } = true;
        public bool AutokillEnemies { get; set; } = false;
        public bool SkipFishingMinigame { get; set; } = false;
        public int PercentageTreasureHunt { get; set; } = 20;
        public bool AutoSave { get; set; } = false;
        public uint AutoSaveInterval { get; set; } = 6000;
        public bool InfiniteWateringCan { get; set; } = true;
        public bool InstantCatchFish { get; set; } = false;
        public bool NoGarbageFishing { get; set; } = false;
        public bool AutoWateringCrops { get; set; } = false;
        public bool InstantGrowTree { get; set; } = false;
        public bool FastMachineProcessing { get; set; } = false;
        public bool UseToolsNearby { get; set; } = false;
        public bool MagnetDropItems { get; set; } = false;
    }
}
