// using UnityEngine;
// using UnityEngine.SceneManagement;

// public class PageSceneLoader : MonoBehaviour
// {
//     [SerializeField] SwipeController swipeController;
//     [SerializeField] string[] sceneNames;

//     [Header("Animation Settings")]
//     [SerializeField] float zoomScale;
//     [SerializeField] float zoomDuration;
//     [SerializeField] LeanTweenType zoomEase;
//     [SerializeField] float delayBeforeSceneLoad;

//     public void LoadSceneForCurrentPage()
//     {
//         if (swipeController == null)
//         {
//             Debug.LogError("SwipeController reference is missing.");
//             return;
//         }

//         int index = swipeController.GetCurrentPageIndex();

//         if (index >= 0 && index < sceneNames.Length)
//         {
//             string sceneToLoad = sceneNames[index];

//             if (!string.IsNullOrEmpty(sceneToLoad))
//             {
//                 // Animate the current page (scale up)
//                 RectTransform currentPageRect = swipeController.GetCurrentPageRect();

//                 if (currentPageRect != null)
//                 {
//                     LeanTween.scale(currentPageRect, Vector3.one * zoomScale, zoomDuration)
//                              .setEase(zoomEase)
//                              .setOnComplete(() =>
//                              {
//                                  // Load the scene after short delay
//                                  Invoke(nameof(DelayedSceneLoad), delayBeforeSceneLoad);
//                              });
//                 }
//                 else
//                 {
//                     SceneManager.LoadScene(sceneToLoad);
//                 }
//             }
//         }
//     }

//     private void DelayedSceneLoad()
//     {
//         int index = swipeController.GetCurrentPageIndex();
//         SceneManager.LoadScene(sceneNames[index]);
//     }
// }


// Updated PageSceneLoader.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class PageSceneLoader : MonoBehaviour
{
    [SerializeField] SwipeController swipeController;
    [SerializeField] string[] sceneNames;

    [Header("Animation Settings")]
    [SerializeField] float zoomScale = 1.2f;
    [SerializeField] float zoomDuration = 0.5f;
    [SerializeField] LeanTweenType zoomEase = LeanTweenType.easeOutBack;
    [SerializeField] float delayBeforeSceneLoad = 0.2f;

    private LevelManager levelManager;

    void Start()
    {
        levelManager = FindFirstObjectByType<LevelManager>();
    }

    public void LoadSceneForCurrentPage()
    {
        if (swipeController == null)
        {
            Debug.LogError("SwipeController reference is missing.");
            return;
        }

        int index = swipeController.GetCurrentPageIndex();

        if (index >= 0 && index < sceneNames.Length)
        {
            // Check if level is unlocked through LevelManager
            if (levelManager != null && !levelManager.IsLevelUnlocked(index))
            {
                Debug.LogWarning($"Level {index} is locked. Cannot load scene.");
                return;
            }

            string sceneToLoad = sceneNames[index];

            if (!string.IsNullOrEmpty(sceneToLoad))
            {
                // Store selected level for other scripts
                PlayerPrefs.SetInt("SelectedLevel", index);
                PlayerPrefs.Save();

                // Animate the current page (scale up)
                RectTransform currentPageRect = swipeController.GetCurrentPageRect();

                if (currentPageRect != null)
                {
                    LeanTween.scale(currentPageRect, Vector3.one * zoomScale, zoomDuration)
                             .setEase(zoomEase)
                             .setOnComplete(() =>
                             {
                                 // Load the scene after short delay
                                 Invoke(nameof(DelayedSceneLoad), delayBeforeSceneLoad);
                             });
                }
                else
                {
                    SceneManager.LoadScene(sceneToLoad);
                }
            }
        }
    }

    private void DelayedSceneLoad()
    {
        int index = swipeController.GetCurrentPageIndex();
        if (index >= 0 && index < sceneNames.Length)
        {
            SceneManager.LoadScene(sceneNames[index]);
        }
    }

    // Public method to load a specific scene by level index
    public void LoadSceneByLevelIndex(int levelIndex)
    {
        if (levelIndex >= 0 && levelIndex < sceneNames.Length)
        {
            // Check if level is unlocked
            if (levelManager != null && !levelManager.IsLevelUnlocked(levelIndex))
            {
                Debug.LogWarning($"Level {levelIndex} is locked. Cannot load scene.");
                return;
            }

            // Store selected level
            PlayerPrefs.SetInt("SelectedLevel", levelIndex);
            PlayerPrefs.Save();

            // Load scene directly
            SceneManager.LoadScene(sceneNames[levelIndex]);
        }
    }
}