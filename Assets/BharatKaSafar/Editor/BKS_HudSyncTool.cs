using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Makes the gameplay Canvas of every mission scene the same as Madurai's (Tamil Nadu Mission 1):
    ///  • elements that exist in Madurai but not in a scene are CREATED there (same parent, same place)
    ///  • every element gets Madurai's position / size / anchors / scale and on/off state
    ///  • every component is copied: missing ones are added, settings are copied (Image sprite, colour,
    ///    text, font, button, scripts...). References are re-pointed to the same object in that scene
    ///    (e.g. a button OnClick to the HUD); if that object does not exist there, the old value is kept.
    ///  • optional: elements that are NOT in Madurai are deleted
    /// Elements are matched by their path under the Canvas (name, plus a number when siblings share a name).
    /// Menu: Bharat Ka Safar > Responsive > Sync Madurai Canvas To All Mission Scenes
    /// </summary>
    public static class BKS_HudSyncTool
    {
        private const string Template = "Assets/BharatKaSafar/Scenes/Tamil nadu/BKS_TN_M01_Madurai.unity";

        private static readonly HashSet<string> SkipProps = new HashSet<string>
        {
            "m_ObjectHideFlags", "m_CorrespondingSourceObject", "m_PrefabInstance", "m_PrefabAsset",
            "m_GameObject", "m_Script", "m_EditorHideFlags", "m_EditorClassIdentifier",
        };

        [MenuItem("Bharat Ka Safar/Responsive/Sync Madurai Canvas To All Mission Scenes")]
        public static void SyncAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            int choice = EditorUtility.DisplayDialogComplex("Sync Madurai Canvas",
                "Copy Madurai's gameplay Canvas (new elements, components, settings, layout) to every mission scene?\n\n" +
                "Tip: commit to git first, so you can undo.",
                "Sync", "Cancel", "Sync + delete extras");
            if (choice == 1) return;
            bool deleteExtras = choice == 2;

            var tplScene = EditorSceneManager.OpenScene(Template, OpenSceneMode.Single);
            var tplCanvas = GameplayCanvas(tplScene);
            if (tplCanvas == null) { Debug.LogError("[HUD Sync] No gameplay Canvas in Madurai."); return; }

            var paths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/BharatKaSafar/Scenes" }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p != Template && !p.Contains("_Backup") && System.Text.RegularExpressions.Regex.IsMatch(System.IO.Path.GetFileName(p), @"_M\d\d_"))
                .ToList();

            int scenes = 0, created = 0, deleted = 0;
            try
            {
                for (int i = 0; i < paths.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("Sync Madurai Canvas", paths[i], i / (float)paths.Count);
                    var dst = EditorSceneManager.OpenScene(paths[i], OpenSceneMode.Additive);
                    try
                    {
                        var dstCanvas = GameplayCanvas(dst);
                        if (dstCanvas == null) { Debug.LogWarning($"[HUD Sync] {dst.name}: no gameplay Canvas, skipped."); continue; }
                        SceneManager.SetActiveScene(dst);
                        var s = new Syncer(tplScene, dst, tplCanvas.transform, dstCanvas.transform);
                        s.Run(deleteExtras);
                        created += s.Created; deleted += s.Deleted;
                        EditorSceneManager.MarkSceneDirty(dst);
                        EditorSceneManager.SaveScene(dst);
                        scenes++;
                    }
                    catch (System.Exception e) { Debug.LogError($"[HUD Sync] {paths[i]}: {e}"); }
                    finally
                    {
                        SceneManager.SetActiveScene(tplScene);
                        EditorSceneManager.CloseScene(dst, true);
                    }
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            Debug.Log($"[HUD Sync] Madurai Canvas synced to {scenes} scenes ({created} elements created, {deleted} deleted).");
        }

        private class Syncer
        {
            private readonly Scene tplScene, dstScene;
            private readonly Transform tplRoot, dstRoot;
            public int Created, Deleted;

            public Syncer(Scene tplScene, Scene dstScene, Transform tplRoot, Transform dstRoot)
            { this.tplScene = tplScene; this.dstScene = dstScene; this.tplRoot = tplRoot; this.dstRoot = dstRoot; }

            public void Run(bool deleteExtras)
            {
                var keep = new HashSet<Transform>();
                Walk(tplRoot, dstRoot, keep);
                if (deleteExtras) DeleteExtras(dstRoot, keep);
            }

            // tpl and dst are the same element; sync it, then its children (creating missing ones)
            private void Walk(Transform tpl, Transform dst, HashSet<Transform> keep)
            {
                keep.Add(dst);
                SyncNode(tpl, dst, tpl == tplRoot);
                foreach (Transform tc in tpl)
                {
                    var dc = Child(dst, tc.name, Occurrence(tc));
                    if (dc == null)
                    {
                        var copy = Object.Instantiate(tc.gameObject, dst, false);
                        copy.name = tc.name;
                        copy.transform.SetSiblingIndex(Mathf.Min(tc.GetSiblingIndex(), dst.childCount - 1));
                        dc = copy.transform;
                        Created++;
                    }
                    Walk(tc, dc, keep);
                }
            }

            private void SyncNode(Transform tpl, Transform dst, bool isRoot)
            {
                if (!isRoot && dst.gameObject.activeSelf != tpl.gameObject.activeSelf) dst.gameObject.SetActive(tpl.gameObject.activeSelf);
                if (!isRoot && tpl is RectTransform a && dst is RectTransform b)
                {
                    b.anchorMin = a.anchorMin; b.anchorMax = a.anchorMax; b.pivot = a.pivot;
                    b.anchoredPosition = a.anchoredPosition; b.sizeDelta = a.sizeDelta;
                    b.localScale = a.localScale; b.localRotation = a.localRotation;
                }

                var counts = new Dictionary<System.Type, int>();
                foreach (var tc in tpl.GetComponents<Component>())
                {
                    if (tc == null || tc is Transform) continue;          // missing script / layout handled above
                    var type = tc.GetType();
                    counts.TryGetValue(type, out int k); counts[type] = k + 1;
                    var same = dst.GetComponents(type);
                    var dc = k < same.Length ? same[k] : dst.gameObject.AddComponent(type);
                    if (dc == null) continue;
                    CopyComponent(tc, dc);
                }
            }

            private void CopyComponent(Component src, Component dst)
            {
                var so = new SerializedObject(dst);
                var before = new Dictionary<string, Object>();
                var it = so.GetIterator();
                while (it.Next(true)) if (it.propertyType == SerializedPropertyType.ObjectReference) before[it.propertyPath] = it.objectReferenceValue;

                var from = new SerializedObject(src).GetIterator();
                if (from.Next(true))
                    do { if (!SkipProps.Contains(from.name)) so.CopyFromSerializedPropertyIfDifferent(from); }
                    while (from.Next(false));
                so.ApplyModifiedPropertiesWithoutUndo();

                // re-point references that still point into Madurai
                so.Update();
                it = so.GetIterator();
                while (it.Next(true))
                {
                    if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var v = it.objectReferenceValue;
                    var go = v is GameObject g ? g : v is Component c ? c.gameObject : null;
                    if (go == null || go.scene != tplScene) continue;
                    var mapped = Resolve(v);
                    it.objectReferenceValue = mapped != null ? mapped : before.TryGetValue(it.propertyPath, out var old) ? old : null;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                if (PrefabUtility.IsPartOfPrefabInstance(dst)) PrefabUtility.RecordPrefabInstancePropertyModifications(dst);
            }

            // same object (by scene path) in the destination scene
            private Object Resolve(Object v)
            {
                var go = v is GameObject g ? g : ((Component)v).gameObject;
                var target = Find(dstScene, ScenePath(go.transform));
                if (target == null) return null;
                if (v is GameObject) return target.gameObject;
                var type = v.GetType();
                int k = System.Array.IndexOf(go.GetComponents(type), v);
                var list = target.GetComponents(type);
                return k >= 0 && k < list.Length ? list[k] : null;
            }

            private void DeleteExtras(Transform dst, HashSet<Transform> keep)
            {
                for (int i = dst.childCount - 1; i >= 0; i--)
                {
                    var c = dst.GetChild(i);
                    if (keep.Contains(c)) { DeleteExtras(c, keep); continue; }
                    if (PrefabUtility.IsPartOfPrefabInstance(c.gameObject) && !PrefabUtility.IsOutermostPrefabInstanceRoot(c.gameObject))
                    { Debug.LogWarning($"[HUD Sync] {dstScene.name}: '{c.name}' is inside a prefab, not deleted."); continue; }
                    Object.DestroyImmediate(c.gameObject);
                    Deleted++;
                }
            }
        }

        // ── paths ───────────────────────────────────────────────────────────────

        private static int Occurrence(Transform t)
        {
            int n = 0;
            var siblings = t.parent != null ? Enumerable.Range(0, t.parent.childCount).Select(t.parent.GetChild)
                                            : t.gameObject.scene.GetRootGameObjects().Select(g => g.transform);
            foreach (var s in siblings) { if (s == t) return n; if (s.name == t.name) n++; }
            return n;
        }

        private static Transform Child(Transform parent, string name, int occurrence)
        {
            int n = 0;
            foreach (Transform c in parent) if (c.name == name && n++ == occurrence) return c;
            return null;
        }

        private static List<(string, int)> ScenePath(Transform t)
        {
            var parts = new List<(string, int)>();
            for (var x = t; x != null; x = x.parent) parts.Add((x.name, Occurrence(x)));
            parts.Reverse();
            return parts;
        }

        private static Transform Find(Scene scene, List<(string name, int occ)> path)
        {
            int n = 0; Transform cur = null;
            foreach (var r in scene.GetRootGameObjects())
                if (r.name == path[0].name && n++ == path[0].occ) { cur = r.transform; break; }
            for (int i = 1; cur != null && i < path.Count; i++) cur = Child(cur, path[i].name, path[i].occ);
            return cur;
        }

        private static Canvas GameplayCanvas(Scene s) =>
            s.GetRootGameObjects().Select(g => g.GetComponent<Canvas>())
             .FirstOrDefault(c => c != null && c.GetComponent<CanvasScaler>() != null && c.GetComponentInChildren<UI.BKS_GameplayHUD>(true) != null)
            ?? s.GetRootGameObjects().Select(g => g.GetComponent<Canvas>()).FirstOrDefault(c => c != null && c.GetComponent<CanvasScaler>() != null);
    }
}
