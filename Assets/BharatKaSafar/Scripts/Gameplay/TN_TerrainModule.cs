using UnityEngine;

namespace BharatKaSafar.Gameplay
{
    /// <summary>
    /// Attach this to every modular Tamil Nadu terrain prefab (e.g. TN_Terrain_Flat, TN_Terrain_SmallHill).
    /// Allows you to perfectly snap terrain pieces together without gaps or bumps!
    /// </summary>
    public class TN_TerrainModule : MonoBehaviour
    {
        [Tooltip("Place an empty GameObject exactly at the START of the terrain floor and assign it here.")]
        public Transform startPoint;
        
        [Tooltip("Place an empty GameObject exactly at the END of the terrain floor and assign it here.")]
        public Transform endPoint;

        private void OnDrawGizmos()
        {
            if (startPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(startPoint.position, 0.5f);
            }
            if (endPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(endPoint.position, 0.5f);
                
                // Draw a guide line showing the rough surface direction
                if (startPoint != null)
                {
                    Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                    Gizmos.DrawLine(startPoint.position, endPoint.position);
                }
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Snap To Previous Module")]
        private void SnapToPrevious()
        {
            // Simple helper: If you place a terrain piece in the scene, 
            // right-click this component and hit 'Snap To Previous' 
            // and it will perfectly align itself to the previous terrain's end point!
            
            int myIndex = transform.GetSiblingIndex();
            if (myIndex > 0)
            {
                Transform previousObj = transform.parent.GetChild(myIndex - 1);
                TN_TerrainModule prevModule = previousObj.GetComponent<TN_TerrainModule>();
                
                if (prevModule != null && prevModule.endPoint != null && startPoint != null)
                {
                    // Calculate offset needed
                    Vector3 offset = prevModule.endPoint.position - startPoint.position;
                    transform.position += offset;
                    UnityEditor.EditorUtility.SetDirty(this);
                }
            }
        }
#endif
    }
}
