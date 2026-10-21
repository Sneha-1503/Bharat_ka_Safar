using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Builds Resources/BKS_QuitConfirmPopup: the "leave this mission?" confirmation opened by the pause menu's Home button.
    /// TealTravel parchment frame, YES / NO buttons, message "Are you sure you want to leave this mission?".
    /// The pause menu (BKS_PausePanel) loads it in every mission, so changing this prefab changes it everywhere.
    /// Edit the prefab freely afterwards; keep the names YesButton / NoButton.
    /// Menu: Bharat Ka Safar > UI > Build Quit Confirmation Popup
    /// </summary>
    public static class BKS_QuitConfirmBuilder
    {
        private const string Art = "Assets/BharatKaSafar/Art/UI/TealTravelSprites/Engraved/";
        private const string PrefabPath = "Assets/Resources/BKS_QuitConfirmPopup.prefab";

        [MenuItem("Bharat Ka Safar/UI/Build Quit Confirmation Popup")]
        public static void Build()
        {
            var root = new GameObject("BKS_QuitConfirmPopup", typeof(RectTransform), typeof(Image));
            Stretch((RectTransform)root.transform);
            var dim = root.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f); // dims the game and blocks clicks behind

            var panel = Img("Panel", root.transform, Sprite("Teal and Gold Adventure Parchment Frame.png"));
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(1140f, 760f); // frame art is 3:2

            Text("Title", panel.transform, "LEAVE MISSION?", new Vector2(0.30f, 0.72f), new Vector2(0.70f, 0.86f), Color.white, 56f);
            Text("Message", panel.transform, "Are you sure you want to leave this mission?",
                 new Vector2(0.14f, 0.40f), new Vector2(0.86f, 0.66f), new Color(0.32f, 0.18f, 0.05f), 60f);

            Button("YesButton", panel.transform, Sprite("yes.png"), new Vector2(0.34f, 0.23f));
            Button("NoButton", panel.transform, Sprite("no.png"), new Vector2(0.66f, 0.23f));

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Debug.Log("[Quit Confirm] Built " + PrefabPath + ". The pause menu Home button uses it in every mission.");
        }

        private static Sprite Sprite(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + file);

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static GameObject Img(string name, Transform parent, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite; img.preserveAspect = true;
            return go;
        }

        private static void Text(string name, Transform parent, string text, Vector2 min, Vector2 max, Color color, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text; t.color = color; t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.enableAutoSizing = true; t.fontSizeMin = 20f; t.fontSizeMax = size;
            t.raycastTarget = false;
        }

        private static void Button(string name, Transform parent, Sprite sprite, Vector2 centre)
        {
            var go = Img(name, parent, sprite);
            go.AddComponent<Button>().targetGraphic = go.GetComponent<Image>();
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = centre;
            rt.sizeDelta = new Vector2(340f, 137f); // yes/no art is 514x207
        }
    }
}
