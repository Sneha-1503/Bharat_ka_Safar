using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BharatKaSafar.UI
{
    /// <summary>Scrolls existing authored cards; mission bindings remain in the selection controller.</summary>
    [ExecuteAlways]
    public class BKS_MissionCarousel : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IScrollHandler
    {
        [SerializeField] private float cardWidth = 740f;
        [SerializeField] private float cardHeight = 650f;
        [SerializeField] private float cardGap = 48f;
        [SerializeField] private float focusedScale = 1.18f;
        [SerializeField] private float sideScale = 0.9f;
        [SerializeField] private float snapSpeed = 12f;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private List<RectTransform> authoredCards = new List<RectTransform>();
        [SerializeField, Min(0)] private int previewCard;
        private readonly List<RectTransform> cards = new List<RectTransform>();
        private ScrollRect scroll;
        private RectTransform viewport;
        private RectTransform content;
        private float spacing;
        private float width;
        private float height;
        private bool dragging;
        private float lastScrollTime = -10f;
        private int targetIndex;

        private void OnValidate()
        {
            width = -1f; // Refresh sizing when Inspector values change in Play Mode too.
        }

        public static BKS_MissionCarousel Setup(List<GameObject> roots)
        {
            roots.RemoveAll(root => root == null || !root.activeSelf);
            if (roots.Count == 0) return null;
            var parent = roots[0].transform.parent;
            var carousel = roots[0].GetComponentInParent<BKS_MissionCarousel>();
            if (carousel == null) carousel = parent.gameObject.AddComponent<BKS_MissionCarousel>();
            carousel.Initialize(roots);
            return carousel;
        }

        private void Initialize(List<GameObject> roots)
        {
            viewport = (RectTransform)transform;
            var image = GetComponent<Image>();
            if (image == null) image = gameObject.AddComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;
            if (GetComponent<RectMask2D>() == null) gameObject.AddComponent<RectMask2D>();
            scroll = GetComponent<ScrollRect>();
            if (scroll == null) scroll = gameObject.AddComponent<ScrollRect>();
            content = contentRoot;
            if (content == null)
            {
                content = new GameObject("Carousel Content", typeof(RectTransform)).GetComponent<RectTransform>();
                content.SetParent(viewport, false);
            }
            cards.Clear();
            content.anchorMin = content.anchorMax = new Vector2(0, 0.5f);
            content.pivot = new Vector2(0, 0.5f);
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;
            scroll.scrollSensitivity = 55f;
            foreach (var root in roots)
            {
                var card = (RectTransform)root.transform;
                cards.Add(card);
                card.SetParent(content, false);
                card.anchorMin = card.anchorMax = new Vector2(0, 0.5f);
                card.pivot = new Vector2(0.5f, 0.5f);
            }
            Canvas.ForceUpdateCanvases();
            Layout();
            targetIndex = Mathf.Clamp(previewCard, 0, cards.Count - 1);
            content.anchoredPosition = new Vector2(-targetIndex * spacing, 0);
            ApplyCardScales();
        }

        private void Layout()
        {
            width = Mathf.Max(1f, viewport.rect.width);
            height = viewport.rect.height;
            // Fit both axes together so resizing the simulator preserves the card shape.
            float focus = Mathf.Max(1f, focusedScale);
            float baseWidth = Mathf.Max(1f, cardWidth);
            float baseHeight = Mathf.Max(1f, cardHeight);
            float fit = Mathf.Min(1f, width * 0.55f / (baseWidth * focus),
                Mathf.Max(1f, height) * 0.94f / (baseHeight * focus));
            float w = baseWidth * fit;
            float h = baseHeight * fit;
            spacing = w * Mathf.Max(1f, focusedScale) + cardGap;
            content.sizeDelta = new Vector2(width + spacing * (cards.Count - 1), Mathf.Max(1f, viewport.rect.height));
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].sizeDelta = new Vector2(w, h);
                cards[i].anchoredPosition = new Vector2(width * 0.5f + i * spacing, 0);
            }
        }

        // A side-card click focuses it. A second click opens its existing briefing.
        public bool Focus(GameObject root)
        {
            int index = cards.FindIndex(card => card.gameObject == root);
            if (index < 0) return true;
            bool centred = Mathf.Abs(content.anchoredPosition.x + index * spacing) < 8f;
            targetIndex = index;
            dragging = false;
            lastScrollTime = -10f;
            scroll.StopMovement();
            return centred;
        }

        public void OnBeginDrag(PointerEventData data) { dragging = true; }
        public void OnEndDrag(PointerEventData data)
        {
            dragging = false;
            targetIndex = Nearest();
            lastScrollTime = -10f;
        }
        public void OnScroll(PointerEventData data) { lastScrollTime = Time.unscaledTime; }
        private int Nearest() => Mathf.Clamp(Mathf.RoundToInt(-content.anchoredPosition.x / spacing), 0, cards.Count - 1);

        private void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                Preview();
                return;
            }
            if (content == null || cards.Count == 0) return;
            if (Mathf.Abs(width - viewport.rect.width) > 1f || Mathf.Abs(height - viewport.rect.height) > 1f) Layout();
            if (Time.unscaledTime - lastScrollTime < 0.18f) targetIndex = Nearest();
            if (!dragging && Time.unscaledTime - lastScrollTime >= 0.18f)
            {
                float target = -targetIndex * spacing;
                float x = Mathf.Lerp(content.anchoredPosition.x, target, 1f - Mathf.Exp(-snapSpeed * Time.unscaledDeltaTime));
                if (Mathf.Abs(x - target) < 0.5f) x = target;
                content.anchoredPosition = new Vector2(x, 0);
            }
            ApplyCardScales();
        }

        private void ApplyCardScales()
        {
            int nearest = Nearest();
            for (int i = 0; i < cards.Count; i++)
            {
                float distance = Mathf.Abs(content.anchoredPosition.x + i * spacing) / spacing;
                float scale = Mathf.Lerp(focusedScale, sideScale, Mathf.Clamp01(distance));
                cards[i].localScale = new Vector3(scale, scale, 1);
            }
            cards[nearest].SetAsLastSibling();
        }

        private void Preview()
        {
            if (contentRoot == null || authoredCards.Count == 0) return;
            viewport = (RectTransform)transform;
            content = contentRoot;
            cards.Clear();
            foreach (var card in authoredCards) if (card != null) cards.Add(card);
            if (cards.Count == 0 || viewport.rect.width <= 0) return;
            Layout();
            int index = Mathf.Clamp(previewCard, 0, cards.Count - 1);
            content.anchoredPosition = new Vector2(-index * spacing, 0);
            ApplyCardScales();
        }
    }
}
