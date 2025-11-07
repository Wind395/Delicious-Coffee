using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Distance Tracker - FIXED: Proper UI updates
/// </summary>
public class DistanceTracker : MonoBehaviour
{
    #region Singleton
    
    private static DistanceTracker _instance;
    public static DistanceTracker Instance => _instance;
    
    #endregion

    #region Serialized Fields - REMOVED UI (UI handled by UIManager)
    
    [Header("Settings")]
    [SerializeField] private float targetDistance = 1000f;
    
    #endregion

    #region State
    
    private Transform _player;
    private float _startZ;
    private float _currentDistance;
    private bool _isTracking;
    private bool _hasReachedGoal;

    [Header("Safe Zone")]
    [SerializeField] private float safeZoneTriggerDistance = 150f; // Khi còn 150m → clear obstacles
    private bool _hasTriggeredSafeZone = false;
    
    #endregion

    #region Properties
    
    public float CurrentDistance => _currentDistance;
    public float TargetDistance => targetDistance;
    public float Progress => Mathf.Clamp01(_currentDistance / targetDistance);
    public bool ReachedGoal => _hasReachedGoal;
    
    #endregion

    #region Events - Observer Pattern
    
    public event System.Action<float, float, float> OnDistanceChanged; // current, target, progress
    
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
        if (_isTracking && GameManager.Instance != null && 
            GameManager.Instance.CurrentState == GameState.Playing)
        {
            UpdateDistance();
            CheckVictoryCondition();
        }
    }
    
    #endregion

    #region Initialization
    
    private void Initialize()
    {
        FindPlayer();
        
        // Subscribe to game events
        if (EventManager.Instance != null)
        {
            EventManager.Instance.StartListening(GameEvents.GAME_STARTED, OnGameStarted);
            EventManager.Instance.StartListening(GameEvents.GAME_OVER, OnGameOver);
        }
    }

    private void FindPlayer()
    {
        if (GameManager.Instance != null)
        {
            _player = GameManager.Instance.GetPlayer()?.transform;
        }

        if (_player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                _player = playerObj.transform;
        }

        if (_player == null)
        {
            Debug.LogError("[DistanceTracker] ❌ Player not found!");
        }
        else
        {
            Debug.Log("[DistanceTracker] ✓ Player found");
        }
    }

    #endregion

    #region Core Logic

    private void UpdateDistance()
    {
        if (_player == null)
        {
            FindPlayer();
            return;
        }

        // Calculate distance traveled
        float newDistance = _player.position.z - _startZ;
        newDistance = Mathf.Max(0f, newDistance);

        // Only trigger event if changed significantly (optimization)
        if (Mathf.Abs(newDistance - _currentDistance) > 0.1f)
        {
            _currentDistance = newDistance;

            // Trigger event for UI update
            OnDistanceChanged?.Invoke(_currentDistance, targetDistance, Progress);
        }

        // ═══ NEW: Trigger safe zone clear ═══
        if (!_hasTriggeredSafeZone && _currentDistance >= (targetDistance - safeZoneTriggerDistance))
        {
            _hasTriggeredSafeZone = true;
            TriggerToiletSafeZone();
        }
    }
    
    /// <summary>
    /// Trigger toilet safe zone - Clear obstacles
    /// </summary>
    private void TriggerToiletSafeZone()
    {
        Debug.Log($"[DistanceTracker] 🚽 Entering toilet safe zone! Distance: {_currentDistance:F0}m");

        // Get spawner and clear obstacles
        JSONSectionSpawner spawner = FindObjectOfType<JSONSectionSpawner>();
        if (spawner != null)
        {
            spawner.ClearObstaclesNearToilet();
        }

        // Optional: Trigger event
        EventManager.Instance?.TriggerEvent("OnEnterToiletSafeZone");
    }

    public void StartTracking()
    {
        _isTracking = true;
        _hasReachedGoal = false;
        _hasTriggeredSafeZone = false; // ← Reset flag
        
        if (_player != null)
        {
            _startZ = _player.position.z;
        }
        
        _currentDistance = 0f;
        
        // Trigger initial update
        OnDistanceChanged?.Invoke(_currentDistance, targetDistance, Progress);
        
        Debug.Log($"[DistanceTracker] Started tracking. Target: {targetDistance}m");
    }

    /// <summary>
    /// Check victory condition - UPDATED: Don't auto trigger, let toilet trigger handle it
    /// </summary>
    private void CheckVictoryCondition()
    {
        if (_hasReachedGoal) return;
        
        if (_currentDistance >= targetDistance)
        {
            // Mark as reached but DON'T trigger victory yet
            // Let ToiletTriggerZone handle it
            _hasReachedGoal = true;
            _isTracking = false;
            
            Debug.Log($"[DistanceTracker] ✓ Reached target distance: {_currentDistance:F0}m (waiting for toilet trigger)");
            
            // DON'T call GameManager.Victory() here anymore
            // Just trigger event for other systems
            EventManager.Instance?.TriggerEvent("OnDistanceComplete");
        }
    }
    
    #endregion

    #region Public API

    public void StopTracking()
    {
        _isTracking = false;
    }

    public void SetTargetDistance(float distance)
    {
        targetDistance = Mathf.Max(100f, distance);
        Debug.Log($"[DistanceTracker] Target distance set to: {targetDistance}m");
    }
    
    #endregion

    #region Event Handlers
    
    private void OnGameStarted()
    {
        FindPlayer();
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