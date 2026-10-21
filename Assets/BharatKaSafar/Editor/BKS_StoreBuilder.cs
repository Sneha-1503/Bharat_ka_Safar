using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BharatKaSafar.Store;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Store setup:
    ///  1. Build Store Catalog  → Resources/BKS_StoreCatalog.asset with the 18 generated vehicles (+ the level's
    ///     default car, free), 2X boost packs and upgrade prices. Prices are editable in the Inspector.
    ///  2. Build Store Scene    → Scenes/BKS_Store.unity (editable Canvas) + Build Settings.
    ///  3. Add Store Button (Open Scene) → a STORE button in the top-right of the open screen (e.g. BKS_StateMap).
    /// </summary>
    public static class BKS_StoreBuilder
    {
        private const string CatalogPath = "Assets/Resources/BKS_StoreCatalog.asset";
        private const string ScenePath = "Assets/BharatKaSafar/Scenes/BKS_Store.unity";
        private const string VehicleArt = "Assets/BharatKaSafar/Art/Vehicles/Generated/";
        private static readonly Color Teal = new Color(0.05f, 0.36f, 0.38f);
        private static readonly Color Saffron = new Color(0.95f, 0.55f, 0.12f);
        private static readonly Color Parchment = new Color(0.98f, 0.93f, 0.8f, 0.96f);

        // cheapest → most expensive
        private static readonly (string name, string display, int price)[] Vehicles =
        {
            ("AutoRickshaw", "Auto Rickshaw", 1500), ("BeetleClassic", "Classic Beetle", 2000), ("RedMini", "Red Mini", 2500),
            ("BlueHatchback", "City Hatchback", 3000), ("BlueSedan", "Vintage Sedan", 3500), ("DeliveryVan", "Delivery Van", 4000),
            ("VintageRoadster", "Vintage Roadster", 4500), ("VintageTourer", "Vintage Tourer", 5000), ("TNJeep", "Tamil Nadu Jeep", 5500),
            ("RedJeep", "Red Safari Jeep", 6500), ("OrangePickup", "Orange Pickup", 7000),
            ("PoliceSUV", "Police SUV", 8000), ("GreenTractor", "Desi Tractor", 8500), ("IndianArmyJeep", "Indian Army Jeep", 10000),
            ("BlueConvertible", "Blue Convertible", 12000), ("DuneBuggy", "Dune Buggy", 14000), ("RedMuscleCar", "Red Muscle Car", 16000),
        };

        [MenuItem("Bharat Ka Safar/Store/1. Build Store Catalog")]
        public static void BuildCatalog()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            var catalog = AssetDatabase.LoadAssetAtPath<BKS_StoreCatalog>(CatalogPath);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<BKS_StoreCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }

            BKS_MissionPlannerImporter.SetSprites(VehicleArt, 512);
            var old = catalog.vehicles.ToDictionary(v => v.id, v => v); // keep prices edited by hand
            var list = new List<BKS_StoreVehicle>
            {
                old.TryGetValue("Default", out var def) ? def : new BKS_StoreVehicle { id = "Default", displayName = "Safar Jeep", price = 0 }
            };
            // The free starter is the same vehicle in every level (Adventure Jeep), never the scene's own old car
            list[0].freeByDefault = true; list[0].price = 0;
            if (list[0].prefab == null) list[0].prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BharatKaSafar/Prefabs/Vehicles/BKS_AdventureJeep.prefab");
            if (list[0].icon == null) list[0].icon = AssetDatabase.LoadAssetAtPath<Sprite>($"{VehicleArt}AdventureJeep/AdventureJeep_icon.png");
            if (list[0].displayName == "Starter Jeep") list[0].displayName = "Safar Jeep";
            foreach (var (name, display, price) in Vehicles)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/BharatKaSafar/Prefabs/Vehicles/BKS_{name}.prefab");
                if (prefab == null) { Debug.LogWarning($"[Store] Missing prefab BKS_{name} (run Vehicle Maker > Build Generated Vehicles)."); continue; }
                var v = old.TryGetValue(name, out var o) ? o : new BKS_StoreVehicle { id = name, displayName = display, price = price };
                v.prefab = prefab;
                v.icon = AssetDatabase.LoadAssetAtPath<Sprite>($"{VehicleArt}{name}/{name}_icon.png");
                list.Add(v);
            }
            catalog.vehicles = list;
            if (catalog.boostIcon == null) catalog.boostIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/BharatKaSafar/Art/Chapters/TamilNadu/HUD/tn_double_badge.png");
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Selection.activeObject = catalog;
            Debug.Log($"[Store] Catalog ready: {list.Count} vehicles, {catalog.boostPacks.Count} boost packs, {catalog.upgrades.Count} upgrades. Edit prices on {CatalogPath}.");
        }

        [MenuItem("Bharat Ka Safar/Store/2. Build Store Scene")]
        public static void BuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Store", "BKS_Store exists. Rebuild it? (edits in that scene are lost)", "Rebuild", "Cancel")) return;
            var catalog = AssetDatabase.LoadAssetAtPath<BKS_StoreCatalog>(CatalogPath);
            if (catalog == null) { BuildCatalog(); catalog = AssetDatabase.LoadAssetAtPath<BKS_StoreCatalog>(CatalogPath); }

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

            var bg = Img("Background", c, Vector2.zero, Vector2.one, Sp("Assets/BharatKaSafar/Art/Missions/StateBackgrounds/26-uttar-pradesh.png"), Color.white);
            var fit = bg.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = 16f / 9f;
            Img("WhiteVeil", c, Vector2.zero, Vector2.one, null, new Color(1, 1, 1, 0.6f));

            var title = Txt("Title", c, "STORE", 72, Teal, FontStyles.Bold, new Vector2(0.03f, 0.87f), new Vector2(0.3f, 0.98f));
            title.alignment = TextAlignmentOptions.Left;
            var coinBox = Img("CoinBox", c, new Vector2(0.55f, 0.885f), new Vector2(0.72f, 0.965f), null, Teal);
            var coins = Txt("Coins", coinBox.transform, "0", 40, Color.white, FontStyles.Bold, new Vector2(0.05f, 0f), new Vector2(0.95f, 1f));
            var boostBox = Img("BoostBox", c, new Vector2(0.73f, 0.885f), new Vector2(0.85f, 0.965f), null, Saffron);
            var boosts = Txt("Boosts", boostBox.transform, "2X x0", 36, Color.white, FontStyles.Bold, Vector2.zero, Vector2.one);
            var back = Img("BackButton", c, new Vector2(0.88f, 0.87f), new Vector2(0.97f, 0.98f), Sp("Assets/BharatKaSafar/Art/UI/Settings/BKS_BackButton.png"), Color.white);
            back.preserveAspect = true;
            var backBtn = back.gameObject.AddComponent<Button>();

            string[] tabNames = { "VEHICLES", "UPGRADES", "2X BOOSTS" };
            var tabs = new BKS_StoreScreen.Tab[3];
            for (int i = 0; i < 3; i++)
            {
                var tb = Img(tabNames[i] + "Tab", c, new Vector2(0.03f + i * 0.165f, 0.77f), new Vector2(0.18f + i * 0.165f, 0.85f), null, Teal);
                var tbtn = tb.gameObject.AddComponent<Button>();
                Txt("Label", tb.transform, tabNames[i], 36, Color.white, FontStyles.Bold, Vector2.zero, Vector2.one);
                tabs[i] = BuildPanel(c, tabNames[i], tbtn, upgrades: i == 1);
            }
            var feedback = Txt("Feedback", c, "", 36, Teal, FontStyles.Bold, new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.08f));
            feedback.gameObject.SetActive(false);

            var screen = canvasGO.AddComponent<BKS_StoreScreen>();
            var so = new SerializedObject(screen);
            so.FindProperty("catalog").objectReferenceValue = catalog;
            string[] props = { "vehiclesTab", "upgradesTab", "boostsTab" };
            for (int i = 0; i < 3; i++)
            {
                var p = so.FindProperty(props[i]);
                p.FindPropertyRelative("button").objectReferenceValue = tabs[i].button;
                p.FindPropertyRelative("panel").objectReferenceValue = tabs[i].panel;
                p.FindPropertyRelative("content").objectReferenceValue = tabs[i].content;
                p.FindPropertyRelative("template").objectReferenceValue = tabs[i].template;
            }
            so.FindProperty("coinsText").objectReferenceValue = coins;
            so.FindProperty("boostsText").objectReferenceValue = boosts;
            so.FindProperty("feedbackText").objectReferenceValue = feedback;
            so.FindProperty("backButton").objectReferenceValue = backBtn;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath)) { scenes.Add(new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = scenes.ToArray(); }
            Debug.Log("[Store] Built " + ScenePath + " and added it to Build Settings.");
        }

        [MenuItem("Bharat Ka Safar/Store/3. Add Store Button (Open Scene)")]
        public static void AddButtonToOpenScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<BKS_OpenStoreButton>(true)).Any())
            { Debug.Log("[Store] This scene already has a Store button."); return; }
            var canvas = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true))
                .Where(cv => cv.isRootCanvas && cv.renderMode != RenderMode.WorldSpace)
                .OrderByDescending(cv => cv.GetComponentsInChildren<Button>(true).Length).FirstOrDefault();
            if (canvas == null) { Debug.LogWarning("[Store] No Canvas in this scene."); return; }
            var b = Img("StoreButton", canvas.transform, new Vector2(0.76f, 0.87f), new Vector2(0.86f, 0.97f), null, Saffron);
            b.gameObject.AddComponent<Button>();
            b.gameObject.AddComponent<BKS_OpenStoreButton>();
            Txt("Label", b.transform, "STORE", 36, Color.white, FontStyles.Bold, Vector2.zero, Vector2.one);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = b.gameObject;
            Debug.Log($"[Store] STORE button added to {scene.name} (top-right). Move it in the Canvas if it overlaps something.");
        }

        private static BKS_StoreScreen.Tab BuildPanel(Transform canvas, string name, Button tabButton, bool upgrades)
        {
            var panel = Img(name + "Panel", canvas, new Vector2(0.03f, 0.1f), new Vector2(0.97f, 0.76f), null, Parchment);
            var scrollRT = Rect("Scroll", panel.transform, new Vector2(0.01f, 0.02f), new Vector2(0.99f, 0.98f));
            var scroll = scrollRT.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = true; scroll.vertical = false; scroll.scrollSensitivity = 30f;
            var viewport = Rect("Viewport", scrollRT, Vector2.zero, Vector2.one);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.001f);
            var content = Rect("Content", viewport, Vector2.zero, new Vector2(0f, 1f));
            content.pivot = new Vector2(0f, 0.5f);
            var row = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 40; row.padding = new RectOffset(30, 30, 20, 20);
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = false;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content;

            var template = BuildCard(content, upgrades);
            return new BKS_StoreScreen.Tab { button = tabButton, panel = panel.gameObject, content = content, template = template };
        }

        private static BKS_StoreItemView BuildCard(Transform content, bool upgrades)
        {
            var card = Img("ItemTemplate", content, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), null, Color.white);
            card.rectTransform.sizeDelta = new Vector2(420, 560);
            var outline = card.gameObject.AddComponent<Outline>(); outline.effectColor = Teal; outline.effectDistance = new Vector2(4, -4);
            var selected = Img("SelectedBadge", card.transform, new Vector2(0f, 0.9f), new Vector2(1f, 1f), null, Saffron);
            Txt("Label", selected.transform, "SELECTED", 30, Color.white, FontStyles.Bold, Vector2.zero, Vector2.one);
            var icon = Img("Icon", card.transform, new Vector2(0.06f, 0.48f), new Vector2(0.94f, 0.88f), null, Color.white);
            icon.preserveAspect = true;
            var title = Txt("Title", card.transform, "Name", 38, Teal, FontStyles.Bold, new Vector2(0.04f, 0.37f), new Vector2(0.96f, 0.47f));
            var sub = Txt("Subtitle", card.transform, "Owned", 26, new Color(0.3f, 0.25f, 0.2f), FontStyles.Normal, new Vector2(0.06f, 0.2f), new Vector2(0.94f, 0.37f));
            var pips = new List<Image>();
            if (upgrades)
                for (int i = 0; i < 5; i++)
                    pips.Add(Img("Pip" + (i + 1), card.transform, new Vector2(0.15f + i * 0.145f, 0.17f), new Vector2(0.27f + i * 0.145f, 0.21f), null, Color.white));
            var btnImg = Img("ActionButton", card.transform, new Vector2(0.1f, 0.03f), new Vector2(0.9f, 0.15f), null, Saffron);
            var btn = btnImg.gameObject.AddComponent<Button>();
            var lbl = Txt("Label", btnImg.transform, "BUY", 34, Color.white, FontStyles.Bold, Vector2.zero, Vector2.one);

            var view = card.gameObject.AddComponent<BKS_StoreItemView>();
            var so = new SerializedObject(view);
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("title").objectReferenceValue = title;
            so.FindProperty("subtitle").objectReferenceValue = sub;
            so.FindProperty("actionButton").objectReferenceValue = btn;
            so.FindProperty("actionLabel").objectReferenceValue = lbl;
            so.FindProperty("selectedBadge").objectReferenceValue = selected.gameObject;
            var arr = so.FindProperty("levelPips"); arr.arraySize = pips.Count;
            for (int i = 0; i < pips.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = pips[i];
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
