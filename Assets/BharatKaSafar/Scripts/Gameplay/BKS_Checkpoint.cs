using UnityEngine;

namespace BharatKaSafar.Gameplay
{
    /// <summary>
    /// A trigger that saves the player's progress when crossed.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BKS_Checkpoint : MonoBehaviour
    {
        [Tooltip("Optional spawn point. If null, uses this object's position.")]
        public Transform respawnPoint;
        
        [SerializeField] private ParticleSystem activationFX;
        [SerializeField] private AudioClip activationSound;

        private bool activated = false;

        private void Start()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (activated) return;

            if (other.CompareTag("Player") || other.GetComponentInParent<CarController>() != null || (other.transform.root != null && other.transform.root.CompareTag("Player")))
            {
                activated = true;
                
                Vector3 spawnPos = respawnPoint != null ? respawnPoint.position : transform.position;

                if (BKS_CheckpointManager.Instance != null)
                {
                    BKS_CheckpointManager.Instance.SaveCheckpoint(spawnPos);
                }

                if (activationFX != null) activationFX.Play();
                if (activationSound != null) AudioSource.PlayClipAtPoint(activationSound, transform.position);

                Debug.Log("[BKS_Checkpoint] Checkpoint reached!");
            }
        }
    }
}
