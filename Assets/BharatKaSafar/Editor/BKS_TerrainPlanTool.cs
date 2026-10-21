using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BharatKaSafar.Missions;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Rebuilds mission scenes from the terrain plan (BKS_TerrainPlanData): new terrain (5 zones, T1–T10 recipe,
    /// state road per zone) and then the normal Populate (bridges from the 35-mission list, obstacles, coins, token, flag).
    /// Obstacle scripts/prefabs are not changed — Populate only places them again on the new ground.
    /// All 140 missions (Tamil Nadu too). In hand-made scenes (Rajasthan, UP, Tamil Nadu) the old level is switched off, not deleted;
    /// the Canvas / HUD, background, camera, StartPoint and MissionManager are never touched.
    /// </summary>
    public static class BKS_TerrainPlanTool
    {
        private static readonly string[] OldOff = { "[BKS_LevelElements]", "PhysicItem", "PhysicItems", "BKS_MissionCollectibles" };
        private static readonly string[] OldRemove = { "[BKS_MemoryToken]", "BKS_GeneratedCoins", "[BKS_Terrain]" };

        [MenuItem("Bharat Ka Safar/Terrain/5. Rebuild Open Scene From Terrain Plan")]
        public static void RebuildOpen()
        {
            var scene = SceneManager.GetActiveScene();
            var m = FindMission(scene.name);
            if (m == null) { Debug.LogWarning($"[Terrain Plan] No MissionData uses scene '{scene.name}'."); return; }
            string report = Rebuild(scene, m);
            if (report == null) return;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Terrain Plan] {scene.name}: {report}");
        }

        [MenuItem("Bharat Ka Safar/Terrain/6. Rebuild ALL Missions From Terrain Plan")]
        public static void RebuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!EditorUtility.DisplayDialog("Rebuild from terrain plan",
                "Rebuild the terrain and level of all 140 mission scenes (Tamil Nadu too) from the terrain plan?\n\n" +
                "Commit to git first. Takes a few minutes.", "Rebuild", "Cancel")) return;

            var paths = AllScenePaths();
            var missions = AllMissions();
            var missionPaths = missions.Select(AssetDatabase.GetAssetPath).ToList();
            int done = 0;
            var problems = new List<string>();
            try
            {
                for (int i = 0; i < missions.Count; i++)
                {
                    // opening a scene unloads unused assets, so load the mission again from its path every time
                    var m = AssetDatabase.LoadAssetAtPath<MissionData>(missionPaths[i]);
                    if (m == null) { problems.Add($"{missionPaths[i]}: missing"); continue; }
                    string sceneName = !string.IsNullOrEmpty(m.GameplaySceneName) ? m.GameplaySceneName : m.DraftSceneName;
                    if (EditorUtility.DisplayCancelableProgressBar("Terrain plan", $"{m.StateName}: {m.PlaceName} ({i + 1}/{missions.Count})", i / (float)missions.Count)) break;
                    if (string.IsNullOrEmpty(sceneName) || !paths.TryGetValue(sceneName, out var path)) { problems.Add($"{m.name}: scene not found"); continue; }
                    try
                    {
                        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                        // OpenScene unloads unused assets (this MissionData too) — load it again now
                        m = AssetDatabase.LoadAssetAtPath<MissionData>(missionPaths[i]);
                        string report = Rebuild(scene, m);
                        if (report == null) { problems.Add($"{sceneName}: rebuild failed"); continue; }
                        EditorSceneManager.SaveScene(scene);
                        done++;
                    }
                    catch (System.Exception e) { problems.Add($"{sceneName}: {e.GetType().Name} - {e.Message}"); Debug.LogException(e); }
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            Debug.Log($"[Terrain Plan] Rebuilt {done}/{missions.Count} mission scenes." +
                      (problems.Count > 0 ? $"\nProblems in {problems.Count}:\n" + string.Join("\n", problems) : ""));
        }

        private static string Rebuild(Scene scene, MissionData m)
        {
            foreach (var g in scene.GetRootGameObjects())
            {
                if (OldRemove.Contains(g.name)) Object.DestroyImmediate(g);
                else if (OldOff.Contains(g.name)) { g.SetActive(false); g.name += "_Old"; }
            }
            if (!BKS_TerrainPlanData.BuildTerrain(scene, m)) return null;
            string populate = BKS_LevelPopulator.Populate(scene, m);
            if (populate == null) return null;
            var recipe = string.Join(" ", BKS_TerrainPlanData.Recipe(m));
            var roads = string.Join(" ", Enumerable.Range(0, 5).Select(z => BKS_TerrainPlanData.ZoneRoad(m, z)));
            return $"recipe {recipe} · roads {roads} · {populate}";
        }

        private static List<MissionData> AllMissions() =>
            AssetDatabase.FindAssets("t:MissionData").Select(g => AssetDatabase.LoadAssetAtPath<MissionData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(m => m != null && !string.IsNullOrEmpty(m.StateName)).OrderBy(m => m.StateName).ThenBy(m => m.MissionNumber).ToList();

        private static MissionData FindMission(string sceneName) =>
            AllMissions().FirstOrDefault(m => m.GameplaySceneName == sceneName || m.DraftSceneName == sceneName);

        private static Dictionary<string, string> AllScenePaths() =>
            AssetDatabase.FindAssets("t:Scene", new[] { "Assets/BharatKaSafar/Scenes" }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !p.Contains("_Backup")).GroupBy(p => Path.GetFileNameWithoutExtension(p)).ToDictionary(g => g.Key, g => g.First());
    }
}
