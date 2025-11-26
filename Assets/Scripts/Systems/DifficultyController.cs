using UnityEngine;

/// <summary>
/// Difficulty Controller - Manages difficulty progression
/// SOLID: Single Responsibility - Chỉ quản lý độ khó
/// Design Pattern: Strategy Pattern (difficulty curves)
/// </summary>
public class DifficultyController : MonoBehaviour
{
    #region Difficulty Curve Types
    
    /// <summary>
    /// Types of difficulty progression
    /// </summary>
    public enum DifficultyMode
    {
        Linear,      // Tăng đều đặn
        Exponential, // Tăng nhanh dần
        Stepped,     // Tăng từng bước
        Custom       // Curve tùy chỉnh
    }
    
    #endregion

    #region Serialized Fields
    
    [Header("Difficulty Settings")]
    [SerializeField] private DifficultyMode difficultyMode = DifficultyMode.Linear;
    
    [SerializeField] private float startDifficulty = 0f;
    [SerializeField] private float maxDifficulty = 30f;
    
    [Header("Progression Rates")]
    [SerializeField] private float linearIncreaseRate = 0.05f;
    [SerializeField] private float exponentialBase = 1.02f;
    
    [Header("Stepped Difficulty")]
    [SerializeField] private float stepInterval = 10f; // Seconds
    [SerializeField] private float stepAmount = 2f;
    
    [Header("Custom Curve")]
    [SerializeField] private AnimationCurve customCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField] private float curveDuration = 60f; // Duration to complete curve
    
    #endregion

    #region State
    
    private float _currentDifficulty;
    private float _sessionTime;
    private float _stepTimer;
    
    #endregion

    #region Properties
    
    public float CurrentDifficulty => _currentDifficulty;
    public float DifficultyPercent => Mathf.Clamp01(_currentDifficulty / maxDifficulty);
    public float SessionTime => _sessionTime;
    
    #endregion

    #region Unity Lifecycle
    
    void Start()
    {
        Initialize();
    }

    void Update()
    {
        if (GameManager.Instance.CurrentState == GameState.Playing)
        {
            UpdateDifficulty();
        }
    }
    
    #endregion

    #region Initialization
    
    /// <summary>
    /// Initialize difficulty system
    /// </summary>
    private void Initialize()
    {
        _currentDifficulty = startDifficulty;
        _sessionTime = 0f;
        _stepTimer = 0f;
    }
    
    #endregion

    #region Difficulty Update
    
    /// <summary>
    /// Update difficulty based on selected mode
    /// SOLID: Open/Closed - Easy to add new modes
    /// </summary>
    private void UpdateDifficulty()
    {
        _sessionTime += Time.deltaTime;

        // Don't exceed max
        if (_currentDifficulty >= maxDifficulty)
        {
            _currentDifficulty = maxDifficulty;
            return;
        }

        // Update based on mode
        switch (difficultyMode)
        {
            case DifficultyMode.Linear:
                UpdateLinearDifficulty();
                break;

            case DifficultyMode.Exponential:
                UpdateExponentialDifficulty();
                break;

            case DifficultyMode.Stepped:
                UpdateSteppedDifficulty();
                break;

            case DifficultyMode.Custom:
                UpdateCustomDifficulty();
                break;
        }

        // Clamp to max
        _currentDifficulty = Mathf.Min(_currentDifficulty, maxDifficulty);
    }

    /// <summary>
    /// Linear difficulty increase - Simple and predictable
    /// KISS: Đơn giản nhất
    /// </summary>
    private void UpdateLinearDifficulty()
    {
        _currentDifficulty += linearIncreaseRate * Time.deltaTime;
    }

    /// <summary>
    /// Exponential difficulty increase - Starts slow, gets harder fast
    /// </summary>
    private void UpdateExponentialDifficulty()
    {
        _currentDifficulty = startDifficulty * Mathf.Pow(exponentialBase, _sessionTime);
    }

    /// <summary>
    /// Stepped difficulty increase - Plateaus with sudden jumps
    /// </summary>
    private void UpdateSteppedDifficulty()
    {
        _stepTimer += Time.deltaTime;

        if (_stepTimer >= stepInterval)
        {
            _stepTimer = 0f;
            _currentDifficulty += stepAmount;

            // Trigger event
            EventManager.Instance.TriggerEvent(GameEvents.DIFFICULTY_INCREASED);
            
            Debug.Log($"[Difficulty] Stepped up to {_currentDifficulty:F1}");
        }
    }

    /// <summary>
    /// Custom curve difficulty - Designer-controlled
    /// </summary>
    private void UpdateCustomDifficulty()
    {
        float curveTime = Mathf.Clamp01(_sessionTime / curveDuration);
        float curveValue = customCurve.Evaluate(curveTime);
        _currentDifficulty = curveValue * maxDifficulty;
    }
    
    #endregion

    #region Difficulty Modifiers
    
    /// <summary>
    /// Add instant difficulty spike
    /// </summary>
    public void AddDifficultySpike(float amount)
    {
        _currentDifficulty = Mathf.Min(_currentDifficulty + amount, maxDifficulty);
        Debug.Log($"[Difficulty] Spike! Now at {_currentDifficulty:F1}");
    }

    /// <summary>
    /// Reduce difficulty (e.g., on player respawn)
    /// </summary>
    public void ReduceDifficulty(float amount)
    {
        _currentDifficulty = Mathf.Max(_currentDifficulty - amount, startDifficulty);
    }

    /// <summary>
    /// Reset difficulty to start
    /// </summary>
    public void ResetDifficulty()
    {
        _currentDifficulty = startDifficulty;
        _sessionTime = 0f;
        _stepTimer = 0f;
    }

    /// <summary>
    /// Set difficulty directly (for testing)
    /// </summary>
    public void SetDifficulty(float difficulty)
    {
        _currentDifficulty = Mathf.Clamp(difficulty, startDifficulty, maxDifficulty);
    }
    
    #endregion

    #region Difficulty Queries
    
    /// <summary>
    /// Get difficulty tier (Easy/Medium/Hard)
    /// </summary>
    public string GetDifficultyTier()
    {
        if (_currentDifficulty < maxDifficulty * 0.33f)
            return "Easy";
        else if (_currentDifficulty < maxDifficulty * 0.66f)
            return "Medium";
        else
            return "Hard";
    }

    /// <summary>
    /// Check if difficulty is in range
    /// </summary>
    public bool IsInDifficultyRange(float min, float max)
    {
        return _currentDifficulty >= min && _currentDifficulty <= max;
    }
    
    #endregion

    #region Debug
    
    #if UNITY_EDITOR
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGUI = true;

    // void OnGUI()
    // {
    //     if (!showDebugGUI || !Application.isPlaying) return;

    //     GUIStyle style = new GUIStyle();
    //     style.fontSize = 14;
    //     style.normal.textColor = Color.yellow;

    //     GUI.Label(new Rect(10, 170, 300, 25), 
    //         $"Difficulty: {_currentDifficulty:F1} / {maxDifficulty:F1}", style);
    //     GUI.Label(new Rect(10, 190, 300, 25), 
    //         $"Tier: {GetDifficultyTier()}", style);
    //     GUI.Label(new Rect(10, 210, 300, 25), 
    //         $"Session Time: {_sessionTime:F1}s", style);
    // }

    /// <summary>
    /// Test difficulty spike
    /// </summary>
    [ContextMenu("Test Difficulty Spike")]
    void TestDifficultySpike()
    {
        AddDifficultySpike(5f);
    }
    
    #endif
    
    #endregion
}