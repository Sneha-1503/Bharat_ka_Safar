using UnityEngine;
using UnityEditor;
using BharatKaSafar.Core;
using BharatKaSafar.Missions;
using System.IO;

public class TN_Bootstrapper
{
    [MenuItem("Bharat Ka Safar/Bootstrap Tamil Nadu Chapter")]
    public static void BootstrapTamilNadu()
    {
        CreateFolders();
        CreateScriptableObjects();
        AssetDatabase.Refresh();
        Debug.Log("[TN Bootstrapper] Successfully generated Tamil Nadu folders and ScriptableObjects!");
    }

    private static void CreateFolders()
    {
        string[] foldersToCreate = new string[]
        {
            "Assets/BharatKaSafar/Prefabs/Terrain/TamilNadu/Core",
            "Assets/BharatKaSafar/Prefabs/Terrain/TamilNadu/Madurai",
            "Assets/BharatKaSafar/Prefabs/Terrain/TamilNadu/Chennai",
            "Assets/BharatKaSafar/Prefabs/Terrain/TamilNadu/Mahabalipuram",
            "Assets/BharatKaSafar/Prefabs/Terrain/TamilNadu/Ooty",
            "Assets/BharatKaSafar/Prefabs/Terrain/TamilNadu/Kanyakumari",
            "Assets/BharatKaSafar/Art/Chapters/TamilNadu/Backgrounds",
            "Assets/BharatKaSafar/Art/Chapters/TamilNadu/Terrain",
            "Assets/BharatKaSafar/Art/Chapters/TamilNadu/Landmarks",
            "Assets/BharatKaSafar/Art/Chapters/TamilNadu/Buildings",
            "Assets/BharatKaSafar/Art/Chapters/TamilNadu/Nature",
            "Assets/BharatKaSafar/Art/Chapters/TamilNadu/Obstacles",
            "Assets/BharatKaSafar/Art/Chapters/TamilNadu/Vehicles",
            "Assets/BharatKaSafar/Art/Chapters/TamilNadu/Collectibles",
            "Assets/BharatKaSafar/Art/Chapters/TamilNadu/VFX",
            "Assets/BharatKaSafar/Art/Chapters/TamilNadu/MissionIcons",
            "Assets/BharatKaSafar/Art/Chapters/TamilNadu/UI",
            "Assets/BharatKaSafar/ScriptableObjects/Chapters",
            "Assets/BharatKaSafar/ScriptableObjects/Missions/TamilNadu"
        };

        foreach (var folder in foldersToCreate)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }
    }

    private static void CreateScriptableObjects()
    {
        // 1. Create Mission Data
        MissionData m1 = CreateMission("TN_M01", "TN", 1, "Madurai Temple Trail", "Meenakshi Temple tak pahuchna hai. Sheher ki busy galiyon se safely nikal kar temple entrance tak jao!", "BKS_TN_M01_Madurai", "Reach Meenakshi Temple", 500, 10, "TN_M02");
        MissionData m2 = CreateMission("TN_M02", "TN", 2, "Chennai Coastal Dash", "Marina Beach ke coastal route par drive karo aur lighthouse checkpoint tak pahucho!", "BKS_TN_M02_Chennai", "Reach Chennai Lighthouse", 600, 10, "TN_M03");
        MissionData m3 = CreateMission("TN_M03", "TN", 3, "Mahabalipuram Heritage Run", "Ancient monuments ke heritage route ko cross karke Shore Temple tak pahucho.", "BKS_TN_M03_Mahabalipuram", "Reach Shore Temple", 700, 10, "TN_M04");
        MissionData m4 = CreateMission("TN_M04", "TN", 4, "Ooty Mountain Challenge", "Ab plains khatam! Ooty ke Nilgiri hills shuru ho rahe hain.", "BKS_TN_M04_Ooty", "Climb to Ooty", 800, 10, "TN_M05");
        MissionData m5 = CreateMission("TN_M05", "TN", 5, "Kanyakumari Final Journey", "Tamil Nadu ka final safar! Kanyakumari ke southern tip tak pahuchna hai.", "BKS_TN_M05_Kanyakumari", "Reach Kanyakumari Tip", 1000, 15, "");

        // 2. Create Chapter Data
        string chapterPath = "Assets/BharatKaSafar/ScriptableObjects/Chapters/TN_ChapterData.asset";
        ChapterData chapter = AssetDatabase.LoadAssetAtPath<ChapterData>(chapterPath);
        if (chapter == null)
        {
            chapter = ScriptableObject.CreateInstance<ChapterData>();
            AssetDatabase.CreateAsset(chapter, chapterPath);
        }

        chapter.InitForEditor("TN", "Tamil Nadu", "Tamil Nadu", "Temples, Trails & Tamil Tales", new System.Collections.Generic.List<MissionData> { m1, m2, m3, m4, m5 });
        EditorUtility.SetDirty(chapter);
        AssetDatabase.SaveAssets();
    }

    private static MissionData CreateMission(string id, string chapterId, int num, string name, string story, string scene, string objText, int coins, int maxStars, string nextId)
    {
        string path = $"Assets/BharatKaSafar/ScriptableObjects/Missions/TamilNadu/{id}.asset";
        MissionData mission = AssetDatabase.LoadAssetAtPath<MissionData>(path);
        
        if (mission == null)
        {
            mission = ScriptableObject.CreateInstance<MissionData>();
            AssetDatabase.CreateAsset(mission, path);
        }

        mission.InitForEditor(id, chapterId, num, name, story, scene, objText, coins, maxStars, 0.6f, 0.8f, 0.5f, nextId, 80 + (num * 10));
        EditorUtility.SetDirty(mission);
        return mission;
    }
}
