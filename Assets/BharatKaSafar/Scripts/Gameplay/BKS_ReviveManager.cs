using UnityEngine;
using BharatKaSafar.Core;
using BharatKaSafar.Missions;
using BharatKaSafar.Rewards;
using BharatKaSafar.UI;

namespace BharatKaSafar.Gameplay
{
    /// <summary>
    /// Coin-based revive. When a run is about to fail and the player has reached Min Score,
    /// "Continue Journey? Revive for 200" is offered. Accepting spends coins and puts the vehicle
    /// back upright at the last checkpoint (or a little behind the crash), keeping the mission,
    /// score, coins and collected items. Limited to Max Revives per run.
    /// </summary>
    public class BKS_ReviveManager : MonoBehaviour
    {
        public static BKS_ReviveManager Instance { get; private set; }

        [Header("Rules (editable)")]
        [Tooltip("Run score needed before a revive is offered")]
        [SerializeField, Min(0)] private int minScore = 1000;
        [SerializeField, Min(0)] private int reviveCost = 200;
        [SerializeField, Min(0)] private int maxRevivesPerRun = 1;

        [Header("Respawn")]
        [Tooltip("Without a checkpoint, respawn this far behind the crash")]
        [SerializeField, Min(0f)] private float backDistance = 6f;
        [SerializeField, Min(0f)] private float dropHeight = 2.5f;
        [Tooltip("Use a checkpoint only if it is at most this far behind the crash")]
        [SerializeField, Min(0f)] private float maxCheckpointDistance = 60f;

        [Header("UI")]
        [SerializeField] private BKS_RevivePopup popup;

        public int MinScore => minScore;
        public int ReviveCost => reviveCost;
        public int RevivesUsed { get; private set; }

        private MissionManager pending;

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Returns true if the revive popup was shown (the mission must not fail yet).</summary>
        public static bool TryOffer(MissionManager mission)
        {
            var r = Instance;
            if (r == null || r.popup == null || r.pending != null) return false;
            if (r.RevivesUsed >= r.maxRevivesPerRun) return false;
            int score = BKS_RewardManager.Instance != null ? BKS_RewardManager.Instance.currentScore : 0;
            if (score < r.minScore) return false;

            r.pending = mission;
            Time.timeScale = 0f;
            int coins = BKS_SaveManager.Instance != null ? BKS_SaveManager.Instance.Coins : 0;
            r.popup.Show(r.reviveCost, coins, r.Accept, r.Decline);
            return true;
        }

        private void Accept()
        {
            var save = BKS_SaveManager.Instance;
            if (save == null || !save.SpendCoins(reviveCost)) { Decline(); return; }
            RevivesUsed++;
            Challenges.BKS_ChallengeStats.Add(Challenges.BKS_Stat.RevivesUsed);
            Respawn();
            Time.timeScale = 1f;
            pending = null;
        }

        private void Decline()
        {
            Time.timeScale = 1f;
            var m = pending;
            pending = null;
            if (m != null) m.FailMissionNow();
        }

        private void Respawn()
        {
            var car = FindFirstObjectByType<CarController>();
            if (car == null) return;
            var chassis = car.GetComponent<Rigidbody2D>();
            Transform root = car.transform.root;

            // Where to put the car
            float crashX = car.transform.position.x;
            float x = crashX - backDistance;
            var cp = BKS_CheckpointManager.Instance;
            if (cp != null && cp.HasCheckpoint && crashX - cp.LastCheckpointPosition.x <= maxCheckpointDistance)
                x = cp.LastCheckpointPosition.x;
            Vector2 target = new Vector2(x, GroundY(x, car.transform.position.y) + dropHeight);

            // Turn the whole vehicle (body, wheels, driver) upright around the chassis and move it
            Vector2 pivot = chassis != null ? chassis.position : (Vector2)car.transform.position;
            float angle = chassis != null ? chassis.rotation : car.transform.eulerAngles.z;
            var undo = Quaternion.Euler(0f, 0f, -angle);
            foreach (var rb in root.GetComponentsInChildren<Rigidbody2D>())
            {
                Vector2 offset = undo * (rb.position - pivot);
                rb.position = target + offset;
                rb.rotation -= angle;
                rb.transform.SetPositionAndRotation(target + offset, Quaternion.Euler(0f, 0f, rb.rotation));
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            Physics2D.SyncTransforms();

            // Fuel, health and failure watchers
            var gm = FindFirstObjectByType<GameManager>();
            if (gm != null && gm.TotalFuel < 25f) gm.TotalFuel = 50f;
            var health = FindFirstObjectByType<Vehicle.BKS_VehicleHealth>();
            if (health != null) health.FullRestore();
            var bridge = FindFirstObjectByType<Integration.BKS_GameplayBridge>();
            if (bridge != null) bridge.ResetAfterRevive();
            Debug.Log($"[BKS_ReviveManager] Revived at x={x:F1} for {reviveCost} coins.");
        }

        private static float GroundY(float x, float fallback)
        {
            foreach (var h in Physics2D.RaycastAll(new Vector2(x, fallback + 200f), Vector2.down, 1000f))
            {
                if (h.collider.isTrigger || Vehicle.BKS_VehicleUtil.IsVehicle(h.collider.gameObject)) continue;
                return h.point.y;
            }
            return fallback;
        }
    }
}
