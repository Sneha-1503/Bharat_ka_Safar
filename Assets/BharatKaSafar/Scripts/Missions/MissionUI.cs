using TMPro;
using UnityEngine;

namespace BharatKaSafar.UI
{
    /// <summary>
    /// Subscribes to MissionManager events and updates TMP text fields.
    /// Handles Started, Completed, and Failed mission states.
    /// </summary>
    public class MissionUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text missionTitleText;
        [SerializeField] private TMP_Text objectiveText;

        private Missions.MissionManager missionManager;

        private void Awake()
        {
            FindManager();
        }

        private void OnEnable()
        {
            FindManager();
            if (missionManager != null)
            {
                missionManager.OnMissionStarted += HandleMissionStarted;
                missionManager.OnMissionCompleted += HandleMissionCompleted;
                missionManager.OnMissionFailed += HandleMissionFailed;

                // Catch up if we subscribed late
                if (missionManager.CurrentState == Missions.MissionState.Active && missionManager.CurrentMission != null)
                {
                    HandleMissionStarted(missionManager.CurrentMission);
                }
            }
        }

        private void Start()
        {
            FindManager();
            if (missionManager != null && missionManager.CurrentMission != null)
            {
                if (missionManager.CurrentState == Missions.MissionState.Active)
                {
                    HandleMissionStarted(missionManager.CurrentMission);
                }
            }
        }

        private void OnDisable()
        {
            if (missionManager != null)
            {
                missionManager.OnMissionStarted -= HandleMissionStarted;
                missionManager.OnMissionCompleted -= HandleMissionCompleted;
                missionManager.OnMissionFailed -= HandleMissionFailed;
            }
        }

        private void FindManager()
        {
            if (missionManager == null)
            {
                missionManager = FindObjectOfType<Missions.MissionManager>();
            }
        }

        private void HandleMissionStarted(Missions.MissionData data)
        {
            if (data == null) return;

            string title = string.IsNullOrEmpty(data.MissionName) ? "THE JOURNEY BEGINS" : data.MissionName.ToUpper();
            string obj = string.IsNullOrEmpty(data.PrimaryObjectiveText) ? "Travel safely and reach the destination." : data.PrimaryObjectiveText;

            SetText(title, obj);
            Debug.Log("[MissionUI] Mission started: " + title);
        }

        private void HandleMissionCompleted(Missions.MissionData data)
        {
            SetText("MISSION COMPLETED!", "You safely reached the destination in Rajasthan.");
            Debug.Log("[MissionUI] MISSION COMPLETED!");
        }

        private void HandleMissionFailed(Missions.MissionData data)
        {
            SetText("MISSION FAILED", "The journey ended before reaching the destination.");
            Debug.LogWarning("[MissionUI] MISSION FAILED");
        }

        private void SetText(string title, string objective)
        {
            if (missionTitleText != null)
                missionTitleText.text = title;
            if (objectiveText != null)
                objectiveText.text = objective;
        }
    }
}
