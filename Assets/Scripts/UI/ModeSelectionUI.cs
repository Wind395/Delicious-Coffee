using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Mode Selection UI - Chọn Level hoặc Endless
/// </summary>
public class ModeSelectionUI : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Panels")]
    [SerializeField] private GameObject modeSelectionPanel;
    
    [Header("Buttons")]
    [SerializeField] private Button levelModeButton;
    [SerializeField] private Button endlessModeButton;
    [SerializeField] private Button backButton;
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI levelModeTitle;
    [SerializeField] private TextMeshProUGUI endlessModeTitle;
    [SerializeField] private TextMeshProUGUI levelModeDescription;
    [SerializeField] private TextMeshProUGUI endlessModeDescription;
    
    [Header("References")]
    [SerializeField] private MapSelectionUI mapSelectionUI;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    #endregion

    #region Unity Lifecycle
    
    void Start()
    {
        SetupButtons();
        UpdateUI();
    }
    
    #endregion

    #region Setup
    
    private void SetupButtons()
    {
        levelModeButton?.onClick.AddListener(OnLevelModeClicked);
        endlessModeButton?.onClick.AddListener(OnEndlessModeClicked);
        backButton?.onClick.AddListener(OnBackClicked);
    }
    
    private void UpdateUI()
    {
        if (levelModeTitle != null)
        {
            levelModeTitle.text = "LEVEL MODE";
        }
        
        if (levelModeDescription != null)
        {
            levelModeDescription.text = "Play through 10 handcrafted levels\n2 Maps × 5 Levels";
        }
        
        if (endlessModeTitle != null)
        {
            endlessModeTitle.text = "ENDLESS MODE";
        }
        
        if (endlessModeDescription != null)
        {
            endlessModeDescription.text = "Run as far as you can!\nCompete for the highest distance";
        }
    }
    
    #endregion

    #region Button Handlers
    
    /// <summary>
    /// Level Mode clicked - Mở Map Selection
    /// </summary>
    private void OnLevelModeClicked()
    {
        if (showDebugLogs)
        {
            Debug.Log("[ModeSelectionUI] Level Mode selected");
        }
        
        AudioManager.Instance?.PlayButtonClickSound();
        
        // Set mode
        GameModeManager.Instance.SetLevelMode();
        
        // Open Map Selection
        if (mapSelectionUI != null)
        {
            modeSelectionPanel?.SetActive(false);
            mapSelectionUI.OpenMapSelection();
        }
        else
        {
            Debug.LogError("[ModeSelectionUI] MapSelectionUI not assigned!");
        }
    }
    
    /// <summary>
    /// Endless Mode clicked - Start game ngay
    /// </summary>
    private void OnEndlessModeClicked()
    {
        if (showDebugLogs)
        {
            Debug.Log("[ModeSelectionUI] Endless Mode selected");
        }
        
        AudioManager.Instance?.PlayButtonClickSound();
        
        // Set mode
        GameModeManager.Instance.SetEndlessMode();
        
        // Start game immediately
        SceneController.Instance.LoadGameplayScene();
    }
    
    /// <summary>
    /// Back to main menu
    /// </summary>
    private void OnBackClicked()
    {
        AudioManager.Instance?.PlayButtonClickSound();
        
        modeSelectionPanel?.SetActive(false);
    }
    
    #endregion

    #region Public API
    
    public void OpenModeSelection()
    {
        modeSelectionPanel?.SetActive(true);
        UpdateUI();
    }
    
    public void CloseModeSelection()
    {
        modeSelectionPanel?.SetActive(false);
    }
    
    #endregion
}