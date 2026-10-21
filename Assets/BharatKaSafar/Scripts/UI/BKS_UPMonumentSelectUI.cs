using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BharatKaSafar.Core;

namespace BharatKaSafar.UI
{
    /// <summary>
    /// Monument Select UI for Uttar Pradesh.
    /// Displays famous landmarks (Taj Mahal, Varanasi Ghats, Sarnath).
    /// Selecting a monument opens the Mission Select screen populated for that monument.
    /// </summary>
    public class BKS_UPMonumentSelectUI : MonoBehaviour
    {
        [System.Serializable]
        public class MonumentCard
        {
            public GameObject    root;
            public TMP_Text      nameText;
            public TMP_Text      descText;
            public Image         monumentImage;
            public Button        selectButton;
            [HideInInspector] public MonumentData boundMonument;
        }

        [Header("Chapter Data")]
        [SerializeField] private ChapterData chapterData;

        [Header("Header Elements")]
        [SerializeField] private TMP_Text stateTitleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text totalCoinsText;
        [SerializeField] private TMP_Text totalStarsText;

        [Header("Monument Cards (Auto-populated or Pre-wired)")]
        [SerializeField] private List<MonumentCard> monumentCards = new List<MonumentCard>();

        [Header("Navigation")]
        [SerializeField] private Button backButton;

        private void Start()
        {
            SetupHeader();
            PopulateMonuments();

            if (backButton != null)
                backButton.onClick.AddListener(OnBackPressed);
        }

        private void SetupHeader()
        {
            if (chapterData != null)
            {
                if (stateTitleText != null)
                    stateTitleText.text = chapterData.StateName.ToUpper();
                if (subtitleText != null)
                    subtitleText.text = chapterData.Subtitle;
            }

            if (BKS_SaveManager.Instance != null)
            {
                if (totalCoinsText != null)
                    totalCoinsText.text = BKS_SaveManager.Instance.Coins.ToString("N0");
                if (totalStarsText != null)
                    totalStarsText.text = $"★ {BKS_SaveManager.Instance.SafarStars}";
            }
        }

        private void PopulateMonuments()
        {
            IReadOnlyList<MonumentData> monuments = null;
            if (chapterData != null && chapterData.Monuments != null)
            {
                monuments = chapterData.Monuments;
            }

            int count = monuments != null ? monuments.Count : 0;

            for (int i = 0; i < monumentCards.Count; i++)
            {
                var card = monumentCards[i];
                if (i < count)
                {
                    var mon = monuments[i];
                    card.boundMonument = mon;

                    if (card.root != null) card.root.SetActive(true);
                    if (card.nameText != null) card.nameText.text = mon.monumentName;
                    if (card.descText != null) card.descText.text = mon.shortDescription;
                    if (card.monumentImage != null && mon.monumentImage != null)
                        card.monumentImage.sprite = mon.monumentImage;

                    if (card.selectButton != null)
                    {
                        card.selectButton.onClick.RemoveAllListeners();
                        card.selectButton.onClick.AddListener(() => OnMonumentSelected(mon));
                    }
                }
                else
                {
                    if (card.root != null) card.root.SetActive(false);
                }
            }
        }

        private void OnMonumentSelected(MonumentData monument)
        {
            if (BKS_SceneLoader.Instance != null)
            {
                BKS_SceneLoader.Instance.ActiveMonument = monument;
                BKS_SceneLoader.Instance.GoToUPMissionSelect();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(BKS_SceneLoader.SCENE_UP_MISSION_SEL);
            }
        }

        private void OnBackPressed()
        {
            if (BKS_SceneLoader.Instance != null)
                BKS_SceneLoader.Instance.GoToIndiaMap();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(BKS_SceneLoader.SCENE_INDIA_MAP);
        }
    }
}
