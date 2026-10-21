using System;
using UnityEngine;

namespace BharatKaSafar.Integration
{
    /// <summary>
    /// Non-intrusive read-only adapter between legacy GameManager and BKS systems.
    /// Only accesses PUBLIC fields from GameManager. Does NOT modify any legacy scripts.
    /// 
    /// Public fields used:
    ///   - targetDistance (int)
    ///   - TotalFuel (float)
    ///   - isDead (bool, HideInInspector)
    ///   - fuelFinished (bool, HideInInspector)
    /// 
    /// Win detection: vehicle.position.x >= targetDistance (mirrors legacy logic)
    /// </summary>
    public class BKS_GameplayBridge : MonoBehaviour
    {
        [Header("Legacy System References (Auto-detected if null)")]
        [SerializeField] private GameManager legacyGameManager;
        [SerializeField] private Transform vehicleTransform;

        // Read-only telemetry
        public float CurrentDistance { get; private set; }
        public float TargetDistance => legacyGameManager != null ? legacyGameManager.targetDistance : 0f;
        public float CurrentFuel => legacyGameManager != null ? legacyGameManager.TotalFuel : 0f;
        public int CurrentCoins => PlayerPrefs.GetInt("Coins", 0);
        public bool IsGameOver => legacyGameManager != null && (legacyGameManager.isDead || legacyGameManager.fuelFinished);
        public Transform VehicleTransform => vehicleTransform;

        // Events for BKS systems to subscribe to
        public event Action<float> OnDistanceUpdated;
        public event Action OnDestinationReached;
        public event Action OnGameplayFailed;

        /// <summary>Why the run failed ("Crashed", "Fuel finished"), for the result screen.</summary>
        public static string LastFailReason { get; set; }

        [Header("Crash detection")]
        [Tooltip("Continuous time upside down against the ground before failing; resets when the vehicle recovers.")]
        [SerializeField, Min(0.1f)] private float crashConfirmSeconds = 1f;

        [Tooltip("Minimum tilt from upright required for a crash. A vertical car (90 degrees) is safe.")]
        [SerializeField, Range(100f, 180f)] private float crashTiltDegrees = 135f;

        [Header("Stuck (e.g. standing up against a cart)")]
        [Tooltip("Tilt from upright that counts as standing on its end")]
        [SerializeField, Range(45f, 100f)] private float stuckTiltDegrees = 70f;
        [Tooltip("Seconds standing up and (almost) not moving before the run fails")]
        [SerializeField, Min(0.5f)] private float stuckSeconds = 2f;

        [Header("Fuel")]
        [Tooltip("A full tank lasts this many seconds of driving (old game: ~170 s)")]
        [SerializeField, Min(10f)] private float fullTankSeconds = 55f;

        private float stuckTimer = 0f;
        public static BKS_GameplayBridge Instance { get; private set; }

        /// <summary>Fail the run from other systems (e.g. vehicle health at 0). Goes through the normal revive / fail flow.</summary>
        public void ReportFailure(string reason)
        {
            if (gameplayFailedReported || destinationReachedReported) return;
            LastFailReason = reason;
            gameplayFailedReported = true;
            if (AudioManager.instance) AudioManager.instance.PlayDieSound();
            OnGameplayFailed?.Invoke();
        }

        private bool destinationReachedReported = false;
        private bool gameplayFailedReported = false;
        private CarController vehicleController;
        private readonly ContactPoint2D[] groundContacts = new ContactPoint2D[32];
        private float deadTimer = 0f;

        private void Awake()
        {
            // BKS levels use the BKS Mission Result screen instead of the legacy win / game-over menus.
            GameManager.UseExternalResultScreen = true;
            LastFailReason = null;
            Instance = this;
            FindLegacySystems();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            GameManager.UseExternalResultScreen = false;
        }

        private void Start()
        {
            FindLegacySystems();
            // fuel burns 0.35 per FuelTime seconds out of 100 → set FuelTime so a full tank lasts fullTankSeconds
            if (legacyGameManager != null) legacyGameManager.FuelTime = 0.35f * fullTankSeconds / 100f;
            if (legacyGameManager != null)
            {
                Debug.Log("[BKS_GameplayBridge] Connected to GameManager. Target distance: " + legacyGameManager.targetDistance);
            }
        }

        private void Update()
        {
            if (legacyGameManager == null)
            {
                FindLegacySystems();
                if (legacyGameManager == null) return;
            }

            // Vehicle may spawn at runtime, keep looking
            if (vehicleTransform == null)
            {
                FindVehicle();
                if (vehicleTransform == null) return;
            }

            float posX = vehicleTransform.position.x;

            // Update distance (only increase, never decrease)
            if (posX > CurrentDistance)
            {
                CurrentDistance = posX;
                OnDistanceUpdated?.Invoke(CurrentDistance);
            }

            // Check win: vehicle reached target distance (mirrors GameManager.CoinDistance logic)
            if (!destinationReachedReported && !gameplayFailedReported)
            {
                if (TargetDistance > 0 && posX >= TargetDistance)
                {
                    destinationReachedReported = true;
                    Debug.Log("[BKS_GameplayBridge] Destination reached! Distance: " + posX);
                    OnDestinationReached?.Invoke();
                }
            }

            // Check failure: crash or fuel depletion
            if (!gameplayFailedReported && !destinationReachedReported)
            {
                // The legacy head trigger can touch hills even when the car is upright.
                // Require an actual overturned pose and continuous ground contact.
                deadTimer = IsOverturnedOnGround() ? deadTimer + Time.deltaTime : 0f;
                bool crashed = deadTimer >= Mathf.Max(0.1f, crashConfirmSeconds);

                // standing on its end and not moving (e.g. nose up against a cart) also ends the run
                stuckTimer = IsStuckStanding() ? stuckTimer + Time.deltaTime : 0f;
                if (!crashed && stuckTimer >= stuckSeconds) { crashed = true; }

                if (crashed || legacyGameManager.fuelFinished)
                {
                    LastFailReason = legacyGameManager.fuelFinished ? "Fuel finished" : "Crashed";
                    gameplayFailedReported = true;
                    Debug.LogWarning("[BKS_GameplayBridge] Gameplay failure detected.");
                    // Legacy GameManager.Dead() is skipped in BKS levels, so play its die sound here.
                    if (AudioManager.instance) AudioManager.instance.PlayDieSound();
                    OnGameplayFailed?.Invoke();
                }
            }
        }

        /// <summary>Called after a revive: watch for crashes / empty fuel again.</summary>
        public void ResetAfterRevive()
        {
            gameplayFailedReported = false;
            deadTimer = 0f;
            stuckTimer = 0f;
            if (vehicleTransform != null) { var h = Vehicle.BKS_VehicleUtil.GetHealth(vehicleTransform.gameObject); if (h != null) h.FullRestore(); }
            LastFailReason = null;
            if (legacyGameManager != null)
            {
                legacyGameManager.isDead = false;
                legacyGameManager.fuelFinished = false;
            }
        }

        private void FindLegacySystems()
        {
            if (legacyGameManager == null)
            {
                legacyGameManager = FindObjectOfType<GameManager>();
            }

            FindVehicle();
        }

        private void FindVehicle()
        {
            if (vehicleTransform != null) return;

            // Use the moving physics body, not a stationary prefab root or wheel tag.
            vehicleController = FindFirstObjectByType<CarController>();
            if (vehicleController != null)
            {
                vehicleTransform = vehicleController.transform;
                return;
            }

            GameObject car = GameObject.FindGameObjectWithTag("Car");
            if (car != null)
            {
                vehicleTransform = car.transform;
                Debug.Log("[BKS_GameplayBridge] Vehicle found via 'Car' tag.");
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                vehicleTransform = player.transform;
                Debug.Log("[BKS_GameplayBridge] Vehicle found via 'Player' tag.");
            }
        }

        private bool IsStuckStanding()
        {
            if (vehicleController == null) return false;
            var body = vehicleController.GetComponent<Rigidbody2D>();
            if (body == null) return false;
            float tilt = Vector2.Angle(vehicleController.transform.up, Vector2.up);
            return tilt >= stuckTiltDegrees && body.linearVelocity.magnitude < 1.2f;
        }

        private bool IsOverturnedOnGround()
        {
            if (vehicleController == null && vehicleTransform != null)
                vehicleController = Vehicle.BKS_VehicleUtil.GetCar(vehicleTransform.gameObject);
            float minimumTilt = Mathf.Clamp(crashTiltDegrees, 100f, 180f);
            if (vehicleController == null || Vector2.Angle(vehicleController.transform.up, Vector2.up) < minimumTilt)
                return false;

            // Head contact is evidence of a crash only after the tilt check.
            if (legacyGameManager != null && legacyGameManager.isDead) return true;

            // A rotation during an airborne jump is allowed. Only solid contact counts.
            var body = vehicleController.GetComponent<Rigidbody2D>();
            if (body == null) return false;
            int count = body.GetContacts(groundContacts);
            for (int i = 0; i < count; i++)
            {
                var contact = groundContacts[i];
                if (contact.collider == null) continue;
                var other = contact.collider.attachedRigidbody == body ? contact.otherCollider : contact.collider;
                if (other != null && !other.isTrigger && !Vehicle.BKS_VehicleUtil.IsVehicle(other.gameObject))
                    return true;
            }
            return false;
        }
    }
}
