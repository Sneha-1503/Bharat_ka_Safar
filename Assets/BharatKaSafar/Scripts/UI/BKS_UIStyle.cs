using TMPro;
using UnityEngine;

namespace BharatKaSafar.UI
{
    /// <summary>
    /// One place for the game's look (TealTravel): colours and the main font, so every panel matches.
    /// Font: Electronic Highway Sign SDF (TextMesh Pro "Fonts & Materials" Resources folder).
    /// </summary>
    public static class BKS_UIStyle
    {
        public static readonly Color Teal      = new Color(0.04f, 0.42f, 0.50f, 0.96f);
        public static readonly Color TealDark  = new Color(0.02f, 0.25f, 0.31f, 0.96f);
        public static readonly Color Gold      = new Color(1.00f, 0.80f, 0.30f, 1f);
        public static readonly Color Parchment = new Color(0.98f, 0.92f, 0.76f, 1f);
        public static readonly Color Ink       = new Color(0.32f, 0.18f, 0.05f, 1f);

        private static TMP_FontAsset font;
        public static TMP_FontAsset Font
        {
            get
            {
                if (font == null) font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Electronic Highway Sign SDF");
                return font;
            }
        }

        /// <summary>
        /// Make a popup independent of the scene's Canvas: its own overlay Canvas (1920x1080, matched to the screen height) so it looks the
        /// same on every device and in every scene, drawn above everything at <paramref name="order"/>.
        /// </summary>
        public static void MakeOverlay(GameObject popup, int order)
        {
            if (popup == null) return;
            popup.transform.SetParent(null, false);
            var rt = popup.GetComponent<RectTransform>();
            var canvas = popup.GetComponent<Canvas>(); if (canvas == null) canvas = popup.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.overrideSorting = true; canvas.sortingOrder = order;
            var scaler = popup.GetComponent<UnityEngine.UI.CanvasScaler>(); if (scaler == null) scaler = popup.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f; // landscape: height decides → same look on every phone / tablet
            if (popup.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null) popup.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            if (rt != null) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; rt.localScale = Vector3.one; }
        }

        /// <summary>Every popup frame is this tall on its 1080-high canvas (≈70% of the screen height)</summary>
        public const float PopupFrameHeight = 760f;

        /// <summary>
        /// Same size for every popup on every device: own overlay canvas that follows the screen height, and the frame
        /// (named, or else the biggest non-fullscreen child) scaled to <see cref="PopupFrameHeight"/>. Returns the scale used.
        /// </summary>
        public static float FitPopup(GameObject popup, int order, string frameName = null)
        {
            if (popup == null) return 1f;
            MakeOverlay(popup, order);
            var frame = FindFrame(popup.transform, frameName);
            if (frame == null) return 1f;
            // height in canvas units worked out from the anchors (the popup canvas is 1080 high): the layout may not have run yet
            float h = frame.parent == popup.transform ? (frame.anchorMax.y - frame.anchorMin.y) * 1080f + frame.sizeDelta.y : frame.rect.height;
            if (h < 1f) return 1f;
            // a frame picture kept in proportion is drawn shorter than its box when the box is wide: size by the picture
            var img = frame.GetComponent<UnityEngine.UI.Image>();
            if (img != null && img.preserveAspect && img.sprite != null && img.type == UnityEngine.UI.Image.Type.Simple)
            {
                float w = frame.parent == popup.transform ? (frame.anchorMax.x - frame.anchorMin.x) * CanvasWidth(popup) + frame.sizeDelta.x : frame.rect.width;
                float aspect = img.sprite.rect.width / img.sprite.rect.height;
                h = Mathf.Min(h, w / aspect);
            }
            float s = PopupFrameHeight / h;
            frame.localScale = Vector3.one * s;
            return s;
        }

        // width of a 1080-high popup canvas on this screen
        private static float CanvasWidth(GameObject popup) => 1080f * Screen.width / Mathf.Max(1f, Screen.height);

        public static RectTransform FindFrame(Transform root, string frameName)
        {
            RectTransform best = null; float bestArea = 0f;
            foreach (var t in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (t == root) continue;
                if (!string.IsNullOrEmpty(frameName)) { if (t.name == frameName) return t; continue; }
                if (t.parent != root) continue;
                if (t.anchorMin == Vector2.zero && t.anchorMax == Vector2.one) continue; // dim background
                float a = t.rect.width * t.rect.height;
                if (a > bestArea) { bestArea = a; best = t; }
            }
            return best;
        }

        /// <summary>A title on one line that shrinks to fit its box instead of wrapping out of it.</summary>
        public static void OneLine(TMP_Text t, float min = 14f)
        {
            if (t == null) return;
            t.enableWordWrapping = false; t.enableAutoSizing = true; t.fontSizeMin = min;
            t.overflowMode = TextOverflowModes.Ellipsis;
        }

        /// <summary>Apply the game font to a text (keeps everything else).</summary>
        public static void Use(TMP_Text t) { if (t != null && Font != null) t.font = Font; }
    }
}
