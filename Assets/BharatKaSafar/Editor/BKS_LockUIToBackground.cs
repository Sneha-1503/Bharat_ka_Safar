using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Stops menu UI from "moving off its place" when the Game view size / zoom changes.
    /// Cause: the background had an AspectRatioFitter (Envelope Parent) that resizes it with the screen in edit mode too,
    /// while the other elements kept fixed offsets — so the art slid under them.
    /// This, for the OPEN scene:
    ///  1. removes the AspectRatioFitter(s) and stretches those images over their parent (the background follows the canvas)
    ///  2. anchors every element to the corners of where it is now (anchors = its rect, offsets 0), so it keeps sitting on
    ///     the same part of the background on any screen. Children of layout groups / scroll lists are left to the layout.
    /// Nothing moves when you run it (positions are kept); Ctrl+Z undoes it.
    /// Menu: Bharat Ka Safar > Responsive > Lock UI To Background (Open Scene)
    /// </summary>
    public static class BKS_LockUIToBackground
    {
        private static readonly string[] MenuScenes =
        {
            "Assets/BharatKaSafar/Scenes/Home/Home.unity", // BKS_StateMap left as it is (reverted on request)
            "Assets/BharatKaSafar/Scenes/BKS_MissionSelect.unity", "Assets/BharatKaSafar/Scenes/BKS_Store.unity",
            "Assets/BharatKaSafar/Scenes/BKS_Challenges.unity", "Assets/BharatKaSafar/Scenes/BKS_SafarDiary.unity",
        };

        [MenuItem("Bharat Ka Safar/Responsive/Lock UI To Background (All Menu Scenes)")]
        public static void RunAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!EditorUtility.DisplayDialog("Lock UI To Background",
                "Home, Mission Select, Store, Challenges and Safar Diary: remove the background's aspect fitter and lock every element to the background, then save. Commit to git first.",
                "Lock + Save", "Cancel")) return;
            foreach (var path in MenuScenes)
            {
                if (!System.IO.File.Exists(path)) continue;
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                Lock(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("[Lock UI] All menu scenes locked and saved.");
        }

        [MenuItem("Bharat Ka Safar/Responsive/Lock UI To Background (Open Scene)")]
        public static void Run()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            Lock(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void Lock(UnityEngine.SceneManagement.Scene scene)
        {
            int fitters = 0, locked = 0;
            Undo.SetCurrentGroupName("Lock UI To Background");
            foreach (var root in scene.GetRootGameObjects())
                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true).Where(c => c.isRootCanvas))
                {
                    foreach (var f in canvas.GetComponentsInChildren<AspectRatioFitter>(true).ToList())
                    {
                        var rt = (RectTransform)f.transform;
                        Undo.RecordObject(rt, "Lock UI");
                        Undo.DestroyObjectImmediate(f);
                        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
                        var img = rt.GetComponent<Image>(); if (img != null) { Undo.RecordObject(img, "Lock UI"); img.preserveAspect = false; }
                        fitters++;
                    }
                    foreach (Transform child in canvas.transform) locked += Lock((RectTransform)child);
                }
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Lock UI] {scene.name}: {fitters} aspect fitter(s) removed, {locked} elements locked to the background (saved).");
        }

        private static int Lock(RectTransform rt)
        {
            if (rt == null) return 0;
            int n = 0;
            var parent = rt.parent as RectTransform;
            bool layoutChild = parent != null && (parent.GetComponent<LayoutGroup>() != null || parent.GetComponent<ScrollRect>() != null);
            bool scrollContent = rt.GetComponentInParent<ScrollRect>(true) != null && rt.GetComponent<ScrollRect>() == null &&
                                 rt.GetComponentInParent<ScrollRect>(true).content != null &&
                                 rt.IsChildOf(rt.GetComponentInParent<ScrollRect>(true).content);
            if (parent != null && !layoutChild && !scrollContent && parent.rect.width > 1f && parent.rect.height > 1f)
            {
                // current corners in the parent's 0..1 space → anchors; offsets 0 (nothing moves now)
                var c = new Vector3[4]; rt.GetWorldCorners(c);
                Vector2 a = parent.InverseTransformPoint(c[0]), b = parent.InverseTransformPoint(c[2]);
                Rect pr = parent.rect;
                var min = new Vector2((a.x - pr.xMin) / pr.width, (a.y - pr.yMin) / pr.height);
                var max = new Vector2((b.x - pr.xMin) / pr.width, (b.y - pr.yMin) / pr.height);
                if (!float.IsNaN(min.x) && !float.IsNaN(max.y))
                {
                    Undo.RecordObject(rt, "Lock UI");
                    var scale = rt.localScale; rt.localScale = Vector3.one;     // anchors describe the unscaled rect
                    if (scale != Vector3.one)
                    {
                        // keep the visible size: fold the scale into the rect around the pivot
                        Vector2 size = (max - min); Vector2 pivot = Vector2.Lerp(min, max, 0.5f);
                        min = pivot - size * 0.5f; max = pivot + size * 0.5f;
                    }
                    rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
                    n++;
                }
            }
            if (rt.GetComponent<LayoutGroup>() != null || rt.GetComponent<ScrollRect>() != null) return n; // the layout owns its children
            foreach (Transform child in rt) n += Lock(child as RectTransform);
            return n;
        }
    }
}
