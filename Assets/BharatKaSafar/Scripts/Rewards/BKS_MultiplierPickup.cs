using UnityEngine;

namespace BharatKaSafar.Rewards
{
    [RequireComponent(typeof(Collider2D))]
    public class BKS_MultiplierPickup : MonoBehaviour
    {
        [SerializeField] private int multiplierValue = 2;
        [SerializeField] private float duration = 10f;
        [SerializeField] private ParticleSystem pickupFX;
        [SerializeField] private AudioClip pickupSound;

        private bool collected = false;

        private void Start()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected) return;

            if (other.CompareTag("Player") || other.GetComponentInParent<CarController>() != null || (other.transform.root != null && other.transform.root.CompareTag("Player")))
            {
                collected = true;

                if (BKS_RewardManager.Instance != null)
                {
                    BKS_RewardManager.Instance.ActivateMultiplier(multiplierValue, duration);
                }

                if (pickupFX != null)
                {
                    var fx = Instantiate(pickupFX, transform.position, Quaternion.identity);
                    fx.Play();
                    Destroy(fx.gameObject, 1.5f);
                }

                if (pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                }

                Destroy(gameObject);
            }
        }
    }
}
