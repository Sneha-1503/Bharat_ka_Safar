using System;
using UnityEngine;
using UnityEngine.UI;

namespace BharatKaSafar.Home
{
    /// <summary>
    /// One state pin/button on the India Map.
    ///
    /// Responsibilities ONLY:
    ///   - know its state ID
    ///   - tell the Home controller when it is clicked
    ///   - show the right visual for its status + selection
    ///
    /// It contains no chapter logic. Locked and upcoming states stay clickable so the
    /// player can preview them — locking only blocks ENTER, never selection.
    ///
    /// Visual priority:
    ///   base look:  Locked/Upcoming → locked sprite, Completed → completed sprite, else normal sprite
    ///   selection:  shown ON TOP of the base look (glow object / outline / scale),
    ///               so a completed state stays green while selected.
    /// </summary>
    [DisallowMultipleComponent]
    public class MapStateButton : MonoBehaviour
    {
        [Header("Identity")]
        [Tooltip("Must match ChapterData.ChapterID, e.g. UP, RJ, TN, PB")]
        [SerializeField] private string stateId;
        [Tooltip("Shown in the panel when this state has no ChapterData yet (e.g. 'Assam').")]
        [SerializeField] private string displayName;

        [Header("References")]
        [SerializeField] private Button     button;
        [SerializeField] private Image      visualImage;
        [Tooltip("Optional extra lock icon (shown for Locked / Upcoming).")]
        [SerializeField] private GameObject lockIcon;
        [Tooltip("Optional glow/ring object shown while selected.")]
        [SerializeField] private GameObject selectionGlow;
        [Tooltip("Optional outline effect enabled while selected (works without extra art).")]
        [SerializeField] private Outline    selectionOutline;

        [Header("Sprites (empty = keep the button's current sprite)")]
        [SerializeField] private Sprite normalSprite;
        [Tooltip("Optional: used instead of Normal when an unlocked (not completed) state is selected.")]
        [SerializeField] private Sprite selectedSprite;
        [SerializeField] private Sprite completedSprite;
        [SerializeField] private Sprite lockedSprite;
        [Tooltip("Optional: falls back to Locked sprite if empty.")]
        [SerializeField] private Sprite upcomingSprite;

        [Header("Tint fallback (used only when that state's sprite is missing)")]
        [SerializeField] private Color lockedTint    = new Color(0.35f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color completedTint = new Color(0.45f, 1f, 0.45f, 1f);

        [Header("Selection")]
        [Tooltip("Scale applied while selected. 1 = no scaling.")]
        [SerializeField] private float selectedScale = 1.12f;

        // ── Runtime ───────────────────────────────────────────────────────────────
        private Action<string> _onClicked;
        private Sprite  _originalSprite;
        private Vector3 _originalScale = Vector3.one;
        private bool    _cached;

        public string StateId     => stateId != null ? stateId.Trim() : string.Empty;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? StateId : displayName;

        private void Reset()
        {
            button      = GetComponent<Button>();
            visualImage = GetComponent<Image>();
        }

        private void Awake()
        {
            CacheDefaults();
        }

        private void CacheDefaults()
        {
            if (_cached) return;
            _cached = true;

            if (button == null)      button      = GetComponent<Button>();
            if (visualImage == null) visualImage = GetComponent<Image>();

            _originalSprite = visualImage != null ? visualImage.sprite : null;
            _originalScale  = transform.localScale;
        }

        /// <summary>Called once by the Home controller. Registers the click listener in code.</summary>
        public void Initialize(Action<string> onClicked)
        {
            CacheDefaults();
            _onClicked = onClicked;

            if (button == null)
            {
                Debug.LogWarning($"[MapStateButton] '{name}' has no Button component.", this);
                return;
            }

            button.onClick.RemoveListener(HandleClick); // never register twice
            button.onClick.AddListener(HandleClick);
            button.interactable = true;                  // locked states must stay clickable
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick()
        {
            _onClicked?.Invoke(StateId);
        }

        /// <summary>Apply the visual for this status. Selection is layered on top.</summary>
        public void SetVisual(ChapterStatus status, bool selected)
        {
            CacheDefaults();

            // 1) Base look
            if (visualImage != null)
            {
                Sprite sprite = null;
                Color  tint   = Color.white;

                switch (status)
                {
                    case ChapterStatus.Upcoming:
                        sprite = upcomingSprite != null ? upcomingSprite : lockedSprite;
                        if (sprite == null) tint = lockedTint;
                        break;

                    case ChapterStatus.Locked:
                        sprite = lockedSprite;
                        if (sprite == null) tint = lockedTint;
                        break;

                    case ChapterStatus.Completed:
                        sprite = completedSprite;
                        if (sprite == null) tint = completedTint;
                        break;

                    default: // Unlocked
                        sprite = (selected && selectedSprite != null) ? selectedSprite : normalSprite;
                        break;
                }

                visualImage.sprite = sprite != null ? sprite : (normalSprite != null ? normalSprite : _originalSprite);
                visualImage.color  = tint;
            }

            if (lockIcon != null)
                lockIcon.SetActive(status == ChapterStatus.Locked || status == ChapterStatus.Upcoming);

            // 2) Selection layer (independent of base look)
            if (selectionGlow != null)    selectionGlow.SetActive(selected);
            if (selectionOutline != null) selectionOutline.enabled = selected;
            transform.localScale = selected ? _originalScale * selectedScale : _originalScale;
        }

#if UNITY_EDITOR
        /// <summary>Used by the Home setup utility.</summary>
        public void ConfigureForEditor(string id, string label, Button btn, Image visual, Outline outline,
                                       Sprite normal, Sprite selected, Sprite completed, Sprite locked)
        {
            stateId          = id;
            displayName      = label;
            button           = btn;
            visualImage      = visual;
            selectionOutline = outline;
            if (normal    != null) normalSprite    = normal;
            if (selected  != null) selectedSprite  = selected;
            if (completed != null) completedSprite = completed;
            if (locked    != null) lockedSprite    = locked;
        }
#endif
    }
}
