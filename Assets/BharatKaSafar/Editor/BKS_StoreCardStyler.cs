using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.Store;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Store cards (BKS_Store, all three tabs) with the new card art from "store card.zip" (Art/UI/StoreCard):
    ///   card frame (2:3) · item picture in the big window · name on the plaque · price / "Owned" on the small bar ·
    ///   state button (BUY / EQUIP / EQUIPPED / LOCKED art) on the orange bar · upgrade level pips under the picture.
    /// Items are placed by the card art's own windows (fractions of the card). Everything stays editable in the Canvas.
    /// Menu: Bharat Ka Safar > Store > Style Store Cards
    /// </summary>
    public static class BKS_StoreCardStyler
    {
        private const string ScenePath = "Assets/BharatKaSafar/Scenes/BKS_Store.unity";
        private const string Art = "Assets/BharatKaSafar/Art/UI/StoreCard/";
        private static readonly Color Ink = new Color(0.30f, 0.17f, 0.05f);

        [MenuItem("Bharat Ka Safar/Store/Style Store Cards")]
        public static void Run()
        {
            Prepare();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var card = Load("store_card.png");
            if (card == null) { Debug.LogError("[Store] Missing Art/UI/StoreCard/store_card.png"); return; }
            int n = 0;
            foreach (var view in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<BKS_StoreItemView>(true)))
            {
                Style(view, card);
                n++;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Store] {n} store card templates styled with the new card art.");
        }

        private static void Style(BKS_StoreItemView view, Sprite card)
        {
            var rt = (RectTransform)view.transform;
            rt.sizeDelta = new Vector2(400f, 600f); // card art is 2:3
            var bg = rt.GetComponent<Image>(); if (bg == null) bg = rt.gameObject.AddComponent<Image>();
            bg.sprite = card; bg.type = Image.Type.Simple; bg.preserveAspect = true; bg.color = Color.white;

            var so = new SerializedObject(view);
            Place(so, "icon", new Vector2(0.13f, 0.44f), new Vector2(0.87f, 0.78f));
            var icon = so.FindProperty("icon").objectReferenceValue as Image; if (icon != null) icon.preserveAspect = true;
            Text(so, "title", new Vector2(0.15f, 0.305f), new Vector2(0.85f, 0.39f), Ink, 36f);
            Text(so, "subtitle", new Vector2(0.28f, 0.222f), new Vector2(0.72f, 0.272f), Ink, 26f);
            Place(so, "actionButton", new Vector2(0.10f, 0.07f), new Vector2(0.90f, 0.20f));
            var label = so.FindProperty("actionLabel").objectReferenceValue as TMP_Text;
            if (label != null) { label.color = Color.white; label.fontStyle = FontStyles.Bold; label.enableAutoSizing = true; label.fontSizeMax = 40; }

            // upgrade pips in a row under the picture
            var pips = so.FindProperty("levelPips");
            for (int i = 0; i < pips.arraySize; i++)
            {
                var pip = pips.GetArrayElementAtIndex(i).objectReferenceValue as Image;
                if (pip == null) continue;
                float x0 = 0.24f + i * 0.106f;
                Anchor((RectTransform)pip.transform, new Vector2(x0, 0.45f), new Vector2(x0 + 0.08f, 0.48f));
            }

            // the old "SELECTED" strip is replaced by the gold glow around the equipped card
            var badge = so.FindProperty("selectedBadge").objectReferenceValue as GameObject;
            if (badge != null) badge.SetActive(false);

            so.FindProperty("cardImage").objectReferenceValue = bg;
            so.FindProperty("buySprite").objectReferenceValue = Load("buy.png");
            so.FindProperty("equipSprite").objectReferenceValue = Load("equip.png");
            so.FindProperty("equippedSprite").objectReferenceValue = Load("equipped.png");
            so.FindProperty("lockedSprite").objectReferenceValue = Load("locked.png");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Place(SerializedObject so, string field, Vector2 min, Vector2 max)
        {
            var c = so.FindProperty(field).objectReferenceValue as Component;
            if (c != null) Anchor((RectTransform)c.transform, min, max);
        }

        private static void Text(SerializedObject so, string field, Vector2 min, Vector2 max, Color color, float maxSize)
        {
            var t = so.FindProperty(field).objectReferenceValue as TMP_Text;
            if (t == null) return;
            Anchor(t.rectTransform, min, max);
            t.color = color; t.alignment = TextAlignmentOptions.Center; t.fontStyle = FontStyles.Bold;
            t.enableAutoSizing = true; t.fontSizeMin = 12f; t.fontSizeMax = maxSize;
        }

        private static void Anchor(RectTransform r, Vector2 min, Vector2 max)
        {
            r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero; r.pivot = new Vector2(0.5f, 0.5f);
        }

        private static Sprite Load(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + file);

        private static void Prepare()
        {
            foreach (var f in new[] { "store_card.png", "buy.png", "equip.png", "equipped.png", "locked.png" })
            {
                if (!(AssetImporter.GetAtPath(Art + f) is TextureImporter ti)) continue;
                if (ti.textureType == TextureImporterType.Sprite && !ti.mipmapEnabled) continue;
                ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
                ti.mipmapEnabled = false; ti.alphaIsTransparency = true; ti.maxTextureSize = 1024;
                ti.SaveAndReimport();
            }
        }
    }
}
