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
    /// <summary>
    /// Automated Setup Utility for Bharat Ka Safar.
    /// 
    /// Adds a Unity Editor Menu item under:
    ///   Bharat Ka Safar -> Automated Setup (Create SOs, Scenes & Wire Everything)
    /// 
    /// Automatically generates:
    ///   - ScriptableObjects (MissionData & ChapterData)
    ///   - All 5 Unity Scenes with proper GameObjects, Canvases, HUD, and components attached
    ///   - Configures Editor Build Settings automatically
    /// </summary>
    public static class BKS_SetupUtility
    {
        private const string MISSION_SO_PATH = "Assets/BharatKaSafar/ScriptableObjects/missions/UP_M01_GaonKiRaah.asset";
        private const string CHAPTER_SO_PATH = "Assets/BharatKaSafar/ScriptableObjects/chapters/UP_UttarPradesh.asset";

        private const string INDIA_MAP_SCENE       = "Assets/BharatKaSafar/Scenes/BKS_IndiaMap.unity";
        private const string MISSION_SELECT_SCENE  = "Assets/BharatKaSafar/Scenes/UP/BKS_UP_MissionSelect.unity";
        private const string GAMEPLAY_M01_SCENE    = "Assets/BharatKaSafar/Scenes/UP/BKS_UP_M01_GaonKiRaah.unity";
        private const string MISSION_RESULT_SCENE  = "Assets/BharatKaSafar/Scenes/BKS_MissionResult.unity";
        private const string GARAGE_SCENE          = "Assets/BharatKaSafar/Scenes/BKS_Garage.unity";

        private const string SOURCE_RAJASTHAN_SCENE = "Assets/BharatKaSafar/Scenes/Rajasthan/BKS_Rajasthan_M01.unity";

        [MenuItem("Bharat Ka Safar/Automated Setup (Create SOs, Scenes & Wire Everything)", false, 0)]
        public static void PerformAutomatedSetup()
        {
            Debug.Log("[BKS Automated Setup] Starting automated setup for Bharat Ka Safar...");

            EnsureDirectories();

            // 1. Create ScriptableObjects
            MissionData missionData = CreateOrLoadMissionSO();
            ChapterData chapterData = CreateOrLoadChapterSO(missionData);

            // 2. Create/Setup Scenes
            SetupIndiaMapScene(chapterData);
            SetupMissionSelectScene(chapterData);
            SetupMissionResultScene();
            SetupGarageScene();
            SetupGameplayM01Scene(missionData); // Run gameplay last so it remains open in Editor!

            // 3. Register in Build Settings
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[BKS Automated Setup] ✅ COMPLETE! All ScriptableObjects, Scenes, and Build Settings have been generated and configured automatically.");
            EditorUtility.DisplayDialog("Bharat Ka Safar", "Automated setup completed successfully!\n\nAll 5 scenes, ScriptableObjects, and Build Settings have been created and wired up.", "Awesome!");
        }

        private static void EnsureDirectories()
        {
            string[] dirs = {
                "Assets/BharatKaSafar/ScriptableObjects/missions",
                "Assets/BharatKaSafar/ScriptableObjects/chapters",
                "Assets/BharatKaSafar/Scenes",
                "Assets/BharatKaSafar/Scenes/UP",
                "Assets/BharatKaSafar/Prefabs/Core"
            };

            foreach (var d in dirs)
            {
                if (!Directory.Exists(d))
                    Directory.CreateDirectory(d);
            }
            AssetDatabase.Refresh();
        }

        private static MissionData CreateOrLoadMissionSO()
        {
            MissionData asset = AssetDatabase.LoadAssetAtPath<MissionData>(MISSION_SO_PATH);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<MissionData>();
                AssetDatabase.CreateAsset(asset, MISSION_SO_PATH);
            }

            asset.InitForEditor(
                mID: "UP_M01",
                cID: "UttarPradesh",
                mNum: 1,
                mName: "Gaon Ki Raah",
                story: "A village waits for essential supplies. Drive safely through the rural roads of Uttar Pradesh.",
                sceneName: "BKS_UP_M01_GaonKiRaah",
                objText: "Reach the village safely.",
                coins: 500,
                maxStars: 10,
                twoStarRatio: 0.6f,
                threeStarRatio: 0.8f,
                threeHealthRatio: 0.5f,
                nextID: "UP_M02",
                power: 80
            );

            EditorUtility.SetDirty(asset);
            Debug.Log($"[BKS Automated Setup] Mission SO created/updated at: {MISSION_SO_PATH}");
            return asset;
        }

        private static ChapterData CreateOrLoadChapterSO(MissionData m01Data)
        {
            ChapterData asset = AssetDatabase.LoadAssetAtPath<ChapterData>(CHAPTER_SO_PATH);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ChapterData>();
                AssetDatabase.CreateAsset(asset, CHAPTER_SO_PATH);
            }

            var missionList = new List<MissionData> { m01Data };
            asset.InitForEditor(
                cID: "UttarPradesh",
                sName: "Uttar Pradesh",
                title: "Uttar Pradesh",
                sub: "Safar Ki Shuruaat",
                missionList: missionList,
                starsRequired: 0
            );

            EditorUtility.SetDirty(asset);
            Debug.Log($"[BKS Automated Setup] Chapter SO created/updated at: {CHAPTER_SO_PATH}");
            return asset;
        }

        private static void SetupIndiaMapScene(ChapterData chapterData)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Create Persistent Managers Bootstrapper
            GameObject pmGo = new GameObject("[BKS_PersistentManagers]");
            pmGo.AddComponent<BKS_PersistentManagers>();

            // Create Canvas
            GameObject canvasGo = CreateCanvasWithEventSystem("IndiaMapCanvas");
            BKS_IndiaMapUI mapUi = canvasGo.AddComponent<BKS_IndiaMapUI>();

            // Camera & Light
            CreateStandard2DCamera();

            EditorSceneManager.SaveScene(scene, INDIA_MAP_SCENE);
            Debug.Log($"[BKS Automated Setup] Created scene: {INDIA_MAP_SCENE}");
        }

        private static void SetupMissionSelectScene(ChapterData chapterData)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject canvasGo = CreateCanvasWithEventSystem("MissionSelectCanvas");
            BKS_UPMissionSelectUI selectUi = canvasGo.AddComponent<BKS_UPMissionSelectUI>();

            // Assign ChapterData via SerializedObject
            SerializedObject so = new SerializedObject(selectUi);
            SerializedProperty prop = so.FindProperty("chapterData");
            if (prop != null)
            {
                prop.objectReferenceValue = chapterData;
                so.ApplyModifiedProperties();
            }

            CreateStandard2DCamera();

            EditorSceneManager.SaveScene(scene, MISSION_SELECT_SCENE);
            Debug.Log($"[BKS Automated Setup] Created scene: {MISSION_SELECT_SCENE}");
        }

        private static void SetupGameplayM01Scene(MissionData missionData)
        {
            Scene scene;

            const string LEVEL1_SOURCE = "Assets/Scenes/Level1.unity";

            // Always force refresh gameplay scene from Level1.unity so changes immediately take effect
            if (File.Exists(LEVEL1_SOURCE))
            {
                if (File.Exists(GAMEPLAY_M01_SCENE))
                {
                    AssetDatabase.DeleteAsset(GAMEPLAY_M01_SCENE);
                }
                AssetDatabase.CopyAsset(LEVEL1_SOURCE, GAMEPLAY_M01_SCENE);
                AssetDatabase.Refresh();
            }

            scene = EditorSceneManager.OpenScene(GAMEPLAY_M01_SCENE, OpenSceneMode.Single);

            // Ensure main camera has a nice sky color
            CreateStandard2DCamera();

            // Create [BKS_Overlay]
            GameObject overlay = GameObject.Find("[BKS_Overlay]");
            if (overlay == null) overlay = new GameObject("[BKS_Overlay]");

            MissionManager mm = overlay.GetComponent<MissionManager>();
            if (mm == null) mm = overlay.AddComponent<MissionManager>();

            SerializedObject soMM = new SerializedObject(mm);
            SerializedProperty propSO = soMM.FindProperty("missionAsset");
            if (propSO != null)
            {
                propSO.objectReferenceValue = missionData;
                soMM.ApplyModifiedProperties();
            }

            if (overlay.GetComponent<Integration.BKS_GameplayBridge>() == null)
                overlay.AddComponent<Integration.BKS_GameplayBridge>();

            if (overlay.GetComponent<ObjectiveManager>() == null)
                overlay.AddComponent<ObjectiveManager>();

            if (overlay.GetComponent<BKS_MissionResult>() == null)
                overlay.AddComponent<BKS_MissionResult>();

            if (overlay.GetComponent<BKS_SafarStarTracker>() == null)
                overlay.AddComponent<BKS_SafarStarTracker>();

            // Find car and attach BKS vehicle scripts
            CarController car = Object.FindObjectOfType<CarController>();
            if (car != null)
            {
                if (car.GetComponent<BKS_VehicleHealth>() == null)
                    car.gameObject.AddComponent<BKS_VehicleHealth>();

                if (car.GetComponent<BKS_HornController>() == null)
                    car.gameObject.AddComponent<BKS_HornController>();

                if (car.GetComponent<BKS_UpgradeLoader>() == null)
                    car.gameObject.AddComponent<BKS_UpgradeLoader>();
            }

            // Ensure all existing canvases (with original throttle & brake pedals) remain active
            Canvas[] existingCanvases = Object.FindObjectsOfType<Canvas>();
            foreach (var c in existingCanvases)
            {
                c.gameObject.SetActive(true);
            }

            // Attach BKS Horn Button and Status Overlay to existing Canvas
            AttachBKSOverlayToExistingCanvas();

            // Populate sample level obstacles & collectibles along track if root doesn't exist
            SpawnSampleLevelElements();

            EditorSceneManager.SaveScene(scene, GAMEPLAY_M01_SCENE);
            Debug.Log($"[BKS Automated Setup] Configured gameplay scene: {GAMEPLAY_M01_SCENE}");
        }

        private static void AttachBKSOverlayToExistingCanvas()
        {
            // Find existing Canvas in scene (which has original throttle & brake pedals)
            Canvas targetCanvas = Object.FindObjectOfType<Canvas>();
            if (targetCanvas == null)
            {
                targetCanvas = CreateCanvasWithEventSystem("BKS_HUDCanvas").GetComponent<Canvas>();
            }

            // Ensure BKS_GameplayHUD is attached
            BKS_GameplayHUD hud = targetCanvas.GetComponent<BKS_GameplayHUD>();
            if (hud == null) hud = targetCanvas.gameObject.AddComponent<BKS_GameplayHUD>();

            // Root RectTransform
            RectTransform canvasRect = targetCanvas.GetComponent<RectTransform>();

            // 1. TOP PANEL (Health, Stars, Coins)
            GameObject topPanel = new GameObject("TopStatusPanel", typeof(RectTransform), typeof(Image));
            topPanel.transform.SetParent(targetCanvas.transform, false);
            RectTransform topRect = topPanel.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0f, 1f);
            topRect.anchorMax = new Vector2(1f, 1f);
            topRect.pivot = new Vector2(0.5f, 1f);
            topRect.sizeDelta = new Vector2(0f, 70f);
            topRect.anchoredPosition = new Vector2(0f, 0f);
            topPanel.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.18f, 0.85f);

            // Health Bar Slider
            GameObject healthGo = new GameObject("HealthSlider", typeof(RectTransform), typeof(Slider));
            healthGo.transform.SetParent(topPanel.transform, false);
            RectTransform healthRect = healthGo.GetComponent<RectTransform>();
            healthRect.anchorMin = new Vector2(0.02f, 0.5f);
            healthRect.anchorMax = new Vector2(0.35f, 0.5f);
            healthRect.sizeDelta = new Vector2(0f, 30f);
            Slider healthSlider = healthGo.GetComponent<Slider>();

            // Health Fill
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(healthGo.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            fill.GetComponent<Image>().color = new Color(0.2f, 0.85f, 0.3f, 1f);
            healthSlider.fillRect = fillRect;

            // Health Text
            GameObject hpTextGo = CreateTMPText("HPText", topPanel.transform, "100% HP", 18, Color.white);
            RectTransform hpTextRect = hpTextGo.GetComponent<RectTransform>();
            hpTextRect.anchorMin = new Vector2(0.03f, 0.5f);
            hpTextRect.anchorMax = new Vector2(0.34f, 0.5f);

            // Stars Text
            GameObject starsGo = CreateTMPText("SafarStarsText", topPanel.transform, "★ 0/10", 22, new Color(1f, 0.85f, 0.1f));
            RectTransform starsRect = starsGo.GetComponent<RectTransform>();
            starsRect.anchorMin = new Vector2(0.4f, 0.5f);
            starsRect.anchorMax = new Vector2(0.6f, 0.5f);

            // Coins Text
            GameObject coinsGo = CreateTMPText("CoinsText", topPanel.transform, "Coins: 0", 20, new Color(1f, 0.95f, 0.4f));
            RectTransform coinsRect = coinsGo.GetComponent<RectTransform>();
            coinsRect.anchorMin = new Vector2(0.65f, 0.5f);
            coinsRect.anchorMax = new Vector2(0.95f, 0.5f);

            // 2. BOTTOM CENTER — PROMINENT HORN BUTTON
            GameObject hornBtnGo = new GameObject("HornButton", typeof(RectTransform), typeof(Image), typeof(Button));
            hornBtnGo.transform.SetParent(targetCanvas.transform, false);
            RectTransform hornRect = hornBtnGo.GetComponent<RectTransform>();
            hornRect.anchorMin = new Vector2(0.5f, 0f);
            hornRect.anchorMax = new Vector2(0.5f, 0f);
            hornRect.pivot = new Vector2(0.5f, 0f);
            hornRect.sizeDelta = new Vector2(220f, 75f);
            hornRect.anchoredPosition = new Vector2(0f, 25f);

            Image hornImg = hornBtnGo.GetComponent<Image>();
            hornImg.color = new Color(1f, 0.45f, 0.05f, 0.95f); // Vibrant Indian Orange

            GameObject hornLabel = CreateTMPText("Label", hornBtnGo.transform, "🔊 BLOW HORN", 22, Color.white);
            RectTransform labelRect = hornLabel.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.sizeDelta = Vector2.zero;

            Button hornBtn = hornBtnGo.GetComponent<Button>();
            hornBtn.onClick.AddListener(() => hud.OnHornPressed());

            // Wire HUD serialized fields
            SerializedObject so = new SerializedObject(hud);
            SetSOProperty(so, "healthBar", healthSlider);
            SetSOProperty(so, "healthText", hpTextGo.GetComponent<TMP_Text>());
            SetSOProperty(so, "safarStarsText", starsGo.GetComponent<TMP_Text>());
            SetSOProperty(so, "coinsText", coinsGo.GetComponent<TMP_Text>());
            SetSOProperty(so, "hornButton", hornBtn);
            so.ApplyModifiedProperties();

            Debug.Log("[BKS Automated Setup] Attached BKS Horn Button & Status Overlay to existing Canvas with original pedals!");
        }

        private static void AddPointerEvents(GameObject go, System.Action onDown, System.Action onUp)
        {
            var trigger = go.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null) trigger = go.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var entryDown = new UnityEngine.EventSystems.EventTrigger.Entry();
            entryDown.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
            entryDown.callback.AddListener((data) => { onDown?.Invoke(); });
            trigger.triggers.Add(entryDown);

            var entryUp = new UnityEngine.EventSystems.EventTrigger.Entry();
            entryUp.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
            entryUp.callback.AddListener((data) => { onUp?.Invoke(); });
            trigger.triggers.Add(entryUp);
        }

        private static GameObject CreateTMPText(string name, Transform parent, string textStr, float fontSize, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = textStr;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            return go;
        }

        private static void SetSOProperty(SerializedObject so, string propName, Object value)
        {
            SerializedProperty prop = so.FindProperty(propName);
            if (prop != null) prop.objectReferenceValue = value;
        }

        private static void SpawnSampleLevelElements()
        {
            GameObject levelContainer = GameObject.Find("[BKS_LevelElements]");
            if (levelContainer != null) return; // already populated

            levelContainer = new GameObject("[BKS_LevelElements]");

            // Create simple 2D White Sprite for visual representations
            Texture2D tex = Texture2D.whiteTexture;
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);

            // 1. Horn-interactable obstacles (Cows on road)
            float[] cowPositionsX = { 35f, 90f, 150f, 220f, 310f };
            foreach (float posX in cowPositionsX)
            {
                GameObject cow = new GameObject($"BKS_CowObstacle_X{posX}");
                cow.transform.SetParent(levelContainer.transform);
                cow.transform.position = new Vector3(posX, 1.2f, 0f);
                cow.transform.localScale = new Vector3(1.8f, 1.4f, 1f);

                var sr = cow.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = new Color(0.95f, 0.95f, 0.85f, 1f); // Off-white cow tint

                var col = cow.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1f, 1f);
                col.isTrigger = true;

                cow.AddComponent<BKS_HornInteractable>();

                // Add label above cow
                CreateWorldLabel(cow.transform, "🐄 COW\n(HONK ME!)", new Color(1f, 0.4f, 0.1f));
            }

            // 2. Damage obstacles (Potholes)
            float[] damagePositionsX = { 60f, 120f, 180f, 260f };
            foreach (float posX in damagePositionsX)
            {
                GameObject pothole = new GameObject($"BKS_DamagePothole_X{posX}");
                pothole.transform.SetParent(levelContainer.transform);
                pothole.transform.position = new Vector3(posX, 0.2f, 0f);
                pothole.transform.localScale = new Vector3(2f, 0.4f, 1f);

                var sr = pothole.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = new Color(0.7f, 0.1f, 0.1f, 1f); // Dark red pothole

                var col = pothole.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1f, 1f);

                pothole.AddComponent<BKS_DamageObstacle>();

                CreateWorldLabel(pothole.transform, "⚠️ POTHOLE\n(-15 HP)", Color.red);
            }

            // 3. Safar Star collectibles (10 stars)
            float[] starPositionsX = { 20f, 50f, 80f, 110f, 140f, 170f, 200f, 240f, 280f, 320f };
            foreach (float posX in starPositionsX)
            {
                GameObject star = new GameObject($"BKS_SafarStar_X{posX}");
                star.transform.SetParent(levelContainer.transform);
                star.transform.position = new Vector3(posX, 2.2f, 0f);
                star.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

                var sr = star.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = new Color(1f, 0.85f, 0.1f, 1f); // Gold yellow star

                var col = star.AddComponent<CircleCollider2D>();
                col.radius = 0.5f;

                star.AddComponent<BKS_SafarStarCollectible>();

                CreateWorldLabel(star.transform, "⭐ STAR", new Color(1f, 0.9f, 0.2f));
            }

            // 4. Chai Dhaba pickup
            float[] dhabaPositionsX = { 100f, 230f };
            foreach (float posX in dhabaPositionsX)
            {
                GameObject dhaba = new GameObject($"BKS_ChaiDhabaStop_X{posX}");
                dhaba.transform.SetParent(levelContainer.transform);
                dhaba.transform.position = new Vector3(posX, 1.5f, 0f);
                dhaba.transform.localScale = new Vector3(2.2f, 2.2f, 1f);

                var sr = dhaba.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = new Color(0.15f, 0.7f, 0.3f, 1f); // Green Dhaba banner tint

                var col = dhaba.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1f, 1f);

                dhaba.AddComponent<BKS_ChaiDhabhaPickup>();

                CreateWorldLabel(dhaba.transform, "🍵 CHAI DHABA\n(+HP RESTORE)", Color.green);
            }

            Debug.Log("[BKS Automated Setup] Populated level obstacles with 2D visuals and labels!");
        }

        private static void CreateWorldLabel(Transform parent, string textStr, Color color)
        {
            GameObject labelGo = new GameObject("WorldLabel");
            labelGo.transform.SetParent(parent, false);
            labelGo.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            labelGo.transform.localScale = new Vector3(0.15f, 0.15f, 1f);

            TextMesh tm = labelGo.AddComponent<TextMesh>();
            tm.text = textStr;
            tm.characterSize = 0.1f;
            tm.fontSize = 120;
            tm.fontStyle = FontStyle.Bold;
            tm.color = color;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
        }

        private static void SetupMissionResultScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject canvasGo = CreateCanvasWithEventSystem("MissionResultCanvas");
            canvasGo.AddComponent<BKS_MissionResultScreenUI>();

            CreateStandard2DCamera();

            EditorSceneManager.SaveScene(scene, MISSION_RESULT_SCENE);
            Debug.Log($"[BKS Automated Setup] Created scene: {MISSION_RESULT_SCENE}");
        }

        private static void SetupGarageScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject canvasGo = CreateCanvasWithEventSystem("GarageCanvas");
            canvasGo.AddComponent<BKS_GarageUI>();

            CreateStandard2DCamera();

            EditorSceneManager.SaveScene(scene, GARAGE_SCENE);
            Debug.Log($"[BKS Automated Setup] Created scene: {GARAGE_SCENE}");
        }

        private static void UpdateBuildSettings()
        {
            string[] scenePaths = {
                INDIA_MAP_SCENE,
                MISSION_SELECT_SCENE,
                GAMEPLAY_M01_SCENE,
                MISSION_RESULT_SCENE,
                GARAGE_SCENE
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
            Debug.Log("[BKS Automated Setup] Build Settings updated with all 5 Bharat Ka Safar scenes.");
        }

        private static GameObject CreateCanvasWithEventSystem(string canvasName)
        {
            GameObject canvasGo = new GameObject(canvasName);
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

            return canvasGo;
        }

        private static void CreateStandard2DCamera()
        {
            if (Camera.main != null) return;

            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f, 1f);
        }
    }
}
#endif
