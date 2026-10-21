using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BharatKaSafar.Gameplay;
using System.IO;

public class TN_Phase10_Ooty_Builder
{
    [MenuItem("Bharat Ka Safar/Automate Phase 10 (Build Ooty Level)")]
    public static void BuildOotyLevel()
    {
        EnsureNewPrefabsExist();

        string originalScenePath = "Assets/BharatKaSafar/Scenes/BKS_TN_M03_Mahabalipuram.unity";
        
        if (!File.Exists(originalScenePath))
        {
            Debug.LogError("[BKS Builder] Could not find Mahabalipuram scene. Did you run Phase 9 first?");
            return;
        }

        string newScenePath = "Assets/BharatKaSafar/Scenes/BKS_TN_M04_Ooty.unity";
        
        if (AssetDatabase.CopyAsset(originalScenePath, newScenePath))
        {
            Scene newScene = EditorSceneManager.OpenScene(newScenePath, OpenSceneMode.Single);

            // 1. Delete Mahabalipuram's Terrain
            GameObject oldTerrain = GameObject.Find("TN_Mahabalipuram_Terrain");
            if (oldTerrain != null)
            {
                GameObject.DestroyImmediate(oldTerrain);
            }

            // 2. Create New Ooty Terrain Root
            GameObject terrainRoot = new GameObject("TN_Ooty_Terrain");
            
            // Define the layout requested for Ooty:
            // START -> Gentle Climb -> Medium Hill -> Valley -> Steep Climb -> Short Flat -> Falling Rock Zone (Flat) -> Downhill -> Large Hill -> Fog Zone (Flat) -> Valley -> Final Steep Climb -> Finish
            string[] layout = {
                "TN_Terrain_Flat",               // Start
                "TN_Terrain_SmallSlopeUp",       // Gentle Climb
                "TN_Terrain_SmallHill",          // Medium Hill
                "TN_Terrain_Valley",             // Valley
                "TN_Terrain_SteepClimb",         // Steep Climb
                "TN_Terrain_Flat",               // Short Flat Recovery
                "TN_Terrain_Flat_Long",          // Falling Rock Zone
                "TN_Terrain_Downhill",           // Downhill
                "TN_Terrain_LargeHill",          // Large Hill
                "TN_Terrain_Flat_Long",          // Fog Zone
                "TN_Terrain_Valley",             // Valley
                "TN_Terrain_SteepClimb",         // Final Steep Climb
                "TN_Terrain_Flat_Long",          // Destination
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

                    // 3. Add Stage Triggers at specific intervals for Ooty
                    if (i == 1) CreateStageTrigger(piece.transform, "STAGE 1: HILL CLIMB", "Chadhai shuru! Momentum maintain karo warna gaadi slow ho jayegi.");
                    if (i == 6) CreateStageTrigger(piece.transform, "STAGE 2: FALLING ROCKS", "Careful! Upar se rocks aa rahe hain — dodge karo!");
                    if (i == 9) CreateStageTrigger(piece.transform, "STAGE 3: FOG ZONE", "Fog aa gaya! Visibility kam hai, speed control karo.");
                    if (i == 11) CreateStageTrigger(piece.transform, "FINAL STAGE: FINAL CLIMB", "Ooty bas upar hai! Last climb — full power!");
                }
                else
                {
                    Debug.LogWarning($"[BKS Builder] Missing prefab: {layout[i]}. Skipping.");
                }
            }

            // 4. Save and Add to Build Settings
            EditorSceneManager.SaveScene(newScene);
            AddSceneToBuildSettings(newScenePath);
            
            Debug.Log("[BKS Builder] Phase 10 Complete! BKS_TN_M04_Ooty created with severe mountain terrain and 4 specific Stage Triggers!");
        }
    }

    private static void EnsureNewPrefabsExist()
    {
        string folderPath = "Assets/BharatKaSafar/Prefabs/Terrain/TamilNadu/Core";
        
        // Generate the extreme mountain prefabs requested in Phase 10
        string[] newPrefabs = {
            "TN_Terrain_SteepClimb",
            "TN_Terrain_LargeHill",
            "TN_Terrain_Downhill"
        };

        foreach (string pName in newPrefabs)
        {
            string fullPath = $"{folderPath}/{pName}.prefab";
            if (File.Exists(fullPath)) continue;

            GameObject root = new GameObject(pName);
            root.layer = LayerMask.NameToLayer("Ground"); 
            root.AddComponent<EdgeCollider2D>();
            TN_TerrainModule module = root.AddComponent<TN_TerrainModule>();

            GameObject startPt = new GameObject("StartPoint");
            startPt.transform.SetParent(root.transform);
            startPt.transform.localPosition = new Vector3(-5f, 0, 0);

            GameObject endPt = new GameObject("EndPoint");
            endPt.transform.SetParent(root.transform);
            
            // Adjust end points to simulate steep height changes physically
            if (pName == "TN_Terrain_SteepClimb")
            {
                endPt.transform.localPosition = new Vector3(8f, 6f, 0); // Short distance, high climb
            }
            else if (pName == "TN_Terrain_Downhill")
            {
                endPt.transform.localPosition = new Vector3(8f, -6f, 0); // Short distance, steep drop
            }
            else if (pName == "TN_Terrain_LargeHill")
            {
                endPt.transform.localPosition = new Vector3(15f, 0f, 0); // Long base, we assume the physical collider will arc upwards heavily in between
            }
            else
            {
                endPt.transform.localPosition = new Vector3(5f, 0, 0);
            }

            module.startPoint = startPt.transform;
            module.endPoint = endPt.transform;

            PrefabUtility.SaveAsPrefabAsset(root, fullPath);
            GameObject.DestroyImmediate(root);
        }
    }

    private static void CreateStageTrigger(Transform parent, string title, string dialogue)
    {
        GameObject triggerObj = new GameObject($"StageTrigger_{title.Split(':')[0].Replace(" ", "")}");
        triggerObj.transform.SetParent(parent);
        triggerObj.transform.localPosition = new Vector3(0, 2f, 0);

        BoxCollider2D box = triggerObj.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(2f, 20f); // Taller box for Ooty because of extreme slopes

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
