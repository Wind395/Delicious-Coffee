using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// UI Manager - UPDATED: Full pause/victory/gameover UI
/// </summary>
public class UIManager : MonoBehaviour
{

    private static UIManager _instance;
    public static UIManager Instance => _instance;

    #region UI Panels

    [Header("=== PANELS ===")]
    [SerializeField] private GameObject gamePlayPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;

    #endregion

    #region GamePlay UI

    [Header("=== GAMEPLAY UI ===")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private RawImage characterIcon;
    [SerializeField] private TextMeshProUGUI levelTimerText;
    [SerializeField] private TextMeshProUGUI levelInfoText;
    [SerializeField] private Button pauseButton;

    #endregion

    #region Render Textures
    [Header("=== RENDER TEXTURES ===")]
    [SerializeField] private Texture[] characterRenderTexture;

    #endregion

    #region Meters UI

    [Header("=== METERS ===")]
    [SerializeField] private Slider dogDistanceSlider; // CHANGED from diarrheaMeterSlider
    [SerializeField] private TextMeshProUGUI dogDistanceText; // CHANGED from diarrheaMeterText
    [SerializeField] private Slider distanceSlider;
    [SerializeField] private TextMeshProUGUI distanceText;

    [Header("Meter Colors")]
    [SerializeField] private Color meterSafeColor = Color.green; // CHANGED from meterNormalColor
    [SerializeField] private Color meterWarningColor = Color.yellow;
    [SerializeField] private Color meterDangerColor = Color.red; // CHANGED from meterCriticalColor

    #endregion

    #region PowerUp UI

    [Header("=== POWERUP UI ===")]
    [SerializeField] private GameObject powerUpPanel;
    [SerializeField] private Image iceTeaIcon;
    [SerializeField] private Image coldTowelIcon;
    [SerializeField] private Image medicineIcon;
    [SerializeField] private TextMeshProUGUI iceTeaTimerText;
    [SerializeField] private TextMeshProUGUI coldTowelTimerText;
    [SerializeField] private TextMeshProUGUI medicineTimerText;

    #endregion

    #region Pause UI

    [Header("=== PAUSE UI ===")]
    [SerializeField] private TextMeshProUGUI pauseTitleText;
    [SerializeField] private Button pauseResumeButton;
    [SerializeField] private Button pauseRestartButton;
    [SerializeField] private Button pauseMainMenuButton;
    [SerializeField] private Slider pauseMusicVolumeSlider;
    [SerializeField] private Slider pauseSFXVolumeSlider;
    [SerializeField] private TextMeshProUGUI pauseMusicVolumeText;
    [SerializeField] private TextMeshProUGUI pauseSFXVolumeText;

    #endregion

    #region Game Over UI

    [Header("=== GAME OVER UI ===")]
    [SerializeField] private TextMeshProUGUI gameOverTitleText;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI finalCoinsText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private TextMeshProUGUI gameOverReasonText;
    [SerializeField] private Button gameOverPlayAgainButton;
    [SerializeField] private Button gameOverMainMenuButton;

    #endregion

    #region Victory UI

    [Header("=== VICTORY UI ===")]
    [SerializeField] private TextMeshProUGUI victoryTitleText;
    [SerializeField] private TextMeshProUGUI victoryScoreText;
    [SerializeField] private TextMeshProUGUI victoryCoinsText;
    [SerializeField] private TextMeshProUGUI victoryMessageText;
    [SerializeField] private TextMeshProUGUI victoryStatsText;
    [SerializeField] private Button victoryNextLevelButton;
    [SerializeField] private Button victoryPlayAgainButton;
    [SerializeField] private Button victoryMainMenuButton;

    #endregion

    #region State

    private int _highScore = 0;

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    void Start()
    {
        Initialize();
        UpdateCharacterIcon();
        ShowGameUI();
    }

    void Update()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState == GameState.Playing)
        {
            // ═══ UPDATED: Check mode ═══
            if (GameModeManager.Instance.CurrentMode == GameMode.Endless)
            {
                UpdateEndlessModeUI(); // ← NEW
            }
            else
            {
                UpdateLevelModeUI(); // ← Existing logic
            }
            
            UpdatePowerUpUI();
            UpdateDogDistanceUI();
        }
    }

    #endregion

    #region Initialization

    private void Initialize()
    {
        _highScore = PlayerPrefs.GetInt("HighScore", 0);

        SubscribeToEvents();
        SubscribeToMeterEvents();
        SetupButtons();
        ValidateReferences();
    }

    private void SubscribeToEvents()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.StartListening(GameEvents.SCORE_CHANGED, OnScoreChanged);
            EventManager.Instance.StartListening(GameEvents.COIN_COLLECTED, OnCoinCollected);
        }
    }

    private void SubscribeToMeterEvents()
    {
        if (DistanceTracker.Instance != null)
        {
            DistanceTracker.Instance.OnDistanceChanged += UpdateDistanceUI;
        }

        // ═══ CHANGED: Subscribe to Dog Chase events ═══
        if (DogChaseController.Instance != null)
        {
            DogChaseController.Instance.OnDogCatchPlayer += OnDogCatchPlayer;
            DogChaseController.Instance.OnDogDisappear += OnDogDisappear;
        }

        /* ═══ COMMENTED OUT: OLD DIARRHEA METER ═══
        if (DiarrheaMeter.Instance != null)
        {
            DiarrheaMeter.Instance.OnMeterChanged += UpdateDiarrheaMeterUI;
        }
        */
    }

    /// <summary>
    /// Setup all button listeners
    /// </summary>
    private void SetupButtons()
    {
        // Gameplay
        pauseButton?.onClick.AddListener(OnPauseButtonClicked);

        // Pause Panel
        pauseResumeButton?.onClick.AddListener(OnResumeButtonClicked);
        pauseRestartButton?.onClick.AddListener(OnRestartButtonClicked);
        pauseMainMenuButton?.onClick.AddListener(OnMainMenuButtonClicked);

        // Pause Volume Sliders
        pauseMusicVolumeSlider?.onValueChanged.AddListener(OnMusicVolumeChanged);
        pauseSFXVolumeSlider?.onValueChanged.AddListener(OnSFXVolumeChanged);

        // Game Over Panel
        gameOverPlayAgainButton?.onClick.AddListener(OnPlayAgainButtonClicked);
        gameOverMainMenuButton?.onClick.AddListener(OnMainMenuButtonClicked);

        // Victory Panel
        victoryNextLevelButton?.onClick.AddListener(OnNextLevelButtonClicked);
        victoryPlayAgainButton?.onClick.AddListener(OnPlayAgainButtonClicked);
        victoryMainMenuButton?.onClick.AddListener(OnMainMenuButtonClicked);
    }

    private void ValidateReferences()
    {
        if (distanceSlider == null)
            Debug.LogWarning("[UIManager] Distance Slider not assigned!");

        if (dogDistanceSlider == null)
            Debug.LogWarning("[UIManager] Dog Distance Slider not assigned!");
    }

    #endregion

    #region Character Icon

    private void UpdateCharacterIcon()
    {
        ShopItemData characterData = ShopManager.Instance?.GetEquippedCharacter();

        if (characterData != null)
        {
            if (characterData.itemType == ShopItemType.Character)
            {
                switch (characterData.itemID)
                {
                    case "char_default":
                        characterIcon.texture = characterRenderTexture[0];
                        break;
                    case "char_bunnyGirl":
                        characterIcon.texture = characterRenderTexture[1];
                        break;
                    case "char_sharkGank":
                        characterIcon.texture = characterRenderTexture[2];
                        break;
                    default:
                        Debug.LogWarning("[UIManager] Unknown character ID for icon!");
                        break;
                }
            }
        }
    }
    #endregion

    #region Panel Management

    /// <summary>
    /// Initialize gameplay UI based on mode - UPDATED: Include PowerUp UI
    /// </summary>
    public void InitializeGameplayUI()
    {
        if (GameModeManager.Instance == null)
        {
            Debug.LogError("[UIManager] GameModeManager not found!");
            return;
        }
        
        GameMode currentMode = GameModeManager.Instance.CurrentMode;
        
        Debug.Log($"[UIManager] ═══ INITIALIZING UI FOR MODE: {currentMode} ═══");
        
        if (currentMode == GameMode.Level)
        {
            InitializeLevelModeUI();
        }
        else if (currentMode == GameMode.Endless)
        {
            InitializeEndlessModeUI();
        }
        else
        {
            Debug.LogWarning("[UIManager] Unknown game mode!");
        }
        
        // ═══ NEW: Initialize PowerUp UI ═══
        InitializePowerUpUI();
        
        // Update common UI
        UpdateAllGameplayUI();
    }

    /// <summary>
    /// Initialize UI for Level mode - UPDATED
    /// </summary>
    private void InitializeLevelModeUI()
    {
        Debug.Log("[UIManager] Initializing LEVEL MODE UI");
        
        // ═══ SHOW: Distance slider (progress to home) ═══
        if (distanceSlider != null)
        {
            distanceSlider.gameObject.SetActive(true);
        }
        
        if (distanceText != null)
        {
            distanceText.gameObject.SetActive(true);
            distanceText.color = Color.white;
        }
        
        Debug.Log("[UIManager] ✓ Level mode UI initialized");
    }

    /// <summary>
    /// Initialize UI for Endless mode - UPDATED
    /// </summary>
    private void InitializeEndlessModeUI()
    {
        Debug.Log("[UIManager] Initializing ENDLESS MODE UI");
        
        // ═══ HIDE: Distance slider (no target in endless) ═══
        if (distanceSlider != null)
        {
            distanceSlider.gameObject.SetActive(false);
        }
        
        // ═══ SHOW: Distance text (current + best) ═══
        if (distanceText != null)
        {
            distanceText.gameObject.SetActive(true);
            distanceText.color = Color.white;
        }
        
        Debug.Log("[UIManager] ✓ Endless mode UI initialized");
    }

    /// <summary>
    /// Initialize PowerUp UI - NEW
    /// </summary>
    private void InitializePowerUpUI()
    {
        Debug.Log("[UIManager] Initializing PowerUp UI");
        
        // ═══ SHOW: PowerUp Panel ═══
        if (powerUpPanel != null)
        {
            powerUpPanel.SetActive(true);
        }
        
        // ═══ HIDE: All powerup icons initially ═══
        HideAllPowerUpIcons();
        
        Debug.Log("[UIManager] ✓ PowerUp UI initialized");
    }

    /// <summary>
    /// Hide all powerup icons - NEW
    /// </summary>
    private void HideAllPowerUpIcons()
    {
        // Ice Tea
        if (iceTeaIcon != null)
        {
            iceTeaIcon.enabled = false;
        }
        if (iceTeaTimerText != null)
        {
            iceTeaTimerText.enabled = false;
        }
        
        // Cold Towel
        if (coldTowelIcon != null)
        {
            coldTowelIcon.enabled = false;
        }
        if (coldTowelTimerText != null)
        {
            coldTowelTimerText.enabled = false;
        }
        
        // Medicine
        if (medicineIcon != null)
        {
            medicineIcon.enabled = false;
        }
        if (medicineTimerText != null)
        {
            medicineTimerText.enabled = false;
        }
    }

    #endregion

    #region Panel Management

    private void HideAllPanels()
    {
        gamePlayPanel?.SetActive(false);
        pausePanel?.SetActive(false);
        gameOverPanel?.SetActive(false);
        victoryPanel?.SetActive(false);
    }

    private void ShowPanel(GameObject panel)
    {
        if (panel == null) return;
        HideAllPanels();
        panel.SetActive(true);
    }

    #endregion

    #region Show Panel Methods

    public void ShowGameUI()
    {
        ShowPanel(gamePlayPanel);
        
        Debug.Log("[UIManager] Game UI panel shown");
    }

    public void ShowPauseMenu()
    {
        ShowPanel(pausePanel);
        UpdatePauseUI();
    }

    public void HidePauseMenu()
    {
        pausePanel?.SetActive(false);
        gamePlayPanel?.SetActive(true);
    }

    public void ShowGameOverUI()
    {
        ShowPanel(gameOverPanel);
        UpdateGameOverUI();
    }

    public void ShowVictoryUI()
    {
        ShowPanel(victoryPanel);
        UpdateVictoryUI();
    }

    #endregion

    #region Gameplay UI Updates

    public void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }

    public void UpdateCoins(int coins)
    {
        if (coinsText != null)
        {
            coinsText.text = $"{coins}";
        }
    }

    public void UpdateLevelTimer(float timeRemaining)
    {
        if (levelTimerText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            levelTimerText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    public void UpdateLevelInfo(int currentLevel, int totalLevels, string levelName)
    {
        if (levelInfoText != null)
        {
            levelInfoText.text = $"{levelName}";
        }
    }

    private void UpdateAllGameplayUI()
    {
        if (GameManager.Instance != null)
        {
            UpdateScore(GameManager.Instance.Score);
            UpdateCoins(GameManager.Instance.Coins);
        }
    }

    #endregion

    #region Distance UI Updates

    private void UpdateDistanceUI(float current, float target, float progress)
    {
        if (distanceSlider != null)
        {
            distanceSlider.value = progress;
        }

        if (distanceText != null)
        {
            // ═══ CHANGED: Text from "toilet" → "home"
            distanceText.text = $"{current:F0}m / {target:F0}m to Home";
        }
    }

    // public void UpdateDiarrheaMeterUI(float current, float max, float percent)
    // {
    //     if (diarrheaMeterSlider != null)
    //     {
    //         diarrheaMeterSlider.value = percent;

    //         Image fillImage = diarrheaMeterSlider.fillRect?.GetComponent<Image>();
    //         if (fillImage != null)
    //         {
    //             if (percent < 0.5f)
    //                 fillImage.color = meterNormalColor;
    //             else if (percent < 0.8f)
    //                 fillImage.color = meterWarningColor;
    //             else
    //                 fillImage.color = meterCriticalColor;
    //         }
    //     }

    //     if (diarrheaMeterText != null)
    //     {
    //         diarrheaMeterText.text = $"{current:F0}%";
    //     }
    // }

    #endregion

    #region Dog Distance UI - NEW

    /// <summary>
    /// Update dog distance meter (replaces diarrhea meter)
    /// </summary>
    private void UpdateDogDistanceUI()
    {
        if (DogChaseController.Instance == null || !DogChaseController.Instance.IsChasing)
        {
            // Dog not chasing → Hide meter
            if (dogDistanceSlider != null)
            {
                dogDistanceSlider.gameObject.SetActive(false);
            }
            if (dogDistanceText != null)
            {
                dogDistanceText.gameObject.SetActive(false);
            }
            return;
        }

        // Show meter
        if (dogDistanceSlider != null)
        {
            dogDistanceSlider.gameObject.SetActive(true);
        }
        if (dogDistanceText != null)
        {
            dogDistanceText.gameObject.SetActive(true);
        }

        float distance = DogChaseController.Instance.DogDistance;
        float maxDistance = 30f; // Match DogChaseController.dogMaxDistance
        float percent = 1f - Mathf.Clamp01(distance / maxDistance); // Inverted (closer = more filled)

        // Update slider
        if (dogDistanceSlider != null)
        {
            dogDistanceSlider.value = percent;

            Image fillImage = dogDistanceSlider.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                if (percent < 0.3f) // Safe (dog far away)
                    fillImage.color = meterSafeColor;
                else if (percent < 0.7f) // Warning
                    fillImage.color = meterWarningColor;
                else // Danger (dog close)
                    fillImage.color = meterDangerColor;
            }
        }

        // Update text
        if (dogDistanceText != null)
        {
            dogDistanceText.text = $"🐕 {distance:F0}m";
        }
    }

    /// <summary>
    /// Dog caught player event handler
    /// </summary>
    private void OnDogCatchPlayer()
    {
        Debug.Log("[UIManager] 🐕 Dog caught player!");
        // Game Over will be triggered by PlayerController
    }

    /// <summary>
    /// Dog disappeared event handler
    /// </summary>
    private void OnDogDisappear()
    {
        Debug.Log("[UIManager] 🐕 Dog disappeared - player is safe!");

        // Hide dog distance meter
        if (dogDistanceSlider != null)
        {
            dogDistanceSlider.gameObject.SetActive(false);
        }
        if (dogDistanceText != null)
        {
            dogDistanceText.gameObject.SetActive(false);
        }
    }

    #endregion

    #region PowerUp UI Updates

    private void UpdatePowerUpUI()
    {
        if (PowerUpManager.Instance == null) return;

        UpdatePowerUpIcon<IceTeaPowerUp>(iceTeaIcon, iceTeaTimerText);
        UpdatePowerUpIcon<ColdTowelPowerUp>(coldTowelIcon, coldTowelTimerText);
        UpdatePowerUpIcon<MedicinePowerUp>(medicineIcon, medicineTimerText);
    }

    private void UpdatePowerUpIcon<T>(Image icon, TextMeshProUGUI timerText) where T : PowerUpBase
    {
        if (icon == null) return;

        T powerUp = PowerUpManager.Instance.GetActivePowerUp<T>();

        if (powerUp != null && powerUp.IsActive)
        {
            icon.enabled = true;

            if (timerText != null)
            {
                timerText.enabled = true;
                timerText.text = $"{Mathf.CeilToInt(powerUp.TimeRemaining)}s";
            }
        }
        else
        {
            icon.enabled = false;

            if (timerText != null)
            {
                timerText.enabled = false;
            }
        }
    }

    #endregion

    #region Pause UI Updates

    /// <summary>
    /// Update pause UI - Load current audio settings
    /// </summary>
    private void UpdatePauseUI()
    {
        if (pauseTitleText != null)
        {
            pauseTitleText.text = "PAUSED";
        }

        // Load current audio volumes
        if (AudioManager.Instance != null)
        {
            if (pauseMusicVolumeSlider != null)
            {
                float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
                pauseMusicVolumeSlider.value = musicVolume;
                UpdateMusicVolumeText(musicVolume);
            }

            if (pauseSFXVolumeSlider != null)
            {
                float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
                pauseSFXVolumeSlider.value = sfxVolume;
                UpdateSFXVolumeText(sfxVolume);
            }
        }
    }

    /// <summary>
    /// Update music volume text display
    /// </summary>
    private void UpdateMusicVolumeText(float volume)
    {
        if (pauseMusicVolumeText != null)
        {
            pauseMusicVolumeText.text = $"Music: {Mathf.RoundToInt(volume * 100)}%";
        }
    }

    /// <summary>
    /// Update SFX volume text display
    /// </summary>
    private void UpdateSFXVolumeText(float volume)
    {
        if (pauseSFXVolumeText != null)
        {
            pauseSFXVolumeText.text = $"SFX: {Mathf.RoundToInt(volume * 100)}%";
        }
    }

    #endregion

    #region Game Over UI Updates

    private void UpdateGameOverUI()
    {
        if (GameManager.Instance == null) return;

        int finalScore = GameManager.Instance.Score;
        int finalCoins = GameManager.Instance.Coins;

        // ═══ NEW: Check mode để show high score phù hợp ═══
        if (GameModeManager.Instance.CurrentMode == GameMode.Endless)
        {
            // Endless mode - Show best distance
            float currentDistance = DistanceTracker.Instance != null ? 
                DistanceTracker.Instance.CurrentDistance : 0f;
            
            float bestDistance = PlayerDataManager.Instance.GetBestEndlessDistance();
            
            if (gameOverTitleText != null)
            {
                gameOverTitleText.text = "GAME OVER";
            }
            
            if (finalScoreText != null)
            {
                finalScoreText.text = $"Distance: {currentDistance:F0}m";
            }
            
            if (finalCoinsText != null)
            {
                finalCoinsText.text = $"Coins: {finalCoins}";
            }
            
            if (highScoreText != null)
            {
                highScoreText.text = $"Best Distance: {bestDistance:F0}m";
            }
        }
        else
        {
            // Level mode - Show normal score
            if (finalScore > _highScore)
            {
                _highScore = finalScore;
                PlayerPrefs.SetInt("HighScore", _highScore);
                PlayerPrefs.Save();
            }
            
            if (gameOverTitleText != null)
            {
                gameOverTitleText.text = "LEVEL FAILED";
            }
            
            // if (finalScoreText != null)
            //     finalScoreText.text = $"Score: {finalScore}";
            
            if (finalCoinsText != null)
                finalCoinsText.text = $"Coins: {finalCoins}";
            
            if (highScoreText != null)
                highScoreText.text = $"High Score: {_highScore}";
        }

        if (gameOverReasonText != null)
        {
            gameOverReasonText.text = GetGameOverReasonText();
        }
    }

    private string GetGameOverReasonText()
    {
        // Dog caught
        if (DogChaseController.Instance != null && DogChaseController.Instance.IsChasing)
        {
            return "🐕 The dog caught you!\n\n" +
                "You couldn't escape!\n" +
                "Run faster next time!";
        }

        // Check distance
        if (DistanceTracker.Instance != null)
        {
            float progress = DistanceTracker.Instance.Progress * 100f;
            float distance = DistanceTracker.Instance.CurrentDistance;
            float target = DistanceTracker.Instance.TargetDistance;
            
            // ═══ NEW: Different message cho endless ═══
            if (GameModeManager.Instance.CurrentMode == GameMode.Endless)
            {
                return $"💥 Hit an obstacle!\n\n" +
                    $"You ran {distance:F0}m\n" +
                    $"Best: {PlayerDataManager.Instance.GetBestEndlessDistance():F0}m";
            }
            else
            {
                return $"💥 Hit an obstacle!\n\n" +
                    $"Distance: {distance:F0}m\n" +
                    $"Progress: {progress:F0}%\n" +
                    $"Target: {target:F0}m to Home";
            }
        }

        return "Game Over!";
    }

    #endregion

    #region Victory UI

    private void UpdateVictoryUI()
    {
        if (GameManager.Instance == null) return;

        int finalScore = GameManager.Instance.Score;
        int finalCoins = GameManager.Instance.Coins;

        if (finalScore > _highScore)
        {
            _highScore = finalScore;
            PlayerPrefs.SetInt("HighScore", _highScore);
            PlayerPrefs.Save();
        }

        if (victoryTitleText != null)
        {
            victoryTitleText.text = "🎉 YOU ESCAPED! 🎉";
        }

        if (victoryScoreText != null)
        {
            victoryScoreText.text = $"Final Score: {finalScore}";
        }

        if (victoryCoinsText != null)
        {
            victoryCoinsText.text = $"Coins: {finalCoins}";
        }

        if (victoryMessageText != null)
        {
            victoryMessageText.text = "You made it home safely!\n🏠 Safe at last! 🏠";
        }

        if (victoryStatsText != null)
        {
            victoryStatsText.text = GetVictoryStatsText();
        }
        
        // ═══ NEW: Show/Hide Next Level button ═══
        UpdateNextLevelButton();
    }

    /// <summary>
    /// Update Next Level button visibility - NEW
    /// </summary>
    private void UpdateNextLevelButton()
    {
        if (victoryNextLevelButton == null) return;
        
        // Only show in Level mode
        if (GameModeManager.Instance.CurrentMode != GameMode.Level)
        {
            victoryNextLevelButton.gameObject.SetActive(false);
            Debug.Log("[UIManager] Hiding Next Level button (not Level mode)");
            return;
        }
        
        // Check if has next level
        bool hasNextLevel = GameModeManager.Instance.HasNextLevel();
        
        victoryNextLevelButton.gameObject.SetActive(hasNextLevel);
        victoryNextLevelButton.interactable = hasNextLevel;
        Debug.Log("[UIManager] Has Next Level");
        
        // Update button text
        TextMeshProUGUI buttonText = victoryNextLevelButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null && hasNextLevel)
        {
            LevelData nextLevel = GameModeManager.Instance.GetNextLevel();
            if (nextLevel != null)
            {
                buttonText.text = $"NEXT: Level {nextLevel.levelNumber}";
            }
            else
            {
                buttonText.text = "NEXT LEVEL";
            }
        }
    }

    /// <summary>
    /// Next Level button handler - NEW
    /// </summary>
    private void OnNextLevelButtonClicked()
    {
        Debug.Log("[UIManager] Next Level button clicked");
        AudioManager.Instance?.PlayButtonClickSound();
        
        // Reset timeScale
        Time.timeScale = 1f;
        
        // Load next level
        if (GameModeManager.Instance != null)
        {
            bool success = GameModeManager.Instance.LoadNextLevel();
            
            if (success)
            {
                Debug.Log("[UIManager] Loading next level...");
                GameManager.Instance?.RestartGame();
            }
            else
            {
                Debug.LogError("[UIManager] No next level available!");
            }
        }
    }

    /// <summary>
    /// Get victory stats - MODIFIED
    /// </summary>
    private string GetVictoryStatsText()
    {
        string stats = "═══ PERFORMANCE ═══\n\n";

        if (DistanceTracker.Instance != null)
        {
            stats += $"📏 Distance: {DistanceTracker.Instance.CurrentDistance:F0}m\n";
        }

        if (GameManager.Instance.GetLevelManager() != null)
        {
            float timeLeft = GameManager.Instance.GetLevelManager().GetTimeRemaining();
            stats += $"⏱️ Time: {Mathf.FloorToInt(timeLeft)}s\n";
        }

        // ═══ CHANGED: Dog escape instead of urgency ═══
        if (DogChaseController.Instance != null)
        {
            float dogDistance = DogChaseController.Instance.DogDistance;
            stats += $"🐕 Dog Distance: {dogDistance:F0}m (Escaped!)\n";
        }

        /* ═══ COMMENTED OUT: URGENCY METER ═══
        if (DiarrheaMeter.Instance != null)
        {
            float urgency = DiarrheaMeter.Instance.CurrentValue;
            stats += $"💩 Urgency: {urgency:F0}%\n";
        }
        */

        stats += $"🪙 Coins: {GameManager.Instance.Coins}\n";

        if (_highScore == GameManager.Instance.Score)
        {
            stats += "\n🏆 NEW HIGH SCORE! 🏆";
        }

        return stats;
    }

    #endregion


    #region Endless Mode UI - NEW

    /// <summary>
    /// Update UI cho Endless mode
    /// </summary>
    private void UpdateEndlessModeUI()
    {
        if (DistanceTracker.Instance == null) return;
        
        float currentDistance = DistanceTracker.Instance.CurrentDistance;
        float bestDistance = PlayerDataManager.Instance.GetBestEndlessDistance();
        
        // ═══ HIDE DISTANCE SLIDER ═══
        if (distanceSlider != null && distanceSlider.gameObject.activeSelf)
        {
            distanceSlider.gameObject.SetActive(false);
        }
        
        // ═══ SHOW CURRENT + BEST DISTANCE TEXT ═══
        if (distanceText != null)
        {
            distanceText.gameObject.SetActive(true);
            
            // Format: Current distance + Best record
            distanceText.text = $"🏃 {currentDistance:F0}m\n" +
                            $"🏆 Best: {bestDistance:F0}m";
            
            // Highlight if breaking record
            if (currentDistance > bestDistance)
            {
                distanceText.color = Color.yellow; // New record!
            }
            else
            {
                distanceText.color = Color.white;
            }
        }
    }

    /// <summary>
    /// Update UI cho Level mode
    /// </summary>
    private void UpdateLevelModeUI()
    {
        // Existing distance slider logic (already implemented)
        // Slider shows progress to home
        if (distanceSlider != null && !distanceSlider.gameObject.activeSelf)
        {
            distanceSlider.gameObject.SetActive(true);
        }
        
        if (distanceText != null)
        {
            distanceText.color = Color.white;
        }
    }

    #endregion


    #region Button Handlers - Pause Panel

    /// <summary>
    /// Pause button - FIXED: Disable input properly
    /// </summary>
    private void OnPauseButtonClicked()
    {
        Debug.Log("[UIManager] Pause button clicked");
        AudioManager.Instance?.PlayButtonClickSound();

        // Disable input when pausing
        DisableGameInput();

        GameManager.Instance?.PauseGame();
    }

    /// <summary>
    /// Resume button - FIXED: Prevent input conflict
    /// </summary>
    private void OnResumeButtonClicked()
    {
        Debug.Log("[UIManager] Resume button clicked");
        AudioManager.Instance?.PlayButtonClickSound();

        // ← FIX: Clear input state BEFORE resuming
        ClearInputState();

        // Resume game
        GameManager.Instance?.ResumeGame();

        // ← FIX: Re-enable input after small delay
        StartCoroutine(EnableInputAfterDelay(0.2f));
    }

    #endregion

    #region Button Handlers - Common

    /// <summary>
    /// Restart button - FIXED: Ensure input will be enabled
    /// </summary>
    private void OnRestartButtonClicked()
    {
        Debug.Log("[UIManager] Restart button clicked");
        AudioManager.Instance?.PlayButtonClickSound();

        // Reset timeScale
        Time.timeScale = 1f;

        // Note: Input will be auto-enabled by SceneController after scene loads
        Debug.Log("[UIManager] Restarting game... (Input will be enabled after load)");

        GameManager.Instance?.RestartGame();
    }

    /// <summary>
    /// Main Menu button - FIXED: Disable input when leaving gameplay
    /// </summary>
    private void OnMainMenuButtonClicked()
    {
        Debug.Log("[UIManager] Main Menu button clicked");
        AudioManager.Instance?.PlayButtonClickSound();

        // Reset timeScale
        Time.timeScale = 1f;

        // Disable input (menu doesn't need it)
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetEnabled(false);
        }

        Debug.Log($"[UIManager] Going to menu... (TimeScale: {Time.timeScale})");

        if (SceneController.Instance != null)
        {
            SceneController.Instance.LoadMenuScene();
        }
    }

    /// <summary>
    /// Play Again button - FIXED: Ensure input will be enabled
    /// </summary>
    private void OnPlayAgainButtonClicked()
    {
        Debug.Log("[UIManager] Play Again button clicked");
        AudioManager.Instance?.PlayButtonClickSound();

        // Reset timeScale
        Time.timeScale = 1f;

        // Note: Input will be auto-enabled by SceneController after scene loads
        Debug.Log("[UIManager] Restarting game... (Input will be enabled after load)");

        GameManager.Instance?.RestartGame();
    }

    #endregion

    #region Audio Volume Handlers

    private void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
            UpdateMusicVolumeText(value);
        }
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value);
            UpdateSFXVolumeText(value);
        }
    }

    #endregion

    #region Event Handlers

    private void OnScoreChanged(int newScore)
    {
        UpdateScore(newScore);
    }

    private void OnCoinCollected(int newCoinCount)
    {
        UpdateCoins(newCoinCount);
        //StartCoroutine(CoinCollectAnimation());
    }

    private IEnumerator CoinCollectAnimation()
    {
        if (coinsText == null) yield break;

        Vector3 originalScale = coinsText.transform.localScale;
        Vector3 targetScale = originalScale * 1.2f;

        float time = 0f;
        while (time < 0.1f)
        {
            coinsText.transform.localScale = Vector3.Lerp(originalScale, targetScale, time / 0.1f);
            time += Time.unscaledDeltaTime;
            yield return null;
        }

        time = 0f;
        while (time < 0.1f)
        {
            coinsText.transform.localScale = Vector3.Lerp(targetScale, originalScale, time / 0.1f);
            time += Time.unscaledDeltaTime;
            yield return null;
        }

        coinsText.transform.localScale = originalScale;
    }

    #endregion


    #region Input Management - NEW

    /// <summary>
    /// Disable game input - Prevent input during pause/gameover
    /// </summary>
    private void DisableGameInput()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetEnabled(false);
            Debug.Log("[UIManager] Input disabled");
        }
    }

    /// <summary>
    /// Enable game input
    /// </summary>
    private void EnableGameInput()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetEnabled(true);
            Debug.Log("[UIManager] Input enabled");
        }
    }

    /// <summary>
    /// Clear input state - Prevent lingering touches/swipes
    /// </summary>
    private void ClearInputState()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.ClearInputState();
            Debug.Log("[UIManager] Input state cleared");
        }
    }

    /// <summary>
    /// Enable input after delay - Prevent resume button from triggering swipe
    /// </summary>
    private IEnumerator EnableInputAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        EnableGameInput();
    }

    #endregion


    #region Cleanup

    void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.StopListening(GameEvents.SCORE_CHANGED, OnScoreChanged);
            EventManager.Instance.StopListening(GameEvents.COIN_COLLECTED, OnCoinCollected);
        }

        if (DistanceTracker.Instance != null)
        {
            DistanceTracker.Instance.OnDistanceChanged -= UpdateDistanceUI;
        }

        // ═══ CHANGED: Unsubscribe from Dog Chase events ═══
        if (DogChaseController.Instance != null)
        {
            DogChaseController.Instance.OnDogCatchPlayer -= OnDogCatchPlayer;
            DogChaseController.Instance.OnDogDisappear -= OnDogDisappear;
        }

        /* ═══ COMMENTED OUT: OLD DIARRHEA METER ═══
        if (DiarrheaMeter.Instance != null)
        {
            DiarrheaMeter.Instance.OnMeterChanged -= UpdateDiarrheaMeterUI;
        }
        */
    }

    #endregion
}