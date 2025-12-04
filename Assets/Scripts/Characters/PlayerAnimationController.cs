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
    #region Animation Parameters

    private static readonly int PARAM_SPEED = Animator.StringToHash("Speed");
    private static readonly int PARAM_IS_GROUNDED = Animator.StringToHash("IsGrounded");
    
    // ═══ ADDED: New animations ═══
    private static readonly int TRIGGER_WALK = Animator.StringToHash("Walk");
    private static readonly int TRIGGER_LOOK_BEHIND = Animator.StringToHash("LookBehind");
    
    private static readonly int TRIGGER_RUNNING = Animator.StringToHash("Running");
    private static readonly int TRIGGER_JUMP = Animator.StringToHash("Jump");
    private static readonly int TRIGGER_SLIDE = Animator.StringToHash("Slide");
    private static readonly int TRIGGER_FALL_FLAT = Animator.StringToHash("FallFlat");
    private static readonly int TRIGGER_MUTANT_DYING = Animator.StringToHash("MutantDying");

    private static readonly int PARAM_IS_SLIDING = Animator.StringToHash("IsSliding");
    private static readonly int TRIGGER_INJURED = Animator.StringToHash("Injured");
    private static readonly int PARAM_IS_INJURED = Animator.StringToHash("IsInjured");

    /* ═══ COMMENTED OUT: OLD ANIMATIONS ═══
    private static readonly int TRIGGER_DRINKING = Animator.StringToHash("Drinking");
    private static readonly int TRIGGER_SITTING = Animator.StringToHash("Sitting");
    */

    #endregion

    #region Serialized Fields

    [Header("Animation Durations")]
    [SerializeField] private float walkDuration = 2f; // NEW
    [SerializeField] private float lookBehindDuration = 1f; // NEW
    [SerializeField] private float fallFlatDuration = 1.5f;
    [SerializeField] private float mutantDyingDuration = 2f;

    /* ═══ COMMENTED OUT: OLD DURATIONS ═══
    [SerializeField] private float drinkingDuration = 2f;
    [SerializeField] private float sittingDuration = 2f;
    */

    // [Header("Debug")]
    // [SerializeField] private bool showDebugLogs = true;

    /* ═══ COMMENTED OUT: FART SYSTEM ═══
    [Header("Fart System")]
    [SerializeField] private bool enableFartEffects = true;
    [SerializeField] private FartVFXController fartVFXController;
    [SerializeField] private float fartDelayAfterDrinking = 0.2f;
    [SerializeField] private float fartDelayOnDeath = 0.5f;
    */

    #endregion

    #region Events

    public event System.Action OnWalkComplete; // NEW
    public event System.Action OnLookBehindComplete; // NEW
    public event System.Action OnDeathAnimationComplete;

    /* ═══ COMMENTED OUT: OLD EVENTS ═══
    public event System.Action OnDrinkingComplete;
    public event System.Action OnSittingComplete;
    */

    #endregion

    #region Components
    
    [SerializeField] private Animator _animator;
    [SerializeField] private PlayerController _playerController;
    
    #endregion

    #region State
    
    public PlayerAnimationState _currentState = PlayerAnimationState.Idle;
    private bool _isSliding = false;
    private bool _isInjured = false; // ← NEW
    private float _turnTimer;
    
    #endregion


    #region Properties

    public float WalkDuration => walkDuration;
    public float LookBehindDuration => lookBehindDuration;
    public bool IsInjured => _isInjured;

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

        // if (_animator == null)
        // {
        //     Debug.LogError("[PlayerAnim] Animator component not found!");
        // }
        
        // if (fartVFXController == null)
        // {
        //     fartVFXController = FindObjectOfType<FartVFXController>();
        // }
    }

    void Start()
    {
        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();

            // if (_animator != null && showDebugLogs)
            // {
            //     Debug.Log("[PlayerAnim] ✓ Found animator in children");
            // }
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
        
        // if (showDebugLogs)
        // {
        //     Debug.Log($"[PlayerAnim] State: {_currentState} → {newState}");
        // }
        
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

        // ← FIX: Auto return to Running when landed
        if (_currentState == PlayerAnimationState.Jumping && _playerController.IsGrounded)
        {
            ChangeState(PlayerAnimationState.Running);
            
            // if (showDebugLogs)
            // {
            //     Debug.Log("[PlayerAnim] ✓ Landed → Running");
            // }
        }

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

    #region Game Start - CHANGED TO WALK → DOG HIT → LOOK BEHIND → RUN

    /// <summary>
    /// Play walk animation at game start
    /// CHANGED: Walk instead of Drinking
    /// </summary>
    public void OnGameStart()
    {
        if (_animator == null)
            return;
        
        ChangeState(PlayerAnimationState.Walking);
        
        _animator.SetTrigger(TRIGGER_WALK);
        
        // if (showDebugLogs)
        // {
        //     Debug.Log($"[PlayerAnim] 🚶 Walk animation started ({walkDuration}s)");
        // }
        
        StartCoroutine(WalkSequence());
    }

    private IEnumerator WalkSequence()
    {
        // Wait for walk duration
        yield return new WaitForSeconds(walkDuration);

        OnWalkFinished();
    }
    
    private void OnWalkFinished()
    {
        // Walk complete → Wait for dog collision
        // if (showDebugLogs)
        // {
        //     Debug.Log("[PlayerAnim] ✓ Walk finished → Waiting for dog collision");
        // }
        
        OnWalkComplete?.Invoke();
    }

    /// <summary>
    /// Trigger look behind animation (after dog collision)
    /// NEW
    /// </summary>
    public void OnDogCollision()
    {
        if (_animator == null)
            return;

        ChangeState(PlayerAnimationState.LookingBehind);
        
        _animator.SetTrigger(TRIGGER_LOOK_BEHIND);
        
        // if (showDebugLogs)
        // {
        //     Debug.Log($"[PlayerAnim] 😱 Look Behind animation started ({lookBehindDuration}s)");
        // }

        StartCoroutine(LookBehindSequence());
    }

    private IEnumerator LookBehindSequence()
    {
        // Wait for look behind duration
        yield return new WaitForSeconds(lookBehindDuration);

        OnLookBehindFinished();
    }

    private void OnLookBehindFinished()
    {
        // Look behind complete → Start running
        ChangeState(PlayerAnimationState.Running);

        _animator.SetTrigger(TRIGGER_RUNNING);

        // if (showDebugLogs)
        // {
        //     Debug.Log("[PlayerAnim] ✓ Look Behind finished → RUNNING!");
        // }

        OnLookBehindComplete?.Invoke();

        // Trigger dog chase
        if (DogChaseController.Instance != null)
        {
            DogChaseController.Instance.StartChase();
        }
    }
    
    #endregion

    #region Movement Animations
    
    /// <summary>
    /// Set player to idle state - NEW
    /// </summary>
    public void SetIdleState()
    {
        if (_animator == null)
            return;
        
        ChangeState(PlayerAnimationState.Idle);
        
        _animator.SetFloat(PARAM_SPEED, 0f);
        _animator.SetBool(PARAM_IS_GROUNDED, true);
        _animator.SetBool(PARAM_IS_SLIDING, false);
        _animator.SetBool(PARAM_IS_INJURED, false);
        
        // if (showDebugLogs)
        // {
        //     Debug.Log("[PlayerAnim] ⏸️ Set to Idle state");
        // }
    }
    
    /// <summary>
    /// Trigger jump animation
    /// </summary>
    public void OnJump()
    {
        if (_animator == null)
            return;
        
        ChangeState(PlayerAnimationState.Jumping);

        _animator.SetTrigger(TRIGGER_JUMP);
        
        // if (showDebugLogs)
        // {
        //     Debug.Log("[PlayerAnim] 🦘 Jump");
        // }
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
        
        // if (showDebugLogs)
        // {
        //     Debug.Log("[PlayerAnim] 🏃 Slide started");
        // }
    }

    /// <summary>
    /// End slide animation
    /// </summary>
    public void OnSlideEnd()
    {
        _isSliding = false;
        ChangeState(PlayerAnimationState.Running);
        
        // if (showDebugLogs)
        // {
        //     Debug.Log("[PlayerAnim] ✓ Slide ended");
        // }
    }

    /// <summary>
    /// Handle lane change animation
    /// </summary>
    public void OnLaneChange(int direction)
    {
        // Optional: Add turn animation if you have it
        //_turnTimer = turnAnimationDuration;
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
        
        // if (showDebugLogs)
        // {
        //     Debug.Log("[PlayerAnim] 🤕 Injured Walking started");
        // }
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
        
        // if (showDebugLogs)
        // {
        //     Debug.Log("[PlayerAnim] ✓ Recovered → Running");
        // }
    }

    #endregion

    #region Death Animations

    /// <summary>
    /// Trigger death animation - UPDATED: Check injured state
    /// </summary>
    public void OnDie(DeathReason reason)
    {
        if (_animator == null)
        {
            return;
        }

        //Debug.Log($"[PlayerAnim] OnDie() called - Reason: {reason}, Current State: {_currentState}");

        // ═══ CHECK: If currently injured, die directly from injured state ═══
        if (_isInjured)
        {
            //Debug.Log("[PlayerAnim] 💀 Dying from INJURED state - direct transition");
            OnDieFromInjured(reason);
            return;
        }

        // ═══ NORMAL DEATH: Based on current state ═══
        switch (reason)
        {
            case DeathReason.ObstacleRunning:
                StartCoroutine(PlayMutantDyingSequence());
                break;

            case DeathReason.ObstacleJumping:
            case DeathReason.ObstacleSliding:
                StartCoroutine(PlayFallFlatSequence());
                break;

            default:
                StartCoroutine(PlayFallFlatSequence());
                break;
        }

        ChangeState(PlayerAnimationState.Dead);
        _animator.SetFloat(PARAM_SPEED, 0f);
        _isInjured = false;
        _animator.SetBool(PARAM_IS_INJURED, false);
    }
    

    /// <summary>
    /// Die directly from injured state - NEW
    /// No transition to running, direct to death
    /// </summary>
    private void OnDieFromInjured(DeathReason reason)
    {
        if (_animator == null)
        {
            Debug.LogWarning("[PlayerAnim] Animator is null!");
            return;
        }

        // Debug.Log("[PlayerAnim] ═══════════════════════════════");
        // Debug.Log("[PlayerAnim] 💀 DEATH FROM INJURED STATE");
        // Debug.Log($"[PlayerAnim] Reason: {reason}");
        // Debug.Log($"[PlayerAnim] Current Animation: Injured Walking");
        // Debug.Log("[PlayerAnim] ═══════════════════════════════");

        // ═══ IMMEDIATELY SET STATE ═══
        ChangeState(PlayerAnimationState.Dead);
        
        // ═══ CLEAR INJURED STATE ═══
        _isInjured = false;
        _animator.SetBool(PARAM_IS_INJURED, false);
        
        // ═══ STOP MOVEMENT ═══
        _animator.SetFloat(PARAM_SPEED, 0f);
        
        // Debug.Log("[PlayerAnim] ✓ Injured state cleared");
        // Debug.Log("[PlayerAnim] ✓ Speed set to 0");

        // ═══ TRIGGER DEATH ANIMATION (ALWAYS MUTANT DYING FROM INJURED) ═══
        //Debug.Log("[PlayerAnim] → Playing MUTANT DYING animation");
        StartCoroutine(PlayMutantDyingSequence());
    }


    /// <summary>
    /// Play Fall Flat death animation
    /// </summary>
    private IEnumerator PlayFallFlatSequence()
    {
        _animator.SetTrigger(TRIGGER_FALL_FLAT);
        
        // if (showDebugLogs)
        // {
        //     Debug.Log("[PlayerAnim] 💀 Death animation: FALL FLAT");
        // }
        
        yield return new WaitForSeconds(fallFlatDuration);
        
        OnDeathAnimationComplete?.Invoke();
    }

    /// <summary>
    /// Play Mutant Dying death animation
    /// </summary>
    private IEnumerator PlayMutantDyingSequence()
    {
        _animator.SetTrigger(TRIGGER_MUTANT_DYING);

        // if (showDebugLogs)
        // {
        //     Debug.Log("[PlayerAnim] 💀 Death animation: MUTANT DYING");
        // }

        yield return new WaitForSeconds(mutantDyingDuration);

        OnDeathAnimationComplete?.Invoke();
    }


    #endregion
    

    #region Fart System - NEW

    /* ═══ COMMENTED OUT: FART SYSTEM ═══
    private enum FartType
    {
        Drinking,
        Death,
        Toilet
    }

    private void PlayFartEffect(FartType type)
    {
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

        if (type != FartType.Toilet)
        {
            if (fartVFXController != null)
            {
                fartVFXController.PlayFartVFX();
            }
        }
    }

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
    }
    */

    #endregion


    #region Victory - Sitting Animation
    
    /* ═══ COMMENTED OUT: VICTORY SITTING ANIMATION ═══
    public void OnUseToilet()
    {
        if (_animator == null)
            return;
        
        ChangeState(PlayerAnimationState.Sitting);
        
        _animator.SetTrigger(TRIGGER_SITTING);
        _animator.SetFloat(PARAM_SPEED, 0f);
        
        StartCoroutine(SittingSequence());
    }

    private IEnumerator SittingSequence()
    {
        yield return new WaitForSeconds(sittingDuration);
        OnSittingFinished();
    }

    private void OnSittingFinished()
    {
        OnSittingComplete?.Invoke();
    }
    */
    
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
        
        // if (showDebugLogs)
        // {
        //     Debug.Log("[PlayerAnim] ✓ Animations reset");
        // }
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
        
        // if (showDebugLogs)
        // {
        //     Debug.Log("[PlayerAnim] ⏸ All animations stopped");
        // }
    }
    
    #endregion
}

public enum PlayerAnimationState
{
    Idle,
    Walking, // NEW
    LookingBehind, // NEW
    Running,
    Jumping,
    Sliding,
    Injured,
    Dead

    /* ═══ COMMENTED OUT: OLD STATES ═══
    Drinking,
    Sitting
    */
}