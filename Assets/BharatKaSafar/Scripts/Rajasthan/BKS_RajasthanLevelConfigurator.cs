using System.Collections.Generic;
using UnityEngine;
using BharatKaSafar.Missions;
using BharatKaSafar.Core;

namespace BharatKaSafar.Rajasthan
{
    /// <summary>
    /// Reads the active MissionData from BKS_SceneLoader and applies
    /// the correct environment, weather, and obstacle configuration
    /// for the current Rajasthan level.
    ///
    /// Architecture: One shared Gameplay scene can host this script.
    /// It reads the active mission ID at runtime and enables/disables
    /// the correct level-specific GameObjects (grouped by level ID).
    ///
    /// Level-specific GameObjects should be placed under:
    ///   [LevelConfig_Raj_M01], [LevelConfig_Raj_M02], etc.
    /// and assigned in the Inspector to the matching LevelConfig entry.
    /// </summary>
    public class BKS_RajasthanLevelConfigurator : MonoBehaviour
    {
        [System.Serializable]
        public class LevelConfig
        {
            [Tooltip("Must match a MissionData.MissionID exactly, e.g. Raj_M01")]
            public string missionID;

            [Header("Environment Roots")]
            [Tooltip("Parent GameObject for this level's background/environment art")]
            public GameObject environmentRoot;
            [Tooltip("Parent GameObject for this level's obstacle layout")]
            public GameObject obstacleRoot;
        }

        [Header("Level Configurations")]
        [SerializeField] private List<LevelConfig> levelConfigs = new List<LevelConfig>();


        private void Awake()
        {
            // Determine which level is active
            string activeMissionID = GetActiveMissionID();

            if (string.IsNullOrEmpty(activeMissionID))
            {
                Debug.LogWarning("[BKS_RajasthanLevelConfigurator] No active mission found. Showing all environments (Editor mode).");
                return;
            }

            ApplyConfig(activeMissionID);
        }

        private string GetActiveMissionID()
        {
            // Primary source: BKS_SceneLoader.ActiveMission (set when LoadMission() is called)
            if (BKS_SceneLoader.Instance != null && BKS_SceneLoader.Instance.ActiveMission != null)
                return BKS_SceneLoader.Instance.ActiveMission.MissionID;

            // Fallback: read from MissionManager in scene
            var mm = FindFirstObjectByType<MissionManager>();
            if (mm != null && mm.CurrentMission != null)
                return mm.CurrentMission.MissionID;

            return null;
        }

        private void ApplyConfig(string missionID)
        {
            Debug.Log($"[BKS_RajasthanLevelConfigurator] Applying config for mission: {missionID}");

            foreach (var config in levelConfigs)
            {
                bool isActive = config.missionID == missionID;

                // Toggle environment and obstacle roots
                if (config.environmentRoot != null)
                    config.environmentRoot.SetActive(isActive);

                if (config.obstacleRoot != null)
                    config.obstacleRoot.SetActive(isActive);
            }
        }

        // ── Editor Helper ─────────────────────────────────────────────────────────
#if UNITY_EDITOR
        [ContextMenu("Preview Level: Raj_M01 (Jaipur)")]
        private void PreviewM01() => ApplyConfig("Raj_M01");

        [ContextMenu("Preview Level: Raj_M02 (Jodhpur)")]
        private void PreviewM02() => ApplyConfig("Raj_M02");

        [ContextMenu("Preview Level: Raj_M03 (Jaisalmer)")]
        private void PreviewM03() => ApplyConfig("Raj_M03");

        [ContextMenu("Preview Level: Raj_M04 (Udaipur)")]
        private void PreviewM04() => ApplyConfig("Raj_M04");

        [ContextMenu("Preview Level: Raj_M05 (Pushkar)")]
        private void PreviewM05() => ApplyConfig("Raj_M05");
#endif
    }
}
