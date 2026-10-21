using UnityEngine;

namespace BharatKaSafar.Challenges
{
    /// <summary>What challenges can count.</summary>
    public enum BKS_Stat
    {
        CoinsCollected,      // coins picked up while driving (2X counted)
        CoinsEarned,         // all coins added to the wallet (pickups + mission rewards)
        MissionsCompleted,
        ThreeStarMissions,
        ChaptersCompleted,
        CollectiblesPicked,  // Diya, Gem … mission items
        AnimalsHorned,       // horn obstacles cleared
        VehiclesBought,
        UpgradesBought,
        BoostsUsed,
        RevivesUsed,
        DistanceDriven,      // metres driven forward (each metre once per run)
        SafarStarsEarned,    // Safar Stars from handling obstacles well
        MemoryTokens,        // Safar Diary memory tokens collected (mission finishes)
    }

    /// <summary>
    /// Lifetime counters for challenges, saved in PlayerPrefs. Gameplay code calls Add();
    /// daily/weekly progress is the lifetime value minus a snapshot taken when the period started.
    /// </summary>
    public static class BKS_ChallengeStats
    {
        private const string Prefix = "BKS_Stat_";
        public static event System.Action<BKS_Stat> OnChanged;

        public static int Get(BKS_Stat stat) => PlayerPrefs.GetInt(Prefix + stat, 0);

        public static void Add(BKS_Stat stat, int amount = 1)
        {
            if (amount <= 0) return;
            BKS_ChallengeService.EnsureBaselines(); // a new day/week starts from the value before this add
            long v = (long)Get(stat) + amount;
            PlayerPrefs.SetInt(Prefix + stat, (int)System.Math.Min(v, int.MaxValue));
            OnChanged?.Invoke(stat);
        }
    }
}
