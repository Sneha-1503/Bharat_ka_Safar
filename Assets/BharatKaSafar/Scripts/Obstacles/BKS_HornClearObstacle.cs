using System.Collections;
using TMPro;
using UnityEngine;

namespace BharatKaSafar.Obstacles
{
    /// <summary>
    /// Pattern 1 — Road block cleared by the horn (barricade, crate, loose rock pile, fallen log, branch).
    ///  • Horn from the right distance → the obstacle clears the road (slides aside / rolls away / crumbles /
    ///    tips over) and awards a Safar Star.
    ///  • Driving into it → counts a hit, the car slows and takes a little damage; the obstacle is knocked
    ///    out of the way so the car is never stuck.
    /// A "HORN!" hint pulses above it while the car is within horn range.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BKS_HornClearObstacle : MonoBehaviour, IBKS_HornTarget, IBKS_SafarStarSource
    {
        public enum ClearStyle { SlideAside, RollAway, Crumble, TipOver }

        [SerializeField] private ClearStyle clearStyle = ClearStyle.SlideAside;
        [SerializeField, Min(0.1f)] private float clearTime = 0.6f;

        [Header("If the car hits it")]
        [SerializeField, Range(0f, 1f)] private float speedKeptOnHit = 0.55f;
        [SerializeField, Min(0f)] private float damageOnHit = 10f;

        [Header("Horn hint")]
        [SerializeField] private bool showHint = true;
        [Tooltip("Show the hint when the car is this close (should match the horn range)")]
        [SerializeField, Min(1f)] private float hintDistance = 10f;

        [Header("Feedback")]
        [SerializeField] private AudioClip clearClip;
        [SerializeField] private ParticleSystem clearParticle;

        private enum State { Waiting, Cleared, Knocked }
        private State state = State.Waiting;
        private Collider2D[] colliders;
        private SpriteRenderer[] renderers;
        private TextMeshPro hint;
        private Vector3 hintScale;
        private Transform car;

        public bool CanRespondToHorn => state == State.Waiting;

        private void Awake()
        {
            colliders = GetComponentsInChildren<Collider2D>();
            renderers = GetComponentsInChildren<SpriteRenderer>();
            if (showHint) hint = CreateHint();
        }

        private void Update()
        {
            if (hint == null) return;
            if (car == null) car = BKS_ObstacleUtil.FindVehicle();
            bool near = state == State.Waiting && car != null &&
                        transform.position.x > car.position.x && transform.position.x - car.position.x < hintDistance;
            if (hint.gameObject.activeSelf != near) hint.gameObject.SetActive(near);
            if (near) hint.transform.localScale = hintScale * (1f + 0.15f * Mathf.Sin(Time.time * 8f));
        }

        public bool OnHorn(float hornPower)
        {
            if (state != State.Waiting) return false;
            state = State.Cleared;
            BKS_ObstacleUtil.AwardSafarStar();
            StartCoroutine(Clear(knocked: false));
            return true;
        }

        private void OnCollisionEnter2D(Collision2D c) => TryHit(c.gameObject);
        private void OnTriggerEnter2D(Collider2D c) => TryHit(c.gameObject);

        private void TryHit(GameObject other)
        {
            if (state != State.Waiting || !Vehicle.BKS_VehicleUtil.IsVehicle(other)) return;
            state = State.Knocked;
            BKS_ObstacleUtil.PenaliseVehicle(other, speedKeptOnHit, damageOnHit);
            StartCoroutine(Clear(knocked: true));
        }

        private IEnumerator Clear(bool knocked)
        {
            foreach (var c in colliders) c.enabled = false; // never block the car after this
            if (hint != null) hint.gameObject.SetActive(false);
            if (clearClip != null) AudioSource.PlayClipAtPoint(clearClip, transform.position);
            if (clearParticle != null) clearParticle.Play();

            Vector3 p0 = transform.position, s0 = transform.localScale;
            Quaternion r0 = transform.rotation;
            var style = knocked ? ClearStyle.TipOver : clearStyle;
            for (float t = 0f; t < clearTime; t += Time.deltaTime)
            {
                float k = t / clearTime;
                switch (style)
                {
                    case ClearStyle.SlideAside: // off the road, away from the camera
                        transform.position = p0 + new Vector3(0f, 0.8f * k, 0f);
                        transform.localScale = s0 * Mathf.Lerp(1f, 0.6f, k);
                        break;
                    case ClearStyle.RollAway: // log rolls forward off the road
                        transform.position = p0 + new Vector3(2.5f * k, 0.5f * k, 0f);
                        transform.rotation = r0 * Quaternion.Euler(0f, 0f, -360f * k);
                        transform.localScale = s0 * Mathf.Lerp(1f, 0.7f, k);
                        break;
                    case ClearStyle.Crumble: // rock pile collapses
                        transform.localScale = new Vector3(s0.x * (1f + 0.3f * k), s0.y * (1f - k), s0.z);
                        transform.position = p0 + (Vector3)(Random.insideUnitCircle * 0.05f);
                        break;
                    case ClearStyle.TipOver: // falls over backwards
                        transform.rotation = r0 * Quaternion.Euler(0f, 0f, -85f * k);
                        transform.position = p0 + new Vector3(0.6f * k, 0f, 0f);
                        break;
                }
                SetAlpha(1f - k * k);
                yield return null;
            }
            gameObject.SetActive(false);
        }

        private void SetAlpha(float a)
        {
            foreach (var r in renderers) { if (r == null) continue; var c = r.color; c.a = a; r.color = c; }
        }

        private TextMeshPro CreateHint()
        {
            var go = new GameObject("HornHint");
            go.transform.SetParent(transform, false);
            float top = 1.5f;
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null) top = sr.bounds.size.y / Mathf.Max(0.0001f, transform.lossyScale.y) + 0.6f;
            go.transform.localPosition = new Vector3(0f, top, 0f);
            var t = go.AddComponent<TextMeshPro>();
            t.text = "HORN!";
            t.fontSize = 9f;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = new Color(1f, 0.85f, 0.2f);
            t.outlineWidth = 0.25f;
            t.outlineColor = new Color32(80, 30, 0, 255);
            t.sortingOrder = 50;
            // keep the hint readable whatever the obstacle's scale
            Vector3 ls = transform.lossyScale;
            hintScale = new Vector3(1f / Mathf.Max(0.0001f, ls.x), 1f / Mathf.Max(0.0001f, ls.y), 1f);
            go.transform.localScale = hintScale;
            go.SetActive(false);
            return t;
        }
    }
}
