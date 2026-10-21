using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BharatKaSafar.Missions;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Rebuilds the hand-made Rajasthan and Uttar Pradesh mission scenes like the generated ones:
    ///  1. new terrain from the terrain plan (length / difficulty by mission number, state ground)
    ///  2. full level populate (bridges, obstacle patterns, collectibles, coins, fuel, 2X, stage messages,
    ///     memory token + finish flag)
    /// The old hand-made level ([BKS_LevelElements], PhysicItem) is switched OFF and renamed "..._Old", not deleted;
    /// the old token / generated coins are removed. Background, camera, Canvas/HUD, StartPoint, MissionManager stay.
    /// Tamil Nadu is never touched.
    /// Menu: Bharat Ka Safar > Level > Rebuild Rajasthan + UP Scenes (Terrain + Populate)
    /// </summary>
    public static class BKS_HandmadeRebuildTool
    {
        private static readonly string[] Folders = { "Assets/BharatKaSafar/Scenes/Rajasthan", "Assets/BharatKaSafar/Scenes/UP" };
        private static readonly string[] OldOff = { "[BKS_LevelElements]", "PhysicItem" };
        private static readonly string[] OldRemove = { "[BKS_MemoryToken]", "BKS_GeneratedCoins" };

        [MenuItem("Bharat Ka Safar/Level/Rebuild Rajasthan + UP Scenes (Terrain + Populate)")]
        public static void RebuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!EditorUtility.DisplayDialog("Rebuild Rajasthan + UP",
                "Replace the terrain and level of the 10 Rajasthan + UP mission scenes with plan terrain + generated level?\n\n" +
                "The old hand-made level is switched off (kept as '..._Old'). Tamil Nadu is not touched.\nCommit to git first.",
                "Rebuild", "Cancel")) return;

            var missions = AssetDatabase.FindAssets("t:MissionData").Select(g => AssetDatabase.LoadAssetAtPath<MissionData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(m => m != null).ToList();
            var paths = AssetDatabase.FindAssets("t:Scene", Folders).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(p), @"_M\d\d_")).OrderBy(p => p).ToList();

            int done = 0;
            var problems = new List<string>();
            try
            {
                for (int i = 0; i < paths.Count; i++)
                {
                    string name = Path.GetFileNameWithoutExtension(paths[i]);
                    EditorUtility.DisplayProgressBar("Rebuild Rajasthan + UP", name, i / (float)paths.Count);
                    var m = missions.FirstOrDefault(x => x.GameplaySceneName == name || x.DraftSceneName == name);
                    if (m == null) { problems.Add($"{name}: no MissionData uses this scene"); continue; }
                    try
                    {
                        var scene = EditorSceneManager.OpenScene(paths[i], OpenSceneMode.Single);
                        foreach (var g in scene.GetRootGameObjects())
                        {
                            if (OldRemove.Contains(g.name)) Object.DestroyImmediate(g);
                            else if (OldOff.Contains(g.name)) { g.SetActive(false); g.name += "_Old"; }
                        }

                        int level = Mathf.Clamp(m.MissionNumber, 1, 5);
                        if (!BKS_GroundMaterialTool.CreateTerrain(scene, level, 1f, true)) { problems.Add($"{name}: terrain not created"); continue; }
                        string code = BKS_GroundMaterialTool.StateGround.TryGetValue(m.StateName ?? "", out var c) ? c : "R3";
                        BKS_GroundMaterialTool.Apply(scene, code, 70, 15, 1f);

                        if (BKS_LevelPopulator.Populate(scene, m) == null) { problems.Add($"{name}: populate failed"); continue; }
                        EditorSceneManager.MarkSceneDirty(scene);
                        EditorSceneManager.SaveScene(scene);
                        done++;
                    }
                    catch (System.Exception e) { problems.Add($"{name}: {e.GetType().Name} - {e.Message}"); Debug.LogException(e); }
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            Debug.Log($"[Rebuild] Rajasthan + UP: {done}/{paths.Count} scenes rebuilt." +
                      (problems.Count > 0 ? "\nProblems:\n" + string.Join("\n", problems) : ""));
        }
    }
}
