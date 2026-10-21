using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.Missions;

namespace BharatKaSafar.Diary
{
    /// <summary>Big view of one memory: token, name, place, stars earned and the place fact.</summary>
    public class BKS_DiaryDetailPopup : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image tokenImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text placeText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Image[] stars = new Image[3];
        [SerializeField] private Button closeButton;
        [SerializeField] private string lockedText = "Complete this mission to unlock this memory.";
        [SerializeField] private Color starOn = new Color(1f, 0.75f, 0.15f);
        [SerializeField] private Color starOff = new Color(0f, 0f, 0f, 0.2f);

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(() => root.SetActive(false));
            if (root != null) root.SetActive(false);
        }

        public void Show(MissionData m, bool unlocked, int starCount)
        {
            if (tokenImage != null) { tokenImage.sprite = m.MemoryTokenSprite; tokenImage.color = unlocked ? Color.white : new Color(0.15f, 0.15f, 0.15f, 0.85f); }
            if (titleText != null) titleText.text = unlocked ? m.MemoryTokenName : "Locked memory";
            if (placeText != null) placeText.text = string.IsNullOrEmpty(m.StateName) ? m.PlaceName : $"{m.PlaceName}, {m.StateName}";
            if (bodyText != null)
            {
                string story = string.IsNullOrEmpty(m.PlaceFact) ? m.StoryText : m.PlaceFact;
                bodyText.text = unlocked ? $"<b>{m.MissionName}</b>\n{story}" : lockedText;
            }
            for (int i = 0; i < stars.Length; i++) if (stars[i] != null) stars[i].color = i < starCount ? starOn : starOff;
            if (root != null) root.SetActive(true);
        }
    }
}
