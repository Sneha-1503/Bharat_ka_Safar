using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using System.IO;

/// <summary>
/// Automatically applies the TamilNadu sprite pack to all 5 TN scenes.
/// Assigns background sprites, terrain sprites, and HUD sprites.
/// </summary>
public class TN_SpriteApplier
{
    private const string ART_ROOT = "Assets/BharatKaSafar/Art/Chapters/TamilNadu";

    [MenuItem("Bharat Ka Safar/Apply Tamil Nadu Sprite Pack")]
    public static void ApplyAllSprites()
    {
        ApplyTerrainSpritesToPrefabs();
        ApplyBackgroundAndHUDToScenes();
        AssetDatabase.Refresh();
        Debug.Log("[TN Sprite Applier] ✅ All sprites applied! Open any TN scene to see the results.");
    }

    // ── STEP 1: Apply terrain sprites to modular prefabs ──────────────────────────
    private static void ApplyTerrainSpritesToPrefabs()
    {
        string terrainArtPath = $"{ART_ROOT}/Terrain";
        string prefabFolder   = "Assets/BharatKaSafar/Prefabs/Terrain/TamilNadu/Core";

        // Map prefab name → sprite name
        var terrainMap = new System.Collections.Generic.Dictionary<string, string>
        {
            { "TN_Terrain_Flat",           "tn_road_platform" },
            { "TN_Terrain_Flat_Long",      "tn_road_seamless" },
            { "TN_Terrain_SmallSlopeUp",   "tn_road_ramp_up" },
            { "TN_Terrain_SmallSlopeDown", "tn_road_ramp_down" },
            { "TN_Terrain_SmallHill",      "tn_road_platform" },
            { "TN_Terrain_Bridge",         "tn_road_platform" },
            { "TN_Terrain_Valley",         "tn_road_ramp_down" },
            { "TN_Terrain_RockyClimb",     "tn_road_ramp_up" },
            { "TN_Terrain_Jump",           "tn_road_ramp_up" },
            { "TN_Terrain_Landing",        "tn_road_ramp_down" },
            { "TN_Terrain_SteepClimb",     "tn_road_ramp_up" },
            { "TN_Terrain_LargeHill",      "tn_road_platform" },
            { "TN_Terrain_Downhill",       "tn_road_ramp_down" },
        };

        foreach (var entry in terrainMap)
        {
            string prefabPath  = $"{prefabFolder}/{entry.Key}.prefab";
            string spritePath  = $"{terrainArtPath}/{entry.Value}.png";
            
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null) 
            {
                Debug.LogWarning($"  Sprite not found: {spritePath}");
                continue;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"  Prefab not found: {prefabPath}");
                continue;
            }

            // Check if the prefab already has a SpriteRenderer
            SpriteRenderer sr = prefab.GetComponentInChildren<SpriteRenderer>();
            if (sr == null)
            {
                // Add a child SpriteRenderer object
                var go = new GameObject("Visual");
                go.transform.SetParent(prefab.transform);
                go.transform.localPosition = Vector3.zero;
                sr = go.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = "Background";
                sr.sortingOrder = 1;
            }

            sr.sprite = sprite;
            EditorUtility.SetDirty(prefab);
            Debug.Log($"  → Applied {entry.Value} to {entry.Key}");
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[TN Sprite Applier] Terrain prefabs updated!");
    }

    // ── STEP 2: Apply per-scene backgrounds ───────────────────────────────────────
    private static void ApplyBackgroundAndHUDToScenes()
    {
        // Scene → background sprite filename
        var sceneMap = new System.Collections.Generic.Dictionary<string, string>
        {
            { "BKS_TN_M01_Madurai",        "tn_scenic_background" },
            { "BKS_TN_M02_Chennai",        "tn_chennai_coastal_background" },
            { "BKS_TN_M03_Mahabalipuram", "tn_mahabalipuram_heritage_background" },
            { "BKS_TN_M04_Ooty",          "tn_ooty_mountain_background" },
            { "BKS_TN_M05_Kanyakumari",   "tn_kanyakumari_coastal_background" },
        };

        foreach (var entry in sceneMap)
        {
            string[] guids = AssetDatabase.FindAssets($"{entry.Key} t:Scene");
            if (guids.Length == 0)
            {
                Debug.LogWarning($"[TN Sprite Applier] Scene not found: {entry.Key}");
                continue;
            }

            string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // ── Apply Background Sprite ────────────────────────────────────────
            string bgSpritePath = $"{ART_ROOT}/Background/{entry.Value}.png";
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgSpritePath);

            if (bgSprite != null)
            {
                // Find background SpriteRenderer in scene (look for "Background" or "[BKS_Overlay]")
                GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
                foreach (var obj in allObjects)
                {
                    if (obj.name.Contains("Background") || obj.name.Contains("BG") || obj.name.Contains("BKS_Overlay"))
                    {
                        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            sr.sprite = bgSprite;
                            EditorUtility.SetDirty(obj);
                            Debug.Log($"  → Background applied to '{obj.name}' in {entry.Key}");
                            break;
                        }
                    }
                }
            }

            // ── Apply HUD Sprites to Canvas UI Images ─────────────────────────
            ApplyHUDSprites();

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"  ✅ {entry.Key} scene updated!");
        }
    }

    private static void ApplyHUDSprites()
    {
        string hudPath = $"{ART_ROOT}/HUD";
        
        // Map UI object name → sprite file
        var hudMap = new System.Collections.Generic.Dictionary<string, string>
        {
            { "FuelSlider",       "tn_fuel_frame" },
            { "DistanceSlider",   "tn_progress_frame" },
            { "FuelCan",          "tn_fuel_can" },
            { "FinishFlag",       "tn_finish_flag" },
            { "Checkpoint",       "tn_checkpoint" },
            { "PauseButton",      "tn_pause_button" },
        };

        foreach (var entry in hudMap)
        {
            GameObject obj = GameObject.Find(entry.Key);
            if (obj == null) continue;

            string spritePath = $"{hudPath}/{entry.Value}.png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null) continue;

            Image img = obj.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = sprite;
                EditorUtility.SetDirty(obj);
            }
        }
    }
}
