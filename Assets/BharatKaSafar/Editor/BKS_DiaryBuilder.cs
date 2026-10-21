using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BharatKaSafar.Core;
using BharatKaSafar.Diary;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Safar Diary setup:
    ///  1. Build Chapter Registry → Resources/BKS_ChapterRegistry.asset (all chapters in the Excel order)
    ///  2. Build Safar Diary Scene → Scenes/BKS_SafarDiary.unity (editable Canvas) + Build Settings
    /// Home's COLLECTION button opens the Diary.
    /// </summary>
    public static class BKS_DiaryBuilder
    {
        private const string RegistryPath = "Assets/Resources/BKS_ChapterRegistry.asset";
        private const string ScenePath = "Assets/BharatKaSafar/Scenes/BKS_SafarDiary.unity";
        private static readonly Color Teal = new Color(0.05f, 0.36f, 0.38f);
        private static readonly Color Saffron = new Color(0.95f, 0.55f, 0.12f);
        private static readonly Color Parchment = new Color(0.98f, 0.93f, 0.8f, 0.96f);

        [MenuItem("Bharat Ka Safar/Diary/1. Build Chapter Registry")]
        public static void BuildRegistry()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            var reg = AssetDatabase.LoadAssetAtPath<BKS_ChapterRegistry>(RegistryPath);
            if (reg == null) { reg = ScriptableObject.CreateInstance<BKS_ChapterRegistry>(); AssetDatabase.CreateAsset(reg, RegistryPath); }

            // Journey order = order of states in the mission sheet
            var order = new List<string>();
            if (File.Exists(BKS_MissionPlannerImporter.CsvPath))
                foreach (var line in File.ReadAllLines(BKS_MissionPlannerImporter.CsvPath).Skip(1))
                {
                    var cells = line.Split(',');
                    if (cells.Length > 1 && cells[0].All(char.IsDigit) && !order.Contains(cells[1])) order.Add(cells[1]);
                }
            var chapters = AssetDatabase.FindAssets("t:ChapterData")
                .Select(g => AssetDatabase.LoadAssetAtPath<ChapterData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(c => c != null && c.MissionCount > 0)
                .OrderBy(c => { int i = order.IndexOf(c.StateName); return i < 0 ? 999 : i; })
                .ToList();
            reg.chapters = chapters;
            EditorUtility.SetDirty(reg);
            AssetDatabase.SaveAssets();
            Selection.activeObject = reg;
            Debug.Log($"[Diary] Registry has {chapters.Count} chapters, {chapters.Sum(c => c.MissionCount)} memories.");
        }

        [MenuItem("Bharat Ka Safar/Diary/2. Build Safar Diary Scene")]
        public static void BuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Safar Diary", "BKS_SafarDiary exists. Rebuild it? (edits in that scene are lost)", "Rebuild", "Cancel")) return;
            var reg = AssetDatabase.LoadAssetAtPath<BKS_ChapterRegistry>(RegistryPath);
            if (reg == null) { BuildRegistry(); reg = AssetDatabase.LoadAssetAtPath<BKS_ChapterRegistry>(RegistryPath); }

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

            var bg = Img("Background", c, Vector2.zero, Vector2.one, Sp("Assets/BharatKaSafar/Art/Missions/StateBackgrounds/23-tamil-nadu.png"), Color.white);
            var fit = bg.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = 16f / 9f;
            Img("WhiteVeil", c, Vector2.zero, Vector2.one, null, new Color(1, 1, 1, 0.6f));

            var title = Txt("Title", c, "SAFAR DIARY", 72, Teal, FontStyles.Bold, new Vector2(0.03f, 0.87f), new Vector2(0.45f, 0.98f));
            title.alignment = TextAlignmentOptions.Left;
            var total = Txt("Total", c, "0 / 140 memories", 38, Saffron, FontStyles.Bold, new Vector2(0.5f, 0.88f), new Vector2(0.85f, 0.97f));
            total.alignment = TextAlignmentOptions.Right;
            var back = Img("BackButton", c, new Vector2(0.88f, 0.87f), new Vector2(0.97f, 0.98f), Sp("Assets/BharatKaSafar/Art/UI/Settings/BKS_BackButton.png"), Color.white);
            back.preserveAspect = true;
            var backBtn = back.gameObject.AddComponent<Button>();

            // left: states
            var left = Img("StatesPanel", c, new Vector2(0.03f, 0.04f), new Vector2(0.27f, 0.85f), null, Parchment);
            var leftContent = Scroll(left.transform, vertical: true, out _);
            var col = leftContent.gameObject.AddComponent<VerticalLayoutGroup>();
            col.spacing = 10; col.padding = new RectOffset(12, 12, 12, 12);
            col.childControlWidth = true; col.childControlHeight = false; col.childForceExpandWidth = true; col.childForceExpandHeight = false;
            leftContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var chapterTemplate = BuildChapterButton(leftContent);

            // right: tokens of the selected state
            var right = Img("TokensPanel", c, new Vector2(0.29f, 0.04f), new Vector2(0.97f, 0.85f), null, Parchment);
            var chapterTitle = Txt("ChapterTitle", right.transform, "Tamil Nadu", 54, Teal, FontStyles.Bold, new Vector2(0.03f, 0.86f), new Vector2(0.97f, 0.98f));
            var gridRT = Rect("Grid", right.transform, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.85f));
            var grid = gridRT.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(330, 400); grid.spacing = new Vector2(30, 30);
            grid.childAlignment = TextAnchor.MiddleCenter; grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3;
            var tokenTemplate = BuildToken(gridRT);

            var detail = BuildDetail(c);

            var screen = canvasGO.AddComponent<BKS_DiaryScreen>();
            var so = new SerializedObject(screen);
            so.FindProperty("registry").objectReferenceValue = reg;
            so.FindProperty("chapterList").objectReferenceValue = leftContent;
            so.FindProperty("chapterTemplate").objectReferenceValue = chapterTemplate;
            so.FindProperty("tokenGrid").objectReferenceValue = gridRT;
            so.FindProperty("tokenTemplate").objectReferenceValue = tokenTemplate;
            so.FindProperty("detail").objectReferenceValue = detail;
            so.FindProperty("totalText").objectReferenceValue = total;
            so.FindProperty("chapterTitle").objectReferenceValue = chapterTitle;
            so.FindProperty("backButton").objectReferenceValue = backBtn;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath)) { scenes.Add(new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = scenes.ToArray(); }
            Debug.Log("[Diary] Built " + ScenePath + " and added it to Build Settings.");
        }

        private static BKS_DiaryChapterButton BuildChapterButton(Transform parent)
        {
            var bg = Img("ChapterTemplate", parent, new Vector2(0f, 1f), new Vector2(1f, 1f), null, Teal);
            bg.rectTransform.sizeDelta = new Vector2(0, 90);
            var btn = bg.gameObject.AddComponent<Button>();
            var name = Txt("Name", bg.transform, "State", 32, Color.white, FontStyles.Bold, new Vector2(0.05f, 0f), new Vector2(0.72f, 1f));
            name.alignment = TextAlignmentOptions.Left;
            var count = Txt("Count", bg.transform, "0/5", 30, Color.white, FontStyles.Bold, new Vector2(0.72f, 0f), new Vector2(0.96f, 1f));
            count.alignment = TextAlignmentOptions.Right;
            var view = bg.gameObject.AddComponent<BKS_DiaryChapterButton>();
            var so = new SerializedObject(view);
            so.FindProperty("button").objectReferenceValue = btn;
            so.FindProperty("background").objectReferenceValue = bg;
            so.FindProperty("nameText").objectReferenceValue = name;
            so.FindProperty("countText").objectReferenceValue = count;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static BKS_DiaryTokenView BuildToken(Transform parent)
        {
            var card = Img("TokenTemplate", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), null, new Color(1, 1, 1, 0.85f));
            var btn = card.gameObject.AddComponent<Button>();
            var token = Img("Token", card.transform, new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.95f), null, Color.white);
            token.preserveAspect = true;
            var lockMark = Txt("Locked", card.transform, "?", 140, Color.white, FontStyles.Bold, new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.95f));
            var name = Txt("Name", card.transform, "Memory", 30, Teal, FontStyles.Bold, new Vector2(0.04f, 0.15f), new Vector2(0.96f, 0.3f));
            var place = Txt("Place", card.transform, "Place", 24, new Color(0.35f, 0.25f, 0.1f), FontStyles.Normal, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.15f));
            var view = card.gameObject.AddComponent<BKS_DiaryTokenView>();
            var so = new SerializedObject(view);
            so.FindProperty("button").objectReferenceValue = btn;
            so.FindProperty("tokenImage").objectReferenceValue = token;
            so.FindProperty("nameText").objectReferenceValue = name;
            so.FindProperty("placeText").objectReferenceValue = place;
            so.FindProperty("lockedMark").objectReferenceValue = lockMark.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static BKS_DiaryDetailPopup BuildDetail(Transform canvas)
        {
            var holder = Rect("MemoryDetail", canvas, Vector2.zero, Vector2.one);
            var root = Img("DetailRoot", holder, Vector2.zero, Vector2.one, null, new Color(0f, 0f, 0f, 0.7f));
            var box = Img("Box", root.transform, new Vector2(0.18f, 0.14f), new Vector2(0.82f, 0.86f), null, Parchment);
            var token = Img("Token", box.transform, new Vector2(0.04f, 0.2f), new Vector2(0.4f, 0.85f), null, Color.white);
            token.preserveAspect = true;
            var title = Txt("Title", box.transform, "Memory", 56, Teal, FontStyles.Bold, new Vector2(0.43f, 0.78f), new Vector2(0.96f, 0.92f));
            title.alignment = TextAlignmentOptions.Left;
            var place = Txt("Place", box.transform, "Place, State", 34, Saffron, FontStyles.Bold, new Vector2(0.43f, 0.7f), new Vector2(0.96f, 0.78f));
            place.alignment = TextAlignmentOptions.Left;
            var stars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                stars[i] = Img("Star" + (i + 1), box.transform, new Vector2(0.43f + i * 0.07f, 0.6f), new Vector2(0.49f + i * 0.07f, 0.69f),
                               Sp("Assets/BharatKaSafar/Art/Missions/UI/star-filled.png"), Color.white);
                stars[i].preserveAspect = true;
            }
            var body = Txt("Body", box.transform, "Story", 34, new Color(0.2f, 0.15f, 0.1f), FontStyles.Normal, new Vector2(0.43f, 0.08f), new Vector2(0.96f, 0.58f));
            body.alignment = TextAlignmentOptions.TopLeft;
            var close = Img("CloseButton", box.transform, new Vector2(0.92f, 0.9f), new Vector2(0.99f, 0.99f), null, new Color(0.75f, 0.2f, 0.12f));
            var closeBtn = close.gameObject.AddComponent<Button>();
            Txt("X", close.transform, "X", 40, Color.white, FontStyles.Bold, Vector2.zero, Vector2.one);

            var popup = holder.gameObject.AddComponent<BKS_DiaryDetailPopup>();
            var so = new SerializedObject(popup);
            so.FindProperty("root").objectReferenceValue = root.gameObject;
            so.FindProperty("tokenImage").objectReferenceValue = token;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("placeText").objectReferenceValue = place;
            so.FindProperty("bodyText").objectReferenceValue = body;
            var arr = so.FindProperty("stars"); arr.arraySize = 3;
            for (int i = 0; i < 3; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = stars[i];
            so.FindProperty("closeButton").objectReferenceValue = closeBtn;
            so.ApplyModifiedPropertiesWithoutUndo();
            root.gameObject.SetActive(false);
            return popup;
        }

        private static RectTransform Scroll(Transform parent, bool vertical, out ScrollRect scroll)
        {
            var scrollRT = Rect("Scroll", parent, new Vector2(0.01f, 0.01f), new Vector2(0.99f, 0.99f));
            scroll = scrollRT.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = !vertical; scroll.vertical = vertical; scroll.scrollSensitivity = 30f;
            var viewport = Rect("Viewport", scrollRT, Vector2.zero, Vector2.one);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.001f);
            var content = Rect("Content", viewport, new Vector2(0f, 1f), Vector2.one);
            content.pivot = new Vector2(0.5f, 1f);
            scroll.viewport = viewport; scroll.content = content;
            return content;
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
