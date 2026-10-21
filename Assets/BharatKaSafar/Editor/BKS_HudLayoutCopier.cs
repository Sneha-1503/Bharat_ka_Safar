using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Copies the gameplay HUD layout (anchors, position, size, pivot of every Canvas element, matched by its
    /// path under the Canvas) from Tamil Nadu Mission 1 (Madurai) to every other mission scene, and copies the
    /// Canvas Scaler settings from Madurai too. Use after fixing the HUD in Madurai.
    /// Menu: Bharat Ka Safar > Responsive > Copy Madurai HUD Layout To All Mission Scenes
    /// </summary>
    public static class BKS_HudLayoutCopier
    {
        private const string Template = "Assets/BharatKaSafar/Scenes/Tamil nadu/BKS_TN_M01_Madurai.unity";

        private struct Layout { public Vector2 aMin, aMax, pos, size, pivot; public Vector3 scale; }

        [MenuItem("Bharat Ka Safar/Responsive/Copy Madurai HUD Layout To All Mission Scenes")]
        public static void CopyAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!EditorUtility.DisplayDialog("HUD layout", "Copy the Madurai HUD layout to every other mission scene?", "Copy", "Cancel")) return;

            var template = EditorSceneManager.OpenScene(Template, OpenSceneMode.Single);
            var canvas = GameplayCanvas(template);
            if (canvas == null) { Debug.LogError("[HUD] No gameplay Canvas in Madurai."); return; }
            var src = canvas.GetComponent<CanvasScaler>();
            var layout = new Dictionary<string, Layout>();
            var byName = new Dictionary<string, Layout>();
            foreach (var rt in canvas.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt == canvas.transform) continue;
                var l = new Layout { aMin = rt.anchorMin, aMax = rt.anchorMax, pos = rt.anchoredPosition, size = rt.sizeDelta, pivot = rt.pivot, scale = rt.localScale };
                layout[Path(rt, canvas.transform)] = l;
                byName[Path(rt, canvas.transform, false)] = l;   // fallback when sibling order differs
            }

            var paths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/BharatKaSafar/Scenes" }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p != Template && !p.Contains("_Backup") && System.Text.RegularExpressions.Regex.IsMatch(System.IO.Path.GetFileName(p), @"_M\d\d_")).ToList();
            int scenes = 0, elements = 0;
            try
            {
                for (int i = 0; i < paths.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("HUD layout", paths[i], i / (float)paths.Count);
                    var scene = EditorSceneManager.OpenScene(paths[i], OpenSceneMode.Single);
                    var c = GameplayCanvas(scene);
                    if (c == null) continue;
                    var scaler = c.GetComponent<CanvasScaler>();
                    // same scaling as Madurai, otherwise the copied sizes look too big / too small
                    scaler.uiScaleMode = src.uiScaleMode;
                    scaler.screenMatchMode = src.screenMatchMode;
                    scaler.matchWidthOrHeight = src.matchWidthOrHeight;
                    scaler.referenceResolution = src.referenceResolution;
                    foreach (var rt in c.GetComponentsInChildren<RectTransform>(true))
                    {
                        if (rt == c.transform) continue;
                        if (!layout.TryGetValue(Path(rt, c.transform), out var l) && !byName.TryGetValue(Path(rt, c.transform, false), out l)) continue;
                        rt.anchorMin = l.aMin; rt.anchorMax = l.aMax; rt.pivot = l.pivot;
                        rt.anchoredPosition = l.pos; rt.sizeDelta = l.size; rt.localScale = l.scale;
                        elements++;
                    }
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    scenes++;
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            EditorSceneManager.OpenScene(Template, OpenSceneMode.Single);
            Debug.Log($"[HUD] Madurai HUD layout copied to {scenes} scenes ({elements} elements).");
        }

        private static Canvas GameplayCanvas(UnityEngine.SceneManagement.Scene s) =>
            s.GetRootGameObjects().Select(g => g.GetComponent<Canvas>())
             .FirstOrDefault(c => c != null && c.GetComponent<CanvasScaler>() != null && c.GetComponentInChildren<UI.BKS_GameplayHUD>(true) != null)
            ?? s.GetRootGameObjects().Select(g => g.GetComponent<Canvas>()).FirstOrDefault(c => c != null && c.GetComponent<CanvasScaler>() != null);

        private static string Path(Transform t, Transform root, bool withIndex = true)
        {
            var parts = new List<string>();
            for (var x = t; x != null && x != root; x = x.parent) parts.Add(withIndex ? x.name + "#" + x.GetSiblingIndex() : x.name);
            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}
