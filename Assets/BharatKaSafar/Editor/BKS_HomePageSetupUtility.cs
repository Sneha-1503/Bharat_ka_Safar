using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.Core;
using BharatKaSafar.Home;

namespace BharatKaSafar.EditorTools
{
    /// <summary>
    /// One-click setup for the Home page (India Map → Chapter Selection).
    ///
    /// Menu: Bharat Ka Safar → Home → ...
    ///   1. Configure Test States   – sets up UP / RJ / TN / PB ChapterData assets
    ///   2. Setup Open Home Scene   – adds components + missing panel children and wires every reference
    ///   3. Update Build Settings   – Home first, chapter scenes added
    ///   Run All (1 → 2 → 3)
    ///
    /// Safe to run more than once: existing objects are reused, nothing is duplicated,
    /// and text/costs you already edited are not overwritten.
    /// Everything is Undo-able. Save the scene (Ctrl+S) afterwards.
    /// </summary>
    public static class BKS_HomePageSetupUtility
    {
        private const string MenuRoot       = "Bharat Ka Safar/Home/";
        private const string ChaptersFolder = "Assets/BharatKaSafar/ScriptableObjects/chapters";
        private const string HomeScenePath  = "Assets/BharatKaSafar/Scenes/Home/BKS_StateMap.unity";
        private const string LegacyHomePath = "Assets/Scenes/Home.unity";
        private const string SpriteFolder   = "Assets/BharatKaSafar/BKS sprite/home/";

        // Existing project art (placeholders until final art arrives)
        private const string PinNormal    = "ChatGPT Image Sep 27, 2026, 07_42_46 PM-1.png"; // blue
        private const string PinSelected  = "ChatGPT Image Sep 27, 2026, 07_42_48 PM-2.png"; // gold
        private const string PinCompleted = "ChatGPT Image Sep 27, 2026, 07_42_49 PM-3.png"; // green ✓
        private const string PinLocked    = "ChatGPT Image Sep 27, 2026, 07_42_51 PM-4.png"; // brown 🔒
        private const string SpriteLock      = "lock.png";
        private const string SpriteCoin      = "coin.png";
        private const string SpriteCompleted = "completed.png";
        private const string SpriteBlankBtn  = "button_yellow_idle.png";
        private const string BakedEnterBtn   = "chap btn";

        // ── Test state configuration ──────────────────────────────────────────────

        private class StateConfig
        {
            public string Id, AssetFile, StateName, Title, Description, Scene;
            public bool   Available, UnlockedByDefault;
            public int    Cost;
        }

        private static readonly StateConfig[] TestStates =
        {
            new StateConfig {
                Id = "UP", AssetFile = "UttarPradesh_UttarPradesh.asset", StateName = "Uttar Pradesh",
                Title = "Heritage & Heartland",
                Description = "From the marble glory of the Taj Mahal to the sacred ghats of Varanasi — begin your journey through the heartland of India.",
                Available = true, UnlockedByDefault = false, Cost = 2000, Scene = "BKS_UP_MissionSelect 1" },

            new StateConfig {
                Id = "RJ", AssetFile = "RajasthanChapter.asset", StateName = "Rajasthan",
                Title = "Desert Kingdoms",
                Description = "Drive through golden sands, royal forts and the colourful cities of the Land of Kings.",
                Available = true, UnlockedByDefault = false, Cost = 1500, Scene = "BKS_Rajasthan_MissionSelect" },

            new StateConfig {
                Id = "TN", AssetFile = "TN_ChapterData.asset", StateName = "Tamil Nadu",
                Title = "Temples, Trails & Tamil Tales",
                Description = "Ancient temples, coastal highways and misty mountain roads — from Chennai to Kanyakumari.",
                Available = true, UnlockedByDefault = true, Cost = 0, Scene = "BKS_TN_MissionSelect 1" },

            new StateConfig {
                Id = "PB", AssetFile = "Punjab_ChapterData.asset", StateName = "Punjab",
                Title = "Land of Five Rivers",
                Description = "Golden fields, the Golden Temple and the spirit of Punjab — this journey is coming soon.",
                Available = false, UnlockedByDefault = false, Cost = 0, Scene = "" },
        };

        // Map button object name → (state ID, display name)
        private static readonly Dictionary<string, (string id, string label)> ButtonIds =
            new Dictionary<string, (string, string)>
        {
            { "UPButton",        ("UP", "Uttar Pradesh") },
            { "RajasthanButton", ("RJ", "Rajasthan") },
            { "TNButton",        ("TN", "Tamil Nadu") },
            { "PBButton",        ("PB", "Punjab") },
            { "AssamButton",     ("AS", "Assam") },
            { "OdishaButton",    ("OD", "Odisha") },
            { "HPButton",        ("HP", "Himachal Pradesh") },
            { "UKButton",        ("UK", "Uttarakhand") },
            { "BiharButton",     ("BR", "Bihar") },
            { "WBButton",        ("WB", "West Bengal") },
        };

        // ── Menu items ────────────────────────────────────────────────────────────

        [MenuItem(MenuRoot + "Run All (1 → 2 → 3)", priority = 0)]
        public static void RunAll()
        {
            var states = ConfigureTestStates(silent: true);
            if (!SetupOpenHomeScene(states, silent: true)) return;
            UpdateBuildSettings(states, silent: true);
            EditorUtility.DisplayDialog("Bharat Ka Safar",
                "Home page setup complete.\n\n" +
                "• ChapterData: UP, RJ, TN, PB configured\n" +
                "• Home scene wired (remember Ctrl+S)\n" +
                "• Build Settings updated\n\n" +
                "Next: arrange the new panel children in the Scene view and assign images on the ChapterData assets.",
                "OK");
        }

        [MenuItem(MenuRoot + "1. Configure Test States (UP, RJ, TN, PB)", priority = 11)]
        private static void MenuConfigure() => ConfigureTestStates(silent: false);

        [MenuItem(MenuRoot + "2. Setup Open Home Scene", priority = 12)]
        private static void MenuSetupScene() => SetupOpenHomeScene(LoadTestStates(), silent: false);

        [MenuItem(MenuRoot + "3. Update Build Settings", priority = 13)]
        private static void MenuBuildSettings() => UpdateBuildSettings(LoadTestStates(), silent: false);

        // ── 1. ChapterData assets ─────────────────────────────────────────────────

        private static List<ChapterData> ConfigureTestStates(bool silent)
        {
            var result = new List<ChapterData>();

            foreach (var cfg in TestStates)
            {
                string path = $"{ChaptersFolder}/{cfg.AssetFile}";
                var asset = AssetDatabase.LoadAssetAtPath<ChapterData>(path);
                bool created = false;

                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<ChapterData>();
                    AssetDatabase.CreateAsset(asset, path);
                    created = true;
                }

                var so = new SerializedObject(asset);
                bool firstTime = created || so.FindProperty("chapterID").stringValue != cfg.Id;

                if (firstTime)
                {
                    // Identity + rules are written only the first time, so later Inspector edits survive re-runs.
                    so.FindProperty("chapterID").stringValue         = cfg.Id;
                    so.FindProperty("chapterAvailable").boolValue    = cfg.Available;
                    so.FindProperty("unlockedByDefault").boolValue   = cfg.UnlockedByDefault;
                    so.FindProperty("unlockCost").intValue           = cfg.Cost;
                    so.FindProperty("chapterSceneName").stringValue  = cfg.Scene;
                }

                SetIfEmpty(so, "stateName",    cfg.StateName);
                SetIfEmpty(so, "chapterTitle", cfg.Title);
                SetIfEmpty(so, "description",  cfg.Description);

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                result.Add(asset);
            }

            AssetDatabase.SaveAssets();

            Debug.Log("[BKS Home Setup] ChapterData configured: " + string.Join(", ", result.Select(r => r.ChapterID)));

            if (!silent)
                EditorUtility.DisplayDialog("Bharat Ka Safar", "UP, RJ, TN and PB ChapterData assets are configured.", "OK");
            return result;
        }

        private static List<ChapterData> LoadTestStates()
        {
            var list = new List<ChapterData>();
            foreach (var cfg in TestStates)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ChapterData>($"{ChaptersFolder}/{cfg.AssetFile}");
                if (asset != null) list.Add(asset);
            }
            if (list.Count < TestStates.Length || list.Any(a => string.IsNullOrEmpty(a.ChapterID)))
                list = ConfigureTestStates(silent: true);
            return list;
        }

        private static void SetIfEmpty(SerializedObject so, string prop, string value)
        {
            var p = so.FindProperty(prop);
            if (p != null && string.IsNullOrEmpty(p.stringValue)) p.stringValue = value;
        }

        // ── 2. Scene setup ────────────────────────────────────────────────────────

        private static bool SetupOpenHomeScene(List<ChapterData> states, bool silent)
        {
            var controller = Object.FindFirstObjectByType<BKS_HomePageController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                EditorUtility.DisplayDialog("Bharat Ka Safar",
                    "No BKS_HomePageController found in the open scene.\n\nOpen:\n" + HomeScenePath, "OK");
                return false;
            }

            Undo.SetCurrentGroupName("BKS Home Page Setup");
            int undoGroup = Undo.GetCurrentGroup();

            Transform root = controller.transform;

            // --- Map buttons ---
            Transform indiaMap = FindDeep(root, "IndiaMap");
            if (indiaMap == null)
            {
                Debug.LogError("[BKS Home Setup] 'IndiaMap' object not found under the controller.");
                return false;
            }
            EnsurePunjabButton(indiaMap);
            var mapButtons = SetupMapButtons(indiaMap);

            // --- Panel ---
            Transform hud = FindDeep(root, "ChapterHud");
            if (hud == null)
            {
                Debug.LogError("[BKS Home Setup] 'ChapterHud' object not found under the controller.");
                return false;
            }
            var panel = SetupChapterPanel(hud);

            // --- Coin HUD ---
            Transform coinPanel = FindDeep(root, "CoinPanel");
            TMP_Text coinText = coinPanel != null ? coinPanel.GetComponentInChildren<TMP_Text>(true) : null;
            if (coinText == null) Debug.LogWarning("[BKS Home Setup] Coin text (CoinPanel/Text (TMP)) not found.");

            // --- Controller references ---
            var so = new SerializedObject(controller);
            SetObjectList(so.FindProperty("states"), states.Cast<Object>().ToList());
            so.FindProperty("defaultStateId").stringValue           = "TN";
            so.FindProperty("mapButtonsRoot").objectReferenceValue  = indiaMap;
            SetObjectList(so.FindProperty("mapButtons"), mapButtons.Cast<Object>().ToList());
            so.FindProperty("chapterPanel").objectReferenceValue    = panel;
            so.FindProperty("coinText").objectReferenceValue        = coinText;
            so.ApplyModifiedProperties();

            // --- Legacy HomeManage: give it the coin text so its Start() no longer throws NullReference ---
            FixLegacyHomeManage(coinText);

            // --- Persistent managers (SaveManager + SceneLoader) ---
            EnsurePersistentManagers();

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);

            Debug.Log($"[BKS Home Setup] Scene wired: {mapButtons.Count} map buttons, panel on '{hud.name}'. Save the scene (Ctrl+S).");
            if (!silent)
                EditorUtility.DisplayDialog("Bharat Ka Safar",
                    "Home scene wired.\n\nArrange the new panel children in the Scene view, then save (Ctrl+S).", "OK");
            return true;
        }

        private static void EnsurePunjabButton(Transform indiaMap)
        {
            if (FindDeep(indiaMap, "PBButton") != null) return;

            Transform template = FindDeep(indiaMap, "RajasthanButton");
            if (template == null) template = FindDeep(indiaMap, "UPButton");
            if (template == null) return;

            var go = Object.Instantiate(template.gameObject, template.parent);
            go.name = "PBButton";
            Undo.RegisterCreatedObjectUndo(go, "Create PBButton");

            // Punjab sits north of Rajasthan — rough position, adjust in the Scene view.
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = ((RectTransform)template).anchoredPosition + new Vector2(60f, 150f);

            var label = go.GetComponentInChildren<TMP_Text>(true);
            if (label != null && !string.IsNullOrEmpty(label.text)) label.text = "Punjab";

            Debug.Log("[BKS Home Setup] Created PBButton (Punjab). Move it to the right place on the map.");
        }

        private static List<MapStateButton> SetupMapButtons(Transform indiaMap)
        {
            Sprite normal    = LoadSprite(PinNormal);
            Sprite selected  = LoadSprite(PinSelected);
            Sprite completed = LoadSprite(PinCompleted);
            Sprite locked    = LoadSprite(PinLocked);

            var list = new List<MapStateButton>();
            foreach (Transform child in indiaMap)
            {
                var button = child.GetComponent<Button>();
                if (button == null) continue;

                string id, label;
                if (ButtonIds.TryGetValue(child.name, out var mapped)) { id = mapped.id; label = mapped.label; }
                else { id = child.name.Replace("Button", "").Trim(); label = id; }

                // Remove old Inspector OnClick entries (SelectState("UP") etc.) — the component registers in code now.
                if (button.onClick.GetPersistentEventCount() > 0)
                {
                    Undo.RecordObject(button, "Remove old OnClick");
                    for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                        UnityEventTools.RemovePersistentListener(button.onClick, i);
                }

                var image = child.GetComponent<Image>();

                var outline = child.GetComponent<Outline>();
                if (outline == null) outline = Undo.AddComponent<Outline>(child.gameObject);
                Undo.RecordObject(outline, "Configure selection outline");
                outline.effectColor    = new Color(1f, 0.85f, 0.2f, 1f);
                outline.effectDistance = new Vector2(5f, -5f);
                outline.enabled        = false;

                var msb = child.GetComponent<MapStateButton>();
                if (msb == null) msb = Undo.AddComponent<MapStateButton>(child.gameObject);
                Undo.RecordObject(msb, "Configure MapStateButton");
                msb.ConfigureForEditor(id, label, button, image, outline, normal, selected, completed, locked);
                EditorUtility.SetDirty(msb);

                list.Add(msb);
            }
            return list;
        }

        private static ChapterDetailPanel SetupChapterPanel(Transform hud)
        {
            var panel = hud.GetComponent<ChapterDetailPanel>();
            if (panel == null) panel = Undo.AddComponent<ChapterDetailPanel>(hud.gameObject);

            TMP_Text title       = GetComp<TMP_Text>(FindDeep(hud, "ChapterTitleText"));
            TMP_Text description = GetComp<TMP_Text>(FindDeep(hud, "ChapterDescriptionText"));
            Button   action      = GetComp<Button>(FindDeep(hud, "EnterChapterButton"));
            TMP_Text actionText  = action != null ? action.GetComponentInChildren<TMP_Text>(true) : null;

            TMP_FontAsset font  = title != null ? title.font : null;
            Color textColor     = title != null ? title.color : Color.white;

            // Action button: swap the baked "ENTER CHAPTER" sprite for a blank one so the TMP label can change.
            if (action != null)
            {
                if (action.onClick.GetPersistentEventCount() > 0)
                {
                    Undo.RecordObject(action, "Remove old OnClick");
                    for (int i = action.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                        UnityEventTools.RemovePersistentListener(action.onClick, i);
                }

                var btnImage = action.GetComponent<Image>();
                var blank    = LoadSprite(SpriteBlankBtn);
                if (btnImage != null && blank != null && btnImage.sprite != null && btnImage.sprite.name == BakedEnterBtn)
                {
                    Undo.RecordObject(btnImage, "Blank action button");
                    btnImage.sprite = blank;
                }
                if (actionText != null)
                {
                    Undo.RecordObject(actionText.gameObject, "Enable action text");
                    actionText.gameObject.SetActive(true);
                }
            }

            // --- Missing children (created once, reused afterwards) ---
            var mainImage = GetOrCreate(hud, "MainStateImage", new Vector2(0.08f, 0.56f), new Vector2(0.92f, 0.80f));
            AddImage(mainImage, null);

            var landmarksRoot = GetOrCreate(hud, "FamousLandmarks", new Vector2(0.06f, 0.38f), new Vector2(0.94f, 0.52f));
            var hlg = GetOrAdd<HorizontalLayoutGroup>(landmarksRoot.gameObject);
            hlg.spacing = 8f; hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = hlg.childControlHeight = true;
            hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;
            var landmarkImages = new List<Object>();
            for (int i = 1; i <= 5; i++)
            {
                var lm = GetOrCreate(landmarksRoot, $"Landmark{i:00}", Vector2.zero, Vector2.one);
                landmarkImages.Add(AddImage(lm, null));
            }

            var statusArea = GetOrCreate(hud, "StatusArea", new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.36f));
            var shlg = GetOrAdd<HorizontalLayoutGroup>(statusArea.gameObject);
            shlg.spacing = 10f; shlg.childAlignment = TextAnchor.MiddleCenter;
            shlg.childControlWidth = shlg.childControlHeight = true;
            shlg.childForceExpandWidth = shlg.childForceExpandHeight = false;

            var lockIcon   = GetOrCreate(statusArea, "LockIcon", Vector2.zero, Vector2.one);
            AddImage(lockIcon, LoadSprite(SpriteLock)); SetPreferredSize(lockIcon, 48, 48);
            var statusText = GetOrCreate(statusArea, "StatusText", Vector2.zero, Vector2.one);
            var statusTmp  = AddText(statusText, "LOCKED CHAPTER", 30, font, textColor); SetPreferredSize(statusText, 280, 48);
            var coinIcon   = GetOrCreate(statusArea, "CoinIcon", Vector2.zero, Vector2.one);
            AddImage(coinIcon, LoadSprite(SpriteCoin)); SetPreferredSize(coinIcon, 40, 40);
            var costText   = GetOrCreate(statusArea, "UnlockCostText", Vector2.zero, Vector2.one);
            var costTmp    = AddText(costText, "1,500", 30, font, textColor); SetPreferredSize(costText, 120, 48);

            var badge = GetOrCreate(hud, "CompletedBadge", new Vector2(0.80f, 0.82f), new Vector2(0.98f, 0.98f));
            AddImage(badge, LoadSprite(SpriteCompleted));
            badge.gameObject.SetActive(false);

            var feedback = GetOrCreate(hud, "FeedbackText", new Vector2(0.05f, 0.20f), new Vector2(0.95f, 0.27f));
            var feedbackTmp = AddText(feedback, "NOT ENOUGH COINS!", 26, font, new Color(1f, 0.35f, 0.3f, 1f));
            feedback.gameObject.SetActive(false);

            GameObject actionCoin = null;
            if (action != null)
            {
                var ac = GetOrCreate(action.transform, "CoinIcon", new Vector2(0.05f, 0.2f), new Vector2(0.18f, 0.8f));
                AddImage(ac, LoadSprite(SpriteCoin));
                ac.gameObject.SetActive(false);
                actionCoin = ac.gameObject;
            }

            // --- Wire panel references ---
            var so = new SerializedObject(panel);
            so.FindProperty("titleText").objectReferenceValue        = title;
            so.FindProperty("descriptionText").objectReferenceValue  = description;
            so.FindProperty("mainStateImage").objectReferenceValue   = mainImage.GetComponent<Image>();
            SetObjectList(so.FindProperty("landmarkImages"), landmarkImages);
            so.FindProperty("lockIcon").objectReferenceValue         = lockIcon.gameObject;
            so.FindProperty("statusText").objectReferenceValue       = statusTmp;
            so.FindProperty("coinIcon").objectReferenceValue         = coinIcon.gameObject;
            so.FindProperty("unlockCostText").objectReferenceValue   = costTmp;
            so.FindProperty("completedBadge").objectReferenceValue   = badge.gameObject;
            so.FindProperty("actionButton").objectReferenceValue     = action;
            so.FindProperty("actionButtonText").objectReferenceValue = actionText;
            so.FindProperty("actionCoinIcon").objectReferenceValue   = actionCoin;
            so.FindProperty("feedbackText").objectReferenceValue     = feedbackTmp;
            so.ApplyModifiedProperties();

            if (title == null || description == null || action == null)
                Debug.LogWarning("[BKS Home Setup] ChapterTitleText / ChapterDescriptionText / EnterChapterButton not all found — assign them on ChapterDetailPanel manually.");

            return panel;
        }

        private static void FixLegacyHomeManage(TMP_Text coinText)
        {
            if (coinText == null) return;
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb == null || mb.GetType().Name != "HomeManage") continue;
                var so = new SerializedObject(mb);
                var p  = so.FindProperty("coinText");
                if (p != null && p.objectReferenceValue == null)
                {
                    p.objectReferenceValue = coinText;
                    so.ApplyModifiedProperties();
                    Debug.Log("[BKS Home Setup] Assigned coin text to legacy HomeManage (fixes its NullReference).");
                }
            }
        }

        private static void EnsurePersistentManagers()
        {
            if (Object.FindFirstObjectByType<BKS_PersistentManagers>(FindObjectsInactive.Include) != null) return;

            var go = new GameObject("[BKS_PersistentManagers]");
            Undo.RegisterCreatedObjectUndo(go, "Create BKS_PersistentManagers");
            Undo.AddComponent<BKS_PersistentManagers>(go);
            Debug.Log("[BKS Home Setup] Added [BKS_PersistentManagers] (creates SaveManager + SceneLoader).");
        }

        // ── 3. Build Settings ─────────────────────────────────────────────────────

        private static void UpdateBuildSettings(List<ChapterData> states, bool silent)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            var report = new List<string>();

            // Preserve the launch sequence when refreshing map/chapter setup.
            string[] launchScenes = {
                "Assets/BharatKaSafar/Scenes/Home/BKS_Splash.unity",
                "Assets/BharatKaSafar/Scenes/Home/Home.unity",
                HomeScenePath
            };
            scenes.RemoveAll(s => launchScenes.Contains(s.path));
            for (int i = 0; i < launchScenes.Length; i++)
                scenes.Insert(i, new EditorBuildSettingsScene(launchScenes[i], true));
            report.Add("Launch sequence: Splash → Home → India Map");

            // The legacy Assets/Scenes/Home.unity has the same scene name "Home" — disable it to avoid a clash.
            foreach (var s in scenes)
                if (s.path == LegacyHomePath && s.enabled) { s.enabled = false; report.Add("Legacy Scenes/Home.unity disabled (name clash)"); }

            // Chapter scenes
            foreach (var data in states)
            {
                if (data == null || string.IsNullOrEmpty(data.ChapterSceneName)) continue;

                string path = FindScenePath(data.ChapterSceneName);
                if (path == null)
                {
                    report.Add($"⚠ {data.ChapterID}: scene '{data.ChapterSceneName}' not found");
                    continue;
                }

                var existing = scenes.FirstOrDefault(s => s.path == path);
                if (existing == null) scenes.Add(new EditorBuildSettingsScene(path, true));
                else existing.enabled = true;
                report.Add($"{data.ChapterID} → {path}");
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[BKS Home Setup] Build Settings updated:\n  " + string.Join("\n  ", report));

            if (!silent)
                EditorUtility.DisplayDialog("Bharat Ka Safar", "Build Settings updated:\n\n" + string.Join("\n", report), "OK");
        }

        private static string FindScenePath(string sceneName)
        {
            var matches = AssetDatabase.FindAssets("t:Scene")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileNameWithoutExtension(p) == sceneName)
                .ToList();
            if (matches.Count == 0) return null;
            return matches.FirstOrDefault(p => p.StartsWith("Assets/BharatKaSafar/")) ?? matches[0];
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>Explicit null checks — Unity's fake-null objects break the ?? operator.</summary>
        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null) c = Undo.AddComponent<T>(go);
            return c;
        }

        private static T GetComp<T>(Transform t) where T : Component
        {
            if (t == null) return null;
            var c = t.GetComponent<T>();
            return c != null ? c : null;
        }

        private static Sprite LoadSprite(string file)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + file);
            if (sprite == null) Debug.LogWarning($"[BKS Home Setup] Sprite not found: {SpriteFolder}{file} (is its Texture Type set to Sprite?)");
            return sprite;
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent == null) return null;
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>Find a direct child by name, or create a stretched UI child with the given anchors.</summary>
        private static RectTransform GetOrCreate(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var existing = parent.Find(name);
            if (existing != null) return (RectTransform)existing;

            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static Image AddImage(RectTransform rt, Sprite sprite)
        {
            var img = GetOrAdd<Image>(rt.gameObject);
            if (sprite != null && img.sprite == null) img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget  = false;
            return img;
        }

        private static TMP_Text AddText(RectTransform rt, string text, float size, TMP_FontAsset font, Color color)
        {
            var tmp = rt.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                tmp = Undo.AddComponent<TextMeshProUGUI>(rt.gameObject);
                tmp.text = text;
                tmp.fontSize = size;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 14;
                tmp.fontSizeMax = size;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = color;
                if (font != null) tmp.font = font;
            }
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void SetPreferredSize(RectTransform rt, float w, float h)
        {
            var le = GetOrAdd<LayoutElement>(rt.gameObject);
            le.preferredWidth  = w;
            le.preferredHeight = h;
        }

        private static void SetObjectList(SerializedProperty listProp, List<Object> items)
        {
            if (listProp == null) return;
            listProp.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++)
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }
}
