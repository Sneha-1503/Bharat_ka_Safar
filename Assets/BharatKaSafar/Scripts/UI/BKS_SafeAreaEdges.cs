using System.Collections.Generic;
using UnityEngine;

namespace BharatKaSafar.UI
{
    /// <summary>
    /// Keeps edge-anchored UI (buttons pinned to a screen edge/corner) out of phone notches and
    /// rounded corners: elements anchored to the left edge move right by the left safe-area inset,
    /// right-anchored ones move left, and so on. Backgrounds and centred elements are untouched.
    /// Put it on the root Canvas (added by Bharat Ka Safar > Responsive tools).
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class BKS_SafeAreaEdges : MonoBehaviour
    {
        private readonly Dictionary<RectTransform, Vector2> original = new Dictionary<RectTransform, Vector2>();
        private Rect lastSafe;
        private Vector2Int lastScreen;
        private Canvas canvas;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            Collect((RectTransform)transform);
        }

        private void Collect(RectTransform parent)
        {
            foreach (RectTransform child in parent)
            {
                bool fullStretch = child.anchorMin == Vector2.zero && child.anchorMax == Vector2.one;
                if (fullStretch) { Collect(child); continue; }        // full-screen container: look inside
                if (child.anchorMin != child.anchorMax) continue;     // other stretched elements manage themselves
                var a = child.anchorMin;
                if (a.x == 0f || a.x == 1f || a.y == 0f || a.y == 1f) original[child] = child.anchoredPosition;
            }
        }

        private void Update()
        {
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.safeArea == lastSafe && screen == lastScreen) return;
            lastSafe = Screen.safeArea;
            lastScreen = screen;
            Apply();
        }

        private void Apply()
        {
            float scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
            Rect s = Screen.safeArea;
            float left = s.xMin / scale, right = (Screen.width - s.xMax) / scale;
            float bottom = s.yMin / scale, top = (Screen.height - s.yMax) / scale;

            foreach (var kv in original)
            {
                if (kv.Key == null) continue;
                Vector2 a = kv.Key.anchorMin, p = kv.Value;
                if (a.x == 0f) p.x += left; else if (a.x == 1f) p.x -= right;
                if (a.y == 0f) p.y += bottom; else if (a.y == 1f) p.y -= top;
                kv.Key.anchoredPosition = p;
            }
        }
    }
}
