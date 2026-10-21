#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BharatKaSafar.UI;
using BharatKaSafar.Core;

namespace BharatKaSafar.EditorTools
{
    public static class BKS_IndiaMapBuilder
    {
        private const string SCENE_PATH = "Assets/BharatKaSafar/Scenes/BKS_IndiaMap.unity";
        private const string UP_CHAPTER_SO = "Assets/BharatKaSafar/ScriptableObjects/chapters/UP_UttarPradesh.asset";
        private const string RAJ_CHAPTER_SO = "Assets/BharatKaSafar/ScriptableObjects/chapters/RajasthanChapter.asset";

        [MenuItem("Bharat Ka Safar/Build India Map UI", false, 3)]
        public static void BuildIndiaMapUI()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

            // Find or create Canvas
            GameObject canvasGo = GameObject.Find("IndiaMapCanvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("IndiaMapCanvas");
                var c = canvasGo.AddComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            // Clear old children
            for (int i = canvasGo.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(canvasGo.transform.GetChild(i).gameObject);
            }

            // Background
            var bg = CreatePanel("Background", canvasGo.transform, new Color(0.1f, 0.15f, 0.25f));
            StretchFull(bg);

            // Title
            var title = CreateTMP("Title", canvasGo.transform, "BHARAT KA SAFAR", 60, Color.white);
            SetRect(title, new Vector2(0f, 0.8f), new Vector2(1f, 1f));

            // Container for State Cards
            var container = new GameObject("StateCardsContainer", typeof(RectTransform));
            container.transform.SetParent(canvasGo.transform, false);
            SetRect(container, new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.75f));
            var hlg = container.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 50;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlHeight = true;
            hlg.childControlWidth = true;

            // Load ChapterData
            var upChapter = AssetDatabase.LoadAssetAtPath<ChapterData>(UP_CHAPTER_SO);
            var rajChapter = AssetDatabase.LoadAssetAtPath<ChapterData>(RAJ_CHAPTER_SO);

            var mapUI = canvasGo.GetComponent<BKS_IndiaMapUI>();
            if (mapUI == null) mapUI = canvasGo.AddComponent<BKS_IndiaMapUI>();

            // Create UP Card
            var upCard = CreateStateCard("UP_Card", container.transform, "UTTAR PRADESH", new Color(0.2f, 0.6f, 0.3f));
            
            // Create Rajasthan Card
            var rajCard = CreateStateCard("Rajasthan_Card", container.transform, "RAJASTHAN", new Color(0.8f, 0.4f, 0.1f));

            // Wire Serialized Object
            SerializedObject so = new SerializedObject(mapUI);
            var cardsProp = so.FindProperty("stateCards");
            cardsProp.arraySize = 2;

            // Wire UP
            var elem0 = cardsProp.GetArrayElementAtIndex(0);
            elem0.FindPropertyRelative("stateID").stringValue = "UttarPradesh";
            elem0.FindPropertyRelative("cardButton").objectReferenceValue = upCard.btn;
            elem0.FindPropertyRelative("lockOverlay").objectReferenceValue = upCard.lockOverlay;
            elem0.FindPropertyRelative("stateNameText").objectReferenceValue = upCard.nameText;
            elem0.FindPropertyRelative("progressText").objectReferenceValue = upCard.progText;
            elem0.FindPropertyRelative("chapterData").objectReferenceValue = upChapter;

            // Wire Rajasthan
            var elem1 = cardsProp.GetArrayElementAtIndex(1);
            elem1.FindPropertyRelative("stateID").stringValue = "Rajasthan";
            elem1.FindPropertyRelative("cardButton").objectReferenceValue = rajCard.btn;
            elem1.FindPropertyRelative("lockOverlay").objectReferenceValue = rajCard.lockOverlay;
            elem1.FindPropertyRelative("stateNameText").objectReferenceValue = rajCard.nameText;
            elem1.FindPropertyRelative("progressText").objectReferenceValue = rajCard.progText;
            elem1.FindPropertyRelative("chapterData").objectReferenceValue = rajChapter;

            so.ApplyModifiedProperties();

            // Set up button listeners using UnityEvents
            UnityEditor.Events.UnityEventTools.AddPersistentListener(upCard.btn.onClick, mapUI.OnUttarPradeshSelected);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(rajCard.btn.onClick, mapUI.OnRajasthanSelected);

            // Add EventSystem if missing
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            AssetDatabase.Refresh();
            
            Debug.Log("[BKS_IndiaMapBuilder] Built India Map UI with UP and Rajasthan cards!");
        }

        private static (Button btn, GameObject lockOverlay, TMP_Text nameText, TMP_Text progText) CreateStateCard(string objName, Transform parent, string titleStr, Color bgColor)
        {
            var go = CreatePanel(objName, parent, bgColor);
            go.AddComponent<LayoutElement>().preferredWidth = 400;
            var btn = go.AddComponent<Button>();

            var title = CreateTMP("StateName", go.transform, titleStr, 40, Color.white);
            SetRect(title, new Vector2(0f, 0.6f), new Vector2(1f, 1f));

            var prog = CreateTMP("Progress", go.transform, "0/5 Missions", 24, new Color(0.8f, 1f, 0.8f));
            SetRect(prog, new Vector2(0f, 0.4f), new Vector2(1f, 0.6f));

            var lockOverlay = CreatePanel("LockOverlay", go.transform, new Color(0, 0, 0, 0.8f));
            StretchFull(lockOverlay);
            var lockTxt = CreateTMP("LockText", lockOverlay.transform, "LOCKED", 36, Color.red);
            StretchFull(lockTxt);

            return (btn, lockOverlay, title.GetComponent<TMP_Text>(), prog.GetComponent<TMP_Text>());
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static GameObject CreateTMP(string name, Transform parent, string text, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            return go;
        }

        private static void StretchFull(GameObject go)
        {
            SetRect(go, Vector2.zero, Vector2.one);
        }

        private static void SetRect(GameObject go, Vector2 min, Vector2 max)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
#endif
