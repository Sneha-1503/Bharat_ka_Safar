#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BharatKaSafar.Core;
using BharatKaSafar.Missions;
using BharatKaSafar.UI;

namespace BharatKaSafar.EditorTools
{
    /// <summary>
    /// Builds the full Rajasthan Mission Select scene UI hierarchy.
    /// Run this AFTER SetupRajasthanChapter to wire the scene properly.
    /// Menu: Bharat Ka Safar -> Build Rajasthan Mission Select Scene UI
    /// </summary>
    public static class BKS_RajasthanSceneBuilder
    {
        private const string CHAPTER_SO_PATH = "Assets/BharatKaSafar/ScriptableObjects/chapters/RajasthanChapter.asset";
        private const string SCENE_PATH = "Assets/BharatKaSafar/Scenes/Rajasthan/BKS_Rajasthan_MissionSelect.unity";

        private const string M01_SO = "Assets/BharatKaSafar/ScriptableObjects/missions/Rajasthan/Rajasthan_M01_Jaipur.asset";
        private const string M02_SO = "Assets/BharatKaSafar/ScriptableObjects/missions/Rajasthan/Rajasthan_M02_Jodhpur.asset";
        private const string M03_SO = "Assets/BharatKaSafar/ScriptableObjects/missions/Rajasthan/Rajasthan_M03_Jaisalmer.asset";
        private const string M04_SO = "Assets/BharatKaSafar/ScriptableObjects/missions/Rajasthan/Rajasthan_M04_Udaipur.asset";
        private const string M05_SO = "Assets/BharatKaSafar/ScriptableObjects/missions/Rajasthan/Rajasthan_M05_Pushkar.asset";

        // Rajasthan color palette
        private static readonly Color COL_BG         = new Color(0.95f, 0.55f, 0.15f, 1f);  // Rajasthan saffron
        private static readonly Color COL_HEADER_BG  = new Color(0.7f,  0.1f,  0.05f, 0.9f); // Deep maroon
        private static readonly Color COL_CARD_BG    = new Color(1f,   0.93f, 0.78f, 1f);    // Warm sandstone
        private static readonly Color COL_CARD_LOCKED= new Color(0.55f, 0.5f,  0.45f, 1f);   // Grey
        private static readonly Color COL_BTN        = new Color(0.18f, 0.5f,  0.2f,  1f);   // Forest green
        private static readonly Color COL_POPUP_BG   = new Color(0.12f, 0.08f, 0.05f, 0.97f);// Dark parchment
        private static readonly Color COL_STAR_FILLED= new Color(1f,   0.85f, 0.1f,  1f);    // Gold
        private static readonly Color COL_STAR_EMPTY = new Color(0.4f,  0.4f,  0.4f,  1f);   // Grey

        [MenuItem("Bharat Ka Safar/Build Rajasthan Mission Select Scene UI", false, 2)]
        public static void BuildScene()
        {
            ChapterData chapter = AssetDatabase.LoadAssetAtPath<ChapterData>(CHAPTER_SO_PATH);
            if (chapter == null)
            {
                EditorUtility.DisplayDialog("Error", "RajasthanChapter.asset not found. Run 'Setup Rajasthan Chapter' first.", "OK");
                return;
            }

            // Load the mission SOs
            MissionData[] missions =
            {
                AssetDatabase.LoadAssetAtPath<MissionData>(M01_SO),
                AssetDatabase.LoadAssetAtPath<MissionData>(M02_SO),
                AssetDatabase.LoadAssetAtPath<MissionData>(M03_SO),
                AssetDatabase.LoadAssetAtPath<MissionData>(M04_SO),
                AssetDatabase.LoadAssetAtPath<MissionData>(M05_SO),
            };

            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

            // Clear old canvas
            var oldCanvas = GameObject.Find("MissionSelectCanvas");
            if (oldCanvas != null) Object.DestroyImmediate(oldCanvas);

            // ── ROOT CANVAS ───────────────────────────────────────────────────────
            var canvasGo = CreateCanvas("RajasthanMissionSelectCanvas");
            Canvas canvas = canvasGo.GetComponent<Canvas>();

            // Background panel
            var bg = CreateImagePanel("Background", canvasGo.transform, COL_BG);
            StretchFull(bg);

            // ── HEADER ────────────────────────────────────────────────────────────
            var header = CreateImagePanel("Header", canvasGo.transform, COL_HEADER_BG);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 0.88f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.offsetMin = headerRect.offsetMax = Vector2.zero;

            var titleTMP = CreateTMP("ChapterTitle", header.transform, "RAJASTHAN", 42, Color.white, FontStyle.Bold);
            var titleRect = titleTMP.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.5f);
            titleRect.anchorMax = new Vector2(0.75f, 1f);
            titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;

            var subtitleTMP = CreateTMP("ChapterSubtitle", header.transform, "DESERT KINGDOMS", 22, new Color(1f, 0.85f, 0.5f));
            var subRect = subtitleTMP.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.05f, 0f);
            subRect.anchorMax = new Vector2(0.75f, 0.55f);
            subRect.offsetMin = subRect.offsetMax = Vector2.zero;

            // Back Button in header
            var backBtn = CreateButton("BackButton", header.transform, "← BACK", 22, COL_BTN, Color.white);
            var backRect = backBtn.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.78f, 0.1f);
            backRect.anchorMax = new Vector2(0.98f, 0.9f);
            backRect.offsetMin = backRect.offsetMax = Vector2.zero;

            // ── MISSION CARDS GRID ────────────────────────────────────────────────
            var cardsContainer = new GameObject("MissionCardsContainer");
            cardsContainer.transform.SetParent(canvasGo.transform, false);
            var cardsRect = cardsContainer.AddComponent<RectTransform>();
            cardsRect.anchorMin = new Vector2(0.02f, 0.04f);
            cardsRect.anchorMax = new Vector2(0.98f, 0.87f);
            cardsRect.offsetMin = cardsRect.offsetMax = Vector2.zero;

            // 5 cards in a horizontal+wrap layout
            // We place them in a 2-row grid: 3 on top, 2 on bottom
            var cardSlots = new (Vector2 min, Vector2 max)[]
            {
                (new Vector2(0f,    0.52f), new Vector2(0.32f, 1f)),
                (new Vector2(0.34f, 0.52f), new Vector2(0.66f, 1f)),
                (new Vector2(0.68f, 0.52f), new Vector2(1f,    1f)),
                (new Vector2(0.1f,  0f),    new Vector2(0.45f, 0.48f)),
                (new Vector2(0.55f, 0f),    new Vector2(0.9f,  0.48f)),
            };

            string[] missionLocations = { "Jaipur", "Jodhpur", "Jaisalmer", "Udaipur", "Pushkar" };

            var cardList = new List<BKS_RajasthanMissionSelectUI.MissionCard>();

            for (int i = 0; i < 5; i++)
            {
                var (cMin, cMax) = cardSlots[i];
                var cardRoot = CreateImagePanel($"MissionCard_{i + 1}", cardsContainer.transform, COL_CARD_BG);
                var cardRect = cardRoot.GetComponent<RectTransform>();
                cardRect.anchorMin = cMin;
                cardRect.anchorMax = cMax;
                cardRect.offsetMin = new Vector2(5f, 5f);
                cardRect.offsetMax = new Vector2(-5f, -5f);

                // Add rounded corner look via Image padding
                var cardImg = cardRoot.GetComponent<Image>();
                cardImg.type = Image.Type.Sliced;

                // Mission number badge
                var numBg = CreateImagePanel("NumberBadge", cardRoot.transform, COL_HEADER_BG);
                var numRect = numBg.GetComponent<RectTransform>();
                numRect.anchorMin = new Vector2(0f, 0.78f);
                numRect.anchorMax = new Vector2(0.35f, 1f);
                numRect.offsetMin = numRect.offsetMax = Vector2.zero;

                var numTMP = CreateTMP("MissionNumber", numBg.transform, $"M{i + 1}", 26, Color.white, FontStyle.Bold);
                StretchFull(numTMP);

                // Location name
                var locTMP = CreateTMP("LocationName", cardRoot.transform, missionLocations[i].ToUpper(), 18, COL_HEADER_BG, FontStyle.Bold);
                var locRect = locTMP.GetComponent<RectTransform>();
                locRect.anchorMin = new Vector2(0.37f, 0.78f);
                locRect.anchorMax = new Vector2(1f, 1f);
                locRect.offsetMin = locRect.offsetMax = Vector2.zero;

                // Mission name
                string mName = missions[i] != null ? missions[i].MissionName : $"Mission {i + 1}";
                var nameTMP = CreateTMP("MissionName", cardRoot.transform, mName, 14, new Color(0.2f, 0.1f, 0f));
                var nameRect = nameTMP.GetComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0.05f, 0.58f);
                nameRect.anchorMax = new Vector2(0.95f, 0.78f);
                nameRect.offsetMin = nameRect.offsetMax = Vector2.zero;

                // Stars row
                var starsGo = new GameObject("Stars");
                starsGo.transform.SetParent(cardRoot.transform, false);
                var starsRect = starsGo.AddComponent<RectTransform>();
                starsRect.anchorMin = new Vector2(0.05f, 0.35f);
                starsRect.anchorMax = new Vector2(0.95f, 0.57f);
                starsRect.offsetMin = starsRect.offsetMax = Vector2.zero;

                var starImgs = new Image[3];
                for (int s = 0; s < 3; s++)
                {
                    var starGo = CreateImagePanel($"Star_{s}", starsGo.transform, COL_STAR_EMPTY);
                    var starRect = starGo.GetComponent<RectTransform>();
                    float w = 0.3f;
                    starRect.anchorMin = new Vector2(s * w, 0f);
                    starRect.anchorMax = new Vector2(s * w + w - 0.03f, 1f);
                    starRect.offsetMin = starRect.offsetMax = Vector2.zero;
                    starImgs[s] = starGo.GetComponent<Image>();
                    CreateTMP($"StarLabel_{s}", starGo.transform, "★", 20, Color.white);
                }

                // Play button
                var playBtn = CreateButton("PlayButton", cardRoot.transform, "PLAY", 18, COL_BTN, Color.white);
                var playBtnRect = playBtn.GetComponent<RectTransform>();
                playBtnRect.anchorMin = new Vector2(0.1f, 0.03f);
                playBtnRect.anchorMax = new Vector2(0.9f, 0.33f);
                playBtnRect.offsetMin = playBtnRect.offsetMax = Vector2.zero;

                // Lock overlay
                var lockOverlay = CreateImagePanel("LockOverlay", cardRoot.transform, new Color(0f, 0f, 0f, 0.65f));
                StretchFull(lockOverlay);
                var lockTMP = CreateTMP("LockText", lockOverlay.transform, "🔒 LOCKED\nComplete previous level", 14, Color.white);
                StretchFull(lockTMP);
                lockOverlay.SetActive(i != 0); // Only Jaipur unlocked by default in editor

                var mCard = new BKS_RajasthanMissionSelectUI.MissionCard
                {
                    root              = cardRoot,
                    playButton        = playBtn.GetComponent<Button>(),
                    missionNumberText = numTMP.GetComponent<TMP_Text>(),
                    missionNameText   = nameTMP.GetComponent<TMP_Text>(),
                    storyText         = null,
                    lockOverlay       = lockOverlay,
                    starImages        = starImgs,
                    starFilled        = null,
                    starEmpty         = null,
                };
                cardList.Add(mCard);
            }

            // ── MISSION BRIEFING POPUP ────────────────────────────────────────────
            var popup = CreateImagePanel("MissionBriefingPopup", canvasGo.transform, COL_POPUP_BG);
            StretchFull(popup, -60f);
            popup.SetActive(false);

            var popupTitleTMP = CreateTMP("PopupTitle", popup.transform, "MISSION NAME", 36, new Color(1f, 0.85f, 0.3f), FontStyle.Bold);
            var popupTitleRect = popupTitleTMP.GetComponent<RectTransform>();
            popupTitleRect.anchorMin = new Vector2(0.05f, 0.8f);
            popupTitleRect.anchorMax = new Vector2(0.95f, 0.97f);
            popupTitleRect.offsetMin = popupTitleRect.offsetMax = Vector2.zero;

            var popupStoryTMP = CreateTMP("PopupStory", popup.transform, "Mission story text...", 16, Color.white);
            var popupStoryRect = popupStoryTMP.GetComponent<RectTransform>();
            popupStoryRect.anchorMin = new Vector2(0.05f, 0.5f);
            popupStoryRect.anchorMax = new Vector2(0.95f, 0.8f);
            popupStoryRect.offsetMin = popupStoryRect.offsetMax = Vector2.zero;

            var popupObjTMP = CreateTMP("PopupObjective", popup.transform, "Objective: ...", 18, new Color(0.5f, 1f, 0.5f));
            var popupObjRect = popupObjTMP.GetComponent<RectTransform>();
            popupObjRect.anchorMin = new Vector2(0.05f, 0.35f);
            popupObjRect.anchorMax = new Vector2(0.95f, 0.52f);
            popupObjRect.offsetMin = popupObjRect.offsetMax = Vector2.zero;

            var popupDiffTMP = CreateTMP("PopupDifficulty", popup.transform, "Difficulty: Easy", 16, new Color(1f, 0.7f, 0.3f));
            var popupDiffRect = popupDiffTMP.GetComponent<RectTransform>();
            popupDiffRect.anchorMin = new Vector2(0.05f, 0.24f);
            popupDiffRect.anchorMax = new Vector2(0.95f, 0.36f);
            popupDiffRect.offsetMin = popupDiffRect.offsetMax = Vector2.zero;

            var popupStartBtn = CreateButton("StartMissionButton", popup.transform, "▶ START MISSION", 22, new Color(0.15f, 0.6f, 0.2f), Color.white);
            var startBtnRect = popupStartBtn.GetComponent<RectTransform>();
            startBtnRect.anchorMin = new Vector2(0.3f, 0.06f);
            startBtnRect.anchorMax = new Vector2(0.95f, 0.22f);
            startBtnRect.offsetMin = startBtnRect.offsetMax = Vector2.zero;

            var popupCloseBtn = CreateButton("CloseBriefingButton", popup.transform, "✕ CLOSE", 18, new Color(0.5f, 0.1f, 0.1f), Color.white);
            var closeBtnRect = popupCloseBtn.GetComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(0.05f, 0.06f);
            closeBtnRect.anchorMax = new Vector2(0.28f, 0.22f);
            closeBtnRect.offsetMin = closeBtnRect.offsetMax = Vector2.zero;

            // ── WIRE BKS_RajasthanMissionSelectUI ─────────────────────────────────
            var ui = canvasGo.AddComponent<BKS_RajasthanMissionSelectUI>();
            var so = new SerializedObject(ui);

            SetField(so, "chapterData",       chapter);
            SetField(so, "chapterTitleText",   titleTMP.GetComponent<TMP_Text>());
            SetField(so, "chapterSubtitleText",subtitleTMP.GetComponent<TMP_Text>());
            SetField(so, "backButton",         backBtn.GetComponent<Button>());

            // Wire popup
            SetField(so, "briefingPopup.panel",        popup);
            SetField(so, "briefingPopup.titleText",     popupTitleTMP.GetComponent<TMP_Text>());
            SetField(so, "briefingPopup.storyText",     popupStoryTMP.GetComponent<TMP_Text>());
            SetField(so, "briefingPopup.objectiveText", popupObjTMP.GetComponent<TMP_Text>());
            SetField(so, "briefingPopup.difficultyText",popupDiffTMP.GetComponent<TMP_Text>());
            SetField(so, "briefingPopup.startButton",   popupStartBtn.GetComponent<Button>());
            SetField(so, "briefingPopup.closeButton",   popupCloseBtn.GetComponent<Button>());

            // Wire mission cards array
            var cardsProp = so.FindProperty("missionCards");
            cardsProp.arraySize = cardList.Count;
            for (int i = 0; i < cardList.Count; i++)
            {
                var elem = cardsProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("root").objectReferenceValue          = cardList[i].root;
                elem.FindPropertyRelative("playButton").objectReferenceValue    = cardList[i].playButton;
                elem.FindPropertyRelative("missionNumberText").objectReferenceValue = cardList[i].missionNumberText;
                elem.FindPropertyRelative("missionNameText").objectReferenceValue   = cardList[i].missionNameText;
                elem.FindPropertyRelative("lockOverlay").objectReferenceValue   = cardList[i].lockOverlay;
                var starsArr = elem.FindPropertyRelative("starImages");
                starsArr.arraySize = 3;
                for (int s = 0; s < 3; s++)
                    starsArr.GetArrayElementAtIndex(s).objectReferenceValue = cardList[i].starImages[s];
            }

            so.ApplyModifiedProperties();

            // Ensure EventSystem exists
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Camera
            if (Camera.main == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.backgroundColor = new Color(0.95f, 0.55f, 0.15f);
            }

            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            AssetDatabase.Refresh();

            Debug.Log("[BKS_RajasthanSceneBuilder] ✅ Rajasthan Mission Select scene built successfully!");
            EditorUtility.DisplayDialog("Done!", "Rajasthan Mission Select UI built and saved.\n\nOpen the scene to review the layout.", "OK");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static GameObject CreateCanvas(string name)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        private static GameObject CreateImagePanel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static GameObject CreateTMP(string name, Transform parent, string text, float size, Color color, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = (style == FontStyle.Bold) ? FontStyles.Bold : FontStyles.Normal;
            tmp.enableWordWrapping = true;
            return go;
        }

        private static GameObject CreateButton(string name, Transform parent, string label, float fontSize, Color bgColor, Color textColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = bgColor;
            var lbl = CreateTMP("Label", go.transform, label, fontSize, textColor, FontStyle.Bold);
            StretchFull(lbl);
            return go;
        }

        private static void StretchFull(GameObject go, float padding = 0f)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        private static void SetField(SerializedObject so, string path, Object value)
        {
            var prop = so.FindProperty(path);
            if (prop != null) prop.objectReferenceValue = value;
        }
    }
}
#endif
