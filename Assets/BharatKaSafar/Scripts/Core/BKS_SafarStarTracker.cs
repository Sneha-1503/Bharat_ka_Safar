using System;
using UnityEngine;

namespace BharatKaSafar.Core
{
    /// <summary>
    /// Tracks Safar Stars collected during a single mission session.
    /// Reset when a new mission begins. Does NOT persist across sessions —
    /// that is handled by BKS_SaveManager.AddSafarStars().
    ///
    /// Architecture: This is a lightweight session tracker. Other systems
    /// (horn obstacles, checkpoints) call CollectStar(). BKS_MissionResult
    /// reads CollectedCount and TotalCount to compute star rating.
    /// </summary>
    public class BKS_SafarStarTracker : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fires whenever a star is collected. Args: (current, total).</summary>
        public event Action<int, int> OnStarCollected;

        /// <summary>Fires when all stars have been collected.</summary>
        public event Action OnAllStarsCollected;

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Mission Config")]
        [SerializeField] private int totalStarsInMission = 10;

        // ── Runtime ───────────────────────────────────────────────────────────────
        public int CollectedCount { get; private set; }
        public int CollectedStars => CollectedCount;
        public int TotalCount     => totalStarsInMission;

        /// <summary>0–1 normalized ratio of collected stars.</summary>
        public float CollectionRatio => totalStarsInMission > 0
            ? (float)CollectedCount / totalStarsInMission
            : 0f;

        // ── API ───────────────────────────────────────────────────────────────────

        /// <summary>Set the total stars available in this mission (call before mission starts).</summary>
        public void SetTotal(int total)
        {
            totalStarsInMission = Mathf.Max(0, total);
            OnStarCollected?.Invoke(CollectedCount, totalStarsInMission); // lets the HUD show "0/3" right away
        }

        /// <summary>Award one Safar Star. Safe to call multiple times; won't exceed total.</summary>
        public void CollectStar()
        {
            if (CollectedCount >= totalStarsInMission)
            {
                Debug.LogWarning("[BKS_SafarStarTracker] Tried to collect more stars than total. Ignoring.");
                return;
            }

            CollectedCount++;
            Challenges.BKS_ChallengeStats.Add(Challenges.BKS_Stat.SafarStarsEarned);
            Debug.Log($"[BKS_SafarStarTracker] Star collected! {CollectedCount}/{totalStarsInMission}");
            OnStarCollected?.Invoke(CollectedCount, totalStarsInMission);

            if (CollectedCount >= totalStarsInMission)
                OnAllStarsCollected?.Invoke();
        }

        /// <summary>Reset for a new mission session.</summary>
        public void Reset()
        {
            CollectedCount = 0;
        }
    }
}
