using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menu UI Manager - Delegates tutorial navigation to TutorialNavigator
/// </summary>
public class MenuUIManager : MonoBehaviour
{
    #region UI References

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject howToPlayPanel;

    [Header("Main Menu")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button howToPlayButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("Settings")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle vibrationToggle;
    [SerializeField] private Button settingsBackButton;

    [Header("Credits")]
    [SerializeField] private Button creditsBackButton;
    [SerializeField] private Button howToPlayBackButton;

    [Header("References")]
    [SerializeField] private ShopUIManager shopUIManager;
    [SerializeField] private ModeSelectionUI modeSelectionUI;
    [SerializeField] private TutorialNavigator tutorialNavigator; // ← NEW

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

        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnGoldChanged += UpdateGoldDisplay;
        }
    }

    private void SetupButtons()
    {
        playButton?.onClick.AddListener(OnPlayButtonClicked);
        shopButton?.onClick.AddListener(OnShopButtonClicked);
        settingsButton?.onClick.AddListener(OnSettingsButtonClicked);
        creditsButton?.onClick.AddListener(OnCreditsButtonClicked);
        howToPlayButton?.onClick.AddListener(OnHowToPlayButtonClicked);
        quitButton?.onClick.AddListener(OnQuitButtonClicked);
        settingsBackButton?.onClick.AddListener(OnSettingsBackClicked);
        creditsBackButton?.onClick.AddListener(OnCreditsBackClicked);
        howToPlayBackButton?.onClick.AddListener(OnHowToPlayBackClicked);

        musicVolumeSlider?.onValueChanged.AddListener(OnMusicVolumeChanged);
        sfxVolumeSlider?.onValueChanged.AddListener(OnSFXVolumeChanged);
        vibrationToggle?.onValueChanged.AddListener(OnVibrationToggled);
    }

    private void LoadAndDisplayStats()
    {
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
        creditsPanel?.SetActive(false);
        howToPlayPanel?.SetActive(false);
    }

    private void ShowSettings()
    {
        mainMenuPanel?.SetActive(false);
        creditsPanel?.SetActive(false);
        settingsPanel?.SetActive(true);
        howToPlayPanel?.SetActive(false);
    }

    private void ShowCredits()
    {
        mainMenuPanel?.SetActive(false);
        creditsPanel?.SetActive(true);
        settingsPanel?.SetActive(false);
        howToPlayPanel?.SetActive(false);
    }

    private void ShowHowToPlay()
    {
        mainMenuPanel?.SetActive(false);
        creditsPanel?.SetActive(false);
        settingsPanel?.SetActive(false);
        howToPlayPanel?.SetActive(true);
    }

    #endregion

    #region Button Handlers

    private void OnPlayButtonClicked()
    {
        Debug.Log("[MenuUI] Play button clicked");
        AudioManager.Instance?.PlayButtonClickSound();

        if (modeSelectionUI != null)
        {
            modeSelectionUI.OpenModeSelection();
        }
        else
        {
            Debug.LogError("[MenuUI] ModeSelectionUI not assigned!");
            GameModeManager.Instance.SetEndlessMode();
            SceneController.Instance.LoadGameplayScene();
        }
    }

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

    private void OnCreditsButtonClicked()
    {
        Debug.Log("[MenuUI] Credits button clicked");
        AudioManager.Instance?.PlayButtonClickSound();
        ShowCredits();
    }

    private void OnHowToPlayButtonClicked()
    {
        Debug.Log("[MenuUI] How To Play button clicked");
        AudioManager.Instance?.PlayButtonClickSound();
        ShowHowToPlay();
    }

    private void OnHowToPlayBackClicked()
    {
        AudioManager.Instance?.PlayButtonClickSound();
        ShowMainMenu();
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

    private void OnCreditsBackClicked()
    {
        AudioManager.Instance?.PlayButtonClickSound();
        ShowSettings();
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