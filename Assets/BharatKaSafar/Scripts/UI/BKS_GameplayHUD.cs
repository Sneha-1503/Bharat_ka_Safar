using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BharatKaSafar.Core;
using BharatKaSafar.Vehicle;

using BharatKaSafar.Rewards;

namespace BharatKaSafar.UI
{
    /// <summary>
    /// Gameplay HUD for Bharat Ka Safar.
    /// 
    /// Layout (per spec):
    ///   TOP LEFT  — Vehicle Health bar, Safar Stars count, Coins count, Score
    ///   TOP RIGHT — Pause button, 2X Multiplier Badge
    ///   BOTTOM LEFT  — Brake / Reverse button
    ///   BOTTOM RIGHT — Accelerate button
    ///   BOTTOM CENTER — Horn button (prominent, signature mechanic), Speedometer
    ///
    /// This script only reads from other systems — it does NOT drive any game logic.
    /// The CarController, BKS_VehicleHealth, etc. are the authoritative sources.
    ///
    /// NOTE: The car is spawned at runtime by the legacy StartPoint script, usually
    /// AFTER this HUD has started. So references are re-checked every
    /// <see cref="referenceRetryInterval"/> seconds until they are found.
    /// </summary>
    public class BKS_GameplayHUD : MonoBehaviour
    {
        // ── Inspector: Status Displays ────────────────────────────────────────────
        [Header("TOP LEFT — Status")]
        [SerializeField] private Slider   healthBar;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text safarStarsText;
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text scoreText;

        [Header("Multiplier")]
        [SerializeField] private GameObject multiplierBadge;
        [SerializeField] private TMP_Text   multiplierText;
        [Tooltip("Shown on the badge while no 2X is running")]
        [SerializeField] private string     multiplierIdleText   = "2X 00:00";
        [Tooltip("{0} = multiplier, {1} = minutes, {2} = seconds")]
        [SerializeField] private string     multiplierActiveFormat = "{0}X {1:00}:{2:00}";

        [Header("Speedometer (value comes from the real car velocity)")]
        [SerializeField] private TMP_Text      speedText;
        [Tooltip("{0} = speed in km/h")]
        [SerializeField] private string        speedTextFormat = "{0} km/h";
        [Tooltip("Optional dial needle. Its pivot must be at the needle's hub.")]
        [SerializeField] private RectTransform speedometerNeedle;
        [Tooltip("Needle Z rotation at 0 km/h (+90 = pointing left)")]
        [SerializeField] private float needleAngleAtZero = 90f;
        [Tooltip("Needle Z rotation at Max Dial Speed (-90 = pointing right)")]
        [SerializeField] private float needleAngleAtMax  = -90f;
        [Tooltip("Highest number printed on the dial")]
        [SerializeField] private float maxDialSpeedKmh   = 120f;
        [Tooltip("How quickly the needle follows the speed (higher = snappier)")]
        [SerializeField] private float needleSmoothing   = 10f;

        [Header("TOP RIGHT — Pause")]
        [SerializeField] private Button pauseButton;

        [Header("BOTTOM — Controls")]
        [SerializeField] private Button brakeButton;
        [SerializeField] private Button accelerateButton;
        [SerializeField] private Button hornButton;

        // ── Inspector: System References (Auto-detected if null) ──────────────────
        [Header("System References (auto-detected if empty)")]
        [SerializeField] private BKS_VehicleHealth vehicleHealth;
        [SerializeField] private BKS_SafarStarTracker starTracker;
        [SerializeField] private CarController carController;

        [Header("Auto-detect")]
        [Tooltip("How often (seconds) to look again for references that are still missing, e.g. the car spawned at runtime.")]
        [SerializeField] private float referenceRetryInterval = 0.25f;

        // ── Runtime ───────────────────────────────────────────────────────────────
        private float _retryTimer;
        private int   _lastShownSpeed = -1;
        private float _needleAngle    = float.NaN;

        // Track what we are subscribed to, so we never subscribe twice
        // and always unsubscribe from the exact object we subscribed to.
        private BKS_VehicleHealth    _subscribedHealth;
        private BKS_SafarStarTracker _subscribedTracker;
        private BKS_SaveManager      _subscribedSave;
        private BKS_RewardManager    _subscribedReward;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            FindStarTexts();
            if (healthBar != null) healthBar.gameObject.SetActive(BKS_VehicleHealth.HealthSystemEnabled);
            if (healthText != null) healthText.gameObject.SetActive(BKS_VehicleHealth.HealthSystemEnabled);
            ResolveReferences();
            SubscribeToEvents();
            RefreshAll();
        }

        private void Start()
        {
            // Some singletons (RewardManager, SaveManager) may only be ready now.
            ResolveReferences();
            SubscribeToEvents();
            RefreshAll();

            // The horn button had no OnClick in the scenes — register it here (once).
            if (hornButton != null)
            {
                hornButton.onClick.RemoveListener(OnHornPressed);
                hornButton.onClick.AddListener(OnHornPressed);
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            // Keep looking for anything that is still missing (car spawns at runtime).
            if (HasMissingReferences())
            {
                _retryTimer -= Time.unscaledDeltaTime;
                if (_retryTimer <= 0f)
                {
                    _retryTimer = referenceRetryInterval;
                    ResolveReferences();
                    SubscribeToEvents();
                    RefreshAll();
                }
            }

            UpdateSpeedometer();
        }

        // ── Reference Resolution ──────────────────────────────────────────────────

        private bool HasMissingReferences()
        {
            return carController == null
                || (BKS_VehicleHealth.HealthSystemEnabled && vehicleHealth == null)
                || starTracker == null
                || (BKS_RewardManager.Instance != null && _subscribedReward != BKS_RewardManager.Instance)
                || (BKS_SaveManager.Instance   != null && _subscribedSave   != BKS_SaveManager.Instance);
        }

        private void ResolveReferences()
        {
            if (carController == null)
                carController = FindFirstObjectByType<CarController>();

            if (vehicleHealth == null)
            {
                // Prefer the health component on the spawned car, if any.
                if (carController != null)
                    vehicleHealth = carController.GetComponentInParent<BKS_VehicleHealth>()
                                 ?? carController.GetComponentInChildren<BKS_VehicleHealth>();
                if (vehicleHealth == null)
                    vehicleHealth = FindFirstObjectByType<BKS_VehicleHealth>();
            }

            if (starTracker == null)
                starTracker = FindFirstObjectByType<BKS_SafarStarTracker>();
        }

        private void SubscribeToEvents()
        {
            if (BKS_VehicleHealth.HealthSystemEnabled && vehicleHealth != null && _subscribedHealth != vehicleHealth)
            {
                if (_subscribedHealth != null) _subscribedHealth.OnHealthChanged -= OnHealthChanged;
                vehicleHealth.OnHealthChanged += OnHealthChanged;
                _subscribedHealth = vehicleHealth;
            }

            if (starTracker != null && _subscribedTracker != starTracker)
            {
                if (_subscribedTracker != null) _subscribedTracker.OnStarCollected -= OnStarCollected;
                starTracker.OnStarCollected += OnStarCollected;
                _subscribedTracker = starTracker;
            }

            var save = BKS_SaveManager.Instance;
            if (save != null && _subscribedSave != save)
            {
                if (_subscribedSave != null) _subscribedSave.OnCoinsChanged -= OnCoinsChanged;
                save.OnCoinsChanged += OnCoinsChanged;
                _subscribedSave = save;
            }

            var reward = BKS_RewardManager.Instance;
            if (reward != null && _subscribedReward != reward)
            {
                if (_subscribedReward != null)
                {
                    _subscribedReward.OnRewardsUpdated    -= OnRewardsUpdated;
                    _subscribedReward.OnMultiplierChanged -= OnMultiplierChanged;
                }
                reward.OnRewardsUpdated    += OnRewardsUpdated;
                reward.OnMultiplierChanged += OnMultiplierChanged;
                _subscribedReward = reward;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_subscribedHealth != null)
                _subscribedHealth.OnHealthChanged -= OnHealthChanged;

            if (_subscribedTracker != null)
                _subscribedTracker.OnStarCollected -= OnStarCollected;

            if (_subscribedSave != null)
                _subscribedSave.OnCoinsChanged -= OnCoinsChanged;

            if (_subscribedReward != null)
            {
                _subscribedReward.OnRewardsUpdated    -= OnRewardsUpdated;
                _subscribedReward.OnMultiplierChanged -= OnMultiplierChanged;
            }

            _subscribedHealth  = null;
            _subscribedTracker = null;
            _subscribedSave    = null;
            _subscribedReward  = null;
        }

        // ── Speedometer ───────────────────────────────────────────────────────────

        private void UpdateSpeedometer()
        {
            float speedKmh = 0f;
            if (carController != null)
            {
                // CarController.speed = Rigidbody2D velocity magnitude in m/s.
                speedKmh = Mathf.Abs(carController.speed) * 3.6f;
            }

            // Text: only touch it when the number changes (avoids string garbage every frame).
            if (speedText != null)
            {
                int shown = Mathf.RoundToInt(speedKmh);
                if (shown != _lastShownSpeed)
                {
                    _lastShownSpeed = shown;
                    speedText.text = string.Format(speedTextFormat, shown);
                }
            }

            // Needle: map 0..maxDialSpeed onto the dial arc, smoothed.
            if (speedometerNeedle != null)
            {
                float t      = maxDialSpeedKmh > 0f ? Mathf.Clamp01(speedKmh / maxDialSpeedKmh) : 0f;
                float target = Mathf.Lerp(needleAngleAtZero, needleAngleAtMax, t);

                if (float.IsNaN(_needleAngle)) _needleAngle = target;
                else _needleAngle = Mathf.LerpAngle(_needleAngle, target, 1f - Mathf.Exp(-needleSmoothing * Time.deltaTime));

                speedometerNeedle.localRotation = Quaternion.Euler(0f, 0f, _needleAngle);
            }
        }

        // ── Refresh All ───────────────────────────────────────────────────────────

        private void RefreshAll()
        {
            // Health
            if (vehicleHealth != null)
                UpdateHealthDisplay(vehicleHealth.HealthRatio);

            // Stars
            if (starTracker != null)
                UpdateStarDisplay(starTracker.CollectedCount, starTracker.TotalCount);
            else
                UpdateStarDisplay(0, 0);

            // Coins & Score
            if (BKS_RewardManager.Instance != null)
            {
                UpdateCoinDisplay(BKS_RewardManager.Instance.currentCoins);
                UpdateScoreDisplay(BKS_RewardManager.Instance.currentScore);
                UpdateMultiplierDisplay(BKS_RewardManager.Instance.isMultiplierActive, 0f);
            }
            else
            {
                UpdateCoinDisplay(0);
            }
        }

        // ── Event Handlers ────────────────────────────────────────────────────────

        private void OnHealthChanged(float ratio)     => UpdateHealthDisplay(ratio);
        private void OnStarCollected(int cur, int tot) => UpdateStarDisplay(cur, tot);

        private void OnCoinsChanged(int coins)
        {
            // During a run the HUD shows the session coins from RewardManager.
            UpdateCoinDisplay(BKS_RewardManager.Instance != null ? BKS_RewardManager.Instance.currentCoins : 0);
        }

        private void OnRewardsUpdated()
        {
            if (BKS_RewardManager.Instance != null)
            {
                UpdateCoinDisplay(BKS_RewardManager.Instance.currentCoins);
                UpdateScoreDisplay(BKS_RewardManager.Instance.currentScore);
            }
        }

        private void OnMultiplierChanged(bool active, float timeLeft)
        {
            UpdateMultiplierDisplay(active, timeLeft);
        }

        // ── Display Updates ───────────────────────────────────────────────────────

        private void UpdateHealthDisplay(float ratio)
        {
            if (healthBar  != null) healthBar.value = ratio;
            if (healthText != null) healthText.text = Mathf.RoundToInt(ratio * 100f) + "%";
        }

        private void UpdateStarDisplay(int collected, int total)
        {
            string s = $"{collected}/{total}"; // the star icon sits next to the text; "★" is missing in LiberationSans
            if (safarStarsText != null) safarStarsText.text = s;
            if (_starTexts != null)
                foreach (var t in _starTexts) if (t != null && t != safarStarsText) t.text = s;
        }

        // The visible "0/3" in the Star Hud is a text called "StarsText"; update it too, even when the
        // Safar Stars Text field points at another text.
        private TMP_Text[] _starTexts;
        private void FindStarTexts()
        {
            if (_starTexts != null) return;
            var canvas = GetComponentInParent<Canvas>();
            var root = canvas != null ? canvas.rootCanvas.transform : transform;
            _starTexts = System.Array.FindAll(root.GetComponentsInChildren<TMP_Text>(true), t => t.name == "StarsText");
        }

        private void UpdateCoinDisplay(int coins)
        {
            if (coinsText != null)
                coinsText.text = coins.ToString("N0");
        }

        private void UpdateScoreDisplay(int score)
        {
            if (scoreText != null)
                scoreText.text = score.ToString("N0");
        }

        private void UpdateMultiplierDisplay(bool active, float timeLeft)
        {
            // The badge is the ONE 2X timer on screen (the 2X button only shows how many boosts are left).
            // It is always visible: idle text when 2X is off, a countdown while it runs.
            if (multiplierBadge != null && !multiplierBadge.activeSelf)
                multiplierBadge.SetActive(true);
            if (multiplierText == null) return;

            var reward = BKS_RewardManager.Instance;
            int secs = Mathf.CeilToInt(Mathf.Max(0f, timeLeft));
            multiplierText.text = active && reward != null
                ? string.Format(multiplierActiveFormat, reward.currentMultiplier, secs / 60, secs % 60)
                : multiplierIdleText;
        }

        // ── Button Callbacks (assigned via Inspector OnClick) ─────────────────────

        /// <summary>Called by Accelerate button (EventTrigger PointerDown).</summary>
        public void OnAccelerateDown()
        {
            if (carController != null) carController.Acceleration();
        }

        /// <summary>Called by Accelerate button (EventTrigger PointerUp).</summary>
        public void OnAccelerateUp()
        {
            if (carController != null) carController.GasBrakeRelease();
        }

        /// <summary>Called by Brake button (EventTrigger PointerDown).</summary>
        public void OnBrakeDown()
        {
            if (carController != null) carController.Brake();
        }

        /// <summary>Called by Brake button (EventTrigger PointerUp).</summary>
        public void OnBrakeUp()
        {
            if (carController != null) carController.GasBrakeRelease();
        }

        /// <summary>Called by Horn button (EventTrigger PointerDown).</summary>
        public void OnHornPressed()
        {
            var horn = FindFirstObjectByType<Vehicle.BKS_HornController>();
            if (horn != null) horn.Honk();
        }

        /// <summary>Called by Pause button.</summary>
        public void OnPausePressed()
        {
            if (BKS_PausePanel.Instance != null) BKS_PausePanel.Instance.Toggle();
            else Time.timeScale = Time.timeScale > 0f ? 0f : 1f;
        }
    }
}
