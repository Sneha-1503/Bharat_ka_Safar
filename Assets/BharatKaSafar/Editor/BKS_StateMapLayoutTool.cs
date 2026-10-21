using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.Home;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// State Map (BKS_StateMap) layout update — everything stays editable in the Canvas:
    ///  • chapter panel: adds CHAPTER number, "Missions 2/5" text + progress bar and "Stars 9/15" (filled by ChapterDetailPanel)
    ///  • coins: the coin counter moves out of the panel to the top-right corner (the "Available coins" label is hidden)
    ///  • ENTER button art (BKS sprite/home "ChatGPT Image Oct 21, 2026, 06_02_54 PM.png")
    ///  • if present in Art/UI/StateMap: tab_normal / tab_selected (region tabs, 9-sliced), tab_selected also = UNLOCK button,
    ///    coin_bar (coin counter background)
    /// Safe to run again (it reuses what it made).
    /// Menu: Bharat Ka Safar > Home > Update State Map Layout
    /// </summary>
    public static class BKS_StateMapLayoutTool
    {
        private const string ScenePath = "Assets/BharatKaSafar/Scenes/Home/BKS_StateMap.unity";
        private const string Art = "Assets/BharatKaSafar/Art/UI/StateMap/";
        private const string EnterArt = "Assets/BharatKaSafar/BKS sprite/home/ChatGPT Image Oct 21, 2026, 06_02_54 PM.png";
        private static readonly Color Gold = new Color(1f, 0.82f, 0.35f);

        [MenuItem("Bharat Ka Safar/Home/Update State Map Layout")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PrepareSprites();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var panel = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ChapterDetailPanel>(true)).FirstOrDefault();
            if (panel == null) { Debug.LogError("[State Map] No ChapterDetailPanel in BKS_StateMap."); return; }
            var prt = (RectTransform)panel.transform;
            var canvas = panel.GetComponentInParent<Canvas>().rootCanvas.transform;
            var font = panel.GetComponentsInChildren<TMP_Text>(true).Select(t => t.font).FirstOrDefault(f => f != null);
            var so = new SerializedObject(panel);

            // chapter number under the title ribbon
            var number = Text(prt, "ChapterNumberText", "CHAPTER 01", new Vector2(0f, 236f), new Vector2(420f, 44f), Gold, 34f, font);
            so.FindProperty("chapterNumberText").objectReferenceValue = number;

            // progress block where the coin counter was
            var block = Rect(prt, "ChapterProgress", new Vector2(0f, -185f), new Vector2(620f, 130f));
            var progress = Text(block, "ProgressText", "Missions  0 / 5", new Vector2(-150f, 38f), new Vector2(300f, 44f), Color.white, 32f, font);
            progress.alignment = TextAlignmentOptions.Left;
            var stars = Text(block, "StarsText", "Stars  0 / 15", new Vector2(150f, 38f), new Vector2(300f, 44f), Gold, 32f, font);
            stars.alignment = TextAlignmentOptions.Right;
            var bg = Img(block, "ProgressBar", new Vector2(0f, -10f), new Vector2(600f, 28f));
            bg.color = new Color(0f, 0f, 0f, 0.45f);
            var fill = Img(bg.rectTransform, "Fill", Vector2.zero, Vector2.zero);
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one; fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            fill.color = Gold; fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = 0.4f;
            so.FindProperty("progressText").objectReferenceValue = progress;
            so.FindProperty("starsText").objectReferenceValue = stars;
            so.FindProperty("progressFill").objectReferenceValue = fill;

            // coins → top-right corner of the screen
            var coins = (prt.Find("CoinPanel") ?? canvas.Find("CoinPanel")) as RectTransform; // second run: already moved
            if (coins != null)
            {
                coins.SetParent(canvas, true);
                coins.anchorMin = coins.anchorMax = new Vector2(1f, 1f);
                coins.anchoredPosition = new Vector2(-470f, -80f); // left of the settings button
                var bar = Load(Art + "coin_bar.png");
                if (bar != null)
                {
                    var barImg = Img(coins, "CoinBar", new Vector2(150f, 0f), new Vector2(380f, 106f));
                    barImg.sprite = bar; barImg.preserveAspect = true; barImg.color = Color.white;
                    barImg.transform.SetAsFirstSibling();
                }
            }
            foreach (var t in prt.GetComponentsInChildren<TMP_Text>(true))
                if (t.transform.parent == prt && t.text.ToLowerInvariant().Contains("coin")) t.gameObject.SetActive(false);

            // button art
            var enter = Load(EnterArt);
            if (enter != null) so.FindProperty("enterSprite").objectReferenceValue = enter;
            var gold = Load(Art + "tab_selected.png");
            if (gold != null) so.FindProperty("plainButtonSprite").objectReferenceValue = gold;
            so.ApplyModifiedPropertiesWithoutUndo();

            // region tabs art
            var tabs = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<BKS_MapRegionTabs>(true)).FirstOrDefault();
            var normal = Load(Art + "tab_normal.png");
            if (tabs != null && normal != null && gold != null)
            {
                var tso = new SerializedObject(tabs);
                tso.FindProperty("tabNormalSprite").objectReferenceValue = normal;
                tso.FindProperty("tabSelectedSprite").objectReferenceValue = gold;
                tso.ApplyModifiedPropertiesWithoutUndo();
                var bar = tabs.transform.parent.Find("RegionTabs") as RectTransform;
                if (bar != null) bar.sizeDelta = new Vector2(Mathf.Max(bar.sizeDelta.x, 1200f), 96f);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[State Map] Layout updated: chapter number, missions progress, stars, coins top-right, ENTER art" +
                      (normal != null ? ", tab art." : ". (Put tab_normal / tab_selected / coin_bar in Art/UI/StateMap and run again for the rest.)"));
        }

        // sprites: single, no mipmaps; tabs/buttons 9-sliced so they stretch without bending the ornaments
        private static void PrepareSprites()
        {
            foreach (var path in new[] { EnterArt, Art + "tab_normal.png", Art + "tab_selected.png", Art + "info_bar.png", Art + "coin_bar.png" })
            {
                if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;
                ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.mipmapEnabled = false;
                if (path.Contains("tab_"))
                {
                    ti.GetSourceTextureWidthAndHeight(out int w, out int h);
                    float side = w * 0.2f, top = h * 0.3f; // ornaments at both ends stay unstretched
                    ti.spriteBorder = new Vector4(side, top, side, top);
                }
                ti.SaveAndReimport();
            }
        }

        private static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static RectTransform Rect(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var t = parent.Find(name) as RectTransform;
            if (t == null) { t = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); t.SetParent(parent, false); }
            t.anchorMin = t.anchorMax = new Vector2(0.5f, 0.5f); t.anchoredPosition = pos; t.sizeDelta = size;
            return t;
        }

        private static Image Img(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var t = Rect(parent, name, pos, size);
            var img = t.GetComponent<Image>(); if (img == null) img = t.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            if (img.sprite == null) img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            return img;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, string text, Vector2 pos, Vector2 size, Color color, float max, TMP_FontAsset font)
        {
            var t = Rect(parent, name, pos, size);
            var tmp = t.GetComponent<TextMeshProUGUI>(); if (tmp == null) tmp = t.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text; tmp.color = color; tmp.alignment = TextAlignmentOptions.Center; tmp.fontStyle = FontStyles.Bold;
            tmp.enableAutoSizing = true; tmp.fontSizeMin = 14f; tmp.fontSizeMax = max; tmp.raycastTarget = false;
            return tmp;
        }
    }
}
