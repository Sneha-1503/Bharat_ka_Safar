using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using BharatKaSafar.Core;

namespace BharatKaSafar.Missions
{
    public enum MissionState { None, Ready, Active, Completed, Failed, FailurePending }

    /// <summary>
    /// One per gameplay scene. Holds the current MissionData and its state.
    ///
    /// Which mission? (first match wins)
    ///   1. BKS_SceneLoader.ActiveMission — the mission launched from Mission Select,
    ///      if its gameplay scene is this scene
    ///   2. The Mission Data assigned in the Inspector
    ///   3. The mission of the active chapter whose gameplay scene is this scene
    /// So gameplay scenes do NOT need Mission Data assigned by hand anymore.
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        [Header("Mission Asset (optional — auto-detected when launched from Mission Select)")]
        [SerializeField] private MissionData missionData;

        [Tooltip("Seconds of ragdoll physics shown after a crash before the revive or result panel.")]
        [SerializeField, Min(0f)] private float crashSettleSeconds = 0.45f;
        private Vehicle.BKS_HeadCrash fallenDriver;

        public MissionData CurrentMission => missionData;
        public MissionState CurrentState { get; private set; } = MissionState.None;

        public event Action<MissionData> OnMissionLoaded;
        public event Action<MissionData> OnMissionStarted;
        public event Action<MissionData> OnMissionCompleted;
        public event Action<MissionData> OnMissionFailed;

        private void Awake()
        {
            if (Rewards.BKS_RewardManager.Instance == null && FindFirstObjectByType<Rewards.BKS_RewardManager>() == null)
                new GameObject("BKS Run Rewards").AddComponent<Rewards.BKS_RewardManager>();
            // Swap in the vehicle chosen in the Store before StartPoint spawns the car
            gameObject.AddComponent<Store.BKS_SelectedVehicleSpawner>();
            // distance milestones: +10 / +25 / +50 coins every 100 / 250 / 500 m
            if (GetComponent<Rewards.BKS_DistanceReward>() == null) gameObject.AddComponent<Rewards.BKS_DistanceReward>();
            ResolveMissionData();

            if (missionData == null)
            {
                Debug.LogWarning("[MissionManager] No MissionData found. Launch this level from Mission Select, or assign Mission Data in the Inspector.", this);
                return;
            }

            CurrentState = MissionState.Ready;
            OnMissionLoaded?.Invoke(missionData);
        }

        private void Start()
        {
            if (missionData != null && (CurrentState == MissionState.Ready || CurrentState == MissionState.None))
            {
                StartMission();
            }
        }

        private void ResolveMissionData()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            var loader = BKS_SceneLoader.Instance;

            // 1. Mission launched from Mission Select
            if (loader != null && loader.ActiveMission != null && loader.ActiveMission.GameplaySceneName == sceneName)
            {
                missionData = loader.ActiveMission;
                return;
            }

            // 2. Inspector value — only if it really belongs to this scene
            //    (scenes generated from the Madurai template still point at Madurai's MissionData)
            if (missionData != null && missionData.GameplaySceneName == sceneName) return;
            var inspectorValue = missionData;
            missionData = null;

            // 3. Look it up by scene name: the active chapter first, then every chapter
            if (Find(loader != null ? loader.ActiveChapter : null, sceneName)) return;
            var registry = Diary.BKS_ChapterRegistry.Load();
            if (registry != null)
                foreach (var ch in registry.chapters)
                    if (Find(ch, sceneName)) return;

            // 4. nothing matched: keep whatever the Inspector had
            missionData = inspectorValue;
        }

        private bool Find(ChapterData chapter, string sceneName)
        {
            if (chapter == null) return false;
            for (int i = 0; i < chapter.MissionCount; i++)
            {
                MissionData m = chapter.GetMission(i);
                if (m != null && m.GameplaySceneName == sceneName) { missionData = m; return true; }
            }
            return false;
        }

        public void StartMission()
        {
            if (missionData == null) return;
            CurrentState = MissionState.Active;

            // Safar Stars available in this mission come from MissionData (e.g. 3)
            var tracker = FindFirstObjectByType<BKS_SafarStarTracker>();
            if (tracker != null)
            {
                tracker.Reset();
                // Safar Stars now come from handling obstacles correctly: one per obstacle that can award one
                int sources = 0;
                foreach (var b in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (b is Obstacles.IBKS_SafarStarSource) sources++;
                tracker.SetTotal(sources > 0 ? sources : missionData.MaxSafarStars);
            }

            var objectives = BKS_MissionObjectiveTracker.Instance;
            if (objectives == null) objectives = gameObject.AddComponent<BKS_MissionObjectiveTracker>();
            objectives.Begin(missionData);

            UI.BKS_DrivingTutorial.TryBegin(this);
            OnMissionStarted?.Invoke(missionData);
            Debug.Log($"[MissionManager] Mission '{missionData.MissionName}' ({missionData.MissionID}) started.");
        }

        public void CompleteMission()
        {
            if (CurrentState != MissionState.Active) return;
            CurrentState = MissionState.Completed;
            if (BKS_MissionObjectiveTracker.Instance != null) BKS_MissionObjectiveTracker.Instance.Stop();
            OnMissionCompleted?.Invoke(missionData);
            Debug.Log($"[MissionManager] Mission '{missionData.MissionName}' completed.");
        }

        // Testing shortcut: in Play Mode right-click the MissionManager component header.
        [ContextMenu("Debug/Complete Mission Now")]
        private void DebugCompleteMission() => CompleteMission();

        public void FailMission()
        {
            if (CurrentState != MissionState.Active) return;
            CurrentState = MissionState.FailurePending;
            if (Integration.BKS_GameplayBridge.LastFailReason != "Fuel finished")
            {
                fallenDriver = FindFirstObjectByType<Vehicle.BKS_HeadCrash>();
                if (fallenDriver != null && fallenDriver.ReleaseOnDeath() && crashSettleSeconds > 0f)
                {
                    StartCoroutine(ShowFailureAfterCrash());
                    return;
                }
            }
            OfferReviveOrFail();
        }

        private System.Collections.IEnumerator ShowFailureAfterCrash()
        {
            yield return new WaitForSeconds(crashSettleSeconds);
            if (CurrentState == MissionState.FailurePending) OfferReviveOrFail();
        }

        private void OfferReviveOrFail()
        {
            if (!Gameplay.BKS_ReviveManager.TryOffer(this)) FailMissionNow();
        }

        public void ResumeAfterRevive()
        {
            if (CurrentState != MissionState.FailurePending) return;
            if (fallenDriver != null) fallenDriver.RestoreAfterRevive();
            fallenDriver = null;
            CurrentState = MissionState.Active;
        }

        /// <summary>Fail without offering a revive.</summary>
        public void FailMissionNow()
        {
            if (CurrentState != MissionState.Active && CurrentState != MissionState.FailurePending) return;
            CurrentState = MissionState.Failed;
            if (BKS_MissionObjectiveTracker.Instance != null) BKS_MissionObjectiveTracker.Instance.Stop();
            OnMissionFailed?.Invoke(missionData);
            Debug.LogWarning($"[MissionManager] Mission '{missionData.MissionName}' failed.");
        }
    }
}
