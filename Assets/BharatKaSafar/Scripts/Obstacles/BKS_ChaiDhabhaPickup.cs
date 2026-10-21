using UnityEngine;
using BharatKaSafar.Vehicle;

namespace BharatKaSafar.Obstacles
{
    /// <summary>
    /// Indian Chai Stall / Dhaba stop pickup!
    /// 
    /// Restores vehicle health (and legacy fuel if present) on contact.
    /// Provides thematic Indian touch to gameplay.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BKS_ChaiDhabhaPickup : MonoBehaviour
    {
        [Header("Restore Settings")]
        [Tooltip("Amount of vehicle health to restore")]
        [SerializeField] private float healthRestore = 35f;

        [Header("Feedback")]
        [SerializeField] private ParticleSystem pickupParticle;
        [SerializeField] private AudioSource    audioSource;
        [SerializeField] private AudioClip      pickupClip;

        private bool _used;

        private void Start()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_used) return;

            var health = other.GetComponentInParent<BKS_VehicleHealth>()
                      ?? other.GetComponent<BKS_VehicleHealth>();

            if (health == null && other.transform.root != null)
                health = other.transform.root.GetComponentInChildren<BKS_VehicleHealth>();

            if (health == null) return;

            _used = true;
            health.Heal(healthRestore);
            Debug.Log($"[BKS_ChaiDhabhaPickup] Chai stop reached! Restored {healthRestore} HP.");

            // Also restore legacy fuel if GameManager exists
            var gm = FindObjectOfType<GameManager>();
            if (gm != null)
            {
                gm.TotalFuel = 100f;
            }

            if (pickupParticle != null) pickupParticle.Play();
            if (audioSource != null && pickupClip != null) audioSource.PlayOneShot(pickupClip);

            var renderer = GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = false;

            Destroy(gameObject, 0.5f);
        }
    }
}
