#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using BharatKaSafar.Core;
using BharatKaSafar.Missions;
using BharatKaSafar.Vehicle;
using BharatKaSafar.UI;
using BharatKaSafar.Obstacles;

namespace BharatKaSafar.EditorTools
{
    public static class BKS_RajasthanSetupUtility
    {
        private const string CHAPTER_SO_PATH = "Assets/BharatKaSafar/ScriptableObjects/chapters/RajasthanChapter.asset";
        
        // Missions
        private const string M01_SO_PATH = "Assets/BharatKaSafar/ScriptableObjects/missions/Rajasthan/Rajasthan_M01_Jaipur.asset";
        private const string M02_SO_PATH = "Assets/BharatKaSafar/ScriptableObjects/missions/Rajasthan/Rajasthan_M02_Jodhpur.asset";
        private const string M03_SO_PATH = "Assets/BharatKaSafar/ScriptableObjects/missions/Rajasthan/Rajasthan_M03_Jaisalmer.asset";
        private const string M04_SO_PATH = "Assets/BharatKaSafar/ScriptableObjects/missions/Rajasthan/Rajasthan_M04_Udaipur.asset";
        private const string M05_SO_PATH = "Assets/BharatKaSafar/ScriptableObjects/missions/Rajasthan/Rajasthan_M05_Pushkar.asset";

        // Scenes
        private const string MISSION_SELECT_SCENE = "Assets/BharatKaSafar/Scenes/Rajasthan/BKS_Rajasthan_MissionSelect.unity";
        private const string GAMEPLAY_JAIPUR_SCENE = "Assets/BharatKaSafar/Scenes/Rajasthan/BKS_Rajasthan_M01_Jaipur.unity";
        private const string GAMEPLAY_JODHPUR_SCENE = "Assets/BharatKaSafar/Scenes/Rajasthan/BKS_Rajasthan_M02_Jodhpur.unity";
        private const string GAMEPLAY_JAISALMER_SCENE = "Assets/BharatKaSafar/Scenes/Rajasthan/BKS_Rajasthan_M03_Jaisalmer.unity";
        private const string GAMEPLAY_UDAIPUR_SCENE = "Assets/BharatKaSafar/Scenes/Rajasthan/BKS_Rajasthan_M04_Udaipur.unity";
        private const string GAMEPLAY_PUSHKAR_SCENE = "Assets/BharatKaSafar/Scenes/Rajasthan/BKS_Rajasthan_M05_Pushkar.unity";

        [MenuItem("Bharat Ka Safar/Setup Rajasthan Chapter", false, 1)]
        public static void SetupRajasthanChapter()
        {
            Debug.Log("[BKS Rajasthan Setup] Starting automated setup for Rajasthan Chapter...");
            EnsureDirectories();

            // 1. Create Mission SOs
            var m01 = CreateMissionSO(M01_SO_PATH, "Raj_M01", 1, "Jaipur: Pink City Welcome", "First stop: the Pink City. Take it slow through the bazaars and get a feel for Jaipur before the palace tour begins.", "BKS_Rajasthan_M01_Jaipur", "Reach the palace gate within the generous time limit.", "Raj_M02", 50);
            var m02 = CreateMissionSO(M02_SO_PATH, "Raj_M02", 2, "Jodhpur: Blue City Climb", "Mehrangarh Fort sits high above the Blue City, and the gates close to visitors at sundown. Climb through the narrow blue lanes before they shut.", "BKS_Rajasthan_M02_Jodhpur", "Reach the fort gate before sundown.", "Raj_M03", 60);
            var m03 = CreateMissionSO(M03_SO_PATH, "Raj_M03", 3, "Jaisalmer: Dune Crossing", "A sandstorm is building on the horizon. Guide the safari group back across the dunes to the fort before it hits — the sand will slow you more than any obstacle.", "BKS_Rajasthan_M03_Jaisalmer", "Cross the desert and reach the fort before the sandstorm arrives.", "Raj_M04", 70);
            var m04 = CreateMissionSO(M04_SO_PATH, "Raj_M04", 4, "Udaipur: Lake Palace Regatta", "Monsoon clouds are rolling in over Lake Pichola. Supplies need to reach the boat before the storm breaks — the lakeside roads are narrow, and one wrong turn means the water.", "BKS_Rajasthan_M04_Udaipur", "Deliver supplies around the lake before the monsoon hits.", "Raj_M05", 85);
            var m05 = CreateMissionSO(M05_SO_PATH, "Raj_M05", 5, "Pushkar: Camel Fair Chaos", "The Camel Fair is at its peak — thousands of people, animals, and stalls packed into the streets. A key ritual begins at the temple soon, and this is the only way through.", "BKS_Rajasthan_M05_Pushkar", "Reach the temple before the ritual begins.", "", 100);

            // 2. Create Chapter SO
            ChapterData chapterData = CreateChapterSO(new List<MissionData>{ m01, m02, m03, m04, m05 });

            // 3. Create Scenes
            SetupMissionSelectScene(chapterData);
            SetupGameplayScene(GAMEPLAY_JAIPUR_SCENE, m01);
            SetupGameplayScene(GAMEPLAY_JODHPUR_SCENE, m02);
            SetupGameplayScene(GAMEPLAY_JAISALMER_SCENE, m03);
            SetupGameplayScene(GAMEPLAY_UDAIPUR_SCENE, m04);
            SetupGameplayScene(GAMEPLAY_PUSHKAR_SCENE, m05);

            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BKS Rajasthan Setup] ✅ COMPLETE!");
        }

        private static void EnsureDirectories()
        {
            string[] dirs = {
                "Assets/BharatKaSafar/ScriptableObjects/missions/Rajasthan",
                "Assets/BharatKaSafar/Scenes/Rajasthan",
            };
            foreach (var d in dirs)
            {
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
            }
            AssetDatabase.Refresh();
        }

        private static MissionData CreateMissionSO(string path, string id, int num, string name, string story, string sceneName, string objective, string nextId, int power)
        {
            MissionData asset = AssetDatabase.LoadAssetAtPath<MissionData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<MissionData>();
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.InitForEditor(id, "Rajasthan", num, name, story, sceneName, objective, 500, 10, 0.6f, 0.8f, 0.5f, nextId, power);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static ChapterData CreateChapterSO(List<MissionData> missions)
        {
            ChapterData asset = AssetDatabase.LoadAssetAtPath<ChapterData>(CHAPTER_SO_PATH);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ChapterData>();
                AssetDatabase.CreateAsset(asset, CHAPTER_SO_PATH);
            }
            asset.InitForEditor("Rajasthan", "Rajasthan", "Desert Kingdoms", "The Royal Desert State", missions, null, 0);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void SetupMissionSelectScene(ChapterData chapterData)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject canvasGo = new GameObject("MissionSelectCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
            
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            BKS_RajasthanMissionSelectUI selectUi = canvasGo.AddComponent<BKS_RajasthanMissionSelectUI>();
            
            SerializedObject so = new SerializedObject(selectUi);
            SerializedProperty prop = so.FindProperty("chapterData");
            if (prop != null)
            {
                prop.objectReferenceValue = chapterData;
                so.ApplyModifiedProperties();
            }

            if (Camera.main == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                Camera cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 5f;
                cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f, 1f);
            }

            EditorSceneManager.SaveScene(scene, MISSION_SELECT_SCENE);
        }

        private static void SetupGameplayScene(string scenePath, MissionData missionData)
        {
            // Just duplicate Level1 or UP_M01 as a base for gameplay scene
            const string BASE_SCENE = "Assets/BharatKaSafar/Scenes/UP/BKS_UP_M01_GaonKiRaah.unity";
            if (File.Exists(BASE_SCENE))
            {
                if (File.Exists(scenePath)) AssetDatabase.DeleteAsset(scenePath);
                AssetDatabase.CopyAsset(BASE_SCENE, scenePath);
                AssetDatabase.Refresh();
            }
            else return;

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            
            GameObject overlay = GameObject.Find("[BKS_Overlay]");
            if (overlay != null)
            {
                MissionManager mm = overlay.GetComponent<MissionManager>();
                if (mm != null)
                {
                    SerializedObject soMM = new SerializedObject(mm);
                    SerializedProperty propSO = soMM.FindProperty("missionAsset");
                    if (propSO != null)
                    {
                        propSO.objectReferenceValue = missionData;
                        soMM.ApplyModifiedProperties();
                    }
                }
            }

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void UpdateBuildSettings()
        {
            string[] scenePaths = {
                MISSION_SELECT_SCENE,
                GAMEPLAY_JAIPUR_SCENE,
                GAMEPLAY_JODHPUR_SCENE,
                GAMEPLAY_JAISALMER_SCENE,
                GAMEPLAY_UDAIPUR_SCENE,
                GAMEPLAY_PUSHKAR_SCENE
            };

            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            foreach (string path in scenePaths)
            {
                bool exists = false;
                foreach (var s in buildScenes)
                {
                    if (s.path == path)
                    {
                        exists = true;
                        s.enabled = true;
                        break;
                    }
                }

                if (!exists)
                {
                    buildScenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
        }
    }
}
#endif
