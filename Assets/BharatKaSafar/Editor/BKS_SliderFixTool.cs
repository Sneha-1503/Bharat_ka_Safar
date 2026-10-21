using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Repairs the Sound / Music sliders of the shared audio settings popup (Resources/BKS_AudioSettingsPopup)
    /// after hand edits, without changing how they look:
    ///  • the track = where the "Fill Area" is drawn now (inside the background art)
    ///  • Fill Area and Handle Slide Area are put directly under the Slider, exactly on that track
    ///  • Fill fills the track height and grows from left to right with the value
    ///  • Handle keeps its current size and slides along the track, centred on it
    /// The background art stays where it is.
    /// Menu: Bharat Ka Safar > UI > Fix Settings Sliders
    /// </summary>
    public static class BKS_SliderFixTool
    {
        private const string PrefabPath = "Assets/Resources/BKS_AudioSettingsPopup.prefab";

        [MenuItem("Bharat Ka Safar/UI/Fix Settings Sliders")]
        public static void Fix()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            int n = 0;
            try
            {
                foreach (var s in root.GetComponentsInChildren<Slider>(true))
                    if (FixSlider(s)) n++;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Debug.Log($"[Sliders] {n} slider(s) fixed in {PrefabPath}.");
        }

        private static bool FixSlider(Slider s)
        {
            var sliderRt = (RectTransform)s.transform;
            var fill = s.fillRect; var handle = s.handleRect;
            if (fill == null || handle == null) { Debug.LogWarning($"[Sliders] {s.name}: Fill Rect / Handle Rect not assigned."); return false; }
            var fillArea = fill.parent as RectTransform;
            var handleArea = handle.parent as RectTransform;
            if (fillArea == null || handleArea == null) return false;

            // the track, in the slider's local space: where the Fill Area is drawn now
            Rect track = LocalRect(fillArea, sliderRt);
            // handle size as it looks now
            Vector2 handleSize = WorldSize(handle, sliderRt);

            s.direction = Slider.Direction.LeftToRight;

            Place(fillArea, sliderRt, track);
            Place(handleArea, sliderRt, track);
            fillArea.SetSiblingIndex(sliderRt.childCount - 1);
            handleArea.SetSiblingIndex(sliderRt.childCount - 1);

            // Fill: the slider drives its anchors (x = 0..value, y = 0..1) → no extra size
            fill.pivot = new Vector2(0f, 0.5f);
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(s.normalizedValue, 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;

            // Handle: anchors x = value, y = 0..1 → keep its look with sizeDelta = (width, height - track height)
            handle.pivot = new Vector2(0.5f, 0.5f);
            handle.anchorMin = new Vector2(s.normalizedValue, 0f); handle.anchorMax = new Vector2(s.normalizedValue, 1f);
            handle.sizeDelta = new Vector2(handleSize.x, handleSize.y - track.height);
            handle.anchoredPosition = Vector2.zero;

            EditorUtility.SetDirty(s);
            return true;
        }

        // rect of t expressed in space's local coordinates
        private static Rect LocalRect(RectTransform t, RectTransform space)
        {
            var c = new Vector3[4]; t.GetWorldCorners(c);
            Vector2 a = space.InverseTransformPoint(c[0]), b = space.InverseTransformPoint(c[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        private static Vector2 WorldSize(RectTransform t, RectTransform space) => LocalRect(t, space).size;

        // put t directly under parent, covering r (parent-local)
        private static void Place(RectTransform t, RectTransform parent, Rect r)
        {
            t.SetParent(parent, false);
            t.localScale = Vector3.one; t.localRotation = Quaternion.identity;
            t.anchorMin = t.anchorMax = new Vector2(0.5f, 0.5f);
            t.pivot = new Vector2(0.5f, 0.5f);
            Vector2 parentPivotOffset = (new Vector2(0.5f, 0.5f) - parent.pivot) * parent.rect.size;
            t.sizeDelta = r.size;
            t.anchoredPosition = r.center - parentPivotOffset;
        }
    }
}
