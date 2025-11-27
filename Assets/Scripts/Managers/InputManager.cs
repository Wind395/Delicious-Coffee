using UnityEngine;

/// <summary>
/// Input Manager - Mobile Optimized
/// SOLID: Single Responsibility - Input handling only
/// Design Pattern: Facade - Simplifies input from multiple sources
/// </summary>
public class InputManager : MonoBehaviour
{
    #region Singleton
    
    private static InputManager _instance;
    public static InputManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<InputManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("InputManager");
                    _instance = go.AddComponent<InputManager>();
                }
            }
            return _instance;
        }
    }
    
    #endregion

    #region Input Settings
    
    [Header("=== INPUT TYPE ===")]
    [SerializeField] private InputType inputType = InputType.Touch;
    
    public enum InputType
    {
        KeyboardAndMouse,
        Touch,
        Both
    }
    
    [Header("=== SWIPE SETTINGS ===")]
    [Tooltip("Minimum pixel distance for swipe")]
    [SerializeField] private float swipeThreshold = 50f;
    
    [Tooltip("Maximum time for swipe (seconds)")]
    [SerializeField] private float swipeTimeThreshold = 0.3f;
    
    [Tooltip("Minimum pixels for valid swipe")]
    [SerializeField] private float minSwipeDistance = 30f;

    [Header("=== TAP SETTINGS ===")]
    [Tooltip("Maximum time for tap (seconds)")]
    [SerializeField] private float tapTimeThreshold = 0.3f;
    
    [Tooltip("Maximum movement for tap")]
    [SerializeField] private float tapPositionThreshold = 10f;

    [Header("=== SENSITIVITY ===")]
    [Tooltip("Swipe angle tolerance (degrees)")]
    [Range(0f, 45f)]
    [SerializeField] private float angleTolerance = 30f;

    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool showSwipeLines = true;
    
    #endregion

    #region Input State
    
    // Touch/Swipe detection
    private Vector2 _touchStartPos;
    private Vector2 _touchCurrentPos;
    private float _touchStartTime;
    private bool _isSwiping = false;
    private bool _hasSwiped = false; // Prevent multiple swipes in one gesture
    
    // Visual debug
    private Vector2 _lastSwipeStart;
    private Vector2 _lastSwipeEnd;
    private float _lastSwipeTime;
    
    #endregion

    #region Events - C# Events cho type safety
    
    public event System.Action OnSwipeLeft;
    public event System.Action OnSwipeRight;
    public event System.Action OnSwipeUp;
    public event System.Action OnSwipeDown;
    public event System.Action OnTap;
    
    #endregion

    #region Unity Lifecycle
    
    void Awake()
    {
        // Singleton
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Mobile optimization
        Application.targetFrameRate = 60;
    }

    void Start()
    {
        // ← FIX: Auto-enable input on Start
        _isEnabled = true;
        ClearInputState();
        
        Debug.Log("[InputManager] Started - Input enabled by default");
    }

    void Update()
    {
        // Check if enabled first
        if (!_isEnabled)
        {
            return;
        }
        
        // Chỉ process input khi đang chơi
        if (GameManager.Instance == null || 
            GameManager.Instance.CurrentState != GameState.Playing)
            return;

        switch (inputType)
        {
            case InputType.KeyboardAndMouse:
                HandleKeyboardInput();
                break;
                
            case InputType.Touch:
                HandleTouchInput();
                break;
                
            case InputType.Both:
                HandleKeyboardInput();
                HandleTouchInput();
                break;
        }
    }
    
    #endregion

    #region Keyboard Input (Testing)
    
    /// <summary>
    /// Handle keyboard - Chỉ cho testing
    /// </summary>
    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            OnSwipeLeft?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            OnSwipeRight?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || 
            Input.GetKeyDown(KeyCode.Space))
        {
            OnSwipeUp?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            OnSwipeDown?.Invoke();
        }
    }
    
    #endregion

    #region Touch Input - MOBILE OPTIMIZED
    
    /// <summary>
    /// Handle touch/swipe input - Main mobile input
    /// </summary>
    private void HandleTouchInput()
    {
        // Touch input (actual mobile)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    OnTouchStart(touch.position);
                    break;

                case TouchPhase.Moved:
                    OnTouchMove(touch.position);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    OnTouchEnd(touch.position);
                    break;
            }
        }
        // Mouse input (for testing on PC)
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                OnTouchStart(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0) && _isSwiping)
            {
                OnTouchMove(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                OnTouchEnd(Input.mousePosition);
            }
        }
    }

    /// <summary>
    /// Touch/swipe started
    /// </summary>
    private void OnTouchStart(Vector2 position)
    {
        _touchStartPos = position;
        _touchCurrentPos = position;
        _touchStartTime = Time.time;
        _isSwiping = true;
        _hasSwiped = false;

        if (showDebugGizmos)
        {
            _lastSwipeStart = position;
        }
    }

    /// <summary>
    /// Touch moved - Detect swipe during movement
    /// Mobile optimization: Detect swipe early
    /// </summary>
    private void OnTouchMove(Vector2 position)
    {
        if (!_isSwiping || _hasSwiped) return;

        _touchCurrentPos = position;

        // Calculate swipe delta
        Vector2 swipeDelta = _touchCurrentPos - _touchStartPos;

        // Early swipe detection (responsive feel)
        if (swipeDelta.magnitude >= swipeThreshold)
        {
            DetectSwipeDirection(swipeDelta);
            _hasSwiped = true; // Prevent multiple swipes
        }
    }

    /// <summary>
    /// Touch/swipe ended
    /// </summary>
    private void OnTouchEnd(Vector2 position)
    {
        if (!_isSwiping) return;

        _isSwiping = false;
        _touchCurrentPos = position;

        // If already swiped during movement, don't process again
        if (_hasSwiped) return;

        // Calculate final swipe
        Vector2 swipeDelta = _touchCurrentPos - _touchStartPos;
        float swipeTime = Time.time - _touchStartTime;

        // Check if it's a tap
        if (swipeDelta.magnitude < tapPositionThreshold && swipeTime < tapTimeThreshold)
        {
            OnTap?.Invoke();
            
            if (showDebugGizmos)
            {
                Debug.Log("[Input] Tap detected");
            }
            return;
        }

        // Check if valid swipe
        if (swipeDelta.magnitude < minSwipeDistance)
        {
            // Too short
            return;
        }

        if (swipeTime > swipeTimeThreshold)
        {
            // Too slow
            return;
        }

        // Detect swipe direction
        DetectSwipeDirection(swipeDelta);

        // Debug visual
        if (showDebugGizmos)
        {
            _lastSwipeEnd = position;
            _lastSwipeTime = Time.time;
        }
    }

    /// <summary>
    /// Detect swipe direction from delta
    /// Improved algorithm with angle tolerance
    /// </summary>
    private void DetectSwipeDirection(Vector2 swipeDelta)
    {
        // Calculate angle
        float angle = Mathf.Atan2(swipeDelta.y, swipeDelta.x) * Mathf.Rad2Deg;

        // Normalize angle to 0-360
        if (angle < 0)
            angle += 360f;

        // Determine swipe direction with tolerance
        // Right: 0° ± tolerance (315-45)
        // Up: 90° ± tolerance (45-135)
        // Left: 180° ± tolerance (135-225)
        // Down: 270° ± tolerance (225-315)

        bool isHorizontal = Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y);

        if (isHorizontal)
        {
            // Horizontal swipe
            if (swipeDelta.x > 0)
            {
                OnSwipeRight?.Invoke();
                LogSwipe("Right", angle, swipeDelta.magnitude);
            }
            else
            {
                OnSwipeLeft?.Invoke();
                LogSwipe("Left", angle, swipeDelta.magnitude);
            }
        }
        else
        {
            // Vertical swipe
            if (swipeDelta.y > 0)
            {
                OnSwipeUp?.Invoke();
                LogSwipe("Up", angle, swipeDelta.magnitude);
            }
            else
            {
                OnSwipeDown?.Invoke();
                LogSwipe("Down", angle, swipeDelta.magnitude);
            }
        }
    }

    /// <summary>
    /// Log swipe for debugging
    /// </summary>
    private void LogSwipe(string direction, float angle, float magnitude)
    {
        if (showDebugGizmos)
        {
            Debug.Log($"[Input] Swipe {direction} | Angle: {angle:F1}° | Distance: {magnitude:F1}px");
        }
    }

    #endregion


    #region Enable/Disable - NEW

    private bool _isEnabled = true; // ← NEW: Control input processing

    /// <summary>
    /// Enable/disable input processing
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
        
        if (!enabled)
        {
            ClearInputState();
        }
        
        if (showDebugGizmos)
        {
            Debug.Log($"[InputManager] Input {(enabled ? "ENABLED" : "DISABLED")}");
        }
    }

    /// <summary>
    /// Clear all input state - Prevent lingering touches/swipes
    /// </summary>
    public void ClearInputState()
    {
        _isSwiping = false;
        _hasSwiped = false;
        _touchStartPos = Vector2.zero;
        _touchCurrentPos = Vector2.zero;
        _touchStartTime = 0f;
        
        if (showDebugGizmos)
        {
            Debug.Log("[InputManager] Input state cleared");
        }
    }

    /// <summary>
    /// Get input enabled state - NEW
    /// </summary>
    public bool IsEnabled()
    {
        return _isEnabled;
    }

    #endregion



    #region Public API

    /// <summary>
    /// Change input type at runtime
    /// </summary>
    public void SetInputType(InputType type)
    {
        inputType = type;
        Debug.Log($"[InputManager] Input type: {type}");
    }

    /// <summary>
    /// Set swipe sensitivity
    /// </summary>
    public void SetSwipeSensitivity(float threshold)
    {
        swipeThreshold = Mathf.Max(10f, threshold);
    }

    /// <summary>
    /// Get current input type
    /// </summary>
    public InputType GetInputType()
    {
        return inputType;
    }

    /// <summary>
    /// Check if currently swiping
    /// </summary>
    public bool IsSwiping()
    {
        return _isSwiping;
    }
    
    #endregion

    #region Debug Visualization
    
    #if UNITY_EDITOR
    
    // void OnGUI()
    // {
    //     if (!showDebugGizmos || !Application.isPlaying) return;

    //     int w = Screen.width, h = Screen.height;
    //     GUIStyle style = new GUIStyle();
    //     style.fontSize = h / 50;
    //     style.normal.textColor = Color.cyan;

    //     // Input type
    //     GUI.Label(new Rect(10, h - 140, 300, 30), $"Input: {inputType}", style);
        
    //     // Swipe status
    //     if (_isSwiping)
    //     {
    //         style.normal.textColor = Color.yellow;
    //         Vector2 delta = _touchCurrentPos - _touchStartPos;
    //         GUI.Label(new Rect(10, h - 110, 300, 30), 
    //             $"Swiping... Distance: {delta.magnitude:F0}px", style);
    //     }

    //     // Last swipe info
    //     if (Time.time - _lastSwipeTime < 1f && showSwipeLines)
    //     {
    //         style.normal.textColor = Color.green;
    //         GUI.Label(new Rect(10, h - 80, 300, 30), "Swipe detected!", style);
    //     }

    //     // Draw swipe line
    //     if (showSwipeLines && _isSwiping)
    //     {
    //         DrawSwipeLine(_touchStartPos, _touchCurrentPos, Color.yellow);
    //     }
    //     else if (showSwipeLines && Time.time - _lastSwipeTime < 0.5f)
    //     {
    //         DrawSwipeLine(_lastSwipeStart, _lastSwipeEnd, Color.green);
    //     }
    // }

    /// <summary>
    /// Draw swipe line for visualization
    /// </summary>
    private void DrawSwipeLine(Vector2 start, Vector2 end, Color color)
    {
        // Draw line on screen
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();

        GUI.color = color;

        // Calculate line rect
        Vector2 delta = end - start;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        
        // Draw thick line
        Matrix4x4 matrixBackup = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, start);
        GUI.DrawTexture(new Rect(start.x, start.y - 2, delta.magnitude, 4), texture);
        GUI.matrix = matrixBackup;

        // Draw start circle
        GUI.DrawTexture(new Rect(start.x - 10, start.y - 10, 20, 20), texture);
        
        // Draw end arrow
        GUI.DrawTexture(new Rect(end.x - 15, end.y - 15, 30, 30), texture);

        GUI.color = Color.white;
    }
    
    #endif
    
    #endregion
}