using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BharatKaSafar.Core;
using BharatKaSafar.Missions;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Uses Tamil Nadu Mission 1 (Madurai) as the template for every gameplay scene:
    /// same Canvas/HUD (top-left icons, brake, throttle, horn, pause, 2X, revive …), camera, managers.
    ///
    ///  • Missions WITHOUT a scene: a copy of Madurai is made with the level content removed
    ///    (no terrain yet) and the mission's own background. Saved as a draft — the mission stays
    ///    "Coming Soon" until the scene is linked.
    ///  • Missions WITH a scene (UP, Rajasthan, other Tamil Nadu): the scene is rebuilt from Madurai and
    ///    keeps its own terrain + level elements + start point + target distance. The original is
    ///    backed up first in Scenes/_Backup_BeforeMaduraiCanvas.
    /// </summary>
    public static class BKS_MissionSceneGenerator
    {
        private const string Template = "Assets/BharatKaSafar/Scenes/Tamil nadu/BKS_TN_M01_Madurai.unity";
        private const string ScenesRoot = "Assets/BharatKaSafar/Scenes";
        private const string BackupFolder = ScenesRoot + "/_Backup_BeforeMaduraiCanvas";

        // Level content that belongs to one level (moved from the old scene / removed from the template)
        private static readonly string[] LevelRoots =
            { "Terrain Level0", "[BKS_LevelElements]", "PhysicItems", "PhysicItem", "BKS_GeneratedCoins", "BKS_MissionCollectibles", "StartCollider" };

        [MenuItem("Bharat Ka Safar/Missions/Generate Draft Scenes For New Missions")]
        public static void GenerateDrafts() => Run(createDrafts: true, upgradeExisting: false);

        [MenuItem("Bharat Ka Safar/Missions/Apply Madurai Canvas To Existing Mission Scenes")]
        public static void UpgradeExisting() => Run(createDrafts: false, upgradeExisting: true);

        [MenuItem("Bharat Ka Safar/Missions/Link Draft Scene (Selected Mission)")]
        public static void LinkSelected()
        {
            foreach (var m in Selection.objects.OfType<MissionData>())
            {
                if (string.IsNullOrEmpty(m.DraftSceneName)) { Debug.LogWarning($"{m.MissionName}: no draft scene."); continue; }
                m.LinkDraftSceneForEditor();
                EditorUtility.SetDirty(m);
                string path = AssetDatabase.FindAssets(m.DraftSceneName + " t:Scene").Select(AssetDatabase.GUIDToAssetPath)
                    .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == m.DraftSceneName);
                if (path != null) AddToBuild(path);
                Debug.Log($"[Scenes] {m.MissionName} now plays {m.DraftSceneName}.");
            }
            AssetDatabase.SaveAssets();
        }

        private static void Run(bool createDrafts, bool upgradeExisting)
        {
            if (!File.Exists(Template)) { Debug.LogError("Template scene missing: " + Template); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string msg = upgradeExisting
                ? "Rebuild the existing mission scenes from the Madurai template? Their terrain and level elements are kept; originals are backed up in Scenes/_Backup_BeforeMaduraiCanvas."
                : "Create draft scenes (Madurai Canvas, mission background, no terrain) for every mission that has no scene yet?";
            if (!EditorUtility.DisplayDialog("Mission Scenes", msg, "Go", "Cancel")) return;

            var chapters = AssetDatabase.FindAssets("t:ChapterData")
                .Select(g => AssetDatabase.LoadAssetAtPath<ChapterData>(AssetDatabase.GUIDToAssetPath(g))).ToList();
            var missions = chapters.SelectMany(c => c.Missions).Where(m => m != null).Distinct().ToList();
            var scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { ScenesRoot }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !p.StartsWith(BackupFolder)).ToList();

            int made = 0, upgraded = 0, i = 0;
            try
            {
                foreach (var m in missions)
                {
                    i++;
                    if (EditorUtility.DisplayCancelableProgressBar("Mission Scenes", m.MissionName, i / (float)missions.Count)) break;
                    string existing = string.IsNullOrEmpty(m.GameplaySceneName) ? null
                        : scenePaths.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == m.GameplaySceneName);

                    if (existing == null && createDrafts)
                    {
                        string path = DraftPath(m);
                        if (!File.Exists(path)) { BuildDraft(m, path); made++; }
                        m.SetDraftSceneForEditor(Path.GetFileNameWithoutExtension(path));
                        EditorUtility.SetDirty(m);
                    }
                    else if (existing != null && upgradeExisting)
                    {
                        if (existing == Template) { SetBackgroundInTemplate(m); continue; }
                        Upgrade(m, existing);
                        upgraded++;
                    }
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(Template, OpenSceneMode.Single);
            Debug.Log($"[Scenes] Draft scenes created: {made}. Existing scenes rebuilt with the Madurai Canvas: {upgraded}.");
        }

        private static string DraftPath(MissionData m)
        {
            string state = string.IsNullOrEmpty(m.StateName) ? "Other" : m.StateName;
            string folder = $"{ScenesRoot}/{state}";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(ScenesRoot, state);
            string place = new string((m.PlaceName ?? "Mission").Where(char.IsLetterOrDigit).ToArray());
            return $"{folder}/BKS_{m.ChapterID}_M{m.MissionNumber:00}_{place}.unity";
        }

        private static void BuildDraft(MissionData m, string path)
        {
            AssetDatabase.CopyAsset(Template, path);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var go in Roots(scene).Where(g => LevelRoots.Contains(g.name)).ToList()) Object.DestroyImmediate(go);
            SetBackground(scene, m.BackgroundSprite);
            EditorSceneManager.SaveScene(scene);
        }

        private static void Upgrade(MissionData m, string oldPath)
        {
            // 1. back up the original once
            if (!AssetDatabase.IsValidFolder(BackupFolder)) AssetDatabase.CreateFolder(ScenesRoot, Path.GetFileName(BackupFolder));
            string backup = $"{BackupFolder}/{Path.GetFileName(oldPath)}";
            if (!File.Exists(backup)) AssetDatabase.CopyAsset(oldPath, backup);

            // 2. new scene = Madurai without its level content
            string temp = $"{ScenesRoot}/__temp_upgrade.unity";
            AssetDatabase.DeleteAsset(temp);
            AssetDatabase.CopyAsset(Template, temp);
            var target = EditorSceneManager.OpenScene(temp, OpenSceneMode.Single);
            foreach (var go in Roots(target).Where(g => LevelRoots.Contains(g.name)).ToList()) Object.DestroyImmediate(go);

            // 3. bring the old level content over (from the backup, which is the untouched original)
            var source = EditorSceneManager.OpenScene(backup, OpenSceneMode.Additive);
            foreach (var go in Roots(source).Where(g => LevelRoots.Contains(g.name)).ToList())
                SceneManager.MoveGameObjectToScene(go, target);
            CopyComponentPosition<StartPoint>(source, target);
            CopyTargetDistance(source, target);
            EditorSceneManager.CloseScene(source, true);

            SetBackground(target, m.BackgroundSprite);
            EditorSceneManager.SaveScene(target, oldPath);
            EditorSceneManager.OpenScene(Template, OpenSceneMode.Single);
            AssetDatabase.DeleteAsset(temp);
        }

        private static void SetBackgroundInTemplate(MissionData m)
        {
            var scene = EditorSceneManager.OpenScene(Template, OpenSceneMode.Single);
            if (SetBackground(scene, m.BackgroundSprite)) EditorSceneManager.SaveScene(scene);
        }

        /// <summary>The background is the lowest-ordered SpriteRenderer under the Main Camera ("New Sprite" in Madurai).</summary>
        private static bool SetBackground(Scene scene, Sprite sprite)
        {
            if (sprite == null) return false;
            var cam = Roots(scene).Select(g => g.GetComponent<Camera>()).FirstOrDefault(c => c != null);
            if (cam == null) return false;
            var bg = cam.GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.enabled && r.gameObject.activeSelf)
                .OrderBy(r => r.sortingOrder).FirstOrDefault();
            if (bg == null || bg.sprite == null) return false;
            // keep the same on-screen coverage as the template background
            Vector2 oldSize = Vector2.Scale(bg.sprite.bounds.size, bg.transform.localScale);
            Vector2 newSize = sprite.bounds.size;
            float s = Mathf.Max(oldSize.x / newSize.x, oldSize.y / newSize.y);
            bg.sprite = sprite;
            bg.transform.localScale = new Vector3(s, s, 1f);
            EditorUtility.SetDirty(bg);
            return true;
        }

        private static void CopyComponentPosition<T>(Scene from, Scene to) where T : Component
        {
            var a = Roots(from).SelectMany(g => g.GetComponentsInChildren<T>(true)).FirstOrDefault();
            var b = Roots(to).SelectMany(g => g.GetComponentsInChildren<T>(true)).FirstOrDefault();
            if (a != null && b != null) b.transform.position = a.transform.position;
        }

        private static void CopyTargetDistance(Scene from, Scene to)
        {
            var a = Roots(from).SelectMany(g => g.GetComponentsInChildren<GameManager>(true)).FirstOrDefault();
            var b = Roots(to).SelectMany(g => g.GetComponentsInChildren<GameManager>(true)).FirstOrDefault();
            if (a != null && b != null) { b.targetDistance = a.targetDistance; EditorUtility.SetDirty(b); }
        }

        private static IEnumerable<GameObject> Roots(Scene s) => s.GetRootGameObjects();

        private static void AddToBuild(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == path)) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
