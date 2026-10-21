using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.Missions;

namespace BharatKaSafar.UI
{
    /// <summary>
    /// Gameplay HUD panel for the mission objective: "Diya 3/5", "Hits 1/3" and, for timed bonuses, the clock.
    /// Only the rows the mission uses are shown, stacked from the top, and the dark panel shrinks to fit them
    /// (hidden completely when the mission has none).
    /// </summary>
    public class BKS_ObjectiveHUD : MonoBehaviour
    {
        [SerializeField] private GameObject collectRow;
        [SerializeField] private Image collectIcon;
        [SerializeField] private TMP_Text collectText;
        [SerializeField] private GameObject hitsRow;
        [SerializeField] private TMP_Text hitsText;
        [SerializeField] private GameObject timeRow;
        [SerializeField] private TMP_Text timeText;

        [Header("Auto size")]
        [Tooltip("Height of one row; 0 = measured from the panel when the scene starts")]
        [SerializeField, Min(0f)] private float rowHeight = 0f;
        [SerializeField, Min(0f)] private float padding = 6f;

        [Header("Formats")]
        [SerializeField] private string collectFormat = "{0} {1}/{2}";
        [SerializeField] private string hitsFormat = "Hits {0}/{1}";
        [SerializeField] private string noHitsFormat = "No hits: {0}";
        [SerializeField] private Color okColor = Color.white;
        [SerializeField] private Color failColor = new Color(1f, 0.4f, 0.3f);
        [SerializeField] private Color doneColor = new Color(0.5f, 1f, 0.45f);

        private BKS_MissionObjectiveTracker tracker;
        private Image background;
        private int lastLayout = -1;

        private void Awake()
        {
            background = GetComponent<Image>();
            var rt = (RectTransform)transform;
            if (rowHeight <= 0f) rowHeight = Mathf.Max(20f, rt.rect.height / 3f);

            // Keep the top edge where it is (wherever the panel was placed); height is set from the visible rows
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector3 oldTop = corners[1];
            float width = rt.rect.width;
            float top = rt.anchorMax.y;
            rt.anchorMin = new Vector2(rt.anchorMin.x, top);
            rt.anchorMax = new Vector2(rt.anchorMax.x, top);
            rt.pivot = new Vector2(rt.pivot.x, 1f);
            if (rt.anchorMin.x == rt.anchorMax.x) rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
            rt.GetWorldCorners(corners);
            rt.position += oldTop - corners[1];
            foreach (var row in new[] { collectRow, hitsRow, timeRow })
            {
                if (row == null) continue;
                var r = (RectTransform)row.transform;
                r.anchorMin = new Vector2(0f, 1f); r.anchorMax = new Vector2(1f, 1f); r.pivot = new Vector2(0.5f, 1f);
                r.sizeDelta = new Vector2(0f, rowHeight);
            }
        }

        private void Update()
        {
            if (tracker == null) tracker = BKS_MissionObjectiveTracker.Instance;
            var m = tracker != null ? tracker.Mission : null;
            if (m == null) { Layout(false, false, false); return; }

            bool showCollect = m.CollectibleCount > 0;
            bool showHits = m.HitLimit >= 0;
            bool showTime = m.Bonus == BKS_BonusType.TimeLimit;
            Layout(showCollect, showHits, showTime);

            if (showCollect && collectText != null)
            {
                collectText.text = string.Format(collectFormat, m.CollectibleName, Mathf.Min(tracker.Collected, m.CollectibleCount), m.CollectibleCount);
                collectText.color = tracker.Collected >= m.CollectibleCount ? doneColor : okColor;
                if (collectIcon != null)
                {
                    if (collectIcon.sprite != m.CollectibleSprite) collectIcon.sprite = m.CollectibleSprite;
                    collectIcon.enabled = collectIcon.sprite != null; // no white box when the icon is missing
                }
            }
            if (showHits && hitsText != null)
            {
                int limit = m.HitLimit;
                hitsText.text = limit == 0 ? string.Format(noHitsFormat, tracker.Hits == 0 ? "OK" : "X") : string.Format(hitsFormat, tracker.Hits, limit);
                hitsText.color = tracker.Hits > limit ? failColor : okColor;
            }
            if (showTime && timeText != null)
            {
                float t = tracker.Elapsed;
                timeText.text = $"{(int)(t / 60)}:{(int)(t % 60):00} / {(int)(m.BonusTimeSeconds / 60)}:{(int)(m.BonusTimeSeconds % 60):00}";
                timeText.color = t > m.BonusTimeSeconds ? failColor : okColor;
            }
        }

        /// <summary>Show only the used rows, stacked from the top, and fit the panel to them.</summary>
        private void Layout(bool c, bool h, bool t)
        {
            int key = (c ? 1 : 0) | (h ? 2 : 0) | (t ? 4 : 0);
            if (key == lastLayout) return;
            lastLayout = key;

            int visible = 0;
            visible = Place(collectRow, c, visible);
            visible = Place(hitsRow, h, visible);
            visible = Place(timeRow, t, visible);

            var rt = (RectTransform)transform;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, visible == 0 ? 0f : visible * rowHeight + padding * 2f);
            if (background != null) background.enabled = visible > 0;
        }

        private int Place(GameObject row, bool show, int index)
        {
            if (row == null) return index;
            row.SetActive(show);
            if (!show) return index;
            ((RectTransform)row.transform).anchoredPosition = new Vector2(0f, -padding - index * rowHeight);
            return index + 1;
        }
    }
}
