using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BharatKaSafar.UI
{
    public class BKS_MissionBriefingView : MonoBehaviour
    {
        public TMP_Text titleText;
        public TMP_Text storyText;
        public TMP_Text objectiveText;
        public TMP_Text difficultyText;
        public Button startButton;
        public Button closeButton;

        public static BKS_MissionBriefingView Create(GameObject previousPanel, Transform fallbackParent)
        {
            var prefab = Resources.Load<BKS_MissionBriefingView>("BKS_MissionBriefingPopup");
            if (prefab == null) return null;
            var parent = previousPanel != null ? previousPanel.transform.parent : fallbackParent;
            if (previousPanel != null) previousPanel.SetActive(false);
            var popup = Instantiate(prefab, parent, false);
            popup.transform.SetAsLastSibling();
            popup.gameObject.SetActive(false);
            return popup;
        }
    }
}
