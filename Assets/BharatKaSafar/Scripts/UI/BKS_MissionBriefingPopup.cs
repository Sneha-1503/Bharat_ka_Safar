using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.Missions;

namespace BharatKaSafar.UI
{
    /// <summary>
    /// Mission Briefing shown before a mission starts: story/objective, what to collect,
    /// obstacles and how to deal with them, and rewards (stars, coins, memory token).
    /// </summary>
    public class BKS_MissionBriefingPopup : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform panel;

        [Header("Texts")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text placeText;
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private TMP_Text collectibleText;
        [SerializeField] private TMP_Text challengesText;
        [SerializeField] private TMP_Text rewardsText;

        [Header("Icons")]
        [SerializeField] private Image collectibleIcon;
        [SerializeField] private Image memoryTokenIcon;

        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private TMP_Text startButtonLabel;
        [SerializeField] private Button closeButton;

        [Header("Text formats")]
        [SerializeField] private string collectibleFormat = "Collect: {0} x{1}";
        [SerializeField] private string noCollectibleText = "";
        [SerializeField] private string rewardsFormat = "Rewards: up to 3 Stars  |  {0} Coins  |  Memory Token: {1}";
        [SerializeField] private string startLabel = "START";
        [SerializeField] private string comingSoonLabel = "COMING SOON";

        private Action<MissionData> onStart;
        private MissionData mission;

        private void Awake()
        {
            if (startButton != null) startButton.onClick.AddListener(() => { if (mission != null && mission.IsPlayable) onStart?.Invoke(mission); });
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (root != null) root.SetActive(false);
        }

        public void Show(MissionData m, Action<MissionData> startAction)
        {
            mission = m;
            onStart = startAction;
            Set(titleText, m.MissionName);
            Set(placeText, string.IsNullOrEmpty(m.StateName) ? m.PlaceName : $"{m.PlaceName}, {m.StateName}");
            Set(objectiveText, string.IsNullOrEmpty(m.StoryText) ? m.PrimaryObjectiveText : m.StoryText);

            bool hasCollectible = m.CollectibleCount > 0;
            Set(collectibleText, hasCollectible ? string.Format(collectibleFormat, m.CollectibleName, m.CollectibleCount) : noCollectibleText);
            // missions without a collectible hide the whole row
            if (collectibleText != null) collectibleText.gameObject.SetActive(hasCollectible || !string.IsNullOrEmpty(noCollectibleText));
            if (collectibleIcon != null)
            {
                collectibleIcon.sprite = m.CollectibleSprite;
                collectibleIcon.gameObject.SetActive(hasCollectible && m.CollectibleSprite != null);
            }

            string challenges = string.IsNullOrEmpty(m.HowToDealText) ? m.ObstaclesText : m.HowToDealText;
            string stars = m.UsesPlannerRating
                ? $"<b>Stars:</b>  1 = Finish   |   2 = {m.ObjectiveRuleText}   |   3 = {m.BonusRuleText}\n"
                : "";
            Set(challengesText, stars + (string.IsNullOrEmpty(challenges) ? "" : "Challenges:\n" + challenges));
            Set(rewardsText, string.Format(rewardsFormat, m.CoinReward, string.IsNullOrEmpty(m.MemoryTokenName) ? "—" : m.MemoryTokenName));
            if (memoryTokenIcon != null)
            {
                memoryTokenIcon.sprite = m.MemoryTokenSprite;
                memoryTokenIcon.gameObject.SetActive(m.MemoryTokenSprite != null);
            }

            if (startButton != null) startButton.interactable = m.IsPlayable;
            Set(startButtonLabel, m.IsPlayable ? startLabel : comingSoonLabel);

            if (root != null) root.SetActive(true);
            if (panel != null) { StopAllCoroutines(); StartCoroutine(Pop()); }
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }

        private static void Set(TMP_Text t, string value)
        {
            if (t != null) t.text = value ?? "";
        }

        private System.Collections.IEnumerator Pop()
        {
            const float time = 0.25f;
            for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
            {
                float k = t / time - 1f;
                panel.localScale = Vector3.one * (1f + 2.70158f * k * k * k + 1.70158f * k * k);
                yield return null;
            }
            panel.localScale = Vector3.one;
        }
    }
}
