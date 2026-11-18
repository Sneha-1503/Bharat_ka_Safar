using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BharatKaSafar.Home;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Puts every state button (pin) on the middle of its state on the India map art
    /// ("Embossed Colourful Map of India", read by hand: positions are pixels on a 700 x 874 copy of the art).
    /// Each pin is anchored to that point of the IndiaMap image, so it stays on its state for any size / zoom.
    /// Menu: Bharat Ka Safar > Home > Place Map Pins On States
    /// </summary>
    public static class BKS_MapPinPlacer
    {
        private const string ScenePath = "Assets/BharatKaSafar/Scenes/Home/BKS_StateMap.unity";
        private const float ArtW = 700f, ArtH = 874f;
        // pin tip a little above the state's middle so the marker sits on it
        private const float Lift = 0.018f;

        private static readonly Dictionary<string, Vector2> Points = new Dictionary<string, Vector2>(System.StringComparer.OrdinalIgnoreCase)
        {
            {"JK",new Vector2(235,62)}, {"LA",new Vector2(185,100)}, {"HP",new Vector2(242,150)}, {"PB",new Vector2(188,170)},
            {"UK",new Vector2(292,196)}, {"HR",new Vector2(205,222)}, {"UttarPradesh",new Vector2(318,272)}, {"UP",new Vector2(318,272)},
            {"RJ",new Vector2(140,272)}, {"GJ",new Vector2(80,385)}, {"MP",new Vector2(255,372)},
            {"BR",new Vector2(432,318)}, {"JH",new Vector2(412,368)}, {"WB",new Vector2(478,385)}, {"OD",new Vector2(418,455)},
            {"CG",new Vector2(345,430)}, {"MH",new Vector2(200,462)}, {"TG",new Vector2(282,520)}, {"AP",new Vector2(268,592)},
            {"KA",new Vector2(180,612)}, {"GA",new Vector2(141,582)}, {"KL",new Vector2(208,716)}, {"TN",new Vector2(248,700)},
            {"SK",new Vector2(490,268)}, {"AR",new Vector2(618,258)}, {"AS",new Vector2(560,316)}, {"ML",new Vector2(546,338)},
            {"NL",new Vector2(612,316)}, {"MN",new Vector2(606,352)}, {"TR",new Vector2(560,377)}, {"MZ",new Vector2(586,398)},
        };

        [MenuItem("Bharat Ka Safar/Home/Place Map Pins On States")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var map = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<RectTransform>(true)).FirstOrDefault(t => t.name == "IndiaMap");
            if (map == null) { Debug.LogError("[Pins] No IndiaMap in BKS_StateMap."); return; }
            int placed = 0; var missing = new List<string>();
            foreach (var b in map.GetComponentsInChildren<MapStateButton>(true))
            {
                if (!Points.TryGetValue(b.StateId, out var px)) { missing.Add(b.StateId); continue; }
                var rt = (RectTransform)b.transform;
                // keep the pin a child of the map itself (not a sub-group) so the anchor means "this point of the map"
                if (rt.parent != map) rt.SetParent(map, true);
                var size = rt.rect.size;
                var a = new Vector2(px.x / ArtW, 1f - px.y / ArtH + Lift);
                rt.anchorMin = rt.anchorMax = a;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = size;
                rt.anchoredPosition = Vector2.zero;
                EditorUtility.SetDirty(rt);
                placed++;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Pins] {placed} state pins placed on their states." + (missing.Count > 0 ? " No point for: " + string.Join(", ", missing) : ""));
        }
    }
}
