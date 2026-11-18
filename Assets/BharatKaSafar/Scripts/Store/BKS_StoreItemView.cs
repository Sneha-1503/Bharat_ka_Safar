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

        [Header("Button art (store card)")]
        [SerializeField] private Sprite buySprite;
        [SerializeField] private Sprite equipSprite;
        [SerializeField] private Sprite equippedSprite;
        [SerializeField] private Sprite lockedSprite;
        [Tooltip("Card background: glows gold when this item is the equipped one")]
        [SerializeField] private Image cardImage;
        [SerializeField] private Color equippedGlow = new Color(1f, 0.85f, 0.3f, 0.95f);

        private Action onClick;

        private void Awake()
        {
            if (actionButton != null) actionButton.onClick.AddListener(() => onClick?.Invoke());
        }

        public void Show(Sprite iconSprite, string titleText, string subtitleText, string buttonText, bool interactable,
                         Action click, int level = -1, int maxLevel = 0, bool selected = false, StoreButtonArt art = StoreButtonArt.Text)
        {
            SetArt(art);
            // equipped card glows
            if (cardImage != null)
            {
                var o = cardImage.GetComponent<Outline>(); if (o == null && selected) o = cardImage.gameObject.AddComponent<Outline>();
                if (o != null) { o.enabled = selected; o.effectColor = equippedGlow; o.effectDistance = new Vector2(8f, -8f); }
            }
            if (icon != null) { icon.sprite = iconSprite; icon.enabled = iconSprite != null; icon.preserveAspect = true; }
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
            if (selectedBadge != null) selectedBadge.SetActive(selected && cardImage == null);
        }

        // the button art has its word painted on it (BUY / EQUIP / EQUIPPED / LOCKED): hide the label then.
        // Text = no art: transparent button, label shown (e.g. MAX) over the card's own orange bar.
        private void SetArt(StoreButtonArt art)
        {
            var img = actionButton != null ? actionButton.targetGraphic as Image : null;
            Sprite s = art == StoreButtonArt.Buy ? buySprite : art == StoreButtonArt.Equip ? equipSprite
                     : art == StoreButtonArt.Equipped ? equippedSprite : art == StoreButtonArt.Locked ? lockedSprite : null;
            if (img != null)
            {
                img.sprite = s; img.preserveAspect = true;
                img.color = s != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            }
            if (actionLabel != null) actionLabel.gameObject.SetActive(s == null);
            if (actionButton != null)
            {
                // no green/orange tint on the art: keep colours white, only fade a little when disabled
                var c = actionButton.colors;
                c.normalColor = c.highlightedColor = c.selectedColor = Color.white;
                c.pressedColor = new Color(0.9f, 0.9f, 0.9f); c.disabledColor = Color.white;
                actionButton.colors = c;
            }
        }
    }

    public enum StoreButtonArt { Text, Buy, Equip, Equipped, Locked }
}
