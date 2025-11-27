using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages tutorial navigation using ScriptableObject data
/// Clean, scalable, and maintainable approach
/// </summary>
public class TutorialNavigator : MonoBehaviour
{
    #region Serialized Fields

    [Header("Tutorial Data")]
    [SerializeField] private TutorialPage[] tutorialPages; // ← ScriptableObject array

    [Header("UI References")]
    [SerializeField] private Image displayImage;
    [SerializeField] private TextMeshProUGUI titleText; // Optional
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;
    
    [Header("Optional")]
    [SerializeField] private TextMeshProUGUI pageIndicator;
    [SerializeField] private bool loopNavigation = false;

    #endregion

    #region Private Fields

    private int currentIndex = 0;

    #endregion

    #region Unity Lifecycle

    private void OnEnable()
    {
        Initialize();
    }

    private void OnDisable()
    {
        UnsubscribeButtons();
    }

    #endregion

    #region Initialization

    private void Initialize()
    {
        ValidateReferences();
        SubscribeButtons();
        ResetToFirstPage();
    }

    private void ValidateReferences()
    {
        if (tutorialPages == null || tutorialPages.Length == 0)
        {
            Debug.LogError("[TutorialNavigator] No tutorial pages assigned!");
            return;
        }

        if (displayImage == null)
        {
            Debug.LogError("[TutorialNavigator] Display Image not assigned!");
        }

        if (descriptionText == null)
        {
            Debug.LogWarning("[TutorialNavigator] Description Text not assigned!");
        }

        if (nextButton == null || previousButton == null)
        {
            Debug.LogWarning("[TutorialNavigator] Navigation buttons not assigned!");
        }
    }

    private void SubscribeButtons()
    {
        nextButton?.onClick.AddListener(OnNextClicked);
        previousButton?.onClick.AddListener(OnPreviousClicked);
    }

    private void UnsubscribeButtons()
    {
        nextButton?.onClick.RemoveListener(OnNextClicked);
        previousButton?.onClick.RemoveListener(OnPreviousClicked);
    }

    #endregion

    #region Navigation Logic

    private void OnNextClicked()
    {
        AudioManager.Instance?.PlayButtonClickSound();

        if (currentIndex < tutorialPages.Length - 1)
        {
            currentIndex++;
        }
        else if (loopNavigation)
        {
            currentIndex = 0;
        }

        UpdateDisplay();
    }

    private void OnPreviousClicked()
    {
        AudioManager.Instance?.PlayButtonClickSound();

        if (currentIndex > 0)
        {
            currentIndex--;
        }
        else if (loopNavigation)
        {
            currentIndex = tutorialPages.Length - 1;
        }

        UpdateDisplay();
    }

    private void ResetToFirstPage()
    {
        currentIndex = 0;
        UpdateDisplay();
    }

    #endregion

    #region UI Update

    private void UpdateDisplay()
    {
        if (tutorialPages == null || tutorialPages.Length == 0) return;

        TutorialPage currentPage = GetCurrentPage();
        if (currentPage == null) return;

        UpdateImage(currentPage);
        UpdateTitle(currentPage);
        UpdateDescription(currentPage);
        UpdatePageIndicator();
        UpdateButtonInteractability();
    }

    private void UpdateImage(TutorialPage page)
    {
        if (displayImage != null && page.Image != null)
        {
            displayImage.sprite = page.Image;
        }
    }

    private void UpdateTitle(TutorialPage page)
    {
        if (titleText != null)
        {
            titleText.text = !string.IsNullOrEmpty(page.Title) ? page.Title : string.Empty;
        }
    }

    private void UpdateDescription(TutorialPage page)
    {
        if (descriptionText != null)
        {
            descriptionText.text = page.Description;
        }
    }

    private void UpdatePageIndicator()
    {
        if (pageIndicator != null)
        {
            pageIndicator.text = $"{currentIndex + 1} / {tutorialPages.Length}";
        }
    }

    private void UpdateButtonInteractability()
    {
        if (loopNavigation) return;

        if (previousButton != null)
        {
            previousButton.interactable = currentIndex > 0;
        }

        if (nextButton != null)
        {
            nextButton.interactable = currentIndex < tutorialPages.Length - 1;
        }
    }

    #endregion

    #region Helper Methods

    private TutorialPage GetCurrentPage()
    {
        if (currentIndex >= 0 && currentIndex < tutorialPages.Length)
        {
            return tutorialPages[currentIndex];
        }
        return null;
    }

    #endregion

    #region Public API

    public void GoToPage(int pageIndex)
    {
        if (pageIndex >= 0 && pageIndex < tutorialPages.Length)
        {
            currentIndex = pageIndex;
            UpdateDisplay();
        }
    }

    public int GetCurrentPageIndex() => currentIndex;

    public int GetTotalPages() => tutorialPages?.Length ?? 0;

    #endregion
}