// using UnityEngine;
// using UnityEngine.EventSystems;
// using UnityEngine.UI;
// using System.Collections;
// public class SwipeController : MonoBehaviour, IEndDragHandler
// {
//     [SerializeField] int maxPage;
//     int currentPage;
//     Vector3 targetPos;
//     [SerializeField] Vector3 pageStep;
//     [SerializeField] RectTransform levelPagesRect;

//     [Header("Swipe Animation")]
//     [SerializeField] float tweenTime = 0.5f;
//     [SerializeField] LeanTweenType swipeTweenType = LeanTweenType.easeOutExpo;

//     [Header("Page Pop Effect")]
//     [SerializeField] float scaleUpSize = 1.2f;
//     [SerializeField] float scaleDuration = 0.2f;
//     [SerializeField] LeanTweenType scaleUpEaseType = LeanTweenType.easeOutBack;

//     float dragThreshold;

//     RectTransform[] pages;
//     PageAnimationHandler[] pageAnimators;

//     private void Awake()
//     {
//         currentPage = 1;
//         targetPos = levelPagesRect.localPosition;
//         dragThreshold = Screen.width / 15f;

//         // Cache all pages and their animators
//         int childCount = levelPagesRect.childCount;
//         pages = new RectTransform[childCount];
//         pageAnimators = new PageAnimationHandler[childCount];

//         for (int i = 0; i < childCount; i++)
//         {
//             pages[i] = levelPagesRect.GetChild(i).GetComponent<RectTransform>();
//             pageAnimators[i] = pages[i].GetComponent<PageAnimationHandler>();
//         }

//         // PlayPopEffect();
//     }


//     private IEnumerator Start()
//     {
//         // Wait one frame to ensure layout is settled
//         yield return null;

//         // Optional: force rebuild just to be safe
//         LayoutRebuilder.ForceRebuildLayoutImmediate(levelPagesRect);

//         // Then play the pop and vehicle effect
//         PlayPopEffect();
//     }

//     // private void Start()
//     // {
//     //     PlayPopEffect();
//     // }

//     public void OnEndDrag(PointerEventData eventData)
//     {
//         if (Mathf.Abs(eventData.position.x - eventData.pressPosition.x) > dragThreshold)
//         {
//             if (eventData.position.x > eventData.pressPosition.x)
//             {
//                 if (currentPage > 1)
//                 {
//                     currentPage--;
//                     targetPos -= pageStep;
//                 }
//             }
//             else
//             {
//                 if (currentPage < maxPage)
//                 {
//                     currentPage++;
//                     targetPos += pageStep;
//                 }
//             }
//         }

//         MovePage();
//     }

//     void MovePage()
//     {
//         levelPagesRect.LeanMoveLocal(targetPos, tweenTime).setEase(swipeTweenType);
//         PlayPopEffect();
//     }

//     void PlayPopEffect()
//     {
//         for (int i = 0; i < pages.Length; i++)
//         {
//             if (pages[i] != null)
//             {
//                 LeanTween.cancel(pages[i].gameObject);
//                 pages[i].LeanScale(Vector3.one, scaleDuration).setEase(scaleUpEaseType);

//                 if (pageAnimators[i] != null)
//                     pageAnimators[i].Deactivate();
//             }
//         }

//         int index = currentPage - 1;
//         if (index >= 0 && index < pages.Length)
//         {
//             RectTransform currentPageRect = pages[index];
//             currentPageRect.LeanScale(Vector3.one * scaleUpSize, scaleDuration).setEase(scaleUpEaseType);

//             if (pageAnimators[index] != null)
//                 pageAnimators[index].Activate();
//         }
//     }

//     public int GetCurrentPageIndex()
//     {
//         return currentPage - 1; // Pages start at 1, array at 0
//     }

//     public RectTransform GetCurrentPageRect()
//     {
//         int index = currentPage - 1;
//         if (index >= 0 && index < pages.Length)
//             return pages[index];
//         return null;
//     }


// }




using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SwipeController : MonoBehaviour
{
    [Header("Swipe Settings")]
    [SerializeField] private RectTransform[] pages;
    [SerializeField] private float swipeThreshold = 50f;
    [SerializeField] private float snapSpeed = 10f;
    [SerializeField] private ScrollRect scrollRect;
    
    [Header("Page Indicators (Optional)")]
    [SerializeField] private GameObject[] pageIndicators;
    [SerializeField] private Color activeIndicatorColor = Color.white;
    [SerializeField] private Color inactiveIndicatorColor = Color.gray;
    
    [Header("Animation")]
    [SerializeField] private PageAnimationHandler[] pageAnimations;
    
    [Header("Page Pop Effect")]
    [SerializeField] private float scaleUpSize = 1.2f;
    [SerializeField] private float scaleDuration = 0.2f;
    [SerializeField] private LeanTweenType scaleUpEaseType = LeanTweenType.easeOutBack;
    
    private int currentPageIndex = 0;
    private Vector2 startTouchPosition;
    private bool isDragging = false;
    private LevelManager levelManager;
    private Vector3[] originalScales;
    
    void Start()
    {
        levelManager = FindFirstObjectByType<LevelManager>();
        
        // Store original scales for all pages
        if (pages != null)
        {
            originalScales = new Vector3[pages.Length];
            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] != null)
                {
                    originalScales[i] = pages[i].localScale;
                }
            }
        }
        
        if (pages.Length > 0)
        {
            SnapToPage(0, true);
        }
        
        UpdatePageIndicators();
        UpdateAnimations();
        
        // Add a small delay before playing initial pop effect
        StartCoroutine(DelayedInitialPopEffect());
    }
    
    IEnumerator DelayedInitialPopEffect()
    {
        yield return new WaitForSeconds(0.1f);
        PlayPopEffect();
    }
    
    void Update()
    {
        HandleInput();
    }
    
    void HandleInput()
    {
        // Handle touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    startTouchPosition = touch.position;
                    isDragging = true;
                    break;
                    
                case TouchPhase.Ended:
                    if (isDragging)
                    {
                        HandleSwipe(touch.position);
                        isDragging = false;
                    }
                    break;
            }
        }
        
        // Handle mouse input for testing in editor
        #if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            startTouchPosition = Input.mousePosition;
            isDragging = true;
        }
        else if (Input.GetMouseButtonUp(0) && isDragging)
        {
            HandleSwipe(Input.mousePosition);
            isDragging = false;
        }
        #endif
    }
    
    void HandleSwipe(Vector2 endPosition)
    {
        Vector2 swipeDirection = endPosition - startTouchPosition;
        
        if (Mathf.Abs(swipeDirection.x) > swipeThreshold)
        {
            if (swipeDirection.x > 0)
            {
                // Swipe right - go to previous page
                GoToPreviousPage();
            }
            else
            {
                // Swipe left - go to next page
                GoToNextPage();
            }
        }
    }
    
    public void GoToNextPage()
    {
        if (currentPageIndex < pages.Length - 1)
        {
            SnapToPage(currentPageIndex + 1);
        }
    }
    
    public void GoToPreviousPage()
    {
        if (currentPageIndex > 0)
        {
            SnapToPage(currentPageIndex - 1);
        }
    }
    
    public void GoToPage(int pageIndex)
    {
        if (pageIndex >= 0 && pageIndex < pages.Length)
        {
            SnapToPage(pageIndex);
        }
    }
    
    void SnapToPage(int pageIndex, bool immediate = false)
    {
        if (pageIndex < 0 || pageIndex >= pages.Length) return;
        
        currentPageIndex = pageIndex;
        
        if (scrollRect != null)
        {
            float targetPosition = (float)pageIndex / (pages.Length - 1);
            
            if (immediate)
            {
                scrollRect.horizontalNormalizedPosition = targetPosition;
                // Play pop effect immediately for initial setup
                PlayPopEffect();
            }
            else
            {
                StartCoroutine(SmoothScrollToPosition(targetPosition));
            }
        }
        
        UpdatePageIndicators();
        UpdateAnimations();
        
        // Notify level manager about page change
        if (levelManager != null)
        {
            levelManager.RefreshUI();
        }
    }
    
    IEnumerator SmoothScrollToPosition(float targetPosition)
    {
        float startPosition = scrollRect.horizontalNormalizedPosition;
        float elapsed = 0f;
        float duration = 1f / snapSpeed;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t); // Smooth curve
            
            scrollRect.horizontalNormalizedPosition = Mathf.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
        
        scrollRect.horizontalNormalizedPosition = targetPosition;
        
        // Play pop effect after scroll is complete
        PlayPopEffect();
        
        // Make sure animations update after scroll is complete
        UpdatePageIndicators();
        UpdateAnimations();
    }
    
    void PlayPopEffect()
    {
        if (pages == null || originalScales == null) return;
        
        // Reset all pages to original scale first
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
            {
                // Cancel any existing animations
                LeanTween.cancel(pages[i].gameObject);
                
                // Reset to original scale
                pages[i].LeanScale(originalScales[i], scaleDuration).setEase(scaleUpEaseType);
                
                // Deactivate page animations for all pages
                if (pageAnimations != null && i < pageAnimations.Length && pageAnimations[i] != null)
                {
                    pageAnimations[i].Deactivate();
                }
            }
        }
        
        // Scale up the current page
        if (currentPageIndex >= 0 && currentPageIndex < pages.Length && pages[currentPageIndex] != null)
        {
            RectTransform currentPageRect = pages[currentPageIndex];
            
            // Cancel any existing animation on current page
            LeanTween.cancel(currentPageRect.gameObject);
            
            // Scale up current page
            Vector3 targetScale = originalScales[currentPageIndex] * scaleUpSize;
            currentPageRect.LeanScale(targetScale, scaleDuration).setEase(scaleUpEaseType);
            
            // Activate current page animation
            if (pageAnimations != null && currentPageIndex < pageAnimations.Length && pageAnimations[currentPageIndex] != null)
            {
                pageAnimations[currentPageIndex].Activate();
            }
        }
    }
    
    void UpdatePageIndicators()
    {
        if (pageIndicators == null) return;
        
        for (int i = 0; i < pageIndicators.Length; i++)
        {
            if (pageIndicators[i] != null)
            {
                Image indicatorImage = pageIndicators[i].GetComponent<Image>();
                if (indicatorImage != null)
                {
                    indicatorImage.color = (i == currentPageIndex) ? activeIndicatorColor : inactiveIndicatorColor;
                }
            }
        }
    }
    
    void UpdateAnimations()
    {
        if (pageAnimations == null) return;
        
        for (int i = 0; i < pageAnimations.Length; i++)
        {
            if (pageAnimations[i] != null)
            {
                if (i == currentPageIndex)
                {
                    pageAnimations[i].Activate();
                }
                else
                {
                    pageAnimations[i].Deactivate();
                }
            }
        }
    }
    
    public int GetCurrentPageIndex()
    {
        return currentPageIndex;
    }
    
    public RectTransform GetCurrentPageRect()
    {
        if (currentPageIndex >= 0 && currentPageIndex < pages.Length)
        {
            return pages[currentPageIndex];
        }
        return null;
    }
    
    public RectTransform GetPageRect(int index)
    {
        if (index >= 0 && index < pages.Length)
        {
            return pages[index];
        }
        return null;
    }
    
    public int GetTotalPages()
    {
        return pages.Length;
    }
}