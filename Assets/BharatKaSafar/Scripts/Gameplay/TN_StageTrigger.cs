using UnityEngine;

namespace BharatKaSafar.Gameplay
{
    /// <summary>
    /// Place this trigger in the scene. When the vehicle passes through it, 
    /// it shows a lightweight notification (Stage 1, Stage 2, etc.) without pausing gameplay.
    /// </summary>
    public class TN_StageTrigger : MonoBehaviour
    {
        [Header("Stage Info")]
        public string stageTitle = "STAGE 1";
        [TextArea]
        public string stageDialogue = "Welcome to the new stage!";
        
        [Header("Trigger Settings")]
        public bool triggerOnce = true;
        public float displayDuration = 3f;

        private bool hasTriggered = false;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (triggerOnce && hasTriggered) return;

            if (collision.CompareTag("Car") || collision.CompareTag("Player"))
            {
                hasTriggered = true;
                
                UI.BKS_StageBanner.Show(stageTitle, stageDialogue, displayDuration);
            }
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
            BoxCollider2D box = GetComponent<BoxCollider2D>();
            if (box != null)
            {
                Gizmos.DrawCube(transform.position + (Vector3)box.offset, box.size);
            }
        }
    }
}
