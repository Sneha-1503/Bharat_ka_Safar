using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BharatKaSafar.Core;
using BharatKaSafar.Gameplay;
using BharatKaSafar.Missions;
using BharatKaSafar.Obstacles;
using BharatKaSafar.Rewards;
using BharatKaSafar.Vehicle;

namespace BharatKaSafar.EditorTools
{
    /// <summary>
    /// Menu: Bharat Ka Safar → Gameplay → Setup UP + Tamil Nadu Missions
    ///
    ///  1. Cars used by these levels: adds BKS_HornController (+ horn sound) and BKS_VehicleHealth to "Body".
    ///  2. Builds reusable prefabs in Assets/BharatKaSafar/Prefabs/Obstacles + Collectables:
    ///       BKS_HornAnimal_Cow, BKS_Rock, BKS_SafarStar, BKS_Coin, BKS_Multiplier2X, BKS_DestinationGate
    ///  3. Every UP + TN mission scene:
    ///       • removes the old floating / invisible builder objects (UP)
    ///       • places, ON the road: 1 horn cow + 2 Safar Stars (= 3 stars), rocks, coins, 2X, finish gate at the end
    ///  4. MissionData (UP + TN): Max Safar Stars = 3
    ///
    /// Objects created by this tool are named "*_Auto" and live under [BKS_LevelElements],
    /// so running it again replaces them instead of duplicating. Move them freely afterwards —
    /// but re-running the tool resets the "_Auto" ones.
    /// </summary>
    public static class BKS_ChapterGameplaySetupUtility
    {
        private const string Root = "Assets/BharatKaSafar/";
        private const string ObstaclePrefabDir   = Root + "Prefabs/Obstacles/";
        private const string CollectablePrefabDir = Root + "Prefabs/Collectables/";

        private const string CowSprite   = Root + "Art/Obstacles/BKS_Cow_Placeholder.png";
        private const string HornClip    = Root + "Audio/SFX/BKS_Horn_Placeholder.wav";
        private const string RockSprite  = Root + "BKS sprite/background/Tamil Nadu/Obstacles/tn_rock_pile.png";
        private const string StarSprite  = Root + "BKS sprite/home/star.png";
        private const string CoinSprite  = Root + "BKS sprite/home/coin.png";
        private const string GateSprite  = Root + "BKS sprite/home/gate.png";
        private const string X2Sprite    = Root + "BKS sprite/background/HUD/tn_double_badge.png";

        private static readonly string[] Scenes =
        {
            Root + "Scenes/UP/BKS_UP_M01_Agra.unity",
            Root + "Scenes/UP/BKS_UP_M02_Varanasi.unity",
            Root + "Scenes/UP/BKS_UP_M03_Sarnath.unity",
            Root + "Scenes/UP/BKS_UP_M04_Prayagraj.unity",
            Root + "Scenes/UP/BKS_UP_M05_Bhulbhulaiya.unity",
            Root + "Scenes/Tamil nadu/BKS_TN_M01_Madurai.unity",
            Root + "Scenes/Tamil nadu/BKS_TN_M02_Chennai.unity",
            Root + "Scenes/Tamil nadu/BKS_TN_M03_Mahabalipuram.unity",
            Root + "Scenes/Tamil nadu/BKS_TN_M04_Ooty.unity",
            Root + "Scenes/Tamil nadu/BKS_TN_M05_Kanyakumari.unity",
        };

        // Layout along the road, in world X (the level ends at GameManager.targetDistance, 1100)
        private static readonly float[] StarX   = { 260f, 790f };
        private const float CowX                = 480f;
        private static readonly float[] RockX   = { 140f, 360f, 620f, 900f };
        private static readonly float[] CoinX   = { 60f, 200f, 320f, 430f, 560f, 700f, 840f, 960f };
        private static readonly float[] X2X     = { 540f };
        private const float GateBeforeTarget    = 2f;

        // Desired world heights (units)
        private const float CowHeight  = 2.2f;
        private const float RockHeight = 1.1f;
        private const float StarHeight = 1.2f;
        private const float CoinHeight = 0.9f;
        private const float X2Height   = 1.3f;
        private const float GateHeight = 7f;

        [MenuItem("Bharat Ka Safar/Gameplay/Setup UP + Tamil Nadu Missions", priority = 40)]
        public static void SetupAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string startScene = EditorSceneManager.GetActiveScene().path;

            EnsureCowSpriteImport();
            var report = new List<string>();

            // 2. Prefabs
            var prefabs = BuildPrefabs(report);
            if (prefabs == null) { Show(report); return; }

            // 1 + 3. Scenes (cars are collected from each scene's StartPoint)
            var carPrefabs = new HashSet<GameObject>();
            foreach (var path in Scenes)
                report.Add(SetupScene(path, prefabs, carPrefabs));

            foreach (var car in carPrefabs)
            {
                try { report.Add(SetupCarPrefab(car)); }
                catch (System.Exception e) { report.Add($"⚠ Car {(car != null ? car.name : "?")}: {e.Message}"); Debug.LogException(e); }
            }

            // 4. Mission data
            report.Add(SetMissionStars());

            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(startScene) && File.Exists(startScene))
                EditorSceneManager.OpenScene(startScene, OpenSceneMode.Single);
            Show(report);
        }

        private static void Show(List<string> report)
        {
            string text = string.Join("\n", report);
            Debug.Log("[BKS Gameplay Setup]\n" + text);
            EditorUtility.DisplayDialog("Bharat Ka Safar — Gameplay Setup", text, "OK");
        }

        // ── Prefabs ───────────────────────────────────────────────────────────────

        private class PrefabSet { public GameObject Cow, Rock, Star, Coin, X2, Gate; }

        private static PrefabSet BuildPrefabs(List<string> report)
        {
            Directory.CreateDirectory(ObstaclePrefabDir);
            Directory.CreateDirectory(CollectablePrefabDir);

            var cow  = Load<Sprite>(CowSprite,  report);
            var rock = Load<Sprite>(RockSprite, report);
            var star = Load<Sprite>(StarSprite, report);
            var coin = Load<Sprite>(CoinSprite, report);
            var gate = Load<Sprite>(GateSprite, report);
            var x2   = Load<Sprite>(X2Sprite,   report);
            if (cow == null || rock == null || star == null || coin == null || gate == null)
            {
                report.Add("⚠ Missing sprites — prefabs not built.");
                return null;
            }

            var set = new PrefabSet();

            set.Cow = MakePrefab(ObstaclePrefabDir + "BKS_HornAnimal_Cow.prefab", "BKS_HornAnimal_Cow", cow, CowHeight, go =>
            {
                AddBox(go, trigger: true, shrink: 0.85f);
                go.AddComponent<BKS_HornInteractable>();
                var dmg = go.AddComponent<BKS_DamageObstacle>();
                SetFields(dmg, ("damageAmount", 25f), ("useTrigger", true), ("disableAfterHit", true));
            });

            set.Rock = MakePrefab(ObstaclePrefabDir + "BKS_Rock.prefab", "BKS_Rock", rock, RockHeight, go =>
            {
                AddBox(go, trigger: true, shrink: 0.8f);
                var ob = go.AddComponent<BKS_Obstacle>();
                ob.severity = CrashSeverity.NormalHit;
                ob.damageAmount = 20f;
                ob.speedPenaltyMultiplier = 0.6f;
            });

            set.Star = MakePrefab(CollectablePrefabDir + "BKS_SafarStar.prefab", "BKS_SafarStar", star, StarHeight, go =>
            {
                AddCircle(go);
                go.AddComponent<BKS_SafarStarCollectible>();
            });

            set.Coin = MakePrefab(CollectablePrefabDir + "BKS_Coin.prefab", "BKS_Coin", coin, CoinHeight, go =>
            {
                AddCircle(go);
                go.AddComponent<BKS_CoinPickup>();
                SetFields(go.GetComponent<BKS_CoinPickup>(), ("coinValue", 5));
            });

            set.X2 = MakePrefab(CollectablePrefabDir + "BKS_Multiplier2X.prefab", "BKS_Multiplier2X", x2 != null ? x2 : coin, X2Height, go =>
            {
                AddCircle(go);
                go.AddComponent<BKS_MultiplierPickup>();
                SetFields(go.GetComponent<BKS_MultiplierPickup>(), ("multiplierValue", 2), ("duration", 15f));
            });

            set.Gate = MakePrefab(ObstaclePrefabDir + "BKS_DestinationGate.prefab", "BKS_DestinationGate", gate, GateHeight, go =>
            {
                // Tall trigger so the finish counts even when the car is in the air
                var box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                var sr = go.GetComponent<SpriteRenderer>();
                Vector2 size = sr.sprite.bounds.size;
                box.size = new Vector2(size.x * 0.5f, size.y * 4f);
                box.offset = new Vector2(0f, size.y * 1.2f);
                go.AddComponent<BKS_Destination>();
            });

            report.Add("✓ Prefabs built: Cow (horn), Rock, Safar Star, Coin, 2X, Destination Gate");
            return set;
        }

        /// <summary>Sprite object scaled to a world height, pivot at the bottom-centre → sits on the road.</summary>
        private static GameObject MakePrefab(string path, string name, Sprite sprite, float worldHeight, System.Action<GameObject> configure)
        {
            var root = new GameObject(name);
            var visual = root; // single object: easier to place
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 20;

            float h = sprite.bounds.size.y;
            float s = h > 0.0001f ? worldHeight / h : 1f;
            root.transform.localScale = new Vector3(s, s, 1f);

            configure(root);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void AddBox(GameObject go, bool trigger, float shrink)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = trigger;
            Bounds b = sr.sprite.bounds;
            box.size = new Vector2(b.size.x * shrink, b.size.y * shrink);
            box.offset = b.center;
        }

        private static void AddCircle(GameObject go)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            var c = go.AddComponent<CircleCollider2D>();
            c.isTrigger = true;
            Bounds b = sr.sprite.bounds;
            c.radius = Mathf.Max(b.extents.x, b.extents.y) * 0.8f;
            c.offset = b.center;
        }

        // ── Scenes ────────────────────────────────────────────────────────────────

        private static string SetupScene(string path, PrefabSet p, HashSet<GameObject> cars)
        {
            string label = Path.GetFileNameWithoutExtension(path);
            if (!File.Exists(path)) return $"⚠ {label}: scene not found";

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Physics2D.SyncTransforms();

            // Cars spawned by this level
            var start = Object.FindFirstObjectByType<StartPoint>(FindObjectsInactive.Include);
            if (start != null && start.cars != null)
                foreach (var c in start.cars) if (c != null) cars.Add(c);

            var gm = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            float target = gm != null && gm.targetDistance > 0 ? gm.targetDistance : 1100f;

            var road = new RoadSampler();

            // Container
            var containerGo = GameObject.Find("[BKS_LevelElements]");
            if (containerGo == null) containerGo = new GameObject("[BKS_LevelElements]");
            Transform container = containerGo.transform;

            // Remove old builder objects (floating, no sprite) and earlier "_Auto" objects
            int removed = 0;
            foreach (Transform child in container.Cast<Transform>().ToList())
            {
                bool oldBuilder = child.name.StartsWith("BKS_Obstacle_") || child.name.StartsWith("BKS_Coin_X")
                               || child.name.StartsWith("BKS_Multiplier2X_X") || child.name.StartsWith("BKS_Destination_Gate_X");
                if (oldBuilder || child.name.EndsWith("_Auto"))
                {
                    Object.DestroyImmediate(child.gameObject);
                    removed++;
                }
            }

            // Old checkpoints: keep, but put them on the road
            foreach (var cp in Object.FindObjectsByType<BKS_Checkpoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Vector3 pos = cp.transform.position;
                pos.y = road.SurfaceY(pos.x) + 2f;
                cp.transform.position = pos;
            }

            int placed = 0;
            placed += Place(p.Cow,  container, "BKS_Cow_Auto",  new[] { CowX }, road, 0f);
            placed += Place(p.Star, container, "BKS_SafarStar_Auto", StarX, road, 2.2f);
            placed += Place(p.Rock, container, "BKS_Rock_Auto", RockX, road, 0f);
            placed += Place(p.Coin, container, "BKS_Coin_Auto", CoinX, road, 1.3f);
            placed += Place(p.X2,   container, "BKS_Multiplier2X_Auto", X2X, road, 1.8f);
            placed += Place(p.Gate, container, "BKS_DestinationGate_Auto", new[] { target - GateBeforeTarget }, road, 0f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return $"✓ {label}: removed {removed} old, placed {placed} (road: {road.Source})";
        }

        private static int Place(GameObject prefab, Transform parent, string name, float[] xs, RoadSampler road, float heightAboveRoad)
        {
            int n = 0;
            for (int i = 0; i < xs.Length; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                go.name = xs.Length > 1 ? $"{name}_{i + 1}".Replace("_Auto_", "_") + "_Auto" : name;
                float x = xs[i];
                float roadY = road.SurfaceY(x);
                go.transform.position = new Vector3(x, roadY, 0f);

                // heightAboveRoad == 0 → stand ON the road (bottom of the sprite on the surface)
                // otherwise         → float with its centre that high above the road
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Bounds b = sr.bounds;
                    float shift = heightAboveRoad <= 0f ? roadY - b.min.y : (roadY + heightAboveRoad) - b.center.y;
                    go.transform.position += new Vector3(0f, shift, 0f);
                }
                n++;
            }
            return n;
        }

        /// <summary>
        /// Road height at an X: raycast down onto solid (non-trigger) ground colliders;
        /// if nothing is hit, use the legacy coin/fuel line (ScoreItem), which follows the road.
        /// </summary>
        private class RoadSampler
        {
            private readonly List<Vector2> _items = new List<Vector2>();
            public string Source = "raycast";
            private bool _usedFallback;

            public RoadSampler()
            {
                foreach (var si in Object.FindObjectsByType<ScoreItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    _items.Add(si.transform.position);
                _items.Sort((a, b) => a.x.CompareTo(b.x));
            }

            public float SurfaceY(float x)
            {
                float guess = ItemY(x);
                var filter = new ContactFilter2D { useTriggers = false };
                var hits = new RaycastHit2D[8];
                int count = Physics2D.Raycast(new Vector2(x, guess + 15f), Vector2.down, filter, hits, 60f);
                for (int i = 0; i < count; i++)
                {
                    var col = hits[i].collider;
                    if (col == null || col.isTrigger) continue;
                    if (col.GetComponentInParent<CarController>() != null) continue;
                    return hits[i].point.y;
                }

                if (!_usedFallback) { _usedFallback = true; Source = "coin line (no ground collider hit)"; }
                return guess - 1f; // coins float about a unit above the road
            }

            private float ItemY(float x)
            {
                if (_items.Count == 0) return 0f;
                Vector2 best = _items[0];
                foreach (var p in _items) if (Mathf.Abs(p.x - x) < Mathf.Abs(best.x - x)) best = p;
                return best.y;
            }
        }

        // ── Cars ──────────────────────────────────────────────────────────────────

        private static string SetupCarPrefab(GameObject carPrefab)
        {
            string path = AssetDatabase.GetAssetPath(carPrefab);
            if (string.IsNullOrEmpty(path)) return "⚠ car prefab path not found";

            var root = PrefabUtility.LoadPrefabContents(path);
            var body = root.GetComponentInChildren<CarController>(true);
            if (body == null)
            {
                PrefabUtility.UnloadPrefabContents(root);
                return $"⚠ {Path.GetFileName(path)}: no CarController";
            }

            var horn = body.GetComponent<BKS_HornController>();
            if (horn == null) horn = body.gameObject.AddComponent<BKS_HornController>();
            // Dedicated AudioSource for the horn (never reuse the engine's source).
            AudioSource hornSource = new SerializedObject(horn).FindProperty("hornAudioSource").objectReferenceValue as AudioSource;
            if (hornSource == null) hornSource = body.gameObject.AddComponent<AudioSource>();
            hornSource.playOnAwake = false;
            hornSource.loop = false;

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(HornClip);
            SetFields(horn, ("hornAudioSource", (Object)hornSource), ("hornClip", (Object)clip), ("baseDetectionRadius", 9f), ("hornCooldown", 0.4f));

            if (body.GetComponent<BKS_VehicleHealth>() == null)
                body.gameObject.AddComponent<BKS_VehicleHealth>();

            string bodyName = body.name; // read BEFORE unloading — the object is destroyed afterwards
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            return $"✓ Car {Path.GetFileNameWithoutExtension(path)}: Horn + Health added to '{bodyName}'";
        }

        // ── Mission data ──────────────────────────────────────────────────────────

        private static string SetMissionStars()
        {
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:MissionData", new[] { Root + "ScriptableObjects/missions" }))
            {
                var m = AssetDatabase.LoadAssetAtPath<MissionData>(AssetDatabase.GUIDToAssetPath(guid));
                if (m == null) continue;
                if (!(m.MissionID.StartsWith("UP_") || m.MissionID.StartsWith("TN_"))) continue;
                var so = new SerializedObject(m);
                so.FindProperty("maxSafarStars").intValue = 3;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(m);
                n++;
            }
            return $"✓ Max Safar Stars = 3 for {n} UP/TN missions";
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static void EnsureCowSpriteImport()
        {
            var imp = AssetImporter.GetAtPath(CowSprite) as TextureImporter;
            if (imp != null && imp.textureType != TextureImporterType.Sprite)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.SaveAndReimport();
            }
        }

        private static T Load<T>(string path, List<string> report) where T : Object
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) report.Add("⚠ Not found: " + path);
            return a;
        }

        private static void SetFields(Object target, params (string name, object value)[] fields)
        {
            var so = new SerializedObject(target);
            foreach (var field in fields)
            {
                string name = field.Item1;
                object value = field.Item2;
                var p = so.FindProperty(name);
                if (p == null) { Debug.LogWarning($"[BKS Gameplay Setup] Field '{name}' not found on {target.GetType().Name}"); continue; }

                if (value is float)      p.floatValue = (float)value;
                else if (value is int)   p.intValue = (int)value;
                else if (value is bool)  p.boolValue = (bool)value;
                else                     p.objectReferenceValue = value as Object; // also clears when null
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
