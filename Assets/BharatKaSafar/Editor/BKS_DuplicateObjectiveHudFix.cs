using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Removes a second, old objective HUD sitting straight under the Canvas (Canvas/BKS_ObjectiveHUD) when the designed one
    /// (Canvas/'hud objective '/BKS_ObjectiveHUD) is also there — it showed the collectible counter twice (AP M01).
    /// Checks every gameplay scene; scenes with only one HUD are not touched.
    /// Menu: Bharat Ka Safar > Level > Remove Duplicate Objective HUD
    /// </summary>
    public static class BKS_DuplicateObjectiveHudFix
    {
        [MenuItem("Bharat Ka Safar/Level/Remove Duplicate Objective HUD")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            int fixedScenes = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Scene BKS_", new[] { "Assets/BharatKaSafar/Scenes" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/_Backup")) continue;
                string text = System.IO.File.ReadAllText(path);
                if (CountOf(text, "m_Name: BKS_ObjectiveHUD\n") + CountOf(text, "m_Name: BKS_ObjectiveHUD\r\n") < 2) continue; // quick skip

                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                bool changed = false;
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (root.GetComponent<Canvas>() == null) continue;
                    var direct = root.transform.Find("BKS_ObjectiveHUD");
                    bool designedToo = false;
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                        if (t.name == "BKS_ObjectiveHUD" && t != direct) { designedToo = true; break; }
                    if (direct != null && designedToo)
                    {
                        Debug.Log($"[ObjectiveHUD] {scene.name}: removed the old duplicate {root.name}/BKS_ObjectiveHUD");
                        Object.DestroyImmediate(direct.gameObject);
                        changed = true;
                    }
                }
                if (changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); fixedScenes++; }
            }
            Debug.Log($"[ObjectiveHUD] Done. Fixed {fixedScenes} scene(s).");
        }

        private static int CountOf(string text, string what)
        {
            int n = 0, i = 0;
            while ((i = text.IndexOf(what, i, System.StringComparison.Ordinal)) >= 0) { n++; i += what.Length; }
            return n;
        }
    }
}
