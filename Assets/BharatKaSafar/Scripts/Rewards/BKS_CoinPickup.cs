using UnityEngine;

namespace BharatKaSafar.Rewards
{
    [RequireComponent(typeof(Collider2D))]
    public class BKS_CoinPickup : MonoBehaviour
    {
        [SerializeField] private int coinValue = 1;
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
                    BKS_RewardManager.Instance.AddCoin(coinValue);
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
                else if (AudioManager.instance != null)
                {
                    AudioManager.instance.PlayCoinSound(); // the original game's coin sound
                }

                Destroy(gameObject);
            }
        }
    }
}
