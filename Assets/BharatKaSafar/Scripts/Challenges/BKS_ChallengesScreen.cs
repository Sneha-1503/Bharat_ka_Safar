using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.Core;

namespace BharatKaSafar.Challenges
{
    /// <summary>
    /// Challenges screen (scene BKS_Challenges): Daily / Weekly / Overall tabs, a row per challenge with
    /// progress bar, reward and CLAIM, and "Resets in hh:mm:ss" for Daily/Weekly.
    /// </summary>
    public class BKS_ChallengesScreen : MonoBehaviour
    {
        [System.Serializable]
        public class Tab
        {
            public Button button;
            public BKS_ChallengePeriod period;
        }

        [SerializeField] private Tab[] tabs = new Tab[0];
        [SerializeField] private RectTransform listContent;
        [SerializeField] private BKS_ChallengeRowView rowTemplate;
        [SerializeField] private TMP_Text resetText;
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private Button backButton;
        [Header("Tabs (the art is painted on the book: no colour tint)")]
        [Tooltip("Selected tab: glow colour around it")]
        [SerializeField] private Color glowColor = new Color(1f, 0.85f, 0.35f, 0.95f);
        [SerializeField, Min(0f)] private float glowSize = 7f;
        [Tooltip("Selected tab grows a little")]
        [SerializeField, Min(1f)] private float selectedScale = 1.1f;
        [Header("List")]
        [Tooltip("Gap between two challenge strips")]
        [SerializeField, Min(0f)] private float rowSpacing = 12f;

        private readonly List<BKS_ChallengeRowView> rows = new List<BKS_ChallengeRowView>();
        private BKS_ChallengePeriod current;

        private void Start()
        {
            if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
            // strips stacked close together from the top (no stretching over the whole page)
            var layout = listContent != null ? listContent.GetComponent<VerticalLayoutGroup>() : null;
            if (layout != null)
            {
                layout.spacing = rowSpacing;
                layout.childForceExpandHeight = false;
                layout.childControlHeight = false;
                layout.childAlignment = TextAnchor.UpperCenter;
            }
            foreach (var t in tabs) if (t.button != null) tabBaseScale[t.button] = t.button.transform.localScale;
            foreach (var t in tabs)
            {
                var tab = t;
                if (tab.button != null) tab.button.onClick.AddListener(() => Open(tab.period));
            }
            if (backButton != null) backButton.onClick.AddListener(() =>
            {
                if (BKS_SceneLoader.Instance != null) BKS_SceneLoader.Instance.ReturnFromMenu();
                else UnityEngine.SceneManagement.SceneManager.LoadScene(BKS_SceneLoader.SCENE_HOME);
            });
            Open(BKS_ChallengePeriod.Daily);
        }

        private void Update()
        {
            if (resetText != null)
            {
                if (current == BKS_ChallengePeriod.Overall) resetText.text = "Permanent challenges";
                else
                {
                    var t = BKS_ChallengeService.TimeUntilReset(current);
                    resetText.text = current == BKS_ChallengePeriod.Weekly
                        ? $"Resets in {t.Days}d {t.Hours:00}:{t.Minutes:00}:{t.Seconds:00}"
                        : $"Resets in {(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
                }
            }
            if (coinsText != null && BKS_SaveManager.Instance != null) coinsText.text = BKS_SaveManager.Instance.Coins.ToString("N0");
        }

        private void Open(BKS_ChallengePeriod period)
        {
            current = period;
            foreach (var t in tabs) SetTab(t.button, t.period == period);
            Refresh();
        }

        private void Refresh()
        {
            foreach (var r in rows) if (r != null) Destroy(r.gameObject);
            rows.Clear();
            if (rowTemplate == null) return;
            var entries = BKS_ChallengeService.Get(current);
            // ready to claim first, then in progress, claimed last
            entries.Sort((a, b) => Rank(a).CompareTo(Rank(b)));
            foreach (var e in entries)
            {
                var entry = e;
                var row = Instantiate(rowTemplate, listContent);
                row.gameObject.SetActive(true);
                row.Show(entry, () =>
                {
                    if (BKS_ChallengeService.Claim(entry) && AudioManager.instance != null) AudioManager.instance.PlayCoinSound();
                    Refresh();
                });
                rows.Add(row);
            }
        }

        private readonly Dictionary<Button, Vector3> tabBaseScale = new Dictionary<Button, Vector3>();

        // selected = golden glow + a little bigger; others = normal (their own colours, no tint)
        private void SetTab(Button b, bool on)
        {
            if (b == null) return;
            if (b.targetGraphic != null) b.targetGraphic.color = Color.white;
            var glow = b.GetComponent<Outline>();
            if (glow == null) glow = b.gameObject.AddComponent<Outline>();
            glow.effectColor = glowColor;
            glow.effectDistance = new Vector2(glowSize, -glowSize);
            glow.useGraphicAlpha = true;
            glow.enabled = on;
            var baseScale = tabBaseScale.TryGetValue(b, out var s) ? s : Vector3.one;
            b.transform.localScale = on ? baseScale * selectedScale : baseScale;
        }

        private static int Rank(BKS_ChallengeService.Entry e) => e.claimed ? 2 : e.Complete ? 0 : 1;
    }
}
