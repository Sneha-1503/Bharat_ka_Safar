using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BharatKaSafar.UI
{
    /// <summary>
    /// Lines up the top HUD pieces (fuel, coins, distance, stars, 2X timer … and the objective HUD at the end) from the
    /// left safe edge with the same gap between each, so a short objective sits right after the others instead of
    /// leaving an empty stretch. The objective HUD keeps its left edge and grows to the right with its text; when it
    /// would run into the right-side pieces (pause), the gaps shrink to make room.
    /// Items keep their own size; only their x position changes. Put on the gameplay Canvas
    /// (Bharat Ka Safar > UI > Add Top Bar Spread). Leave "items" empty to find them automatically.
    /// </summary>
    public class BKS_TopBarSpread : MonoBehaviour
    {
        [Tooltip("Top bar pieces, left to right (without the objective HUD). Empty = every Canvas child whose top sits in the top band")]
        public List<RectTransform> items = new List<RectTransform>();
        [Tooltip("Pieces at the right end that stay where they are (pause). Empty = found by name")]
        public List<RectTransform> rightSide = new List<RectTransform>();
        [Tooltip("Free space at the left screen edge and before the right-side pieces (canvas units)")]
        public float edgeGap = 24f;
        [Tooltip("Gap between two pieces")]
        public float gap = 28f;
        [Tooltip("Smallest gap when space is short")]
        public float minGap = 10f;
        [Range(0.7f, 0.98f)] public float topBand = 0.86f;

        private RectTransform canvasRt;
        private BKS_ObjectiveHUD objective;
        private Vector2Int lastScreen;
        private Rect lastSafe;
        private float lastObjectiveWidth = -1f;

        private void Start()
        {
            canvasRt = (RectTransform)transform;
            objective = GetComponentInChildren<BKS_ObjectiveHUD>(true);
            if (rightSide.Count == 0)
                foreach (RectTransform c in transform)
                    if (c.name.ToLower().Contains("pause")) rightSide.Add(c);
            if (items.Count == 0)
                foreach (RectTransform c in transform)
                {
                    if (!c.gameObject.activeSelf || rightSide.Contains(c)) continue;
                    if (objective != null && objective.transform.IsChildOf(c)) continue; // the objective holder
                    bool top = c.anchorMin.y >= topBand && c.anchorMax.y >= topBand;
                    bool smallish = (c.anchorMax.x - c.anchorMin.x) < 0.5f; // not a full-width strip
                    if (top && smallish) items.Add(c);
                }
            Spread();
        }

        private void LateUpdate()
        {
            // again when the screen changes, or the objective HUD changes width (text grew / a part appeared)
            float ow = objective != null && objective.gameObject.activeInHierarchy ? ((RectTransform)objective.transform).rect.width : 0f;
            if (lastScreen.x != Screen.width || lastScreen.y != Screen.height || lastSafe != Screen.safeArea || Mathf.Abs(ow - lastObjectiveWidth) > 0.5f)
            {
                lastObjectiveWidth = ow;
                Spread();
            }
        }

        [ContextMenu("Spread Now")]
        public void Spread()
        {
            lastScreen = new Vector2Int(Screen.width, Screen.height); lastSafe = Screen.safeArea;
            if (canvasRt == null) canvasRt = (RectTransform)transform;
            var list = items.Where(i => i != null && i.gameObject.activeInHierarchy).ToList();
            list.Sort((a, b) => Left(a).CompareTo(Left(b)));
            var obj = objective != null ? (RectTransform)objective.transform : null;
            if (obj != null && obj.gameObject.activeInHierarchy && obj.rect.width > 1f) list.Add(obj); // always last
            if (list.Count == 0) return;

            float sw = Mathf.Max(1, Screen.width);
            float start = canvasRt.rect.xMin + Screen.safeArea.xMin / sw * canvasRt.rect.width + edgeGap;
            float end = canvasRt.rect.xMax - (Screen.width - Screen.safeArea.xMax) / sw * canvasRt.rect.width - edgeGap;
            foreach (var r in rightSide)
                if (r != null && r.gameObject.activeInHierarchy) end = Mathf.Min(end, Left(r) - edgeGap);

            // fixed gap; smaller only if everything wouldn't fit before the pause button
            float total = list.Sum(r => Right(r) - Left(r));
            float g = gap;
            if (list.Count > 1 && start + total + g * (list.Count - 1) > end)
                g = Mathf.Max(minGap, (end - start - total) / (list.Count - 1));

            // the objective sits in the same row: its middle at the row's middle
            float rowMid = 0f; int n = 0;
            foreach (var r in list) if (r != obj) { rowMid += Mid(r); n++; }
            if (n > 0) rowMid /= n;

            float x = start;
            foreach (var r in list)
            {
                float w = Right(r) - Left(r);
                float dy = r == obj && n > 0 ? rowMid - Mid(r) : 0f;
                r.position += canvasRt.TransformVector(new Vector3(x - Left(r), dy, 0f));
                x += w + g;
            }
        }

        private static readonly Vector3[] corners = new Vector3[4];
        private float Left(RectTransform r) { r.GetWorldCorners(corners); return canvasRt.InverseTransformPoint(corners[0]).x; }
        private float Mid(RectTransform r) { r.GetWorldCorners(corners); return (canvasRt.InverseTransformPoint(corners[0]).y + canvasRt.InverseTransformPoint(corners[1]).y) / 2f; }
        private float Right(RectTransform r) { r.GetWorldCorners(corners); return canvasRt.InverseTransformPoint(corners[2]).x; }
    }
}
