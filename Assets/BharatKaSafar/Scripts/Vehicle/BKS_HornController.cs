using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BharatKaSafar.Vehicle
{
    /// <summary>
    /// Horn controller — signature mechanic of Bharat Ka Safar.
    ///
    /// Placed on the vehicle root (same as CarController or as a child).
    /// Manages:
    ///   - Horn button input (called from UI Button event)
    ///   - 2D forward detection zone (OverlapCircle)
    ///   - Finding valid BKS_HornInteractable obstacles
    ///   - Awarding Safar Stars via BKS_SafarStarTracker
    ///   - Horn upgrade level increases effective range and power
    ///
    /// Does NOT modify CarController. Only reads vehicle's facing direction.
    /// </summary>
    public class BKS_HornController : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fires on every horn press, regardless of success.</summary>
        public event Action OnHornPressed;

        /// <summary>Fires when a horn interaction successfully clears an obstacle.</summary>
        public event Action<GameObject> OnHornSuccess;

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Detection Zone")]
        [SerializeField] private Transform detectionOrigin;
        [Tooltip("Base radius of horn detection zone in world units")]
        [SerializeField] private float baseDetectionRadius = 3f;
        [Tooltip("Additional radius per horn upgrade level")]
        [SerializeField] private float radiusPerHornLevel = 0.5f;

        [Header("Layers")]
        [Tooltip("Optional. Leave as 'Nothing' to check every layer (horn animals are found by their component).")]
        [SerializeField] private LayerMask hornInteractableLayer;

        [Header("Horn Cooldown")]
        [Tooltip("Minimum seconds between horn presses")]
        [SerializeField] private float hornCooldown = 0.5f;

        [Header("Audio")]
        [SerializeField] private AudioSource hornAudioSource;
        [SerializeField] private AudioClip   hornClip;

        // ── Internal References (Auto-found) ─────────────────────────────────────
        private Core.BKS_SafarStarTracker _starTracker;
        private int   _hornLevel;
        private float _cooldownTimer;

        // ── Properties ────────────────────────────────────────────────────────────
        // Vehicles are ~6.5 units long, so obstacles must be heard from well ahead of the car
        private const float MinimumRadius = 9f;
        public float CurrentRadius => Mathf.Max(baseDetectionRadius, MinimumRadius) + (radiusPerHornLevel * _hornLevel);

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Start()
        {
            _starTracker = FindObjectOfType<Core.BKS_SafarStarTracker>();

            if (Core.BKS_SaveManager.Instance != null)
                _hornLevel = Core.BKS_SaveManager.Instance.HornLevel;

            // Fallback: use own transform as detection origin
            if (detectionOrigin == null)
                detectionOrigin = transform;

            // most vehicle prefabs have no horn clip set → use the game's horn sound (Resources/BKS_Horn)
            if (hornClip == null) hornClip = Resources.Load<AudioClip>("BKS_Horn");

            // Make sure the horn can be heard even if no AudioSource was set up on the prefab
            // CarController treats a source on the chassis as a looping engine.
            if (hornClip != null && (hornAudioSource == null || hornAudioSource.GetComponent<CarController>() != null))
            {
                var speaker = new GameObject("Horn Audio");
                speaker.transform.SetParent(transform, false);
                hornAudioSource = speaker.AddComponent<AudioSource>();
            }
            if (hornAudioSource != null)
            {
                hornAudioSource.playOnAwake = false;
                hornAudioSource.loop = false;
            }
        }

        private void Update()
        {
            if (_cooldownTimer > 0f)
                _cooldownTimer -= Time.deltaTime;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Called from UI Horn Button (via Button OnClick event).
        /// Performs detection and interaction.
        /// </summary>
        public void Honk()
        {
            // Control practice plays the sound without clearing obstacles or granting rewards.
            if (UI.BKS_DrivingTutorial.IsShowing)
            {
                OnHornPressed?.Invoke();
                PlayHornSound();
                return;
            }
            if (_cooldownTimer > 0f) return;

            _cooldownTimer = hornCooldown;
            OnHornPressed?.Invoke();

            PlayHornSound();

            // Find horn-interactable obstacles in the forward detection zone
            Collider2D[] hits = hornInteractableLayer.value != 0
                ? Physics2D.OverlapCircleAll(detectionOrigin.position, CurrentRadius, hornInteractableLayer)
                : Physics2D.OverlapCircleAll(detectionOrigin.position, CurrentRadius);

            bool interacted = false;

            foreach (var hit in hits)
            {
                // Only interact with obstacles ahead of (or around) the vehicle
                // Direction check: obstacle should be roughly in front (positive X for right-facing vehicle)
                Vector2 toObstacle = hit.transform.position - detectionOrigin.position;
                float dot = Vector2.Dot(transform.right, toObstacle.normalized);

                if (dot < 0f) continue; // obstacle is behind us — skip

                // New obstacles (barricade, crate, log, rock pile, crossing animal/cart) award their own Safar Star
                var target = hit.GetComponentInParent<Obstacles.IBKS_HornTarget>();
                if (target != null)
                {
                    if (!target.CanRespondToHorn) continue;
                    target.OnHorn(CurrentRadius);
                    interacted = true;
                    OnHornSuccess?.Invoke(hit.gameObject);
                    break;
                }

                // Older horn animals
                var interactable = hit.GetComponentInParent<Obstacles.BKS_HornInteractable>();

                if (interactable == null) continue;
                if (!interactable.IsInteractable) continue;

                // Interact!
                interactable.Interact(CurrentRadius);
                interacted = true;

                Debug.Log($"[BKS_HornController] Horn interaction with: {hit.gameObject.name}");

                // Award a Safar Star
                if (_starTracker == null) _starTracker = FindFirstObjectByType<Core.BKS_SafarStarTracker>();
                if (_starTracker != null)
                    _starTracker.CollectStar();

                OnHornSuccess?.Invoke(hit.gameObject);

                // Only interact with the closest valid obstacle per honk
                break;
            }

            if (!interacted)
                Debug.Log("[BKS_HornController] Honk! No valid interactable in range.");
        }

        /// <summary>Override horn level at runtime (called by BKS_UpgradeLoader).</summary>
        public void SetHornLevel(int level)
        {
            _hornLevel = Mathf.Max(0, level);
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private void PlayHornSound()
        {
            if (hornAudioSource == null || hornClip == null) return;
            if (hornAudioSource.isPlaying)
                hornAudioSource.Stop();
            hornAudioSource.volume = PlayerPrefs.GetFloat("SFXVolume", 1f)
                * (AudioManager.instance != null ? AudioManager.instance.maxSFXVolume : 1f);
            hornAudioSource.PlayOneShot(hornClip);
        }

        // ── Editor Gizmos ─────────────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            if (detectionOrigin == null) return;
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.35f);
            Gizmos.DrawWireSphere(detectionOrigin.position, baseDetectionRadius);
        }
    }
}
