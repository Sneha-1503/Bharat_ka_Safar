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

            // Make sure the horn can be heard even if no AudioSource was set up on the prefab
            if (hornAudioSource == null && hornClip != null)
            {
                hornAudioSource = gameObject.AddComponent<AudioSource>();
                hornAudioSource.playOnAwake = false;
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
            hornAudioSource.clip = hornClip;
            hornAudioSource.volume = PlayerPrefs.GetFloat("SFXVolume", 1f)
                * (AudioManager.instance != null ? AudioManager.instance.maxSFXVolume : 1f);
            hornAudioSource.Play();
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
