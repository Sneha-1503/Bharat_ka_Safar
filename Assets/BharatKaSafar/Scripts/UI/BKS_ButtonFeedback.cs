using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BharatKaSafar.UI
{
    /// <summary>
    /// Feedback for EVERY button in the game, added automatically (no scene changes):
    ///  • click sound (AudioManager's button sound, else Resources/BKS_ButtonClick), respecting the SFX volume
    ///  • the button shrinks a little while pressed and pops back
    ///  • a short golden glow on click
    /// Pedals (gas / brake) and buttons with BKS_NoButtonFeedback are skipped.
    /// </summary>
    public class BKS_ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private static AudioClip fallbackClip;
        private static AudioSource fallbackSource;
        private static readonly Color GlowColor = new Color(1f, 0.85f, 0.35f, 0.9f);

        private Button button;
        private Vector3 baseScale;
        private bool pressed;
        private bool ownSound;
        private Outline glow;
        private Coroutine glowing;

        // ── automatic install ───────────────────────────────────────────────────
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var go = new GameObject("[BKS_ButtonFeedback]");
            DontDestroyOnLoad(go);
            go.AddComponent<BKS_ButtonFeedbackInstaller>();
        }

        // ── per button ──────────────────────────────────────────────────────────
        private void Awake()
        {
            button = GetComponent<Button>();
            baseScale = transform.localScale;
            if (button != null)
            {
                // buttons that already play a sound in their own OnClick don't get a second one
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                    if ((button.onClick.GetPersistentMethodName(i) ?? "").ToLowerInvariant().Contains("sound")) ownSound = true;
                button.onClick.AddListener(OnClicked);
            }
        }

        private void OnDestroy() { if (button != null) button.onClick.RemoveListener(OnClicked); }

        public void OnPointerDown(PointerEventData e)
        {
            if (button == null || !button.IsInteractable()) return;
            if (!pressed) baseScale = transform.localScale;
            pressed = true;
            transform.localScale = baseScale * 0.94f;
        }

        public void OnPointerUp(PointerEventData e) => Release();
        public void OnPointerExit(PointerEventData e) => Release();

        private void Release()
        {
            if (!pressed) return;
            pressed = false;
            transform.localScale = baseScale;
        }

        private void OnClicked()
        {
            if (!ownSound) PlayClick();
            if (!isActiveAndEnabled) return;
            if (glowing != null) StopCoroutine(glowing);
            glowing = StartCoroutine(Glow());
        }

        private IEnumerator Glow()
        {
            // keep an Outline that something else uses (e.g. tab glow) untouched
            var existing = GetComponent<Outline>();
            bool own = existing == null || existing == glow;
            if (!own) yield break;
            if (glow == null) glow = gameObject.AddComponent<Outline>();
            glow.useGraphicAlpha = true;
            for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
            {
                float k = 1f - t / 0.35f;
                glow.enabled = true;
                glow.effectColor = new Color(GlowColor.r, GlowColor.g, GlowColor.b, GlowColor.a * k);
                glow.effectDistance = new Vector2(6f, -6f) * k;
                yield return null;
            }
            if (glow != null) glow.enabled = false;
            glowing = null;
        }

        public static void PlayClick()
        {
            var am = AudioManager.instance;
            if (am != null && am.buttonClick != null) { am.PlayButtonClickSound(); return; }
            if (fallbackClip == null) fallbackClip = Resources.Load<AudioClip>("BKS_ButtonClick");
            if (fallbackClip == null) return;
            if (fallbackSource == null)
            {
                var go = new GameObject("[BKS_ClickAudio]");
                DontDestroyOnLoad(go);
                fallbackSource = go.AddComponent<AudioSource>();
                fallbackSource.playOnAwake = false; fallbackSource.ignoreListenerPause = true;
            }
            fallbackSource.PlayOneShot(fallbackClip, PlayerPrefs.GetFloat("SFXVolume", 1f));
        }
    }
}
