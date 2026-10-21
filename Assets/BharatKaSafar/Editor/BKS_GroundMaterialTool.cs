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
    /// Road / ground materials for the 2D Terrain Editor terrains.
    ///  1. Build Ground Materials → for every folder in Art/Terrain/Ground (R1…R10, E1…E6) makes
    ///     &lt;code&gt;_Ground.mat (fill, tiles both ways) and &lt;code&gt;_Cap.mat (top strip).
    ///  2. Apply Ground (Open Scene) → pick a material in the window.
    ///  3. Apply State Grounds (All Mission Scenes) → each mission scene gets its state's ground (table below).
    /// </summary>
    public class BKS_GroundMaterialTool : EditorWindow
    {
        private const string Root = "Assets/BharatKaSafar/Art/Terrain/Ground";

        // Default ground per state (biome). Change freely.
        internal static readonly Dictionary<string, string> StateGround = new Dictionary<string, string>
        {
            {"Uttar Pradesh","R3"},{"Bihar","R3"},{"Punjab","R3"},{"Haryana","R3"},{"Madhya Pradesh","R3"},{"West Bengal","R3"},
            {"Rajasthan","R8"},{"Gujarat","R8"},
            {"Tamil Nadu","R6"},{"Goa","R6"},{"Odisha","R6"},
            {"Kerala","R7"},{"Chhattisgarh","R7"},{"Jharkhand","R7"},{"Tripura","R7"},
            {"Karnataka","R4"},{"Maharashtra","R4"},
            {"Telangana","E3"},{"Andhra Pradesh","E4"},
            {"Assam","R9"},
            {"Meghalaya","E1"},{"Mizoram","E1"},{"Manipur","E6"},{"Nagaland","E6"},
            {"Arunachal Pradesh","R5"},{"Himachal Pradesh","R5"},{"Uttarakhand","R5"},
            {"Sikkim","R10"},
        };

        private string[] codes = new string[0];
        private int pick;
        // Same values as the hand-made Madurai terrain
        private int fillTiling = 70, capTiling = 15;
        private float capHeight = 1f;
        private int level = 1;              // mission number 1-5 -> length and difficulty from the terrain plan
        private float challenge = 1f;       // extra multiplier on hill sizes
        private bool replaceExisting = true;
        private static readonly int[] PlanLength = { 1000, 1100, 1200, 1400, 1500 }; // terrain plan: L4 = 1400

        [MenuItem("Bharat Ka Safar/Terrain/1. Build Ground Materials")]
        public static void BuildMaterials()
        {
            int n = 0;
            foreach (var dir in Directory.GetDirectories(Root))
            {
                string folder = dir.Replace('\\', '/');
                string code = Path.GetFileName(folder).Split('_')[0];
                var fill = Prepare($"{folder}/{code}_fill.png");
                var cap = Prepare($"{folder}/{code}_cap.png");
                if (fill == null || cap == null) { Debug.LogWarning("[Ground] Missing textures in " + folder); continue; }
                MakeMaterial($"{folder}/{code}_Ground.mat", fill);
                MakeMaterial($"{folder}/{code}_Cap.mat", cap);
                n++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Ground] {n} ground materials ready in {Root}.");
        }

        [MenuItem("Bharat Ka Safar/Terrain/2. Apply Ground (Open Scene)")]
        public static void OpenWindow() => GetWindow<BKS_GroundMaterialTool>("Ground");

        [MenuItem("Bharat Ka Safar/Terrain/3. Apply State Grounds (All Mission Scenes)")]
        public static void ApplyAllScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var missions = AssetDatabase.FindAssets("t:MissionData").Select(g => AssetDatabase.LoadAssetAtPath<MissionData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(m => m != null && !string.IsNullOrEmpty(m.StateName)).ToList();
            var sceneToState = new Dictionary<string, string>();
            foreach (var m in missions)
            {
                if (!string.IsNullOrEmpty(m.GameplaySceneName)) sceneToState[m.GameplaySceneName] = m.StateName;
                if (!string.IsNullOrEmpty(m.DraftSceneName)) sceneToState[m.DraftSceneName] = m.StateName;
            }
            var paths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/BharatKaSafar/Scenes" }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !p.Contains("_Backup") && sceneToState.ContainsKey(Path.GetFileNameWithoutExtension(p))).ToList();
            int done = 0;
            try
            {
                for (int i = 0; i < paths.Count; i++)
                {
                    string state = sceneToState[Path.GetFileNameWithoutExtension(paths[i])];
                    EditorUtility.DisplayProgressBar("Ground", $"{state}: {Path.GetFileName(paths[i])}", i / (float)paths.Count);
                    if (!StateGround.TryGetValue(state, out var code)) continue;
                    var scene = EditorSceneManager.OpenScene(paths[i], OpenSceneMode.Single);
                    if (Apply(scene, code, 70, 15, 1f) > 0) { EditorSceneManager.SaveScene(scene); done++; }
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            Debug.Log($"[Ground] State grounds applied to {done} scenes (scenes without a terrain are skipped).");
        }

        /// <summary>
        /// Every mission scene that has no terrain yet (the 125 drafts) gets a terrain:
        /// level = mission number (1-5, sets length + difficulty), ground = its state's ground.
        /// Scenes that already have a terrain (the hand-made ones) are left alone.
        /// </summary>
        [MenuItem("Bharat Ka Safar/Terrain/4. Create Terrain In All Draft Scenes")]
        public static void CreateAllDraftTerrains()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            int choice = EditorUtility.DisplayDialogComplex("Create terrains",
                "Create a terrain (plan length + difficulty, state ground) in every mission scene that has none?\n\n" +
                "Also make those missions playable in Mission Select? (They have no obstacles/collectibles yet.)",
                "Create + make playable", "Cancel", "Create only");
            if (choice == 1) return;
            bool link = choice == 0;

            var missions = AssetDatabase.FindAssets("t:MissionData").Select(g => AssetDatabase.LoadAssetAtPath<MissionData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(m => m != null && !string.IsNullOrEmpty(m.DraftSceneName) && !m.IsPlayable).ToList();
            var scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/BharatKaSafar/Scenes" }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !p.Contains("_Backup")).ToDictionary(p => Path.GetFileNameWithoutExtension(p), p => p);
            int made = 0, linked = 0;
            try
            {
                for (int i = 0; i < missions.Count; i++)
                {
                    var m = missions[i];
                    if (EditorUtility.DisplayCancelableProgressBar("Create terrains", $"{m.StateName}: {m.PlaceName}", i / (float)missions.Count)) break;
                    if (!scenePaths.TryGetValue(m.DraftSceneName, out var path)) continue;
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    bool hasTerrain = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TerrainEditor2D>(true)).Any();
                    if (!hasTerrain && CreateTerrain(scene, Mathf.Clamp(m.MissionNumber, 1, 5), 1f, false))
                    {
                        string code = StateGround.TryGetValue(m.StateName ?? "", out var c) ? c : "R3";
                        Apply(scene, code, 70, 15, 1f);
                        EditorSceneManager.SaveScene(scene);
                        made++;
                    }
                    if (link)
                    {
                        m.LinkDraftSceneForEditor();
                        EditorUtility.SetDirty(m);
                        var scenes = EditorBuildSettings.scenes.ToList();
                        if (!scenes.Any(s => s.path == path)) { scenes.Add(new EditorBuildSettingsScene(path, true)); EditorBuildSettings.scenes = scenes.ToArray(); }
                        linked++;
                    }
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Ground] Terrains created in {made} scenes." + (link ? $" {linked} missions are now playable (added to Build Settings)." : ""));
        }

        private void OnEnable()
        {
            codes = Directory.Exists(Root) ? Directory.GetDirectories(Root).Select(d => Path.GetFileName(d)).OrderBy(c => c).ToArray() : new string[0];
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Apply a ground material to every terrain in the open scene", EditorStyles.boldLabel);
            if (codes.Length == 0) { EditorGUILayout.HelpBox("Run 1. Build Ground Materials first.", MessageType.Warning); return; }
            pick = EditorGUILayout.Popup("Ground", pick, codes);
            fillTiling = EditorGUILayout.IntSlider("Fill tiling", fillTiling, 10, 150);
            capTiling = EditorGUILayout.IntSlider("Cap tiling", capTiling, 4, 60);
            capHeight = EditorGUILayout.Slider("Cap height", capHeight, 0.3f, 6f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("No terrain yet? (draft scenes)", EditorStyles.boldLabel);
            level = EditorGUILayout.IntSlider(new GUIContent("Level (1-5)", "Mission number in the chapter: sets length and difficulty"), level, 1, 5);
            EditorGUILayout.LabelField("Length", PlanLength[level - 1] + " units");
            challenge = EditorGUILayout.Slider(new GUIContent("Extra challenge", "1 = plan default; higher = bigger hills and bumps"), challenge, 0.5f, 2f);
            replaceExisting = EditorGUILayout.Toggle("Replace existing terrain", replaceExisting);
            if (GUILayout.Button("Create Terrain"))
            {
                var scene = SceneManager.GetActiveScene();
                if (CreateTerrain(scene, level, challenge, replaceExisting))
                {
                    Apply(scene, codes[pick].Split('_')[0], fillTiling, capTiling, capHeight);
                    EditorSceneManager.MarkSceneDirty(scene);
                    Debug.Log($"[Ground] Created a Level {level} terrain ({PlanLength[level - 1]} units) in {scene.name} with {codes[pick]}. Save the scene to keep it.");
                }
            }
            if (GUILayout.Button("Apply", GUILayout.Height(28)))
            {
                var scene = SceneManager.GetActiveScene();
                int n = Apply(scene, codes[pick].Split('_')[0], fillTiling, capTiling, capHeight);
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log($"[Ground] {codes[pick]} applied to {n} terrain(s) in {scene.name}. Save the scene to keep it.");
            }
        }

        /// <summary>
        /// Terrain for a mission, following the terrain plan zones (fractions of the length):
        ///   0-10% safe start, 15-30% first challenge (bumps + a steep climb), 35-55% main zone (deep valley, later
        ///   the bridge), 60-75% second challenge (twin steep hills), 78-90% final big climb, 90-100% flat finish.
        /// Hill size and steepness grow with the level; each scene gets its own variation (seeded by its name).
        /// Slopes are capped so every vehicle can climb them.
        /// </summary>
        internal static bool CreateTerrain(Scene scene, int level, float challenge, bool replace)
        {
            var existing = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TerrainEditor2D>(true)).ToList();
            if (existing.Count > 0)
            {
                if (!replace) { Debug.LogWarning("[Ground] This scene already has a terrain (tick 'Replace existing terrain')."); return false; }
                foreach (var e in existing) Undo.DestroyObjectImmediate(e.gameObject);
            }
            var start = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<StartPoint>(true)).FirstOrDefault();
            Vector3 sp = start != null ? start.transform.position : new Vector3(-5f, 4f, 0f);
            int length = PlanLength[Mathf.Clamp(level, 1, 5) - 1];

            // tall enough for the deepest valley and the biggest hill
            const int height = 80; const float topLocal = 45f; const float lead = 35f;
            var go = TerrainEditor2D.InstantiateTerrain2D(new Vector3(sp.x - lead, sp.y - 9f - topLocal, 0f), length + 70, height, 1);
            go.name = "Terrain Level0";
            go.tag = "Ground"; // legacy crash sensors look for this tag
            SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, "Create Terrain");
            var t = go.GetComponent<TerrainEditor2D>();
            t.OrderInLayer = 10; t.CapOrderInLayer = 11;

            // reshape the top: find the top vertex per x and move that column to the new height
            var verts = t.GetVertsPos();
            var top = new Dictionary<int, float>();
            foreach (var v in verts) { int x = Mathf.RoundToInt(v.x); if (!top.TryGetValue(x, out var y) || v.y > y) top[x] = v.y; }
            float[] profile = BuildProfile(length + 71, lead, length, level, challenge, scene.name.GetHashCode());
            for (int i = 0; i < verts.Length; i++)
            {
                int x = Mathf.RoundToInt(verts[i].x);
                float h = Mathf.Max(4f, topLocal + profile[Mathf.Clamp(x, 0, profile.Length - 1)]); // never below the terrain bottom
                if (verts[i].y > top[x] - 2f) verts[i].y += h - top[x];
            }
            t.EditTerrain(verts, true);
            t.UpdateCollider2D(); // the car drives on the collider, not the mesh
            t.SetSortingLayerAndOrder();

            // the run ends near the end of the terrain
            var gm = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameManager>(true)).FirstOrDefault();
            if (gm != null) { gm.targetDistance = Mathf.RoundToInt(sp.x + length - 40f); EditorUtility.SetDirty(gm); }
            return true;
        }

        /// <summary>Height offset for every unit of x (index 0 = left edge of the terrain, lead units before the start).</summary>
        private static float[] BuildProfile(int count, float lead, int length, int level, float challenge, int seed)
        {
            var rng = new System.Random(seed);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            float A = (8f + 2.5f * level) * challenge;   // feature size: L1 ~10.5, L5 ~20.5
            float maxSlope = 0.5f + 0.05f * level;       // ~29 deg (L1) ... ~37 deg (L5)
            float p1 = R(0, 6.28f), p2 = R(0, 6.28f), p3 = R(0, 6.28f);
            float climb = R(0.22f, 0.27f), valley = R(0.43f, 0.47f), finalHill = R(0.82f, 0.86f);
            float twinA = R(0.61f, 0.65f), twinB = twinA + R(0.06f, 0.09f);

            float Window(float u, float a, float b)
            {
                if (u <= a || u >= b) return 0f;
                return 0.5f - 0.5f * Mathf.Cos((u - a) / (b - a) * Mathf.PI * 2f);
            }
            float Hill(float u, float c, float w) { float d = (u - c) / w; return Mathf.Exp(-d * d * 4f); }

            var h = new float[count];
            for (int i = 0; i < count; i++)
            {
                float x = i - lead, u = x / length;   // u = 0 at the start point, 1 at the finish
                float active = Mathf.Clamp01((u - 0.08f) / 0.04f) * Mathf.Clamp01((0.92f - u) / 0.03f);
                float y = 0f;
                // rolling ground everywhere (two wave sizes)
                y += A * (0.35f * Mathf.Sin(x * 0.07f + p1) + 0.22f * Mathf.Sin(x * 0.17f + p2) + 0.15f * Mathf.Sin(x * 0.023f + p3));
                // first challenge: short bumps, then a steep climb and drop
                y += Window(u, 0.14f, 0.31f) * A * 0.25f * Mathf.Abs(Mathf.Sin(x * 0.45f));
                y += A * 1.3f * Hill(u, climb, 0.05f);
                // main zone: deep valley (the bridge will span it)
                y -= A * 1.6f * Hill(u, valley, 0.09f);
                // second challenge: twin steep hills
                y += A * 1.2f * Hill(u, twinA, 0.035f) + A * 1.0f * Hill(u, twinB, 0.035f);
                // final challenge: the biggest climb
                y += A * 2.0f * Hill(u, finalHill, 0.07f);
                h[i] = y * active;
            }
            // cap steepness both ways so every vehicle can climb and nothing becomes a wall
            for (int pass = 0; pass < 3; pass++)
            {
                for (int i = 1; i < count; i++) h[i] = Mathf.Clamp(h[i], h[i - 1] - maxSlope, h[i - 1] + maxSlope);
                for (int i = count - 2; i >= 0; i--) h[i] = Mathf.Clamp(h[i], h[i + 1] - maxSlope, h[i + 1] + maxSlope);
            }
            return h;
        }

        /// <summary>Set materials + tiling on every TerrainEditor2D in the scene and rebuild its mesh/UVs.</summary>
        internal static int Apply(Scene scene, string code, int fillTiling, int capTiling, float capHeight)
        {
            string folder = Directory.GetDirectories(Root).Select(d => d.Replace('\\', '/')).FirstOrDefault(d => Path.GetFileName(d).StartsWith(code + "_"));
            if (folder == null) return 0;
            var ground = AssetDatabase.LoadAssetAtPath<Material>($"{folder}/{code}_Ground.mat");
            var cap = AssetDatabase.LoadAssetAtPath<Material>($"{folder}/{code}_Cap.mat");
            if (ground == null || cap == null) { Debug.LogWarning("[Ground] Build the materials first (Terrain > 1)."); return 0; }

            int n = 0;
            foreach (var t in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TerrainEditor2D>(true)))
                if (ApplyTo(t, ground, cap, fillTiling, capTiling, capHeight)) n++;
            return n;
        }

        /// <summary>Ground + cap materials of road code R1..R10 (null if missing).</summary>
        internal static (Material ground, Material cap) Materials(string code)
        {
            string folder = Directory.GetDirectories(Root).Select(d => d.Replace('\\', '/')).FirstOrDefault(d => Path.GetFileName(d).StartsWith(code + "_"));
            if (folder == null) return (null, null);
            return (AssetDatabase.LoadAssetAtPath<Material>($"{folder}/{code}_Ground.mat"), AssetDatabase.LoadAssetAtPath<Material>($"{folder}/{code}_Cap.mat"));
        }

        /// <summary>Set materials + tiling on one terrain and rebuild its mesh/UVs.</summary>
        internal static bool ApplyTo(TerrainEditor2D t, Material ground, Material cap, int fillTiling, int capTiling, float capHeight)
        {
            if (t == null || ground == null || cap == null) return false;
            {
                Undo.RecordObject(t, "Apply Ground");
                t.MainMaterial = ground; t.MainTextureSize = fillTiling;
                t.CapMaterial = cap; t.CapTextureTiling = capTiling; t.CapHeight = capHeight;
                t.GenerateCap = true;
                t.EditTerrain(t.GetVertsPos(), false); // rebuild mesh, cap and UVs with the new settings
                var mr = t.GetComponent<MeshRenderer>(); if (mr != null) mr.sharedMaterial = ground;
                if (t.CapObj != null) { var cr = t.CapObj.GetComponent<MeshRenderer>(); if (cr != null) cr.sharedMaterial = cap; }
                EditorUtility.SetDirty(t);
            }
            return true;
        }

        private static Texture2D Prepare(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return null;
            bool change = imp.textureType != TextureImporterType.Default || imp.wrapMode != TextureWrapMode.Repeat || imp.maxTextureSize != 2048;
            if (change)
            {
                imp.textureType = TextureImporterType.Default;
                imp.alphaIsTransparency = true;
                imp.wrapModeU = TextureWrapMode.Repeat;
                imp.wrapModeV = path.EndsWith("_cap.png") ? TextureWrapMode.Clamp : TextureWrapMode.Repeat; // cap repeats sideways only
                imp.mipmapEnabled = true;
                imp.maxTextureSize = 2048;
                imp.textureCompression = TextureImporterCompression.Compressed;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void MakeMaterial(string path, Texture2D tex)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Unlit/Transparent")); // same shader as the existing terrain materials
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.mainTexture = tex;
            EditorUtility.SetDirty(mat);
        }
    }
}
