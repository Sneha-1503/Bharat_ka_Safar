using System;
using UnityEngine;
using BharatKaSafar.Core;

namespace BharatKaSafar.Missions
{
    /// <summary>
    /// Computes and stores the mission result after a session ends.
    /// Subscribes to MissionManager events to know when the mission is done.
    /// Reads BKS_SafarStarTracker and BKS_VehicleHealth for data.
    /// Calls BKS_SaveManager to persist results and unlock next mission.
    /// Fires OnResultReady for the result UI screen.
    ///
    /// Lifecycle:
    ///   MissionManager.OnMissionCompleted / OnMissionFailed
    ///     → BKS_MissionResult.HandleResult()
    ///     → Computes rating
    ///     → Saves via BKS_SaveManager
    ///     → Fires OnResultReady(ResultData)
    ///     → (optional) loads result scene after short delay
    /// </summary>
    public class BKS_MissionResult : MonoBehaviour
    {
        // ── Result Data ───────────────────────────────────────────────────────────
        public struct ResultData
        {
            public bool   MissionCompleted;
            public int    StarRating;        // 0 if failed, 1–3 if completed
            public int    CoinsEarned;
            public int    CoinsCollected;
            public int    SafarStarsEarned;
            public float  FinalHealthRatio;
            public float  StarCollectionRatio;
            public string MissionName;
            public string MissionID;
            public string NextMissionID;
            public string FailReason;         // "Crashed", "Fuel finished", "Vehicle destroyed" ...
            public bool   ChapterCompleted;   // true when this mission finished the whole chapter
            public string ChapterName;
            public int    ChapterMissionsDone;
            public int    ChapterMissionCount;
            // 3-star rules (planner missions)
            public bool   ObjectiveMet;
            public bool   BonusMet;
            public string ObjectiveText;
            public string BonusText;
        }

        // ── Events ────────────────────────────────────────────────────────────────
        public event Action<ResultData> OnResultReady;

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Scene Transition")]
        [Tooltip("Seconds to wait before auto-loading result scene (0 = don't auto-load)")]
        [SerializeField] private float autoLoadResultDelay = 1.5f;

        [Header("System References (Auto-detected if null)")]
        [SerializeField] private MissionManager      missionManager;
        [SerializeField] private BKS_SafarStarTracker starTracker;
        [SerializeField] private Vehicle.BKS_VehicleHealth vehicleHealth;

        // ── Runtime ───────────────────────────────────────────────────────────────
        public ResultData LastResult { get; private set; }
        private bool _resultHandled;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (missionManager != null)
            {
                missionManager.OnMissionCompleted += HandleCompleted;
                missionManager.OnMissionFailed    += HandleFailed;
            }
        }

        private void OnDisable()
        {
            if (missionManager != null)
            {
                missionManager.OnMissionCompleted -= HandleCompleted;
                missionManager.OnMissionFailed    -= HandleFailed;
            }
        }

        // ── Event Handlers ────────────────────────────────────────────────────────

        private void HandleCompleted(MissionData data)
        {
            if (_resultHandled) return;
            _resultHandled = true;
            ProcessResult(data, completed: true);
        }

        private void HandleFailed(MissionData data)
        {
            if (_resultHandled) return;
            _resultHandled = true;
            ProcessResult(data, completed: false);
        }

        // ── Core Result Logic ─────────────────────────────────────────────────────

        private void ProcessResult(MissionData data, bool completed)
        {
            if (data == null)
            {
                Debug.LogError("[BKS_MissionResult] MissionData is null, cannot process result.");
                return;
            }

            // Vehicle objects can spawn after this component's Awake.
            ResolveReferences();

            float starRatio   = starTracker   != null ? starTracker.CollectionRatio  : 0f;
            float healthRatio = !Vehicle.BKS_VehicleHealth.HealthSystemEnabled ? 1f
                : vehicleHealth != null ? vehicleHealth.HealthRatio : 0f;

            // Coins picked up during the run (BKS_CoinPickup → RewardManager) are kept even on failure.
            var rewards        = Rewards.BKS_RewardManager.Instance;
            int collectedCoins = rewards != null ? rewards.currentCoins : 0;

            var objectives   = BKS_MissionObjectiveTracker.Instance;
            bool plannerRules = data.UsesPlannerRating && objectives != null;
            int starRating   = !completed ? 0
                             : plannerRules ? objectives.ComputeStars(true)
                             : data.ComputeStarRating(starRatio, healthRatio);
            int coinsEarned  = (completed ? data.CoinReward : 0) + collectedCoins;
            int starsEarned  = completed && starTracker != null ? starTracker.CollectedCount : 0;

            var loader  = BKS_SceneLoader.Instance;
            var save    = BKS_SaveManager.Instance;
            ChapterData chapter = loader != null ? loader.ActiveChapter : null;
            bool inChapter = chapter != null && chapter.GetMissionByID(data.MissionID) != null;

            var result = new ResultData
            {
                MissionCompleted    = completed,
                StarRating          = starRating,
                CoinsEarned         = coinsEarned,
                CoinsCollected      = collectedCoins,
                SafarStarsEarned    = starsEarned,
                FinalHealthRatio    = healthRatio,
                StarCollectionRatio = starRatio,
                MissionName         = data.MissionName,
                MissionID           = data.MissionID,
                NextMissionID       = data.NextMissionID,
                ChapterName         = inChapter ? chapter.DisplayName : string.Empty,
                FailReason          = completed ? null : (Integration.BKS_GameplayBridge.LastFailReason ?? "Journey interrupted"),
                ObjectiveMet        = plannerRules && objectives.ObjectiveMet,
                BonusMet            = plannerRules && objectives.BonusMet,
                ObjectiveText       = data.ObjectiveRuleText,
                BonusText           = data.BonusRuleText
            };

            // Persist to save system
            if (save != null)
            {
                if (completed && inChapter)
                {
                    // Chapter-aware: unlocks the next mission by order, completes the chapter after the last one.
                    result.ChapterCompleted = save.CompleteMission(chapter, data, starRating, coinsEarned, starsEarned);
                }
                else if (completed)
                {
                    // Level started without a chapter (e.g. played directly in the Editor) — old behaviour.
                    save.SaveMissionResult(data.MissionID, starRating, coinsEarned, starsEarned, data.NextMissionID);
                }
                else
                {
                    save.AddCoins(coinsEarned); // failed run: keep collected coins only
                }

                if (inChapter)
                {
                    result.ChapterMissionsDone = save.GetCompletedMissionCount(chapter);
                    result.ChapterMissionCount = chapter.MissionCount;
                }
            }

            if (completed)
            {
                Challenges.BKS_ChallengeStats.Add(Challenges.BKS_Stat.MissionsCompleted);
                if (starRating >= 3) Challenges.BKS_ChallengeStats.Add(Challenges.BKS_Stat.ThreeStarMissions);
                if (result.ChapterCompleted) Challenges.BKS_ChallengeStats.Add(Challenges.BKS_Stat.ChaptersCompleted);
            }

            LastResult = result;

            Debug.Log($"[BKS_MissionResult] '{data.MissionName}' — Completed:{completed}, Stars:{starRating}, Coins:{coinsEarned}, SafarStars:{starsEarned}");

            OnResultReady?.Invoke(result);

            // Store in BKS_SceneLoader for the result scene to pick up
            BKS_MissionResultStore.Store(result);

            // Stop driving at the terminal result; the transition uses realtime.
            Time.timeScale = 0f;

            // Show the shared UI over this scene, preserving the car and terrain.
            if (autoLoadResultDelay > 0f)
                StartCoroutine(DelayedPopup(autoLoadResultDelay));
            else
                ShowResultPopup();
        }

        private System.Collections.IEnumerator DelayedPopup(float delay)
        {
            // Realtime: legacy GameManager sets timeScale = 0 on game over, which would freeze a scaled wait.
            yield return new WaitForSecondsRealtime(delay);
            ShowResultPopup();
        }

        private void ShowResultPopup()
        {
            var popup = Resources.Load<GameObject>("BKS_MissionResultPopup");
            if (popup == null)
            {
                Debug.LogError("[BKS_MissionResult] Missing BKS_MissionResultPopup prefab.");
                return;
            }
            Instantiate(popup);
        }

        // ── Reference Resolution ──────────────────────────────────────────────────

        private void ResolveReferences()
        {
            if (missionManager == null)
                missionManager = FindObjectOfType<MissionManager>();

            if (starTracker == null)
                starTracker = FindObjectOfType<BKS_SafarStarTracker>();

            if (vehicleHealth == null)
                vehicleHealth = FindObjectOfType<Vehicle.BKS_VehicleHealth>();
        }
    }

    /// <summary>
    /// Static store so the result screen (in a different scene) can read the last result.
    /// Simple and avoids the need for DontDestroyOnLoad data objects for a single result.
    /// </summary>
    public static class BKS_MissionResultStore
    {
        public static BKS_MissionResult.ResultData LastResult { get; private set; }
        public static bool HasResult { get; private set; }

        public static void Store(BKS_MissionResult.ResultData result)
        {
            LastResult = result;
            HasResult  = true;
        }

        public static void Clear()
        {
            HasResult = false;
        }
    }
}
