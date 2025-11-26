using System;
using UnityEngine;

/// <summary>
/// Door Controller - Manages door animation and states
/// Pattern: State Pattern + Observer Pattern
/// SOLID: Single Responsibility (chỉ quản lý door)
/// </summary>
public class DoorController : MonoBehaviour
{
    #region Events
    
    /// <summary>
    /// Fired when door open animation completes
    /// </summary>
    public event Action OnDoorOpenComplete;
    
    /// <summary>
    /// Fired when door close animation completes
    /// </summary>
    public event Action OnDoorCloseComplete;
    
    #endregion

    #region Serialized Fields
    
    [Header("References")]
    [SerializeField] private Animator doorAnimator;
    
    [Header("Animation Parameters")]
    [Tooltip("Trigger name to open door")]
    [SerializeField] private string openTrigger = "Open";
    
    [Tooltip("Trigger name to close door")]
    [SerializeField] private string closeTrigger = "Close";
    
    [Tooltip("Bool parameter to track open state")]
    [SerializeField] private string isOpenParameter = "IsOpen";
    
    [Header("Animation Timing")]
    [Tooltip("Duration of opening animation (seconds)")]
    [SerializeField] private float openDuration = 1.0f;
    
    [Tooltip("Duration of closing animation (seconds)")]
    [SerializeField] private float closeDuration = 1.0f;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    
    #endregion

    #region State
    
    private IDoorState _currentState;
    private DoorStateContext _context;
    
    #endregion

    #region Properties
    
    public bool IsOpen => _currentState is DoorOpenState;
    public bool IsClosed => _currentState is DoorClosedState;
    public bool IsAnimating => _currentState is DoorOpeningState || _currentState is DoorClosingState;
    
    #endregion

    #region Unity Lifecycle
    
    void Awake()
    {
        Initialize();
    }
    
    #endregion

    #region Initialization
    
    private void Initialize()
    {
        // Auto-find animator if not assigned
        if (doorAnimator == null)
        {
            doorAnimator = GetComponent<Animator>();
        }
        
        if (doorAnimator == null)
        {
            Debug.LogError("[DoorController] ❌ No Animator found!");
            return;
        }
        
        // Initialize state context
        _context = new DoorStateContext(this);
        
        // Set initial state to Closed
        SetState(new DoorClosedState());
        
        if (debugMode)
        {
            Debug.Log("[DoorController] ✓ Initialized - State: Closed");
        }
    }
    
    #endregion

    #region Public Methods
    
    /// <summary>
    /// Open the door (plays opening animation)
    /// </summary>
    public void Open()
    {
        if (debugMode)
        {
            Debug.Log($"[DoorController] 🚪 Open() called - Current state: {_currentState?.GetType().Name}");
        }
        
        _currentState?.Open(_context);
    }
    
    /// <summary>
    /// Close the door (plays closing animation)
    /// </summary>
    public void Close()
    {
        if (debugMode)
        {
            Debug.Log($"[DoorController] 🚪 Close() called - Current state: {_currentState?.GetType().Name}");
        }
        
        _currentState?.Close(_context);
    }
    
    #endregion

    #region State Management
    
    public void SetState(IDoorState newState)
    {
        _currentState?.OnExit(_context);
        _currentState = newState;
        _currentState?.OnEnter(_context);
        
        if (debugMode)
        {
            Debug.Log($"[DoorController] State changed to: {newState?.GetType().Name}");
        }
    }
    
    #endregion

    #region Animation Control
    
    public void PlayOpenAnimation()
    {
        if (doorAnimator == null) return;
        
        doorAnimator.SetTrigger(openTrigger);
        doorAnimator.SetBool(isOpenParameter, true);
        
        if (debugMode)
        {
            Debug.Log("[DoorController] ▶️ Playing OPEN animation");
        }
        
        // Start coroutine to detect animation completion
        StartCoroutine(WaitForAnimationComplete(openDuration, OnOpenAnimationComplete));
    }
    
    public void PlayCloseAnimation()
    {
        if (doorAnimator == null) return;
        
        doorAnimator.SetTrigger(closeTrigger);
        doorAnimator.SetBool(isOpenParameter, false);
        
        if (debugMode)
        {
            Debug.Log("[DoorController] ▶️ Playing CLOSE animation");
        }
        
        // Start coroutine to detect animation completion
        StartCoroutine(WaitForAnimationComplete(closeDuration, OnCloseAnimationComplete));
    }
    
    #endregion

    #region Animation Completion Callbacks
    
    private System.Collections.IEnumerator WaitForAnimationComplete(float duration, Action onComplete)
    {
        yield return new WaitForSeconds(duration);
        onComplete?.Invoke();
    }
    
    private void OnOpenAnimationComplete()
    {
        if (debugMode)
        {
            Debug.Log("[DoorController] ✅ Open animation COMPLETE");
        }
        
        // Transition to Open state
        SetState(new DoorOpenState());
        
        // Fire event
        OnDoorOpenComplete?.Invoke();
    }
    
    private void OnCloseAnimationComplete()
    {
        if (debugMode)
        {
            Debug.Log("[DoorController] ✅ Close animation COMPLETE");
        }
        
        // Transition to Closed state
        SetState(new DoorClosedState());
        
        // Fire event
        OnDoorCloseComplete?.Invoke();
    }
    
    #endregion

    #region Debug
    
    // void OnGUI()
    // {
    //     if (!debugMode) return;
        
    //     GUILayout.BeginArea(new Rect(10, 200, 300, 150));
    //     GUILayout.Box("🚪 DOOR DEBUG");
    //     GUILayout.Label($"State: {_currentState?.GetType().Name}");
    //     GUILayout.Label($"IsOpen: {IsOpen}");
    //     GUILayout.Label($"IsAnimating: {IsAnimating}");
        
    //     if (GUILayout.Button("Test Open"))
    //     {
    //         Open();
    //     }
        
    //     if (GUILayout.Button("Test Close"))
    //     {
    //         Close();
    //     }
        
    //     GUILayout.EndArea();
    // }
    
    #endregion
}