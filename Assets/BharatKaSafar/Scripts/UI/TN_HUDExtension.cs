using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BharatKaSafar.Rewards;
using BharatKaSafar.Core;
using BharatKaSafar.Vehicle;

namespace BharatKaSafar.UI
{
    /// <summary>
    /// Standalone controller for the 3 new UI elements added to TN scenes:
    ///   1. Speedometer needle rotation
    ///   2. Star counter (live update)
    ///   3. 2X Double badge (activates when multiplier is active)
    ///
    /// Attach this to any GameObject in your Canvas.
    /// It auto-detects all required references — no manual linking needed!
    /// </summary>
    public class TN_HUDExtension : MonoBehaviour
    {
        [Header("=== SPEEDOMETER ===")]
        [Tooltip("The needle Image that rotates to show speed. Assign the needle RectTransform.")]
        [SerializeField] private RectTransform speedometerNeedle;
        [Tooltip("Text showing km/h value")]
        [SerializeField] private TMP_Text speedText;
        [Tooltip("Rotation at 0 km/h (degrees). Usually around -130")]
        [SerializeField] private float minNeedleAngle = -130f;
        [Tooltip("Rotation at max speed. Usually around 130")]
        [SerializeField] private float maxNeedleAngle = 130f;
        [Tooltip("Maximum speed the needle can show (km/h)")]
        [SerializeField] private float maxSpeedKmH = 150f;

        [Header("=== STAR COUNTER ===")]
        [Tooltip("Text showing collected/total stars. e.g. '3 / 10'")]
        [SerializeField] private TMP_Text starCountText;
        [Tooltip("Optional: panel that pulses when a star is collected")]
        [SerializeField] private GameObject starPanel;

        [Header("=== 2X DOUBLE BADGE ===")]
        [Tooltip("The whole 2X badge GameObject. Will show/hide based on multiplier state.")]
        [SerializeField] private GameObject doubleBadge;
        [Tooltip("Optional: timer text inside the badge showing how many seconds left")]
        [SerializeField] private TMP_Text doubleTimerText;

        // ── Auto-detected references ──────────────────────────────────────────────
        private CarController _car;
        private BKS_SafarStarTracker _starTracker;

        // ── Internal state ────────────────────────────────────────────────────────
        private bool _multiplierActive = false;

        // ─────────────────────────────────────────────────────────────────────────

        private void Start()
        {
            // Auto-detect
            _car         = FindObjectOfType<CarController>();
            _starTracker = FindObjectOfType<BKS_SafarStarTracker>();

            // Subscribe to star events
            if (_starTracker != null)
                _starTracker.OnStarCollected += OnStarCollected;

            // Subscribe to multiplier events
            if (BKS_RewardManager.Instance != null)
                BKS_RewardManager.Instance.OnMultiplierChanged += OnMultiplierChanged;

            // Hide badge at start
            if (doubleBadge != null) doubleBadge.SetActive(false);

            // Initial star display
            RefreshStars();
        }

        private void OnDestroy()
        {
            if (_starTracker != null)
                _starTracker.OnStarCollected -= OnStarCollected;

            if (BKS_RewardManager.Instance != null)
                BKS_RewardManager.Instance.OnMultiplierChanged -= OnMultiplierChanged;
        }

        // ─────────────────────────────────────────────────────────────────────────

        private void Update()
        {
            UpdateSpeedometer();
        }

        // ── 1. SPEEDOMETER ────────────────────────────────────────────────────────

        private void UpdateSpeedometer()
        {
            if (_car == null) return;

            float speedKmH = Mathf.Abs(_car.speed * 3.6f);
            float t        = Mathf.Clamp01(speedKmH / maxSpeedKmH);

            // Rotate needle
            if (speedometerNeedle != null)
            {
                float angle = Mathf.Lerp(minNeedleAngle, maxNeedleAngle, t);
                speedometerNeedle.localRotation = Quaternion.Euler(0f, 0f, -angle);
            }

            // Update speed text
            if (speedText != null)
                speedText.text = $"{Mathf.RoundToInt(speedKmH)} km/h";
        }

        // ── 2. STAR COUNTER ───────────────────────────────────────────────────────

        private void OnStarCollected(int collected, int total)
        {
            RefreshStars();

            // Pulse animation on the star panel
            if (starPanel != null)
            {
                LeanTween.cancel(starPanel);
                starPanel.transform.localScale = Vector3.one;
                LeanTween.scale(starPanel, Vector3.one * 1.25f, 0.12f)
                    .setEase(LeanTweenType.easeOutQuad)
                    .setOnComplete(() =>
                        LeanTween.scale(starPanel, Vector3.one, 0.12f)
                            .setEase(LeanTweenType.easeInQuad));
            }
        }

        private void RefreshStars()
        {
            if (starCountText == null) return;

            if (_starTracker != null)
                starCountText.text = $"{_starTracker.CollectedCount}";
            else
                starCountText.text = "0";
        }

        // ── 3. 2X DOUBLE BADGE ────────────────────────────────────────────────────

        private void OnMultiplierChanged(bool isActive, float timeLeft)
        {
            // The 2X button shows the shared timer; keep one timer on screen
            if (BharatKaSafar.Rewards.BKS_DoubleRewardButton.Instance != null) isActive = false;
            _multiplierActive = isActive;

            if (doubleBadge != null)
            {
                if (isActive && !doubleBadge.activeSelf)
                {
                    // Pop-in animation
                    doubleBadge.SetActive(true);
                    doubleBadge.transform.localScale = Vector3.zero;
                    LeanTween.scale(doubleBadge, Vector3.one, 0.3f).setEase(LeanTweenType.easeOutBack);
                }
                else if (!isActive && doubleBadge.activeSelf)
                {
                    // Pop-out animation, then hide
                    LeanTween.scale(doubleBadge, Vector3.zero, 0.2f)
                        .setEase(LeanTweenType.easeInBack)
                        .setOnComplete(() => doubleBadge.SetActive(false));
                }
            }

            // Show countdown timer inside badge
            if (doubleTimerText != null)
            {
                doubleTimerText.gameObject.SetActive(isActive);
                if (isActive)
                    doubleTimerText.text = $"{(int)(timeLeft / 60):00}:{(int)(timeLeft % 60):00}";
            }
        }
    }
}
