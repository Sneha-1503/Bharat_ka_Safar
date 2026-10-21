using UnityEngine;
using BharatKaSafar.Vehicle;

namespace BharatKaSafar.Obstacles
{
    /// <summary>
    /// Damage obstacle that reduces vehicle health on collision.
    ///
    /// Attach to any GameObject with a Collider2D (set as Trigger or solid).
    /// Examples: rocks, potholes, barriers, fallen debris.
    ///
    /// The collision is handled by OnCollisionEnter2D (solid collider)
    /// or OnTriggerEnter2D (trigger collider) — configurable below.
    ///
    /// Important: Does NOT instantly fail the mission. Vehicle Health handles
    /// death when health reaches 0 (BKS_MissionResult listens to OnVehicleDead).
    /// </summary>
    public class BKS_DamageObstacle : MonoBehaviour
    {
        [Header("Damage Config")]
        [Tooltip("Damage to apply to vehicle health on contact")]
        [SerializeField] private float damageAmount = 15f;

        [Tooltip("Use trigger collider (OnTriggerEnter2D) instead of solid collision")]
        [SerializeField] private bool useTrigger = false;

        [Header("Feedback")]
        [SerializeField] private ParticleSystem impactParticle;
        [SerializeField] private AudioSource    impactAudio;
        [SerializeField] private AudioClip      impactClip;

        [Header("One-shot (disable after first hit)")]
        [Tooltip("If true, obstacle is disabled after dealing damage once")]
        [SerializeField] private bool disableAfterHit = false;

        // ── Collision Detection ───────────────────────────────────────────────────

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (useTrigger) return;
            TryDamage(collision.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!useTrigger) return;
            TryDamage(other.gameObject);
        }

        // ── Damage Logic ──────────────────────────────────────────────────────────

        private void TryDamage(GameObject other)
        {
            // Only damage the player vehicle (works for every collider of the car)
            if (!BKS_VehicleUtil.IsVehicle(other)) return;

            var health = BKS_VehicleUtil.GetHealth(other);
            if (health == null)
            {
                Debug.LogWarning($"[BKS_DamageObstacle] Hit vehicle '{other.name}' but found no BKS_VehicleHealth!");
                return;
            }

            health.TakeDamage(damageAmount);
            Missions.BKS_MissionObjectiveTracker.ReportHit();
            Debug.Log($"[BKS_DamageObstacle] '{gameObject.name}' dealt {damageAmount} damage.");

            PlayImpactFeedback();

            if (disableAfterHit)
                gameObject.SetActive(false);
        }

        // ── Feedback ──────────────────────────────────────────────────────────────

        private void PlayImpactFeedback()
        {
            if (impactParticle != null)
                impactParticle.Play();

            if (impactAudio != null && impactClip != null)
                impactAudio.PlayOneShot(impactClip);
        }

        // ── Editor Gizmos ─────────────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
            var col = GetComponent<Collider2D>();
            if (col != null)
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}
