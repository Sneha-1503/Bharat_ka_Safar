using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BharatKaSafar.UI
{
    /// <summary>
    /// "Continue Journey?  Revive for 200" popup. Works while the game is paused (unscaled time).
    /// Auto-declines when the countdown runs out.
    /// </summary>
    public class BKS_RevivePopup : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text balanceText;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private Button reviveButton;
        [SerializeField] private Button declineButton;

        [Header("Texts")]
        [SerializeField] private string title = "Continue Journey?";
        [SerializeField] private string costFormat = "Revive for {0} Coins";
        [SerializeField] private string balanceFormat = "You have {0} coins";
        [SerializeField] private string notEnoughFormat = "Not enough coins ({0})";
        [SerializeField, Min(1f)] private float countdownSeconds = 8f;

        private Action onAccept, onDecline;
        private float timeLeft;
        private bool open;

        private void Awake()
        {
            UseTealPopup();
            if (reviveButton != null) reviveButton.onClick.AddListener(() => Close(onAccept));
            if (declineButton != null) declineButton.onClick.AddListener(() => Close(onDecline));
            if (root != null) root.SetActive(false);
        }

        // the shared TealTravel design (Resources/BKS_RevivePopup) replaces the panel built into each scene
        private void UseTealPopup()
        {
            var prefab = Resources.Load<GameObject>("BKS_RevivePopup");
            var canvas = GetComponentInParent<Canvas>(true);
            if (prefab == null || canvas == null) return;
            if (root != null) root.SetActive(false);
            var popup = Instantiate(prefab, canvas.rootCanvas.transform, false);
            popup.name = "BKS_RevivePopup";
            popup.transform.SetAsLastSibling();
            root = popup;
            titleText = Find<TMP_Text>(popup, "Title");
            costText = Find<TMP_Text>(popup, "Cost");
            balanceText = Find<TMP_Text>(popup, "Balance");
            countdownText = Find<TMP_Text>(popup, "Countdown");
            reviveButton = Find<Button>(popup, "ReviveButton");
            declineButton = Find<Button>(popup, "DeclineButton");
            title = title.ToUpperInvariant();
        }

        private static T Find<T>(GameObject root, string name) where T : Component
        {
            foreach (var t in root.GetComponentsInChildren<T>(true)) if (t.name == name) return t;
            return null;
        }

        public void Show(int cost, int coins, Action accept, Action decline)
        {
            onAccept = accept; onDecline = decline;
            bool canPay = coins >= cost;
            if (titleText != null) titleText.text = title;
            if (costText != null) costText.text = string.Format(costFormat, cost);
            if (balanceText != null) balanceText.text = string.Format(canPay ? balanceFormat : notEnoughFormat, coins);
            if (reviveButton != null) reviveButton.interactable = canPay;
            timeLeft = countdownSeconds;
            open = true;
            if (root != null) { root.SetActive(true); root.transform.SetAsLastSibling(); }
        }

        private void Update()
        {
            if (!open) return;
            timeLeft -= Time.unscaledDeltaTime;
            if (countdownText != null) countdownText.text = Mathf.CeilToInt(Mathf.Max(0f, timeLeft)).ToString();
            if (timeLeft <= 0f) Close(onDecline);
        }

        private void Close(Action action)
        {
            if (!open) return;
            open = false;
            if (root != null) root.SetActive(false);
            action?.Invoke();
        }
    }
}
