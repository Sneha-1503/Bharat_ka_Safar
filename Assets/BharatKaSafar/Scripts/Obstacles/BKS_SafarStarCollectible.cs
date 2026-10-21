using UnityEngine;
using BharatKaSafar.Core;

namespace BharatKaSafar.Obstacles
{
    /// <summary>
    /// Safar Star Collectible item placed along the level terrain.
    /// 
    /// Driving into this pickup collects +1 Safar Star towards the mission star rating.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BKS_SafarStarCollectible : MonoBehaviour
    {
        [Header("Feedback")]
        [SerializeField] private ParticleSystem collectParticle;
        [SerializeField] private AudioSource    audioSource;
        [SerializeField] private AudioClip      collectClip;

        [Header("Floating Animation")]
        [SerializeField] private bool  animateFloat = true;
        [SerializeField] private float floatSpeed   = 2f;
        [SerializeField] private float floatHeight  = 0.25f;

        private Vector3 _startPos;
        private bool    _collected;

        private void Start()
        {
            _startPos = transform.position;
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void Update()
        {
            if (animateFloat && !_collected)
            {
                float newY = _startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
                transform.position = new Vector3(_startPos.x, newY, _startPos.z);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_collected) return;
            if (!Vehicle.BKS_VehicleUtil.IsVehicle(other.gameObject)) return;

            _collected = true;

            var tracker = FindObjectOfType<BKS_SafarStarTracker>();
            if (tracker != null)
            {
                tracker.CollectStar();
                Debug.Log($"[BKS_SafarStarCollectible] Star collected! total: {tracker.CollectedCount}");
            }

            if (collectParticle != null) collectParticle.Play();
            if (audioSource != null && collectClip != null) audioSource.PlayOneShot(collectClip);

            // Hide visual mesh/sprite
            var renderer = GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = false;

            Destroy(gameObject, 0.5f);
        }
    }
}
