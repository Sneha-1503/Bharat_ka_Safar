using System;
using UnityEngine;
using BharatKaSafar.Core;

namespace BharatKaSafar.Missions
{
    /// <summary>
    /// Counts what the 3-star rules need during one run: mission collectibles picked up,
    /// obstacle hits and driving time. Created automatically by MissionManager.
    /// Obstacles call ReportHit(); BKS_MissionCollectible calls ReportCollectible().
    /// </summary>
    public class BKS_MissionObjectiveTracker : MonoBehaviour
    {
        public static BKS_MissionObjectiveTracker Instance { get; private set; }

        public event Action OnChanged;

        public MissionData Mission { get; private set; }
        public int Collected { get; private set; }
        public int Hits { get; private set; }
        public float Elapsed { get; private set; }
        public bool Running { get; private set; }

        [Tooltip("Ignore further hits for this long after one (a car scraping a rock counts once)")]
        [SerializeField, Min(0f)] private float hitCooldown = 1f;
        private float lastHitTime = -10f;

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void Begin(MissionData mission)
        {
            Mission = mission;
            Collected = Hits = 0;
            Elapsed = 0f;
            Running = true;
            OnChanged?.Invoke();
        }

        public void Stop() => Running = false;

        private void Update()
        {
            if (!Running) return;
            Elapsed += Time.deltaTime;
        }

        public static void ReportCollectible()
        {
            var t = Instance;
            if (t == null || !t.Running) return;
            t.Collected++;
            Challenges.BKS_ChallengeStats.Add(Challenges.BKS_Stat.CollectiblesPicked);
            t.OnChanged?.Invoke();
        }

        public static void ReportHit()
        {
            var t = Instance;
            if (t == null || !t.Running || Time.time - t.lastHitTime < t.hitCooldown) return;
            t.lastHitTime = Time.time;
            t.Hits++;
            t.OnChanged?.Invoke();
        }

        /// <summary>2nd-star objective: all collectibles and within the hit limit.</summary>
        public bool ObjectiveMet
        {
            get
            {
                if (Mission == null) return false;
                bool items = Mission.CollectibleCount <= 0 || Collected >= Mission.CollectibleCount;
                int limit = Mission.HitLimit;
                bool hits = limit < 0 || Hits <= limit;
                return items && hits;
            }
        }

        /// <summary>3rd-star bonus.</summary>
        public bool BonusMet
        {
            get
            {
                if (Mission == null) return false;
                switch (Mission.Bonus)
                {
                    case BKS_BonusType.NoHits: return Hits == 0;
                    case BKS_BonusType.TimeLimit: return Elapsed <= Mission.BonusTimeSeconds;
                    case BKS_BonusType.NoRevive:
                        var revive = Gameplay.BKS_ReviveManager.Instance;
                        return revive == null || revive.RevivesUsed == 0;
                    case BKS_BonusType.AllSafarStars:
                        var stars = FindFirstObjectByType<BKS_SafarStarTracker>();
                        return stars != null && stars.CollectionRatio >= 1f;
                    default: return true;
                }
            }
        }

        public int ComputeStars(bool completed)
        {
            if (!completed) return 0;
            if (!ObjectiveMet) return 1;
            return BonusMet ? 3 : 2;
        }
    }
}
