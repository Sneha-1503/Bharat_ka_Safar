using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BharatKaSafar.Home
{
    /// <summary>
    /// Region tabs for the India map (All India / North / West / South / East / North-East).
    /// Picking a tab zooms the map onto that region's state buttons and hides the other regions' buttons.
    /// The buttons keep their on-screen size while the map is zoomed, so small regions (North-East) no longer clash.
    ///
    /// Setup (done by Bharat Ka Safar > Home > Add Map Region Tabs): this sits on "MapViewport" (a RectMask2D
    /// the size of the map), the IndiaMap is its child, and the tab buttons live in "RegionTabs".
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class BKS_MapRegionTabs : MonoBehaviour, IDragHandler, IScrollHandler
    {
        [System.Serializable]
        public class Region
        {
            public string name = "North";
            [Tooltip("State IDs in this region (MapStateButton.StateId). Empty = All India.")]
            public string[] stateIds = new string[0];
            public Button tab;
        }

        [SerializeField] private RectTransform map;
        [SerializeField] private List<Region> regions = new List<Region>();

        [Header("Zoom")]
        [Tooltip("Empty space around the region's buttons, as a fraction of the viewport")]
        [SerializeField, Range(0f, 0.4f)] private float padding = 0.12f;
        [SerializeField] private float maxZoom = 3.5f;
        [SerializeField] private float zoomSpeed = 8f;

        [Header("Touch (whole map)")]
        [Tooltip("Drag with one finger to move the map, pinch (or mouse wheel) to zoom - on every tab")]
        [SerializeField] private bool allowDragAndPinch = true;
        [Tooltip("Smallest zoom when pinching out")]
        [SerializeField] private float minZoom = 1f;

        [Header("Tab look")]
        [SerializeField] private Color tabNormal = new Color(1f, 1f, 1f, 0.75f);
        [SerializeField] private Color tabSelected = new Color(1f, 0.78f, 0.25f, 1f);
        [Tooltip("Optional tab art. When set, the selected tab uses Tab Selected Sprite and the others Tab Normal Sprite (no colour tint)")]
        [SerializeField] private Sprite tabNormalSprite;
        [SerializeField] private Sprite tabSelectedSprite;
        [SerializeField] private Color tabTextNormal = new Color(1f, 0.85f, 0.4f);
        [SerializeField] private Color tabTextSelected = new Color(0.1f, 0.15f, 0.4f);
        [SerializeField] private int startRegion = 0;

        private RectTransform viewport;
        private readonly List<(MapStateButton button, RectTransform pin, CanvasGroup group)> pins = new List<(MapStateButton, RectTransform, CanvasGroup)>();
        private float targetScale = 1f, scale = 1f;
        private Vector2 targetPos, pos, homePos;
        private int current = -1;
        private float lastPinch = -1f;

        private void Awake()
        {
            viewport = (RectTransform)transform;
            if (map == null) return;
            homePos = pos = targetPos = map.anchoredPosition;

            // put every state button on a "pin" so the pin can cancel the map zoom (the button keeps its own scale logic)
            foreach (var b in map.GetComponentsInChildren<MapStateButton>(true))
            {
                var bt = (RectTransform)b.transform;
                var pin = new GameObject(b.name + "_Pin", typeof(RectTransform)).GetComponent<RectTransform>();
                pin.SetParent(bt.parent, false);
                pin.SetSiblingIndex(bt.GetSiblingIndex());
                pin.anchorMin = pin.anchorMax = bt.anchorMin == bt.anchorMax ? bt.anchorMin : new Vector2(0.5f, 0.5f);
                pin.sizeDelta = Vector2.zero;
                pin.position = bt.position;
                bt.SetParent(pin, true);
                var group = b.GetComponent<CanvasGroup>();
                if (group == null) group = b.gameObject.AddComponent<CanvasGroup>();
                pins.Add((b, pin, group));
            }

            for (int i = 0; i < regions.Count; i++)
            {
                int k = i;
                if (regions[i].tab != null) regions[i].tab.onClick.AddListener(() => Select(k));
            }
        }

        private void Start() => Select(Mathf.Clamp(startRegion, 0, Mathf.Max(0, regions.Count - 1)), instant: true);

        /// <summary>Zoom onto region <paramref name="index"/> (0 = first tab).</summary>
        public void Select(int index) => Select(index, false);

        private void Select(int index, bool instant)
        {
            if (map == null || index < 0 || index >= regions.Count) return;
            current = index;
            var r = regions[index];
            var ids = new HashSet<string>(r.stateIds.Select(s => s.Trim()), System.StringComparer.OrdinalIgnoreCase);
            bool all = ids.Count == 0;

            // show only this region's buttons, and find their bounds in map space (at zoom 1)
            bool any = false;
            Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
            foreach (var p in pins)
            {
                // buttons switched off in the scene (e.g. J&K, Ladakh) never count
                bool inRegion = p.button.gameObject.activeSelf && (all || ids.Contains(p.button.StateId));
                p.group.alpha = inRegion ? 1f : 0f;
                p.group.interactable = p.group.blocksRaycasts = inRegion;
                if (!inRegion) continue;
                Vector2 c = PinPosInMap(p.pin);
                min = Vector2.Min(min, c); max = Vector2.Max(max, c);
                any = true;
            }

            if (all || !any) { targetScale = 1f; targetPos = homePos; }
            else
            {
                Vector2 view = viewport.rect.size;
                Vector2 box = Vector2.Max(max - min, new Vector2(60f, 60f)); // one button alone still gets a sensible zoom
                Vector2 usable = view * (1f - 2f * padding);
                targetScale = Mathf.Clamp(Mathf.Min(usable.x / box.x, usable.y / box.y), 1f, maxZoom);
                Vector2 centre = (min + max) * 0.5f;
                // map point "centre" must land in the middle of the viewport
                targetPos = Clamp(homePos - centre * targetScale, targetScale);
            }

            for (int i = 0; i < regions.Count; i++)
            {
                var tab = regions[i].tab;
                if (tab == null || tab.targetGraphic == null) continue;
                bool on = i == index;
                var img = tab.targetGraphic as Image;
                if (img != null && tabNormalSprite != null && tabSelectedSprite != null)
                {
                    img.sprite = on ? tabSelectedSprite : tabNormalSprite;
                    img.type = Image.Type.Sliced;
                    img.color = Color.white;
                    var label = tab.GetComponentInChildren<TMP_Text>(true);
                    if (label != null) label.color = on ? tabTextSelected : tabTextNormal;
                }
                else tab.targetGraphic.color = on ? tabSelected : tabNormal;
            }

            if (instant) { scale = targetScale; pos = targetPos; Apply(); }
        }

        // pin position relative to the map's pivot, in map units (local space ignores the map's own zoom)
        private Vector2 PinPosInMap(RectTransform pin) => map.InverseTransformPoint(pin.position);

        // ── touch: drag to move, pinch / wheel to zoom (whole map) ─────────────────

        public void OnDrag(PointerEventData e)
        {
            if (!allowDragAndPinch || map == null || Input.touchCount > 1) return;
            float canvasScale = viewport.lossyScale.x > 0f ? viewport.lossyScale.x : 1f;
            targetPos = Clamp(targetPos + e.delta / canvasScale, targetScale);
            pos = targetPos; Apply();
        }

        public void OnScroll(PointerEventData e)
        {
            if (allowDragAndPinch) ZoomBy(1f + e.scrollDelta.y * 0.1f);
        }

        private void ZoomBy(float factor)
        {
            float s = Mathf.Clamp(targetScale * factor, minZoom, maxZoom);
            // zoom around the middle of the viewport
            targetPos = Clamp(homePos + (targetPos - homePos) * (s / targetScale), s);
            targetScale = s;
        }

        // keep the map covering the viewport (no empty borders) at zoom s
        private Vector2 Clamp(Vector2 p, float s)
        {
            Vector2 slack = Vector2.Max(Vector2.zero, (map.rect.size * s - viewport.rect.size) * 0.5f);
            Vector2 d = p - homePos;
            return homePos + new Vector2(Mathf.Clamp(d.x, -slack.x, slack.x), Mathf.Clamp(d.y, -slack.y, slack.y));
        }

        private void Update()
        {
            if (allowDragAndPinch && Input.touchCount == 2)
            {
                float d = Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);
                if (lastPinch > 0f) ZoomBy(d / lastPinch);
                lastPinch = d;
            }
            else lastPinch = -1f;

            if (map == null || current < 0) return;
            if (Mathf.Abs(scale - targetScale) < 0.001f && (pos - targetPos).sqrMagnitude < 0.01f) return;
            float k = 1f - Mathf.Exp(-zoomSpeed * Time.unscaledDeltaTime);
            scale = Mathf.Lerp(scale, targetScale, k);
            pos = Vector2.Lerp(pos, targetPos, k);
            Apply();
        }

        private void Apply()
        {
            map.localScale = new Vector3(scale, scale, 1f);
            map.anchoredPosition = pos;
            float inv = 1f / scale;
            foreach (var p in pins) p.pin.localScale = new Vector3(inv, inv, 1f);
        }
    }
}
