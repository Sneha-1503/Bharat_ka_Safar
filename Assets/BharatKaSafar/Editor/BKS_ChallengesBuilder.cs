using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BharatKaSafar.Challenges;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Challenges setup:
    ///  1. Create Challenge Catalog → Resources/BKS_ChallengeCatalog.asset (edit titles, targets, rewards there)
    ///  2. Build Challenges Scene  → Scenes/BKS_Challenges.unity (editable Canvas) + Build Settings
    /// Home's CHALLENGES button opens it.
    /// </summary>
    public static class BKS_ChallengesBuilder
    {
        private const string CatalogPath = "Assets/Resources/BKS_ChallengeCatalog.asset";
        private const string ScenePath = "Assets/BharatKaSafar/Scenes/BKS_Challenges.unity";
        private static readonly Color Teal = new Color(0.05f, 0.36f, 0.38f);
        private static readonly Color Saffron = new Color(0.95f, 0.55f, 0.12f);

        [MenuItem("Bharat Ka Safar/Challenges/1. Create Challenge Catalog")]
        public static void CreateCatalog()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            var cat = AssetDatabase.LoadAssetAtPath<BKS_ChallengeCatalog>(CatalogPath);
            if (cat == null) { cat = ScriptableObject.CreateInstance<BKS_ChallengeCatalog>(); AssetDatabase.CreateAsset(cat, CatalogPath); }
            AssetDatabase.SaveAssets();
            Selection.activeObject = cat;
            Debug.Log($"[Challenges] Catalog at {CatalogPath}: {cat.dailyPool.Count} daily, {cat.weeklyPool.Count} weekly, {cat.overall.Count} overall.");
        }

        [MenuItem("Bharat Ka Safar/Challenges/2. Build Challenges Scene")]
        public static void BuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Challenges", "BKS_Challenges exists. Rebuild it? (edits in that scene are lost)", "Rebuild", "Cancel")) return;
            if (AssetDatabase.LoadAssetAtPath<BKS_ChallengeCatalog>(CatalogPath) == null) CreateCatalog();

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
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            canvasGO.AddComponent<BharatKaSafar.UI.BKS_SafeAreaEdges>();
            var c = canvasGO.transform;

            var bg = Img("Background", c, Vector2.zero, Vector2.one, Sp("Assets/BharatKaSafar/Art/Missions/StateBackgrounds/21-rajasthan.png"), Color.white);
            var fit = bg.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = 16f / 9f;
            Img("WhiteVeil", c, Vector2.zero, Vector2.one, null, new Color(1, 1, 1, 0.6f));

            var title = Txt("Title", c, "CHALLENGES", 72, Teal, FontStyles.Bold, new Vector2(0.03f, 0.87f), new Vector2(0.45f, 0.98f));
            title.alignment = TextAlignmentOptions.Left;
            var coinBox = Img("CoinBox", c, new Vector2(0.66f, 0.885f), new Vector2(0.85f, 0.965f), null, Teal);
            var coins = Txt("Coins", coinBox.transform, "0", 40, Color.white, FontStyles.Bold, Vector2.zero, Vector2.one);
            var back = Img("BackButton", c, new Vector2(0.88f, 0.87f), new Vector2(0.97f, 0.98f), Sp("Assets/BharatKaSafar/Art/UI/Settings/BKS_BackButton.png"), Color.white);
            back.preserveAspect = true;
            var backBtn = back.gameObject.AddComponent<Button>();

            string[] names = { "DAILY", "WEEKLY", "OVERALL" };
            var tabButtons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                var tb = Img(names[i] + "Tab", c, new Vector2(0.03f + i * 0.15f, 0.77f), new Vector2(0.17f + i * 0.15f, 0.85f), null, Teal);
                tabButtons[i] = tb.gameObject.AddComponent<Button>();
                Txt("Label", tb.transform, names[i], 36, Color.white, FontStyles.Bold, Vector2.zero, Vector2.one);
            }
            var reset = Txt("ResetTimer", c, "Resets in 00:00:00", 34, Saffron, FontStyles.Bold, new Vector2(0.6f, 0.77f), new Vector2(0.97f, 0.85f));
            reset.alignment = TextAlignmentOptions.Right;

            // vertical list
            var panel = Img("ListPanel", c, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.76f), null, new Color(0.98f, 0.93f, 0.8f, 0.96f));
            var scrollRT = Rect("Scroll", panel.transform, new Vector2(0.01f, 0.02f), new Vector2(0.99f, 0.98f));
            var scroll = scrollRT.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 30f;
            var viewport = Rect("Viewport", scrollRT, Vector2.zero, Vector2.one);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.001f);
            var content = Rect("Content", viewport, new Vector2(0f, 1f), Vector2.one);
            content.pivot = new Vector2(0.5f, 1f);
            var col = content.gameObject.AddComponent<VerticalLayoutGroup>();
            col.spacing = 18; col.padding = new RectOffset(20, 20, 20, 20);
            col.childControlWidth = true; col.childControlHeight = false;
            col.childForceExpandWidth = true; col.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content;

            var row = BuildRow(content);

            var screen = canvasGO.AddComponent<BKS_ChallengesScreen>();
            var so = new SerializedObject(screen);
            var tabsProp = so.FindProperty("tabs");
            tabsProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                var t = tabsProp.GetArrayElementAtIndex(i);
                t.FindPropertyRelative("button").objectReferenceValue = tabButtons[i];
                t.FindPropertyRelative("period").enumValueIndex = i;
            }
            so.FindProperty("listContent").objectReferenceValue = content;
            so.FindProperty("rowTemplate").objectReferenceValue = row;
            so.FindProperty("resetText").objectReferenceValue = reset;
            so.FindProperty("coinsText").objectReferenceValue = coins;
            so.FindProperty("backButton").objectReferenceValue = backBtn;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath)) { scenes.Add(new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = scenes.ToArray(); }
            Debug.Log("[Challenges] Built " + ScenePath + " and added it to Build Settings.");
        }

        private static BKS_ChallengeRowView BuildRow(Transform content)
        {
            var row = Img("RowTemplate", content, new Vector2(0f, 1f), new Vector2(1f, 1f), null, Color.white);
            row.rectTransform.sizeDelta = new Vector2(0, 140);
            var outline = row.gameObject.AddComponent<Outline>(); outline.effectColor = Teal; outline.effectDistance = new Vector2(3, -3);
            var title = Txt("Title", row.transform, "Challenge", 38, Teal, FontStyles.Bold, new Vector2(0.02f, 0.52f), new Vector2(0.6f, 0.95f));
            title.alignment = TextAlignmentOptions.Left;
            var barBg = Img("ProgressBar", row.transform, new Vector2(0.02f, 0.14f), new Vector2(0.45f, 0.4f), null, new Color(0f, 0f, 0f, 0.15f));
            var fill = Img("Fill", barBg.transform, Vector2.zero, Vector2.one, null, Saffron);
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = 0.4f;
            fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); // Filled needs a sprite
            var prog = Txt("Progress", barBg.transform, "0 / 0", 28, Color.black, FontStyles.Bold, Vector2.zero, Vector2.one);
            var reward = Txt("Reward", row.transform, "100 Coins", 32, new Color(0.35f, 0.25f, 0.1f), FontStyles.Bold, new Vector2(0.47f, 0.1f), new Vector2(0.78f, 0.5f));
            reward.alignment = TextAlignmentOptions.Left;
            var btnImg = Img("ClaimButton", row.transform, new Vector2(0.8f, 0.18f), new Vector2(0.98f, 0.82f), null, Saffron);
            var btn = btnImg.gameObject.AddComponent<Button>();
            var lbl = Txt("Label", btnImg.transform, "CLAIM", 34, Color.white, FontStyles.Bold, Vector2.zero, Vector2.one);

            var view = row.gameObject.AddComponent<BKS_ChallengeRowView>();
            var so = new SerializedObject(view);
            so.FindProperty("title").objectReferenceValue = title;
            so.FindProperty("progressText").objectReferenceValue = prog;
            so.FindProperty("progressFill").objectReferenceValue = fill;
            so.FindProperty("rewardText").objectReferenceValue = reward;
            so.FindProperty("claimButton").objectReferenceValue = btn;
            so.FindProperty("claimLabel").objectReferenceValue = lbl;
            so.FindProperty("background").objectReferenceValue = row;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
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

        private static Sprite Sp(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
