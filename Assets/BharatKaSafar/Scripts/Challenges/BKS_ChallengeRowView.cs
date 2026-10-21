using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BharatKaSafar.Challenges
{
    /// <summary>One challenge row: title, progress bar "320 / 500", reward and CLAIM button.</summary>
    public class BKS_ChallengeRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Image progressFill;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private Button claimButton;
        [SerializeField] private TMP_Text claimLabel;
        [SerializeField] private Image background;
        [SerializeField] private Color readyColor = new Color(1f, 0.95f, 0.75f);
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color claimedColor = new Color(0.85f, 0.85f, 0.85f);
        [Header("Claim button colours")]
        [SerializeField] private Color buttonReady = new Color(0.20f, 0.62f, 0.30f);
        [SerializeField] private Color buttonInProgress = new Color(0.55f, 0.42f, 0.28f);
        [SerializeField] private Color buttonClaimed = new Color(0.55f, 0.55f, 0.55f);
        [Tooltip("Optional: shown only when the reward is ready (e.g. a glow behind the button)")]
        [SerializeField] private GameObject readyGlow;

        private Action onClaim;

        private void Awake()
        {
            if (claimButton != null) claimButton.onClick.AddListener(() => onClaim?.Invoke());
        }

        public void Show(BKS_ChallengeService.Entry e, Action claim)
        {
            onClaim = claim;
            int shown = Mathf.Min(e.progress, e.def.target);
            if (title != null) title.text = e.def.title;
            if (progressText != null) progressText.text = $"{shown:N0} / {e.def.target:N0}";
            if (progressFill != null) progressFill.fillAmount = e.def.target > 0 ? shown / (float)e.def.target : 0f;
            if (rewardText != null) rewardText.text = BKS_ChallengeService.RewardText(e.def);
            if (claimLabel != null) claimLabel.text = e.claimed ? "DONE" : e.Complete ? "CLAIM" : "IN PROGRESS";
            if (claimButton != null) claimButton.interactable = e.Complete && !e.claimed;
            if (background != null) background.color = e.claimed ? claimedColor : e.Complete ? readyColor : normalColor;
            if (claimButton != null && claimButton.targetGraphic != null)
                claimButton.targetGraphic.color = e.claimed ? buttonClaimed : e.Complete ? buttonReady : buttonInProgress;
            if (readyGlow != null) readyGlow.SetActive(e.Complete && !e.claimed);
        }
    }
}
