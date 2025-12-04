using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// Event Manager - Observer Pattern
/// SOLID: Single Responsibility - Chỉ quản lý events
/// Design Pattern: Observer - Decoupling giữa các components
/// </summary>
public class EventManager : MonoBehaviour
{
    // Singleton instance
    private static EventManager _instance;
    private static bool _isQuitting = false;
    public static EventManager Instance
    {
        get
        {
            if (_isQuitting)
            {
                return null;
            }
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<EventManager>();
                if (_instance == null && !_isQuitting)
                {
                    GameObject go = new GameObject("EventManager");
                    _instance = go.AddComponent<EventManager>();
                }
            }
            return _instance;
        }
    }

    void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _isQuitting = true;
        }
    }

    // Dictionary lưu các events theo tên
    // KISS: Đơn giản, dễ hiểu
    private Dictionary<string, UnityEvent> eventDictionary = new Dictionary<string, UnityEvent>();
    
    // Dictionary cho events có parameter
    private Dictionary<string, UnityEvent<int>> intEventDictionary = new Dictionary<string, UnityEvent<int>>();
    private Dictionary<string, UnityEvent<float>> floatEventDictionary = new Dictionary<string, UnityEvent<float>>();

    void Awake()
    {
        // Singleton pattern - Đảm bảo chỉ có 1 instance
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Đăng ký listener cho event
    /// </summary>
    public void StartListening(string eventName, UnityAction listener)
    {
        UnityEvent thisEvent = null;
        
        // Nếu event đã tồn tại, thêm listener
        if (eventDictionary.TryGetValue(eventName, out thisEvent))
        {
            thisEvent.AddListener(listener);
        }
        else
        {
            // Tạo event mới
            thisEvent = new UnityEvent();
            thisEvent.AddListener(listener);
            eventDictionary.Add(eventName, thisEvent);
        }
    }

    /// <summary>
    /// Đăng ký listener cho event có int parameter
    /// </summary>
    public void StartListening(string eventName, UnityAction<int> listener)
    {
        UnityEvent<int> thisEvent = null;
        
        if (intEventDictionary.TryGetValue(eventName, out thisEvent))
        {
            thisEvent.AddListener(listener);
        }
        else
        {
            thisEvent = new UnityEvent<int>();
            thisEvent.AddListener(listener);
            intEventDictionary.Add(eventName, thisEvent);
        }
    }

    /// <summary>
    /// Hủy đăng ký listener
    /// </summary>
    public void StopListening(string eventName, UnityAction listener)
    {
        if (_instance == null) return;
        
        UnityEvent thisEvent = null;
        if (eventDictionary.TryGetValue(eventName, out thisEvent))
        {
            thisEvent.RemoveListener(listener);
        }
    }

    public void StopListening(string eventName, UnityAction<int> listener)
    {
        if (_instance == null) return;
        
        UnityEvent<int> thisEvent = null;
        if (intEventDictionary.TryGetValue(eventName, out thisEvent))
        {
            thisEvent.RemoveListener(listener);
        }
    }

    /// <summary>
    /// Trigger event không parameter
    /// </summary>
    public void TriggerEvent(string eventName)
    {
        UnityEvent thisEvent = null;
        if (eventDictionary.TryGetValue(eventName, out thisEvent))
        {
            thisEvent.Invoke();
        }
    }

    /// <summary>
    /// Trigger event có int parameter
    /// </summary>
    public void TriggerEvent(string eventName, int value)
    {
        UnityEvent<int> thisEvent = null;
        if (intEventDictionary.TryGetValue(eventName, out thisEvent))
        {
            thisEvent.Invoke(value);
        }
    }

    /// <summary>
    /// Clear tất cả events - Dọn dẹp memory
    /// </summary>
    public void ClearAllEvents()
    {
        eventDictionary.Clear();
        intEventDictionary.Clear();
        floatEventDictionary.Clear();
    }
}

/// <summary>
/// Game Events - UPDATED for Dog Chase theme
/// </summary>
public static class GameEvents
{
    // Core game events
    public const string GAME_STARTED = "OnGameStarted";
    public const string GAME_PAUSED = "OnGamePaused";
    public const string GAME_RESUMED = "OnGameResumed";
    public const string GAME_OVER = "OnGameOver";
    public const string LEVEL_COMPLETE = "OnLevelComplete";
    
    // Score events
    public const string SCORE_CHANGED = "OnScoreChanged";
    public const string COIN_COLLECTED = "OnCoinCollected";
    
    // Player events
    public const string PLAYER_DIED = "OnPlayerDied";
    
    // Difficulty
    public const string DIFFICULTY_INCREASED = "OnDifficultyIncreased";

    // PowerUp events (keep if using collectibles)
    public const string POWERUP_COLLECTED = "OnPowerUpCollected";
    public const string SHIELD_ACTIVATED = "OnShieldActivated";
    public const string SHIELD_BROKEN = "OnShieldBroken";

    // ═══ ADDED: Dog chase events ═══
    public const string DOG_CHASE_STARTED = "OnDogChaseStarted";
    public const string DOG_ACCELERATED = "OnDogAccelerated";
    public const string DOG_CAUGHT_PLAYER = "OnDogCaughtPlayer";
    public const string DOG_DISAPPEARED = "OnDogDisappeared";
    public const string HOME_REACHED = "OnHomeReached";
    public const string ENTERING_HOME_SAFE_ZONE = "OnEnterHomeSafeZone";
}