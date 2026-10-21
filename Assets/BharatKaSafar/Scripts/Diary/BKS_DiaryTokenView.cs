using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.Missions;

namespace BharatKaSafar.Diary
{
    /// <summary>One memory token in the Diary grid. Locked tokens are a dark silhouette with "?".</summary>
    public class BKS_DiaryTokenView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image tokenImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text placeText;
        [SerializeField] private GameObject lockedMark;
        [SerializeField] private Color lockedTint = new Color(0.15f, 0.15f, 0.15f, 0.85f);

        public void Show(MissionData m, bool unlocked, Action<MissionData> click)
        {
            if (tokenImage != null) { tokenImage.sprite = m.MemoryTokenSprite; tokenImage.color = unlocked ? Color.white : lockedTint; }
            if (nameText != null) nameText.text = unlocked ? m.MemoryTokenName : "???";
            if (placeText != null) placeText.text = m.PlaceName;
            if (lockedMark != null) lockedMark.SetActive(!unlocked);
            if (button != null) { button.onClick.RemoveAllListeners(); button.onClick.AddListener(() => click?.Invoke(m)); }
        }
    }
}
