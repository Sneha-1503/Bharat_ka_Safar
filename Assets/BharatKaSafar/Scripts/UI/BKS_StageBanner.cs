using System.Collections;
using UnityEngine;
using TMPro;

namespace BharatKaSafar.UI
{
    /// <summary>
    /// Top-of-screen notification for stage triggers ("STAGE 2: COASTAL BRIDGE" + one line of dialogue).
    /// Fades in, stays, fades out without pausing gameplay. Called by TN_StageTrigger.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class BKS_StageBanner : MonoBehaviour
    {
        public static BKS_StageBanner Instance { get; private set; }

        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField, Min(0f)] private float fadeTime = 0.3f;

        private CanvasGroup group;

        private void Awake()
        {
            Instance = this;
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Show a banner if one exists in the scene; otherwise just log.</summary>
        public static void Show(string title, string dialogue, float duration)
        {
            if (Instance == null)
            {
                Debug.Log($"[STAGE] {title}: {dialogue} (no BKS_StageBanner in scene)");
                return;
            }
            Instance.StopAllCoroutines();
            Instance.StartCoroutine(Instance.Run(title, dialogue, duration));
        }

        private IEnumerator Run(string title, string dialogue, float duration)
        {
            if (titleText != null) titleText.text = title;
            if (dialogueText != null)
            {
                dialogueText.text = dialogue;
                dialogueText.gameObject.SetActive(!string.IsNullOrEmpty(dialogue));
            }
            yield return Fade(group.alpha, 1f);
            yield return new WaitForSeconds(Mathf.Max(0.5f, duration));
            yield return Fade(1f, 0f);
        }

        private IEnumerator Fade(float from, float to)
        {
            for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / fadeTime);
                yield return null;
            }
            group.alpha = to;
        }
    }
}
