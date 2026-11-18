using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.Home;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Puts the "Completed" seal (Art/UI/StateMap/CompletedBadge.png) on the State Map chapter panel's completedBadge Image:
    /// placed in the round ticket frame (same spot and size as the ticket picture, which the panel hides once completed). Only that object changes; it stays editable in the Canvas.
    /// Menu: Bharat Ka Safar > UI > Set Completed Badge (State Map)
    /// </summary>
    public static class BKS_CompletedBadgeTool
    {
        private const string Scene = "Assets/BharatKaSafar/Scenes/Home/BKS_StateMap.unity";
        private const string Art = "Assets/BharatKaSafar/Art/UI/StateMap/CompletedBadge.png";

        [MenuItem("Bharat Ka Safar/UI/Set Completed Badge (State Map)")]
        public static void Apply()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var imp = (TextureImporter)AssetImporter.GetAtPath(Art);
            if (imp == null) { Debug.LogError("[Badge] Missing " + Art); return; }
            if (imp.textureType != TextureImporterType.Sprite || imp.maxTextureSize != 256)
            {
                imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled = false; imp.alphaIsTransparency = true; imp.maxTextureSize = 256; // shown at ~130 px
                imp.SaveAndReimport();
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art);

            var scene = EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);
            var panel = Object.FindFirstObjectByType<ChapterDetailPanel>(FindObjectsInactive.Include);
            if (panel == null) { Debug.LogError("[Badge] ChapterDetailPanel not found in the State Map."); return; }
            var so = new SerializedObject(panel);
            var badge = so.FindProperty("completedBadge").objectReferenceValue as GameObject;
            if (badge == null) { Debug.LogError("[Badge] completedBadge is not assigned on ChapterDetailPanel."); return; }

            var img = badge.GetComponent<Image>(); if (img == null) img = Undo.AddComponent<Image>(badge);
            Undo.RecordObject(img, "Completed Badge");
            img.sprite = sprite; img.color = Color.white; img.type = Image.Type.Simple; img.preserveAspect = true; img.raycastTarget = false;
            var rt = (RectTransform)badge.transform;
            Undo.RecordObject(rt, "Completed Badge");
            // inside the round ticket frame: same place and size as the ticket picture (hidden by the panel once completed)
            var ticket = so.FindProperty("mainStateImage").objectReferenceValue as Image;
            if (ticket != null)
            {
                var trt = (RectTransform)ticket.transform;
                Undo.SetTransformParent(rt, trt.parent, "Completed Badge");
                rt.anchorMin = trt.anchorMin; rt.anchorMax = trt.anchorMax; rt.pivot = trt.pivot;
                rt.anchoredPosition = trt.anchoredPosition; rt.sizeDelta = trt.sizeDelta;
                rt.SetSiblingIndex(trt.GetSiblingIndex() + 1);
            }
            else { rt.sizeDelta = new Vector2(130f, 130f); badge.transform.SetAsLastSibling(); }
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = badge;
            Debug.Log("[Badge] Completed badge set. Move/resize it in the Canvas if you want it elsewhere.");
        }
    }
}
