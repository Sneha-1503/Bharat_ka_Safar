using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BharatKaSafar.Core;

namespace BharatKaSafar.UI
{
    /// <summary>
    /// India Map scene controller.
    /// Shows all states as interactive cards. Only Uttar Pradesh is unlocked initially.
    /// Other states are grayed out with a lock icon and "Coming Soon" label.
    ///
    /// For Phase 1, states are represented as simple UI buttons.
    /// Art can be swapped in later without changing this script.
    /// </summary>
    public class BKS_IndiaMapUI : MonoBehaviour
    {
        [System.Serializable]
        public class StateCard
        {
            public string       stateID;
            public Button       cardButton;
            public GameObject   lockOverlay;
            public TMP_Text     stateNameText;
            public TMP_Text     progressText;
            public ChapterData  chapterData;
        }

        [Header("State Cards")]
        [SerializeField] private StateCard[] stateCards;

        [Header("Header")]
        [SerializeField] private TMP_Text totalStarsText;
        [SerializeField] private TMP_Text totalCoinsText;

        private void Start()
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            // Update header stats
            if (BKS_SaveManager.Instance != null)
            {
                if (totalStarsText != null)
                    totalStarsText.text = $"★ {BKS_SaveManager.Instance.SafarStars}";
                if (totalCoinsText != null)
                    totalCoinsText.text = BKS_SaveManager.Instance.Coins.ToString("N0");
            }

            // Update each state card
            foreach (var card in stateCards)
            {
                bool unlocked = BKS_SaveManager.Instance != null &&
                                BKS_SaveManager.Instance.IsStateUnlocked(card.stateID);

                if (card.lockOverlay  != null) card.lockOverlay.SetActive(!unlocked);
                if (card.cardButton   != null) card.cardButton.interactable = unlocked;

                if (card.progressText != null && card.chapterData != null)
                {
                    if (unlocked)
                    {
                        // Count completed missions in this chapter
                        int completed = 0;
                        int total     = card.chapterData.MissionCount;
                        for (int i = 0; i < total; i++)
                        {
                            var m = card.chapterData.GetMission(i);
                            if (m != null && BKS_SaveManager.Instance != null &&
                                BKS_SaveManager.Instance.GetMissionBestRating(m.MissionID) > 0)
                                completed++;
                        }
                        card.progressText.text = $"{completed}/{total} Missions";
                    }
                    else
                    {
                        card.progressText.text = "Locked";
                    }
                }
            }
        }

        /// <summary>Called by Uttar Pradesh card button.</summary>
        public void OnUttarPradeshSelected()
        {
            if (BKS_SceneLoader.Instance != null)
                BKS_SceneLoader.Instance.GoToUPMissionSelect();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(BKS_SceneLoader.SCENE_UP_MISSION_SEL);
        }

        /// <summary>Called by Rajasthan card button.</summary>
        public void OnRajasthanSelected()
        {
            if (BKS_SceneLoader.Instance != null)
                BKS_SceneLoader.Instance.GoToRajasthanMissionSelect();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(BKS_SceneLoader.SCENE_RAJASTHAN_MISSION_SEL);
        }
    }
}
