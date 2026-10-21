using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.Gameplay;
using BharatKaSafar.UI;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Turns the automatic responsiveness OFF everywhere (scenes in Assets/BharatKaSafar/Scenes and the UI prefabs):
    ///  • removes BKS_SafeAreaEdges (moved edge buttons at runtime)
    ///  • removes BKS_CameraBackgroundFit (rescaled the gameplay background whenever the Game view size changed)
    ///  • sets plain, predictable Canvas scaling: gameplay HUD 800x600 match width, menus 1920x1080 match 0.5
    /// After this, what you place in the Scene view stays where you put it.
    /// Menu: Bharat Ka Safar > Responsive > Turn Off Responsive (All Scenes)
    /// </summary>
    public static class BKS_ResponsiveTool
    {
        [MenuItem("Bharat Ka Safar/Responsive/Turn Off Responsive (All Scenes)")]
        public static void TurnOffAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var paths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/BharatKaSafar/Scenes" })
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => !p.Contains("_Backup")).ToList();
            int scenes = 0;
            try
            {
                for (int i = 0; i < paths.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("Turn off responsive", paths[i], i / (float)paths.Count);
                    var scene = EditorSceneManager.OpenScene(paths[i], OpenSceneMode.Single);
                    bool changed = false;
                    foreach (var root in scene.GetRootGameObjects()) changed |= Clean(root);
                    if (changed) { EditorSceneManager.SaveScene(scene); scenes++; }
                }
                foreach (var p in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources", "Assets/BharatKaSafar/Prefabs/UI" }).Select(AssetDatabase.GUIDToAssetPath))
                {
                    var root = PrefabUtility.LoadPrefabContents(p);
                    if (Clean(root)) PrefabUtility.SaveAsPrefabAsset(root, p);
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            Debug.Log($"[Responsive] Turned off in {scenes} scenes. Canvas scaling is now fixed (gameplay 800x600 width, menus 1920x1080).");
        }

        [MenuItem("Bharat Ka Safar/Responsive/Turn Off Responsive (Open Scene)")]
        public static void TurnOffOpen()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            bool changed = false;
            foreach (var root in scene.GetRootGameObjects()) changed |= Clean(root);
            if (changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
            Debug.Log($"[Responsive] Turned off in {scene.name}.");
        }

        private static bool Clean(GameObject root)
        {
            bool changed = false;
            foreach (var c in root.GetComponentsInChildren<BKS_SafeAreaEdges>(true)) { Object.DestroyImmediate(c, true); changed = true; }
            foreach (var c in root.GetComponentsInChildren<BKS_CameraBackgroundFit>(true)) { Object.DestroyImmediate(c, true); changed = true; }
            foreach (var scaler in root.GetComponentsInChildren<CanvasScaler>(true))
            {
                if (scaler.GetComponentInChildren<BKS_GameplayHUD>(true) != null) continue;   // gameplay HUD: scaling is set by hand in Madurai
                if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) continue;
                bool gameplay = scaler.referenceResolution.y <= 700f;
                var res = gameplay ? new Vector2(800f, 600f) : new Vector2(1920f, 1080f);
                float match = gameplay ? 0f : 0.5f;
                if (scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.MatchWidthOrHeight || scaler.referenceResolution != res || !Mathf.Approximately(scaler.matchWidthOrHeight, match))
                {
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.referenceResolution = res;
                    scaler.matchWidthOrHeight = match;
                    EditorUtility.SetDirty(scaler);
                    changed = true;
                }
            }
            return changed;
        }
    }
}
