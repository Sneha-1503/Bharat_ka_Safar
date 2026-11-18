using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BharatKaSafar.UI;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Adds BKS_TopBarSpread to the gameplay Canvas (the one with the objective HUD) so the top-left HUD pieces spread
    /// across the screen width when playing. Nothing is moved in the editor; remove the component to undo.
    /// Menus: Bharat Ka Safar > UI > Add Top Bar Spread (Open Scene / All Scenes)
    /// </summary>
    public static class BKS_TopBarSpreadTool
    {
        [MenuItem("Bharat Ka Safar/UI/Add Top Bar Spread (Open Scene)")]
        public static void OpenScene()
        {
            if (AddTo()) EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        [MenuItem("Bharat Ka Safar/UI/Add Top Bar Spread (All Scenes)")]
        public static void AllScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Scene BKS_", new[] { "Assets/BharatKaSafar/Scenes" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/_Backup") || !System.IO.File.ReadAllText(path).Contains("BKS_ObjectiveHUD")) continue;
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                if (AddTo()) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); n++; }
            }
            Debug.Log($"[TopBar] Added to {n} scene(s).");
        }

        private static bool AddTo()
        {
            var hud = Object.FindFirstObjectByType<BKS_ObjectiveHUD>(FindObjectsInactive.Include);
            var canvas = hud != null ? hud.GetComponentInParent<Canvas>(true)?.rootCanvas : null;
            if (canvas == null) { Debug.LogWarning("[TopBar] No gameplay Canvas (with the objective HUD) in this scene."); return false; }
            if (canvas.GetComponent<BKS_TopBarSpread>() != null) return false;
            Undo.AddComponent<BKS_TopBarSpread>(canvas.gameObject);
            return true;
        }
    }
}
