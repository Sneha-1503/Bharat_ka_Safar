using System.Collections;
using UnityEngine;

namespace BharatKaSafar.Obstacles
{
    /// <summary>
    /// Horn-interactable obstacle — Phase 2 implementation.
    ///
    /// The obstacle reacts to the horn, moves away (shrinks into background), 
    /// plays sound, and triggers particles/animations.
    /// </summary>
    public class BKS_HornInteractable : MonoBehaviour
    {
        [Header("Obstacle Config")]
        [Tooltip("How far the obstacle flees on interaction (world units)")]
        [SerializeField] private float fleeDistance = 3f;
        [Tooltip("Speed at which obstacle moves away")]
        [SerializeField] private float fleeSpeed = 8f;
        [Tooltip("Direction to flee (set in inspector or computed at runtime)")]
        [SerializeField] private Vector2 fleeDirection = new Vector2(0.5f, 1f);
        
        [Header("Feedback")]
        [SerializeField] private AudioSource reactionAudio;
        [SerializeField] private AudioClip reactionClip;
        [SerializeField] private Animator animator;
        [SerializeField] private ParticleSystem reactionParticle;

        // ── State ─────────────────────────────────────────────────────────────────
        private enum State { Idle, Fleeing, Done }
        private State _state = State.Idle;

        public bool IsInteractable => _state == State.Idle;
        
        private Transform _worldLabel;
        private Vector3 _initialScale;

        private void Start()
        {
            _initialScale = transform.localScale;
            
            // Find the floating label child to hide it when fleeing
            foreach (Transform child in transform)
            {
                if (child.name == "WorldLabel")
                {
                    _worldLabel = child;
                    break;
                }
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        public void Interact(float hornPower)
        {
            if (!IsInteractable) return;
            _state = State.Fleeing;
            Challenges.BKS_ChallengeStats.Add(Challenges.BKS_Stat.AnimalsHorned);
            StartCoroutine(FleeRoutine(hornPower));
        }

        // ── Flee Behaviour ────────────────────────────────────────────────────────

        private IEnumerator FleeRoutine(float hornPower)
        {
            // Hide the hint label
            if (_worldLabel != null) _worldLabel.gameObject.SetActive(false);

            // Play Sound
            if (reactionAudio != null && reactionClip != null)
            {
                reactionAudio.PlayOneShot(reactionClip);
            }
            else if (reactionClip != null)
            {
                AudioSource.PlayClipAtPoint(reactionClip, transform.position);
            }

            // Play Animation
            if (animator != null)
            {
                animator.SetTrigger("Flee");
            }

            // Play Particle
            if (reactionParticle != null)
            {
                reactionParticle.Play();
            }

            // Stronger horn = faster flee
            float actualDistance = fleeDistance + (hornPower * 0.2f);
            float actualSpeed = fleeSpeed + (hornPower * 0.5f);

            Vector2 startPos  = transform.position;
            Vector2 targetPos = startPos + (fleeDirection.normalized * actualDistance);
            float   elapsed   = 0f;
            float   duration  = actualDistance / Mathf.Max(0.1f, actualSpeed);

            Debug.Log($"[BKS_HornInteractable] '{gameObject.name}' fleeing from road (Horn Power: {hornPower}).");

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Move in the flee direction
                transform.position = Vector2.Lerp(startPos, targetPos, t);
                
                // Rotate slightly to simulate stumbling away
                transform.Rotate(0, 0, -180f * Time.deltaTime);
                
                // Shrink to simulate moving into the background (Z depth)
                transform.localScale = Vector3.Lerp(_initialScale, Vector3.zero, t);
                
                yield return null;
            }

            _state = State.Done;
            gameObject.SetActive(false); // Hide completely when done
        }
    }
}
