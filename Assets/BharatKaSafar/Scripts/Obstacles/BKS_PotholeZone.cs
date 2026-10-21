using UnityEngine;

namespace BharatKaSafar.Obstacles
{
    /// <summary>
    /// Pattern 2 — Pothole / rough patch (not a jump). A trigger over a dip in the road.
    /// Slow (≤ Safe Speed): the car rolls through and earns a Safar Star.
    /// Fast: the wheels drop in — the body is kicked up and speed is lost, more the faster you go.
    /// Shape a real dip in the terrain under it for the best feel; this script adds the bounce either way.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BKS_PotholeZone : MonoBehaviour, IBKS_SafarStarSource
    {
        [Tooltip("Speed (units/s) the car can cross at without trouble")]
        [SerializeField, Min(0.5f)] private float safeSpeed = 6f;
        [Tooltip("Upward kick per unit of speed above Safe Speed")]
        [SerializeField, Min(0f)] private float bouncePerSpeed = 0.9f;
        [SerializeField, Min(0f)] private float maxBounce = 9f;
        [Tooltip("Fraction of speed lost at very high speed")]
        [SerializeField, Range(0f, 0.9f)] private float maxSpeedLoss = 0.45f;
        [Tooltip("Counts as an obstacle hit when crossed too fast")]
        [SerializeField] private bool countsAsHit = true;
        [SerializeField] private AudioClip thudClip;

        private bool handled;

        private void Awake() { GetComponent<Collider2D>().isTrigger = true; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (handled) return;
            var car = Vehicle.BKS_VehicleUtil.GetCar(other.gameObject);
            if (car == null) return;
            handled = true;

            var rb = car.GetComponent<Rigidbody2D>();
            float speed = rb != null ? Mathf.Abs(rb.linearVelocity.x) : 0f;
            if (speed <= safeSpeed)
            {
                BKS_ObstacleUtil.AwardSafarStar(); // crossed carefully
                return;
            }

            float over = speed - safeSpeed;
            if (rb != null)
            {
                var v = rb.linearVelocity;
                v.x *= 1f - Mathf.Clamp01(over / (safeSpeed * 3f)) * maxSpeedLoss;
                v.y += Mathf.Min(maxBounce, over * bouncePerSpeed);
                rb.linearVelocity = v;
                rb.AddTorque(over * 2f * rb.mass, ForceMode2D.Impulse); // nose kicks up a little (car faces right)
            }
            if (thudClip != null) AudioSource.PlayClipAtPoint(thudClip, transform.position);
            if (countsAsHit) Missions.BKS_MissionObjectiveTracker.ReportHit();
        }
    }
}
