using UnityEngine;
using BharatKaSafar.Missions;

namespace BharatKaSafar.Rewards
{
    /// <summary>
    /// Mission collectible (Diya, Gem, Dharma Wheel ...). Counts toward the mission objective
    /// ("Collect 5 Diya"). If the SpriteRenderer has no sprite, the current mission's collectible
    /// icon is used, so one prefab works for every mission.
    /// Stands out from coins: soft pulsing glow behind it, gentle float, sway and "breathing" scale.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BKS_MissionCollectible : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("World height of the icon when it is taken from the mission data")]
        [SerializeField, Min(0.1f)] private float autoSpriteHeight = 3f;

        [Header("Motion")]
        [SerializeField] private float floatSpeed = 2f;
        [SerializeField] private float floatHeight = 0.25f;
        [SerializeField] private float swayDegrees = 8f;
        [SerializeField] private float pulseAmount = 0.06f;

        [Header("Glow")]
        [SerializeField] private bool glow = true;
        [SerializeField] private Color glowColor = new Color(1f, 0.85f, 0.3f, 0.75f);
        [Tooltip("Glow size compared with the icon")]
        [SerializeField, Min(1f)] private float glowSize = 1.9f;
        [SerializeField] private float glowPulseSpeed = 3f;

        [Header("Feedback")]
        [SerializeField] private ParticleSystem collectParticle;
        [SerializeField] private AudioClip collectClip;

        private static Sprite glowSprite;
        /// <summary>Shared soft glow (also used by the memory token at the destination).</summary>
        public static Sprite GlowSprite => glowSprite != null ? glowSprite : (glowSprite = MakeGlowSprite());
        private SpriteRenderer glowRenderer;
        private Vector3 startPos, baseScale;
        private float phase;
        private bool collected;

        private void Start()
        {
            startPos = transform.position;
            phase = Random.value * 10f; // items do not move in sync
            GetComponent<Collider2D>().isTrigger = true;
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            TryUseMissionSprite();
            baseScale = spriteRenderer != null ? spriteRenderer.transform.localScale : Vector3.one;
            if (glow) CreateGlow();
        }

        // The tracker starts with the mission (MissionManager.Start), which may run after this Start.
        private void TryUseMissionSprite()
        {
            if (spriteRenderer == null || spriteRenderer.sprite != null) return;
            var mission = BKS_MissionObjectiveTracker.Instance != null ? BKS_MissionObjectiveTracker.Instance.Mission : null;
            if (mission == null || mission.CollectibleSprite == null) return;
            spriteRenderer.sprite = mission.CollectibleSprite;
            float h = mission.CollectibleSprite.bounds.size.y;
            if (h > 0f) spriteRenderer.transform.localScale = Vector3.one * (autoSpriteHeight / h);
            baseScale = spriteRenderer.transform.localScale;
            if (glowRenderer != null) FitGlow();
        }

        private void CreateGlow()
        {
            if (glowSprite == null) glowSprite = MakeGlowSprite();
            var go = new GameObject("Glow");
            go.transform.SetParent(transform, false);
            glowRenderer = go.AddComponent<SpriteRenderer>();
            glowRenderer.sprite = glowSprite;
            glowRenderer.color = glowColor;
            if (spriteRenderer != null)
            {
                glowRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
                glowRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
            }
            FitGlow();
        }

        private void FitGlow()
        {
            if (glowRenderer == null) return;
            float iconSize = spriteRenderer != null && spriteRenderer.sprite != null ? spriteRenderer.bounds.size.y : autoSpriteHeight;
            float worldScale = Mathf.Max(0.0001f, transform.lossyScale.y);
            glowRenderer.transform.localScale = Vector3.one * (iconSize * glowSize / glowSprite.bounds.size.y / worldScale);
        }

        private void Update()
        {
            if (collected) return;
            TryUseMissionSprite();
            float t = Time.time + phase;
            transform.position = startPos + Vector3.up * (Mathf.Sin(t * floatSpeed) * floatHeight);
            if (spriteRenderer != null)
            {
                spriteRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * floatSpeed * 0.7f) * swayDegrees);
                spriteRenderer.transform.localScale = baseScale * (1f + Mathf.Sin(t * glowPulseSpeed) * pulseAmount);
            }
            if (glowRenderer != null)
            {
                float k = 0.5f + 0.5f * Mathf.Sin(t * glowPulseSpeed);
                var c = glowColor; c.a = glowColor.a * Mathf.Lerp(0.45f, 1f, k);
                glowRenderer.color = c;
                glowRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, t * 20f);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || !Vehicle.BKS_VehicleUtil.IsVehicle(other.gameObject)) return;
            collected = true;
            BKS_MissionObjectiveTracker.ReportCollectible();

            if (collectParticle != null) collectParticle.Play();
            if (collectClip != null) AudioSource.PlayClipAtPoint(collectClip, transform.position);
            else if (AudioManager.instance != null) AudioManager.instance.PlayCoinSound();

            if (spriteRenderer != null) spriteRenderer.enabled = false;
            if (glowRenderer != null) glowRenderer.enabled = false;
            Destroy(gameObject, 0.5f);
        }

        /// <summary>Soft round glow with a few faint rays, generated once at runtime (no extra art needed).</summary>
        private static Sprite MakeGlowSprite()
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float c = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - c) / c, dy = (y - c) / c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float soft = Mathf.Clamp01(1f - d);
                    float rays = 0.25f * Mathf.Pow(Mathf.Abs(Mathf.Cos(Mathf.Atan2(dy, dx) * 4f)), 8f) * Mathf.Clamp01(1f - d);
                    float a = Mathf.Clamp01(soft * soft + rays);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
