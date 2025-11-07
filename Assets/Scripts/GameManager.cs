using System.Collections;
using UnityEngine;

/// <summary>
/// Game Manager - UPDATED: Scene-aware management
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Singleton
    
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
            }
            return _instance;
        }
    }

    #endregion

    #region Properties

    private GameState _currentState;
    public GameState CurrentState
    {
        get { return _currentState; }
        private set
        {
            if (_currentState != value)
            {
                _currentState = value;
                OnStateChanged(value);
            }
        }
    }

    private int _score;
    public int Score
    {
        get { return _score; }
        private set
        {
            _score = value;
            EventManager.Instance?.TriggerEvent(GameEvents.SCORE_CHANGED, _score);
        }
    }

    private int _coins;
    public int Coins
    {
        get { return _coins; }
        private set
        {
            _coins = value;
            EventManager.Instance?.TriggerEvent(GameEvents.COIN_COLLECTED, _coins);
        }
    }

    #endregion

    #region References

    [Header("References")]
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private JSONSectionSpawner jsonSpawner;
    [SerializeField] private CharacterSpawner characterSpawner;

    private PlayerController playerController;

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        // ← CHANGED: Don't use DontDestroyOnLoad, let it be scene-specific
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        FindReferences();
    }

    void Start()
    {
        InitializeGame();
        // ← CHANGED: Start in Playing state immediately
        StartGame();
    }

    void Update()
    {
        HandleInput();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Find references - UPDATED: Find CharacterSpawner
    /// </summary>
    private void FindReferences()
    {
        // Find CharacterSpawner
        if (characterSpawner == null)
        {
            characterSpawner = FindObjectOfType<CharacterSpawner>();
            
            if (characterSpawner == null)
            {
                Debug.LogError("[GameManager] ❌ CharacterSpawner not found!");
            }
            else
            {
                Debug.Log("[GameManager] ✓ CharacterSpawner found");
            }
        }
        
        // Get player from spawner
        if (characterSpawner != null)
        {
            playerController = characterSpawner.GetPlayerController();
            
            if (playerController != null)
            {
                Debug.Log("[GameManager] ✓ PlayerController found via CharacterSpawner");
            }
        }
        
        // Find UIManager
        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
        }
        
        // Find LevelManager
        if (levelManager == null)
        {
            levelManager = FindObjectOfType<LevelManager>();
        }
    }

    private void InitializeGame()
    {

        // Validate references
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();
        
        if (levelManager == null)
            levelManager = FindObjectOfType<LevelManager>();
        
        if (uiManager == null)
            uiManager = FindObjectOfType<UIManager>();
        
        if (jsonSpawner == null)
            jsonSpawner = FindObjectOfType<JSONSectionSpawner>();

        _currentState = GameState.MainMenu;
        _score = 0;
        _coins = 0;
    }

    #endregion

    #region State Management

    private void ChangeState(GameState newState)
    {
        ExitCurrentState();
        CurrentState = newState;
        EnterNewState(newState);
    }

    private void EnterNewState(GameState state)
    {
        switch (state)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                uiManager?.ShowGameUI();
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                uiManager?.ShowPauseMenu();
                EventManager.Instance?.TriggerEvent(GameEvents.GAME_PAUSED);
                break;

            case GameState.GameOver:
                Time.timeScale = 0f;
                uiManager?.ShowGameOverUI();
                EventManager.Instance?.TriggerEvent(GameEvents.GAME_OVER);
                break;

            case GameState.Victory:
                Time.timeScale = 0f;
                uiManager?.ShowVictoryUI();
                break;
        }
    }

    private void ExitCurrentState()
    {
        // Cleanup logic
    }

    private void OnStateChanged(GameState newState)
    {
        Debug.Log($"[GameManager] State changed to: {newState}");
    }

    #endregion

    #region Character Management - NEW
    
    /// <summary>
    /// Called by CharacterSpawner when character is spawned
    /// </summary>
    public void OnCharacterSpawned(PlayerController player)
    {
        playerController = player;
        
        // Notify camera
        NotifyCamera();
    }
    
    /// <summary>
    /// Notify camera to find new player
    /// </summary>
    private void NotifyCamera()
    {
        CameraFollowController camera = FindObjectOfType<CameraFollowController>();
        
        if (camera != null && playerController != null)
        {
            camera.SetTarget(playerController.transform);
        }
    }
    
    #endregion

    #region Game Flow

    /// <summary>
    /// Start game - UPDATED: Use CharacterSpawner
    /// </summary>
    public void StartGame()
    {
        Debug.Log("[GameManager] ===== STARTING GAME =====");

        // Validate player
        if (playerController == null)
        {
            Debug.LogWarning("[GameManager] PlayerController was null, trying to get from spawner...");
            
            if (characterSpawner != null)
            {
                playerController = characterSpawner.GetPlayerController();
            }
            
            if (playerController == null)
            {
                Debug.LogError("[GameManager] ❌ CANNOT START GAME - No player!");
                return;
            }
        }

        Score = 0;
        Coins = 0;

        // Reset player
        playerController.ResetPlayer();
        
        // Start drinking sequence
        playerController.StartDrinkingSequence();
        Debug.Log("[GameManager] ✓ Drinking sequence started");

        // Start level
        if (levelManager != null)
        {
            levelManager.StartFirstLevel();
        }

        // Clear powerups
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.ClearAllPowerUps();
        }

        // Enable input
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetEnabled(true);
            InputManager.Instance.ClearInputState();
        }

        ChangeState(GameState.Playing);
        
        Debug.Log("[GameManager] ===== GAME START COMPLETE =====");
    }

    /// <summary>
    /// Record game statistics and save - ADD THIS
    /// </summary>
    private void RecordGameStats(bool isVictory)
    {
        if (PlayerDataManager.Instance == null) return;

        // Record game played
        PlayerDataManager.Instance.RecordGamePlayed();

        // Get stats
        int finalScore = Score;
        float distance = DistanceTracker.Instance != null ? 
            DistanceTracker.Instance.CurrentDistance : 0f;

        if (isVictory)
        {
            PlayerDataManager.Instance.RecordVictory(finalScore, distance);
        }
        else
        {
            PlayerDataManager.Instance.RecordLoss(distance);
        }

        Debug.Log($"[GameManager] Stats recorded - Victory: {isVictory}, Score: {finalScore}, Distance: {distance:F0}m");
    }

    public void PauseGame()
    {
        if (CurrentState == GameState.Playing)
        {
            ChangeState(GameState.Paused);
            
            // ← NEW: Disable input when pausing
            if (InputManager.Instance != null)
            {
                InputManager.Instance.SetEnabled(false);
            }
            
            Debug.Log("[GameManager] Game paused (Input disabled)");
        }
    }

    public void ResumeGame()
    {
        if (CurrentState == GameState.Paused)
        {
            ChangeState(GameState.Playing);
            EventManager.Instance?.TriggerEvent(GameEvents.GAME_RESUMED);

            // ← NEW: Clear input state BEFORE re-enabling
            if (InputManager.Instance != null)
            {
                InputManager.Instance.ClearInputState();

                // Re-enable after small delay
                StartCoroutine(EnableInputAfterDelay(0.2f));
            }

            Debug.Log("[GameManager] Game resumed");
        }
    }
    
    /// <summary>
    /// Enable input after delay - NEW
    /// </summary>
    private IEnumerator EnableInputAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetEnabled(true);
            Debug.Log("[GameManager] Input re-enabled after delay");
        }
    }

    /// <summary>
    /// Game Over - UPDATED: Optional delay for dramatic effect
    /// </summary>
    public void GameOver()
    {
        if (CurrentState != GameState.Playing) return;

        // ═══ OPTIONAL: Add small delay for dramatic pause ═══
        StartCoroutine(GameOverSequence());
    }

    /// <summary>
    /// Game Over sequence with optional delay - NEW
    /// </summary>
    private IEnumerator GameOverSequence()
    {
        Debug.Log("[GameManager] 💀 Game Over - starting sequence...");
        
        // Optional: Small delay after animation for dramatic effect
        yield return new WaitForSeconds(0.3f);
        
        // Change state
        ChangeState(GameState.GameOver);

        if (playerController != null)
            playerController.StopPlayer();

        DiarrheaMeter.Instance?.StopMeter();
        DistanceTracker.Instance?.StopTracking();
        PowerUpManager.Instance?.ClearAllPowerUps();
        
        // Disable input on game over
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetEnabled(false);
        }

        // Record game stats
        RecordGameStats(false);
        
        Debug.Log("[GameManager] ✓ Game Over sequence complete");
    }

    public void Victory()
    {
        if (CurrentState != GameState.Playing) return;

        ChangeState(GameState.Victory);

        if (playerController != null)
            playerController.StopPlayer();

        DiarrheaMeter.Instance?.StopMeter();
        DistanceTracker.Instance?.StopTracking();
        
        // ← NEW: Disable input on victory
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetEnabled(false);
        }

        CalculateVictoryBonus();
        RecordGameStats(true);

        Debug.Log("[GameManager] 🎉 VICTORY! (Input disabled)");
    }


    private void CalculateVictoryBonus()
    {
        int bonus = 1000;

        if (levelManager != null)
        {
            float timeLeft = levelManager.GetTimeRemaining();
            int timeBonus = Mathf.RoundToInt(timeLeft * 10f);
            bonus += timeBonus;
        }

        if (DiarrheaMeter.Instance != null)
        {
            float meterPercent = DiarrheaMeter.Instance.MeterPercent;
            int meterBonus = Mathf.RoundToInt((1f - meterPercent) * 500f);
            bonus += meterBonus;
        }

        AddScore(bonus);
    }

    /// <summary>
    /// Restart game - FIXED: Reset timeScale + input
    /// </summary>
    public void RestartGame()
    {
        // ← FIX: Reset timeScale FIRST
        Time.timeScale = 1f;
        
        Debug.Log($"[GameManager] Restarting... (TimeScale: {Time.timeScale})");
        
        SceneController.Instance.LoadGameplayScene();
    }

    /// <summary>
    /// Back to menu - FIXED: Reset timeScale
    /// </summary>
    public void BackToMenu()
    {
        // ← FIX: Reset timeScale FIRST
        Time.timeScale = 1f;
        
        Debug.Log($"[GameManager] Back to menu... (TimeScale: {Time.timeScale})");
        
        SceneController.Instance.LoadMenuScene();
    }

    #endregion

    #region Score Management

    public void AddScore(int amount)
    {
        if (amount <= 0) return;
        Score += amount;
    }

    public void AddCoin()
    {
        Coins++;
        AddScore(10);
        
        // Add gold to persistent data
        PlayerDataManager.Instance?.AddGold(1);
        // Add to total coins (persistent)
        int totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);
        totalCoins++;
        PlayerPrefs.SetInt("TotalCoins", totalCoins);
        PlayerPrefs.Save();
    }

    private void SaveHighScore()
    {
        int currentHighScore = PlayerPrefs.GetInt("HighScore", 0);
        
        if (Score > currentHighScore)
        {
            PlayerPrefs.SetInt("HighScore", Score);
            PlayerPrefs.Save();
            
            Debug.Log($"[GameManager] 🏆 New High Score: {Score}!");
        }
    }

    #endregion

    #region Input Handling

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (CurrentState == GameState.Playing)
            {
                PauseGame();
            }
            else if (CurrentState == GameState.Paused)
            {
                ResumeGame();
            }
        }
    }

    #endregion

    #region Getters

    public PlayerController GetPlayer() => playerController;
    public LevelManager GetLevelManager() => levelManager;
    public UIManager GetUIManager() => uiManager;
    public JSONSectionSpawner GetSpawner() => jsonSpawner;

    #endregion
}