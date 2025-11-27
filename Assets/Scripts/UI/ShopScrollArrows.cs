using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple Scroll Arrows for Shop
/// </summary>
public class ShopScrollArrows : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Button leftArrow;
    [SerializeField] private Button rightArrow;
    
    [Header("Settings")]
    [SerializeField] private float scrollSpeed = 0.3f;
    
    void Start()
    {
        // Subscribe to buttons
        leftArrow.onClick.AddListener(ScrollLeft);
        rightArrow.onClick.AddListener(ScrollRight);
        
        // Update arrow visibility
        UpdateArrows();
        
        // Listen to scroll changes
        scrollRect.onValueChanged.AddListener((pos) => UpdateArrows());
    }
    
    void ScrollLeft()
    {
        float newPos = Mathf.Max(0f, scrollRect.horizontalNormalizedPosition - scrollSpeed);
        scrollRect.horizontalNormalizedPosition = newPos;
    }
    
    void ScrollRight()
    {
        float newPos = Mathf.Min(1f, scrollRect.horizontalNormalizedPosition + scrollSpeed);
        scrollRect.horizontalNormalizedPosition = newPos;
    }
    
    void UpdateArrows()
    {
        // Hide left arrow if at start
        leftArrow.gameObject.SetActive(scrollRect.horizontalNormalizedPosition > 0.01f);
        
        // Hide right arrow if at end
        rightArrow.gameObject.SetActive(scrollRect.horizontalNormalizedPosition < 0.99f);
    }
}