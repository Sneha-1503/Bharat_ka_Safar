using System;
using System.Collections.Generic;
using UnityEngine;

namespace BharatKaSafar.Challenges
{
    public enum BKS_ChallengePeriod { Daily, Weekly, Overall }

    [Serializable]
    public class BKS_ChallengeDef
    {
        public string id;
        public string title;
        public BKS_Stat stat;
        [Min(1)] public int target = 1;
        [Min(0)] public int rewardCoins = 100;
        [Min(0)] public int rewardBoosts = 0;
    }

    /// <summary>
    /// All challenges with targets and rewards (Resources/BKS_ChallengeCatalog — edit in the Inspector).
    /// Daily and Weekly pick a few from their pool each day/week; Overall challenges are permanent.
    /// </summary>
    [CreateAssetMenu(menuName = "Bharat Ka Safar/Challenge Catalog", fileName = "BKS_ChallengeCatalog")]
    public class BKS_ChallengeCatalog : ScriptableObject
    {
        [Min(1)] public int dailyCount = 3;
        [Min(1)] public int weeklyCount = 3;

        public List<BKS_ChallengeDef> dailyPool = new List<BKS_ChallengeDef>
        {
            new BKS_ChallengeDef { id = "d_coins300",  title = "Collect 300 coins while driving", stat = BKS_Stat.CoinsCollected, target = 300, rewardCoins = 100 },
            new BKS_ChallengeDef { id = "d_coins500",  title = "Collect 500 coins while driving", stat = BKS_Stat.CoinsCollected, target = 500, rewardCoins = 150 },
            new BKS_ChallengeDef { id = "d_missions2", title = "Complete 2 missions",             stat = BKS_Stat.MissionsCompleted, target = 2, rewardCoins = 150 },
            new BKS_ChallengeDef { id = "d_missions3", title = "Complete 3 missions",             stat = BKS_Stat.MissionsCompleted, target = 3, rewardCoins = 200 },
            new BKS_ChallengeDef { id = "d_horn3",     title = "Clear 3 animals with the horn",   stat = BKS_Stat.AnimalsHorned, target = 3, rewardCoins = 150 },
            new BKS_ChallengeDef { id = "d_items10",   title = "Pick up 10 mission items",        stat = BKS_Stat.CollectiblesPicked, target = 10, rewardCoins = 150 },
            new BKS_ChallengeDef { id = "d_3star1",    title = "Get 3 stars in a mission",        stat = BKS_Stat.ThreeStarMissions, target = 1, rewardCoins = 200 },
            new BKS_ChallengeDef { id = "d_boost1",    title = "Use a 2X boost",                  stat = BKS_Stat.BoostsUsed, target = 1, rewardCoins = 100 },
        };

        public List<BKS_ChallengeDef> weeklyPool = new List<BKS_ChallengeDef>
        {
            new BKS_ChallengeDef { id = "w_missions10", title = "Complete 10 missions",              stat = BKS_Stat.MissionsCompleted, target = 10, rewardCoins = 1000 },
            new BKS_ChallengeDef { id = "w_3star5",     title = "Get 3 stars in 5 missions",         stat = BKS_Stat.ThreeStarMissions, target = 5, rewardCoins = 0, rewardBoosts = 2 },
            new BKS_ChallengeDef { id = "w_items50",    title = "Pick up 50 mission items",          stat = BKS_Stat.CollectiblesPicked, target = 50, rewardCoins = 800 },
            new BKS_ChallengeDef { id = "w_coins3000",  title = "Collect 3,000 coins while driving", stat = BKS_Stat.CoinsCollected, target = 3000, rewardCoins = 0, rewardBoosts = 3 },
            new BKS_ChallengeDef { id = "w_horn15",     title = "Clear 15 animals with the horn",    stat = BKS_Stat.AnimalsHorned, target = 15, rewardCoins = 0, rewardBoosts = 1 },
            new BKS_ChallengeDef { id = "w_upgrade2",   title = "Buy 2 upgrades",                    stat = BKS_Stat.UpgradesBought, target = 2, rewardCoins = 600 },
        };

        public List<BKS_ChallengeDef> overall = new List<BKS_ChallengeDef>
        {
            new BKS_ChallengeDef { id = "o_chapter1",   title = "Complete your first chapter",  stat = BKS_Stat.ChaptersCompleted, target = 1, rewardCoins = 2000 },
            new BKS_ChallengeDef { id = "o_chapter5",   title = "Complete 5 chapters",          stat = BKS_Stat.ChaptersCompleted, target = 5, rewardCoins = 10000, rewardBoosts = 5 },
            new BKS_ChallengeDef { id = "o_missions25", title = "Complete 25 missions",         stat = BKS_Stat.MissionsCompleted, target = 25, rewardCoins = 3000 },
            new BKS_ChallengeDef { id = "o_missions50", title = "Complete 50 missions",         stat = BKS_Stat.MissionsCompleted, target = 50, rewardCoins = 6000 },
            new BKS_ChallengeDef { id = "o_missions100",title = "Complete 100 missions",        stat = BKS_Stat.MissionsCompleted, target = 100, rewardCoins = 15000, rewardBoosts = 10 },
            new BKS_ChallengeDef { id = "o_vehicles5",  title = "Buy 5 vehicles",               stat = BKS_Stat.VehiclesBought, target = 5, rewardCoins = 5000 },
            new BKS_ChallengeDef { id = "o_earn100k",   title = "Earn 1,00,000 coins",          stat = BKS_Stat.CoinsEarned, target = 100000, rewardCoins = 10000 },
            new BKS_ChallengeDef { id = "o_3star50",    title = "Get 3 stars in 50 missions",   stat = BKS_Stat.ThreeStarMissions, target = 50, rewardCoins = 8000, rewardBoosts = 5 },
        };

        private static BKS_ChallengeCatalog cached;
        public static BKS_ChallengeCatalog Load()
        {
            if (cached == null) cached = Resources.Load<BKS_ChallengeCatalog>("BKS_ChallengeCatalog");
            if (cached == null) cached = CreateInstance<BKS_ChallengeCatalog>(); // defaults above
            return cached;
        }
    }
}
