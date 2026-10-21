using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BharatKaSafar.Core;
using BharatKaSafar.Missions;

namespace BharatKaSafar.EditorTools
{
    /// <summary>
    /// Menu: Bharat Ka Safar → Build → Fix Build Settings + Mission Links
    ///
    /// Reads every available ChapterData and makes sure the game can actually load its scenes:
    ///   1. Build Settings: Home (index 0), BKS_MissionResult, every chapter's Mission Select scene
    ///      and every mission's gameplay scene are added and ENABLED. Nothing else is removed.
    ///      (A disabled BKS_MissionResult means no Complete / Failed screen and the game just keeps running.)
    ///   2. Each gameplay scene's MissionManager gets its MissionData assigned, so pressing Play
    ///      directly inside a mission scene also runs the mission (stars, result, unlocks).
    ///
    /// Data-driven: new chapters / missions are picked up automatically. Safe to run again.
    /// </summary>
    public static class BKS_BuildSettingsUtility
    {
        private const string ScenesRoot  = "Assets/BharatKaSafar/Scenes";
        private const string HomeScene   = "Assets/BharatKaSafar/Scenes/Home/Home.unity";
        private const string ResultScene = "Assets/BharatKaSafar/Scenes/BKS_MissionResult.unity";

        [MenuItem("Bharat Ka Safar/Build/Fix Build Settings + Mission Links", priority = 10)]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var report        = new List<string>();
            var sceneIndex    = IndexScenes();
            var required      = new List<string>();
            var missionByPath = new Dictionary<string, MissionData>();

            AddRequired(required, HomeScene, report);
            AddRequired(required, ResultScene, report);

            foreach (string guid in AssetDatabase.FindAssets("t:ChapterData"))
            {
                var chapter = AssetDatabase.LoadAssetAtPath<ChapterData>(AssetDatabase.GUIDToAssetPath(guid));
                if (chapter == null || !chapter.ChapterAvailable) continue;

                string select = Resolve(sceneIndex, chapter.ChapterSceneName, chapter.DisplayName + " → Mission Select", report);
                if (select != null) AddRequired(required, select, report);

                for (int i = 0; i < chapter.MissionCount; i++)
                {
                    MissionData mission = chapter.GetMission(i);
                    if (mission == null)
                    {
                        report.Add($"⚠ {chapter.DisplayName}: mission slot {i + 1} is empty");
                        continue;
                    }

                    string path = Resolve(sceneIndex, mission.GameplaySceneName, mission.MissionID, report);
                    if (path == null) continue;

                    AddRequired(required, path, report);
                    if (!missionByPath.ContainsKey(path)) missionByPath[path] = mission;
                }
            }

            ApplyBuildSettings(required, report);

            // ── Mission links ──
            string startScene = EditorSceneManager.GetActiveScene().path;
            int linked = 0;
            foreach (var kv in missionByPath)
            {
                try
                {
                    if (LinkMission(kv.Key, kv.Value, report)) linked++;
                }
                catch (System.Exception e)
                {
                    report.Add($"⚠ {Path.GetFileNameWithoutExtension(kv.Key)}: {e.Message}");
                }
            }
            report.Add($"✓ MissionData linked in {linked} scene(s), {missionByPath.Count - linked} already correct");

            if (!string.IsNullOrEmpty(startScene) && File.Exists(startScene))
                EditorSceneManager.OpenScene(startScene, OpenSceneMode.Single);

            string text = string.Join("\n", report);
            Debug.Log("[BKS Build Settings]\n" + text);
            EditorUtility.DisplayDialog("Bharat Ka Safar — Build Settings", text, "OK");
        }

        // ── Scenes ────────────────────────────────────────────────────────────────

        /// <summary>Scene file name → paths, for every scene under the Bharat Ka Safar folder.</summary>
        private static Dictionary<string, List<string>> IndexScenes()
        {
            var index = new Dictionary<string, List<string>>();
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { ScenesRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                if (!index.TryGetValue(name, out var list)) index[name] = list = new List<string>();
                list.Add(path);
            }
            return index;
        }

        private static string Resolve(Dictionary<string, List<string>> index, string sceneName, string owner, List<string> report)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                report.Add($"⚠ {owner}: no scene name set");
                return null;
            }
            if (!index.TryGetValue(sceneName.Trim(), out var paths) || paths.Count == 0)
            {
                report.Add($"⚠ {owner}: scene '{sceneName}' not found under {ScenesRoot}");
                return null;
            }
            if (paths.Count > 1)
                report.Add($"⚠ {owner}: '{sceneName}' exists {paths.Count} times — using {paths[0]}");
            return paths[0];
        }

        private static void AddRequired(List<string> required, string path, List<string> report)
        {
            if (!File.Exists(path))
            {
                report.Add($"⚠ Missing scene file: {path}");
                return;
            }
            if (!required.Contains(path)) required.Add(path);
        }

        private static void ApplyBuildSettings(List<string> required, List<string> report)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int added = 0, enabled = 0, disabled = 0;

            foreach (string path in required)
            {
                int i = scenes.FindIndex(s => s.path == path);
                if (i < 0)
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                    added++;
                }
                else if (!scenes[i].enabled)
                {
                    scenes[i] = new EditorBuildSettingsScene(path, true);
                    enabled++;
                }
            }

            // Another ENABLED scene with the same name would be loaded instead of ours — switch it off.
            var requiredNames = new HashSet<string>();
            foreach (string path in required) requiredNames.Add(Path.GetFileNameWithoutExtension(path));
            for (int i = 0; i < scenes.Count; i++)
            {
                var s = scenes[i];
                if (!s.enabled || required.Contains(s.path)) continue;
                if (!requiredNames.Contains(Path.GetFileNameWithoutExtension(s.path))) continue;
                scenes[i] = new EditorBuildSettingsScene(s.path, false);
                disabled++;
                report.Add($"• Disabled duplicate-name scene {s.path}");
            }

            // Home must be the first scene of the game
            int home = scenes.FindIndex(s => s.path == HomeScene);
            if (home > 0)
            {
                var h = scenes[home];
                scenes.RemoveAt(home);
                scenes.Insert(0, h);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            report.Add($"✓ Build Settings: {required.Count} required scenes — {enabled} re-enabled, {added} added, {disabled} duplicates disabled");
        }

        // ── Mission links ─────────────────────────────────────────────────────────

        private static bool LinkMission(string path, MissionData mission, List<string> report)
        {
            string label = Path.GetFileNameWithoutExtension(path);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            var manager = Object.FindFirstObjectByType<MissionManager>(FindObjectsInactive.Include);
            if (manager == null)
            {
                report.Add($"⚠ {label}: no MissionManager in the scene");
                return false;
            }

            var so   = new SerializedObject(manager);
            var prop = so.FindProperty("missionData");
            if (prop == null || prop.objectReferenceValue == mission) return false;

            prop.objectReferenceValue = mission;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return true;
        }
    }
}
