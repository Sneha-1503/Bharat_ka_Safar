using UnityEngine;

namespace BharatKaSafar.Vehicle
{
    /// <summary>
    /// Applies upgrade modifiers to the vehicle's runtime stats.
    /// Uses BaseStat + UpgradeModifier pattern.
    /// Does NOT permanently modify CarController base values — only applies
    /// the delta at runtime each time a level changes.
    ///
    /// Attach to the same GameObject as CarController.
    /// </summary>
    [RequireComponent(typeof(CarController))]
    public class BKS_UpgradeLoader : MonoBehaviour
    {
        [Tooltip("ON: upgrades add a % on top of THIS vehicle's own power/speed (each vehicle keeps its character). OFF: old fixed values below.")]
        [SerializeField] private bool relativeToVehicle = true;
        [SerializeField, Range(0f, 0.3f)] private float powerPercentPerLevel = 0.08f;
        [SerializeField, Range(0f, 0.3f)] private float speedPercentPerLevel = 0.06f;

        [Header("Engine (affects motorPower)")]
        [SerializeField] private float basePower = 1400f;
        [SerializeField] private float powerPerLevel = 100f;

        [Header("Tyres (affects maxSpeed)")]
        [SerializeField] private float baseMaxSpeed = 14f;
        [SerializeField] private float speedPerLevel = 0.8f;

        [Header("Suspension (affects groundDistance tolerance)")]
        [SerializeField] private float baseGroundDistance = 2.1f;
        [SerializeField] private float groundDistancePerLevel = 0.1f;

        private CarController    _car;
        private BKS_VehicleHealth _health;
        private BKS_HornController _horn;

        private void Awake()
        {
            _car    = GetComponent<CarController>();
            _health = GetComponent<BKS_VehicleHealth>();
            _horn   = GetComponent<BKS_HornController>();
            if (relativeToVehicle && _car != null)
            {
                basePower = _car.motorPower;
                baseMaxSpeed = _car.maxSpeed;
                baseGroundDistance = _car.groundDistance;
            }
        }

        private void Start()
        {
            ApplyAllUpgrades();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Read all levels from BKS_SaveManager and apply to vehicle stats.</summary>
        public void ApplyAllUpgrades()
        {
            if (Core.BKS_SaveManager.Instance == null) return;

            ApplyEngine(Core.BKS_SaveManager.Instance.EngineLevel);
            ApplyTyres(Core.BKS_SaveManager.Instance.TyreLevel);
            ApplySuspension(Core.BKS_SaveManager.Instance.SuspensionLevel);
            ApplyHorn(Core.BKS_SaveManager.Instance.HornLevel);
        }

        // ── Individual Upgrade Appliers ────────────────────────────────────────────

        public void ApplyEngine(int level)
        {
            if (_car == null) return;
            // motorPower is public in CarController
            _car.motorPower = relativeToVehicle ? basePower * (1f + powerPercentPerLevel * level) : basePower + (powerPerLevel * level);
            Debug.Log($"[BKS_UpgradeLoader] Engine L{level}: motorPower = {_car.motorPower}");
        }

        public void ApplyTyres(int level)
        {
            if (_car == null) return;
            _car.maxSpeed = relativeToVehicle ? baseMaxSpeed * (1f + speedPercentPerLevel * level) : baseMaxSpeed + (speedPerLevel * level);
            Debug.Log($"[BKS_UpgradeLoader] Tyres L{level}: maxSpeed = {_car.maxSpeed}");
        }

        public void ApplySuspension(int level)
        {
            if (_car == null) return;
            _car.groundDistance = baseGroundDistance + (groundDistancePerLevel * level);

            if (_health != null)
                _health.SetSuspensionLevel(level);

            Debug.Log($"[BKS_UpgradeLoader] Suspension L{level}: groundDistance = {_car.groundDistance}");
        }

        public void ApplyHorn(int level)
        {
            if (_horn != null)
                _horn.SetHornLevel(level);

            Debug.Log($"[BKS_UpgradeLoader] Horn L{level} applied.");
        }
    }
}
