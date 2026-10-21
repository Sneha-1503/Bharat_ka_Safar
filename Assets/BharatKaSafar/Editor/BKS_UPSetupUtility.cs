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
using BharatKaSafar.Rewards;
using BharatKaSafar.Gameplay;
using BharatKaSafar.Vehicle;
using BharatKaSafar.UI;

namespace BharatKaSafar.EditorTools
{
    /// <summary>
    /// Master Setup Utility for Uttar Pradesh (UP) Vertical Slice.
    /// Builds:
    ///   - All UP ScriptableObjects (Missions, Monuments, Chapter)
    ///   - BKS_UP_MonumentSelect scene with rich UI & routing
    ///   - BKS_UP_MissionSelect scene with cards, lock states & briefing popup
    ///   - 3 Gameplay scenes (Agra, Varanasi, Sarnath) with Checkpoints, Multipliers, Obstacles, and Destination
    ///   - Configures Build Settings
    /// </summary>
    public static class BKS_UPSetupUtility
    {
        private const string SCENE_DIR = "Assets/BharatKaSafar/Scenes/UP";
        private const string DATA_DIR  = "Assets/BharatKaSafar/ScriptableObjects";
        private const string LEVEL1_SOURCE = "Assets/Scenes/Level1.unity";

        public const string SCENE_MONUMENT_SEL   = "Assets/BharatKaSafar/Scenes/UP/BKS_UP_MonumentSelect.unity";
        public const string SCENE_MISSION_SEL    = "Assets/BharatKaSafar/Scenes/UP/BKS_UP_MissionSelect.unity";
        public const string SCENE_M01_AGRA       = "Assets/BharatKaSafar/Scenes/UP/BKS_UP_M01_Agra.unity";
        public const string SCENE_M02_VARANASI   = "Assets/BharatKaSafar/Scenes/UP/BKS_UP_M02_Varanasi.unity";
        public const string SCENE_M03_SARNATH    = "Assets/BharatKaSafar/Scenes/UP/BKS_UP_M03_Sarnath.unity";
        public const string SCENE_M04_PRAYAGRAJ  = "Assets/BharatKaSafar/Scenes/UP/BKS_UP_M04_Prayagraj.unity";
        public const string SCENE_M05_BHULBHULAIYA = "Assets/BharatKaSafar/Scenes/UP/BKS_UP_M05_Bhulbhulaiya.unity";

        // Palette
        private static readonly Color COL_UP_BG        = new Color(0.92f, 0.48f, 0.16f, 1f); // UP Marigold / Saffron
        private static readonly Color COL_HEADER_BG    = new Color(0.48f, 0.12f, 0.08f, 0.95f); // Deep terracotta
        private static readonly Color COL_CARD_BG      = new Color(0.98f, 0.94f, 0.88f, 1f); // Warm Sandstone
        private static readonly Color COL_CARD_LOCKED  = new Color(0.6f, 0.55f, 0.5f, 1f);
        private static readonly Color COL_BTN_PRIMARY  = new Color(0.12f, 0.52f, 0.22f, 1f); // Forest green
        private static readonly Color COL_BTN_ACCENT   = new Color(0.85f, 0.35f, 0.1f, 1f);  // Ochre
        private static readonly Color COL_BTN_CLOSE    = new Color(0.72f, 0.15f, 0.15f, 1f); // Crimson
        private static readonly Color COL_POPUP_BG     = new Color(0.15f, 0.1f, 0.08f, 0.96f);

        [MenuItem("Bharat Ka Safar/Build UP Chapter (Complete Vertical Slice)", false, 1)]
        public static void BuildCompleteUPChapter()
        {
            Debug.Log("[BKS_UPSetupUtility] Starting complete Uttar Pradesh Chapter generation...");

            EnsureDirectories();

            // 1. ScriptableObjects
            var (chapter, monuments, missions) = GenerateScriptableObjects();

            // 2. UI Scenes
            BuildMonumentSelectScene(chapter, monuments);
            BuildMissionSelectScene(chapter, monuments, missions);

            // 3. Gameplay Scenes
            BuildGameplayScene(SCENE_M01_AGRA,          missions[0], new Color(0.95f, 0.72f, 0.5f),  "Agra - Taj Heritage Track");
            BuildGameplayScene(SCENE_M02_VARANASI,       missions[1], new Color(0.88f, 0.45f, 0.35f), "Varanasi - Ganges Ghats Highway");
            BuildGameplayScene(SCENE_M03_SARNATH,        missions[2], new Color(0.35f, 0.6f, 0.75f),  "Sarnath - Ancient Pilgrimage Trail");
            BuildGameplayScene(SCENE_M04_PRAYAGRAJ,      missions[3], new Color(0.28f, 0.52f, 0.88f),  "Prayagraj - Sangam Confluence Road");
            BuildGameplayScene(SCENE_M05_BHULBHULAIYA,   missions[4], new Color(0.45f, 0.28f, 0.72f),  "Bhulbhulaiya - Labyrinth of Lucknow");

            // 4. Update Build Settings
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[BKS_UPSetupUtility] Complete UP Chapter (5 missions) created and wired successfully!");
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Bharat Ka Safar", "Uttar Pradesh Chapter (5 Missions) generated!\n\n- Monument Select Scene Created\n- Mission Select Scene Created\n- 5 Playable Gameplay Scenes Configured\n  (Agra, Varanasi, Sarnath, Prayagraj, Bhulbhulaiya)\n- Build Settings Updated", "Let's Go!");
            }
        }

        private static void EnsureDirectories()
        {
            string[] dirs = {
                SCENE_DIR,
                $"{DATA_DIR}/chapters",
                $"{DATA_DIR}/monuments",
                $"{DATA_DIR}/missions"
            };

            foreach (var d in dirs)
            {
                if (!Directory.Exists(d))
                    Directory.CreateDirectory(d);
            }
            AssetDatabase.Refresh();
        }

        private static (ChapterData, List<MonumentData>, List<MissionData>) GenerateScriptableObjects()
        {
            // Missions
            var m1 = CreateMission("UP_M01", "UttarPradesh", 1, "Agra Heritage Run",
                "Navigate the royal avenues surrounding the majestic Taj Mahal.",
                "Reach the destination safely while keeping your vehicle intact.",
                "BKS_UP_M01_Agra", "UP_M02");

            var m2 = CreateMission("UP_M02", "UttarPradesh", 2, "Varanasi Riverside Route",
                "A bustling road alongside the holy ghats of Varanasi.",
                "Avoid riverside obstacles, grab bonus multipliers, and reach the ghat.",
                "BKS_UP_M02_Varanasi", "UP_M03");

            var m3 = CreateMission("UP_M03", "UttarPradesh", 3, "Sarnath Pilgrimage Drive",
                "Traverse the peaceful archaeological grounds of ancient Sarnath.",
                "Complete the spiritual circuit with maximum score and stars.",
                "BKS_UP_M03_Sarnath", "UP_M04");

            var m4 = CreateMission("UP_M04", "UttarPradesh", 4, "Prayagraj Sangam Sprint",
                "Race along the sacred Triveni Sangam where three holy rivers meet at Prayagraj.",
                "Navigate the confluence road, collect Safar Stars, and reach the Sangam ghat.",
                "BKS_UP_M04_Prayagraj", "UP_M05");

            var m5 = CreateMission("UP_M05", "UttarPradesh", 5, "Bhulbhulaiya Maze Run",
                "Drive through the legendary labyrinthine passages of Lucknow's Bhulbhulaiya.",
                "Navigate the winding maze, avoid dead-end hazards, and find the exit gate.",
                "BKS_UP_M05_Bhulbhulaiya", "");

            var missions = new List<MissionData> { m1, m2, m3, m4, m5 };

            // Monuments
            var mon1 = CreateMonument("UP_MON_01", "Taj Mahal",      "The breathtaking ivory-white marble mausoleum on the Yamuna river.",                      new List<MissionData> { m1 });
            var mon2 = CreateMonument("UP_MON_02", "Varanasi Ghats", "The historic steps leading down to the sacred Ganges, rich with timeless rituals.",      new List<MissionData> { m2 });
            var mon3 = CreateMonument("UP_MON_03", "Sarnath Ruins",  "The revered deer park where Gautama Buddha taught his first sermon.",                       new List<MissionData> { m3 });
            var mon4 = CreateMonument("UP_MON_04", "Triveni Sangam", "The holy confluence of Ganga, Yamuna, and the mythical Saraswati at Prayagraj.",           new List<MissionData> { m4 });
            var mon5 = CreateMonument("UP_MON_05", "Bhulbhulaiya",   "The mesmerizing labyrinth inside the Bada Imambara of Lucknow — a marvel of architecture.", new List<MissionData> { m5 });

            var monuments = new List<MonumentData> { mon1, mon2, mon3, mon4, mon5 };

            // Chapter
            var chapter = CreateChapter("UttarPradesh", "Uttar Pradesh", "Heritage & Heartland", "The cultural and spiritual epicenter of India.", missions, monuments);

            return (chapter, monuments, missions);
        }

        private static MissionData CreateMission(string mID, string cID, int mNum, string name, string story, string objText, string scene, string nextID)
        {
            string path = $"{DATA_DIR}/missions/{mID}_{name.Replace(" ", "")}.asset";
            var m = AssetDatabase.LoadAssetAtPath<MissionData>(path);
            if (m == null)
            {
                m = ScriptableObject.CreateInstance<MissionData>();
                AssetDatabase.CreateAsset(m, path);
            }
            m.InitForEditor(mID, cID, mNum, name, story, scene, objText, 500, 10, 0.6f, 0.8f, 0.5f, nextID, 50);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static MonumentData CreateMonument(string id, string name, string desc, List<MissionData> missions)
        {
            string path = $"{DATA_DIR}/monuments/{id}_{name.Replace(" ", "")}.asset";
            var m = AssetDatabase.LoadAssetAtPath<MonumentData>(path);
            if (m == null)
            {
                m = ScriptableObject.CreateInstance<MonumentData>();
                AssetDatabase.CreateAsset(m, path);
            }
            m.InitForEditor(id, name, desc, missions);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static ChapterData CreateChapter(string cID, string sName, string title, string sub, List<MissionData> missions, List<MonumentData> monuments)
        {
            string path = $"{DATA_DIR}/chapters/{cID}_{sName.Replace(" ", "")}.asset";
            var c = AssetDatabase.LoadAssetAtPath<ChapterData>(path);
            if (c == null)
            {
                c = ScriptableObject.CreateInstance<ChapterData>();
                AssetDatabase.CreateAsset(c, path);
            }
            c.InitForEditor(cID, sName, title, sub, missions, monuments, 0);
            EditorUtility.SetDirty(c);
            return c;
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // 1. MONUMENT SELECT SCENE
        // ══════════════════════════════════════════════════════════════════════════════

        private static void BuildMonumentSelectScene(ChapterData chapter, List<MonumentData> monuments)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateStandard2DCamera();

            var canvasGo = CreateCanvasWithEventSystem("UPMonumentSelectCanvas");
            var canvas = canvasGo.GetComponent<Canvas>();

            // Fullscreen BG
            var bg = CreateImagePanel("Background", canvasGo.transform, COL_UP_BG);
            StretchFull(bg);

            // Header
            var header = CreateImagePanel("Header", canvasGo.transform, COL_HEADER_BG);
            SetAnchors(header, new Vector2(0f, 0.86f), new Vector2(1f, 1f));

            var titleTMP = CreateTMP("StateTitle", header.transform, "UTTAR PRADESH", 38, Color.white, FontStyles.Bold);
            SetAnchors(titleTMP.gameObject, new Vector2(0.04f, 0.45f), new Vector2(0.6f, 0.95f));

            var subTMP = CreateTMP("Subtitle", header.transform, "HERITAGE & HEARTLAND — SELECT MONUMENT", 18, new Color(1f, 0.85f, 0.5f));
            SetAnchors(subTMP.gameObject, new Vector2(0.04f, 0.05f), new Vector2(0.6f, 0.48f));

            var statsTMP = CreateTMP("Stats", header.transform, "★ 0   🪙 0", 22, Color.yellow, FontStyles.Bold);
            SetAnchors(statsTMP.gameObject, new Vector2(0.62f, 0.2f), new Vector2(0.82f, 0.8f));

            var backBtn = CreateButton("BackButton", header.transform, "← INDIA MAP", 18, COL_BTN_ACCENT, Color.white);
            SetAnchors(backBtn.gameObject, new Vector2(0.85f, 0.2f), new Vector2(0.97f, 0.8f));

            // Content Panel — 5 Monument Cards in 2 rows (3 top, 2 bottom-centered)
            var cardsContainer = new GameObject("MonumentCardsContainer", typeof(RectTransform));
            cardsContainer.transform.SetParent(canvasGo.transform, false);
            SetAnchors(cardsContainer, new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.82f));

            var ui = canvasGo.AddComponent<BKS_UPMonumentSelectUI>();
            var cardsList = new List<BKS_UPMonumentSelectUI.MonumentCard>();

            // Row layout: row 0 = top 3 cards, row 1 = bottom 2 cards centered
            float cardW = 0.305f;
            float cardSp = 0.025f;
            // positions for each card [xMin, yMin, xMax, yMax]
            float[,] cardPos = {
                // Row 0 (top half: y 0.52 -> 0.98)
                { 0.01f,  0.52f,  0.315f, 0.98f },
                { 0.345f, 0.52f,  0.655f, 0.98f },
                { 0.685f, 0.52f,  0.99f,  0.98f },
                // Row 1 (bottom half: y 0.02 -> 0.48) — 2 cards centered
                { 0.175f, 0.02f,  0.48f,  0.48f },
                { 0.52f,  0.02f,  0.825f, 0.48f },
            };

            for (int i = 0; i < 5; i++)
            {
                var cardGo = CreateImagePanel($"MonumentCard_{i + 1}", cardsContainer.transform, COL_CARD_BG);
                SetAnchors(cardGo, new Vector2(cardPos[i, 0], cardPos[i, 1]), new Vector2(cardPos[i, 2], cardPos[i, 3]));

                // Banner
                var banner = CreateImagePanel("MonumentBanner", cardGo.transform, new Color(0.85f, 0.75f, 0.65f));
                SetAnchors(banner, new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.95f));

                string defaultMonName = i < monuments.Count ? monuments[i].monumentName : $"Monument {i + 1}";
                var bannerLabel = CreateTMP("BannerLabel", banner.transform, defaultMonName, 18, new Color(0.3f, 0.2f, 0.1f), FontStyles.Bold);
                StretchFull(bannerLabel.gameObject);

                var nameTMP = CreateTMP("NameText", cardGo.transform, defaultMonName, 20, new Color(0.2f, 0.1f, 0.05f), FontStyles.Bold);
                SetAnchors(nameTMP.gameObject, new Vector2(0.05f, 0.44f), new Vector2(0.95f, 0.53f));

                string desc = i < monuments.Count ? monuments[i].shortDescription : "Explore the historical grounds.";
                var descTMP = CreateTMP("DescText", cardGo.transform, desc, 13, new Color(0.25f, 0.2f, 0.18f));
                SetAnchors(descTMP.gameObject, new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.42f));

                var selBtn = CreateButton("SelectBtn", cardGo.transform, "EXPLORE →", 16, COL_BTN_PRIMARY, Color.white);
                SetAnchors(selBtn.gameObject, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.18f));

                var mCard = new BKS_UPMonumentSelectUI.MonumentCard
                {
                    root = cardGo,
                    nameText = nameTMP,
                    descText = descTMP,
                    monumentImage = banner.GetComponent<Image>(),
                    selectButton = selBtn
                };
                cardsList.Add(mCard);
            }

            // Wire UI SerializedObject
            SerializedObject so = new SerializedObject(ui);
            SetSOProperty(so, "chapterData", chapter);
            SetSOProperty(so, "stateTitleText", titleTMP);
            SetSOProperty(so, "subtitleText", subTMP);
            SetSOProperty(so, "totalCoinsText", statsTMP);
            SetSOProperty(so, "totalStarsText", statsTMP);
            SetSOProperty(so, "backButton", backBtn);

            // Populate cards serialized list
            var cardsProp = so.FindProperty("monumentCards");
            if (cardsProp != null)
            {
                cardsProp.ClearArray();
                for (int i = 0; i < cardsList.Count; i++)
                {
                    cardsProp.InsertArrayElementAtIndex(i);
                    var el = cardsProp.GetArrayElementAtIndex(i);
                    el.FindPropertyRelative("root").objectReferenceValue = cardsList[i].root;
                    el.FindPropertyRelative("nameText").objectReferenceValue = cardsList[i].nameText;
                    el.FindPropertyRelative("descText").objectReferenceValue = cardsList[i].descText;
                    el.FindPropertyRelative("monumentImage").objectReferenceValue = cardsList[i].monumentImage;
                    el.FindPropertyRelative("selectButton").objectReferenceValue = cardsList[i].selectButton;
                }
            }
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, SCENE_MONUMENT_SEL);
            Debug.Log($"[BKS_UPSetupUtility] Saved: {SCENE_MONUMENT_SEL}");
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // 2. MISSION SELECT SCENE
        // ══════════════════════════════════════════════════════════════════════════════

        private static void BuildMissionSelectScene(ChapterData chapter, List<MonumentData> monuments, List<MissionData> missions)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateStandard2DCamera();

            var canvasGo = CreateCanvasWithEventSystem("UPMissionSelectCanvas");
            var canvas = canvasGo.GetComponent<Canvas>();

            // Fullscreen BG
            var bg = CreateImagePanel("Background", canvasGo.transform, COL_UP_BG);
            StretchFull(bg);

            // Header
            var header = CreateImagePanel("Header", canvasGo.transform, COL_HEADER_BG);
            SetAnchors(header, new Vector2(0f, 0.86f), new Vector2(1f, 1f));

            var titleTMP = CreateTMP("MonumentTitle", header.transform, "TAJ MAHAL MISSIONS", 38, Color.white, FontStyles.Bold);
            SetAnchors(titleTMP.gameObject, new Vector2(0.04f, 0.45f), new Vector2(0.6f, 0.95f));

            var subTMP = CreateTMP("Subtitle", header.transform, "SELECT A MISSION ROUTE", 18, new Color(1f, 0.85f, 0.5f));
            SetAnchors(subTMP.gameObject, new Vector2(0.04f, 0.05f), new Vector2(0.6f, 0.48f));

            var statsTMP = CreateTMP("Stats", header.transform, "★ 0   🪙 0", 22, Color.yellow, FontStyles.Bold);
            SetAnchors(statsTMP.gameObject, new Vector2(0.62f, 0.2f), new Vector2(0.82f, 0.8f));

            var backBtn = CreateButton("BackButton", header.transform, "← MONUMENTS", 18, COL_BTN_ACCENT, Color.white);
            SetAnchors(backBtn.gameObject, new Vector2(0.85f, 0.2f), new Vector2(0.97f, 0.8f));

            // Content Panel
            var cardsContainer = new GameObject("MissionCardsContainer", typeof(RectTransform));
            cardsContainer.transform.SetParent(canvasGo.transform, false);
            SetAnchors(cardsContainer, new Vector2(0.03f, 0.06f), new Vector2(0.97f, 0.82f));

            var ui = canvasGo.AddComponent<BKS_UPMissionSelectUI>();
            var cardsList = new List<BKS_UPMissionSelectUI.MissionCard>();

            // 5 mission cards in 2 rows (3 top, 2 bottom-centered) — same layout as monument select
            float[,] mCardPos = {
                { 0.01f,  0.52f,  0.315f, 0.98f },
                { 0.345f, 0.52f,  0.655f, 0.98f },
                { 0.685f, 0.52f,  0.99f,  0.98f },
                { 0.175f, 0.02f,  0.48f,  0.48f },
                { 0.52f,  0.02f,  0.825f, 0.48f },
            };

            for (int i = 0; i < 5; i++)
            {
                var cardGo = CreateImagePanel($"MissionCard_{i + 1}", cardsContainer.transform, COL_CARD_BG);
                SetAnchors(cardGo, new Vector2(mCardPos[i, 0], mCardPos[i, 1]), new Vector2(mCardPos[i, 2], mCardPos[i, 3]));

                var numTMP = CreateTMP("MissionNumber", cardGo.transform, $"MISSION {i + 1:D2}", 16, new Color(0.7f, 0.2f, 0.1f), FontStyles.Bold);
                SetAnchors(numTMP.gameObject, new Vector2(0.06f, 0.86f), new Vector2(0.94f, 0.95f));

                string mName = i < missions.Count ? missions[i].MissionName : $"Mission {i + 1}";
                var nameTMP = CreateTMP("MissionName", cardGo.transform, mName, 18, new Color(0.15f, 0.12f, 0.1f), FontStyles.Bold);
                SetAnchors(nameTMP.gameObject, new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.85f));

                // Stars display
                var starsPanel = new GameObject("StarsPanel", typeof(RectTransform));
                starsPanel.transform.SetParent(cardGo.transform, false);
                SetAnchors(starsPanel, new Vector2(0.15f, 0.6f), new Vector2(0.85f, 0.7f));
                for (int s = 0; s < 3; s++)
                {
                    var starGo = CreateTMP($"Star_{s + 1}", starsPanel.transform, "*", 24, Color.yellow, FontStyles.Bold);
                    SetAnchors(starGo.gameObject, new Vector2(s * 0.33f, 0f), new Vector2((s + 1) * 0.33f, 1f));
                }

                string obj = i < missions.Count ? missions[i].PrimaryObjectiveText : "Drive safely to destination.";
                var objTMP = CreateTMP("ObjectiveText", cardGo.transform, obj, 13, new Color(0.3f, 0.25f, 0.2f));
                SetAnchors(objTMP.gameObject, new Vector2(0.06f, 0.26f), new Vector2(0.94f, 0.58f));

                var playBtn = CreateButton("PlayBtn", cardGo.transform, "START MISSION", 16, COL_BTN_PRIMARY, Color.white);
                SetAnchors(playBtn.gameObject, new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.2f));

                // Lock Overlay
                var lockOverlay = CreateImagePanel("LockOverlay", cardGo.transform, new Color(0.2f, 0.2f, 0.2f, 0.85f));
                StretchFull(lockOverlay);
                var lockText = CreateTMP("LockText", lockOverlay.transform, "LOCKED\nComplete Previous Mission", 16, Color.white, FontStyles.Bold);
                StretchFull(lockText.gameObject);
                lockOverlay.SetActive(i > 0); // Only M01 unlocked initially

                var mCard = new BKS_UPMissionSelectUI.MissionCard
                {
                    root = cardGo,
                    missionNumberText = numTMP,
                    missionNameText = nameTMP,
                    objectiveText = objTMP,
                    playButton = playBtn,
                    lockOverlay = lockOverlay,
                    lockReasonText = lockText
                };
                cardsList.Add(mCard);
            }

            // Briefing Popup Modal
            var popupModal = CreateImagePanel("MissionBriefingPopup", canvasGo.transform, new Color(0f, 0f, 0f, 0.8f));
            StretchFull(popupModal);

            var popupBox = CreateImagePanel("PopupBox", popupModal.transform, COL_POPUP_BG);
            SetAnchors(popupBox, new Vector2(0.22f, 0.18f), new Vector2(0.78f, 0.82f));

            var pTitle = CreateTMP("Title", popupBox.transform, "MISSION BRIEFING", 32, Color.white, FontStyles.Bold);
            SetAnchors(pTitle.gameObject, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.95f));

            var pStory = CreateTMP("Story", popupBox.transform, "Story text goes here...", 18, new Color(0.9f, 0.85f, 0.75f));
            SetAnchors(pStory.gameObject, new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.8f));

            var pObj = CreateTMP("Objective", popupBox.transform, "Objective: Complete the run.", 18, new Color(0.5f, 0.95f, 0.5f), FontStyles.Bold);
            SetAnchors(pObj.gameObject, new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.5f));

            var pDiff = CreateTMP("Difficulty", popupBox.transform, "Difficulty: Moderate", 16, Color.yellow);
            SetAnchors(pDiff.gameObject, new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.34f));

            var pStart = CreateButton("StartBtn", popupBox.transform, "LET'S DRIVE! ▶", 22, COL_BTN_PRIMARY, Color.white);
            SetAnchors(pStart.gameObject, new Vector2(0.1f, 0.05f), new Vector2(0.52f, 0.18f));

            var pClose = CreateButton("CloseBtn", popupBox.transform, "CLOSE", 20, COL_BTN_CLOSE, Color.white);
            SetAnchors(pClose.gameObject, new Vector2(0.58f, 0.05f), new Vector2(0.9f, 0.18f));

            popupModal.SetActive(false);

            // Wire SerializedObject
            SerializedObject so = new SerializedObject(ui);
            SetSOProperty(so, "chapterData", chapter);
            if (monuments.Count > 0) SetSOProperty(so, "defaultMonument", monuments[0]);
            SetSOProperty(so, "monumentTitleText", titleTMP);
            SetSOProperty(so, "monumentSubtitleText", subTMP);
            SetSOProperty(so, "totalCoinsText", statsTMP);
            SetSOProperty(so, "totalStarsText", statsTMP);
            SetSOProperty(so, "backButton", backBtn);

            var popProp = so.FindProperty("briefingPopup");
            if (popProp != null)
            {
                popProp.FindPropertyRelative("panel").objectReferenceValue = popupModal;
                popProp.FindPropertyRelative("titleText").objectReferenceValue = pTitle;
                popProp.FindPropertyRelative("storyText").objectReferenceValue = pStory;
                popProp.FindPropertyRelative("objectiveText").objectReferenceValue = pObj;
                popProp.FindPropertyRelative("difficultyText").objectReferenceValue = pDiff;
                popProp.FindPropertyRelative("startButton").objectReferenceValue = pStart;
                popProp.FindPropertyRelative("closeButton").objectReferenceValue = pClose;
            }

            var cardsProp = so.FindProperty("missionCards");
            if (cardsProp != null)
            {
                cardsProp.ClearArray();
                for (int i = 0; i < cardsList.Count; i++)
                {
                    cardsProp.InsertArrayElementAtIndex(i);
                    var el = cardsProp.GetArrayElementAtIndex(i);
                    el.FindPropertyRelative("root").objectReferenceValue = cardsList[i].root;
                    el.FindPropertyRelative("missionNumberText").objectReferenceValue = cardsList[i].missionNumberText;
                    el.FindPropertyRelative("missionNameText").objectReferenceValue = cardsList[i].missionNameText;
                    el.FindPropertyRelative("objectiveText").objectReferenceValue = cardsList[i].objectiveText;
                    el.FindPropertyRelative("playButton").objectReferenceValue = cardsList[i].playButton;
                    el.FindPropertyRelative("lockOverlay").objectReferenceValue = cardsList[i].lockOverlay;
                    el.FindPropertyRelative("lockReasonText").objectReferenceValue = cardsList[i].lockReasonText;
                }
            }
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, SCENE_MISSION_SEL);
            Debug.Log($"[BKS_UPSetupUtility] Saved: {SCENE_MISSION_SEL}");
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // 3. GAMEPLAY SCENE BUILDER
        // ══════════════════════════════════════════════════════════════════════════════

        private static void BuildGameplayScene(string targetScenePath, MissionData missionData, Color skyColor, string trackName)
        {
            if (File.Exists(LEVEL1_SOURCE))
            {
                if (File.Exists(targetScenePath))
                {
                    AssetDatabase.DeleteAsset(targetScenePath);
                }
                AssetDatabase.CopyAsset(LEVEL1_SOURCE, targetScenePath);
                AssetDatabase.Refresh();
            }

            var scene = EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);

            // Camera sky color
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = skyColor;
            }

            // 1. [BKS_Overlay] Container
            var overlay = GameObject.Find("[BKS_Overlay]");
            if (overlay == null) overlay = new GameObject("[BKS_Overlay]");

            var mm = overlay.GetComponent<MissionManager>();
            if (mm == null) mm = overlay.AddComponent<MissionManager>();
            var soMM = new SerializedObject(mm);
            var propSO = soMM.FindProperty("missionAsset");
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

            // RewardManager
            var rm = overlay.GetComponent<BKS_RewardManager>();
            if (rm == null) rm = overlay.AddComponent<BKS_RewardManager>();

            // CheckpointManager
            var cm = overlay.GetComponent<BKS_CheckpointManager>();
            if (cm == null) cm = overlay.AddComponent<BKS_CheckpointManager>();

            // 2. Setup Car
            CarController car = Object.FindFirstObjectByType<CarController>();
            if (car != null)
            {
                car.gameObject.tag = "Player";
                if (car.GetComponent<BKS_VehicleHealth>() == null)
                    car.gameObject.AddComponent<BKS_VehicleHealth>();

                if (car.GetComponent<BKS_HornController>() == null)
                    car.gameObject.AddComponent<BKS_HornController>();

                if (car.GetComponent<BKS_UpgradeLoader>() == null)
                    car.gameObject.AddComponent<BKS_UpgradeLoader>();

                cm.playerObject = car.gameObject;
            }

            // 3. HUD Overlay
            AttachHUDToScene();

            // 4. Level Elements (Checkpoints, Multipliers, Pickups, Obstacles, Destination)
            SpawnTrackGameplayElements(trackName);

            EditorSceneManager.SaveScene(scene, targetScenePath);
            Debug.Log($"[BKS_UPSetupUtility] Saved Gameplay Scene: {targetScenePath}");
        }

        private static void AttachHUDToScene()
        {
            Canvas targetCanvas = Object.FindFirstObjectByType<Canvas>();
            if (targetCanvas == null)
            {
                targetCanvas = CreateCanvasWithEventSystem("BKS_HUDCanvas").GetComponent<Canvas>();
            }

            // Top Status Panel
            var topPanel = GameObject.Find("TopStatusPanel");
            if (topPanel == null)
            {
                topPanel = CreateImagePanel("TopStatusPanel", targetCanvas.transform, new Color(0.12f, 0.1f, 0.08f, 0.9f));
                var topRect = topPanel.GetComponent<RectTransform>();
                topRect.anchorMin = new Vector2(0f, 0.88f);
                topRect.anchorMax = new Vector2(1f, 1f);
                topRect.offsetMin = topRect.offsetMax = Vector2.zero;

                // Health Slider
                var healthGo = new GameObject("HealthSlider", typeof(RectTransform), typeof(Slider));
                healthGo.transform.SetParent(topPanel.transform, false);
                SetAnchors(healthGo, new Vector2(0.02f, 0.2f), new Vector2(0.24f, 0.8f));
                var slider = healthGo.GetComponent<Slider>();
                var fillBg = CreateImagePanel("Background", healthGo.transform, new Color(0.3f, 0.1f, 0.1f, 0.8f));
                StretchFull(fillBg);
                var fillArea = new GameObject("Fill Area", typeof(RectTransform));
                fillArea.transform.SetParent(healthGo.transform, false);
                StretchFull(fillArea);
                var fill = CreateImagePanel("Fill", fillArea.transform, new Color(0.2f, 0.85f, 0.3f, 1f));
                StretchFull(fill);
                slider.targetGraphic = fill.GetComponent<Image>();
                slider.fillRect = fill.GetComponent<RectTransform>();
                slider.value = 1f;

                var hpText = CreateTMP("HPText", healthGo.transform, "100%", 16, Color.white, FontStyles.Bold);
                StretchFull(hpText.gameObject);

                // Stats: Stars, Coins, Score
                var starsTMP = CreateTMP("StarsText", topPanel.transform, "STARS: 0/10", 20, Color.yellow, FontStyles.Bold);
                SetAnchors(starsTMP.gameObject, new Vector2(0.35f, 0.1f), new Vector2(0.55f, 0.9f));

                // Redundant CoinsText removed

                var scoreTMP = CreateTMP("ScoreText", topPanel.transform, "SCORE: 0", 20, Color.white, FontStyles.Bold);
                SetAnchors(scoreTMP.gameObject, new Vector2(0.65f, 0.1f), new Vector2(0.85f, 0.9f));

                // Multiplier Badge
                var multBadge = CreateImagePanel("MultiplierBadge", topPanel.transform, new Color(0.9f, 0.2f, 0.1f, 0.95f));
                SetAnchors(multBadge, new Vector2(0.78f, 0.1f), new Vector2(0.96f, 0.9f));
                var multText = CreateTMP("MultiplierText", multBadge.transform, "2X BOOST!", 16, Color.yellow, FontStyles.Bold);
                StretchFull(multText.gameObject);
                multBadge.SetActive(false);

                // Speedometer in bottom center
                var speedTMP = CreateTMP("Speedometer", targetCanvas.transform, "0 km/h", 26, Color.white, FontStyles.Bold);
                SetAnchors(speedTMP.gameObject, new Vector2(0.4f, 0.02f), new Vector2(0.6f, 0.12f));

                // Signature Horn Button (Bottom Center)
                var hornBtn = CreateButton("BKS_HornButton", targetCanvas.transform, "📯 HONK!", 24, new Color(0.95f, 0.45f, 0.05f), Color.white);
                SetAnchors(hornBtn.gameObject, new Vector2(0.42f, 0.13f), new Vector2(0.58f, 0.28f));

                // Wire HUD component
                var hud = targetCanvas.GetComponent<BKS_GameplayHUD>();
                if (hud == null) hud = targetCanvas.gameObject.AddComponent<BKS_GameplayHUD>();

                SerializedObject so = new SerializedObject(hud);
                SetSOProperty(so, "healthBar", slider);
                SetSOProperty(so, "healthText", hpText);
                SetSOProperty(so, "safarStarsText", starsTMP);
                // SetSOProperty(so, "coinsText", coinsTMP); // Removed to avoid duplicate coin tracking
                SetSOProperty(so, "scoreText", scoreTMP);
                SetSOProperty(so, "multiplierBadge", multBadge);
                SetSOProperty(so, "multiplierText", multText);
                SetSOProperty(so, "speedText", speedTMP);
                SetSOProperty(so, "hornButton", hornBtn);
                so.ApplyModifiedProperties();
            }
        }

        private static void SpawnTrackGameplayElements(string trackName)
        {
            var old = GameObject.Find("[BKS_LevelElements]");
            if (old != null) Object.DestroyImmediate(old);

            var root = new GameObject("[BKS_LevelElements]");

            Sprite whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);

            // Track Marker Label
            var trackSign = new GameObject("TrackSign");
            trackSign.transform.SetParent(root.transform);
            trackSign.transform.position = new Vector3(5f, 3.5f, 0f);
            CreateWorldLabel(trackSign.transform, $"🏁 {trackName.ToUpper()} 🏁", Color.yellow);

            // 1. Checkpoint 1 at X = 110
            SpawnCheckpoint(root.transform, 110f, 1, whiteSprite);

            // 2. Checkpoint 2 at X = 230
            SpawnCheckpoint(root.transform, 230f, 2, whiteSprite);

            // 3. Multiplier Pickups (2X)
            SpawnMultiplierPickup(root.transform, 75f, whiteSprite);
            SpawnMultiplierPickup(root.transform, 190f, whiteSprite);

            // 4. Coin Pickups along hills
            float[] coinXs = { 20f, 35f, 50f, 65f, 95f, 130f, 150f, 170f, 210f, 250f, 275f, 305f };
            foreach (float cx in coinXs)
            {
                SpawnCoin(root.transform, cx, whiteSprite);
            }

            // 5. Normal Obstacles (Speed penalty / pothole)
            float[] normObsXs = { 45f, 135f, 215f, 280f };
            foreach (float ox in normObsXs)
            {
                SpawnObstacle(root.transform, ox, CrashSeverity.NormalHit, whiteSprite);
            }

            // 6. Critical Obstacles (Requires caution)
            float[] critObsXs = { 160f, 290f };
            foreach (float ox in critObsXs)
            {
                SpawnObstacle(root.transform, ox, CrashSeverity.CriticalCrash, whiteSprite);
            }

            // 7. Destination Gate at finish line
            SpawnDestination(root.transform, 340f, whiteSprite);
        }

        private static void SpawnCheckpoint(Transform parent, float posX, int cpIndex, Sprite sprite)
        {
            var cpGo = new GameObject($"BKS_Checkpoint_{cpIndex}_X{posX}");
            cpGo.transform.SetParent(parent);
            cpGo.transform.position = new Vector3(posX, 1.8f, 0f);

            var sr = cpGo.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.2f, 0.7f, 0.9f, 0.75f);
            cpGo.transform.localScale = new Vector3(1.2f, 3.5f, 1f);

            var col = cpGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 1f);

            cpGo.AddComponent<BKS_Checkpoint>();
            CreateWorldLabel(cpGo.transform, $"🚩 CHECKPOINT {cpIndex}\n(Auto-Save)", Color.cyan);
        }

        private static void SpawnMultiplierPickup(Transform parent, float posX, Sprite sprite)
        {
            var mGo = new GameObject($"BKS_Multiplier2X_X{posX}");
            mGo.transform.SetParent(parent);
            mGo.transform.position = new Vector3(posX, 2.2f, 0f);
            mGo.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            var sr = mGo.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(1f, 0.2f, 0.1f, 1f); // Neon red

            var col = mGo.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;

            mGo.AddComponent<BKS_MultiplierPickup>();
            CreateWorldLabel(mGo.transform, "⚡ 2X MULTIPLIER\n(10s BOOST)", Color.red);
        }

        private static void SpawnCoin(Transform parent, float posX, Sprite sprite)
        {
            var cGo = new GameObject($"BKS_Coin_X{posX}");
            cGo.transform.SetParent(parent);
            cGo.transform.position = new Vector3(posX, 1.8f, 0f);
            cGo.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

            var sr = cGo.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(1f, 0.85f, 0.1f, 1f); // Gold

            var col = cGo.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;

            cGo.AddComponent<BKS_CoinPickup>();
            CreateWorldLabel(cGo.transform, "🪙 +10", Color.yellow);
        }

        private static void SpawnObstacle(Transform parent, float posX, CrashSeverity severity, Sprite sprite)
        {
            string label = severity == CrashSeverity.CriticalCrash ? "🚨 CRITICAL HAZARD!\n(Crash Warning)" : "⚠️ ROAD BUMP\n(Slow Down)";
            Color colTint = severity == CrashSeverity.CriticalCrash ? new Color(0.8f, 0.1f, 0.1f, 1f) : new Color(0.9f, 0.5f, 0.1f, 1f);

            var oGo = new GameObject($"BKS_Obstacle_{severity}_X{posX}");
            oGo.transform.SetParent(parent);
            oGo.transform.position = new Vector3(posX, 0.6f, 0f);
            oGo.transform.localScale = severity == CrashSeverity.CriticalCrash ? new Vector3(1.8f, 1.8f, 1f) : new Vector3(2.2f, 0.6f, 1f);

            var sr = oGo.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = colTint;

            var col = oGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 1f);

            var obs = oGo.AddComponent<BKS_Obstacle>();
            obs.severity = severity;
            obs.speedPenaltyMultiplier = 0.4f;

            CreateWorldLabel(oGo.transform, label, colTint);
        }

        private static void SpawnDestination(Transform parent, float posX, Sprite sprite)
        {
            var dGo = new GameObject($"BKS_Destination_Gate_X{posX}");
            dGo.transform.SetParent(parent);
            dGo.transform.position = new Vector3(posX, 2.5f, 0f);
            dGo.transform.localScale = new Vector3(2f, 5f, 1f);

            var sr = dGo.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.1f, 0.8f, 0.3f, 0.8f); // Glowing Green

            var col = dGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 1f);

            dGo.AddComponent<BKS_Destination>();
            CreateWorldLabel(dGo.transform, "🏁 DESTINATION 🏁\n(Finish Line)", Color.green);
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // 4. BUILD SETTINGS CONFIGURATION
        // ══════════════════════════════════════════════════════════════════════════════

        private static void UpdateBuildSettings()
        {
            string[] requiredScenes = {
                "Assets/BharatKaSafar/Scenes/BKS_IndiaMap.unity",
                SCENE_MONUMENT_SEL,
                SCENE_MISSION_SEL,
                SCENE_M01_AGRA,
                SCENE_M02_VARANASI,
                SCENE_M03_SARNATH,
                SCENE_M04_PRAYAGRAJ,
                SCENE_M05_BHULBHULAIYA,
                "Assets/BharatKaSafar/Scenes/BKS_MissionResult.unity",
                "Assets/BharatKaSafar/Scenes/BKS_Garage.unity"
            };

            var existingScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            var existingPaths = new HashSet<string>();
            foreach (var s in existingScenes) existingPaths.Add(s.path);

            bool modified = false;
            foreach (var p in requiredScenes)
            {
                if (!existingPaths.Contains(p) && File.Exists(p))
                {
                    existingScenes.Add(new EditorBuildSettingsScene(p, true));
                    modified = true;
                }
            }

            if (modified)
            {
                EditorBuildSettings.scenes = existingScenes.ToArray();
                Debug.Log("[BKS_UPSetupUtility] Updated EditorBuildSettings with UP scenes.");
            }
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // UI HELPERS
        // ══════════════════════════════════════════════════════════════════════════════

        private static GameObject CreateCanvasWithEventSystem(string name)
        {
            var canvasGo = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = canvasGo.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            }

            return canvasGo;
        }

        private static void CreateStandard2DCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camGo.tag = "MainCamera";
                cam = camGo.GetComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static GameObject CreateImagePanel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static TMP_Text CreateTMP(string name, Transform parent, string text, float size, Color color, FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private static Button CreateButton(string name, Transform parent, string text, float size, Color bgCol, Color textCol)
        {
            var go = CreateImagePanel(name, parent, bgCol);
            var btn = go.AddComponent<Button>();
            var tmp = CreateTMP("Label", go.transform, text, size, textCol, FontStyles.Bold);
            StretchFull(tmp.gameObject);
            return btn;
        }

        private static void StretchFull(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void SetAnchors(GameObject go, Vector2 min, Vector2 max)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void CreateWorldLabel(Transform parent, string textStr, Color color)
        {
            var labelGo = new GameObject("WorldLabel");
            labelGo.transform.SetParent(parent, false);
            labelGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            labelGo.transform.localScale = new Vector3(0.12f, 0.12f, 1f);

            var tm = labelGo.AddComponent<TextMesh>();
            tm.text = textStr;
            tm.characterSize = 0.1f;
            tm.fontSize = 100;
            tm.fontStyle = FontStyle.Bold;
            tm.color = color;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
        }

        private static void SetSOProperty(SerializedObject so, string propName, Object value)
        {
            var prop = so.FindProperty(propName);
            if (prop != null) prop.objectReferenceValue = value;
        }
    }
}
#endif
