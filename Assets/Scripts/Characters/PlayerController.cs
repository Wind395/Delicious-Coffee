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

    private CameraFollowController _cameraController;

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
        _cameraController = FindObjectOfType<CameraFollowController>();
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

        // ═══ CRITICAL: Subscribe to animation events ═══
        if (_animationController != null)
        {
            //_animationController.OnDrinkingComplete += OnDrinkingComplete;
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

    #region Walking Sequence - FOR INTRO

    private bool _isWalking = false;
    private float _walkingSpeed = 0f;

    /// <summary>
    /// Start walking sequence (for intro)
    /// </summary>
    public void StartWalkingSequence()
    {
        _canMove = true; // Enable movement
        _isWalking = true;
        _walkingSpeed = baseForwardSpeed * 0.5f; // Half speed
        _currentSpeed = _walkingSpeed;
    }

    /// <summary>
    /// Stop walking
    /// </summary>
    public void StopWalking()
    {
        _isWalking = false;
        _currentSpeed = 0f;
    }

    /// <summary>
    /// Enable movement control
    /// </summary>
    public void EnableMovement()
    {
        _canMove = true;
        _isWalking = false;
        _currentSpeed = baseForwardSpeed;
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
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        _speedMultiplier = Mathf.Max(0.1f, multiplier);
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

    #region Slow Effect System - REFACTORED

    /// <summary>
    /// Apply slow effect - UPDATED: Percentage-based reduction + Clear speed buff
    /// </summary>
    /// <param name="slowMultiplier">Speed multiplier (0.6 = giảm còn 60% tốc độ)</param>
    /// <param name="duration">Slow duration in seconds</param>
    /// <param name="sourceObstacle">Obstacle that caused slow</param>
    public void ApplySlowEffect(float slowMultiplier, float duration, GameObject sourceObstacle)
    {
        if (!_isAlive)
        {
            return;
        }

        // ═══ 1. CLEAR COLD TOWEL BUFF ═══
        if (PowerUpManager.Instance != null)
        {
            bool hadColdTowel = PowerUpManager.Instance.IsPowerUpActive<ColdTowelPowerUp>();
            
            if (hadColdTowel)
            {
                PowerUpManager.Instance.ClearPowerUp<ColdTowelPowerUp>();
            }
        }

        // ═══ 2. APPLY SLOW MULTIPLIER (TEMPORARY) ═══
        _isSlowed = true;
        _slowMultiplier = slowMultiplier;
        _slowTimer = duration;
        
        float newSpeed = _currentSpeed * slowMultiplier;
        

        // ═══ 3. TRIGGER INJURED ANIMATION ═══
        if (_animationController != null)
        {
            _animationController.OnInjured();
        }

        // ═══ 4. TRIGGER TEMPORARY DOG CHASE ═══
        if (DogChaseController.Instance != null)
        {
            DogChaseController.Instance.StartTemporaryChase();
        }

        // ═══ 5. DESTROY OBSTACLE ═══
        if (sourceObstacle != null)
        {
            sourceObstacle.SetActive(false);
        }

        // ═══ 6. PLAY FEEDBACK ═══
        PlaySlowEffectFeedback();

        // ═══ 7. SHOW VISUAL EFFECT (Optional) ═══
        if (slowEffectVisual != null)
        {
            slowEffectVisual.SetActive(true);
        }
    }

    /// <summary>
    /// Update slow effect timer - ALREADY EXISTS, just ensure it's called
    /// </summary>
    private void UpdateSlowEffect()
    {
        if (!_isSlowed) return;

        _slowTimer -= Time.deltaTime;

        if (_slowTimer <= 0f)
        {
            RecoverFromSlow();
        }
    }

    /// <summary>
    /// Recover from slow effect - NEW
    /// </summary>
    private void RecoverFromSlow()
    {
        _isSlowed = false;
        _slowMultiplier = 1f;
        _slowTimer = 0f;

        // Hide visual effect
        if (slowEffectVisual != null)
        {
            slowEffectVisual.SetActive(false);
        }

        
        // Notify animation controller
        if (_animationController != null && _animationController.IsInjured)
        {
            _animationController.OnRecovered();
        }
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
        // Sound
        AudioManager.Instance?.PlayObstacleDestroySound();
        
        // Light camera shake
        _cameraController.Shake(0.2f, 0.3f);
        
        // Optional: Haptic feedback
        #if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        #endif
    }

    #endregion

    #region Movement

    private void UpdateMovement()
    {
        if (!_canMove)
        {
            return;
        }

        Vector3 moveVector = Vector3.zero;

        // ═══ UPDATED: Check if walking (auto-move forward) ═══
        if (_isWalking)
        {
            moveVector += Vector3.forward * _walkingSpeed * Time.deltaTime;
        }
        else
        {
            // Normal movement with speed
            moveVector += Vector3.forward * _currentSpeed * Time.deltaTime;
        }

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

    /// <summary>
    /// Try to jump - UPDATED: Block when injured
    /// </summary>
    private void TryJump()
    {
        // ═══ CHECK: Cannot jump when injured ═══
        if (_animationController != null && _animationController.IsInjured)
        {
            
            PlayInvalidInputFeedback();
            return;
        }

        // Normal jump check
        if (_isGrounded && !_isSliding)
        {
            Jump();
        }
        else
        {
            PlayInvalidInputFeedback();
        }
    }

    /// <summary>
    /// Execute jump
    /// </summary>
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


        // Priority 1: Shield (Medicine PowerUp) - Blocks everything
        if (HasShield)
        {
            OnShieldHitObstacle(obstacle);
            return;
        }

        // Priority 2: Ice Tea Invincibility - Pass through everything
        bool hasIceTeaInvincibility = PowerUpManager.Instance != null &&
                                       PowerUpManager.Instance.IsPowerUpActive<IceTeaPowerUp>();

        if (hasIceTeaInvincibility)
        {
            return;
        }
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
        }
    }

    #endregion

    #region Death

    /// <summary>
    /// Trigger death - UPDATED: Instant game over for double hit
    /// </summary>
    public void TriggerDeath()
    {
        if (!_isAlive)
        {
            return;
        }

        DeathReason reason = DetermineObstacleDeathReason();


        Die(reason, playHitSound: true);
    }
    
    /// <summary>
    /// Determine specific obstacle death reason - FIXED: Ưu tiên IsGrounded
    /// </summary>
    private DeathReason DetermineObstacleDeathReason()
    {

        // ═══ PRIORITY 1: Check IsSliding (highest priority)
        if (_isSliding)
        {
            return DeathReason.ObstacleSliding;
        }

        // ═══ PRIORITY 2: Check IsGrounded (physical state)
        if (!_isGrounded)
        {
            return DeathReason.ObstacleJumping;
        }

        // ═══ PRIORITY 3: Check Animation State (fallback)
        if (_animationController != null)
        {
            PlayerAnimationState animState = _animationController._currentState;
            
            switch (animState)
            {
                case PlayerAnimationState.Jumping:
                    return DeathReason.ObstacleJumping;
                    
                case PlayerAnimationState.Sliding:
                    return DeathReason.ObstacleSliding;
            }
        }

        // ═══ DEFAULT: Running
        
        return DeathReason.ObstacleRunning;
    }

    /// <summary>
    /// Core death logic - UPDATED: Faster game over
    /// </summary>
    private void Die(DeathReason reason, bool playHitSound)
    {
        if (!_isAlive) return;

        _isAlive = false;
        _canMove = false;
        _currentSpeed = 0;

        
        // ═══ TRIGGER ANIMATION ═══
        if (_animationController != null)
        {
            _animationController.OnDie(reason);
        }
        else
        {
            OnDeathAnimationComplete();
        }
        
        // ═══ PLAY SOUND ═══
        if (playHitSound)
        {
            AudioManager.Instance?.PlayHitSound();
        }

        // Camera shake
        _cameraController.Shake();

        // Haptic
        #if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        #endif
    }


    /// <summary>
    /// Called when death animation completes - TRIGGERS GAME OVER
    /// </summary>
    private void OnDeathAnimationComplete()
    {
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
    }

    public void DisableShield()
    {
        _activeShield = null;

        if (shieldVisual != null)
        {
            shieldVisual.SetActive(false);
        }
    }

    public void OnShieldHitObstacle(GameObject obstacle)
    {
        if (_activeShield == null || !_activeShield.IsActive)
        {
            return;
        }


        DestroyObstacle(obstacle);
        PlayShieldHitEffects(obstacle.transform.position);
        _activeShield.OnObstacleDestroyed(obstacle);

    }

    private void DestroyObstacle(GameObject obstacle)
    {
        if (obstacle == null) return;

        obstacle.SetActive(false);
    }

    private void PlayShieldHitEffects(Vector3 position)
    {
        AudioManager.Instance?.PlayShieldHitSound();
        AudioManager.Instance?.PlayObstacleDestroySound();
        _cameraController.Shake(0.3f, 0.5f);

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
    }

    public void StopPlayer()
    {
        _canMove = false;
        _currentSpeed = 0;
    }

    public void SetForwardSpeed(float speed)
    {
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

    #endregion

    #region Cleanup

    void OnDestroy()
    {
        UnsubscribeFromInputEvents();

        // Unsubscribe from animation events
        if (_animationController != null)
        {
            //_animationController.OnDrinkingComplete -= OnDrinkingComplete;
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
    ObstacleJumping
}