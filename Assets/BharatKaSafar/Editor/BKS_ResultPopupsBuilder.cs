using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.UI;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Builds two separate result popups (TealTravel parchment, own canvas on top of everything, sorting 32000):
    ///  • Resources/BKS_MissionCompletePopup — title, mission name, 3 stars (pop in), coins + Safar Stars, INDIA MAP / RETRY / CONTINUE
    ///  • Resources/BKS_MissionFailPopup     — title, fail reason, mission name, coins collected, INDIA MAP / RETRY
    /// Every mission picks the right one (BKS_MissionResult). After building, the prefabs are yours to edit; nothing is
    /// resized at runtime. Running the menu again rebuilds them (your edits to them are replaced).
    /// Menu: Bharat Ka Safar > UI > Build Result Popups (Complete + Fail)
    /// </summary>
    public static class BKS_ResultPopupsBuilder
    {
        private const string Engraved = "Assets/BharatKaSafar/Art/UI/TealTravelSprites/Engraved/";
        private const string Home = "Assets/BharatKaSafar/BKS sprite/home/";
        private const string FontPath = "Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Electronic Highway Sign SDF.asset";
        public const string CompletePath = "Assets/Resources/BKS_MissionCompletePopup.prefab";
        public const string FailPath = "Assets/Resources/BKS_MissionFailPopup.prefab";

        private static readonly Color Ink = new Color(0.32f, 0.18f, 0.05f);
        private static readonly Color Reason = new Color(0.62f, 0.16f, 0.08f);
        private static TMP_FontAsset font;

        [MenuItem("Bharat Ka Safar/UI/Build Result Popups (Complete + Fail)")]
        public static void BuildBoth()
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Build(true);
            Build(false);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ResultPopups] Built {CompletePath} and {FailPath}. Edit them in Prefab mode as you like.");
        }

        private static void Build(bool complete)
        {
            var root = new GameObject(complete ? "BKS_MissionCompletePopup" : "BKS_MissionFailPopup", typeof(RectTransform));
            try
            {
                var canvas = root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32000;
                var scaler = root.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = 1f;
                root.AddComponent<GraphicRaycaster>();

                var dim = Stretch(Node("Dim", root.transform)).gameObject.AddComponent<Image>();
                dim.color = new Color(0f, 0f, 0f, 0.6f);

                var panel = Node("ResultPanel", root.transform);
                panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f); panel.sizeDelta = new Vector2(1140f, 760f);
                var frame = panel.gameObject.AddComponent<Image>();
                frame.sprite = Load(Engraved + "Teal and Gold Adventure Parchment Frame.png"); frame.preserveAspect = true; frame.raycastTarget = false;

                var title = Text("ResultStatusText", panel, complete ? "MISSION COMPLETE!" : "MISSION FAILED", 0.29f, 0.715f, 0.71f, 0.865f, Color.white, 48f);
                TMP_Text reason = null, missionName;
                Image[] stars = new Image[0];
                TMP_Text coins, safarStars = null;
                Button cont = null, retry, map;

                if (complete)
                {
                    missionName = Text("MissionNameText", panel, "Mission Name", 0.2f, 0.60f, 0.8f, 0.68f, Ink, 40f);
                    stars = new Image[3];
                    for (int i = 0; i < 3; i++) stars[i] = Icon("Star" + (i + 1), panel, Home + "star.png", new Vector2(0.38f + 0.12f * i, 0.49f), 112f);
                    Icon("CoinIcon", panel, Home + "rupee.png", new Vector2(0.33f, 0.335f), 66f);
                    coins = Text("CoinsEarnedText", panel, "+0", 0.365f, 0.30f, 0.50f, 0.37f, Ink, 40f, TextAlignmentOptions.MidlineLeft);
                    Icon("SafarStarIcon", panel, Home + "star.png", new Vector2(0.58f, 0.335f), 62f);
                    safarStars = Text("SafarStarsEarnedText", panel, "+0", 0.615f, 0.30f, 0.75f, 0.37f, Ink, 40f, TextAlignmentOptions.MidlineLeft);
                    map = Btn("MapButton", panel, "india-map.png", new Vector2(0.28f, 0.155f), new Vector2(250f, 100f));
                    retry = Btn("RetryButton", panel, "retry.png", new Vector2(0.50f, 0.155f), new Vector2(250f, 100f));
                    cont = Btn("ContinueButton", panel, "continue.png", new Vector2(0.72f, 0.155f), new Vector2(290f, 116f)); // main action
                }
                else
                {
                    reason = Text("FailReasonText", panel, "Driver crashed", 0.2f, 0.60f, 0.8f, 0.68f, Reason, 40f);
                    missionName = Text("MissionNameText", panel, "Mission Name", 0.2f, 0.50f, 0.8f, 0.58f, Ink, 34f);
                    Icon("CoinIcon", panel, Home + "rupee.png", new Vector2(0.39f, 0.365f), 66f);
                    coins = Text("CoinsEarnedText", panel, "+0", 0.425f, 0.33f, 0.68f, 0.40f, Ink, 40f, TextAlignmentOptions.MidlineLeft);
                    map = Btn("MapButton", panel, "india-map.png", new Vector2(0.35f, 0.17f), new Vector2(260f, 104f));
                    retry = Btn("RetryButton", panel, "retry.png", new Vector2(0.65f, 0.17f), new Vector2(300f, 120f)); // main action
                }

                var ui = root.AddComponent<BKS_MissionResultScreenUI>();
                var so = new SerializedObject(ui);
                so.FindProperty("missionNameText").objectReferenceValue = missionName;
                so.FindProperty("resultStatusText").objectReferenceValue = title;
                so.FindProperty("coinsEarnedText").objectReferenceValue = coins;
                so.FindProperty("safarStarsEarnedText").objectReferenceValue = safarStars;
                so.FindProperty("failReasonText").objectReferenceValue = reason;
                so.FindProperty("starFilled").objectReferenceValue = Load(Home + "star.png");
                var arr = so.FindProperty("starImages"); arr.arraySize = stars.Length;
                for (int i = 0; i < stars.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = stars[i];
                so.FindProperty("nextMissionButton").objectReferenceValue = cont;
                so.FindProperty("retryButton").objectReferenceValue = retry;
                so.FindProperty("indiaMapButton").objectReferenceValue = map;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, complete ? CompletePath : FailPath);
            }
            finally { Object.DestroyImmediate(root); }
        }

        // ── helpers ───────────────────────────────────────────────────────────────
        private static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; return rt;
        }

        private static TMP_Text Text(string name, Transform parent, string text, float x0, float y0, float x1, float y1, Color color, float size,
                                     TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = Node(name, parent);
            rt.anchorMin = new Vector2(x0, y0); rt.anchorMax = new Vector2(x1, y1); rt.offsetMin = rt.offsetMax = Vector2.zero;
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text; t.color = color; t.alignment = align; t.raycastTarget = false;
            t.enableWordWrapping = false; t.enableAutoSizing = true; t.fontSizeMin = 16f; t.fontSizeMax = size;
            t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        private static Image Icon(string name, Transform parent, string sprite, Vector2 centre, float size)
        {
            var rt = Node(name, parent);
            rt.anchorMin = rt.anchorMax = centre; rt.sizeDelta = new Vector2(size, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Load(sprite); img.preserveAspect = true; img.raycastTarget = false;
            return img;
        }

        private static Button Btn(string name, Transform parent, string sprite, Vector2 centre, Vector2 size)
        {
            var rt = Node(name, parent);
            rt.anchorMin = rt.anchorMax = centre; rt.sizeDelta = size;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Load(Engraved + sprite); img.preserveAspect = true;
            var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = img;
            return b;
        }
    }
}
