using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BharatKaSafar.Store
{
    /// <summary>One card in the Store (vehicle, upgrade or 2X boost pack).</summary>
    public class BKS_StoreItemView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private Button actionButton;
        [SerializeField] private TMP_Text actionLabel;
        [Tooltip("Upgrade level pips (filled = bought)")]
        [SerializeField] private Image[] levelPips = new Image[0];
        [SerializeField] private GameObject selectedBadge;
        [SerializeField] private Color pipOn = new Color(1f, 0.7f, 0.15f);
        [SerializeField] private Color pipOff = new Color(1f, 1f, 1f, 0.25f);

        private Action onClick;

        private void Awake()
        {
            if (actionButton != null) actionButton.onClick.AddListener(() => onClick?.Invoke());
        }

        public void Show(Sprite iconSprite, string titleText, string subtitleText, string buttonText, bool interactable,
                         Action click, int level = -1, int maxLevel = 0, bool selected = false)
        {
            if (icon != null) { icon.sprite = iconSprite; icon.enabled = iconSprite != null; }
            if (title != null) title.text = titleText;
            if (subtitle != null) subtitle.text = subtitleText;
            if (actionLabel != null) actionLabel.text = buttonText;
            if (actionButton != null) actionButton.interactable = interactable;
            onClick = click;
            for (int i = 0; i < levelPips.Length; i++)
            {
                if (levelPips[i] == null) continue;
                levelPips[i].gameObject.SetActive(level >= 0 && i < maxLevel);
                levelPips[i].color = i < level ? pipOn : pipOff;
            }
            if (selectedBadge != null) selectedBadge.SetActive(selected);
        }
    }
}
