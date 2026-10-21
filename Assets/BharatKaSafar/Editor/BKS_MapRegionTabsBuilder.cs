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
    /// Adds region tabs to the India map in BKS_StateMap (editable Canvas objects):
    ///   MapViewport (RectMask2D + BKS_MapRegionTabs)  ← same place/size as the IndiaMap was
    ///     IndiaMap                                     ← moved inside, centred
    ///   RegionTabs (6 buttons above the map: All India, North, West, South, East, North-East)
    /// Running it again rebuilds the tabs and keeps the viewport.
    /// Menu: Bharat Ka Safar > Home > Add Map Region Tabs (Open StateMap Scene)
    /// </summary>
    public static class BKS_MapRegionTabsBuilder
    {
        private const string ScenePath = "Assets/BharatKaSafar/Scenes/Home/BKS_StateMap.unity";

        private static readonly (string name, string[] ids)[] Regions =
        {
            ("All India",  new string[0]),
            ("North",      new[] { "PB", "HR", "HP", "UK", "UP", "UttarPradesh", "JK", "LA" }),
            ("West",       new[] { "RJ", "GJ", "MH", "GA", "MP" }),
            ("South",      new[] { "KA", "KL", "TN", "AP", "TG" }),
            ("East",       new[] { "BR", "JH", "WB", "OD", "CG" }),
            ("North-East", new[] { "AS", "AR", "NL", "MN", "MZ", "TR", "ML", "SK" }),
        };

        [MenuItem("Bharat Ka Safar/Home/Add Map Region Tabs (Open StateMap Scene)")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var map = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<RectTransform>(true)).FirstOrDefault(t => t.name == "IndiaMap");
            if (map == null) { Debug.LogError("[Map Tabs] No 'IndiaMap' in BKS_StateMap."); return; }

            // 1. viewport in the map's current place (only once)
            var viewport = map.parent as RectTransform;
            if (viewport == null || viewport.name != "MapViewport")
            {
                var parent = (RectTransform)map.parent;
                viewport = new GameObject("MapViewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
                viewport.SetParent(parent, false);
                viewport.SetSiblingIndex(map.GetSiblingIndex());
                viewport.anchorMin = map.anchorMin; viewport.anchorMax = map.anchorMax; viewport.pivot = new Vector2(0.5f, 0.5f);
                viewport.sizeDelta = map.rect.size * (Vector2)map.localScale;
                viewport.position = map.TransformPoint(map.rect.center);
                map.SetParent(viewport, true);
                map.anchorMin = map.anchorMax = new Vector2(0.5f, 0.5f);
                var size = map.rect.size;
                map.pivot = new Vector2(0.5f, 0.5f);
                map.sizeDelta = size;
                map.anchoredPosition = Vector2.zero;
            }
            var tabsComp = viewport.GetComponent<BKS_MapRegionTabs>();
            if (tabsComp == null) tabsComp = viewport.gameObject.AddComponent<BKS_MapRegionTabs>();

            // 2. tab bar just above the viewport
            var old = viewport.parent.Find("RegionTabs");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var bar = new GameObject("RegionTabs", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
            bar.SetParent(viewport.parent, false);
            bar.SetSiblingIndex(viewport.GetSiblingIndex() + 1);
            bar.anchorMin = viewport.anchorMin; bar.anchorMax = viewport.anchorMax; bar.pivot = new Vector2(0.5f, 0f);
            bar.sizeDelta = new Vector2(Mathf.Max(viewport.sizeDelta.x, 900f), 70f);
            bar.anchoredPosition = viewport.anchoredPosition + new Vector2(0f, viewport.sizeDelta.y * 0.5f + 10f);
            var layout = bar.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f; layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;

            var so = new SerializedObject(tabsComp);
            so.FindProperty("map").objectReferenceValue = map;
            var list = so.FindProperty("regions");
            list.arraySize = Regions.Length;
            for (int i = 0; i < Regions.Length; i++)
            {
                var tab = MakeTab(bar, Regions[i].name);
                var el = list.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("name").stringValue = Regions[i].name;
                var ids = el.FindPropertyRelative("stateIds");
                ids.arraySize = Regions[i].ids.Length;
                for (int k = 0; k < Regions[i].ids.Length; k++) ids.GetArrayElementAtIndex(k).stringValue = Regions[i].ids[k];
                el.FindPropertyRelative("tab").objectReferenceValue = tab;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = bar.gameObject;
            Debug.Log("[Map Tabs] Region tabs added to BKS_StateMap. Move/resize 'RegionTabs' and 'MapViewport' in the Canvas as you like.");
        }

        private static Button MakeTab(Transform parent, string label)
        {
            var go = new GameObject(label.Replace(" ", "") + "Tab", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.75f);
            var text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(go.transform, false);
            var rt = (RectTransform)text.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            text.text = label; text.fontSize = 28; text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center; text.color = new Color(0.25f, 0.12f, 0.02f);
            text.enableAutoSizing = true; text.fontSizeMin = 14; text.fontSizeMax = 30;
            return go.GetComponent<Button>();
        }
    }
}
