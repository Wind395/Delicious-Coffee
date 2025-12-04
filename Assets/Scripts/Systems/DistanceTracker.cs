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

        // if (_player == null)
        // {
        //     Debug.LogError("[DistanceTracker] ❌ Player not found!");
        // }
        // else
        // {
        //     Debug.Log("[DistanceTracker] ✓ Player found");
        // }
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

        float newDistance = _player.position.z - _startZ;
        newDistance = Mathf.Max(0f, newDistance);

        if (Mathf.Abs(newDistance - _currentDistance) > 0.1f)
        {
            _currentDistance = newDistance;
            OnDistanceChanged?.Invoke(_currentDistance, targetDistance, Progress);
        }

        // ═══ CHANGED: Trigger home safe zone clear ═══
        if (!_hasTriggeredSafeZone && _currentDistance >= (targetDistance - safeZoneTriggerDistance))
        {
            _hasTriggeredSafeZone = true;
            TriggerHomeSafeZone(); // CHANGED from TriggerToiletSafeZone
        }
    }

    /// <summary>
    /// Trigger home safe zone - Clear obstacles, coins & powerups near finish
    /// </summary>
    private void TriggerHomeSafeZone()
    {
        //Debug.Log($"[DistanceTracker] 🏠 Entering home safe zone! Distance: {_currentDistance:F0}m");

        JSONSectionSpawner spawner = FindAnyObjectByType<JSONSectionSpawner>();
        if (spawner != null)
        {
            spawner.ClearObstaclesNearHome(); // ← Updated method clears all
        }

        EventManager.Instance?.TriggerEvent("OnEnterHomeSafeZone");
    }

    

    public void StartTracking()
    {
        _isTracking = true;
        _hasReachedGoal = false;
        _hasTriggeredSafeZone = false;
        
        if (_player != null)
        {
            _startZ = _player.position.z;
        }
        
        _currentDistance = 0f;
        OnDistanceChanged?.Invoke(_currentDistance, targetDistance, Progress);
        
        //Debug.Log($"[DistanceTracker] Started tracking. Target: {targetDistance}m to Home"); // CHANGED text
    }

    /// <summary>
    /// Check victory condition - UPDATED: Only for Level mode
    /// </summary>
    private void CheckVictoryCondition()
    {
        if (_hasReachedGoal) return;
        
        // ═══ NEW: Only check victory in Level mode ═══
        if (GameModeManager.Instance != null && 
            GameModeManager.Instance.CurrentMode == GameMode.Endless)
        {
            return; // No victory in Endless mode
        }
        
        // Existing victory check (Level mode only)
        if (_currentDistance >= targetDistance)
        {
            _hasReachedGoal = true;
            _isTracking = false;
            
            //Debug.Log($"[DistanceTracker] ✓ Reached home: {_currentDistance:F0}m (waiting for home trigger)");
            
            EventManager.Instance?.TriggerEvent("OnDistanceComplete");
        }
    }
    
    #endregion

    #region Public API

    public void StopTracking()
    {
        _isTracking = false;
    }

    /// <summary>
    /// Set target distance - NEW
    /// </summary>
    public void SetTargetDistance(float distance)
    {
        targetDistance = Mathf.Max(100f, distance);
        
        //Debug.Log($"[DistanceTracker] 🎯 Target distance set to: {targetDistance}m");
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