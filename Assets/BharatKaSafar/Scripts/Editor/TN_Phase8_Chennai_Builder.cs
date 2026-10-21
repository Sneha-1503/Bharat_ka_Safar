using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BharatKaSafar.Gameplay;
using System.IO;

public class TN_Phase8_Chennai_Builder
{
    [MenuItem("Bharat Ka Safar/Automate Phase 8 (Build Chennai Level)")]
    public static void BuildChennaiLevel()
    {
        string originalScenePath = "Assets/BharatKaSafar/Scenes/BKS_TN_M01_Madurai.unity";
        
        if (!File.Exists(originalScenePath))
        {
            Debug.LogError("[BKS Builder] Could not find Madurai scene. Did you run Phase 6 first?");
            return;
        }

        string newScenePath = "Assets/BharatKaSafar/Scenes/BKS_TN_M02_Chennai.unity";
        
        if (AssetDatabase.CopyAsset(originalScenePath, newScenePath))
        {
            Scene newScene = EditorSceneManager.OpenScene(newScenePath, OpenSceneMode.Single);

            // 1. Delete Madurai's Terrain
            GameObject oldTerrain = GameObject.Find("TN_Madurai_Terrain");
            if (oldTerrain != null)
            {
                GameObject.DestroyImmediate(oldTerrain);
            }

            // 2. Create New Chennai Terrain Root
            GameObject terrainRoot = new GameObject("TN_Chennai_Terrain");
            
            // Define the layout requested for Chennai:
            // START -> Long Flat -> Gentle Downhill -> Long High-Speed Flat -> Gentle Uphill -> Small Dip -> Coastal Straight -> Finish
            string[] layout = {
                "TN_Terrain_Flat",               // Start
                "TN_Terrain_Flat_Long",          // Long Flat
                "TN_Terrain_SmallSlopeDown",     // Gentle Downhill
                "TN_Terrain_Flat_Long",          // Long High-Speed Flat
                "TN_Terrain_Flat_Long",          // (Extend High Speed section)
                "TN_Terrain_SmallSlopeUp",       // Gentle Uphill
                "TN_Terrain_SmallSlopeDown",     // Small Dip down
                "TN_Terrain_SmallSlopeUp",       // Small Dip up
                "TN_Terrain_Flat_Long",          // Coastal Straight
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

                    // 3. Add Stage Triggers at specific intervals for Chennai
                    if (i == 1) CreateStageTrigger(piece.transform, "STAGE 1: BEACH ROAD", "Samundar ek taraf, road doosri taraf — coastal ride shuru!");
                    if (i == 3) CreateStageTrigger(piece.transform, "STAGE 2: FAST TRAFFIC", "Traffic speed badh rahi hai! Timing sahi rakho.");
                    if (i == 7) CreateStageTrigger(piece.transform, "STAGE 3: LIGHTHOUSE SPRINT", "Lighthouse saamne hai! Final stretch mein stars miss mat karna!");
                }
            }

            // 4. Save and Add to Build Settings
            EditorSceneManager.SaveScene(newScene);
            AddSceneToBuildSettings(newScenePath);
            
            Debug.Log("[BKS Builder] Phase 8 Complete! BKS_TN_M02_Chennai created with long high-speed coastal terrain and Stage Triggers!");
        }
    }

    private static void CreateStageTrigger(Transform parent, string title, string dialogue)
    {
        GameObject triggerObj = new GameObject($"StageTrigger_{title.Split(':')[0].Replace(" ", "")}");
        triggerObj.transform.SetParent(parent);
        triggerObj.transform.localPosition = new Vector3(0, 2f, 0);

        BoxCollider2D box = triggerObj.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(2f, 10f);

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
