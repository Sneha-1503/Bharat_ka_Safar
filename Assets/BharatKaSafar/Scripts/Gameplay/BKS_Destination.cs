using UnityEngine;
using BharatKaSafar.Missions;
using BharatKaSafar.Core;

namespace BharatKaSafar.Gameplay
{
    /// <summary>
    /// Place this at the end of each level. When the player enters, the mission is complete.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BKS_Destination : MonoBehaviour
    {
        [Header("Feedback")]
        [SerializeField] private ParticleSystem arrivalFX;
        [SerializeField] private AudioClip arrivalSound;
        [SerializeField] private Animator finishAnimator;
        [SerializeField] private string triggerName = "Open";

        private bool triggered = false;

        private void Start()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggered) return;

            if (Vehicle.BKS_VehicleUtil.IsVehicle(other.gameObject))
            {
                triggered = true;
                Debug.Log("[BKS_Destination] Destination Reached!");

                if (arrivalFX != null) arrivalFX.Play();
                if (arrivalSound != null) AudioSource.PlayClipAtPoint(arrivalSound, transform.position);
                if (finishAnimator != null) finishAnimator.SetTrigger(triggerName);

                var mm = FindFirstObjectByType<MissionManager>();
                if (mm != null)
                {
                    mm.CompleteMission();
                }
                else
                {
                    Debug.LogWarning("[BKS_Destination] MissionManager not found! Cannot complete mission.");
                }
            }
        }
    }
}
