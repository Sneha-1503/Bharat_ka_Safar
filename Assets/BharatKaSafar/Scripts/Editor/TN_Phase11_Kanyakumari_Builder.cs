using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BharatKaSafar.Gameplay;
using System.IO;

public class TN_Phase11_Kanyakumari_Builder
{
    [MenuItem("Bharat Ka Safar/Automate Phase 11 (Build Kanyakumari Level)")]
    public static void BuildKanyakumariLevel()
    {
        string originalScenePath = "Assets/BharatKaSafar/Scenes/BKS_TN_M04_Ooty.unity";
        
        if (!File.Exists(originalScenePath))
        {
            Debug.LogError("[BKS Builder] Could not find Ooty scene. Did you run Phase 10 first?");
            return;
        }

        string newScenePath = "Assets/BharatKaSafar/Scenes/BKS_TN_M05_Kanyakumari.unity";
        
        if (AssetDatabase.CopyAsset(originalScenePath, newScenePath))
        {
            Scene newScene = EditorSceneManager.OpenScene(newScenePath, OpenSceneMode.Single);

            // 1. Delete Ooty's Terrain
            GameObject oldTerrain = GameObject.Find("TN_Ooty_Terrain");
            if (oldTerrain != null)
            {
                GameObject.DestroyImmediate(oldTerrain);
            }

            // 2. Create New Kanyakumari Terrain Root
            GameObject terrainRoot = new GameObject("TN_Kanyakumari_Terrain");
            
            // Define the layout requested for Kanyakumari (Grand Finale combining elements):
            // START -> Village Flat -> Small Hill -> Coastal Straight -> Rocky Dip -> Bridge -> Small Jump -> Downhill -> Fast Coastal Section -> Medium Rocky Climb -> Final Hill -> Long Scenic Straight -> Finish
            string[] layout = {
                "TN_Terrain_Flat",               // Start / Village Flat
                "TN_Terrain_SmallHill",          // Small Hill
                "TN_Terrain_Flat_Long",          // Coastal Straight
                "TN_Terrain_Valley",             // Rocky Dip
                "TN_Terrain_Bridge",             // Bridge
                "TN_Terrain_Jump",               // Small Jump
                "TN_Terrain_Landing",            // Landing for Jump
                "TN_Terrain_Downhill",           // Downhill
                "TN_Terrain_Flat_Long",          // Fast Coastal Section
                "TN_Terrain_RockyClimb",         // Medium Rocky Climb
                "TN_Terrain_LargeHill",          // Final Hill
                "TN_Terrain_Flat_Long",          // Long Scenic Straight
                "TN_Terrain_Flat"                // Finish
            };

            Transform previousEnd = null;
            float currentX = 0f;

            for (int i = 0; i < layout.Length; i++)
            {
                string prefabPath = $"Assets/BharatKaSafar/Prefabs/Terrain/TamilNadu/Core/{layout[i]}.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                
                if (prefab != null)
                {
                    GameObject piece = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    piece.transform.SetParent(terrainRoot.transform);
                    
                    TN_TerrainModule module = piece.GetComponent<TN_TerrainModule>();
                    
                    if (previousEnd != null && module != null && module.startPoint != null)
                    {
                        Vector3 offset = previousEnd.position - module.startPoint.position;
                        piece.transform.position += offset;
                    }
                    else
                    {
                        piece.transform.position = new Vector3(currentX, 0, 0);
                    }

                    if (module != null && module.endPoint != null)
                    {
                        previousEnd = module.endPoint;
                        currentX = module.endPoint.position.x;
                    }

                    // 3. Add Stage Triggers at specific intervals for Kanyakumari
                    if (i == 1) CreateStageTrigger(piece.transform, "STAGE 1: SOUTHERN ROAD", "Final journey shuru! Saare Safar Stars collect karte hue aage badho.");
                    if (i == 4) CreateStageTrigger(piece.transform, "STAGE 2: COASTAL BRIDGE", "Bridge ahead! Balance aur speed dono important hain.");
                    if (i == 9) CreateStageTrigger(piece.transform, "STAGE 3: FINAL RUSH", "Kanyakumari saamne hai! Last obstacles ko clear karo!");
                    if (i == 11) CreateStageTrigger(piece.transform, "FINAL EVENT: DESTINATION SPOTTED", "Destination spotted! Final Safar Star collect karo!");
                }
                else
                {
                    Debug.LogWarning($"[BKS Builder] Missing prefab: {layout[i]}. Skipping.");
                }
            }

            // 4. Save and Add to Build Settings
            EditorSceneManager.SaveScene(newScene);
            AddSceneToBuildSettings(newScenePath);
            
            Debug.Log("[BKS Builder] Phase 11 Complete! BKS_TN_M05_Kanyakumari created! The ultimate finale!");
        }
    }

    private static void CreateStageTrigger(Transform parent, string title, string dialogue)
    {
        GameObject triggerObj = new GameObject($"StageTrigger_{title.Split(':')[0].Replace(" ", "")}");
        triggerObj.transform.SetParent(parent);
        triggerObj.transform.localPosition = new Vector3(0, 2f, 0);

        BoxCollider2D box = triggerObj.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(2f, 15f); 

        TN_StageTrigger trigger = triggerObj.AddComponent<TN_StageTrigger>();
        trigger.stageTitle = title;
        trigger.stageDialogue = dialogue;
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
        bool exists = false;
        foreach (var s in buildScenes)
        {
            if (s.path == scenePath) { exists = true; break; }
        }

        if (!exists)
        {
            EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[buildScenes.Length + 1];
            System.Array.Copy(buildScenes, newScenes, buildScenes.Length);
            newScenes[buildScenes.Length] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = newScenes;
        }
    }
}
