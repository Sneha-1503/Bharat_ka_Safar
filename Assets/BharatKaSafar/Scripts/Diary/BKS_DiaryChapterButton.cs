using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.Missions;

namespace BharatKaSafar.Diary
{
    /// <summary>A state in the Safar Diary's chapter list: name + "3/5".</summary>
    public class BKS_DiaryChapterButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text countText;

        public void Show(string stateName, int found, int total, bool selected, Color on, Color off, Action click)
        {
            if (nameText != null) nameText.text = stateName;
            if (countText != null) countText.text = $"{found}/{total}";
            if (background != null) background.color = selected ? on : off;
            // dark ink text on the parchment / gold strips
            var ink = new Color(0.36f, 0.21f, 0.08f);
            if (nameText != null) { nameText.color = ink; nameText.fontStyle = selected ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal; }
            if (countText != null) countText.color = ink;
            transform.localScale = selected ? new Vector3(1.04f, 1.04f, 1f) : Vector3.one;
            if (button != null) { button.onClick.RemoveAllListeners(); button.onClick.AddListener(() => click?.Invoke()); }
        }
    }
}
