using System;
using UnityEngine;

namespace BharatKaSafar.Vehicle
{
    /// <summary>
    /// Vehicle Health component for Bharat Ka Safar.
    ///
    /// Attach to the same GameObject as CarController (or the vehicle root).
    /// Does NOT modify CarController.cs in any way.
    ///
    /// Damage sources: BKS_DamageObstacle calls TakeDamage().
    /// Health is 0–maxHealth. Reaching 0 fires OnVehicleDead.
    /// Suspension upgrade level reduces damage taken (via BKS_UpgradeLoader).
    /// </summary>
    public class BKS_VehicleHealth : MonoBehaviour
    {
        // Temporary game-wide switch. Keep the components and tuning for later reuse.
        public static bool HealthSystemEnabled => true; // obstacles damage the vehicle; 0 health = run fails (revive offered)

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fires when health changes. Arg: normalized 0–1.</summary>
        public event Action<float> OnHealthChanged;

        /// <summary>Fires once when health reaches 0.</summary>
        public event Action OnVehicleDead;

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Health Config")]
        [SerializeField] private float maxHealth = 100f;

        [Header("Suspension Damage Reduction")]
        [Tooltip("Each suspension level reduces damage by this fraction (e.g. 0.05 = 5% per level)")]
        [SerializeField] private float damageReductionPerSuspensionLevel = 0.05f;

        [Header("Invincibility After Hit")]
        [Tooltip("Seconds of invincibility after taking damage (prevents one-frame spam damage)")]
        [SerializeField] private float invincibilityDuration = 0.3f;

        // ── Runtime ───────────────────────────────────────────────────────────────
        public float CurrentHealth  { get; private set; }
        public float MaxHealth      => maxHealth;
        public bool  IsDead         { get; private set; }

        /// <summary>Normalized 0–1 health ratio.</summary>
        public float HealthRatio    => maxHealth > 0 ? CurrentHealth / maxHealth : 0f;

        private float _invincibilityTimer;
        private int   _suspensionLevel;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Start()
        {
            CurrentHealth = maxHealth;
            // Load suspension level from SaveManager if present
            if (Core.BKS_SaveManager.Instance != null)
                _suspensionLevel = Core.BKS_SaveManager.Instance.SuspensionLevel;
        }

        private void Update()
        {
            if (_invincibilityTimer > 0f)
                _invincibilityTimer -= Time.deltaTime;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Apply damage to the vehicle.
        /// Damage is reduced by suspension level.
        /// No effect if dead or currently invincible.
        /// </summary>
        public void TakeDamage(float rawDamage)
        {
            if (!HealthSystemEnabled) return;
            if (IsDead) return;
            if (_invincibilityTimer > 0f) return;

            // Apply suspension damage reduction: BaseDamage * (1 - reductionFraction * level)
            float reduction = damageReductionPerSuspensionLevel * _suspensionLevel;
            float actualDamage = rawDamage * Mathf.Clamp01(1f - reduction);

            CurrentHealth = Mathf.Max(0f, CurrentHealth - actualDamage);
            _invincibilityTimer = invincibilityDuration;

            Debug.Log($"[BKS_VehicleHealth] Took {actualDamage:F1} damage (raw:{rawDamage:F1}, reduction:{reduction*100:F0}%). HP: {CurrentHealth:F1}/{maxHealth}");

            OnHealthChanged?.Invoke(HealthRatio);

            if (CurrentHealth <= 0f && !IsDead)
            {
                IsDead = true;
                Debug.LogWarning("[BKS_VehicleHealth] Vehicle destroyed!");
                OnVehicleDead?.Invoke();

                // No health left → the run fails through the normal flow (revive popup first, then the result screen)
                var bridge = Integration.BKS_GameplayBridge.Instance;
                if (bridge != null) bridge.ReportFailure("Vehicle destroyed");
                else
                {
                    Integration.BKS_GameplayBridge.LastFailReason = "Vehicle destroyed";
                    var mm = FindFirstObjectByType<Missions.MissionManager>();
                    if (mm != null) mm.FailMission();
                }
            }
        }

        /// <summary>Heal the vehicle by a given amount (capped at maxHealth).</summary>
        public void Heal(float amount)
        {
            if (!HealthSystemEnabled) return;
            if (IsDead) return;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(HealthRatio);
        }

        /// <summary>Full restore (e.g. mission restart).</summary>
        public void FullRestore()
        {
            IsDead = false;
            CurrentHealth = maxHealth;
            _invincibilityTimer = 0f;
            OnHealthChanged?.Invoke(HealthRatio);
        }

        /// <summary>Override suspension level at runtime (called by BKS_UpgradeLoader).</summary>
        public void SetSuspensionLevel(int level)
        {
            _suspensionLevel = Mathf.Max(0, level);
        }
    }
}
