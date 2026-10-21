using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using System.IO;

public class TN_FixAndApply
{
    private const string ART_ROOT = "Assets/BharatKaSafar/Art/Chapters/TamilNadu";

    // ─────────────────────────────────────────────────────────────────────────────
    // STEP 1: Fix Texture Import Settings (MUST run first)
    // ─────────────────────────────────────────────────────────────────────────────
    [MenuItem("Bharat Ka Safar/STEP 1 - Fix Sprite Import Settings")]
    public static void FixImportSettings()
    {
        string[] pngGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { ART_ROOT });
        int count = 0;

        foreach (string guid in pngGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType           = TextureImporterType.Sprite;
                importer.spriteImportMode      = SpriteImportMode.Single;
                importer.alphaIsTransparency   = true;
                importer.mipmapEnabled         = false;
                importer.filterMode            = FilterMode.Bilinear;
                importer.textureCompression    = TextureImporterCompression.Uncompressed;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                count++;
                Debug.Log($"  Fixed: {Path.GetFileName(path)}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[TN Fix] ✅ Fixed {count} sprites! Now run STEP 2.");
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // STEP 2: Apply sprites to terrain prefabs + scenes
    // ─────────────────────────────────────────────────────────────────────────────
    [MenuItem("Bharat Ka Safar/STEP 2 - Apply Sprites to Scenes & Prefabs")]
    public static void ApplyAll()
    {
        ApplyTerrainSprites();
        ApplySceneBackgrounds();
        Debug.Log("[TN Apply] ✅ All done! Open any TN scene and press Play.");
    }

    // ── Terrain Prefabs ───────────────────────────────────────────────────────────
    private static void ApplyTerrainSprites()
    {
        string prefabFolder = "Assets/BharatKaSafar/Prefabs/Terrain/TamilNadu/Core";
        string terrainArt   = $"{ART_ROOT}/Terrain";

        var map = new System.Collections.Generic.Dictionary<string, string>
        {
            { "TN_Terrain_Flat",           "tn_road_platform" },
            { "TN_Terrain_Flat_Long",      "tn_road_seamless" },
            { "TN_Terrain_SmallSlopeUp",   "tn_road_ramp_up" },
            { "TN_Terrain_SmallSlopeDown", "tn_road_ramp_down" },
            { "TN_Terrain_SmallHill",      "tn_road_platform" },
            { "TN_Terrain_Bridge",         "tn_road_seamless" },
            { "TN_Terrain_Valley",         "tn_road_ramp_down" },
            { "TN_Terrain_RockyClimb",     "tn_road_ramp_up" },
            { "TN_Terrain_Jump",           "tn_road_ramp_up" },
            { "TN_Terrain_Landing",        "tn_road_ramp_down" },
            { "TN_Terrain_SteepClimb",     "tn_road_ramp_up" },
            { "TN_Terrain_LargeHill",      "tn_road_seamless" },
            { "TN_Terrain_Downhill",       "tn_road_ramp_down" },
        };

        foreach (var kv in map)
        {
            string spritePath = $"{terrainArt}/{kv.Value}.png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null) { Debug.LogWarning($"Sprite missing: {spritePath}"); continue; }

            string prefabPath = $"{prefabFolder}/{kv.Key}.prefab";
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null) { Debug.LogWarning($"Prefab missing: {prefabPath}"); continue; }

            // Edit the prefab directly
            using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                GameObject root = scope.prefabContentsRoot;

                // Find or create a "Visual" child with SpriteRenderer
                Transform visual = root.transform.Find("Visual");
                GameObject visualGO;
                if (visual == null)
                {
                    visualGO = new GameObject("Visual");
                    visualGO.transform.SetParent(root.transform);
                    visualGO.transform.localPosition = Vector3.zero;
                    visualGO.transform.localScale = Vector3.one;
                }
                else
                {
                    visualGO = visual.gameObject;
                }

                SpriteRenderer sr = visualGO.GetComponent<SpriteRenderer>();
                if (sr == null) sr = visualGO.AddComponent<SpriteRenderer>();

                sr.sprite           = sprite;
                sr.sortingLayerName = "Default";
                sr.sortingOrder     = 0;
            }
            Debug.Log($"  Prefab updated: {kv.Key} → {kv.Value}");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[TN Apply] Terrain prefabs done!");
    }

    // ── Per-scene Backgrounds ─────────────────────────────────────────────────────
    private static void ApplySceneBackgrounds()
    {
        var sceneMap = new System.Collections.Generic.Dictionary<string, string>
        {
            { "BKS_TN_M01_Madurai",         "tn_scenic_background" },
            { "BKS_TN_M02_Chennai",         "tn_chennai_coastal_background" },
            { "BKS_TN_M03_Mahabalipuram",   "tn_mahabalipuram_heritage_background" },
            { "BKS_TN_M04_Ooty",            "tn_ooty_mountain_background" },
            { "BKS_TN_M05_Kanyakumari",     "tn_kanyakumari_coastal_background" },
        };

        foreach (var entry in sceneMap)
        {
            string[] guids = AssetDatabase.FindAssets($"{entry.Key} t:Scene");
            if (guids.Length == 0) { Debug.LogWarning($"Scene not found: {entry.Key}"); continue; }

            string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            string bgPath  = $"{ART_ROOT}/Background/{entry.Value}.png";
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgPath);

            if (bgSprite == null)
            {
                Debug.LogWarning($"  Background sprite not found: {bgPath}");
            }
            else
            {
                // Try to find any background SpriteRenderer in the scene
                SpriteRenderer[] allSRs = Object.FindObjectsOfType<SpriteRenderer>();
                bool applied = false;
                foreach (var sr in allSRs)
                {
                    string n = sr.gameObject.name.ToLower();
                    if (n.Contains("bg") || n.Contains("background") || n.Contains("overlay") || n.Contains("sky"))
                    {
                        sr.sprite = bgSprite;
                        EditorUtility.SetDirty(sr.gameObject);
                        Debug.Log($"  → BG applied to '{sr.gameObject.name}' in {entry.Key}");
                        applied = true;
                        break;
                    }
                }

                // If no BG object exists, CREATE one
                if (!applied)
                {
                    GameObject bgObj = new GameObject("TN_Background");
                    bgObj.transform.position = new Vector3(50f, 5f, 10f); // Behind everything (Z=10)
                    bgObj.transform.localScale = new Vector3(30f, 20f, 1f);
                    SpriteRenderer newSR = bgObj.AddComponent<SpriteRenderer>();
                    newSR.sprite = bgSprite;
                    newSR.sortingLayerName = "Default";
                    newSR.sortingOrder = -10; // Behind everything
                    Debug.Log($"  → New TN_Background object created in {entry.Key}");
                }
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"  ✅ Saved: {entry.Key}");
        }
    }
}
