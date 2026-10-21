using UnityEngine;

namespace BharatKaSafar.Gameplay
{
    public enum CrashSeverity
    {
        NormalHit,
        CriticalCrash
    }

    /// <summary>
    /// Configurable obstacle that can cause either a minor speed penalty/damage or an instant critical crash.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BKS_Obstacle : MonoBehaviour
    {
        public CrashSeverity severity = CrashSeverity.NormalHit;
        
        [Tooltip("Speed reduction applied on NormalHit (multiplier). 1 = no reduction, 0 = complete stop.")]
        [Range(0f, 1f)]
        public float speedPenaltyMultiplier = 0.5f;

        [Tooltip("Vehicle health lost on NormalHit")]
        public float damageAmount = 20f;

        [Tooltip("Only hurt once (the car usually touches the same rock several times)")]
        public bool hitOnlyOnce = true;

        private bool _hasHit;

        [SerializeField] private ParticleSystem impactFX;
        [SerializeField] private AudioClip impactSound;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleCollision(collision.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleCollision(other.gameObject);
        }

        private void HandleCollision(GameObject other)
        {
            if (!Vehicle.BKS_VehicleUtil.IsVehicle(other)) return;
            if (hitOnlyOnce && _hasHit) return;
            _hasHit = true;

            Missions.BKS_MissionObjectiveTracker.ReportHit();
            if (impactFX != null) impactFX.Play();
            if (impactSound != null) AudioSource.PlayClipAtPoint(impactSound, transform.position);

            if (severity == CrashSeverity.CriticalCrash)
            {
                Debug.Log("[BKS_Obstacle] CRITICAL CRASH! Mission failed.");
                Integration.BKS_GameplayBridge.LastFailReason = "Crashed into " + gameObject.name;
                var mm = FindFirstObjectByType<Missions.MissionManager>();
                if (mm != null) mm.FailMission();
                return;
            }

            // Normal hit: lose some health and slow down
            var health = Vehicle.BKS_VehicleUtil.GetHealth(other);
            if (health != null) health.TakeDamage(damageAmount);

            var car = Vehicle.BKS_VehicleUtil.GetCar(other);
            var rb = car != null ? car.GetComponent<Rigidbody2D>() : other.GetComponentInParent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity *= speedPenaltyMultiplier;

            Debug.Log($"[BKS_Obstacle] Hit '{name}': -{damageAmount} health, speed x{speedPenaltyMultiplier}.");
        }
    }
}
