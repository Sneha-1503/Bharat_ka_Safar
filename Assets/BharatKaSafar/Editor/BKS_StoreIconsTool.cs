using UnityEditor;
using UnityEngine;
using BharatKaSafar.Store;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Puts the store icons (Art/UI/StoreIcons) on the Store catalog: Engine / Tyres / Suspension / Horn upgrades and the 2X Boost packs.
    /// Menu: Bharat Ka Safar > Store > Apply Store Icons
    /// </summary>
    public static class BKS_StoreIconsTool
    {
        private const string Dir = "Assets/BharatKaSafar/Art/UI/StoreIcons/";

        [MenuItem("Bharat Ka Safar/Store/Apply Store Icons")]
        public static void Apply()
        {
            var catalog = BKS_StoreCatalog.Load();
            if (catalog == null) { Debug.LogError("[StoreIcons] Resources/BKS_StoreCatalog not found."); return; }
            Undo.RecordObject(catalog, "Apply Store Icons");
            foreach (var u in catalog.upgrades)
            {
                string file = u.kind switch
                {
                    BKS_UpgradeKind.Engine => "engine", BKS_UpgradeKind.Tyres => "tyre",
                    BKS_UpgradeKind.Suspension => "suspension", BKS_UpgradeKind.Horn => "horn", _ => null,
                };
                if (file != null) u.icon = Icon(file);
            }
            catalog.boostIcon = Icon("2x");
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("[StoreIcons] Upgrade + 2X icons set on the Store catalog.");
        }

        private static Sprite Icon(string name)
        {
            string path = Dir + name + ".png";
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) { Debug.LogWarning("[StoreIcons] Missing " + path); return null; }
            if (imp.textureType != TextureImporterType.Sprite || imp.maxTextureSize != 512)
            {
                imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled = false; imp.alphaIsTransparency = true; imp.maxTextureSize = 512; // small on the APK
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
