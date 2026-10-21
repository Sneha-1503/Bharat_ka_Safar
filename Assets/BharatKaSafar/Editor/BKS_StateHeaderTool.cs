using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.Core;
using BharatKaSafar.UI;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Mission Select title → state header art.
    ///  • gives each ChapterData its header slice from "BKS sprite/background/header" (matched by reading the art:
    ///    each sheet holds 5 states, some slices are empty)
    ///  • adds "StateHeader" (an Image at the title's place) to the BKS_MissionSelect scene and links it to the screen
    /// The screen shows the header and hides the title text when the chapter has a header.
    /// Menu: Bharat Ka Safar > UI > Mission Select State Headers
    /// </summary>
    public static class BKS_StateHeaderTool
    {
        private const string Folder = "Assets/BharatKaSafar/BKS sprite/background/header/";
        private const string Scene = "Assets/BharatKaSafar/Scenes/BKS_MissionSelect.unity";

        // sheet number (…PM-N.png) and slice index → state, as painted on the art
        private static readonly Dictionary<string, (int sheet, int slice)> Map = new Dictionary<string, (int, int)>
        {
            {"Andhra Pradesh",(1,0)}, {"Arunachal Pradesh",(1,1)}, {"Assam",(1,2)}, {"Bihar",(1,3)}, {"Chhattisgarh",(1,4)},
            {"Goa",(2,0)}, {"Gujarat",(2,1)}, {"Haryana",(2,2)}, {"Himachal Pradesh",(2,3)}, {"Jharkhand",(2,4)},
            {"Manipur",(3,0)}, {"Maharashtra",(3,2)}, {"Madhya Pradesh",(3,3)}, {"Kerala",(3,4)}, {"Karnataka",(3,5)},
            {"Punjab",(4,0)}, {"Odisha",(4,1)}, {"Nagaland",(4,2)}, {"Mizoram",(4,3)}, {"Meghalaya",(4,4)},
            {"Tripura",(5,0)}, {"Rajasthan",(5,1)}, {"Sikkim",(5,2)}, {"Tamil Nadu",(5,3)}, {"Telangana",(5,4)},
            {"West Bengal",(6,0)}, {"Uttar Pradesh",(6,3)}, {"Uttarakhand",(6,4)},
        };

        [MenuItem("Bharat Ka Safar/UI/Mission Select State Headers")]
        public static void Run()
        {
            // 1. sprites → chapters
            var sprites = AssetDatabase.FindAssets("t:Sprite", new[] { Folder.TrimEnd('/') })
                .SelectMany(g => AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(g)).OfType<Sprite>()).ToList();
            int linked = 0; var missing = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:ChapterData"))
            {
                var ch = AssetDatabase.LoadAssetAtPath<ChapterData>(AssetDatabase.GUIDToAssetPath(guid));
                if (ch == null || !Map.TryGetValue(ch.StateName ?? "", out var at)) continue;
                var sprite = sprites.FirstOrDefault(s => s.name.EndsWith($"PM-{at.sheet}_{at.slice}"));
                if (sprite == null) { missing.Add(ch.StateName); continue; }
                var so = new SerializedObject(ch);
                so.FindProperty("headerImage").objectReferenceValue = sprite;
                so.ApplyModifiedPropertiesWithoutUndo();
                linked++;
            }
            AssetDatabase.SaveAssets();

            // 2. header image in the Mission Select scene
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);
            var screen = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<BKS_MissionSelectScreen>(true)).FirstOrDefault();
            if (screen == null) { Debug.LogError("[Headers] No BKS_MissionSelectScreen in BKS_MissionSelect."); return; }
            var sso = new SerializedObject(screen);
            var title = sso.FindProperty("titleText").objectReferenceValue as TMP_Text;
            var img = sso.FindProperty("headerImage").objectReferenceValue as Image;
            if (img == null && title != null)
            {
                var trt = (RectTransform)title.transform;
                var go = new GameObject("StateHeader", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(trt.parent, false);
                go.transform.SetSiblingIndex(trt.GetSiblingIndex() + 1);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = trt.anchorMin; rt.anchorMax = trt.anchorMax; rt.pivot = trt.pivot;
                rt.anchoredPosition = trt.anchoredPosition;
                rt.sizeDelta = new Vector2(Mathf.Max(1100f, trt.rect.width), 110f); // header art is ~11:1
                img = go.GetComponent<Image>();
                img.preserveAspect = true; img.raycastTarget = false;
                sso.FindProperty("headerImage").objectReferenceValue = img;
                sso.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeObject = img != null ? img.gameObject : null;
            Debug.Log($"[Headers] {linked} chapters got their header." + (missing.Count > 0 ? " Missing: " + string.Join(", ", missing) : "") +
                      " Move / resize 'StateHeader' in the Mission Select Canvas as you like.");
        }
    }
}
