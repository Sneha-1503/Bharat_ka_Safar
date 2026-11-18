using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BharatKaSafar.UI;

namespace BharatKaSafar.Editor
{
    public static class BKS_RestoreBriefingTool
    {
        private const string Current = "Assets/BharatKaSafar/Scenes/BKS_MissionSelect.unity";

        [MenuItem("Bharat Ka Safar/UI/Restore Mission Briefing Panel")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(Current, OpenSceneMode.Single);
            var screen = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<BKS_MissionSelectScreen>(true)).FirstOrDefault();
            if (screen == null) { Debug.LogError("[Briefing] No BKS_MissionSelectScreen in BKS_MissionSelect."); return; }
            var canvas = screen.GetComponentInParent<Canvas>(true);
            if (canvas == null) canvas = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).FirstOrDefault(c => c.isRootCanvas);

            var existing = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<BKS_MissionBriefingPopup>(true)).ToList();
            var popup = BKS_MissionSelectSceneBuilder.BuildBriefing(canvas.rootCanvas.transform);
            foreach (var previous in existing) Object.DestroyImmediate(previous.gameObject);
            popup.transform.SetAsLastSibling();

            var so = new SerializedObject(screen);
            so.FindProperty("briefing").objectReferenceValue = popup;
            so.ApplyModifiedPropertiesWithoutUndo();
            Selection.activeGameObject = popup.gameObject;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Briefing] Mission Briefing panel restored in BKS_MissionSelect and linked. ENTER now opens it again.");
        }
    }
}
