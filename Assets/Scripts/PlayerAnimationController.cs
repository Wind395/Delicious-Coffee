using UnityEngine;
using System.Collections;

/// <summary>
/// Player Animation Controller - Complete Animation System
/// SOLID: Single Responsibility - Animation logic only
/// Design Pattern: State Pattern
/// </summary>
//[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    #region Animation Parameters - Constants
    
    private static readonly int PARAM_SPEED = Animator.StringToHash("Speed");
    private static readonly int PARAM_IS_GROUNDED = Animator.StringToHash("IsGrounded");
    
    private static readonly int TRIGGER_DRINKING = Animator.StringToHash("Drinking");
    private static readonly int TRIGGER_RUNNING = Animator.StringToHash("Running");
    private static readonly int TRIGGER_JUMP = Animator.StringToHash("Jump");
    private static readonly int TRIGGER_SLIDE = Animator.StringToHash("Slide");
    private static readonly int TRIGGER_FALL_FLAT = Animator.StringToHash("FallFlat");
    private static readonly int TRIGGER_MUTANT_DYING = Animator.StringToHash("MutantDying");
    private static readonly int TRIGGER_SITTING = Animator.StringToHash("Sitting");

    private static readonly int PARAM_IS_SLIDING = Animator.StringToHash("IsSliding");
    
    // ═══ NEW: Injured Walking ═══
    private static readonly int TRIGGER_INJURED = Animator.StringToHash("Injured");
    private static readonly int PARAM_IS_INJURED = Animator.StringToHash("IsInjured");

    #endregion

    #region Serialized Fields

    [Header("Animation Durations")]
    [SerializeField] private float drinkingDuration = 2f;
    [SerializeField] private float sittingDuration = 2f;

    // ═══ NEW: Death Animation Durations ═══
    [Header("Death Animation Durations")]
    [SerializeField] private float fallFlatDuration = 1.5f;
    [SerializeField] private float mutantDyingDuration = 2f;

    [Header("Turn Animation")]
    [SerializeField] private float turnAnimationDuration = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    [Header("Fart System")]
    [SerializeField] private bool enableFartEffects = true;
    [SerializeField] private FartVFXController fartVFXController;

    [Header("Fart Timing")]
    [Tooltip("Delay after drinking animation before fart")]
    [SerializeField] private float fartDelayAfterDrinking = 0.2f;
    
    [Tooltip("Delay after sitting animation before fart")]
    [SerializeField] private float afterFart = 0.2f;
    
    [Tooltip("Delay after death animation starts before fart")]
    [SerializeField] private float fartDelayOnDeath = 0.5f;

    #endregion

    #region Components
    
    [SerializeField] private Animator _animator;
    [SerializeField] private PlayerController _playerController;
    
    #endregion

    #region State
    
    private PlayerAnimationState _currentState = PlayerAnimationState.Idle;
    private bool _isSliding = false;
    private bool _isInjured = false; // ← NEW
    private float _turnTimer;
    
    #endregion

    #region Events - Observer Pattern

    public event System.Action OnDrinkingComplete;
    public event System.Action OnSittingComplete;
    public event System.Action OnDeathAnimationComplete;

    #endregion

    #region Properties

    public PlayerAnimationState CurrentState => _currentState;
    public bool IsSliding => _isSliding;
    public bool IsInjured => _isInjured;
    public float DrinkingDuration => drinkingDuration;
    public float SittingDuration => sittingDuration;

    // ═══ NEW: Death Animation Durations ═══
    public float FallFlatDuration => fallFlatDuration;
    public float MutantDyingDuration => mutantDyingDuration;

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        // if (_animator == null)
        // {
        //     _animator = GetComponentInChildren<Animator>();

        //     if (_animator != null && showDebugLogs)
        //     {
        //         Debug.Log("[PlayerAnim] ✓ Found animator in children");
        //     }
        // }

        _playerController = GetComponent<PlayerController>();

        if (_animator == null)
        {
            Debug.LogError("[PlayerAnim] Animator component not found!");
        }
        
        if (fartVFXController == null)
        {
            fartVFXController = FindObjectOfType<FartVFXController>();
        }
    }

    void Start()
    {
        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();

            if (_animator != null && showDebugLogs)
            {
                Debug.Log("[PlayerAnim] ✓ Found animator in children");
            }
        }

        _playerController = GetComponent<PlayerController>();

        // if ( _playerController == null)
        // {
        //     _playerController = GetComponentInChildren<PlayerController>();
        //     if (_playerController != null && showDebugLogs)
        //     {
        //         Debug.Log("[PlayerAnim] ✓ Found PlayerController in children");
        //     }
        // }
    }

    void Update()
    {
        UpdateAnimationParameters();
        UpdateTurnAnimation();
    }
    
    #endregion

    #region Animation State Management
    
    /// <summary>
    /// Change animation state - State Pattern
    /// </summary>
    private void ChangeState(PlayerAnimationState newState)
    {
        if (_currentState == newState)
            return;
        
        if (showDebugLogs)
        {
            Debug.Log($"[PlayerAnim] State: {_currentState} → {newState}");
        }
        
        _currentState = newState;
    }
    
    #endregion

    #region Continuous Animation Updates
    
    /// <summary>
    /// Update continuous animation parameters
    /// </summary>
    private void UpdateAnimationParameters()
    {
        if (_animator == null || _playerController == null) 
            return;

        // Update speed parameter (normalized)
        float normalizedSpeed = _playerController.IsAlive ? 1f : 0f;
        _animator.SetFloat(PARAM_SPEED, normalizedSpeed);

        // Update grounded state
        _animator.SetBool(PARAM_IS_GROUNDED, _playerController.IsGrounded);

        // Update sliding state
        _animator.SetBool(PARAM_IS_SLIDING, _isSliding);
        
        // Update injured state
        _animator.SetBool(PARAM_IS_INJURED, _isInjured);
    }

    /// <summary>
    /// Update turn animation timer
    /// </summary>
    private void UpdateTurnAnimation()
    {
        if (_turnTimer > 0)
        {
            _turnTimer -= Time.deltaTime;
        }
    }
    
    #endregion

    #region Game Start - Drinking Animation
    
    /// <summary>
    /// Play drinking animation at game start
    /// </summary>
    public void OnGameStart()
    {
        if (_animator == null)
            return;
        
        ChangeState(PlayerAnimationState.Drinking);
        
        _animator.SetTrigger(TRIGGER_DRINKING);
        
        if (showDebugLogs)
        {
            Debug.Log($"[PlayerAnim] ☕ Drinking animation started ({drinkingDuration}s)");
        }
        
        // Start coroutine
        StartCoroutine(DrinkingSequence());
    }

    private IEnumerator DrinkingSequence()
    {
        // Wait for drinking duration
        yield return new WaitForSeconds(drinkingDuration);
        PlayQuestionVFX();

        yield return new WaitForSeconds(fartDelayAfterDrinking);
        PlayFartEffect(FartType.Drinking);

        yield return new WaitForSeconds(afterFart);
        // Drinking finished
        OnDrinkingFinished();
        
        // if (enableFartEffects)
        // {
        //     yield return new WaitForSeconds(fartDelayAfterDrinking);
        // }
    }

    private void OnDrinkingFinished()
    {
        ChangeState(PlayerAnimationState.Running);
        
        _animator.SetTrigger(TRIGGER_RUNNING);
        
        if (showDebugLogs)
        {
            Debug.Log("[PlayerAnim] ✓ Drinking finished → Running");
        }
        
        // Trigger event
        OnDrinkingComplete?.Invoke();
    }
    
    #endregion

    #region Movement Animations
    
    /// <summary>
    /// Trigger jump animation
    /// </summary>
    public void OnJump()
    {
        if (_animator == null)
            return;
        
        _animator.SetTrigger(TRIGGER_JUMP);
        
        if (showDebugLogs)
        {
            Debug.Log("[PlayerAnim] 🦘 Jump");
        }
    }

    /// <summary>
    /// Trigger slide animation
    /// </summary>
    public void OnSlide()
    {
        if (_animator == null)
            return;
        
        _isSliding = true;
        _animator.SetTrigger(TRIGGER_SLIDE);
        
        ChangeState(PlayerAnimationState.Sliding);
        
        if (showDebugLogs)
        {
            Debug.Log("[PlayerAnim] 🏃 Slide started");
        }
    }

    /// <summary>
    /// End slide animation
    /// </summary>
    public void OnSlideEnd()
    {
        _isSliding = false;
        ChangeState(PlayerAnimationState.Running);
        
        if (showDebugLogs)
        {
            Debug.Log("[PlayerAnim] ✓ Slide ended");
        }
    }

    /// <summary>
    /// Handle lane change animation
    /// </summary>
    public void OnLaneChange(int direction)
    {
        // Optional: Add turn animation if you have it
        _turnTimer = turnAnimationDuration;
    }
    
    #endregion

    #region Injured Walking - NEW

    /// <summary>
    /// Start injured walking animation when hit by slow obstacle
    /// </summary>
    public void OnInjured()
    {
        if (_animator == null)
            return;
        
        _isInjured = true;
        _animator.SetTrigger(TRIGGER_INJURED);
        
        ChangeState(PlayerAnimationState.Injured);
        
        if (showDebugLogs)
        {
            Debug.Log("[PlayerAnim] 🤕 Injured Walking started");
        }
    }

    /// <summary>
    /// End injured walking animation when slow effect ends
    /// </summary>
    public void OnRecovered()
    {
        if (_animator == null)
            return;
        
        _isInjured = false;
        
        ChangeState(PlayerAnimationState.Running);
        
        if (showDebugLogs)
        {
            Debug.Log("[PlayerAnim] ✓ Recovered → Running");
        }
    }

    #endregion

    #region Death Animations

    /// <summary>
    /// Trigger death animation - UPDATED: New logic
    /// 
    /// RULES:
    /// - MeterFull → Fall Flat
    /// - ObstacleRunning → Mutant Dying
    /// - ObstacleJumping → Fall Flat
    /// - ObstacleSliding → Fall Flat
    /// </summary>
    public void OnDie(DeathReason reason)
    {
        if (_animator == null)
        {
            Debug.LogError("[PlayerAnim] Animator is null!");
            return;
        }
        
        Debug.Log($"[PlayerAnim] ═══════════════════════════════════");
        Debug.Log($"[PlayerAnim] OnDie() called");
        Debug.Log($"[PlayerAnim] Death Reason: {reason}");
        Debug.Log($"[PlayerAnim] Current State: {_currentState}");
        Debug.Log($"[PlayerAnim] Is Sliding: {_isSliding}");
        Debug.Log($"[PlayerAnim] ═══════════════════════════════════");
        
        // ═══ SELECT ANIMATION BASED ON DEATH REASON ═══
        switch (reason)
        {
            case DeathReason.MeterFull:
                // Meter full → Always Fall Flat
                Debug.Log("[PlayerAnim] 💩 Meter Full → FALL FLAT");
                StartCoroutine(PlayFallFlatSequence());
                break;
                
            case DeathReason.ObstacleRunning:
                // Running + Obstacle → Mutant Dying ✨ NEW!
                Debug.Log("[PlayerAnim] 🏃 Running + Obstacle → MUTANT DYING");
                StartCoroutine(PlayMutantDyingSequence());
                break;
                
            case DeathReason.ObstacleJumping:
                // Jumping + Obstacle → Fall Flat
                Debug.Log("[PlayerAnim] 🦘 Jumping + Obstacle → FALL FLAT");
                StartCoroutine(PlayFallFlatSequence());
                break;
                
            case DeathReason.ObstacleSliding:
                // Sliding + Obstacle → Fall Flat
                Debug.Log("[PlayerAnim] 🏃 Sliding + Obstacle → MUTANT DYING");
                StartCoroutine(PlayMutantDyingSequence());
                break;
                
            default:
                Debug.LogWarning($"[PlayerAnim] Unknown death reason: {reason} → FALL FLAT");
                StartCoroutine(PlayFallFlatSequence());
                break;
        }
        
        ChangeState(PlayerAnimationState.Dead);
        
        // Stop movement animations
        _animator.SetFloat(PARAM_SPEED, 0f);
        
        // Reset injured state
        _isInjured = false;
        _animator.SetBool(PARAM_IS_INJURED, false);
    }

    /// <summary>
    /// Play Fall Flat death sequence - UPDATED: Fart on death
    /// </summary>
    private IEnumerator PlayFallFlatSequence()
    {
        if (showDebugLogs)
        {
            Debug.Log($"[PlayerAnim] 💀 Fall Flat animation started ({fallFlatDuration}s)");
        }
        
        // Trigger animation
        _animator.SetTrigger(TRIGGER_FALL_FLAT);
        
        // ═══ NEW: FART ON DEATH ═══
        if (enableFartEffects)
        {
            yield return new WaitForSeconds(fartDelayOnDeath);
            PlayFartEffect(FartType.Death);
            
            // Continue waiting for rest of animation
            yield return new WaitForSeconds(fallFlatDuration - fartDelayOnDeath);
        }
        else
        {
            // Wait for full animation
            yield return new WaitForSeconds(fallFlatDuration);
        }
        
        if (showDebugLogs)
        {
            Debug.Log("[PlayerAnim] ✓ Fall Flat animation complete");
        }
        
        // Trigger death complete event
        OnDeathAnimationComplete?.Invoke();
    }

    /// <summary>
    /// Play Mutant Dying death sequence - UPDATED: Fart on death
    /// </summary>
    private IEnumerator PlayMutantDyingSequence()
    {
        if (showDebugLogs)
        {
            Debug.Log($"[PlayerAnim] 🧟 Mutant Dying animation started ({mutantDyingDuration}s)");
        }

        // Trigger animation
        _animator.SetTrigger(TRIGGER_MUTANT_DYING);

        // ═══ NEW: FART ON DEATH ═══
        if (enableFartEffects)
        {
            yield return new WaitForSeconds(fartDelayOnDeath);
            PlayFartEffect(FartType.Death);

            // Continue waiting for rest of animation
            yield return new WaitForSeconds(mutantDyingDuration);
        }
        else
        {
            // Wait for full animation
            yield return new WaitForSeconds(mutantDyingDuration);
        }

        if (showDebugLogs)
        {
            Debug.Log("[PlayerAnim] ✓ Mutant Dying animation complete");
        }

        // Trigger death complete event
        OnDeathAnimationComplete?.Invoke();
    }

    #endregion
    

    #region Fart System - NEW

    /// <summary>
    /// Fart types
    /// </summary>
    private enum FartType
    {
        Drinking,  // After drinking
        Death,     // On death
        Toilet     // On toilet (handled by VictorySequenceController)
    }

    /// <summary>
    /// Play fart effect (sound + VFX)
    /// </summary>
    private void PlayFartEffect(FartType type)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[PlayerAnim] 💨 Playing {type} fart effect");
        }

        // Play sound
        switch (type)
        {
            case FartType.Drinking:
                AudioManager.Instance?.PlayFartDrinking();
                break;
                
            case FartType.Death:
                AudioManager.Instance?.PlayFartDeath();
                break;
                
            case FartType.Toilet:
                AudioManager.Instance?.PlayFartToilet();
                break;
        }

        // Play VFX (except toilet - no VFX needed)
        if (type != FartType.Toilet)
        {
            if (fartVFXController != null)
            {
                fartVFXController.PlayFartVFX();
            }
            else if (FartVFXController.Instance != null)
            {
                FartVFXController.Instance.PlayFartVFX();
            }
            else
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning("[PlayerAnim] FartVFXController not found!");
                }
            }
        }
    }

    /// <summary>
    /// Public API: Play toilet fart (called externally)
    /// </summary>
    public void PlayToiletFart()
    {
        PlayFartEffect(FartType.Toilet);
    }

    public void PlayQuestionVFX()
    {
        if (fartVFXController != null)
        {
            fartVFXController.PlayQuestionVFX();
        }
        else if (FartVFXController.Instance != null)
        {
            FartVFXController.Instance.PlayQuestionVFX();
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("[PlayerAnim] FartVFXController not found!");
            }
        }
    }

    #endregion


    #region Victory - Sitting Animation
    
    /// <summary>
    /// Play sitting animation when reaching toilet
    /// </summary>
    public void OnUseToilet()
    {
        if (_animator == null)
            return;
        
        ChangeState(PlayerAnimationState.Sitting);
        
        _animator.SetTrigger(TRIGGER_SITTING);
        _animator.SetFloat(PARAM_SPEED, 0f); // Stop running
        
        if (showDebugLogs)
        {
            Debug.Log($"[PlayerAnim] 🚽 Sitting animation started ({sittingDuration}s)");
        }
        
        // Start sitting sequence
        StartCoroutine(SittingSequence());
    }

    private IEnumerator SittingSequence()
    {
        // Wait for sitting duration
        yield return new WaitForSeconds(sittingDuration);
        
        // Sitting complete
        OnSittingFinished();
    }

    private void OnSittingFinished()
    {
        if (showDebugLogs)
        {
            Debug.Log("[PlayerAnim] ✓ Sitting finished → Trigger Victory");
        }
        
        // ═══ CRITICAL: Trigger event ═══
        OnSittingComplete?.Invoke();
    }
    
    #endregion

    #region Reset
    
    /// <summary>
    /// Reset all animation states
    /// </summary>
    public void ResetAnimations()
    {
        if (_animator == null)
            return;
        
        _animator.SetFloat(PARAM_SPEED, 0f);
        _animator.SetBool(PARAM_IS_GROUNDED, true);
        _animator.SetBool(PARAM_IS_SLIDING, false);
        _animator.SetBool(PARAM_IS_INJURED, false); // ← NEW
        
        _isSliding = false;
        _isInjured = false; // ← NEW
        _turnTimer = 0f;
        _currentState = PlayerAnimationState.Idle;
        
        if (showDebugLogs)
        {
            Debug.Log("[PlayerAnim] ✓ Animations reset");
        }
    }
    #endregion

    #region Public API
    
    /// <summary>
    /// Force stop all animations
    /// </summary>
    public void StopAllAnimations()
    {
        if (_animator == null)
            return;
        
        _animator.SetFloat(PARAM_SPEED, 0f);
        _isSliding = false;
        
        if (showDebugLogs)
        {
            Debug.Log("[PlayerAnim] ⏸ All animations stopped");
        }
    }
    
    #endregion
}

/// <summary>
/// Player Animation States - State Pattern
/// </summary>
public enum PlayerAnimationState
{
    Idle,
    Drinking,
    Running,
    Jumping,
    Sliding,
    Injured,
    Sitting,
    Dead
}