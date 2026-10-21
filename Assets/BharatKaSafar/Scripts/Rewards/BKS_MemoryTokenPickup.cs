using System.Collections;
using UnityEngine;
using BharatKaSafar.Missions;

namespace BharatKaSafar.Rewards
{
    /// <summary>
    /// The mission's Safar Diary memory token, placed at the destination. Collecting it completes the mission
    /// (the token is then unlocked in the Safar Diary, because the mission is completed).
    /// Uses the current mission's token sprite when the SpriteRenderer has none; glows and floats like collectibles.
    /// The trigger is tall so a jumping car cannot miss it.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class BKS_MemoryTokenPickup : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField, Min(0.5f)] private float iconHeight = 3.5f;
        [SerializeField] private Color glowColor = new Color(1f, 0.9f, 0.4f, 0.85f);
        [SerializeField] private AudioClip collectClip;
        [Tooltip("Short pause so the player sees the token collected before the result appears")]
        [SerializeField, Min(0f)] private float completeDelay = 0.6f;

        private SpriteRenderer glow;
        private Vector3 startPos, baseScale;
        private bool collected;

        private void Start()
        {
            var box = GetComponent<BoxCollider2D>();
            box.isTrigger = true;
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            startPos = transform.position;
            UseMissionSprite();
            glow = new GameObject("Glow").AddComponent<SpriteRenderer>();
            glow.transform.SetParent(spriteRenderer != null ? spriteRenderer.transform : transform, false);
            glow.sprite = BKS_MissionCollectible.GlowSprite;
            glow.color = glowColor;
            if (spriteRenderer != null) glow.sortingOrder = spriteRenderer.sortingOrder - 1;
            FitGlow();
        }

        private void UseMissionSprite()
        {
            if (spriteRenderer == null || spriteRenderer.sprite != null) return;
            var m = BKS_MissionObjectiveTracker.Instance != null ? BKS_MissionObjectiveTracker.Instance.Mission : null;
            if (m == null || m.MemoryTokenSprite == null) return;
            spriteRenderer.sprite = m.MemoryTokenSprite;
            float h = m.MemoryTokenSprite.bounds.size.y;
            if (h > 0f) spriteRenderer.transform.localScale = Vector3.one * (iconHeight / h);
            baseScale = spriteRenderer.transform.localScale;
            FitGlow();
        }

        private void FitGlow()
        {
            if (glow == null || spriteRenderer == null || spriteRenderer.sprite == null) return;
            float local = spriteRenderer.sprite.bounds.size.y * 2f;
            glow.transform.localScale = Vector3.one * (local / BKS_MissionCollectible.GlowSprite.bounds.size.y);
        }

        private void Update()
        {
            if (collected) return;
            UseMissionSprite();
            float t = Time.time;
            transform.position = startPos + Vector3.up * (Mathf.Sin(t * 2f) * 0.3f);
            if (spriteRenderer != null && baseScale != Vector3.zero)
                spriteRenderer.transform.localScale = baseScale * (1f + Mathf.Sin(t * 3f) * 0.06f);
            if (glow != null)
            {
                var c = glowColor; c.a *= 0.6f + 0.4f * Mathf.Sin(t * 3f);
                glow.color = c;
                glow.transform.localRotation = Quaternion.Euler(0f, 0f, t * 25f);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || !Vehicle.BKS_VehicleUtil.IsVehicle(other.gameObject)) return;
            collected = true;
            Challenges.BKS_ChallengeStats.Add(Challenges.BKS_Stat.MemoryTokens);
            if (collectClip != null) AudioSource.PlayClipAtPoint(collectClip, transform.position);
            else if (AudioManager.instance != null) AudioManager.instance.PlayCoinSound();
            var m = BKS_MissionObjectiveTracker.Instance != null ? BKS_MissionObjectiveTracker.Instance.Mission : null;
            if (m != null && !string.IsNullOrEmpty(m.MemoryTokenName))
                UI.BKS_StageBanner.Show("MEMORY UNLOCKED!", m.MemoryTokenName + " - Safar Diary mein jud gaya", 2f);
            StartCoroutine(Complete());
        }

        private IEnumerator Complete()
        {
            // fly up and shrink, then finish the mission
            for (float t = 0f; t < completeDelay; t += Time.deltaTime)
            {
                transform.position += Vector3.up * Time.deltaTime * 4f;
                transform.localScale *= 1f - Time.deltaTime * 1.5f;
                yield return null;
            }
            var mm = FindFirstObjectByType<MissionManager>();
            if (mm != null) mm.CompleteMission();
            gameObject.SetActive(false);
        }
    }
}
