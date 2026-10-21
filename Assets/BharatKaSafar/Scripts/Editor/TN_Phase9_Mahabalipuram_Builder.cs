using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BharatKaSafar.Gameplay;
using System.IO;

public class TN_Phase9_Mahabalipuram_Builder
{
    [MenuItem("Bharat Ka Safar/Automate Phase 9 (Build Mahabalipuram Level)")]
    public static void BuildMahabalipuramLevel()
    {
        EnsureNewPrefabsExist();

        string originalScenePath = "Assets/BharatKaSafar/Scenes/BKS_TN_M02_Chennai.unity";
        
        if (!File.Exists(originalScenePath))
        {
            Debug.LogError("[BKS Builder] Could not find Chennai scene. Did you run Phase 8 first?");
            return;
        }

        string newScenePath = "Assets/BharatKaSafar/Scenes/BKS_TN_M03_Mahabalipuram.unity";
        
        if (AssetDatabase.CopyAsset(originalScenePath, newScenePath))
        {
            Scene newScene = EditorSceneManager.OpenScene(newScenePath, OpenSceneMode.Single);

            // 1. Delete Chennai's Terrain
            GameObject oldTerrain = GameObject.Find("TN_Chennai_Terrain");
            if (oldTerrain != null)
            {
                GameObject.DestroyImmediate(oldTerrain);
            }

            // 2. Create New Mahabalipuram Terrain Root
            GameObject terrainRoot = new GameObject("TN_Mahabalipuram_Terrain");
            
            // Define the layout requested for Mahabalipuram:
            // START -> Flat Sandy Road -> Small Valley -> Rocky Climb -> FIRST JUMP -> Landing -> Stone Bridge -> Rocky Hill -> Finish
            string[] layout = {
                "TN_Terrain_Flat",               // Start
                "TN_Terrain_Flat",               // Flat Sandy Road
                "TN_Terrain_Valley",             // Small Valley
                "TN_Terrain_RockyClimb",         // Rocky Climb / Uneven
                "TN_Terrain_Jump",               // FIRST JUMP
                "TN_Terrain_Landing",            // Landing Section
                "TN_Terrain_Bridge",             // Stone Bridge
                "TN_Terrain_SmallHill",          // Rocky Hill
                "TN_Terrain_Flat_Long",          // Temple Approach
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

                    // 3. Add Stage Triggers at specific intervals for Mahabalipuram
                    if (i == 1) CreateStageTrigger(piece.transform, "STAGE 1: STONE ROUTE", "Purana heritage route hai — road thoda rough hone wala hai!");
                    if (i == 4) CreateStageTrigger(piece.transform, "STAGE 2: BROKEN PATH", "Aage road toot gaya hai! Speed aur jump ko perfectly time karo!");
                    if (i == 8) CreateStageTrigger(piece.transform, "STAGE 3: SHORE TEMPLE", "Temple dikh gaya! Ab safely final checkpoint cross karo!");
                }
                else
                {
                    Debug.LogWarning($"[BKS Builder] Missing prefab: {layout[i]}. Skipping.");
                }
            }

            // 4. Save and Add to Build Settings
            EditorSceneManager.SaveScene(newScene);
            AddSceneToBuildSettings(newScenePath);
            
            Debug.Log("[BKS Builder] Phase 9 Complete! BKS_TN_M03_Mahabalipuram created with rough heritage terrain and Jumps!");
        }
    }

    private static void EnsureNewPrefabsExist()
    {
        string folderPath = "Assets/BharatKaSafar/Prefabs/Terrain/TamilNadu/Core";
        
        // Generate the new rough prefabs requested in Phase 9
        string[] newPrefabs = {
            "TN_Terrain_Valley",
            "TN_Terrain_RockyClimb",
            "TN_Terrain_Jump",
            "TN_Terrain_Landing"
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
            
            // For a Jump, we make the EndPoint have a physical gap in X, meaning the next piece will start further away
            if (pName == "TN_Terrain_Jump")
            {
                endPt.transform.localPosition = new Vector3(15f, -2f, 0); // 10 units of gap, slightly lower landing
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
