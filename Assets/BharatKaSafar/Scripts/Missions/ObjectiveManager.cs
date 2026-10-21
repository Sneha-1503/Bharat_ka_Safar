using UnityEngine;
using BharatKaSafar.Integration;

namespace BharatKaSafar.Missions
{
    /// <summary>
    /// Evaluates mission objectives using BKS_GameplayBridge telemetry.
    /// Triggers MissionManager completion or failure without touching legacy systems.
    /// </summary>
    public class ObjectiveManager : MonoBehaviour
    {
        [Header("System References (Auto-detected if null)")]
        [SerializeField] private MissionManager missionManager;
        [SerializeField] private BKS_GameplayBridge gameplayBridge;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (gameplayBridge != null)
            {
                gameplayBridge.OnDestinationReached += HandleDestinationReached;
                gameplayBridge.OnGameplayFailed += HandleGameplayFailed;
            }
        }

        private void OnDisable()
        {
            if (gameplayBridge != null)
            {
                gameplayBridge.OnDestinationReached -= HandleDestinationReached;
                gameplayBridge.OnGameplayFailed -= HandleGameplayFailed;
            }
        }

        private void ResolveReferences()
        {
            if (missionManager == null)
            {
                missionManager = GetComponent<MissionManager>();
                if (missionManager == null)
                {
                    missionManager = FindObjectOfType<MissionManager>();
                }
            }

            if (gameplayBridge == null)
            {
                gameplayBridge = FindObjectOfType<BKS_GameplayBridge>();
            }
        }

        private void HandleDestinationReached()
        {
            if (missionManager != null && missionManager.CurrentState == MissionState.Active)
            {
                Debug.Log("[ObjectiveManager] Primary objective achieved! Completing mission.");
                missionManager.CompleteMission();
            }
        }

        private void HandleGameplayFailed()
        {
            if (missionManager != null && missionManager.CurrentState == MissionState.Active)
            {
                Debug.LogWarning("[ObjectiveManager] Journey interrupted. Failing mission.");
                missionManager.FailMission();
            }
        }
    }
}
