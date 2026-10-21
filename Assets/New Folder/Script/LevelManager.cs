// // LevelManager.cs
// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;
// using System.Collections;

// public class LevelManager : MonoBehaviour
// {
//     [Header("Level Configuration")]
//     [SerializeField] private LevelData[] levels = new LevelData[6];
    
//     [Header("UI References")]
//     [SerializeField] private SwipeController swipeController;
//     [SerializeField] private PageSceneLoader sceneLoader;
//     [SerializeField] private Button startButton;
//     [SerializeField] private TextMeshProUGUI startButtonText;
//     [SerializeField] private TextMeshProUGUI coinDisplayText;
    
//     [Header("Lock UI Panel")]
//     [SerializeField] private GameObject lockPanel;
//     [SerializeField] private GameObject lockIcon;
//     [SerializeField] private TextMeshProUGUI lockCostText;
    
//     [Header("Not Enough Coins Panel")]
//     [SerializeField] private GameObject notEnoughCoinsPanel;
//     [SerializeField] private TextMeshProUGUI notEnoughCoinsText;
//     [SerializeField] private float notEnoughCoinsPanelDuration = 3f;
    
//     [Header("Animation Settings")]
//     [SerializeField] private float bounceTime = 0.5f;
    
//     private int currentLevelIndex = 0;
//     private int currentCoins;
    
//     // Default unlock costs for each level (level 0 is always unlocked)
//     private int[] defaultUnlockCosts = { 0, 100, 100, 100, 100, 100 };
    
//     void Start()
//     {
//         InitializeLevels();
//         UpdateCoinDisplay();
//         SetupUI();
        
//         if (swipeController != null)
//         {
//             // Subscribe to page change events if available
//             currentLevelIndex = swipeController.GetCurrentPageIndex();
//             UpdateLevelUI();
//         }
//     }
    
//     void InitializeLevels()
//     {
//         for (int i = 0; i < levels.Length; i++)
//         {
//             if (levels[i] == null)
//             {
//                 levels[i] = new LevelData();
//             }
            
//             levels[i].levelIndex = i;
//             levels[i].isUnlocked = PlayerPrefs.GetInt($"Level_{i}_Unlocked", i == 0 ? 1 : 0) == 1;
//             levels[i].unlockCost = defaultUnlockCosts[i];
//         }
//     }
    
//     void SetupUI()
//     {
//         if (startButton != null)
//         {
//             startButton.onClick.AddListener(OnStartButtonClicked);
//         }
//     }
    
//     void Update()
//     {
//         // Check if the current page has changed
//         if (swipeController != null)
//         {
//             int newIndex = swipeController.GetCurrentPageIndex();
//             if (newIndex != currentLevelIndex)
//             {
//                 currentLevelIndex = newIndex;
//                 UpdateLevelUI();
//             }
//         }
//     }
    
//     void UpdateLevelUI()
//     {
//         if (currentLevelIndex < 0 || currentLevelIndex >= levels.Length) return;
        
//         LevelData currentLevel = levels[currentLevelIndex];
        
//         // Update lock panel visibility
//         if (lockPanel != null)
//         {
//             lockPanel.SetActive(!currentLevel.isUnlocked);
//         }
        
//         if (lockIcon != null)
//         {
//             lockIcon.SetActive(!currentLevel.isUnlocked);
//         }
        
//         // Update lock cost text
//         if (lockCostText != null && !currentLevel.isUnlocked)
//         {
//             lockCostText.text = currentLevel.unlockCost.ToString();
//         }
        
//         // Update start button
//         UpdateStartButton(currentLevel);
//     }
    
//     void UpdateStartButton(LevelData level)
//     {
//         if (startButton == null || startButtonText == null) return;
        
//         if (level.isUnlocked)
//         {
//             startButtonText.text = "START";
//             startButton.interactable = true;
//         }
//         else
//         {
//             startButtonText.text = "UNLOCK";
//             startButton.interactable = true;
//         }
//     }
    
//     void UpdateCoinDisplay()
//     {
//         currentCoins = PlayerPrefs.GetInt("Coins", 0);
//         if (coinDisplayText != null)
//         {
//             coinDisplayText.text = currentCoins.ToString();
//         }
//     }
    
//     public void OnStartButtonClicked()
//     {
//         if (currentLevelIndex < 0 || currentLevelIndex >= levels.Length) return;
        
//         LevelData currentLevel = levels[currentLevelIndex];
        
//         if (currentLevel.isUnlocked)
//         {
//             // Start the level
//             StartLevel();
//         }
//         else
//         {
//             // Try to unlock the level
//             TryUnlockLevel(currentLevel);
//         }
//     }
    
//     void TryUnlockLevel(LevelData level)
//     {
//         UpdateCoinDisplay(); // Refresh coin count
        
//         if (currentCoins >= level.unlockCost)
//         {
//             // Player has enough coins, unlock the level
//             UnlockLevel(level);
//         }
//         else
//         {
//             // Not enough coins, show warning
//             ShowNotEnoughCoinsPanel(level.unlockCost);
//         }
//     }
    
//     void UnlockLevel(LevelData level)
//     {
//         // Deduct coins
//         currentCoins -= level.unlockCost;
//         PlayerPrefs.SetInt("Coins", currentCoins);
        
//         // Unlock the level
//         level.isUnlocked = true;
//         PlayerPrefs.SetInt($"Level_{level.levelIndex}_Unlocked", 1);
//         PlayerPrefs.Save();
        
//         // Play unlock sound if AudioManager exists
//         if (AudioManager.instance != null)
//         {
//             AudioManager.instance.PlayButtonClickSound(); // Or add a specific unlock sound
//         }
        
//         // Update UI
//         UpdateCoinDisplay();
//         UpdateLevelUI();
        
//         // Optional: Add some unlock animation or effect here
//         StartCoroutine(ShowUnlockEffect());
//     }
    
//     IEnumerator ShowUnlockEffect()
//     {
//         // Simple scale animation for unlock effect
//         if (lockPanel != null)
//         {
//             Vector3 originalScale = lockPanel.transform.localScale;
            
//             // Scale up then down
//             LeanTween.scale(lockPanel, originalScale * 1.2f, bounceTime * 0.5f)
//                 .setEase(LeanTweenType.easeOutBack)
//                 .setOnComplete(() => {
//                     LeanTween.scale(lockPanel, originalScale, bounceTime * 0.5f)
//                         .setEase(LeanTweenType.easeInBack);
//                 });
//         }
        
//         yield return new WaitForSeconds(bounceTime);
//     }
    
//     void ShowNotEnoughCoinsPanel(int requiredCoins)
//     {
//         if (notEnoughCoinsPanel != null)
//         {
//             // Update the text
//             if (notEnoughCoinsText != null)
//             {
//                 int needed = requiredCoins - currentCoins;
//                 notEnoughCoinsText.text = $"Not enough coins!\nYou need {needed} more coins to unlock this level.";
//             }
            
//             // Show panel with animation
//             notEnoughCoinsPanel.SetActive(true);
//             notEnoughCoinsPanel.transform.localScale = Vector3.zero;
            
//             LeanTween.scale(notEnoughCoinsPanel, Vector3.one, bounceTime)
//                 .setEase(LeanTweenType.easeOutBack)
//                 .setOnComplete(() => {
//                     // Auto-hide after duration
//                     StartCoroutine(HideNotEnoughCoinsPanel());
//                 });
            
//             // Play error sound if available
//             if (AudioManager.instance != null)
//             {
//                 // You might want to add a specific error sound method to AudioManager
//                 // AudioManager.instance.PlayErrorSound();
//             }
//         }
//     }
    
//     IEnumerator HideNotEnoughCoinsPanel()
//     {
//         yield return new WaitForSeconds(notEnoughCoinsPanelDuration);
        
//         if (notEnoughCoinsPanel != null && notEnoughCoinsPanel.activeInHierarchy)
//         {
//             LeanTween.scale(notEnoughCoinsPanel, Vector3.zero, bounceTime * 0.5f)
//                 .setEase(LeanTweenType.easeInBack)
//                 .setOnComplete(() => {
//                     notEnoughCoinsPanel.SetActive(false);
//                 });
//         }
//     }
    
//     void StartLevel()
//     {
//         // Set the selected level for other scripts to use
//         PlayerPrefs.SetInt("SelectedLevel", currentLevelIndex);
//         PlayerPrefs.Save();
        
//         // Use existing scene loader
//         if (sceneLoader != null)
//         {
//             sceneLoader.LoadSceneForCurrentPage();
//         }
//     }
    
//     // Public method to refresh UI (useful when returning from other scenes)
//     public void RefreshUI()
//     {
//         UpdateCoinDisplay();
//         UpdateLevelUI();
//     }
    
//     // Public method to unlock a level directly (for testing or special rewards)
//     public void UnlockLevelDirectly(int levelIndex)
//     {
//         if (levelIndex >= 0 && levelIndex < levels.Length)
//         {
//             levels[levelIndex].isUnlocked = true;
//             PlayerPrefs.SetInt($"Level_{levelIndex}_Unlocked", 1);
//             PlayerPrefs.Save();
            
//             if (levelIndex == currentLevelIndex)
//             {
//                 UpdateLevelUI();
//             }
//         }
//     }
    
//     // Public method to check if a level is unlocked
//     public bool IsLevelUnlocked(int levelIndex)
//     {
//         if (levelIndex >= 0 && levelIndex < levels.Length)
//         {
//             return levels[levelIndex].isUnlocked;
//         }
//         return false;
//     }
    
//     // Public method to get unlock cost for a level
//     public int GetUnlockCost(int levelIndex)
//     {
//         if (levelIndex >= 0 && levelIndex < levels.Length)
//         {
//             return levels[levelIndex].unlockCost;
//         }
//         return 0;
//     }
// }

// [System.Serializable]
// public class LevelData
// {
//     public int levelIndex;
//     public bool isUnlocked;
//     public int unlockCost;
    
//     public LevelData()
//     {
//         levelIndex = 0;
//         isUnlocked = false;
//         unlockCost = 0;
//     }
// }





using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LevelManager : MonoBehaviour
{
    [Header("Level Configuration")]
    [SerializeField] private LevelData[] levels = new LevelData[6];
    
    [Header("UI References - Global Elements")]
    [SerializeField] private SwipeController swipeController;
    [SerializeField] private PageSceneLoader sceneLoader;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI startButtonText;
    [SerializeField] private TextMeshProUGUI coinDisplayText;
    
    [Header("Lock UI Elements - One Array for Each Level")]
    [SerializeField] private LockUIElements[] lockUIElements = new LockUIElements[6];
    
    [Header("Not Enough Coins Panel - Global")]
    [SerializeField] private GameObject notEnoughCoinsPanel;
    [SerializeField] private TextMeshProUGUI notEnoughCoinsText;
    [SerializeField] private float notEnoughCoinsPanelDuration = 1f;
    
    [Header("Animation Settings")]
    [SerializeField] private float bounceTime = 0.5f;
    
    private int currentLevelIndex = 0;
    private int currentCoins;
    
    // Default unlock costs for each level (level 0 is always unlocked)
    private int[] defaultUnlockCosts = { 0, 500, 1000, 2000, 3500, 5000 };
    
    void Start()
    {
        InitializeLevels();
        UpdateCoinDisplay();
        SetupUI();
        
        if (swipeController != null)
        {
            currentLevelIndex = swipeController.GetCurrentPageIndex();
            UpdateLevelUI();
        }
    }
    
    void InitializeLevels()
    {
        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] == null)
            {
                levels[i] = new LevelData();
            }
            
            levels[i].levelIndex = i;
            levels[i].isUnlocked = PlayerPrefs.GetInt($"Level_{i}_Unlocked", i == 0 ? 1 : 0) == 1;
            levels[i].unlockCost = defaultUnlockCosts[i];
        }
    }
    
    void SetupUI()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
        }
    }
    
    void Update()
    {
        // Check if the current page has changed
        if (swipeController != null)
        {
            int newIndex = swipeController.GetCurrentPageIndex();
            if (newIndex != currentLevelIndex)
            {
                currentLevelIndex = newIndex;
                UpdateLevelUI();
            }
        }
    }
    
    void UpdateLevelUI()
    {
        if (currentLevelIndex < 0 || currentLevelIndex >= levels.Length) return;
        
        LevelData currentLevel = levels[currentLevelIndex];
        
        // Update current level's lock UI
        if (currentLevelIndex < lockUIElements.Length && lockUIElements[currentLevelIndex] != null)
        {
            LockUIElements lockUI = lockUIElements[currentLevelIndex];
            
            // Update lock panel visibility
            if (lockUI.lockPanel != null)
            {
                lockUI.lockPanel.SetActive(!currentLevel.isUnlocked);
            }
            
            if (lockUI.lockIcon != null)
            {
                lockUI.lockIcon.SetActive(!currentLevel.isUnlocked);
            }
            
            // Update lock cost text
            if (lockUI.lockCostText != null && !currentLevel.isUnlocked)
            {
                lockUI.lockCostText.text = currentLevel.unlockCost.ToString();
            }
        }
        
        // Update start button
        UpdateStartButton(currentLevel);
        
        // Update all level UI states (for consistency)
        UpdateAllLevelUI();
    }
    
    void UpdateAllLevelUI()
    {
        for (int i = 0; i < levels.Length; i++)
        {
            if (i < lockUIElements.Length && lockUIElements[i] != null)
            {
                LockUIElements lockUI = lockUIElements[i];
                bool isUnlocked = levels[i].isUnlocked;
                
                if (lockUI.lockPanel != null)
                    lockUI.lockPanel.SetActive(!isUnlocked);
                
                if (lockUI.lockIcon != null)
                    lockUI.lockIcon.SetActive(!isUnlocked);
                
                if (lockUI.lockCostText != null && !isUnlocked)
                    lockUI.lockCostText.text = levels[i].unlockCost.ToString();
            }
        }
    }
    
    void UpdateStartButton(LevelData level)
    {
        if (startButton == null || startButtonText == null) return;
        
        if (level.isUnlocked)
        {
            startButtonText.text = "START";
            startButton.gameObject.GetComponent<Image>().color = new Color32(82, 249, 0, 255); // Change button color to indicate start
            startButton.interactable = true;
        }
        else
        {
            startButtonText.text = "UNLOCK";
            startButton.gameObject.GetComponent<Image>().color = new Color32(0, 243, 249, 255); // Change button color to indicate unlock
            startButton.interactable = true;
        }
    }
    
    void UpdateCoinDisplay()
    {
        currentCoins = PlayerPrefs.GetInt("Coins", 0);
        if (coinDisplayText != null)
        {
            coinDisplayText.text = currentCoins.ToString();
        }
    }
    
    public void OnStartButtonClicked()
    {
        if (currentLevelIndex < 0 || currentLevelIndex >= levels.Length) return;
        
        LevelData currentLevel = levels[currentLevelIndex];
        
        if (currentLevel.isUnlocked)
        {
            // Start the level
            StartLevel();
        }
        else
        {
            // Try to unlock the level
            TryUnlockLevel(currentLevel);
        }
    }
    
    void TryUnlockLevel(LevelData level)
    {
        UpdateCoinDisplay(); // Refresh coin count
        
        if (currentCoins >= level.unlockCost)
        {
            // Player has enough coins, unlock the level
            UnlockLevel(level);
        }
        else
        {
            // Not enough coins, show warning
            ShowNotEnoughCoinsPanel(level.unlockCost);
        }
    }
    
    void UnlockLevel(LevelData level)
    {
        // Deduct coins
        currentCoins -= level.unlockCost;
        PlayerPrefs.SetInt("Coins", currentCoins);
        
        // Unlock the level
        level.isUnlocked = true;
        PlayerPrefs.SetInt($"Level_{level.levelIndex}_Unlocked", 1);
        PlayerPrefs.Save();
        
        // Play unlock sound if AudioManager exists
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayButtonClickSound(); // Or add a specific unlock sound
        }
        
        // Update UI
        UpdateCoinDisplay();
        UpdateLevelUI();
        
        // Optional: Add some unlock animation or effect here
        StartCoroutine(ShowUnlockEffect());
    }
    
    IEnumerator ShowUnlockEffect()
    {
        // Simple scale animation for unlock effect on the current level's lock panel
        if (currentLevelIndex < lockUIElements.Length && lockUIElements[currentLevelIndex] != null)
        {
            GameObject lockPanel = lockUIElements[currentLevelIndex].lockPanel;
            if (lockPanel != null)
            {
                Vector3 originalScale = lockPanel.transform.localScale;
                
                // Scale up then down
                LeanTween.scale(lockPanel, originalScale * 1.2f, bounceTime * 0.5f)
                    .setEase(LeanTweenType.easeOutBack)
                    .setOnComplete(() => {
                        LeanTween.scale(lockPanel, originalScale, bounceTime * 0.5f)
                            .setEase(LeanTweenType.easeInBack);
                    });
            }
        }
        
        yield return new WaitForSeconds(bounceTime);
    }
    
    void ShowNotEnoughCoinsPanel(int requiredCoins)
    {
        if (notEnoughCoinsPanel != null)
        {
            // Update the text
            if (notEnoughCoinsText != null)
            {
                int needed = requiredCoins - currentCoins;
                notEnoughCoinsText.text = $"Not enough coins!\nYou need {needed} more coins to unlock this level.";
            }
            
            // Show panel with animation
            notEnoughCoinsPanel.SetActive(true);
            notEnoughCoinsPanel.transform.localScale = Vector3.zero;
            
            LeanTween.scale(notEnoughCoinsPanel, Vector3.one, bounceTime)
                .setEase(LeanTweenType.easeOutBack)
                .setOnComplete(() => {
                    // Auto-hide after duration
                    StartCoroutine(HideNotEnoughCoinsPanel());
                });
            
            // Play error sound if available
            if (AudioManager.instance != null)
            {
                // You might want to add a specific error sound method to AudioManager
                // AudioManager.instance.PlayErrorSound();
            }
        }
    }
    
    IEnumerator HideNotEnoughCoinsPanel()
    {
        yield return new WaitForSeconds(notEnoughCoinsPanelDuration);
        
        if (notEnoughCoinsPanel != null && notEnoughCoinsPanel.activeInHierarchy)
        {
            LeanTween.scale(notEnoughCoinsPanel, Vector3.zero, bounceTime * 0.5f)
                .setEase(LeanTweenType.easeInBack)
                .setOnComplete(() => {
                    notEnoughCoinsPanel.SetActive(false);
                });
        }
    }
    
    void StartLevel()
    {
        // Set the selected level for other scripts to use
        PlayerPrefs.SetInt("SelectedLevel", currentLevelIndex);
        PlayerPrefs.Save();
        
        // Use existing scene loader
        if (sceneLoader != null)
        {
            sceneLoader.LoadSceneForCurrentPage();
        }
    }
    
    // Public method to refresh UI (useful when returning from other scenes)
    public void RefreshUI()
    {
        UpdateCoinDisplay();
        UpdateLevelUI();
    }
    
    // Public method to unlock a level directly (for testing or special rewards)
    public void UnlockLevelDirectly(int levelIndex)
    {
        if (levelIndex >= 0 && levelIndex < levels.Length)
        {
            levels[levelIndex].isUnlocked = true;
            PlayerPrefs.SetInt($"Level_{levelIndex}_Unlocked", 1);
            PlayerPrefs.Save();
            
            if (levelIndex == currentLevelIndex)
            {
                UpdateLevelUI();
            }
        }
    }
    
    // Public method to check if a level is unlocked
    public bool IsLevelUnlocked(int levelIndex)
    {
        if (levelIndex >= 0 && levelIndex < levels.Length)
        {
            return levels[levelIndex].isUnlocked;
        }
        return false;
    }
    
    // Public method to get unlock cost for a level
    public int GetUnlockCost(int levelIndex)
    {
        if (levelIndex >= 0 && levelIndex < levels.Length)
        {
            return levels[levelIndex].unlockCost;
        }
        return 0;
    }
}

[System.Serializable]
public class LevelData
{
    public int levelIndex;
    public bool isUnlocked;
    public int unlockCost;
    
    public LevelData()
    {
        levelIndex = 0;
        isUnlocked = false;
        unlockCost = 0;
    }
}

[System.Serializable]
public class LockUIElements
{
    [Header("Lock UI Components for this specific level")]
    public GameObject lockPanel;
    public GameObject lockIcon;
    public TextMeshProUGUI lockCostText;
}