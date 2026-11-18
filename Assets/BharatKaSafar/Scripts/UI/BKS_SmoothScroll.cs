using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BharatKaSafar.UI
{
    /// <summary>
    /// Smooth card scrolling for a horizontal ScrollRect (Store cards):
    ///  • longer, softer glide after a swipe and a gentle bounce at the ends
    ///  • when the swipe slows down it eases onto the nearest card (snap) instead of stopping half-way
    ///  • a slightly bigger drag threshold so tapping a card's button doesn't shake the list
    /// Add to the ScrollRect (BKS_StoreScreen does it automatically).
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class BKS_SmoothScroll : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        [Tooltip("0..1 — higher = glides longer after a swipe")]
        [Range(0.01f, 0.5f)] public float glide = 0.08f;
        [Tooltip("Ease onto the nearest card when the swipe slows down")]
        public bool snapToCards = true;
        [Tooltip("Snap starts below this speed (canvas units / s)")]
        public float snapSpeed = 400f;
        [Tooltip("How fast it settles on the card")]
        public float snapSharpness = 10f;

        private ScrollRect scroll;
        private bool dragging, snapping;
        private float target;

        private void Awake()
        {
            scroll = GetComponent<ScrollRect>();
            scroll.inertia = true;
            scroll.decelerationRate = glide;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.15f;
            scroll.scrollSensitivity = 40f;
            // a finger needs to move a little before it counts as a drag (taps on buttons stay taps)
            var es = EventSystem.current;
            if (es != null) es.pixelDragThreshold = Mathf.Max(es.pixelDragThreshold, Mathf.RoundToInt(Mathf.Max(10f, Screen.dpi * 0.06f)));
            KeepCardsOnScreen();
        }

        [Tooltip("Gap kept free at both ends of the list (canvas units), plus the phone's notch / rounded corner")]
        public float edgeGap = 50f;

        // first / last card stop clear of the screen edge and the notch (they were cut off at the list ends)
        private void KeepCardsOnScreen()
        {
            if (scroll.content == null) return;
            var layout = scroll.content.GetComponent<LayoutGroup>();
            if (layout == null) return;
            var canvas = GetComponentInParent<Canvas>();
            float k = canvas != null ? Mathf.Max(0.01f, canvas.rootCanvas.scaleFactor) : 1f;
            var safe = Screen.safeArea;
            float left = edgeGap + safe.xMin / k, right = edgeGap + (Screen.width - safe.xMax) / k;
            layout.padding.left = Mathf.Max(layout.padding.left, Mathf.RoundToInt(left));
            layout.padding.right = Mathf.Max(layout.padding.right, Mathf.RoundToInt(right));
            LayoutRebuilder.MarkLayoutForRebuild(scroll.content);
        }

        public void OnBeginDrag(PointerEventData e) { dragging = true; snapping = false; }
        public void OnEndDrag(PointerEventData e) { dragging = false; }

        private void LateUpdate()
        {
            if (!snapToCards || dragging || scroll.content == null || !scroll.horizontal) return;
            float max = scroll.content.rect.width - scroll.viewport.rect.width;
            if (max <= 1f) return;

            if (!snapping && Mathf.Abs(scroll.velocity.x) < snapSpeed)
            {
                float step = CardStep();
                if (step <= 1f) return;
                float x = -scroll.content.anchoredPosition.x;               // how far the list is scrolled
                float pad = Padding();
                target = Mathf.Clamp(Mathf.Round((x - pad) / step) * step + pad, 0f, max);
                snapping = Mathf.Abs(target - x) > 0.5f;
                if (snapping) scroll.velocity = Vector2.zero;
            }
            if (snapping)
            {
                var p = scroll.content.anchoredPosition;
                float x = Mathf.Lerp(-p.x, target, 1f - Mathf.Exp(-snapSharpness * Time.unscaledDeltaTime));
                if (Mathf.Abs(x - target) < 0.5f) { x = target; snapping = false; }
                scroll.content.anchoredPosition = new Vector2(-x, p.y);
            }
        }

        // card width + spacing, read from the layout
        private float CardStep()
        {
            var layout = scroll.content.GetComponent<HorizontalOrVerticalLayoutGroup>();
            float spacing = layout != null ? layout.spacing : 0f;
            foreach (RectTransform c in scroll.content)
                if (c.gameObject.activeSelf) return c.rect.width + spacing;
            return 0f;
        }

        private float Padding()
        {
            var layout = scroll.content.GetComponent<LayoutGroup>();
            return layout != null ? layout.padding.left : 0f;
        }
    }
}
