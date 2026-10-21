using UnityEngine;
using BharatKaSafar.Core;

namespace BharatKaSafar
{
    /// <summary>
    /// Persistent manager bootstrapper.
    ///
    /// Place ONE instance of this prefab in the FIRST scene that loads
    /// (BKS_IndiaMap or a dedicated Bootstrap scene).
    ///
    /// It ensures BKS_SaveManager and BKS_SceneLoader are alive for the
    /// entire session without DontDestroyOnLoad conflicts.
    ///
    /// This object uses a "one instance survives" pattern:
    ///   - First instance → stays alive
    ///   - Subsequent instances → destroy themselves
    /// </summary>
    public class BKS_PersistentManagers : MonoBehaviour
    {
        private static BKS_PersistentManagers _instance;

        [Header("Auto-created if not already in scene")]
        [SerializeField] private BKS_SaveManager  saveManagerPrefab;
        [SerializeField] private BKS_SceneLoader  sceneLoaderPrefab;

        private void Awake()
        {
            if (_instance != null)
            {
                // Already initialized — destroy this duplicate
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Ensure SaveManager exists
            if (BKS_SaveManager.Instance == null)
            {
                if (saveManagerPrefab != null)
                    Instantiate(saveManagerPrefab);
                else
                    new GameObject("BKS_SaveManager").AddComponent<BKS_SaveManager>();
            }

            // Ensure SceneLoader exists
            if (BKS_SceneLoader.Instance == null)
            {
                if (sceneLoaderPrefab != null)
                    Instantiate(sceneLoaderPrefab);
                else
                    new GameObject("BKS_SceneLoader").AddComponent<BKS_SceneLoader>();
            }

            Debug.Log("[BKS_PersistentManagers] Core managers initialized.");
        }
    }
}
