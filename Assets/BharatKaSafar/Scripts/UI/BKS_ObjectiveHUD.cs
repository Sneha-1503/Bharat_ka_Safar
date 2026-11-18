using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.Missions;

namespace BharatKaSafar.UI
{
    /// <summary>
    /// Gameplay HUD panel for the mission objective: "Diya 3/5", "Hits 1/3" and, for timed bonuses, the clock.
    /// Only the parts the mission uses are shown, side by side on ONE line; the framed background (9-sliced) grows and
    /// shrinks with the text, and the panel keeps a gap from the other HUD elements (moves down if it would touch one).
    /// Hidden completely when the mission has none.
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

        [Header("One-line layout")]
        [Tooltip("Height of the text line; 0 = measured from the panel when the scene starts")]
        [SerializeField, Min(0f)] private float rowHeight = 0f;
        [Tooltip("Space between the frame and the text (left/right, top/bottom)")]
        [SerializeField] private Vector2 padding = new Vector2(30f, 12f);
        [Tooltip("Space between two parts, e.g. 'Seashell 3/10' and 'Hits 0/3'")]
        [SerializeField, Min(0f)] private float spacing = 28f;
        [SerializeField, Min(0f)] private float iconGap = 8f;
        [Tooltip("Free space kept from every other HUD element")]
        [SerializeField, Min(0f)] private float hudGap = 16f;

        [Header("Formats")]
        [SerializeField] private string collectFormat = "{0} {1}/{2}";
        [SerializeField] private string hitsFormat = "Hits {0}/{1}";
        [SerializeField] private string noHitsFormat = "No hits: {0}";
        [SerializeField] private Color okColor = Color.white;
        [SerializeField] private Color failColor = new Color(1f, 0.4f, 0.3f);
        [SerializeField] private Color doneColor = new Color(0.5f, 1f, 0.45f);

        private BKS_MissionObjectiveTracker tracker;
        private Image background;
        private Vector2 homePos;
        private string lastKey;

        private void Awake()
        {
            background = GetComponent<Image>();
            var rt = (RectTransform)transform;
            if (rowHeight <= 0f) rowHeight = Mathf.Clamp(rt.rect.height / 3f, 32f, 60f);

            // keep the top-left corner where the panel was placed; it grows to the right with the text
            // (BKS_TopBarSpread lines it up right after the other top pieces)
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector3 oldTopLeft = corners[1];
            rt.anchorMin = rt.anchorMax = new Vector2(rt.anchorMin.x, rt.anchorMax.y);
            rt.pivot = new Vector2(0f, 1f);
            rt.GetWorldCorners(corners);
            rt.position += oldTopLeft - corners[1];
            homePos = rt.anchoredPosition;

            foreach (var t in new[] { collectText, hitsText, timeText })
            {
                if (t == null) continue;
                t.enableWordWrapping = false; t.enableAutoSizing = false; t.overflowMode = TextOverflowModes.Overflow;
                t.alignment = TextAlignmentOptions.MidlineLeft;
            }
        }

        private void Update()
        {
            if (tracker == null) tracker = BKS_MissionObjectiveTracker.Instance;
            var m = tracker != null ? tracker.Mission : null;
            bool showCollect = m != null && m.CollectibleCount > 0;
            bool showHits = m != null && m.HitLimit >= 0;
            bool showTime = m != null && m.Bonus == BKS_BonusType.TimeLimit;

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

            // re-layout only when something visible changed
            string key = $"{showCollect}{showHits}{showTime}|{(showCollect && collectText ? collectText.text : "")}|{(showHits && hitsText ? hitsText.text : "")}|{(showTime && timeText ? timeText.text.Length : 0)}|{(collectIcon != null && collectIcon.enabled)}";
            if (key == lastKey) return;
            lastKey = key;
            Layout(showCollect, showHits, showTime);
        }

        /// <summary>The used parts side by side on one line; the panel fits around them and stays clear of other HUD.</summary>
        private void Layout(bool c, bool h, bool t)
        {
            var rt = (RectTransform)transform;
            float x = padding.x;
            int visible = 0;
            x = Place(collectRow, collectText, c ? collectIcon : null, c, x, ref visible);
            x = Place(hitsRow, hitsText, null, h, x, ref visible);
            x = Place(timeRow, timeText, null, t, x, ref visible);

            if (visible == 0) { rt.sizeDelta = Vector2.zero; if (background != null) background.enabled = false; return; }
            rt.sizeDelta = new Vector2(x - spacing + padding.x, rowHeight + padding.y * 2f);
            if (background != null) background.enabled = true;
            // on a canvas with the top bar line-up, that places it (in the row, after the others); else keep clear here
            var root = GetComponentInParent<Canvas>()?.rootCanvas;
            if (root == null || root.GetComponent<BKS_TopBarSpread>() == null) KeepClear(rt);
        }

        private float Place(GameObject row, TMP_Text text, Image icon, bool show, float x, ref int visible)
        {
            if (row == null) return x;
            row.SetActive(show);
            if (!show) return x;
            var r = (RectTransform)row.transform;
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f); r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = new Vector2(x, -padding.y);

            float w = 0f;
            if (icon != null && icon.enabled)
            {
                var ir = icon.rectTransform;
                ir.anchorMin = ir.anchorMax = new Vector2(0f, 0.5f); ir.pivot = new Vector2(0f, 0.5f);
                ir.sizeDelta = Vector2.one * rowHeight; ir.anchoredPosition = Vector2.zero;
                w = rowHeight + iconGap;
            }
            if (text != null)
            {
                float tw = text.GetPreferredValues(text.text).x;
                var tr = text.rectTransform;
                tr.anchorMin = tr.anchorMax = new Vector2(0f, 0.5f); tr.pivot = new Vector2(0f, 0.5f);
                tr.sizeDelta = new Vector2(tw, rowHeight); tr.anchoredPosition = new Vector2(w, 0f);
                w += tw;
            }
            r.sizeDelta = new Vector2(w, rowHeight);
            visible++;
            return x + w + spacing;
        }

        // ── stay clear of buttons / bars / badges on the same canvas ──────────────
        private readonly List<RectTransform> others = new List<RectTransform>();

        private void KeepClear(RectTransform rt)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            canvas = canvas.rootCanvas;
            others.Clear();
            foreach (var g in canvas.GetComponentsInChildren<Graphic>(false))
            {
                if (!g.enabled || g.transform.IsChildOf(transform) || transform.IsChildOf(g.transform)) continue;
                var gr = g.rectTransform;
                // only real HUD pieces: skip full-screen / very large backgrounds and invisible holders
                var cr = (RectTransform)canvas.transform;
                if (gr.rect.width * gr.lossyScale.x > cr.rect.width * cr.lossyScale.x * 0.6f) continue;
                if (g.color.a < 0.05f) continue;
                others.Add(gr);
            }

            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, homePos.y); // x is set by the top bar line-up
            float scale = Mathf.Max(0.0001f, canvas.transform.lossyScale.y);
            for (int pass = 0; pass < 6; pass++)
            {
                Rect me = WorldRect(rt);
                me.xMin -= hudGap * scale; me.xMax += hudGap * scale; me.yMin -= hudGap * scale; me.yMax += hudGap * scale;
                float push = 0f;
                foreach (var o in others)
                {
                    Rect r = WorldRect(o);
                    // only things beside / above it push it down (things below it stay where they are)
                    if (r.Overlaps(me) && r.center.y > me.yMin) push = Mathf.Max(push, me.yMax - r.yMin); // move my top below its bottom
                }
                if (push <= 0f) break;
                rt.position += Vector3.down * push;
            }
        }

        private static readonly Vector3[] corners = new Vector3[4];
        private static Rect WorldRect(RectTransform r)
        {
            r.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
    }
}
