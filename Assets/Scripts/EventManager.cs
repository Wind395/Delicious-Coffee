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
    public static EventManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<EventManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("EventManager");
                    _instance = go.AddComponent<EventManager>();
                }
            }
            return _instance;
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
/// Class chứa tên các events - Tránh typo
/// SOLID: Open/Closed - Dễ thêm event mới
/// </summary>
public static class GameEvents
{
    public const string GAME_STARTED = "OnGameStarted";
    public const string GAME_PAUSED = "OnGamePaused";
    public const string GAME_RESUMED = "OnGameResumed";
    public const string GAME_OVER = "OnGameOver";
    public const string LEVEL_COMPLETE = "OnLevelComplete";
    public const string SCORE_CHANGED = "OnScoreChanged";
    public const string COIN_COLLECTED = "OnCoinCollected";
    public const string PLAYER_DIED = "OnPlayerDied";
    public const string DIFFICULTY_INCREASED = "OnDifficultyIncreased";

    public const string POWERUP_COLLECTED = "OnPowerUpCollected";
    public const string SHIELD_ACTIVATED = "OnShieldActivated";
    public const string SHIELD_BROKEN = "OnShieldBroken";
    public const string METER_WARNING = "OnMeterWarning";
    public const string METER_CRITICAL = "OnMeterCritical";
}