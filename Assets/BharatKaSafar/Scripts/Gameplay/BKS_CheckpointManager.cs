using UnityEngine;
using BharatKaSafar.Rewards;

namespace BharatKaSafar.Gameplay
{
    /// <summary>
    /// Manages the latest checkpoint and the revive flow.
    /// </summary>
    public class BKS_CheckpointManager : MonoBehaviour
    {
        public static BKS_CheckpointManager Instance { get; private set; }

        private Vector3 lastCheckpointPos;
        private bool hasCheckpoint = false;
        
        // Snapshot of rewards at checkpoint
        private int savedCoins = 0;
        private int savedScore = 0;

        [Header("Player Reference")]
        [Tooltip("If null, will try to find player by tag 'Player'")]
        public GameObject playerObject;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            if (playerObject == null)
            {
                playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject == null)
                {
                    var cc = Object.FindFirstObjectByType<CarController>();
                    if (cc != null) playerObject = cc.gameObject;
                }
            }
            
            // Initial spawn is default checkpoint
            if (playerObject != null)
            {
                lastCheckpointPos = playerObject.transform.position;
                hasCheckpoint = true;
            }
        }

        public void SaveCheckpoint(Vector3 position)
        {
            lastCheckpointPos = position;
            hasCheckpoint = true;

            if (BKS_RewardManager.Instance != null)
            {
                savedCoins = BKS_RewardManager.Instance.currentCoins;
                savedScore = BKS_RewardManager.Instance.currentScore;
            }
        }

        public bool HasCheckpoint => hasCheckpoint;
        public Vector3 LastCheckpointPosition => lastCheckpointPos;

        public bool CanRevive()
        {
            return hasCheckpoint;
        }

        public void RevivePlayer()
        {
            if (!hasCheckpoint || playerObject == null) return;

            // Reset position
            playerObject.transform.position = lastCheckpointPos;
            
            // Reset Rigidbody velocity if exists
            var rb = playerObject.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            // Restore state in RewardManager
            if (BKS_RewardManager.Instance != null)
            {
                BKS_RewardManager.Instance.SetState(savedCoins, savedScore);
            }

            // Reset health if VehicleHealth exists
            var health = playerObject.GetComponent<Vehicle.BKS_VehicleHealth>();
            if (health != null)
            {
                health.Heal(100f); // Full heal on revive
            }

            Debug.Log("[BKS_CheckpointManager] Player Revived!");
        }
    }
}
