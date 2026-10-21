using UnityEditor;
using UnityEngine;
using BharatKaSafar.Obstacles;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Builds ready-to-place obstacle prefabs from Art/Missions/Obstacles into Prefabs/Obstacles/Patterns:
    ///   Horn Clear : Barricade (slide aside), Crate (tip over), Log (roll away), RockPile (crumble)
    ///   Crossing   : Cow, Cart
    ///   Pothole    : Pothole
    ///   Surface    : MudPatch, SandPatch, SnowPatch (sand/snow use placeholder colour until art arrives)
    /// The origin of every prefab is the point that sits ON the road, so drop them onto the terrain surface.
    /// Menu: Bharat Ka Safar > Obstacles > Build Obstacle Prefabs
    /// </summary>
    public static class BKS_ObstaclePrefabBuilder
    {
        private const string Art = "Assets/BharatKaSafar/Art/Missions/Obstacles/";
        private const string Out = "Assets/BharatKaSafar/Prefabs/Obstacles/Patterns";

        [MenuItem("Bharat Ka Safar/Obstacles/Build Obstacle Prefabs")]
        public static void Build()
        {
            BKS_MissionPlannerImporter.SetSprites(Art, 512);
            if (!AssetDatabase.IsValidFolder("Assets/BharatKaSafar/Prefabs/Obstacles")) AssetDatabase.CreateFolder("Assets/BharatKaSafar/Prefabs", "Obstacles");
            if (!AssetDatabase.IsValidFolder(Out)) AssetDatabase.CreateFolder("Assets/BharatKaSafar/Prefabs/Obstacles", "Patterns");

            int n = 0;
            n += HornClear("BKS_Barricade", "barricade.png", 3.75f, BKS_HornClearObstacle.ClearStyle.SlideAside);
            n += HornClear("BKS_Crate", "crate.png", 3.0f, BKS_HornClearObstacle.ClearStyle.TipOver);
            n += HornClear("BKS_Log", "log.png", 2.1f, BKS_HornClearObstacle.ClearStyle.RollAway);
            n += HornClear("BKS_RockPile", "rock.png", 2.5f, BKS_HornClearObstacle.ClearStyle.Crumble);
            n += Crossing("BKS_CowCrossing", "animal-cow.png", 4.25f);
            n += Crossing("BKS_CartCrossing", "cart.png", 4.0f);
            n += Pothole("BKS_Pothole", "pothole-mud.png");
            n += Surface("BKS_MudPatch", BKS_SurfaceZone.Surface.Mud, Sprite("pothole-mud.png"), Color.white);
            n += Surface("BKS_SandPatch", BKS_SurfaceZone.Surface.Sand, null, new Color(0.95f, 0.8f, 0.45f, 0.85f));
            n += Surface("BKS_SnowPatch", BKS_SurfaceZone.Surface.Snow, null, new Color(0.92f, 0.96f, 1f, 0.9f));
            AssetDatabase.SaveAssets();
            Debug.Log($"[Obstacles] Built {n} prefabs in {Out}. Place them on the road surface; the HUD Safar Star total counts them automatically.");
        }

        private static int HornClear(string name, string sprite, float height, BKS_HornClearObstacle.ClearStyle style)
        {
            var root = Root(name, sprite, height, out var sr);
            if (root == null) return 0;
            var box = root.AddComponent<BoxCollider2D>();
            FitBox(box, sr, 0.85f);
            var o = root.AddComponent<BKS_HornClearObstacle>();
            var so = new SerializedObject(o);
            so.FindProperty("clearStyle").enumValueIndex = (int)style;
            so.ApplyModifiedPropertiesWithoutUndo();
            return Save(root, name);
        }

        private static int Crossing(string name, string sprite, float height)
        {
            var root = Root(name, sprite, height, out var sr);
            if (root == null) return 0;
            var box = root.AddComponent<BoxCollider2D>();
            FitBox(box, sr, 0.8f);
            root.AddComponent<BKS_CrossingObstacle>();
            return Save(root, name);
        }

        private static int Pothole(string name, string sprite)
        {
            var root = Root(name, sprite, 0.9f, out var sr, width: 6.9f, sinkFraction: 0.6f);
            if (root == null) return 0;
            var box = root.AddComponent<BoxCollider2D>();
            box.isTrigger = true; box.size = new Vector2(6f, 5f); box.offset = new Vector2(0f, 2f);
            root.AddComponent<BKS_PotholeZone>();
            return Save(root, name);
        }

        private static int Surface(string name, BKS_SurfaceZone.Surface kind, Sprite sprite, Color tint)
        {
            const float length = 17f;
            var root = new GameObject(name);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = sprite != null ? sprite : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.color = tint; sr.sortingOrder = 4;
            // a flat strip lying on the road
            Vector2 size = sr.sprite.bounds.size;
            visual.transform.localScale = new Vector3(length / size.x, 1f / size.y, 1f);
            visual.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            var box = root.AddComponent<BoxCollider2D>();
            box.isTrigger = true; box.size = new Vector2(length, 4f); box.offset = new Vector2(0f, 1.5f);
            var zone = root.AddComponent<BKS_SurfaceZone>();
            var so = new SerializedObject(zone);
            so.FindProperty("surface").enumValueIndex = (int)kind;
            so.ApplyModifiedPropertiesWithoutUndo();
            return Save(root, name);
        }

        /// <summary>Root at road level, sprite child scaled to the wanted height (or width) and lifted so its base sits on the road.</summary>
        private static GameObject Root(string name, string sprite, float height, out SpriteRenderer sr, float width = 0f, float sinkFraction = 0f)
        {
            sr = null;
            var sp = Sprite(sprite);
            if (sp == null) { Debug.LogWarning("[Obstacles] Missing sprite " + Art + sprite); return null; }
            var root = new GameObject(name);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = sp; sr.sortingOrder = 5;
            Vector2 size = sp.bounds.size;
            float s = width > 0f ? width / size.x : height / size.y;
            visual.transform.localScale = new Vector3(s, s, 1f);
            float h = size.y * s;
            visual.transform.localPosition = new Vector3(0f, h * (0.5f - sinkFraction), 0f);
            return root;
        }

        private static void FitBox(BoxCollider2D box, SpriteRenderer sr, float shrink)
        {
            var t = sr.transform;
            Vector2 size = Vector2.Scale(sr.sprite.bounds.size, t.localScale);
            box.size = size * shrink;
            box.offset = (Vector2)t.localPosition;
        }

        private static int Save(GameObject root, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(root, $"{Out}/{name}.prefab", out bool ok);
            Object.DestroyImmediate(root);
            return ok ? 1 : 0;
        }

        private static Sprite Sprite(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + file);
    }
}
