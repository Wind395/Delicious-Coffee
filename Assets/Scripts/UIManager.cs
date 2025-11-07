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
    [SerializeField] private TextMeshProUGUI levelTimerText;
    [SerializeField] private TextMeshProUGUI levelInfoText;
    [SerializeField] private Button pauseButton;
    
    #endregion

    #region Meters UI
    
    [Header("=== METERS ===")]
    [SerializeField] private Slider diarrheaMeterSlider;
    [SerializeField] private TextMeshProUGUI diarrheaMeterText;
    [SerializeField] private Slider distanceSlider;
    [SerializeField] private TextMeshProUGUI distanceText;

    [Header("Meter Colors")]
    [SerializeField] private Color meterNormalColor = Color.green;
    [SerializeField] private Color meterWarningColor = Color.yellow;
    [SerializeField] private Color meterCriticalColor = Color.red;
    
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
        ShowGameUI();
    }

    void Update()
    {
        if (GameManager.Instance != null && 
            GameManager.Instance.CurrentState == GameState.Playing)
        {
            UpdatePowerUpUI();
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

        if (DiarrheaMeter.Instance != null)
        {
            DiarrheaMeter.Instance.OnMeterChanged += UpdateDiarrheaMeterUI;
        }
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
        victoryPlayAgainButton?.onClick.AddListener(OnPlayAgainButtonClicked);
        victoryMainMenuButton?.onClick.AddListener(OnMainMenuButtonClicked);
    }

    private void ValidateReferences()
    {
        if (distanceSlider == null)
            Debug.LogWarning("[UIManager] Distance Slider not assigned!");
        
        if (diarrheaMeterSlider == null)
            Debug.LogWarning("[UIManager] Diarrhea Meter Slider not assigned!");
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
        UpdateAllGameplayUI();
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

    #region Meter UI Updates

    private void UpdateDistanceUI(float current, float target, float progress)
    {
        if (distanceSlider != null)
        {
            distanceSlider.value = progress;
        }
        
        if (distanceText != null)
        {
            distanceText.text = $"{current:F0}m / {target:F0}m";
        }
    }

    public void UpdateDiarrheaMeterUI(float current, float max, float percent)
    {
        if (diarrheaMeterSlider != null)
        {
            diarrheaMeterSlider.value = percent;
            
            Image fillImage = diarrheaMeterSlider.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                if (percent < 0.5f)
                    fillImage.color = meterNormalColor;
                else if (percent < 0.8f)
                    fillImage.color = meterWarningColor;
                else
                    fillImage.color = meterCriticalColor;
            }
        }
        
        if (diarrheaMeterText != null)
        {
            diarrheaMeterText.text = $"{current:F0}%";
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

        // Update high score
        if (finalScore > _highScore)
        {
            _highScore = finalScore;
            PlayerPrefs.SetInt("HighScore", _highScore);
            PlayerPrefs.Save();
        }

        // Title
        if (gameOverTitleText != null)
        {
            gameOverTitleText.text = "GAME OVER";
        }

        // Scores
        if (finalScoreText != null)
            finalScoreText.text = $"Score: {finalScore}";

        if (finalCoinsText != null)
            finalCoinsText.text = $"Coins: {finalCoins}";

        if (highScoreText != null)
            highScoreText.text = $"High Score: {_highScore}";

        // Reason
        if (gameOverReasonText != null)
        {
            gameOverReasonText.text = GetGameOverReasonText();
        }
    }

    private string GetGameOverReasonText()
    {
        // Check meter full
        if (DiarrheaMeter.Instance != null && DiarrheaMeter.Instance.IsFull)
        {
            return "💩 Couldn't hold it anymore!\n" +
                   "Urgency meter reached 100%!\n" +
                   "Collect items to reduce it next time!";
        }

        // Check distance
        if (DistanceTracker.Instance != null)
        {
            float progress = DistanceTracker.Instance.Progress * 100f;
            float distance = DistanceTracker.Instance.CurrentDistance;
            
            return $"💥 Hit an obstacle!\n\n" +
                   $"Distance: {distance:F0}m\n" +
                   $"Progress: {progress:F0}%\n" +
                   $"Target: {DistanceTracker.Instance.TargetDistance:F0}m";
        }

        return "Game Over!";
    }

    #endregion

    #region Victory UI Updates

    private void UpdateVictoryUI()
    {
        if (GameManager.Instance == null) return;

        int finalScore = GameManager.Instance.Score;
        int finalCoins = GameManager.Instance.Coins;

        // Update high score
        if (finalScore > _highScore)
        {
            _highScore = finalScore;
            PlayerPrefs.SetInt("HighScore", _highScore);
            PlayerPrefs.Save();
        }

        // Title
        if (victoryTitleText != null)
        {
            victoryTitleText.text = "🎉 VICTORY! 🎉";
        }

        // Score
        if (victoryScoreText != null)
        {
            victoryScoreText.text = $"Final Score: {finalScore}";
        }

        // Coins
        if (victoryCoinsText != null)
        {
            victoryCoinsText.text = $"Coins: {finalCoins}";
        }

        // Message
        if (victoryMessageText != null)
        {
            victoryMessageText.text = "You made it to the toilet in time!\n🚽 Relief! 🚽";
        }

        // Stats
        if (victoryStatsText != null)
        {
            victoryStatsText.text = GetVictoryStatsText();
        }
    }

    private string GetVictoryStatsText()
    {
        string stats = "═══ PERFORMANCE ═══\n\n";

        // Distance
        if (DistanceTracker.Instance != null)
        {
            stats += $"📏 Distance: {DistanceTracker.Instance.CurrentDistance:F0}m\n";
        }

        // Time
        if (GameManager.Instance.GetLevelManager() != null)
        {
            float timeLeft = GameManager.Instance.GetLevelManager().GetTimeRemaining();
            stats += $"⏱️ Time Left: {Mathf.FloorToInt(timeLeft)}s\n";
        }

        // Urgency
        if (DiarrheaMeter.Instance != null)
        {
            float urgency = DiarrheaMeter.Instance.CurrentValue;
            stats += $"💩 Urgency: {urgency:F0}%\n";
        }

        // Coins
        stats += $"🪙 Coins: {GameManager.Instance.Coins}\n";

        // High Score
        if (_highScore == GameManager.Instance.Score)
        {
            stats += "\n🏆 NEW HIGH SCORE! 🏆";
        }

        return stats;
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

        if (DiarrheaMeter.Instance != null)
        {
            DiarrheaMeter.Instance.OnMeterChanged -= UpdateDiarrheaMeterUI;
        }
    }
    
    #endregion
}