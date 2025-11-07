using UnityEngine;

/// <summary>
/// Player Controller - UPDATED with Invincibility System
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    #region Serialized Fields

    [Header("Movement")]
    [SerializeField] private float baseForwardSpeed = 10f;
    [SerializeField] private float speedIncreaseRate = 0.1f;
    [SerializeField] private float maxForwardSpeed = 20f;
    [SerializeField] private float laneChangeSpeed = 10f;
    [SerializeField] private float laneDistance = 3f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float gravity = 20f;

    [Header("Slide")]
    [SerializeField] private float slideDuration = 1f;
    [SerializeField] private float slideColliderHeight = 1f;

    [Header("Input")]
    [SerializeField] private bool useInputManager = true;
    [SerializeField] private float inputCooldown = 0.1f;

    [Header("Shield System")]
    [SerializeField] private GameObject shieldVisual;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Slow Effect")]
    [SerializeField] private GameObject slowEffectVisual;
    [SerializeField] private Color slowEffectColor = Color.blue;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    #endregion

    #region Components

    private CharacterController _characterController;
    private PlayerAnimationController _animationController;

    #endregion

    #region State

    private int _currentLane = 1;

    private float _currentSpeed;
    private float _speedMultiplier = 1f;

    private Vector3 _targetPosition;

    private float _verticalVelocity;
    private bool _isGrounded;

    private bool _isSliding;
    private float _slideTimer;
    private float _originalColliderHeight;

    private bool _canMove = false; // ← CHANGED: Start as false
    private bool _isAlive = true;
    private float _lastInputTime;

    private MedicinePowerUp _activeShield;
    public bool HasShield => _activeShield != null && _activeShield.IsActive;

    // Slow Effect State
    private bool _isSlowed = false;
    private float _slowTimer = 0f;
    private float _slowMultiplier = 1f;

    #endregion

    #region Properties

    public float CurrentSpeed => _currentSpeed;
    public float BaseSpeed => baseForwardSpeed;
    public bool IsSlowed => _isSlowed;

    // // ═══ NEW: Invincibility Property ═══
    // public bool IsInvincible => invincibilityController != null && invincibilityController.IsInvincible;

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _animationController = GetComponent<PlayerAnimationController>();
        _originalColliderHeight = _characterController.height;
    }

    void Start()
    {
        _targetPosition = transform.position;
        _currentSpeed = baseForwardSpeed;

        if (useInputManager)
        {
            SubscribeToInputEvents();
        }

        // Subscribe to invincibility events (if used)
        // if (invincibilityController != null)
        // {
        //     invincibilityController.OnInvincibilityStart += OnInvincibilityStarted;
        //     invincibilityController.OnInvincibilityEnd += OnInvincibilityEnded;
        // }

        // ═══ CRITICAL: Subscribe to animation events ═══
        if (_animationController != null)
        {
            _animationController.OnDrinkingComplete += OnDrinkingComplete;
            _animationController.OnDeathAnimationComplete += OnDeathAnimationComplete;
        }

        // ═══ NO: Don't start drinking here, let GameManager do it ═══
    }

    void Update()
    {
        // ═══ UPDATED: Check if can move AND alive ═══
        if (!_isAlive || !_canMove || GameManager.Instance.CurrentState != GameState.Playing)
            return;

        if (!useInputManager)
        {
            HandleKeyboardInput();
        }

        UpdateMovement();
        UpdateSlide();
        UpdateSpeed();
        UpdateSlowEffect();
    }

    #endregion


    #region Game Start - NEW

    /// <summary>
    /// Start drinking animation - Called by GameManager
    /// </summary>
    public void StartDrinkingSequence()
    {
        _canMove = false; // Ensure no movement
        _currentSpeed = 0f;

        if (_animationController != null)
        {
            _animationController.OnGameStart();

            if (showDebugLogs)
            {
                Debug.Log($"[Player] ☕ Drinking started - movement LOCKED for {_animationController.DrinkingDuration}s");
            }
        }
        else
        {
            Debug.LogError("[Player] No AnimationController - skipping drinking");
            OnDrinkingComplete(); // Skip to movement
        }
    }

    /// <summary>
    /// Called when drinking animation completes - ENABLE MOVEMENT
    /// </summary>
    private void OnDrinkingComplete()
    {
        _canMove = true;

        if (showDebugLogs)
        {
            Debug.Log("[Player] ✓ Drinking complete - player CAN MOVE now");
        }

        // Trigger game start events
        EventManager.Instance?.TriggerEvent(GameEvents.GAME_STARTED);
    }

    #endregion


    #region Speed System

    private void UpdateSpeed()
    {
        // Increase base speed over time
        if (baseForwardSpeed < maxForwardSpeed)
        {
            baseForwardSpeed += speedIncreaseRate * Time.deltaTime;
            baseForwardSpeed = Mathf.Min(baseForwardSpeed, maxForwardSpeed);
        }

        // Calculate actual speed with ALL multipliers
        float slowMult = GetSlowMultiplier();
        _currentSpeed = baseForwardSpeed * _speedMultiplier * slowMult;

        // Debug
        if (showDebugLogs && _isSlowed)
        {
            Debug.Log($"[Player] Speed: Base={baseForwardSpeed:F1} × PowerUp={_speedMultiplier:F2} × Slow={slowMult:F2} = {_currentSpeed:F1}");
        }
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        _speedMultiplier = Mathf.Max(0.1f, multiplier);

        if (showDebugLogs)
        {
            Debug.Log($"[Player] Speed multiplier set to {_speedMultiplier:F2}x");
        }
    }

    public void ResetSpeedMultiplier()
    {
        _speedMultiplier = 1f;
    }

    public float GetSpeedMultiplier()
    {
        return _speedMultiplier;
    }

    #endregion

    #region Slow Effect System

    /// <summary>
    /// Apply slow effect - UPDATED: No invincibility
    /// </summary>
    public void ApplySlowEffect(float multiplier, float duration, GameObject sourceObstacle)
    {
        if (!_isAlive)
        {
            if (showDebugLogs)
            {
                Debug.Log("[Player] Cannot apply slow - player dead");
            }
            return;
        }

        Debug.Log($"[Player] 🐌 Slow effect applied: {multiplier * 100:F0}% speed for {duration:F1}s");

        // Apply slow
        if (_isSlowed)
        {
            // Already slowed - check if new effect is stronger
            if (multiplier < _slowMultiplier)
            {
                _slowMultiplier = multiplier;
                _slowTimer = duration;
                Debug.Log($"[Player] Slow effect REFRESHED with stronger effect!");
            }
            else
            {
                // Extend duration
                _slowTimer = Mathf.Max(_slowTimer, duration);
                Debug.Log($"[Player] Slow effect EXTENDED to {_slowTimer:F1}s");
            }
        }
        else
        {
            // Start new slow effect
            _isSlowed = true;
            _slowMultiplier = multiplier;
            _slowTimer = duration;

            // Show visual
            if (slowEffectVisual != null)
            {
                slowEffectVisual.SetActive(true);
            }

            // ═══ TRIGGER INJURED ANIMATION ═══
            if (_animationController != null)
            {
                _animationController.OnInjured();
                Debug.Log($"[Player] ✓ Injured walking animation triggered");
            }

            Debug.Log($"[Player] ✓ Slow effect STARTED - Speed now {multiplier * 100:F0}%");
        }

        // ═══ DESTROY OBSTACLE ═══
        if (sourceObstacle != null)
        {
            // Play destruction effect
            Vector3 position = sourceObstacle.transform.position;
            //ObstacleDestructionEffect.Play(position);

            // Disable obstacle
            sourceObstacle.SetActive(false);

            Debug.Log($"[Player] 💥 Destroyed slow obstacle: {sourceObstacle.name}");
        }

        // ═══ REMOVED: No invincibility activation ═══
        // if (invincibilityController != null)
        // {
        //     invincibilityController.ActivateInvincibility(1f);
        // }

        // ═══ OPTIONAL: Visual flash (if you want to keep it) ═══
        // Can keep visual flash without invincibility
        // Uncomment if you want flashing without immunity:
        // if (visualEffects != null)
        // {
        //     visualEffects.StartFlashing();
        //     
        //     // Stop flashing after slow effect ends
        //     StartCoroutine(StopFlashingAfterDelay(duration));
        // }

        // Play feedback
        PlaySlowEffectFeedback();
    }

    /// <summary>
    /// Update slow effect timer
    /// </summary>
    private void UpdateSlowEffect()
    {
        if (!_isSlowed) return;

        _slowTimer -= Time.deltaTime;

        if (_slowTimer <= 0f)
        {
            EndSlowEffect();
        }
    }

    /// <summary>
    /// End slow effect
    /// </summary>
    private void EndSlowEffect()
    {
        _isSlowed = false;
        _slowMultiplier = 1f;
        _slowTimer = 0f;

        if (slowEffectVisual != null)
        {
            slowEffectVisual.SetActive(false);
        }

        // End injured walking animation
        if (_animationController != null)
        {
            _animationController.OnRecovered();
            Debug.Log($"[Player] ✓ Injured animation ended - back to running");
        }

        // ═══ OPTIONAL: Stop visual flash if used ═══
        // if (visualEffects != null)
        // {
        //     visualEffects.StopFlashing();
        // }

        Debug.Log($"[Player] ✓ Slow effect ENDED - speed restored to 100%");
    }

    /// <summary>
    /// Get current slow multiplier
    /// </summary>
    public float GetSlowMultiplier()
    {
        return _isSlowed ? _slowMultiplier : 1f;
    }

    /// <summary>
    /// Get slow time remaining
    /// </summary>
    public float GetSlowTimeRemaining()
    {
        return _isSlowed ? _slowTimer : 0f;
    }

    /// <summary>
    /// Play slow effect feedback
    /// </summary>
    private void PlaySlowEffectFeedback()
    {
        // Light camera shake
        FindObjectOfType<CameraFollowController>()?.Shake(0.2f, 0.2f);
    }

    #endregion

    #region Movement

    private void UpdateMovement()
    {
        // ═══ SAFETY: Check _canMove ═══
        if (!_canMove)
        {
            return;
        }

        Vector3 moveVector = Vector3.zero;

        // Forward movement
        moveVector += Vector3.forward * _currentSpeed * Time.deltaTime;

        // Lane change
        float currentX = transform.position.x;
        float targetX = _targetPosition.x;
        if (Mathf.Abs(currentX - targetX) > 0.01f)
        {
            float newX = Mathf.Lerp(currentX, targetX, laneChangeSpeed * Time.deltaTime);
            moveVector.x = newX - currentX;
        }

        UpdateVerticalMovement(ref moveVector);
        _characterController.Move(moveVector);
        _isGrounded = _characterController.isGrounded;
    }

    private void UpdateVerticalMovement(ref Vector3 moveVector)
    {
        if (_isGrounded)
        {
            if (_verticalVelocity < 0)
            {
                _verticalVelocity = -2f;
            }
        }
        else
        {
            _verticalVelocity -= gravity * Time.deltaTime;
        }

        moveVector.y = _verticalVelocity * Time.deltaTime;
    }

    #endregion

    #region Input System

    private void SubscribeToInputEvents()
    {
        if (InputManager.Instance == null)
        {
            Debug.LogError("[PlayerController] InputManager not found!");
            return;
        }

        InputManager.Instance.OnSwipeLeft += HandleSwipeLeft;
        InputManager.Instance.OnSwipeRight += HandleSwipeRight;
        InputManager.Instance.OnSwipeUp += HandleSwipeUp;
        InputManager.Instance.OnSwipeDown += HandleSwipeDown;
        InputManager.Instance.OnTap += HandleTap;
    }

    private void UnsubscribeFromInputEvents()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnSwipeLeft -= HandleSwipeLeft;
            InputManager.Instance.OnSwipeRight -= HandleSwipeRight;
            InputManager.Instance.OnSwipeUp -= HandleSwipeUp;
            InputManager.Instance.OnSwipeDown -= HandleSwipeDown;
            InputManager.Instance.OnTap -= HandleTap;
        }
    }

    private void HandleSwipeLeft()
    {
        if (!CanProcessInput()) return;
        ChangeLane(-1);
        _lastInputTime = Time.time;
        //PlayHapticFeedback();
    }

    private void HandleSwipeRight()
    {
        if (!CanProcessInput()) return;
        ChangeLane(1);
        _lastInputTime = Time.time;
        //PlayHapticFeedback();
    }

    private void HandleSwipeUp()
    {
        if (!CanProcessInput()) return;
        TryJump();
        _lastInputTime = Time.time;
        //PlayHapticFeedback();
    }

    private void HandleSwipeDown()
    {
        if (!CanProcessInput()) return;
        TrySlide();
        _lastInputTime = Time.time;
        //PlayHapticFeedback();
    }

    private void HandleTap()
    {
        if (!CanProcessInput()) return;
        TryJump();
        _lastInputTime = Time.time;
    }

    private bool CanProcessInput()
    {
        if (!_canMove || !_isAlive) return false;
        if (Time.time - _lastInputTime < inputCooldown) return false;
        return true;
    }

    private void PlayHapticFeedback()
    {
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    private void HandleKeyboardInput()
    {
        if (!_canMove) return;

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ChangeLane(-1);
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            ChangeLane(1);
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W))
        {
            TryJump();
        }

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            TrySlide();
        }
    }

    #endregion

    #region Lane Change

    private void ChangeLane(int direction)
    {
        int newLane = _currentLane + direction;

        if (newLane < 0 || newLane > 2)
        {
            PlayInvalidInputFeedback();
            return;
        }

        _currentLane = newLane;
        float targetX = (_currentLane - 1) * laneDistance;
        _targetPosition = new Vector3(targetX, transform.position.y, transform.position.z);

        _animationController?.OnLaneChange(direction);
        PlayLaneChangeFeedback();
    }

    #endregion

    #region Jump

    private void TryJump()
    {
        if (_isGrounded && !_isSliding)
        {
            Jump();
        }
        else
        {
            PlayInvalidInputFeedback();
        }
    }

    private void Jump()
    {
        _verticalVelocity = jumpForce;
        _isGrounded = false;

        _animationController?.OnJump();
        AudioManager.Instance?.PlayJumpSound();
    }

    #endregion

    #region Slide

    private void TrySlide()
    {
        if (_isGrounded && !_isSliding)
        {
            StartSlide();
        }
        else
        {
            PlayInvalidInputFeedback();
        }
    }

    private void StartSlide()
    {
        _isSliding = true;
        _slideTimer = slideDuration;

        _characterController.height = slideColliderHeight;
        _characterController.center = new Vector3(0, slideColliderHeight / 2, 0);

        _animationController?.OnSlide();
        AudioManager.Instance?.PlaySlideSound();
    }

    private void UpdateSlide()
    {
        if (!_isSliding) return;

        _slideTimer -= Time.deltaTime;

        if (_slideTimer <= 0)
        {
            EndSlide();
        }
    }

    private void EndSlide()
    {
        _isSliding = false;
        _characterController.height = _originalColliderHeight;
        _characterController.center = new Vector3(0, _originalColliderHeight / 2, 0);

        // ═══ NEW: Notify animation controller ═══
        _animationController?.OnSlideEnd();
    }

    #endregion

    #region Collision

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("Obstacle"))
        {
            HandleObstacleCollision(hit.gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            HandleObstacleCollision(other.gameObject);
        }
    }

    /// <summary>
    /// Handle obstacle collision - UPDATED: Check invincibility
    /// </summary>
    private void HandleObstacleCollision(GameObject obstacle)
    {
        if (!_isAlive || obstacle == null)
        {
            return;
        }

        if (showDebugLogs)
        {
            Debug.Log($"[Player] ═══ COLLISION WITH OBSTACLE ═══");
            Debug.Log($"[Player] Obstacle: {obstacle.name}");
            Debug.Log($"[Player] HasShield: {HasShield}");
        }

        // Priority 1: Shield (Medicine PowerUp) - Blocks everything
        if (HasShield)
        {
            Debug.Log("[Player] 🛡️ Shield blocked obstacle!");
            OnShieldHitObstacle(obstacle);
            return;
        }

        // Priority 2: Ice Tea Invincibility - Pass through everything
        bool hasIceTeaInvincibility = PowerUpManager.Instance != null &&
                                       PowerUpManager.Instance.IsPowerUpActive<IceTeaPowerUp>();

        if (hasIceTeaInvincibility)
        {
            Debug.Log("[Player] 🧊 Ice Tea invincible - passed through!");
            return;
        }

        // ═══ NEW: Priority 3: Slow Hit Invincibility - Ignore collision ═══
        // if (IsInvincible)
        // {
        //     Debug.Log("[Player] ⚡ Invincible from slow hit - ignored collision!");
        //     return;
        // }

        // Priority 4: Let obstacle handle behavior (Deadly vs Slow)
        // Obstacle will call ApplySlowEffect() if it's a Slow type
        // Or collision will trigger Die() if it's Deadly
    }

    #endregion

    #region Victory - Toilet Sequence

    /// <summary>
    /// Stop player for victory sequence - Called by ToiletTriggerZone
    /// </summary>
    public void StopForVictory()
    {
        _canMove = false;
        _currentSpeed = 0f;
        _isAlive = false; // Prevent any other actions

        if (showDebugLogs)
        {
            Debug.Log("[Player] 🚽 Stopped for victory sequence");
        }
    }

    /// <summary>
    /// Set position in front of toilet
    /// </summary>
    public void SetPositionInFrontOfToilet(Vector3 toiletPosition, float distance = 1.5f)
    {
        // Calculate position in front of toilet
        Vector3 targetPosition = toiletPosition - new Vector3(0, 0, distance);
        targetPosition.y = transform.position.y; // Keep current Y

        // Teleport
        _characterController.enabled = false;
        transform.position = targetPosition;
        _characterController.enabled = true;

        if (showDebugLogs)
        {
            Debug.Log($"[Player] ✓ Positioned at: {targetPosition}");
        }
    }

    /// <summary>
    /// Face toilet
    /// </summary>
    public void FaceToilet(Vector3 toiletPosition)
    {
        Vector3 direction = (toiletPosition - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = targetRotation;

            if (showDebugLogs)
            {
                Debug.Log("[Player] ✓ Facing toilet");
            }
        }
    }

    #endregion

    #region Death

    /// <summary>
    /// Trigger death from meter full - No hit sound
    /// </summary>
    public void TriggerDeathFromMeterFull()
    {
        if (!_isAlive)
        {
            Debug.LogWarning("[Player] Already dead!");
            return;
        }

        Debug.Log("[Player] ═══════════════════════════════════");
        Debug.Log("[Player] 💩 DEATH FROM METER FULL!");
        Debug.Log("[Player] ═══════════════════════════════════");
        
        // Die with meter full reason (no hit sound)
        Die(DeathReason.MeterFull, playHitSound: false);
    }

    /// <summary>
    /// Trigger death from obstacle - Determine specific reason
    /// </summary>
    public void TriggerDeath()
    {
        if (!_isAlive)
        {
            Debug.LogWarning("[Player] Already dead!");
            return;
        }

        // ═══ DETERMINE DEATH REASON BASED ON CURRENT STATE ═══
        DeathReason reason = DetermineObstacleDeathReason();

        Debug.Log($"[Player] 💥 Death from obstacle - Reason: {reason}");

        // Die with obstacle reason (play hit sound)
        Die(reason, playHitSound: true);
    }
    
    /// <summary>
    /// Determine specific obstacle death reason based on player state - NEW
    /// </summary>
    private DeathReason DetermineObstacleDeathReason()
    {
        // Check animation controller state if available
        if (_animationController != null)
        {
            PlayerAnimationState animState = _animationController.CurrentState;
            
            if (showDebugLogs)
            {
                Debug.Log($"[Player] Current animation state: {animState}");
            }

            switch (animState)
            {
                case PlayerAnimationState.Jumping:
                    return DeathReason.ObstacleJumping;
                    
                case PlayerAnimationState.Sliding:
                    return DeathReason.ObstacleSliding;
                    
                case PlayerAnimationState.Running:
                case PlayerAnimationState.Idle:
                default:
                    return DeathReason.ObstacleRunning;
            }
        }

        // Fallback: Check if sliding
        if (_isSliding)
        {
            return DeathReason.ObstacleSliding;
        }

        // Fallback: Check if grounded
        if (!_isGrounded)
        {
            return DeathReason.ObstacleJumping;
        }

        // Default: Running
        return DeathReason.ObstacleRunning;
    }

    /// <summary>
    /// Core death logic - UPDATED: Conditional sound
    /// </summary>
    private void Die(DeathReason deathReason, bool playHitSound)
    {
        if (!_isAlive) return;

        _isAlive = false;
        _canMove = false;
        _currentSpeed = 0;

        Debug.Log($"[Player] 💀 Die() - Reason: {deathReason}, Hit Sound: {playHitSound}");
        
        // ═══ TRIGGER ANIMATION ═══
        if (_animationController != null)
        {
            _animationController.OnDie(deathReason);
        }
        else
        {
            Debug.LogError("[Player] No AnimationController - calling GameOver directly");
            OnDeathAnimationComplete();
        }
        
        // ═══ UPDATED: CONDITIONAL HIT SOUND ═══
        if (playHitSound)
        {
            AudioManager.Instance?.PlayHitSound();
            Debug.Log("[Player] 🔊 Hit sound played");
        }
        else
        {
            Debug.Log("[Player] 🔇 Hit sound skipped (meter full death)");
        }

        // Camera shake
        FindObjectOfType<CameraFollowController>()?.Shake();

        // Haptic
        #if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        #endif
        
        Debug.Log("[Player] Waiting for death animation to complete...");
    }


    /// <summary>
    /// Called when death animation completes
    /// </summary>
    private void OnDeathAnimationComplete()
    {
        if (showDebugLogs)
        {
            Debug.Log("[Player] ✓ Death animation complete - triggering Game Over");
        }
        
        GameManager.Instance?.GameOver();
        EventManager.Instance?.TriggerEvent(GameEvents.PLAYER_DIED);
    }

    #endregion

    #region Shield System

    public void EnableShield(MedicinePowerUp shieldPowerUp)
    {
        _activeShield = shieldPowerUp;

        if (shieldVisual != null)
        {
            shieldVisual.SetActive(true);
        }

        Debug.Log("[Player] ✓ Shield enabled");
    }

    public void DisableShield()
    {
        _activeShield = null;

        if (shieldVisual != null)
        {
            shieldVisual.SetActive(false);
        }

        Debug.Log("[Player] Shield disabled");
    }

    public void OnShieldHitObstacle(GameObject obstacle)
    {
        if (_activeShield == null || !_activeShield.IsActive)
        {
            Debug.LogWarning("[Player] Shield hit but not active!");
            return;
        }

        Debug.Log($"[Player] 🛡️ Shield destroying obstacle: {obstacle.name}");

        DestroyObstacle(obstacle);
        PlayShieldHitEffects(obstacle.transform.position);
        _activeShield.OnObstacleDestroyed(obstacle);

        Debug.Log("[Player] ✓ Shield hit handled successfully");
    }

    private void DestroyObstacle(GameObject obstacle)
    {
        if (obstacle == null) return;

        obstacle.SetActive(false);

        if (showDebugLogs)
        {
            Debug.Log($"[Player] ✓ Obstacle disabled: {obstacle.name}");
        }
    }

    private void PlayShieldHitEffects(Vector3 position)
    {
        AudioManager.Instance?.PlayShieldHitSound();
        AudioManager.Instance?.PlayObstacleDestroySound();

        CameraFollowController camera = FindObjectOfType<CameraFollowController>();
        if (camera != null)
        {
            camera.Shake(0.3f, 0.5f);
        }

#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    #endregion

    #region Feedback

    private void PlayLaneChangeFeedback()
    {
        // TODO: VFX/SFX
    }

    private void PlayInvalidInputFeedback()
    {
        if (showDebugLogs)
        {
            Debug.Log("[Player] Invalid input");
        }
    }

    #endregion

    #region Public API

    public void ResetPlayer()
    {
        _isAlive = true;
        _canMove = false; // ← CRITICAL: Start disabled
        _currentLane = 1;
        _verticalVelocity = 0;
        _isSliding = false;
        _lastInputTime = 0f;
        _activeShield = null;
        _speedMultiplier = 1f;
        _currentSpeed = 0f; // ← Start at 0

        // Reset slow effect
        _isSlowed = false;
        _slowTimer = 0f;
        _slowMultiplier = 1f;
        if (slowEffectVisual != null)
        {
            slowEffectVisual.SetActive(false);
        }

        transform.position = new Vector3(0, -3.81f, 0);
        _targetPosition = transform.position;

        _characterController.height = _originalColliderHeight;

        if (shieldVisual != null)
        {
            shieldVisual.SetActive(false);
        }

        // ═══ CRITICAL: Reset and START drinking animation ═══
        if (_animationController != null)
        {
            _animationController.ResetAnimations();
            // Note: Don't start drinking here, GameManager will call StartDrinkingSequence()
        }

        if (showDebugLogs)
        {
            Debug.Log("[Player] ✓ Reset complete - waiting for drinking sequence");
        }
    }

    public void StopPlayer()
    {
        _canMove = false;
        _currentSpeed = 0;
    }

    public void SetForwardSpeed(float speed)
    {
        Debug.LogWarning("[Player] SetForwardSpeed is deprecated. Use SetSpeedMultiplier instead.");
    }

    public void SetLaneChangeSpeed(float speed)
    {
        laneChangeSpeed = speed;
    }

    public void SetUseInputManager(bool use)
    {
        useInputManager = use;

        if (use)
        {
            SubscribeToInputEvents();
        }
        else
        {
            UnsubscribeFromInputEvents();
        }
    }

    #endregion

    #region Getters

    public bool IsAlive => _isAlive;
    public bool IsGrounded => _isGrounded;
    public int CurrentLane => _currentLane;
    public bool IsSliding => _isSliding;

    // ═══ ADD: Detailed invincibility check ═══
    // public bool IsInvincible
    // {
    //     get
    //     {
    //         bool result = invincibilityController != null && invincibilityController.IsInvincible;

    //         if (showDebugLogs)
    //         {
    //             if (invincibilityController != null)
    //             {
    //                 Debug.Log($"[Player] IsInvincible check: {result} - {invincibilityController.GetStateInfo()}");
    //             }
    //             else
    //             {
    //                 Debug.Log($"[Player] IsInvincible check: false (no controller)");
    //             }
    //         }

    //         return result;
    //     }
    // }

    #endregion

    #region Cleanup

    void OnDestroy()
    {
        UnsubscribeFromInputEvents();

        // ═══ REMOVE OR COMMENT OUT: ═══
        // if (invincibilityController != null)
        // {
        //     invincibilityController.OnInvincibilityStart -= OnInvincibilityStarted;
        //     invincibilityController.OnInvincibilityEnd -= OnInvincibilityEnded;
        // }

        // Unsubscribe from animation events
        if (_animationController != null)
        {
            _animationController.OnDrinkingComplete -= OnDrinkingComplete;
            _animationController.OnDeathAnimationComplete -= OnDeathAnimationComplete;
        }
    }

    #endregion
}

/// <summary>
/// Death Reason Enum - NEW
/// </summary>
public enum DeathReason
{
    ObstacleRunning,
    ObstacleSliding,
    ObstacleJumping,
    MeterFull   // Diarrhea meter reached 100%
}