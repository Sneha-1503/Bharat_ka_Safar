using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BharatKaSafar.Gameplay;
using BharatKaSafar.Missions;
using BharatKaSafar.Rewards;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Fills a mission scene that already has its terrain, following the terrain plan:
    ///   • Bridges (B1–B6) at the plan's X positions; the terrain under each bridge is carved into a valley.
    ///   • The mission's Excel obstacles (Barricade, Pothole, Mud Patch, Cart …) in the challenge zones,
    ///     never on a bridge or its joints.
    ///   • Mission collectibles, coin groups, fuel cans (every ~420 units, and after a bridge when none is near), one 2X pickup.
    ///   • 3 stage messages and the destination gate.
    /// Everything goes under "[BKS_Level]" — running again rebuilds it (the valley carve is safe to repeat).
    /// Menu: Bharat Ka Safar > Level > Populate …
    /// </summary>
    public static class BKS_LevelPopulator
    {
        private const string Root = "[BKS_Level]";
        private const string Patterns = "Assets/BharatKaSafar/Prefabs/Obstacles/Patterns/";
        private static readonly int[] PlanLength = { 1000, 1100, 1200, 1400, 1500 }; // terrain plan: L4 = 1400

        // ── Bridge plan (state → level 1..5 → "B3@420" / "B4@520,1100" / "-") ─────────────────────────
        private const string BridgePlan = @"
Andhra Pradesh|B3@420|B4@650|B4@480|B1@500|B4@520,1100
Arunachal Pradesh|B4@450|B2@500|B4@620|B4@430,900|B5@600,1150
Assam|B3@400|B5@530|B2@450|B1@600|B5@500,1080
Bihar|B1@400|-|B4@650|B3@680|B2@550
Chhattisgarh|B5@430|B5@550|B2@620|B1@450|B4@480,1100
Goa|B1@420|-|B3@550|B1@630|B2@500,1050
Gujarat|B3@450|-|B1@530|B3@650|B5@600
Haryana|-|B4@600|B2@450|B1@550|B2@600
Himachal Pradesh|B4@450|B4@570|B4@650|B6@500,970|B2@600,1150
Jharkhand|B1@410|-|B5@630|B4@600|B4@450,1050
Karnataka|B1@400|-|B6@530|B4@620|B3@650
Kerala|B3@440|B3@550|B4@650|B5@500,980|B3@600
Madhya Pradesh|-|B2@450|B1@550|B4@640|B5@500,1100
Maharashtra|B1@450|B4@520|B4@600|B1@670|B6@520,1100
Manipur|B1@400|B3@530|B5@650|B4@570|B2@520
Meghalaya|B4@450|B3@530|B3@620|B2@580|B4@500,1100
Mizoram|B4@430|B4@550|B5@630|B6@520,980|B4@620
Nagaland|B4@440|B5@520|B2@600|B1@650|B4@500,1050
Odisha|B3@450|B1@550|-|B3@640|B5@520,1100
Punjab|B1@400|B3@500|-|B2@650|B1@550
Rajasthan|-|B1@500|B2@580|B3@650|B6@540,1100
Sikkim|B4@450|B4@550|B6@630|B4@700|B4@500,1120
Tamil Nadu|B1@430|B6@530|B4@620|B3@500,980|B1@650
Telangana|B1@400|B4@530|B2@630|-|B5@550,1100
Tripura|B1@430|B3@550|B4@620|B2@650|B4@500,1100
Uttar Pradesh|B1@400|B2@530|-|B3@650|B1@540
Uttarakhand|B4@420|B4@550|B4@650|B6@490,1000|B5@560,1120
West Bengal|B1@420|B4@550|B5@630|B3@650|B4@500,1100";

        // Bridge type → length (middle of the plan's range), valley depth factor
        private static readonly Dictionary<string, float> BridgeLength = new Dictionary<string, float>
            { {"B1",45f},{"B2",40f},{"B3",75f},{"B4",60f},{"B5",55f},{"B6",45f} };

        private static readonly Dictionary<string, string> ObstaclePrefab = new Dictionary<string, string>
        {
            {"barricade","BKS_Barricade"},{"crate","BKS_Crate"},{"rock","BKS_RockPile"},{"log","BKS_Log"},
            {"pothole","BKS_Pothole"},{"mud patch","BKS_MudPatch"},{"sand patch","BKS_SandPatch"},{"snow patch","BKS_SnowPatch"},
            {"cart","BKS_CartCrossing"},{"animal crossing","BKS_CowCrossing"},
        };

        [MenuItem("Bharat Ka Safar/Level/Populate Open Scene")]
        public static void PopulateOpen()
        {
            var scene = SceneManager.GetActiveScene();
            var m = FindMission(scene.name);
            if (m == null) { Debug.LogWarning($"[Level] No MissionData uses scene '{scene.name}'."); return; }
            if (scene.GetRootGameObjects().Any(g => g.name == "[BKS_LevelElements]") &&
                !EditorUtility.DisplayDialog("Hand-made level", "This scene has a hand-made level ([BKS_LevelElements]). Populating removes its spawner coins/fuel and adds generated content on top. Continue?", "Continue", "Cancel"))
                return;
            string report = Populate(scene, m);
            if (report != null) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); Debug.Log($"[Level] {scene.name}: {report}"); }
        }

        [MenuItem("Bharat Ka Safar/Level/Populate All Draft Scenes")]
        public static void PopulateAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!EditorUtility.DisplayDialog("Populate levels", "Add bridges, obstacles, collectibles, coins, fuel, 2X, stage messages and the finish gate to every generated mission scene?\n(Hand-made scenes with [BKS_LevelElements] are skipped. Commit to git first.)", "Go", "Cancel")) return;
            var missions = AllMissions().Where(m => !string.IsNullOrEmpty(m.DraftSceneName)).ToList();
            // GroupBy (not ToDictionary) so two scenes with the same file name can never stop the run
            var paths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/BharatKaSafar/Scenes" }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !p.Contains("_Backup")).GroupBy(p => Path.GetFileNameWithoutExtension(p)).ToDictionary(g => g.Key, g => g.First());
            Debug.Log($"[Level] Populating {missions.Count} generated mission scenes…");
            int done = 0, skipped = 0;
            var failed = new List<string>();
            try
            {
                for (int i = 0; i < missions.Count; i++)
                {
                    var m = missions[i];
                    if (EditorUtility.DisplayCancelableProgressBar("Populate levels", $"{m.StateName}: {m.PlaceName} ({i + 1}/{missions.Count})", i / (float)missions.Count))
                    { Debug.LogWarning("[Level] Cancelled."); break; }
                    if (!paths.TryGetValue(m.DraftSceneName, out var path)) { failed.Add($"{m.DraftSceneName}: scene file not found"); continue; }
                    try
                    {
                        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                        if (scene.GetRootGameObjects().Any(g => g.name == "[BKS_LevelElements]")) { skipped++; continue; } // hand-made level
                        if (Populate(scene, m) != null) { EditorSceneManager.SaveScene(scene); done++; }
                        else failed.Add($"{m.DraftSceneName}: no terrain");
                    }
                    catch (System.Exception e)
                    {
                        // keep going with the next scene, report at the end
                        failed.Add($"{m.DraftSceneName}: {e.GetType().Name} - {e.Message}");
                        Debug.LogException(e);
                    }
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            Debug.Log($"[Level] Populated {done} scenes, skipped {skipped} hand-made." +
                      (failed.Count > 0 ? $"\nProblems in {failed.Count}:\n" + string.Join("\n", failed) : ""));
        }

        /// <summary>
        /// Hand-made scenes (UP, Rajasthan, Tamil Nadu): add only the Safar Diary memory token in front of the
        /// existing destination gate so collecting it completes the mission; the gate becomes the backdrop.
        /// Nothing else in the level is touched. Safe to run again.
        /// </summary>
        [MenuItem("Bharat Ka Safar/Level/Add Memory Token Finish (Hand-made Scenes)")]
        public static void AddTokenToHandmade()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var missions = AllMissions().Where(m => m.IsPlayable && m.MemoryTokenSprite != null).ToList();
            var paths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/BharatKaSafar/Scenes" }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !p.Contains("_Backup")).ToDictionary(p => Path.GetFileNameWithoutExtension(p), p => p);
            int done = 0;
            try
            {
                for (int i = 0; i < missions.Count; i++)
                {
                    var m = missions[i];
                    EditorUtility.DisplayProgressBar("Memory tokens", m.PlaceName, i / (float)missions.Count);
                    if (!paths.TryGetValue(m.GameplaySceneName, out var path)) continue;
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var roots = scene.GetRootGameObjects();
                    if (!roots.Any(g => g.name == "[BKS_LevelElements]")) continue; // generated levels already have it
                    var terrain = roots.SelectMany(r => r.GetComponentsInChildren<TerrainEditor2D>(true)).FirstOrDefault();
                    if (terrain == null) continue;
                    var ground = new Ground(terrain);

                    // where the level ends today: the gate, else the run distance
                    var gates = roots.SelectMany(r => r.GetComponentsInChildren<BharatKaSafar.Rajasthan.BKS_DestinationGate>(true)).ToList();
                    var gm = roots.SelectMany(r => r.GetComponentsInChildren<GameManager>(true)).FirstOrDefault();
                    float endX = gates.Count > 0 ? gates.Max(g => g.transform.position.x) : gm != null ? gm.targetDistance : 0f;
                    if (endX <= 0f) continue;

                    foreach (var old in roots.Where(g => g.name == "[BKS_MemoryToken]")) Object.DestroyImmediate(old);
                    var holder = new GameObject("[BKS_MemoryToken]");
                    SceneManager.MoveGameObjectToScene(holder, scene);
                    CreateToken(holder.transform, m, endX - 10f, ground);
                    foreach (var g in gates) { g.enabled = false; g.gameObject.SetActive(false); EditorUtility.SetDirty(g); } // replaced by the flag
                    FinishFlag(holder.transform, endX, ground);
                    if (gm != null) { gm.targetDistance = Mathf.RoundToInt(endX + 25f); EditorUtility.SetDirty(gm); } // fallback after the token
                    EditorSceneManager.SaveScene(scene);
                    done++;
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            Debug.Log($"[Level] Memory token finish added to {done} hand-made scenes.");
        }

        /// <summary>The finish flag (tn_finish_flag) standing on the road at the end of the level.</summary>
        private static void FinishFlag(Transform parent, float x, Ground ground)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/BharatKaSafar/BKS sprite/background/HUD/tn_finish_flag.png");
            if (sprite == null) { Debug.LogWarning("[Level] Missing tn_finish_flag sprite."); return; }
            var flag = new GameObject("FinishFlag");
            flag.transform.SetParent(parent);
            var sr = flag.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.sortingOrder = 7; // behind the car
            float s = 6f / sprite.bounds.size.y;      // 6 units tall
            flag.transform.localScale = new Vector3(s, s, 1f);
            flag.transform.position = new Vector3(x, ground.Y(x), 0f);
            flag.transform.position += Vector3.up * (ground.Y(x) - sr.bounds.min.y); // stand on the road
        }

        /// <summary>The Safar Diary token pickup that completes the mission.</summary>
        private static void CreateToken(Transform parent, MissionData m, float x, Ground ground)
        {
            var token = new GameObject("MemoryToken");
            token.transform.SetParent(parent);
            token.transform.position = new Vector3(x, ground.Y(x) + PickupGap + 1.75f, 0f);
            var icon = new GameObject("Icon");
            icon.transform.SetParent(token.transform, false);
            var sr = icon.AddComponent<SpriteRenderer>();
            sr.sprite = m.MemoryTokenSprite; sr.sortingOrder = 22;
            if (sr.sprite != null) icon.transform.localScale = Vector3.one * (3.5f / Mathf.Max(0.01f, sr.sprite.bounds.size.y));
            var box = token.AddComponent<BoxCollider2D>(); box.isTrigger = true; box.size = new Vector2(3f, 30f); // tall: cannot be jumped over
            token.AddComponent<BKS_MemoryTokenPickup>();
        }

        // ── main ─────────────────────────────────────────────────────────────────────────────────
        internal static string Populate(Scene scene, MissionData m)
        {
            var roots = scene.GetRootGameObjects();
            // the plan terrain is one piece per zone; older scenes have a single piece
            var terrains = roots.SelectMany(r => r.GetComponentsInChildren<TerrainEditor2D>(true)).Where(t => t.gameObject.activeInHierarchy).ToList();
            if (terrains.Count == 0) { Debug.LogWarning($"[Level] {scene.name}: no terrain (Terrain > 5 first)."); return null; }
            var start = roots.SelectMany(r => r.GetComponentsInChildren<StartPoint>(true)).FirstOrDefault();
            float x0 = start != null ? start.transform.position.x : -5f;
            int level = Mathf.Clamp(m.MissionNumber, 1, 5);
            int length = BKS_TerrainPlanData.Length(level);
            var rng = new System.Random(StableHash(scene.name));

            // clean previous run + coins/fuel copied from the Madurai template
            foreach (var g in roots.Where(g => g.name == Root)) Object.DestroyImmediate(g);
            roots = scene.GetRootGameObjects(); // the destroyed root must not be touched below
            foreach (var g in roots.Where(g => g.name == "CoinSpawner" || g.name == "FuelSpawner"))
            {
                foreach (Transform c in g.transform.Cast<Transform>().ToList()) if (c.name == "Items") Object.DestroyImmediate(c.gameObject);
                g.SetActive(false);
            }
            var root = new GameObject(Root);
            SceneManager.MoveGameObjectToScene(root, scene);
            roots = scene.GetRootGameObjects(); // the old root was destroyed above; never touch it again

            // the old destination gate (copied from the template) is replaced by the finish flag
            foreach (var gate in roots.SelectMany(r => r.GetComponentsInChildren<BharatKaSafar.Rajasthan.BKS_DestinationGate>(true)))
            { gate.enabled = false; gate.gameObject.SetActive(false); EditorUtility.SetDirty(gate); }
            Transform Group(string n) { var g = new GameObject(n); g.transform.SetParent(root.transform); return g.transform; }

            var ground = new Ground(terrains);
            var reserved = new List<Vector2>(); // world x ranges where nothing else may go
            bool Free(float x, float pad) => reserved.All(r => x < r.x - pad || x > r.y + pad);

            // 1. Bridge: only the 35 missions of the terrain plan, in the Main Challenge zone where both ends are level
            var bridges = new List<(string type, float x)>();
            string bridgeType = BKS_TerrainPlanData.Bridge(m);
            if (bridgeType != null)
            {
                float len0 = BridgeLength[bridgeType], best = float.MaxValue, bestX = length * 0.5f;
                for (float bx = length * 0.42f; bx < length * 0.63f - len0; bx += 5f)
                {
                    float score = Mathf.Abs(ground.Y(x0 + bx) - ground.Y(x0 + bx + len0)) + 3f * (Mathf.Abs(ground.Slope(x0 + bx)) + Mathf.Abs(ground.Slope(x0 + bx + len0)));
                    if (score < best) { best = score; bestX = bx; }
                }
                bridges.Add((bridgeType, bestX));
            }
            var bridgeGroup = Group("Bridges");
            foreach (var (type, bx) in bridges)
            {
                float a = x0 + bx, len = BridgeLength[type];
                foreach (var t in terrains) CarveValley(t, ground, a, a + len, 6f + 2f * level);
                ground = new Ground(terrains);
                BuildBridge(bridgeGroup, type, a, a + len, ground.Y(a), ground.Y(a + len), rng);
                reserved.Add(new Vector2(a - 20f, a + len + 20f));
            }

            // 2. Obstacles from the mission sheet, spread over the challenge zones
            var types = (m.ObstaclesText ?? "").Split(',').Select(s => s.Trim().ToLower().Replace(" (optional)", ""))
                .Where(s => ObstaclePrefab.ContainsKey(s)).ToList();
            // terrain plan zones: Exploration, Main Challenge, Finale (+ Recovery from level 3)
            var zones = new List<Vector2> { new Vector2(0.17f, 0.38f), new Vector2(0.42f, 0.63f), new Vector2(0.86f, 0.92f) };
            if (level >= 3) zones.Add(new Vector2(0.67f, 0.83f));
            var obstGroup = Group("Obstacles");
            int obstacles = 0, wanted = types.Count == 0 ? 0 : 2 + level;
            for (int i = 0; i < wanted; i++)
            {
                var z = zones[i % zones.Count];
                for (int tries = 0; tries < 30; tries++)
                {
                    float x = x0 + length * Mathf.Lerp(z.x, z.y, (float)rng.NextDouble());
                    if (!Free(x, 30f) || Mathf.Abs(ground.Slope(x)) > 0.3f) continue;
                    string key = types[i % types.Count];
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Patterns + ObstaclePrefab[key] + ".prefab");
                    if (prefab == null) { Debug.LogWarning("[Level] Missing obstacle prefab " + ObstaclePrefab[key] + " (Obstacles > Build Obstacle Prefabs)."); break; }
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, obstGroup);
                    go.transform.position = new Vector3(x, ground.Y(x), 0f);
                    bool flat = key.Contains("patch") || key == "pothole";
                    if (flat) go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan(ground.Slope(x)) * Mathf.Rad2Deg);
                    float half = key.Contains("patch") ? 9f : 5f;
                    reserved.Add(new Vector2(x - half, x + half));
                    obstacles++;
                    break;
                }
            }

            // 3. Mission collectibles
            var colGroup = Group("Collectibles");
            int items = 0;
            for (int i = 0; i < m.CollectibleCount; i++)
            {
                float x = x0 + length * Mathf.Lerp(0.12f, 0.9f, m.CollectibleCount == 1 ? 0.5f : i / (float)(m.CollectibleCount - 1));
                for (int s = 0; s < 40 && !Free(x, 8f); s++) x += 6f;
                var item = new GameObject($"Collectible_{i + 1:00}");
                item.transform.SetParent(colGroup);
                // mission item: 3 units tall, bottom PickupGap above the road
                item.transform.position = new Vector3(x, ground.Y(x) + PickupGap + 1.5f, 0f);
                var sr = item.AddComponent<SpriteRenderer>();
                sr.sprite = m.CollectibleSprite; sr.sortingOrder = 20;
                if (sr.sprite != null) item.transform.localScale = Vector3.one * (3f / Mathf.Max(0.01f, sr.sprite.bounds.size.y));
                var col = item.AddComponent<CircleCollider2D>(); col.isTrigger = true;
                col.radius = sr.sprite != null ? sr.sprite.bounds.extents.y : 0.6f;
                item.AddComponent<BKS_MissionCollectible>();
                reserved.Add(new Vector2(x - 3f, x + 3f));
                items++;
            }

            // 4. Coins (groups of 5), fuel, 2X
            var coin = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BharatKaSafar/Prefabs/Collectables/BKS_Coin.prefab");
            var fuel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/HillClimb_Core/Prefabs/Item/Fuel.prefab");
            var twoX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BharatKaSafar/Prefabs/Collectables/BKS_Multiplier2X.prefab");
            var pickGroup = Group("Pickups");
            int coins = 0, fuels = 0;
            if (coin != null)
                for (float gx = x0 + length * 0.05f; gx < x0 + length * 0.93f; gx += 35f)
                {
                    if (!Free(gx, 4f) || !Free(gx + 18f, 4f)) continue;
                    for (int k = 0; k < 6; k++)
                    {
                        float cx = gx + k * 3.5f;
                        var c = PlacePickup(coin, pickGroup, cx, ground.Y(cx), 0f, CoinScale);
                        c.transform.position += Vector3.up * (Mathf.Sin(k / 5f * Mathf.PI) * 1.2f); // small arc
                        coins++;
                    }
                }
            if (fuel != null)
            {
                var fuelXs = new List<float>();
                // fewer cans than before (was every 280): fuel has to be managed
                for (float fx = x0 + FuelEvery; fx < x0 + length - 120f; fx += FuelEvery) fuelXs.Add(fx);
                // after a bridge only when no other can is close
                foreach (var (type, bx) in bridges)
                {
                    float after = x0 + bx + BridgeLength[type] + 25f;
                    if (fuelXs.All(f => Mathf.Abs(f - after) > 150f)) fuelXs.Add(after);
                }
                foreach (var fx0 in fuelXs.OrderBy(v => v))
                {
                    float fx = fx0;
                    for (int s = 0; s < 30 && !Free(fx, 3f); s++) fx += 5f;
                    PlacePickup(fuel, pickGroup, fx, ground.Y(fx), 2.75f); fuels++;
                    reserved.Add(new Vector2(fx - 2f, fx + 2f));
                }
            }
            if (twoX != null)
            {
                float tx = x0 + length * 0.5f;
                for (int s = 0; s < 40 && !Free(tx, 4f); s++) tx += 7f;
                PlacePickup(twoX, pickGroup, tx, ground.Y(tx), 2.75f);
            }

            // 5. Stage messages
            var stageGroup = Group("StageTriggers");
            string howTo = (m.HowToDealText ?? "").Split('\n').FirstOrDefault() ?? "";
            Stage(stageGroup, x0 + length * 0.12f, ground, $"STAGE 1: {m.PlaceName}", m.PrimaryObjectiveText);
            Stage(stageGroup, x0 + length * 0.55f, ground, "STAGE 2: Obstacles aage!", howTo);
            Stage(stageGroup, x0 + length * 0.88f, ground, "FINAL: Destination paas hai!",
                  m.CollectibleCount > 0 ? $"Saare {m.CollectibleName} liye? Bas thoda aur!" : "Bas thoda aur, safar poora karo!");

            // 6. Finish flag + memory token + run length
            float endX = x0 + length - 45f;
            // Safar Diary memory token in front of the gate: collecting it completes the mission
            CreateToken(root.transform, m, endX - 10f, ground);
            FinishFlag(root.transform, endX, ground);
            var gm = roots.SelectMany(r => r.GetComponentsInChildren<GameManager>(true)).FirstOrDefault();
            // distance finish only as a fallback, after the token
            if (gm != null) { gm.targetDistance = Mathf.RoundToInt(x0 + length + 20f); EditorUtility.SetDirty(gm); }

            return $"L{level} {length}u · bridges {bridges.Count} · obstacles {obstacles} · items {items} · coins {coins} · fuel {fuels}";
        }

        // ── bridges ──────────────────────────────────────────────────────────────────────────────
        private static List<(string type, float x)> Bridges(string state, int level)
        {
            var list = new List<(string, float)>();
            foreach (var line in BridgePlan.Split('\n'))
            {
                var cells = line.Trim().Split('|');
                if (cells.Length != 6 || cells[0] != state) continue;
                string cell = cells[level];
                if (cell == "-" || !cell.Contains("@")) return list;
                var parts = cell.Split('@');
                foreach (var x in parts[1].Split(',')) if (float.TryParse(x, out float v)) list.Add((parts[0], v));
            }
            return list;
        }

        /// <summary>Lower the terrain between the bridge ends into a valley below the straight deck line.</summary>
        private static void CarveValley(TerrainEditor2D t, Ground g, float a, float b, float depth)
        {
            float ya = g.Y(a), yb = g.Y(b);
            var verts = t.GetVertsPos();
            Vector3 o = t.transform.position;
            for (int i = 0; i < verts.Length; i += 2) // even = top vertex of each column
            {
                float wx = verts[i].x + o.x;
                if (wx <= a + 1f || wx >= b - 1f) continue;
                float k = (wx - a) / (b - a);
                float deck = Mathf.Lerp(ya, yb, k);
                float target = deck - depth * Mathf.Sqrt(Mathf.Sin(k * Mathf.PI)) - o.y;
                verts[i].y = Mathf.Max(3f, Mathf.Min(verts[i].y, target));
            }
            t.EditTerrain(verts, true);
            t.UpdateCollider2D();
        }

        // Bridge art: Assets/BharatKaSafar/Art/Bridges/Bx/Bx_deck.png, Bx_railing.png, Bx_support.png,
        // Bx_end_left.png, Bx_end_right.png (+ B3_cable.png, B5_top.png). Missing parts fall back to the game's old bridge sprites.
        private const string BridgeArt = "Assets/BharatKaSafar/Art/Bridges/";
        private static readonly Dictionary<string, (string oldTheme, Color tint)> BridgeLook = new Dictionary<string, (string, Color)>
        {
            {"B1", ("desert",      new Color(0.95f, 0.85f, 0.66f))},  // stone heritage (sandstone)
            {"B2", ("countryside", new Color(0.72f, 0.72f, 0.74f))},  // modern highway (concrete)
            {"B3", ("snow",        new Color(0.70f, 0.52f, 0.34f))},  // mountain suspension (wood + cable)
            {"B4", ("cliff",       new Color(0.86f, 0.63f, 0.38f))},  // wooden forest
            {"B5", ("night",       new Color(0.55f, 0.64f, 0.76f))},  // steel truss
            {"B6", ("cliff",       new Color(0.80f, 0.74f, 0.62f))},  // waterfall / cliff
        };

        private static Sprite BridgeSprite(string type, string part)
        {
            string path = $"{BridgeArt}{type}/{type}_{part}.png";
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return null;
            if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; imp.SaveAndReimport(); }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>
        /// One solid bridge (an EdgeCollider2D deck, so the car never bounces) with the type's art:
        /// deck tiles along the span, railing on top, supports below, end caps; B3 gets a hanging cable, B5 a top frame.
        /// B4 (wooden) has small plank bumps; the others are smooth.
        /// </summary>
        private static void BuildBridge(Transform parent, string type, float a, float b, float ya, float yb, System.Random rng)
        {
            var bridge = new GameObject($"Bridge_{type}_{Mathf.RoundToInt(a)}");
            bridge.transform.SetParent(parent);
            bridge.tag = "Ground";
            var look = BridgeLook.TryGetValue(type, out var l) ? l : ("countryside", Color.white);
            var deckArt = BridgeSprite(type, "deck");
            var deck = deckArt != null ? deckArt : PlankSprite(look.Item1);
            Color tint = deckArt != null ? Color.white : look.Item2;
            float len = b - a;

            // collider
            var edge = bridge.AddComponent<EdgeCollider2D>();
            var pts = new List<Vector2>();
            for (float x = a; x <= b + 0.01f; x += 0.8f)
            {
                float k = (x - a) / len, y = Mathf.Lerp(ya, yb, k);
                if (type == "B4" && x > a + 2f && x < b - 2f) y += (Mathf.RoundToInt((x - a) / 0.8f) % 2 == 0) ? 0.1f : 0f;
                pts.Add(new Vector2(x, y));
            }
            edge.points = pts.ToArray();
            float Deck(float x) => Mathf.Lerp(ya, yb, Mathf.Clamp01((x - a) / len));

            // deck: art tiles (1.2 units high, own aspect), or the old planks
            float tileLen = deckArt != null ? Mathf.Max(2f, 1.2f * deckArt.bounds.size.x / deckArt.bounds.size.y) : 1.6f;
            int n = Mathf.Max(1, Mathf.RoundToInt(len / tileLen));
            for (int i = 0; i < n; i++)
            {
                float x0 = a + len * i / n, x1 = a + len * (i + 1) / n;
                var go = PlankObject(bridge.transform, $"Deck_{i:00}", new Vector2(x0, Deck(x0) - 0.35f), new Vector2(x1, Deck(x1) - 0.35f), deck, tint);
                if (deckArt != null) go.transform.localScale = new Vector3((x1 - x0 + 0.05f) / deckArt.bounds.size.x, 1.2f / deckArt.bounds.size.y, 1f);
            }

            // railing along the deck (behind the car)
            var rail = BridgeSprite(type, "railing");
            if (rail != null)
            {
                float rl = Mathf.Max(2f, 1.6f * rail.bounds.size.x / rail.bounds.size.y);
                int rn = Mathf.Max(1, Mathf.RoundToInt(len / rl));
                for (int i = 0; i < rn; i++)
                {
                    float x0 = a + len * i / rn, x1 = a + len * (i + 1) / rn, xm = (x0 + x1) * 0.5f;
                    var go = Decor(bridge.transform, "Railing", rail, new Vector2(xm, Deck(xm) + 0.8f), x1 - x0 + 0.05f, 1.6f, 7);
                    go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(Deck(x1) - Deck(x0), x1 - x0) * Mathf.Rad2Deg);
                }
            }

            // supports under the deck (B3: one tower at each end)
            var support = BridgeSprite(type, "support");
            var white = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var xs = new List<float>();
            if (type == "B3") { xs.Add(a); xs.Add(b); }
            else { float sp = type == "B4" ? 9f : 14f; for (float x = a + sp * 0.6f; x < b - sp * 0.4f; x += sp) xs.Add(x); }
            foreach (float x in xs)
            {
                float y = Deck(x);
                if (support != null)
                {
                    if (type == "B3") Decor(bridge.transform, "Tower", support, new Vector2(x, y + 3.5f), 0f, 9f, 6);
                    else Decor(bridge.transform, "Support", support, new Vector2(x, y - 4.7f), 0f, 8f, 6);
                }
                else
                {
                    var p = Decor(bridge.transform, "Pillar", white, new Vector2(x, type == "B3" ? y + 3.5f : y - 10.4f), type == "B4" ? 0.6f : 1.4f, type == "B3" ? 9f : 20f, 6);
                    p.GetComponent<SpriteRenderer>().color = look.Item2 * 0.7f;
                }
            }

            // end caps
            var endL = BridgeSprite(type, "end_left"); var endR = BridgeSprite(type, "end_right");
            if (endL != null) Decor(bridge.transform, "EndLeft", endL, new Vector2(a - 1.2f, ya - 0.4f), 0f, 2.6f, 9);
            if (endR != null) Decor(bridge.transform, "EndRight", endR, new Vector2(b + 1.2f, yb - 0.4f), 0f, 2.6f, 9);

            // B3: hanging cable between the towers; B5: top truss frame
            if (type == "B3")
            {
                var cable = BridgeSprite(type, "cable");
                if (cable != null) Decor(bridge.transform, "Cable", cable, new Vector2((a + b) * 0.5f, (ya + yb) * 0.5f + 5f), len, 0f, 7);
                else
                {
                    var lr = new GameObject("Cable").AddComponent<LineRenderer>();
                    lr.transform.SetParent(bridge.transform);
                    lr.useWorldSpace = true; lr.widthMultiplier = 0.15f; lr.sortingOrder = 7;
                    lr.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
                    lr.startColor = lr.endColor = new Color(0.25f, 0.22f, 0.2f);
                    lr.positionCount = 21;
                    for (int i = 0; i <= 20; i++) { float k = i / 20f, x = Mathf.Lerp(a, b, k); lr.SetPosition(i, new Vector3(x, Deck(x) + 8f - 6f * Mathf.Sin(k * Mathf.PI), 0f)); }
                }
            }
            if (type == "B5")
            {
                var top = BridgeSprite(type, "top");
                if (top != null) Decor(bridge.transform, "TopFrame", top, new Vector2((a + b) * 0.5f, (ya + yb) * 0.5f + 4.5f), len * 0.8f, 0f, 7);
            }
        }

        /// <summary>Decoration sprite; width or height 0 = keep the sprite's aspect from the other one.</summary>
        private static GameObject Decor(Transform parent, string name, Sprite sprite, Vector2 pos, float width, float height, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = order;
            if (sprite != null)
            {
                Vector2 sz = sprite.bounds.size;
                float sx = width > 0f ? width / sz.x : height / sz.y;
                float sy = height > 0f ? height / sz.y : sx;
                go.transform.localScale = new Vector3(sx, sy, 1f);
            }
            return go;
        }

        private static GameObject PlankObject(Transform parent, string name, Vector2 p0, Vector2 p1, Sprite sprite, Color tint)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.tag = "Ground";
            Vector2 d = p1 - p0;
            go.transform.position = (p0 + p1) * 0.5f;
            go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.color = tint; sr.sortingOrder = 9;
            if (sprite != null) go.transform.localScale = new Vector3((d.magnitude + 0.05f) / sprite.bounds.size.x, 0.8f / sprite.bounds.size.y, 1f);
            return go;
        }

        private static Rigidbody2D Anchor(Transform parent, Vector2 at)
        {
            var go = new GameObject("Anchor");
            go.transform.SetParent(parent);
            go.transform.position = at;
            var rb = go.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Static;
            return rb;
        }

        private static Sprite PlankSprite(string theme)
        {
            string path = $"Assets/Graphics/assets/scene/{theme}/bridge.png";
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null && imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; imp.SaveAndReimport(); }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ── small helpers ────────────────────────────────────────────────────────────────────────
        private static void Stage(Transform parent, float x, Ground g, string title, string text)
        {
            var go = new GameObject($"Stage_{title.Split(':')[0].Replace(" ", "")}");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(x, g.Y(x) + 5f, 0f);
            var box = go.AddComponent<BoxCollider2D>(); box.isTrigger = true; box.size = new Vector2(2f, 20f);
            var st = go.AddComponent<TN_StageTrigger>();
            st.stageTitle = title; st.stageDialogue = text;
        }

        // All floating pickups (coins, fuel, 2X, mission items) sit this far above the road
        private const float PickupGap = 1.5f;
        private const float FuelEvery = 420f; // fuel can spacing (units)
        private const float CoinScale = 0.128f; // rupee sprite is 12.54 units -> ~1.6 units on screen

        private static GameObject Place(GameObject prefab, Transform parent, float x, float y, float height = 0f)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = new Vector3(x, y, 0f);
            if (height > 0f) FitHeight(go, height);
            return go;
        }

        /// <summary>Pickup whose lowest point is exactly PickupGap above the ground at x.</summary>
        private static GameObject PlacePickup(GameObject prefab, Transform parent, float x, float groundY, float height, float scale = 0f)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = new Vector3(x, groundY, 0f);
            if (scale > 0f) go.transform.localScale = new Vector3(scale, scale, 1f);
            else if (height > 0f) FitHeight(go, height);
            var rs = go.GetComponentsInChildren<SpriteRenderer>(true);
            if (rs.Length > 0)
            {
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                go.transform.position += Vector3.up * (groundY + PickupGap - b.min.y);
            }
            return go;
        }

        /// <summary>Scale an object so its sprites are this tall (world units). Vehicles are ~6.5 long, ~2.5 tall.</summary>
        private static void FitHeight(GameObject go, float height)
        {
            var rs = go.GetComponentsInChildren<SpriteRenderer>(true);
            if (rs.Length == 0) return;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            if (b.size.y > 0.001f) go.transform.localScale *= height / b.size.y;
        }

        private static IEnumerable<MissionData> AllMissions() =>
            AssetDatabase.FindAssets("t:MissionData").Select(g => AssetDatabase.LoadAssetAtPath<MissionData>(AssetDatabase.GUIDToAssetPath(g))).Where(m => m != null);

        private static MissionData FindMission(string sceneName) =>
            AllMissions().FirstOrDefault(m => m.DraftSceneName == sceneName || m.GameplaySceneName == sceneName);

        private static int StableHash(string s) { unchecked { int h = (int)2166136261; foreach (char c in s) h = (h ^ c) * 16777619; return h; } }

        /// <summary>Terrain surface height (world) at any x, read from the terrain mesh.</summary>
        private class Ground
        {
            private readonly float[] xs, ys;
            public Ground(TerrainEditor2D t) : this(new List<TerrainEditor2D> { t }) { }
            public Ground(IEnumerable<TerrainEditor2D> ts)
            {
                var top = new List<Vector2>();
                foreach (var t in ts)
                {
                    var v = t.GetVertsPos(); var o = t.transform.position;
                    for (int i = 0; i < v.Length; i += 2) top.Add(new Vector2(v[i].x + o.x, v[i].y + o.y));
                }
                top.Sort((p, q) => p.x.CompareTo(q.x));
                xs = top.Select(p => p.x).ToArray(); ys = top.Select(p => p.y).ToArray();
            }
            public float Y(float x)
            {
                if (xs.Length == 0) return 0f;
                if (x <= xs[0]) return ys[0];
                if (x >= xs[xs.Length - 1]) return ys[ys.Length - 1];
                int i = System.Array.BinarySearch(xs, x); if (i < 0) i = ~i;
                float k = (x - xs[i - 1]) / Mathf.Max(0.0001f, xs[i] - xs[i - 1]);
                return Mathf.Lerp(ys[i - 1], ys[i], k);
            }
            public float Slope(float x) => (Y(x + 2f) - Y(x - 2f)) / 4f;
        }
    }
}
