using System.Collections;
using UnityEngine;


public class GameManager : MonoBehaviour
{
    #region Singleton
    
    private static GameManager _instance;
    public static GameManager Instance => _instance;

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

        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
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
            characterSpawner = FindAnyObjectByType<CharacterSpawner>();
        }
        
        // Get player from spawner
        if (characterSpawner != null)
        {
            playerController = characterSpawner.GetPlayerController();
        }
    }

    private void InitializeGame()
    {

        // Validate references
        if (playerController == null)
            playerController = FindAnyObjectByType<PlayerController>();
        
        if (jsonSpawner == null)
            jsonSpawner = FindAnyObjectByType<JSONSectionSpawner>();

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
        CameraFollowController camera = FindAnyObjectByType<CameraFollowController>();
        
        if (camera != null && playerController != null)
        {
            camera.SetTarget(playerController.transform);
        }
    }

    #endregion

    #region Game Flow

    public void StartGame()
    {

        // Validate player
        if (playerController == null)
        {
            if (characterSpawner != null)
            {
                playerController = characterSpawner.GetPlayerController();
            }

            if (playerController == null)
            {

                return;
            }
        }

        Score = 0;
        Coins = 0;

        // Reset player
        playerController.ResetPlayer();

        // ═══ NEW: Setup distance tracker theo mode ═══
        if (DistanceTracker.Instance != null)
        {
            float targetDistance = GameModeManager.Instance.GetTargetDistance();
            DistanceTracker.Instance.SetTargetDistance(targetDistance);
        }

        // ═══ NEW: Setup JSON spawner theo mode ═══
        if (jsonSpawner != null)
        {
            string sectionsFileName = GameModeManager.Instance.GetSectionsFileName();
            jsonSpawner.SetSectionsFileName(sectionsFileName);
        }

        // Start intro sequence
        if (DogIntroSequenceController.Instance != null)
        {
            DogIntroSequenceController.Instance.StartIntroSequence(playerController);

            DogIntroSequenceController.Instance.OnSequenceComplete += OnIntroSequenceComplete;
        }
        else
        {
            playerController.StartWalkingSequence();
        }

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

        if (UIManager.Instance != null)
        {
            UIManager.Instance.InitializeGameplayUI();
        }

        // Disable input (will enable after intro)
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetEnabled(false);
        }

        // Level mode - continue normally
        ContinueGameStart();

        ChangeState(GameState.Playing);
    }

    /// <summary>
    /// Continue game start (extracted from StartGame)
    /// </summary>
    private void ContinueGameStart()
    {
        // ═══ EXISTING STARTGAME CODE ═══
        
        Score = 0;
        Coins = 0;

        // Reset player
        playerController.ResetPlayer();

        // Setup distance tracker
        if (DistanceTracker.Instance != null)
        {
            float targetDistance = GameModeManager.Instance.GetTargetDistance();
            DistanceTracker.Instance.SetTargetDistance(targetDistance);
        }

        // Setup JSON spawner
        if (jsonSpawner != null)
        {
            string sectionsFileName = GameModeManager.Instance.GetSectionsFileName();
            jsonSpawner.SetSectionsFileName(sectionsFileName);
        }

        // Start intro sequence
        if (DogIntroSequenceController.Instance != null)
        {
            DogIntroSequenceController.Instance.StartIntroSequence(playerController);

            DogIntroSequenceController.Instance.OnSequenceComplete += OnIntroSequenceComplete;
        }

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

        // Disable input
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetEnabled(false);
        }

        ChangeState(GameState.Playing);
    }
    
    /// <summary>
    /// Called when intro sequence completes - NEW
    /// </summary>
    private void OnIntroSequenceComplete()
    {
        
        // Enable input
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetEnabled(true);
            InputManager.Instance.ClearInputState();
        }
        
        // Trigger game started event
        EventManager.Instance?.TriggerEvent(GameEvents.GAME_STARTED);
        
        // Unsubscribe
        if (DogIntroSequenceController.Instance != null)
        {
            DogIntroSequenceController.Instance.OnSequenceComplete -= OnIntroSequenceComplete;
        }
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
    }

    public void PauseGame()
    {
        if (CurrentState == GameState.Playing)
        {
            ChangeState(GameState.Paused);
            SaveData();
            
            // ← NEW: Disable input when pausing
            if (InputManager.Instance != null)
            {
                InputManager.Instance.SetEnabled(false);
            }
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
        }
    }

    /// <summary>
    /// Game Over - UPDATED: Instant transition
    /// </summary>
    public void GameOver()
    {
        if (CurrentState != GameState.Playing) return;

        // ═══ INSTANT STATE CHANGE (NO DELAY) ═══
        ChangeState(GameState.GameOver);
        SaveData();

        if (playerController != null)
            playerController.StopPlayer();

        DistanceTracker.Instance?.StopTracking();
        PowerUpManager.Instance?.ClearAllPowerUps();
        
        // Disable input on game over
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetEnabled(false);
        }

        // Record game stats
        RecordGameStats(false);

        // ═══ NEW: Submit to ranking if Endless mode ═══
        if (GameModeManager.Instance != null && 
            GameModeManager.Instance.CurrentMode == GameMode.Endless)
        {
            if (DistanceTracker.Instance != null && RankingManager.Instance != null)
            {
                float finalDistance = DistanceTracker.Instance.CurrentDistance;
                RankingManager.Instance.SubmitPlayerDistance(finalDistance);
                
                //Debug.Log($"[GameManager] Submitted distance to ranking: {finalDistance:F0}m");
            }
        }
    }

    public void Victory()
    {
        if (CurrentState != GameState.Playing) return;

        // ═══ 1. STOP GAME SYSTEMS ═══
        if (playerController != null)
            playerController.StopPlayer();

        DistanceTracker.Instance?.StopTracking();
        
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetEnabled(false);
        }

        // ═══ 2. CALCULATE & RECORD ═══
        SaveData();
        CalculateVictoryBonus();
        RecordGameStats(true);
        
        // ═══ 3. SAVE PROGRESS (UNLOCK NEXT LEVEL) - BEFORE UI ═══
        SaveGameProgress();
        
        // ═══ 4. NOW CHANGE STATE & SHOW UI ═══
        ChangeState(GameState.Victory);
    }

    
    /// <summary>
    /// Save game progress - NEW
    /// </summary>
    private void SaveGameProgress()
    {
        if (GameModeManager.Instance.CurrentMode == GameMode.Level)
        {
            // Level mode - Mark completed
            LevelData currentLevel = GameModeManager.Instance.SelectedLevel;
            
            if (currentLevel != null)
            {
                // Mark completed
                PlayerDataManager.Instance.MarkLevelCompleted(currentLevel.levelID);
                
                // Update record
                float time = LevelManager.Instance != null ? 
                    LevelManager.Instance.GetElapsedTime() : 0f;
                
                PlayerDataManager.Instance.UpdateLevelRecord(
                    currentLevel.levelID, 
                    time, 
                    Score
                );
                
                // Complete level in GameModeManager
                GameModeManager.Instance.CompleteCurrentLevel();
                
            }
        }
        else if (GameModeManager.Instance.CurrentMode == GameMode.Endless)
        {
            // Endless mode - Update best distance
            float distance = DistanceTracker.Instance != null ? 
                DistanceTracker.Instance.CurrentDistance : 0f;
            
            PlayerDataManager.Instance.UpdateEndlessRecord(distance);
            
        }
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

        AddScore(bonus);
    }

    /// <summary>
    /// Restart game - FIXED: Reset timeScale + input
    /// </summary>
    public void RestartGame()
    {
        // ← FIX: Reset timeScale FIRST
        Time.timeScale = 1f;
        
        SceneController.Instance.LoadGameplayScene();
    }

    /// <summary>
    /// Back to menu - FIXED: Reset timeScale
    /// </summary>
    public void BackToMenu()
    {
        // ← FIX: Reset timeScale FIRST
        Time.timeScale = 1f;
        
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
        // PlayerPrefs.Save();
    }

    private void SaveData()
    {
        PlayerPrefs.Save();
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

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveData();
        }
    }

    #region Getters

    public PlayerController GetPlayer() => playerController;
    public LevelManager GetLevelManager() => levelManager;
    public UIManager GetUIManager() => uiManager;
    public JSONSectionSpawner GetSpawner() => jsonSpawner;

    #endregion
}