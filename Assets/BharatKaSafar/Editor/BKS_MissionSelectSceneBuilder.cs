using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BharatKaSafar.Core;
using BharatKaSafar.UI;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Builds the shared Mission Select v2 scene (Scenes/BKS_MissionSelect.unity) with every element
    /// as a normal, editable Canvas object, and adds it to Build Settings.
    /// Menu: Bharat Ka Safar > UI > Build Mission Select Scene
    /// </summary>
    public static class BKS_MissionSelectSceneBuilder
    {
        private const string ScenePath = "Assets/BharatKaSafar/Scenes/BKS_MissionSelect.unity";
        private const string Art = "Assets/BharatKaSafar/Art/Missions/";
        private static readonly Color Teal = new Color(0.05f, 0.36f, 0.38f);
        private static readonly Color Saffron = new Color(0.95f, 0.55f, 0.12f);

        [MenuItem("Bharat Ka Safar/UI/Build Mission Select Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (System.IO.File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Mission Select", "BKS_MissionSelect already exists. Rebuild it? (edits made in that scene will be lost)", "Rebuild", "Cancel"))
                return;

            BKS_MissionPlannerImporter.SetSprites(Art + "UI", 2048);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            cam.tag = "MainCamera"; cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            new GameObject("BKS_PersistentManagers").AddComponent<BKS_PersistentManagers>();

            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.layer = LayerMask.NameToLayer("UI");
            canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var canvas = canvasGO.transform;

            // Background + white veil
            var bg = Img("Background", canvas, Vector2.zero, Vector2.one, null, Color.white);
            bg.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            bg.GetComponent<AspectRatioFitter>().aspectRatio = 16f / 9f;
            bg.sprite = Sprite(Art + "StateBackgrounds/26-uttar-pradesh.png");
            var veil = Img("WhiteVeil", canvas, Vector2.zero, Vector2.one, null, new Color(1f, 1f, 1f, 0.6f));

            // Top bar: title + back button (top-right)
            var title = Txt("Title", canvas, "Chapter : Uttar Pradesh", 64, Teal, FontStyles.Bold, new Vector2(0.05f, 0.85f), new Vector2(0.8f, 0.97f));
            title.alignment = TextAlignmentOptions.Left;
            title.outlineWidth = 0.15f; title.outlineColor = Color.white;
            var back = Img("BackButton", canvas, new Vector2(0.88f, 0.86f), new Vector2(0.97f, 0.97f), Sprite("Assets/BharatKaSafar/Art/UI/Settings/BKS_BackButton.png"), Color.white);
            back.preserveAspect = true;
            var backBtn = back.gameObject.AddComponent<Button>();

            // Scroll list of cards
            var scrollRT = Rect("MissionScroll", canvas, new Vector2(0f, 0.17f), new Vector2(1f, 0.84f));
            var scroll = scrollRT.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = true; scroll.vertical = false; scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.decelerationRate = 0.12f; scroll.scrollSensitivity = 30f;
            var viewport = Rect("Viewport", scrollRT, Vector2.zero, Vector2.one);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.001f); // catches drags between cards
            var content = Rect("Content", viewport, new Vector2(0f, 0f), new Vector2(0f, 1f));
            content.pivot = new Vector2(0f, 0.5f);
            var row = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 110; row.padding = new RectOffset(700, 700, 0, 0);
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = false;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content;

            var template = BuildCard(content);

            // Enter button
            var enterImg = Img("EnterButton", canvas, new Vector2(0.4f, 0.04f), new Vector2(0.6f, 0.14f), null, Saffron);
            var enterBtn = enterImg.gameObject.AddComponent<Button>();
            Txt("Label", enterImg.transform, "ENTER", 54, Color.white, FontStyles.Bold, Vector2.zero, Vector2.one);

            var briefing = BuildBriefing(canvas);

            var screen = canvasGO.AddComponent<BKS_MissionSelectScreen>();
            var so = new SerializedObject(screen);
            so.FindProperty("testChapter").objectReferenceValue = AssetDatabase.FindAssets("t:ChapterData")
                .Select(g => AssetDatabase.LoadAssetAtPath<ChapterData>(AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault(c => c.ChapterID == "TN");
            so.FindProperty("backgroundImage").objectReferenceValue = bg;
            so.FindProperty("whiteVeil").objectReferenceValue = veil;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("backButton").objectReferenceValue = backBtn;
            so.FindProperty("scrollRect").objectReferenceValue = scroll;
            so.FindProperty("cardContainer").objectReferenceValue = content;
            so.FindProperty("cardTemplate").objectReferenceValue = template;
            so.FindProperty("enterButton").objectReferenceValue = enterBtn;
            so.FindProperty("briefing").objectReferenceValue = briefing;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath)) { scenes.Add(new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = scenes.ToArray(); }
            Debug.Log("[Mission Select] Built " + ScenePath + " and added it to Build Settings. Test Chapter = Tamil Nadu (used only when playing this scene directly).");
        }

        private static BKS_MissionCardView BuildCard(Transform content)
        {
            var card = Rect("CardTemplate", content, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            card.sizeDelta = new Vector2(450, 600);

            var glow = Img("SelectedGlow", card, Vector2.zero, Vector2.one, null, new Color(1f, 0.8f, 0.2f, 0.9f));
            glow.rectTransform.offsetMin = new Vector2(-14, -14); glow.rectTransform.offsetMax = new Vector2(14, 14);
            glow.raycastTarget = false;

            var art = Img("CardArt", card, Vector2.zero, Vector2.one, Sprite(Art + "Cards/001-agra.png"), Color.white);
            art.preserveAspect = true;
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = art;

            // Gold stars sit exactly on the 3 empty stars printed on the card art (x 25% / 50% / 75%, y 10%)
            var stars = new Image[3];
            float[] xs = { 0.249f, 0.5f, 0.75f };
            for (int i = 0; i < 3; i++)
            {
                const float half = 0.075f;
                stars[i] = Img("Star" + (i + 1), card, new Vector2(xs[i] - half, 0.1f - half * 0.75f), new Vector2(xs[i] + half, 0.1f + half * 0.75f),
                               Sprite(Art + "UI/star-filled.png"), Color.white);
                stars[i].preserveAspect = true; stars[i].raycastTarget = false;
            }

            var locked = Img("LockOverlay", card, Vector2.zero, Vector2.one, null, new Color(0f, 0f, 0f, 0.55f));
            locked.raycastTarget = false;
            var lockIcon = Img("LockIcon", locked.transform, new Vector2(0.22f, 0.33f), new Vector2(0.78f, 0.67f), Sprite(Art + "UI/lock.png"), Color.white);
            lockIcon.preserveAspect = true; lockIcon.raycastTarget = false;

            var soon = Img("ComingSoon", card, new Vector2(0f, 0.42f), new Vector2(1f, 0.56f), null, new Color(0.75f, 0.2f, 0.12f, 0.92f));
            soon.raycastTarget = false;
            Txt("Label", soon.transform, "COMING SOON", 40, Color.white, FontStyles.Bold, Vector2.zero, Vector2.one);

            var fallback = Txt("FallbackTitle", card, "Place", 40, Teal, FontStyles.Bold, new Vector2(0.05f, 0.8f), new Vector2(0.95f, 0.97f));

            var view = card.gameObject.AddComponent<BKS_MissionCardView>();
            var so = new SerializedObject(view);
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("cardImage").objectReferenceValue = art;
            var arr = so.FindProperty("starFills");
            arr.arraySize = 3;
            for (int i = 0; i < 3; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = stars[i];
            so.FindProperty("lockOverlay").objectReferenceValue = locked.gameObject;
            so.FindProperty("comingSoonOverlay").objectReferenceValue = soon.gameObject;
            so.FindProperty("selectedHighlight").objectReferenceValue = glow.gameObject;
            so.FindProperty("fallbackTitle").objectReferenceValue = fallback;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static BKS_MissionBriefingPopup BuildBriefing(Transform canvas)
        {
            var holder = Rect("MissionBriefing", canvas, Vector2.zero, Vector2.one);
            var root = Img("BriefingRoot", holder, Vector2.zero, Vector2.one, null, new Color(0f, 0f, 0f, 0.8f));
            var panel = Img("Panel", root.transform, new Vector2(0.12f, 0.1f), new Vector2(0.88f, 0.9f), Sprite(Art + "UI/briefing-panel.png"), Color.white);
            panel.preserveAspect = true;
            // keep the rect exactly the size of the artwork so child anchors line up with the parchment
            var fit = panel.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = panel.sprite != null ? panel.sprite.rect.width / panel.sprite.rect.height : 2f;
            var p = panel.transform;

            // text sits on the clear parchment between the compass (left) and stamps (right)
            var titleT = Txt("Title", p, "Mission", 60, Teal, FontStyles.Bold, new Vector2(0.3f, 0.76f), new Vector2(0.8f, 0.86f));
            var placeT = Txt("Place", p, "Place, State", 34, Saffron, FontStyles.Bold, new Vector2(0.3f, 0.7f), new Vector2(0.8f, 0.76f));
            var objT = Txt("Objective", p, "Objective", 34, new Color(0.15f, 0.12f, 0.08f), FontStyles.Normal, new Vector2(0.3f, 0.58f), new Vector2(0.8f, 0.7f));
            var colIcon = Img("CollectibleIcon", p, new Vector2(0.31f, 0.5f), new Vector2(0.35f, 0.58f), null, Color.white);
            colIcon.preserveAspect = true;
            var colT = Txt("Collectible", p, "Collect", 32, Teal, FontStyles.Bold, new Vector2(0.36f, 0.5f), new Vector2(0.8f, 0.58f));
            colT.alignment = TextAlignmentOptions.Left;
            var chT = Txt("Challenges", p, "Challenges", 36, new Color(0.25f, 0.18f, 0.1f), FontStyles.Normal, new Vector2(0.31f, 0.32f), new Vector2(0.8f, 0.5f));
            chT.alignment = TextAlignmentOptions.TopLeft;
            var tokIcon = Img("MemoryTokenIcon", p, new Vector2(0.31f, 0.22f), new Vector2(0.35f, 0.31f), null, Color.white);
            tokIcon.preserveAspect = true;
            var rwT = Txt("Rewards", p, "Rewards", 34, Teal, FontStyles.Bold, new Vector2(0.36f, 0.22f), new Vector2(0.8f, 0.31f));
            rwT.alignment = TextAlignmentOptions.Left;
            rwT.textWrappingMode = TextWrappingModes.NoWrap; // one line; auto-size shrinks it to fit

            var startImg = Img("StartButton", p, new Vector2(0.44f, 0.09f), new Vector2(0.62f, 0.2f), null, Saffron);
            var startBtn = startImg.gameObject.AddComponent<Button>();
            var startLbl = Txt("Label", startImg.transform, "START", 44, Color.white, FontStyles.Bold, Vector2.zero, Vector2.one);
            // Vehicle chooser next to START: ‹ [icon + name] ›
            var pickerRT = Rect("VehiclePicker", p, new Vector2(0.64f, 0.07f), new Vector2(0.86f, 0.21f));
            var pickIcon = Img("Icon", pickerRT, new Vector2(0.18f, 0.3f), new Vector2(0.82f, 1f), null, Color.white);
            pickIcon.preserveAspect = true;
            var pickName = Txt("Name", pickerRT, "Safar Jeep", 26, Teal, FontStyles.Bold, new Vector2(0.1f, 0f), new Vector2(0.9f, 0.3f));
            var prevImg = Img("Prev", pickerRT, new Vector2(0f, 0.35f), new Vector2(0.15f, 0.85f), null, Teal);
            var prevBtn = prevImg.gameObject.AddComponent<Button>();
            Txt("Label", prevImg.transform, "<", 40, Color.white, FontStyles.Bold, Vector2.zero, Vector2.one);
            var nextImg = Img("Next", pickerRT, new Vector2(0.85f, 0.35f), new Vector2(1f, 0.85f), null, Teal);
            var nextBtn = nextImg.gameObject.AddComponent<Button>();
            Txt("Label", nextImg.transform, ">", 40, Color.white, FontStyles.Bold, Vector2.zero, Vector2.one);
            var picker = pickerRT.gameObject.AddComponent<Store.BKS_VehiclePicker>();
            var pso = new SerializedObject(picker);
            pso.FindProperty("icon").objectReferenceValue = pickIcon;
            pso.FindProperty("nameText").objectReferenceValue = pickName;
            pso.FindProperty("prevButton").objectReferenceValue = prevBtn;
            pso.FindProperty("nextButton").objectReferenceValue = nextBtn;
            pso.ApplyModifiedPropertiesWithoutUndo();

            var closeImg = Img("CloseButton", p, new Vector2(0.88f, 0.8f), new Vector2(0.93f, 0.9f), null, new Color(0.75f, 0.2f, 0.12f));
            var closeBtn = closeImg.gameObject.AddComponent<Button>();
            Txt("X", closeImg.transform, "X", 44, Color.white, FontStyles.Bold, Vector2.zero, Vector2.one);

            var popup = holder.gameObject.AddComponent<BKS_MissionBriefingPopup>();
            var so = new SerializedObject(popup);
            so.FindProperty("root").objectReferenceValue = root.gameObject;
            so.FindProperty("panel").objectReferenceValue = panel.rectTransform;
            so.FindProperty("titleText").objectReferenceValue = titleT;
            so.FindProperty("placeText").objectReferenceValue = placeT;
            so.FindProperty("objectiveText").objectReferenceValue = objT;
            so.FindProperty("collectibleText").objectReferenceValue = colT;
            so.FindProperty("challengesText").objectReferenceValue = chT;
            so.FindProperty("rewardsText").objectReferenceValue = rwT;
            so.FindProperty("collectibleIcon").objectReferenceValue = colIcon;
            so.FindProperty("memoryTokenIcon").objectReferenceValue = tokIcon;
            so.FindProperty("startButton").objectReferenceValue = startBtn;
            so.FindProperty("startButtonLabel").objectReferenceValue = startLbl;
            so.FindProperty("closeButton").objectReferenceValue = closeBtn;
            so.ApplyModifiedPropertiesWithoutUndo();
            root.gameObject.SetActive(false);
            return popup;
        }

        // ── helpers ──────────────────────────────────────────────────────────
        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static Image Img(string name, Transform parent, Vector2 min, Vector2 max, Sprite sprite, Color color)
        {
            var img = Rect(name, parent, min, max).gameObject.AddComponent<Image>();
            img.sprite = sprite; img.color = color;
            return img;
        }

        private static TMP_Text Txt(string name, Transform parent, string text, float size, Color color, FontStyles style, Vector2 min, Vector2 max)
        {
            var t = Rect(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text; t.color = color; t.fontStyle = style; t.alignment = TextAlignmentOptions.Center;
            t.enableAutoSizing = true; t.fontSizeMax = size; t.fontSizeMin = 12; t.raycastTarget = false;
            return t;
        }

        private static Sprite Sprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
