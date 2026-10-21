using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BharatKaSafar.Gameplay;
using BharatKaSafar.Missions;

namespace BharatKaSafar.Editor
{
    public class BKS_TerrainDifficultyBuilder : EditorWindow
    {
        private BKS_TerrainPlan plan;
        private Vector2 scroll;
        private UnityEditor.Editor inspector;
        private string report;
        [MenuItem("Bharat Ka Safar/Terrain Difficulty Builder")]
        public static void Open() => GetWindow<BKS_TerrainDifficultyBuilder>("Terrain Difficulty");
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("140 plans follow the mission planner's state order. Select a plan to preview its terrain, bake a prefab, or create a gameplay draft copy. Existing authored scenes are preserved.", MessageType.Info);
            var next = (BKS_TerrainPlan)EditorGUILayout.ObjectField("Mission terrain plan", plan, typeof(BKS_TerrainPlan), false);
            if (next != plan) { plan = next; if (inspector != null) DestroyImmediate(inspector); }
            if (GUILayout.Button("Select Tamil Nadu Easy Reference"))
            {
                plan = Plans().FirstOrDefault(p => p.stateName == "Tamil Nadu" && p.missionNumber == 1);
                if (inspector != null) DestroyImmediate(inspector);
            }
            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (plan != null) { if (inspector == null) inspector = UnityEditor.Editor.CreateEditor(plan); inspector.OnInspectorGUI(); }
            EditorGUILayout.EndScrollView();
            using (new EditorGUI.DisabledScope(plan == null || EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Add Terrain Preview to Open Scene")) Run(() =>
                {
                    var root = MakeRoute(plan);
                    Undo.RegisterCreatedObjectUndo(root, "Preview Difficulty Terrain");
                    Selection.activeGameObject = root;
                    SceneView.lastActiveSceneView?.FrameSelected();
                    report = "Preview added. Existing terrain was preserved; use a draft for driving tests.";
                });
                if (GUILayout.Button("Bake Selected Terrain Prefab")) Run(() => report = BakePrefab(plan));
                if (GUILayout.Button("Create Playable Terrain Draft Copy")) Run(() => report = BuildDraft(plan));
            }
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Bake All 140 Terrain Prefabs")) Run(() =>
                {
                    var plans = Plans(); foreach (var item in plans) BakePrefab(item);
                    report = "Baked " + plans.Length + " terrain prefabs. Existing outputs receive unique filenames.";
                });
                if (GUILayout.Button("Validate 140 Plans and Slopes")) Run(() => report = ValidateCatalogue());
            }
            if (!string.IsNullOrEmpty(report)) EditorGUILayout.HelpBox(report, MessageType.Info);
        }
        private void Run(Action action) { try { action(); } catch (Exception ex) { report = ex.Message; Debug.LogException(ex); } }
        private void OnDisable() { if (inspector != null) DestroyImmediate(inspector); }
        private static BKS_TerrainPlan[] Plans() => AssetDatabase.FindAssets("t:BKS_TerrainPlan")
            .Select(g => AssetDatabase.LoadAssetAtPath<BKS_TerrainPlan>(AssetDatabase.GUIDToAssetPath(g))).ToArray();

        private static GameObject MakeRoute(BKS_TerrainPlan p)
        {
            var root = new GameObject("Terrain " + p.stateName + " M" + p.missionNumber + " " + p.difficulty);
            root.tag = "Ground";
            var route = root.AddComponent<BKS_TerrainRoute>(); route.plan = p; route.Rebuild();
            return root;
        }
        private static string BakePrefab(BKS_TerrainPlan p)
        {
            EnsureFolder("Assets/BharatKaSafar/Prefabs/Terrain/Progression");
            var root = MakeRoute(p);
            try
            {
                string path = AssetDatabase.GenerateUniqueAssetPath("Assets/BharatKaSafar/Prefabs/Terrain/Progression/" + p.name + ".prefab");
                PrefabUtility.SaveAsPrefabAsset(root, path);
                return path;
            }
            finally { DestroyImmediate(root); }
        }
        private static string ScenePath(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return null;
            return AssetDatabase.FindAssets(sceneName + " t:Scene").Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(path => Path.GetFileNameWithoutExtension(path) == sceneName);
        }
        private static string BuildDraft(BKS_TerrainPlan p)
        {
            if (p.mission == null) throw new InvalidOperationException("Assign this plan's Mission Data before building a gameplay draft.");
            string source = ScenePath(p.mission.GameplaySceneName) ?? ScenePath(p.mission.DraftSceneName)
                ?? "Assets/BharatKaSafar/Scenes/Tamil nadu/BKS_TN_M01_Madurai.unity";
            EnsureFolder("Assets/BharatKaSafar/Scenes/TerrainDrafts");
            string target = AssetDatabase.GenerateUniqueAssetPath("Assets/BharatKaSafar/Scenes/TerrainDrafts/" + p.name + ".unity");
            if (!AssetDatabase.CopyAsset(source, target)) throw new InvalidOperationException("Could not copy gameplay template.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return "Cancelled; draft copy retained at " + target;
            var scene = EditorSceneManager.OpenScene(target, OpenSceneMode.Single);
            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            var car = all.Select(t => t.GetComponent<CarController>()).FirstOrDefault(c => c != null && c.gameObject.activeInHierarchy);
            if (car == null) throw new InvalidOperationException("Draft created, but its template has no active CarController. Configure a player before testing: " + target);
            var colliders = car.transform.root.GetComponentsInChildren<Collider2D>();
            float surfaceY = colliders.Where(c => !c.isTrigger).Select(c => c.bounds.min.y).DefaultIfEmpty(car.transform.position.y - 2).Min() - 0.15f;
            // Disable terrain only in the draft, preserving the authored source scene.
            foreach (var t in all)
            {
                if (t.IsChildOf(car.transform.root)) continue;
                if ((t.CompareTag("Ground") || t.CompareTag("rope2D")) && t.GetComponent<Collider2D>() != null)
                    t.gameObject.SetActive(false);
                if (t.GetComponent<TN_TerrainModule>() != null || t.GetComponent<BKS_TerrainRoute>() != null)
                    t.gameObject.SetActive(false);
                // Template pickups/checkpoints are positioned for its old road.
                if (t.GetComponent<ScoreItem>() != null || t.GetComponent<BKS_Checkpoint>() != null ||
                    t.GetComponent<Rewards.BKS_CoinPickup>() != null || t.GetComponent<Obstacles.BKS_SafarStarCollectible>() != null)
                    t.gameObject.SetActive(false);
            }
            var root = MakeRoute(p);
            root.transform.position = new Vector3(car.transform.position.x - 20, surfaceY, 0);
            var route = root.GetComponent<BKS_TerrainRoute>();
            foreach (var manager in all.Select(t => t.GetComponent<GameManager>()).Where(m => m != null))
            {
                manager.targetDistance = Mathf.CeilToInt(route.Finish.x);
                manager.TotalFuel = 100; manager.FuelTime = 60; // Generous fuel for isolated terrain tests.
                if (manager.DistanceSlider != null) manager.DistanceSlider.maxValue = manager.targetDistance;
            }
            foreach (var manager in all.Select(t => t.GetComponent<MissionManager>()).Where(m => m != null))
            {
                var serialized = new SerializedObject(manager);
                serialized.FindProperty("missionData").objectReferenceValue = p.mission;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (var destination in all.Select(t => t.GetComponent<BKS_Destination>()).Where(d => d != null))
                destination.transform.position = new Vector3(route.Finish.x, route.Finish.y, 0);
            foreach (var destination in all.Select(t => t.GetComponent<Rajasthan.BKS_DestinationGate>()).Where(d => d != null))
                destination.transform.position = new Vector3(route.Finish.x, route.Finish.y, 0);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root;
            return "Draft saved: " + target + ". Terrain-only driving test: place new pickups/checkpoints and balance fuel before publishing.";
        }
        public static string ValidateCatalogue()
        {
            var plans = Plans();
            if (plans.Length != 140) throw new InvalidOperationException("Expected 140 terrain plans; found " + plans.Length);
            int[] counts = new int[4];
            foreach (var p in plans)
            {
                if (p.difficulty != BKS_TerrainPlan.DifficultyFor(p.stateOrder, p.missionNumber)) throw new InvalidOperationException("Wrong difficulty: " + p.name);
                if (p.mission == null || p.sections == null || p.sections.Length == 0 || p.sections.Any(s => s == null)) throw new InvalidOperationException("Missing mission/sections: " + p.name);
                counts[(int)p.difficulty]++;
                var points = p.BuildPoints(0.5f);
                float maxSlope = p.sections.Max(s => s.slopeDegrees);
                for (int i = 1; i < points.Length; i++)
                {
                    var delta = points[i] - points[i-1];
                    if (delta.x <= 0 || Mathf.Abs(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg) > maxSlope + 0.15f)
                        throw new InvalidOperationException("Invalid join or excessive slope: " + p.name);
                }
            }
            if (string.Join(",", counts) != "28,56,42,14") throw new InvalidOperationException("Unexpected difficulty distribution.");
            return "PASS: 140 plans; 28 Easy, 56 Medium, 42 Hard, 14 Super Complex. Route joins and slope limits checked. Vehicle playtesting is separate.";
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/')); EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }
    }
}
