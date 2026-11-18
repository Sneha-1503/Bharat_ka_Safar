using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.UI;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Puts the gold-framed background (Art/UI/HUD/ObjectiveHudBG.png, 9-sliced so it stretches with the text) on every
    /// objective HUD (BKS_ObjectiveHUD) in all gameplay scenes. An old frame on the "hud objective" holder is switched off
    /// so there is only one background. Stays editable in the Canvas.
    /// Menu: Bharat Ka Safar > UI > Objective HUD Background (All Scenes)
    /// </summary>
    public static class BKS_ObjectiveHudBGTool
    {
        private const string Art = "Assets/BharatKaSafar/Art/UI/HUD/ObjectiveHudBG.png";

        [MenuItem("Bharat Ka Safar/UI/Objective HUD Background (All Scenes)")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var sprite = PrepareSprite();
            if (sprite == null) { Debug.LogError("[ObjectiveHUD] Missing " + Art); return; }

            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Scene BKS_", new[] { "Assets/BharatKaSafar/Scenes" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/_Backup") || !System.IO.File.ReadAllText(path).Contains("BKS_ObjectiveHUD")) continue;
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                bool changed = false;
                foreach (var hud in Object.FindObjectsByType<BKS_ObjectiveHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var img = hud.GetComponent<Image>(); if (img == null) img = hud.gameObject.AddComponent<Image>();
                    img.sprite = sprite; img.type = Image.Type.Sliced; img.fillCenter = true;
                    img.pixelsPerUnitMultiplier = 2.5f; // thinner frame on the ~70-unit-high panel
                    img.color = Color.white; img.raycastTarget = false;
                    var parent = hud.transform.parent;
                    if (parent != null && parent.name.Trim().ToLower() == "hud objective")
                    {
                        var old = parent.GetComponent<Image>();
                        if (old != null) old.enabled = false;
                    }
                    changed = true;
                }
                if (changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); count++; }
            }
            Debug.Log($"[ObjectiveHUD] Background set in {count} scene(s). The panel fits its text on one line when playing.");
        }

        private static Sprite PrepareSprite()
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(Art);
            if (imp == null) return null;
            var border = new Vector4(80f, 70f, 80f, 70f); // left, bottom, right, top (image is 735x200): gold rim + round corners
            if (imp.textureType != TextureImporterType.Sprite || imp.spriteBorder != border)
            {
                imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled = false; imp.alphaIsTransparency = true; imp.spriteBorder = border;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(Art);
        }
    }
}
