using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.UI;

namespace BharatKaSafar.Rewards
{
    /// <summary>
    /// Distance milestones (added to every mission by MissionManager):
    ///   every 100 m → +10 coins, every 250 m → +25, every 500 m → +50 (only the biggest one when they meet).
    /// Coins go through BKS_RewardManager, so the 2X boost doubles them and the result screen shows them.
    /// Each milestone pays once per run (driving back after a revive does not pay again).
    /// A small TealTravel banner "+25  250 m" pops in at the top of the screen.
    /// </summary>
    public class BKS_DistanceReward : MonoBehaviour
    {
        [System.Serializable]
        public class Tier { public float every = 100f; public int coins = 10; }

        public bool rewardsOn = true;
        [Tooltip("Biggest first. When two tiers meet (e.g. 500 m is also 100 m), only the first pays.")]
        public Tier[] tiers = { new Tier { every = 500f, coins = 50 }, new Tier { every = 250f, coins = 25 }, new Tier { every = 100f, coins = 10 } };
        [Tooltip("World units per metre (the distance HUD uses the same units)")]
        public float unitsPerMetre = 1f;
        public float bannerTime = 1.6f;

        private Transform car;
        private float startX;
        private int paidUpTo;
        private int countedMetres;          // highest milestone (in the smallest step) already paid
        private RectTransform banner;
        private TMP_Text bannerText;
        private Coroutine showing;

        private void Update()
        {
            if (!rewardsOn) return;
            if (car == null)
            {
                var c = FindFirstObjectByType<CarController>();
                if (c == null) return;
                car = c.transform;
                var start = FindFirstObjectByType<StartPoint>();
                startX = start != null ? start.transform.position.x : car.position.x;
                return;
            }

            float metres = (car.position.x - startX) / Mathf.Max(0.01f, unitsPerMetre);
            // challenge stat: every new metre counts once per run
            int whole = Mathf.FloorToInt(metres);
            if (whole > countedMetres) { Challenges.BKS_ChallengeStats.Add(Challenges.BKS_Stat.DistanceDriven, whole - countedMetres); countedMetres = whole; }
            float step = SmallestStep();
            int reached = Mathf.FloorToInt(metres / step);
            while (paidUpTo < reached)
            {
                paidUpTo++;
                float at = paidUpTo * step;
                foreach (var t in tiers)
                {
                    if (t.every <= 0f || Mathf.Abs(Mathf.Repeat(at + 0.01f, t.every)) > 0.1f) continue;
                    Pay(t.coins, at);
                    break;
                }
            }
        }

        private float SmallestStep()
        {
            float s = float.MaxValue;
            foreach (var t in tiers) if (t.every > 0f) s = Mathf.Min(s, t.every);
            return s == float.MaxValue ? 100f : s;
        }

        private void Pay(int coins, float metres)
        {
            var rm = BKS_RewardManager.Instance;
            if (rm == null) return;
            rm.AddCoin(coins);
            if (AudioManager.instance != null) AudioManager.instance.PlayCoinSound();
            int shown = coins * Mathf.Max(1, rm.currentMultiplier);
            Show($"+{shown}   {metres:0} m");
        }

        // ── banner ─────────────────────────────────────────────────────────────
        private void Show(string text)
        {
            if (banner == null && !MakeBanner()) return;
            bannerText.text = text;
            if (showing != null) StopCoroutine(showing);
            showing = StartCoroutine(Pop());
        }

        private IEnumerator Pop()
        {
            banner.gameObject.SetActive(true);
            for (float t = 0f; t < bannerTime; t += Time.unscaledDeltaTime)
            {
                float k = t < 0.2f ? t / 0.2f : t > bannerTime - 0.3f ? (bannerTime - t) / 0.3f : 1f;
                banner.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, k);
                banner.GetComponent<CanvasGroup>().alpha = k;
                yield return null;
            }
            banner.gameObject.SetActive(false);
        }

        private bool MakeBanner()
        {
            var hud = FindFirstObjectByType<BKS_GameplayHUD>(FindObjectsInactive.Include);
            var canvas = hud != null ? hud.GetComponentInParent<Canvas>() : FindFirstObjectByType<Canvas>();
            if (canvas == null) return false;

            var go = new GameObject("BKS_DistanceBanner", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(CanvasGroup));
            go.transform.SetParent(canvas.rootCanvas.transform, false);
            banner = (RectTransform)go.transform;
            banner.anchorMin = banner.anchorMax = new Vector2(0.5f, 0.78f);
            banner.sizeDelta = new Vector2(420f, 84f);
            var img = go.GetComponent<Image>(); img.color = BKS_UIStyle.Teal; img.raycastTarget = false;
            var outline = go.GetComponent<Outline>(); outline.effectColor = BKS_UIStyle.Gold; outline.effectDistance = new Vector2(4f, -4f);

            var t = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            t.transform.SetParent(banner, false);
            var rt = (RectTransform)t.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            bannerText = t.GetComponent<TextMeshProUGUI>();
            BKS_UIStyle.Use(bannerText);
            bannerText.color = BKS_UIStyle.Gold; bannerText.alignment = TextAlignmentOptions.Center;
            bannerText.enableAutoSizing = true; bannerText.fontSizeMin = 18f; bannerText.fontSizeMax = 52f; bannerText.raycastTarget = false;
            go.SetActive(false);
            return true;
        }
    }
}
