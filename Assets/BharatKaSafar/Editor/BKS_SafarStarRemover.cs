using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BharatKaSafar.Obstacles;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Removes the Safar Star pickups placed inside mission levels (BKS_SafarStarCollectible),
    /// including prefab instances such as BKS_SafarStar_1_Auto. The star tracker itself stays.
    /// Menu: Bharat Ka Safar > Missions > Remove Safar Star Pickups (All Mission Scenes)
    /// </summary>
    public static class BKS_SafarStarRemover
    {
        [MenuItem("Bharat Ka Safar/Missions/Remove Safar Star Pickups (All Mission Scenes)")]
        public static void RemoveAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var paths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/BharatKaSafar/Scenes" })
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => !p.Contains("_Backup")).ToList();
            int removed = 0, scenes = 0;
            try
            {
                for (int i = 0; i < paths.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("Remove Safar Stars", paths[i], i / (float)paths.Count);
                    var scene = EditorSceneManager.OpenScene(paths[i], OpenSceneMode.Single);
                    int n = 0;
                    foreach (var star in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<BKS_SafarStarCollectible>(true)).ToList())
                    {
                        if (star == null) continue; // already removed with its prefab root
                        var root = PrefabUtility.GetOutermostPrefabInstanceRoot(star.gameObject);
                        if (root == null) Object.DestroyImmediate(star.gameObject);
                        else if (root.GetComponentsInChildren<Component>(true).All(c => c is Transform || c is Renderer || c is Collider2D || c is BKS_SafarStarCollectible || c is ParticleSystem || c is AudioSource))
                            Object.DestroyImmediate(root); // the star prefab itself
                        else { Debug.LogWarning($"[Safar Stars] {scene.name}: star inside '{root.name}' left in place (part of a bigger prefab).", root); continue; }
                        n++;
                    }
                    if (n > 0) { EditorSceneManager.SaveScene(scene); removed += n; scenes++; }
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            Debug.Log($"[Safar Stars] Removed {removed} star pickups from {scenes} scenes.");
        }
    }
}
