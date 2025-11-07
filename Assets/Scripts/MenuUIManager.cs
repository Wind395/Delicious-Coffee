using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menu UI Manager - UPDATED: Add Shop button
/// </summary>
public class MenuUIManager : MonoBehaviour
{
    #region UI References
    
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    
    [Header("Main Menu")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button shopButton; // ← NEW
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TextMeshProUGUI goldText; // ← CHANGED from highScoreText
    
    [Header("Settings")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle vibrationToggle;
    [SerializeField] private Button settingsBackButton;
    
    [Header("Shop Reference")]
    [SerializeField] private ShopUIManager shopUIManager; // ← NEW
    
    #endregion

    #region Unity Lifecycle
    
    void Start()
    {
        Initialize();
    }
    
    #endregion

    #region Initialization
    
    private void Initialize()
    {
        SetupButtons();
        LoadAndDisplayStats();
        LoadSettings();
        ShowMainMenu();
        
        AudioManager.Instance?.PlayMenuMusic();
        
        // Subscribe to gold changes
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnGoldChanged += UpdateGoldDisplay;
        }
    }

    private void SetupButtons()
    {
        playButton?.onClick.AddListener(OnPlayButtonClicked);
        shopButton?.onClick.AddListener(OnShopButtonClicked); // ← NEW
        settingsButton?.onClick.AddListener(OnSettingsButtonClicked);
        quitButton?.onClick.AddListener(OnQuitButtonClicked);
        settingsBackButton?.onClick.AddListener(OnSettingsBackClicked);
        
        musicVolumeSlider?.onValueChanged.AddListener(OnMusicVolumeChanged);
        sfxVolumeSlider?.onValueChanged.AddListener(OnSFXVolumeChanged);
        vibrationToggle?.onValueChanged.AddListener(OnVibrationToggled);
    }

    private void LoadAndDisplayStats()
    {
        // Display gold instead of high score
        int gold = PlayerDataManager.Instance.Gold;
        UpdateGoldDisplay(gold);
    }

    private void LoadSettings()
    {
        if (AudioManager.Instance != null)
        {
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
            }
            
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
            }
        }
        
        if (vibrationToggle != null)
        {
            vibrationToggle.isOn = PlayerPrefs.GetInt("Vibration", 1) == 1;
        }
    }
    
    #endregion

    #region Panel Management
    
    private void ShowMainMenu()
    {
        mainMenuPanel?.SetActive(true);
        settingsPanel?.SetActive(false);
    }

    private void ShowSettings()
    {
        mainMenuPanel?.SetActive(false);
        settingsPanel?.SetActive(true);
    }
    
    #endregion

    #region Button Handlers
    
    /// <summary>
    /// Play button - FIXED: Input will be enabled after gameplay loads
    /// </summary>
    private void OnPlayButtonClicked()
    {
        Debug.Log("[MenuUI] Play button clicked");
        AudioManager.Instance?.PlayButtonClickSound();
        
        // Note: Input will be auto-enabled by SceneController after gameplay loads
        Debug.Log("[MenuUI] Loading gameplay... (Input will be enabled after load)");
        
        SceneController.Instance.LoadGameplayScene();
    }

    /// <summary>
    /// Shop button - NEW
    /// </summary>
    private void OnShopButtonClicked()
    {
        Debug.Log("[MenuUI] Shop button clicked");
        AudioManager.Instance?.PlayButtonClickSound();
        
        if (shopUIManager != null)
        {
            shopUIManager.OpenShop();
        }
        else
        {
            Debug.LogError("[MenuUI] ShopUIManager not assigned!");
        }
    }

    private void OnSettingsButtonClicked()
    {
        Debug.Log("[MenuUI] Settings button clicked");
        AudioManager.Instance?.PlayButtonClickSound();
        ShowSettings();
    }

    private void OnQuitButtonClicked()
    {
        Debug.Log("[MenuUI] Quit button clicked");
        AudioManager.Instance?.PlayButtonClickSound();
        SceneController.Instance.QuitGame();
    }

    private void OnSettingsBackClicked()
    {
        AudioManager.Instance?.PlayButtonClickSound();
        ShowMainMenu();
    }
    
    #endregion

    #region Settings Handlers
    
    private void OnMusicVolumeChanged(float value)
    {
        AudioManager.Instance?.SetMusicVolume(value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        AudioManager.Instance?.SetSFXVolume(value);
    }

    private void OnVibrationToggled(bool isOn)
    {
        PlayerPrefs.SetInt("Vibration", isOn ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    #endregion

    #region UI Updates
    
    /// <summary>
    /// Update gold display - NEW
    /// </summary>
    private void UpdateGoldDisplay(int gold)
    {
        if (goldText != null)
        {
            goldText.text = $"{gold}";
        }
    }
    
    #endregion

    #region Cleanup
    
    void OnDestroy()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnGoldChanged -= UpdateGoldDisplay;
        }
    }
    
    #endregion
}