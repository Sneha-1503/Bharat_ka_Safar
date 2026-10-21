using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.Core;

namespace BharatKaSafar.Rewards
{
    /// <summary>
    /// HUD "2X" button. Uses one 2X Boost bought in the Store (BKS_SaveManager.Boost2XCount) to double
    /// collected coins and score for Duration seconds. The button only shows how many boosts are left;
    /// the shared 2X timer (button + road pickups) is shown on the top-bar Multiplier Badge (BKS_GameplayHUD).
    /// </summary>
    public class BKS_DoubleRewardButton : MonoBehaviour
    {
        public static BKS_DoubleRewardButton Instance { get; private set; }

        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        [Header("Rules")]
        [SerializeField, Min(1f)] private float boostDuration = 10f; // (renamed so scenes saved with the old 20 s value pick up 10 s)

        [Header("Texts")]
        [Tooltip("{0} = boosts owned")]
        [SerializeField] private string readyFormat = "2X  x{0}";
        [SerializeField] private string noneText = "2X  (Store)";
        [Tooltip("Off = the button never shows the timer (the top-bar badge does)")]
        [SerializeField] private bool showTimer = false;
        [SerializeField] private string activeFormat = "2X ACTIVE - {0:00}:{1:00}";

        private void Awake()
        {
            Instance = this;
            if (button != null) button.onClick.AddListener(Activate);
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Activate()
        {
            var rewards = BKS_RewardManager.Instance;
            var save = BKS_SaveManager.Instance;
            if (rewards == null || save == null || !save.UseBoost2X()) return;
            Challenges.BKS_ChallengeStats.Add(Challenges.BKS_Stat.BoostsUsed);
            rewards.ActivateMultiplier(2, boostDuration); // adds to the timer if a pickup's 2X is already running
        }

        private void Update()
        {
            var rewards = BKS_RewardManager.Instance;
            int owned = BKS_SaveManager.Instance != null ? BKS_SaveManager.Instance.Boost2XCount : 0;
            if (showTimer && rewards != null && rewards.isMultiplierActive)
            {
                float left = Mathf.Max(0f, rewards.MultiplierTimeLeft);
                Set(string.Format(activeFormat, (int)(left / 60), Mathf.CeilToInt(left % 60)), owned > 0);
            }
            else Set(owned > 0 ? string.Format(readyFormat, owned) : noneText, owned > 0);
        }

        private void Set(string text, bool interactable)
        {
            if (label != null && label.text != text) label.text = text;
            if (button != null && button.interactable != interactable) button.interactable = interactable;
        }
    }
}
