using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Stops HUD elements drifting when the Game view size / zoom changes.
    /// Each HUD element gets its anchor moved to the corner / edge it is closest to (top-left, top, top-right,
    /// left, center, right, bottom-left, bottom, bottom-right), keeping exactly the position and size it has now.
    ///  • small elements (also small "stretched" ones like the horn / 2X button) become fixed-size, corner-pinned
    ///  • big stretched panels (full screen, top bar, ...) are kept and their children are pinned inside them
    ///  • elements inside a fixed-size element are left alone (they already move with their parent)
    ///  • works inside prefab instances too (e.g. "Top Left Icons"): only the scene override changes, never the prefab file
    /// Menu: Bharat Ka Safar > Responsive > Pin HUD Elements To Nearest Corner (Open Scene)
    /// </summary>
    public static class BKS_HudPinTool
    {
        private const float SmallSpan = 0.5f;

        [MenuItem("Bharat Ka Safar/Responsive/Pin HUD Elements To Nearest Corner (Open Scene)")]
        public static void PinOpenScene()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            int pinned = 0;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                    if (canvas.isRootCanvas) pinned += Pin(canvas.GetComponent<RectTransform>());
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[HUD Pin] {pinned} elements pinned to their nearest corner in {scene.name}. Save the scene (Ctrl+S).");
        }

        // container = something whose size follows the screen (the canvas or a stretched panel)
        private static int Pin(RectTransform container)
        {
            int count = 0;
            foreach (Transform t in container)
            {
                var rt = t as RectTransform;
                if (rt == null) continue;
                Vector2 span = rt.anchorMax - rt.anchorMin;
                bool small = span.x < SmallSpan && span.y < SmallSpan;
                if (small) { if (PinOne(rt, container)) count++; }
                else count += Pin(rt);   // big panel: keep it, pin its children
            }
            return count;
        }

        private static bool PinOne(RectTransform rt, RectTransform parent)
        {
            var pr = parent.rect;
            if (pr.width <= 0f || pr.height <= 0f) return false;
            Vector2 local = (Vector2)parent.InverseTransformPoint(rt.TransformPoint(rt.rect.center));
            var n = new Vector2((local.x - pr.xMin) / pr.width, (local.y - pr.yMin) / pr.height);
            var a = new Vector2(Snap(n.x), Snap(n.y));
            if (rt.anchorMin == a && rt.anchorMax == a) return false;

            Undo.RecordObject(rt, "Pin HUD element");
            Vector3 worldPos = rt.position;
            Vector2 size = rt.rect.size;
            rt.anchorMin = rt.anchorMax = a;
            rt.sizeDelta = size;
            rt.position = worldPos;
            if (PrefabUtility.IsPartOfPrefabInstance(rt)) PrefabUtility.RecordPrefabInstancePropertyModifications(rt);
            EditorUtility.SetDirty(rt);
            return true;
        }

        private static float Snap(float v) => v < 0.34f ? 0f : v > 0.66f ? 1f : 0.5f;
    }
}
