using UnityEngine;
using BharatKaSafar.Missions;
using BharatKaSafar.Core;

namespace BharatKaSafar.Rajasthan
{
    /// <summary>
    /// Destination Gate trigger. When the player's vehicle enters this trigger,
    /// the active MissionManager.CompleteMission() is called.
    ///
    /// Place this at the end of each level (palace gate, fort entrance, temple, etc.)
    /// Add a BoxCollider2D set to IsTrigger on this GameObject.
    /// </summary>
    public class BKS_DestinationGate : MonoBehaviour
    {
        [Header("Visual Feedback")]
        [SerializeField] private ParticleSystem arrivalParticles;
        [SerializeField] private AudioClip      arrivalSound;
        [SerializeField] private Animator       gateAnimator;
        [Tooltip("Animation trigger name to play on arrival")]
        [SerializeField] private string         gateOpenTrigger = "Open";

        private bool _triggered;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_triggered) return;
            if (!Vehicle.BKS_VehicleUtil.IsVehicle(other.gameObject)) return;

            _triggered = true;

            Debug.Log("[BKS_DestinationGate] Player reached the destination!");

            // Play arrival FX
            if (arrivalParticles != null) arrivalParticles.Play();
            if (arrivalSound != null)     AudioSource.PlayClipAtPoint(arrivalSound, transform.position);
            if (gateAnimator  != null)    gateAnimator.SetTrigger(gateOpenTrigger);

            // Signal mission completion via MissionManager
            var mm = FindFirstObjectByType<MissionManager>();
            if (mm != null)
                mm.CompleteMission();
            else
                Debug.LogWarning("[BKS_DestinationGate] MissionManager not found in scene.");
        }
    }
}
