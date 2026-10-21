using System.Collections;
using UnityEngine;

namespace BharatKaSafar.Obstacles
{
    /// <summary>
    /// Pattern 4 — Moving crossing (cow, goat, camel, handcart).
    /// In side view the road runs across the screen, so "crossing" means coming from the far side of the road
    /// (small, higher, behind) onto the road (normal size) and out to the near side (bigger, lower, in front).
    /// It only blocks while it is ON the road. The player should brake, wait, then go.
    /// Horn makes it hurry (it never disappears). Passing it without a hit awards a Safar Star.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BKS_CrossingObstacle : MonoBehaviour, IBKS_HornTarget, IBKS_SafarStarSource
    {
        [Header("Timing")]
        [Tooltip("Starts walking when the car is this close")]
        [SerializeField, Min(1f)] private float startDistance = 22f;
        [SerializeField, Min(0.1f)] private float enterTime = 1.5f;
        [Tooltip("Time spent standing/walking on the road (blocking)")]
        [SerializeField, Min(0.1f)] private float onRoadTime = 2.5f;
        [SerializeField, Min(0.1f)] private float exitTime = 1.5f;
        [Tooltip("Horn makes it this much faster")]
        [SerializeField, Min(1f)] private float hornSpeedUp = 2.2f;
        [Tooltip("Cross again after this many seconds (0 = only once)")]
        [SerializeField, Min(0f)] private float repeatAfter = 0f;

        [Header("Depth look")]
        [SerializeField] private float farHeight = 1.2f;
        [SerializeField] private float farScale = 0.65f;
        [SerializeField] private float nearDrop = 0.6f;
        [SerializeField] private float nearScale = 1.25f;

        [Header("If the car hits it")]
        [SerializeField, Range(0f, 1f)] private float speedKeptOnHit = 0.4f;
        [SerializeField, Min(0f)] private float damageOnHit = 15f;

        [Header("Feedback")]
        [SerializeField] private AudioClip hornReactionClip;

        private enum Phase { Waiting, Entering, OnRoad, Exiting, Done }
        private Phase phase = Phase.Waiting;
        private Collider2D[] colliders;
        private SpriteRenderer[] renderers;
        private int[] baseOrders;
        private Vector3 roadPos, roadScale;
        private float speed = 1f;
        private bool wasHit, hornUsed;
        private Transform car;

        public bool CanRespondToHorn => phase == Phase.Entering || phase == Phase.OnRoad;

        private void Awake()
        {
            colliders = GetComponentsInChildren<Collider2D>();
            renderers = GetComponentsInChildren<SpriteRenderer>();
            baseOrders = new int[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseOrders[i] = renderers[i].sortingOrder;
            roadPos = transform.position;
            roadScale = transform.localScale;
            SetBlocking(false);
            Pose(0f); // start on the far side
        }

        private void Update()
        {
            if (phase != Phase.Waiting) return;
            if (car == null) car = BKS_ObstacleUtil.FindVehicle();
            if (car != null && transform.position.x > car.position.x && roadPos.x - car.position.x < startDistance)
                StartCoroutine(Cross());
        }

        private IEnumerator Cross()
        {
            wasHit = false; hornUsed = false; speed = 1f;
            phase = Phase.Entering;
            yield return Move(0f, 0.5f, enterTime);

            phase = Phase.OnRoad;
            SetBlocking(true);
            for (float t = 0f; t < onRoadTime; t += Time.deltaTime * speed) yield return null;
            SetBlocking(false);

            phase = Phase.Exiting;
            yield return Move(0.5f, 1f, exitTime);
            phase = Phase.Done;

            // waited for it (car still behind) and never touched it → Safar Star
            if (!wasHit && car != null && car.position.x < roadPos.x) BKS_ObstacleUtil.AwardSafarStar();

            if (repeatAfter > 0f)
            {
                yield return new WaitForSeconds(repeatAfter);
                Pose(0f);
                phase = Phase.Waiting;
            }
            else SetAlpha(0f);
        }

        private IEnumerator Move(float from, float to, float time)
        {
            for (float t = 0f; t < time; t += Time.deltaTime * speed)
            {
                Pose(Mathf.Lerp(from, to, t / time));
                yield return null;
            }
            Pose(to);
        }

        /// <summary>0 = far side, 0.5 = on the road, 1 = near side (gone).</summary>
        private void Pose(float k)
        {
            float y, s;
            if (k <= 0.5f) { float a = k / 0.5f; y = Mathf.Lerp(farHeight, 0f, a); s = Mathf.Lerp(farScale, 1f, a); }
            else { float a = (k - 0.5f) / 0.5f; y = Mathf.Lerp(0f, -nearDrop, a); s = Mathf.Lerp(1f, nearScale, a); }
            transform.position = roadPos + new Vector3(0f, y, 0f);
            transform.localScale = roadScale * s;
            // behind the car while far, in front while near
            int shift = k < 0.4f ? -30 : k > 0.6f ? 30 : 0;
            for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].sortingOrder = baseOrders[i] + shift;
            SetAlpha(k < 0.1f ? k / 0.1f : k > 0.9f ? (1f - k) / 0.1f : 1f);
        }

        public bool OnHorn(float hornPower)
        {
            if (!CanRespondToHorn) return false;
            speed = hornSpeedUp;
            hornUsed = true;
            if (hornReactionClip != null) AudioSource.PlayClipAtPoint(hornReactionClip, transform.position);
            return true;
        }

        private void OnCollisionEnter2D(Collision2D c) => TryHit(c.gameObject);
        private void OnTriggerEnter2D(Collider2D c) => TryHit(c.gameObject);

        private void TryHit(GameObject other)
        {
            if (phase != Phase.OnRoad || wasHit || !Vehicle.BKS_VehicleUtil.IsVehicle(other)) return;
            wasHit = true;
            BKS_ObstacleUtil.PenaliseVehicle(other, speedKeptOnHit, damageOnHit);
            SetBlocking(false);  // let the car through after the bump
            speed = hornSpeedUp; // and get off the road quickly
        }

        // Off the road the collider stays as a trigger so the horn can still find it; on the road it is solid
        private void SetBlocking(bool on)
        {
            foreach (var c in colliders) if (c != null) c.isTrigger = !on;
        }

        private void SetAlpha(float a)
        {
            foreach (var r in renderers) { if (r == null) continue; var c = r.color; c.a = a; r.color = c; }
        }
    }
}
