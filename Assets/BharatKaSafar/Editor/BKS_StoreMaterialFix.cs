using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// The Asset Store "Free Stylized Textures" materials use the 3D Standard (lit) shader with normal/metallic maps,
    /// so in this 2D game (no 3D lights) they look dark. This makes an Unlit copy of each one in
    /// Assets/BharatKaSafar/Art/Terrain/StoreUnlit using only its colour (albedo) texture. The store files are not changed.
    /// Menu: Bharat Ka Safar > Terrain > Make Unlit Copies Of Store Materials
    /// </summary>
    public static class BKS_StoreMaterialFix
    {
        private const string Source = "Assets/Game Buffs";
        private const string Target = "Assets/BharatKaSafar/Art/Terrain/StoreUnlit";

        [MenuItem("Bharat Ka Safar/Terrain/Make Unlit Copies Of Store Materials")]
        public static void Make()
        {
            if (!AssetDatabase.IsValidFolder(Target)) { Directory.CreateDirectory(Target); AssetDatabase.Refresh(); }
            int n = 0;
            foreach (var path in AssetDatabase.FindAssets("t:Material", new[] { Source }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var src = AssetDatabase.LoadAssetAtPath<Material>(path);
                var tex = src != null && src.HasProperty("_MainTex") ? src.GetTexture("_MainTex") : null;
                if (tex == null) continue;
                string dst = $"{Target}/{src.name}_Unlit.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(dst);
                if (mat == null) { mat = new Material(Shader.Find("Unlit/Texture")); AssetDatabase.CreateAsset(mat, dst); }
                mat.mainTexture = tex;
                mat.mainTextureScale = src.mainTextureScale;
                EditorUtility.SetDirty(mat);
                n++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Store Materials] {n} unlit copies made in {Target}. Use these on terrains instead of the store ones.");
        }
    }
}
