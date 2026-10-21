using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Every mission shows the result as a popup made from Resources/BKS_MissionResultPopup (not the BKS_MissionResult scene).
    /// This copies the result UI designed in the BKS_MissionResult scene ("MissionResultCanvas") into that popup prefab,
    /// so all 140 missions show the new design. The popup keeps drawing on top of everything (sorting order 32000).
    /// Menu: Bharat Ka Safar > UI > Copy Result Scene Design To Mission Popup
    /// </summary>
    public static class BKS_ResultPopupSync
    {
        private const string ScenePath = "Assets/BharatKaSafar/Scenes/BKS_MissionResult.unity";
        private const string PopupPath = "Assets/Resources/BKS_MissionResultPopup.prefab";

        [MenuItem("Bharat Ka Safar/UI/Copy Result Scene Design To Mission Popup")]
        public static void Copy()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var loaded = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
            bool wasOpen = loaded.IsValid() && loaded.isLoaded;
            var scene = wasOpen ? loaded : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var source = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "MissionResultCanvas");
                if (source == null) { Debug.LogError("[Result] No 'MissionResultCanvas' in BKS_MissionResult."); return; }

                var copy = Object.Instantiate(source);
                copy.name = "BKS_MissionResultPopup";
                var canvas = copy.GetComponent<Canvas>();
                if (canvas != null) { canvas.overrideSorting = true; canvas.sortingOrder = 32000; }
                copy.SetActive(true);
                PrefabUtility.SaveAsPrefabAsset(copy, PopupPath); // same path → same asset, missions keep loading it
                Object.DestroyImmediate(copy);
                Debug.Log("[Result] Result scene design copied to Resources/BKS_MissionResultPopup. All missions use it now.");
            }
            finally { if (!wasOpen) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
