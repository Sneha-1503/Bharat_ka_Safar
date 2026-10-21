using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.Missions;

namespace BharatKaSafar.UI
{
    /// <summary>
    /// One mission card in the Mission Select list. The card artwork already contains the place name,
    /// mission title and 3 empty stars; earned stars are drawn on top of those empty stars.
    /// Selected cards grow to Selected Scale.
    /// </summary>
    public class BKS_MissionCardView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image cardImage;
        [Tooltip("Gold stars placed exactly over the 3 empty stars printed on the card")]
        [SerializeField] private Image[] starFills = new Image[3];
        [Tooltip("Shown when the mission is locked (previous mission not finished)")]
        [SerializeField] private GameObject lockOverlay;
        [Tooltip("Shown when no gameplay scene exists yet for this mission")]
        [SerializeField] private GameObject comingSoonOverlay;
        [Tooltip("Glow/frame shown on the selected card")]
        [SerializeField] private GameObject selectedHighlight;
        [SerializeField] private TMP_Text fallbackTitle;

        [Header("Selection")]
        [SerializeField, Min(1f)] private float selectedScale = 1.15f;
        [SerializeField, Min(0.1f)] private float scaleSpeed = 12f;
        [SerializeField] private Color lockedTint = new Color(0.55f, 0.55f, 0.55f, 1f);

        public MissionData Mission { get; private set; }
        public bool Unlocked { get; private set; }

        private float targetScale = 1f;

        public void Setup(MissionData mission, bool unlocked, int stars, Action<BKS_MissionCardView> onClick)
        {
            Mission = mission;
            Unlocked = unlocked;
            name = "Card_" + mission.MissionID;

            if (cardImage != null)
            {
                cardImage.sprite = mission.CardSprite;
                cardImage.color = unlocked ? Color.white : lockedTint;
            }
            if (fallbackTitle != null)
            {
                fallbackTitle.text = mission.PlaceName;
                fallbackTitle.gameObject.SetActive(mission.CardSprite == null);
            }
            for (int i = 0; i < starFills.Length; i++)
                if (starFills[i] != null) starFills[i].gameObject.SetActive(i < stars);
            if (lockOverlay != null) lockOverlay.SetActive(!unlocked);
            if (comingSoonOverlay != null) comingSoonOverlay.SetActive(unlocked && !mission.IsPlayable);

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClick?.Invoke(this));
            }
            SetSelected(false, true);
        }

        public void SetSelected(bool selected, bool instant = false)
        {
            targetScale = selected ? selectedScale : 1f;
            if (selectedHighlight != null) selectedHighlight.SetActive(selected);
            if (instant) transform.localScale = Vector3.one * targetScale;
        }

        private void Update()
        {
            float s = Mathf.Lerp(transform.localScale.x, targetScale, 1f - Mathf.Exp(-scaleSpeed * Time.unscaledDeltaTime));
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
