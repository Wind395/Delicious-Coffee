using UnityEngine;

/// <summary>
/// Level Manager - UPDATED: Time is just a timer, not win condition
/// Win condition = Distance only
/// </summary>
public class LevelManager : MonoBehaviour
{

    #region Singleton
    
    public static LevelManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    #endregion

    #region Serialized Fields
    
    [Header("References")]
    [SerializeField] private JSONSectionSpawner jsonSpawner;
    [SerializeField] private PlayerController player;
    [SerializeField] private UIManager uiManager;
    
    [Header("Session Settings")]
    [SerializeField] private float sessionDuration = 60f; // Just for display, not win condition
    
    #endregion

    #region State
    
    private float _sessionTimer;
    private bool _isSessionActive;
    
    #endregion

    #region Properties
    
    public float TimeRemaining => _sessionTimer;
    public float SessionProgress => 1f - (_sessionTimer / sessionDuration);

    #endregion

    #region Unity Lifecycle
    
    void Start()
    {
        ValidateReferences();
        
        // ═══ NEW: Subscribe to victory event ═══
        if (EventManager.Instance != null)
        {
            EventManager.Instance.StartListening("OnToiletReached", OnVictory);
        }
    }

    void Update()
    {
        if (_isSessionActive && GameManager.Instance.CurrentState == GameState.Playing)
        {
            UpdateSessionTimer();
        }
    }
    
    #endregion

    #region Initialization

    /// <summary>
    /// Get elapsed time - NEW
    /// </summary>
    public float GetElapsedTime()
    {
        return sessionDuration - _sessionTimer;
    }
    
    private void ValidateReferences()
    {
        if (jsonSpawner == null)
            jsonSpawner = FindObjectOfType<JSONSectionSpawner>();
        
        if (player == null)
            player = FindObjectOfType<PlayerController>();
        
        if (uiManager == null)
            uiManager = FindObjectOfType<UIManager>();
    }
    
    #endregion

    #region Session Control
    
    /// <summary>
    /// Start game session
    /// </summary>
    public void StartFirstLevel()
    {
        _sessionTimer = sessionDuration;
        _isSessionActive = true;

        ApplySessionSettings();
        UpdateSessionUI();

        Debug.Log($"[LevelManager] Session started - Reach toilet before meter fills!");
    }

    /// <summary>
    /// Apply settings for endless mode
    /// </summary>
    private void ApplySessionSettings()
    {
        if (player != null)
        {
            player.SetForwardSpeed(10f);
            player.SetLaneChangeSpeed(10f);
        }
    }
    
    #endregion

    #region Timer Management - DISPLAY ONLY
    
    /// <summary>
    /// Update session timer - Just for display, NOT win condition
    /// </summary>
    private void UpdateSessionTimer()
    {
        _sessionTimer -= Time.deltaTime;

        // Update UI
        if (uiManager != null)
        {
            uiManager.UpdateLevelTimer(_sessionTimer);
        }

        // ← REMOVED: No auto game over when time up
        // Victory is determined by DistanceTracker only
        
        // Optional: Warning when time low
        if (_sessionTimer <= 10f && _sessionTimer > 9.9f)
        {
            Debug.LogWarning("[LevelManager] ⏰ 10 seconds remaining!");
        }
    }
    
    #endregion

    #region UI Updates
    
    private void UpdateSessionUI()
    {
        if (uiManager != null)
        {
            uiManager.UpdateLevelInfo(1, 1, "Endless Run");
        }
    }
    
    #endregion

    #region Public API
    
    /// <summary>
    /// Get remaining time
    /// </summary>
    public float GetTimeRemaining()
    {
        return _sessionTimer;
    }

    /// <summary>
    /// Restart session
    /// </summary>
    public void RestartSession()
    {
        StartFirstLevel();
    }

    /// <summary>
    /// Handle victory - Stop all level systems
    /// </summary>
    private void OnVictory()
    {
        _isSessionActive = false;
        
        Debug.Log("[LevelManager] 🚽 Victory - stopping level systems");
    }

    void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.StopListening("OnToiletReached", OnVictory);
        }
    }
    
    #endregion
}