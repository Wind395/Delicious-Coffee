using UnityEngine;

/// <summary>
/// Diarrhea Meter - FIXED: Trigger player death animation before GameOver
/// </summary>
public class DiarrheaMeter : MonoBehaviour
{
    #region Singleton
    
    private static DiarrheaMeter _instance;
    public static DiarrheaMeter Instance => _instance;
    
    #endregion

    #region Serialized Fields
    
    [Header("Settings")]
    [SerializeField] private float increasePerSecond = 1.67f;
    [SerializeField] private float maxMeter = 100f;
    
    [Header("Item Reductions")]
    [SerializeField] private float medicineReduction = 30f;
    [SerializeField] private float iceTeaReduction = 20f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    #endregion

    #region State
    
    private float _currentMeter;
    private bool _isActive;
    private bool _hasTriggeredFull = false; // ← NEW: Prevent multiple triggers
    
    #endregion

    #region Properties
    
    public float CurrentValue => _currentMeter;
    public float MaxValue => maxMeter;
    public bool IsFull => _currentMeter >= maxMeter;
    public float MeterPercent => _currentMeter / maxMeter;
    
    #endregion

    #region Events
    
    public event System.Action<float, float, float> OnMeterChanged;
    public event System.Action OnMeterFull;
    
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
    }

    void Update()
    {
        if (_isActive && !IsFull)
        {
            UpdateMeter();
        }
    }
    
    #endregion

    #region Initialization
    
    private void Initialize()
    {
        ResetMeter();
        
        if (EventManager.Instance != null)
        {
            EventManager.Instance.StartListening(GameEvents.GAME_STARTED, OnGameStarted);
            EventManager.Instance.StartListening(GameEvents.GAME_OVER, OnGameOver);
        }
    }
    
    #endregion

    #region Core Logic
    
    /// <summary>
    /// Update meter value
    /// </summary>
    private void UpdateMeter()
    {
        _currentMeter += increasePerSecond * Time.deltaTime;
        _currentMeter = Mathf.Min(_currentMeter, maxMeter);
        
        // Trigger event for UI update
        OnMeterChanged?.Invoke(_currentMeter, maxMeter, MeterPercent);
        
        // Check if full
        if (IsFull && !_hasTriggeredFull)
        {
            OnMeterFullInternal();
        }
    }
    
    #endregion

    #region Public API
    
    public void ReduceMeter(float amount)
    {
        _currentMeter -= amount;
        _currentMeter = Mathf.Max(0f, _currentMeter);
        
        // ← NEW: Reset full trigger if meter reduced
        if (_currentMeter < maxMeter)
        {
            _hasTriggeredFull = false;
        }
        
        OnMeterChanged?.Invoke(_currentMeter, maxMeter, MeterPercent);
        
        if (showDebugLogs)
        {
            Debug.Log($"[Meter] Reduced by {amount}. Current: {_currentMeter:F1}%");
        }
    }

    public void ApplyMedicine()
    {
        ReduceMeter(medicineReduction);
    }

    public void ApplyIceTea()
    {
        ReduceMeter(iceTeaReduction);
    }

    public void ResetMeter()
    {
        _currentMeter = 0f;
        _hasTriggeredFull = false; // ← NEW: Reset trigger flag
        OnMeterChanged?.Invoke(_currentMeter, maxMeter, MeterPercent);
    }

    public void StartTracking()
    {
        _isActive = true;
        _hasTriggeredFull = false; // ← NEW: Reset on start
        ResetMeter();
        Debug.Log("[Meter] Started tracking");
    }

    public void StopTracking()
    {
        _isActive = false;
        Debug.Log("[Meter] Stopped tracking");
    }

    public void StopMeter()
    {
        StopTracking();
    }
    
    #endregion

    #region Meter Full Logic - UPDATED
    
    /// <summary>
    /// Called when meter reaches 100% - UPDATED: Trigger player death animation
    /// </summary>
    private void OnMeterFullInternal()
    {
        // ← CRITICAL: Set flag immediately to prevent multiple triggers
        _hasTriggeredFull = true;
        
        // Stop meter
        _isActive = false;
        
        Debug.Log("[Meter] ═══════════════════════════════════");
        Debug.Log("[Meter] 💩 METER FULL! TRIGGERING DEATH!");
        Debug.Log("[Meter] ═══════════════════════════════════");
        
        // Trigger event (for UI warning sound, etc)
        OnMeterFull?.Invoke();
        
        // ═══ FIX: Trigger PLAYER DEATH instead of direct GameOver ═══
        PlayerController player = FindPlayer();
        
        if (player != null && player.IsAlive)
        {
            Debug.Log("[Meter] → Triggering player death from meter full");
            
            // Trigger death (will play Fall Flat animation)
            player.TriggerDeathFromMeterFull();
            
            // GameOver will be called AFTER death animation completes
            // via PlayerAnimationController.OnDeathAnimationComplete event
        }
        else
        {
            Debug.LogWarning("[Meter] Player not found or already dead - calling GameOver directly");
            
            // Fallback: Direct GameOver if player not found
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
        }
    }
    
    /// <summary>
    /// Find player in scene
    /// </summary>
    private PlayerController FindPlayer()
    {
        // Method 1: Via GameManager
        if (GameManager.Instance != null)
        {
            PlayerController player = GameManager.Instance.GetPlayer();
            if (player != null) return player;
        }
        
        // Method 2: Via tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            return playerObj.GetComponent<PlayerController>();
        }
        
        // Method 3: FindObjectOfType (last resort)
        return FindObjectOfType<PlayerController>();
    }
    
    #endregion

    #region Event Handlers
    
    private void OnGameStarted()
    {
        StartTracking();
    }

    private void OnGameOver()
    {
        StopTracking();
    }
    
    #endregion

    #region Cleanup
    
    void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.StopListening(GameEvents.GAME_STARTED, OnGameStarted);
            EventManager.Instance.StopListening(GameEvents.GAME_OVER, OnGameOver);
        }
    }
    
    #endregion
}