using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Shrinks the Android build: every texture in Assets/BharatKaSafar gets an Android override
    ///  • ASTC compression (6x6 for UI/sprites, 8x8 for big backgrounds) instead of uncompressed RGBA
    ///  • max size 1024 (icons, buttons, panels, sprites) or 2048 (full-screen backgrounds)
    ///  • no mipmaps for sprites / UI (not needed in 2D)
    /// Only import settings (.meta) change; the image files stay as they are. Other folders are not touched.
    /// Undo: "Remove Android Overrides" puts every texture back to the default settings.
    /// Menu: Bharat Ka Safar > Build > Optimize Textures For Android
    /// </summary>
    public static class BKS_TextureOptimizer
    {
        private const string Folder = "Assets/BharatKaSafar";

        [MenuItem("Bharat Ka Safar/Build/Optimize Textures For Android")]
        public static void Optimize()
        {
            if (!EditorUtility.DisplayDialog("Optimize textures",
                "Compress every texture in Assets/BharatKaSafar for Android (ASTC, max 1024 / 2048 for backgrounds, no mipmaps for sprites)?\n\n" +
                "Unity will re-import all of them (can take 10-20 minutes).", "Optimize", "Cancel")) return;
            Run(false);
        }

        [MenuItem("Bharat Ka Safar/Build/Remove Android Texture Overrides (Undo)")]
        public static void Undo()
        {
            if (!EditorUtility.DisplayDialog("Undo texture optimize", "Remove the Android overrides from every texture in Assets/BharatKaSafar?", "Remove", "Cancel")) return;
            Run(true);
        }

        private static void Run(bool remove)
        {
            var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { Folder }).Select(AssetDatabase.GUIDToAssetPath).Distinct().ToList();
            int changed = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < paths.Count; i++)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("Textures", paths[i], i / (float)paths.Count)) break;
                    if (!(AssetImporter.GetAtPath(paths[i]) is TextureImporter ti)) continue;

                    var android = ti.GetPlatformTextureSettings("Android");
                    if (remove)
                    {
                        if (!android.overridden) continue;
                        android.overridden = false;
                        ti.SetPlatformTextureSettings(android);
                        ti.SaveAndReimport();
                        changed++;
                        continue;
                    }

                    ti.GetSourceTextureWidthAndHeight(out int w, out int h);
                    bool background = IsBackground(paths[i], w, h);
                    int max = background ? 2048 : 1024;
                    var format = background ? TextureImporterFormat.ASTC_8x8 : TextureImporterFormat.ASTC_6x6;
                    bool noMips = ti.textureType == TextureImporterType.Sprite || ti.textureType == TextureImporterType.GUI;

                    if (android.overridden && android.format == format && android.maxTextureSize == max && (!noMips || !ti.mipmapEnabled)) continue;
                    android.overridden = true;
                    android.format = format;
                    android.maxTextureSize = max;
                    android.compressionQuality = 50;
                    ti.SetPlatformTextureSettings(android);
                    if (noMips) ti.mipmapEnabled = false;
                    ti.SaveAndReimport();
                    changed++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }
            Debug.Log(remove ? $"[Textures] Android overrides removed from {changed} textures."
                             : $"[Textures] {changed} of {paths.Count} textures compressed for Android. Build again to see the new size.");
        }

        // full-screen pictures: mission / chapter backgrounds and other very large images
        private static bool IsBackground(string path, int w, int h)
        {
            string p = path.ToLowerInvariant();
            if (p.Contains("/hud/") || p.Contains("/obstacles/") || p.Contains("/player/")) return false;
            return p.Contains("background") ? Mathf.Max(w, h) > 1024 : Mathf.Max(w, h) >= 2000;
        }
    }
}
