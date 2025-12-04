using UnityEngine;
using System.Collections;

/// <summary>
/// Dog Chase Controller - UPDATED: Death catch with position offset
/// </summary>
public class DogChaseController : MonoBehaviour
{
    #region Singleton
    
    private static DogChaseController _instance;
    public static DogChaseController Instance => _instance;
    
    #endregion

    #region Serialized Fields

    [Header("Dog Settings")]
    [SerializeField] private GameObject dogPrefab;
    [SerializeField] private float dogChaseSpeed = 8f;
    [SerializeField] private float dogReturnSpeed = 5f;
    [SerializeField] private float dogStartDistance = 15f;
    [SerializeField] private float dogMaxDistance = 30f;

    [Header("Dog Position")]
    [Tooltip("Dog Y position (ground level, should match track Y)")]
    [SerializeField] private float dogGroundY = -3.81f;

    [Header("Chase Mechanics")]
    [SerializeField] private float chaseAccelerationOnHit = 5f;
    [SerializeField] private float safeDuration = 3f;

    [Header("Temporary Chase (Slow Obstacle Hit)")]
    [Tooltip("Duration dog stays visible after reaching position")]
    [SerializeField] private float temporaryChaseDisplayDuration = 3f;

    [Tooltip("Final position dog stops at (behind player)")]
    [SerializeField] private float temporaryChaseStopDistance = 10f;

    [Tooltip("Dog spawn distance when temporary chase starts (far behind)")]
    [SerializeField] private float temporaryChaseSpawnDistance = 25f;

    [Tooltip("Dog run speed during temporary chase")]
    [SerializeField] private float temporaryChaseRunSpeed = 7f;

    // ═══ NEW: Death Catch Settings ═══
    [Header("Death Catch Settings (Player Loses)")]
    [Tooltip("Distance dog stops from player when catching (meters)")]
    [SerializeField] private float deathCatchStopDistance = 3f;

    [Tooltip("Dog spawn distance behind player when death catch starts")]
    [SerializeField] private float deathCatchSpawnDistance = 12f;

    [Tooltip("Dog run speed during death catch")]
    [SerializeField] private float deathCatchRunSpeed = 15f;

    [Tooltip("Max time for dog to reach player (seconds)")]
    [SerializeField] private float deathCatchMaxDuration = 3f;

    [Tooltip("Dog position offset from player center (X, Y, Z)")]
    [SerializeField] private Vector3 deathCatchPositionOffset = new Vector3(0f, 0f, -3f);

    [Header("Retreat Settings (After Temporary Chase)")]
    [Tooltip("Initial retreat speed")]
    [SerializeField] private float retreatStartSpeed = 6f;

    [Tooltip("Retreat deceleration rate (m/s²)")]
    [SerializeField] private float retreatDeceleration = 2f;

    [Tooltip("Minimum retreat speed before stop")]
    [SerializeField] private float retreatMinSpeed = 1f;

    [Tooltip("Extra distance to retreat before off-screen check")]
    [SerializeField] private float retreatExtraDistance = 10f;

    [Header("Audio")]
    [SerializeField] private AudioClip barkSound;

    // [Header("Debug")]
    // [SerializeField] private bool showDebugLogs = false;

    #endregion

    #region State
    
    private GameObject _dogInstance;
    private Animator _dogAnimator;
    private Transform _playerTransform;
    private bool _isChasing = false;
    private bool _isActive = false;
    private float _currentDogDistance;
    private float _safeTimer = 0f;
    
    private bool _isTemporaryChase = false;
    private Coroutine _temporaryChaseCoroutine;
    
    // Animation parameters
    private static readonly int PARAM_IS_RUNNING = Animator.StringToHash("IsRunning");
    private static readonly int TRIGGER_BARK = Animator.StringToHash("Bark");
    private static readonly int TRIGGER_IDLE = Animator.StringToHash("Idle");
    
    #endregion

    #region Properties
    
    public bool IsChasing => _isChasing;
    public float DogDistance => _currentDogDistance;
    public GameObject DogInstance => _dogInstance;
    
    #endregion

    #region Events
    
    public event System.Action OnDogCatchPlayer;
    public event System.Action OnDogDisappear;
    
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
        if (_isActive && _isChasing && !_isTemporaryChase)
        {
            UpdateDogPosition();
            UpdateSafeTimer();
        }
    }
    
    #endregion

    #region Initialization
    
    private void Initialize()
    {
        FindPlayer();
        
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
            PlayerController player = GameManager.Instance.GetPlayer();
            if (player != null)
            {
                _playerTransform = player.transform;
            }
        }

        if (_playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                _playerTransform = playerObj.transform;
            }
        }
    }
    
    #endregion

    #region Dog Spawning
    
    private void SpawnDog()
    {
        if (dogPrefab == null)
        {
            //Debug.LogError("[DogChase] Dog prefab not assigned!");
            return;
        }

        if (_dogInstance != null)
        {
            //Debug.LogWarning("[DogChase] Dog already exists!");
            return;
        }

        Vector3 spawnPos = _playerTransform.position - _playerTransform.forward * dogStartDistance;
        spawnPos.y = dogGroundY;

        _dogInstance = Instantiate(dogPrefab, spawnPos, Quaternion.identity);
        _dogInstance.name = "ChasingDog";

        _dogAnimator = _dogInstance.GetComponent<Animator>();
        
        _currentDogDistance = dogStartDistance;

        // if (showDebugLogs)
        // {
        //     Debug.Log($"[DogChase] 🐕 Dog spawned at distance {dogStartDistance}m (Y={dogGroundY})");
        // }
    }

    private void RemoveDog()
    {
        if (_dogInstance != null)
        {
            Destroy(_dogInstance);
            _dogInstance = null;
            _dogAnimator = null;
        }
    }
    
    #endregion

    #region Chase Control - PERMANENT CHASE
    
    public void StartChase()
    {
        if (_isChasing && !_isTemporaryChase)
        {
            // if (showDebugLogs)
            //     Debug.Log("[DogChase] Already in permanent chase");
            return;
        }

        if (_isTemporaryChase)
        {
            StopTemporaryChase();
        }

        _isChasing = true;
        _isActive = true;
        _isTemporaryChase = false;
        _safeTimer = 0f;

        if (_dogInstance == null)
        {
            SpawnDog();
        }

        if (_dogAnimator != null)
        {
            _dogAnimator.SetBool(PARAM_IS_RUNNING, true);
            //Debug.Log("[DogChase] ✓ Dog animation: RUNNING");
        }
        // else
        // {
        //     Debug.LogWarning("[DogChase] ⚠ Dog Animator is NULL!");
        // }

        PlayBark();

        // if (showDebugLogs)
        // {
        //     Debug.Log("[DogChase] 🐕 PERMANENT chase started!");
        // }
    }

    public void StopChase()
    {
        _isChasing = false;
        _isActive = false;
        _isTemporaryChase = false;

        RemoveDog();

        OnDogDisappear?.Invoke();

        // if (showDebugLogs)
        // {
        //     Debug.Log("[DogChase] 🐕 Dog disappeared");
        // }
    }

    #endregion

    #region Victory/Home Stop

    public void StopChaseOnVictory()
    {
        //Debug.Log("[DogChase] 🏠 Player reached home - stopping chase");

        _isChasing = false;
        _isTemporaryChase = false;

        if (_dogAnimator != null)
        {
            _dogAnimator.SetBool("IsRunning", false);
            _dogAnimator.SetBool("IsIdle", true);
            
            //Debug.Log("[DogChase] 🐕 Dog animations stopped");
        }

        OnDogDisappear?.Invoke();

        //Debug.Log("[DogChase] ✓ Chase stopped on victory");
    }

    #endregion

    #region Dog Retreat

    private IEnumerator RetreatDogOffScreen()
    {
        if (_dogInstance == null || _playerTransform == null)
        {
            //Debug.LogWarning("[DogChase] Cannot retreat - dog or player missing");
            yield break;
        }

        //Debug.Log("[DogChase] 🐕 Dog retreating off screen...");

        float currentSpeed = retreatStartSpeed;
        Vector3 retreatDirection = -_playerTransform.forward;
        retreatDirection.y = 0;
        retreatDirection.Normalize();

        if (_dogAnimator != null)
        {
            _dogAnimator.SetBool(PARAM_IS_RUNNING, true);
        }

        Camera mainCamera = Camera.main;
        // if (mainCamera == null)
        // {
        //     Debug.LogWarning("[DogChase] No main camera - using distance fallback");
        // }

        // if (showDebugLogs)
        // {
        //     Debug.Log($"[DogChase] Retreat start - Speed: {currentSpeed} m/s, Direction: {retreatDirection}");
        // }

        float retreatTime = 0f;
        bool isOffScreen = false;

        while (!isOffScreen && currentSpeed > retreatMinSpeed)
        {
            if (_dogInstance == null)
            {
                //Debug.LogWarning("[DogChase] Dog destroyed during retreat!");
                yield break;
            }

            currentSpeed -= retreatDeceleration * Time.deltaTime;
            currentSpeed = Mathf.Max(currentSpeed, retreatMinSpeed);

            _dogInstance.transform.position += retreatDirection * currentSpeed * Time.deltaTime;

            Vector3 faceDirection = (_playerTransform.position - _dogInstance.transform.position).normalized;
            faceDirection.y = 0;
            if (faceDirection != Vector3.zero)
            {
                _dogInstance.transform.rotation = Quaternion.LookRotation(faceDirection);
            }

            if (mainCamera != null)
            {
                isOffScreen = IsOffScreen(_dogInstance.transform.position, mainCamera);
            }
            else
            {
                float distanceFromPlayer = Vector3.Distance(_dogInstance.transform.position, _playerTransform.position);
                isOffScreen = distanceFromPlayer > (temporaryChaseStopDistance + retreatExtraDistance);
            }

            retreatTime += Time.deltaTime;

            if (retreatTime > 10f)
            {
                //Debug.LogWarning("[DogChase] Retreat timeout - forcing off screen");
                break;
            }

            yield return null;
        }

        if (_dogAnimator != null)
        {
            _dogAnimator.SetBool(PARAM_IS_RUNNING, false);
        }

        // if (showDebugLogs)
        // {
        //     Debug.Log($"[DogChase] ✓ Dog off screen after {retreatTime:F1}s at speed {currentSpeed:F1} m/s");
        // }

        yield return new WaitForSeconds(0.2f);

        _isTemporaryChase = false;
        _isChasing = false;
        _isActive = false;

        RemoveDog();

        OnDogDisappear?.Invoke();

        // if (showDebugLogs)
        // {
        //     Debug.Log("[DogChase] 🐕 Dog removed from scene");
        // }
    }

    private bool IsOffScreen(Vector3 worldPosition, Camera camera)
    {
        if (camera == null) return false;

        Vector3 viewportPos = camera.WorldToViewportPoint(worldPosition);
        
        bool isOutsideX = viewportPos.x < -0.1f || viewportPos.x > 1.1f;
        bool isOutsideY = viewportPos.y < -0.1f || viewportPos.y > 1.1f;
        bool isBehindCamera = viewportPos.z < 0f;

        bool offScreen = isOutsideX || isOutsideY || isBehindCamera;

        // if (showDebugLogs && offScreen)
        // {
        //     Debug.Log($"[DogChase] Off screen check: VP=({viewportPos.x:F2}, {viewportPos.y:F2}, {viewportPos.z:F2})");
        // }

        return offScreen;
    }

    #endregion

    #region Temporary Chase

    public void StartTemporaryChase()
    {
        if (_isChasing && !_isTemporaryChase)
        {
            OnPlayerHitObstacle();
            return;
        }

        if (_isTemporaryChase)
        {
            if (_temporaryChaseCoroutine != null)
            {
                StopCoroutine(_temporaryChaseCoroutine);
            }
        }

        _temporaryChaseCoroutine = StartCoroutine(TemporaryChaseSequence());
    }

    private IEnumerator TemporaryChaseSequence()
    {
        //Debug.Log("[DogChase] ▶️ Starting TEMPORARY chase (dog will run to player)...");

        _isTemporaryChase = true;
        _isChasing = true;
        _isActive = false;

        if (_dogInstance == null)
        {
            SpawnDogForTemporaryChase();
        }
        else
        {
            RepositionDogFarBehind();
        }

        yield return StartCoroutine(RunDogToPosition());

        //Debug.Log($"[DogChase] Dog reached position - staying for {temporaryChaseDisplayDuration}s");

        if (_dogAnimator != null)
        {
            _dogAnimator.SetBool(PARAM_IS_RUNNING, true);
        }

        float elapsed = 0f;
        while (elapsed < temporaryChaseDisplayDuration)
        {
            if (_dogInstance != null && _playerTransform != null)
            {
                UpdateTemporaryDogPosition();
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return StartCoroutine(RetreatDogOffScreen());

        //Debug.Log("[DogChase] ✓ Temporary chase ended - dog off screen");
    }

    private void SpawnDogForTemporaryChase()
    {
        if (dogPrefab == null || _playerTransform == null)
        {
            //Debug.LogError("[DogChase] Cannot spawn dog - missing prefab or player!");
            return;
        }

        Vector3 spawnPos = _playerTransform.position - _playerTransform.forward * temporaryChaseSpawnDistance;
        spawnPos.y = dogGroundY;

        _dogInstance = Instantiate(dogPrefab, spawnPos, Quaternion.identity);
        _dogInstance.name = "ChasingDog_Temporary";

        _dogAnimator = _dogInstance.GetComponent<Animator>();

        Vector3 directionToPlayer = (_playerTransform.position - _dogInstance.transform.position).normalized;
        directionToPlayer.y = 0;
        if (directionToPlayer != Vector3.zero)
        {
            _dogInstance.transform.rotation = Quaternion.LookRotation(directionToPlayer);
        }

        // if (showDebugLogs)
        // {
        //     Debug.Log($"[DogChase] 🐕 Dog spawned at {temporaryChaseSpawnDistance}m behind player (Y={dogGroundY})");
        //     Debug.Log($"[DogChase] Spawn position: {spawnPos}");
        // }
    }

    private void RepositionDogFarBehind()
    {
        if (_dogInstance == null || _playerTransform == null) return;

        Vector3 spawnPos = _playerTransform.position - _playerTransform.forward * temporaryChaseSpawnDistance;
        spawnPos.y = dogGroundY;

        _dogInstance.transform.position = spawnPos;

        Vector3 directionToPlayer = (_playerTransform.position - _dogInstance.transform.position).normalized;
        directionToPlayer.y = 0;
        if (directionToPlayer != Vector3.zero)
        {
            _dogInstance.transform.rotation = Quaternion.LookRotation(directionToPlayer);
        }

        // if (showDebugLogs)
        // {
        //     Debug.Log($"[DogChase] 🐕 Dog repositioned to {temporaryChaseSpawnDistance}m behind (Y={dogGroundY})");
        // }
    }

    private IEnumerator RunDogToPosition()
    {
        if (_dogInstance == null || _playerTransform == null)
        {
            //Debug.LogError("[DogChase] Cannot run - dog or player missing!");
            yield break;
        }

        //Debug.Log("[DogChase] 🐕 Dog running to player...");

        if (_dogAnimator != null)
        {
            _dogAnimator.SetBool(PARAM_IS_RUNNING, true);
            // if (showDebugLogs)
            // {
            //     Debug.Log("[DogChase] ✓ Dog animation: RUNNING");
            // }
        }

        float targetDistance = temporaryChaseStopDistance;
        float currentDistance = Vector3.Distance(_dogInstance.transform.position, _playerTransform.position);

        while (currentDistance > targetDistance)
        {
            if (_dogInstance == null || _playerTransform == null)
            {
                //Debug.LogWarning("[DogChase] Dog or player destroyed during run!");
                yield break;
            }

            Vector3 targetPos = _playerTransform.position - _playerTransform.forward * targetDistance;
            targetPos.y = dogGroundY;

            Vector3 direction = (targetPos - _dogInstance.transform.position).normalized;
            direction.y = 0;

            _dogInstance.transform.position += direction * temporaryChaseRunSpeed * Time.deltaTime;

            if (direction != Vector3.zero)
            {
                _dogInstance.transform.rotation = Quaternion.LookRotation(direction);
            }

            currentDistance = Vector3.Distance(_dogInstance.transform.position, _playerTransform.position);

            yield return null;
        }

        // if (showDebugLogs)
        // {
        //     Debug.Log($"[DogChase] ✓ Dog reached position ({currentDistance:F1}m from player)");
        // }

        _currentDogDistance = temporaryChaseStopDistance;
    }

    private void UpdateTemporaryDogPosition()
    {
        if (_dogInstance == null || _playerTransform == null)
            return;

        Vector3 targetPos = _playerTransform.position - _playerTransform.forward * temporaryChaseStopDistance;
        targetPos.y = dogGroundY;

        _dogInstance.transform.position = Vector3.Lerp(
            _dogInstance.transform.position,
            targetPos,
            5f * Time.deltaTime
        );

        Vector3 directionToPlayer = (_playerTransform.position - _dogInstance.transform.position).normalized;
        directionToPlayer.y = 0;
        if (directionToPlayer != Vector3.zero)
        {
            _dogInstance.transform.rotation = Quaternion.LookRotation(directionToPlayer);
        }

        _currentDogDistance = temporaryChaseStopDistance;
    }

    private void StopTemporaryChase()
    {
        _isTemporaryChase = false;
        _isChasing = false;
        _isActive = false;

        if (_dogAnimator != null)
        {
            _dogAnimator.SetBool(PARAM_IS_RUNNING, false);
        }

        RemoveDog();

        OnDogDisappear?.Invoke();

        // if (showDebugLogs)
        // {
        //     Debug.Log("[DogChase] 🐕 Temporary chase stopped (emergency cleanup)");
        // }
    }

    #endregion

    #region Dog Catch Player - UPDATED WITH OFFSET

    /// <summary>
    /// Dog catches player (when double hit slow obstacle)
    /// </summary>
    public void CatchPlayer()
    {
        //Debug.Log("[DogChase] ⚠ CatchPlayer() deprecated - use CatchPlayerParallel()");
        CatchPlayerParallel();
    }

    /// <summary>
    /// Dog catch player (parallel execution - visual decoration only)
    /// UPDATED: With position offset
    /// </summary>
    public void CatchPlayerParallel()
    {
        // Debug.Log("[DogChase] ═══════════════════════════════");
        // Debug.Log("[DogChase] 🐕 DOG CATCH (PARALLEL MODE)");
        // Debug.Log("[DogChase] Visual only - does not block game over");
        // Debug.Log("[DogChase] ═══════════════════════════════");

        if (_temporaryChaseCoroutine != null)
        {
            StopCoroutine(_temporaryChaseCoroutine);
            _temporaryChaseCoroutine = null;
        }

        StartCoroutine(DogCatchSequenceParallel());
    }

    /// <summary>
    /// Dog catch sequence (parallel) - UPDATED: Idle animation on stop
    /// </summary>
    private IEnumerator DogCatchSequenceParallel()
    {
        //Debug.Log("[DogChase] Starting parallel catch sequence with offset...");

        // ═══ VALIDATION ═══
        if (_playerTransform == null || dogPrefab == null)
        {
            //Debug.LogWarning("[DogChase] Cannot catch - missing references");
            yield break;
        }

        // ═══ CLEANUP EXISTING DOG ═══
        if (_dogInstance != null)
        {
            Destroy(_dogInstance);
            _dogInstance = null;
            _dogAnimator = null;
        }

        yield return new WaitForSeconds(0.1f);

        // ═══ SPAWN DOG BEHIND PLAYER ═══
        Vector3 spawnPos = _playerTransform.position + Vector3.back * deathCatchSpawnDistance;
        spawnPos.y = dogGroundY;

        _dogInstance = Instantiate(dogPrefab, spawnPos, Quaternion.identity);
        _dogInstance.name = "ChasingDog_Catching";

        if (_dogInstance == null)
        {
            //Debug.LogWarning("[DogChase] Failed to spawn dog");
            yield break;
        }

        // ═══ GET ANIMATOR ═══
        _dogAnimator = _dogInstance.GetComponent<Animator>();

        // if (_dogAnimator == null)
        // {
        //     Debug.LogWarning("[DogChase] Dog has no Animator component!");
        // }

        // ═══ FACE PLAYER ═══
        Vector3 directionToPlayer = (_playerTransform.position - _dogInstance.transform.position).normalized;
        directionToPlayer.y = 0;
        
        if (directionToPlayer != Vector3.zero)
        {
            _dogInstance.transform.rotation = Quaternion.LookRotation(directionToPlayer);
            
            // if (showDebugLogs)
            // {
            //     Debug.Log($"[DogChase] Dog spawned at: {spawnPos} (Y={dogGroundY})");
            //     Debug.Log($"[DogChase] Player at: {_playerTransform.position}");
            //     Debug.Log($"[DogChase] Direction: {directionToPlayer}");
            // }
        }

        // ═══ START RUNNING ANIMATION ═══
        if (_dogAnimator != null)
        {
            _dogAnimator.SetBool(PARAM_IS_RUNNING, true);
            
            // if (showDebugLogs)
            // {
            //     Debug.Log("[DogChase] ✓ Dog animation: RUNNING");
            // }
        }

        //Debug.Log($"[DogChase] Dog running to player (will stop at {deathCatchStopDistance}m with offset {deathCatchPositionOffset})...");

        // ═══ CALCULATE TARGET POSITION WITH OFFSET ═══
        Vector3 targetPosition = CalculateDeathCatchTargetPosition();

        // if (showDebugLogs)
        // {
        //     Debug.Log($"[DogChase] Target position: {targetPosition}");
        //     Debug.Log($"[DogChase] Distance from player: {Vector3.Distance(targetPosition, _playerTransform.position):F2}m");
        // }

        // ═══ RUN TO TARGET POSITION ═══
        float elapsed = 0f;
        bool reached = false;

        while (elapsed < deathCatchMaxDuration && !reached && _dogInstance != null && _playerTransform != null)
        {
            // Recalculate target (player might be moving/dying)
            targetPosition = CalculateDeathCatchTargetPosition();

            float currentDistance = Vector3.Distance(_dogInstance.transform.position, targetPosition);

            // Check if reached (within 0.5m tolerance)
            if (currentDistance <= 0.5f)
            {
                reached = true;
                break;
            }

            // Calculate direction (ignore Y)
            Vector3 direction = (targetPosition - _dogInstance.transform.position).normalized;
            direction.y = 0;

            // Move dog
            Vector3 newPos = _dogInstance.transform.position + direction * deathCatchRunSpeed * Time.deltaTime;
            newPos.y = dogGroundY;
            _dogInstance.transform.position = newPos;

            // Update rotation to face target
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                _dogInstance.transform.rotation = Quaternion.Slerp(
                    _dogInstance.transform.rotation,
                    targetRotation,
                    10f * Time.deltaTime
                );
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // ═══ FINAL POSITION SNAP ═══
        if (_dogInstance != null && _playerTransform != null)
        {
            targetPosition = CalculateDeathCatchTargetPosition();
            Vector3 finalPos = targetPosition;
            finalPos.y = dogGroundY;
            _dogInstance.transform.position = finalPos;

            // Face player (look at player)
            Vector3 finalDirection = (_playerTransform.position - _dogInstance.transform.position).normalized;
            finalDirection.y = 0;
            if (finalDirection != Vector3.zero)
            {
                _dogInstance.transform.rotation = Quaternion.LookRotation(finalDirection);
            }
        }

        // ═══════════════════════════════════════════════════════
        // NEW: SMOOTH TRANSITION TO IDLE
        // ═══════════════════════════════════════════════════════

        if (_dogAnimator != null)
        {
            // Debug.Log("[DogChase] ═══ DOG REACHED TARGET ═══");
            // Debug.Log("[DogChase] Transitioning to IDLE animation...");

            // ═══ STEP 1: Stop running (set bool to false) ═══
            _dogAnimator.SetBool(PARAM_IS_RUNNING, false);

            // Small delay for animation blend
            yield return new WaitForSeconds(0.1f);

            // ═══ STEP 2: Trigger idle animation ═══
            _dogAnimator.SetTrigger(TRIGGER_IDLE);

            // if (showDebugLogs)
            // {
            //     Debug.Log("[DogChase] ✓ Dog animation: IDLE");
                
            //     // Debug animator state
            //     AnimatorStateInfo currentState = _dogAnimator.GetCurrentAnimatorStateInfo(0);
            //     Debug.Log($"[DogChase] Animator state: {currentState.shortNameHash}");
            //     Debug.Log($"[DogChase] IsRunning param: {_dogAnimator.GetBool(PARAM_IS_RUNNING)}");
            // }

            // ═══ OPTIONAL: Play bark or growl sound ═══
            PlayBark();

            // ═══ OPTIONAL: Small wait to ensure animation started ═══
            yield return new WaitForSeconds(0.2f);
        }
        // else
        // {
        //     Debug.LogWarning("[DogChase] Cannot set idle - Animator is null!");
        // }

        // Debug.Log($"[DogChase] ✓ Dog catch complete (parallel) - reached: {reached}, time: {elapsed:F1}s");
        // Debug.Log($"[DogChase] Final distance from player: {Vector3.Distance(_dogInstance.transform.position, _playerTransform.position):F2}m");
        // Debug.Log($"[DogChase] Dog is now IDLE at position");

        _isChasing = false;
        _isActive = false;
        _isTemporaryChase = false;
    }

    /// <summary>
    /// Calculate target position for death catch with offset - NEW
    /// </summary>
    private Vector3 CalculateDeathCatchTargetPosition()
    {
        if (_playerTransform == null)
        {
            //Debug.LogError("[DogChase] Cannot calculate target - player missing!");
            return Vector3.zero;
        }

        // ═══ BASE POSITION: Behind player at stop distance ═══
        Vector3 basePosition = _playerTransform.position - _playerTransform.forward * deathCatchStopDistance;

        // ═══ APPLY OFFSET (in player's local space) ═══
        Vector3 offsetWorldSpace = _playerTransform.TransformDirection(deathCatchPositionOffset);
        Vector3 targetPosition = basePosition + offsetWorldSpace;

        // Keep ground Y
        targetPosition.y = dogGroundY;

        return targetPosition;
    }

    #endregion

    #region Obstacle Hit

    public void OnPlayerHitObstacle()
    {
        if (_isChasing && !_isTemporaryChase)
        {
            _currentDogDistance = Mathf.Max(0, _currentDogDistance - chaseAccelerationOnHit);
            _safeTimer = 0f;

            // if (showDebugLogs)
            // {
            //     Debug.Log($"[DogChase] 🐕 Dog speeds up! Distance: {_currentDogDistance:F1}m");
            // }
            return;
        }

        StartTemporaryChase();
    }
    
    #endregion

    #region Update Logic
    
    private void UpdateDogPosition()
    {
        if (_dogInstance == null || _playerTransform == null)
            return;

        if (_dogAnimator != null && !_dogAnimator.GetBool(PARAM_IS_RUNNING))
        {
            _dogAnimator.SetBool(PARAM_IS_RUNNING, true);
            //Debug.LogWarning("[DogChase] ⚠ Re-enabled running animation!");
        }

        Vector3 targetPos = _playerTransform.position + (Vector3.back * _currentDogDistance);
        targetPos.y = dogGroundY;

        _dogInstance.transform.position = Vector3.Lerp(
            _dogInstance.transform.position,
            targetPos,
            dogChaseSpeed * Time.deltaTime
        );

        Vector3 directionToPlayer = (_playerTransform.position - _dogInstance.transform.position).normalized;
        directionToPlayer.y = 0;
        if (directionToPlayer != Vector3.zero)
        {
            _dogInstance.transform.rotation = Quaternion.LookRotation(directionToPlayer);
        }

        if (_safeTimer >= safeDuration)
        {
            _currentDogDistance += dogReturnSpeed * Time.deltaTime;

            if (_currentDogDistance >= dogMaxDistance)
            {
                StopChase();
            }
        }
        else
        {
            _currentDogDistance -= dogChaseSpeed * Time.deltaTime * 0.1f;
            _currentDogDistance = Mathf.Max(2f, _currentDogDistance);
        }

        if (_currentDogDistance <= 1f)
        {
            OnDogCatchesPlayer();
        }
    }

    private void UpdateSafeTimer()
    {
        _safeTimer += Time.deltaTime;

        // if (showDebugLogs && Mathf.FloorToInt(_safeTimer) % 1 == 0)
        // {
        //     Debug.Log($"[DogChase] Safe time: {_safeTimer:F1}s / {safeDuration}s");
        // }
    }

    private void OnDogCatchesPlayer()
    {
        // if (showDebugLogs)
        // {
        //     Debug.Log("[DogChase] 🐕 DOG CAUGHT PLAYER!");
        // }

        _isChasing = false;
        _isActive = false;

        if (_dogAnimator != null)
        {
            _dogAnimator.SetBool(PARAM_IS_RUNNING, false);
            _dogAnimator.SetTrigger(TRIGGER_IDLE);
        }

        OnDogCatchPlayer?.Invoke();

        PlayerController player = GameManager.Instance?.GetPlayer();
        if (player != null)
        {
            player.TriggerDeath();
        }
    }
    
    #endregion

    #region Audio/VFX
    
    private void PlayBark()
    {
        if (_dogAnimator != null)
        {
            _dogAnimator.SetTrigger(TRIGGER_BARK);
        }

        if (barkSound != null)
        {
            AudioManager.Instance?.PlaySFX(barkSound);
        }
        else
        {
            AudioManager.Instance?.PlayDogBarkSound();
        }

        // if (showDebugLogs)
        // {
        //     Debug.Log("[DogChase] 🐕 BARK!");
        // }
    }

    #endregion

    #region Intro Integration

    public void TakeDogFromIntro(GameObject dogInstance)
    {
        if (dogInstance == null)
        {
            //Debug.LogError("[DogChase] Cannot take null dog from intro!");
            return;
        }

        //Debug.Log("[DogChase] ═══ TAKING DOG FROM INTRO ═══");

        _dogInstance = dogInstance;
        _dogInstance.name = "ChasingDog_Active";

        _dogAnimator = _dogInstance.GetComponent<Animator>();

        _currentDogDistance = Vector3.Distance(_dogInstance.transform.position, _playerTransform.position);

        _isChasing = true;
        _isActive = true;
        _isTemporaryChase = false;
        _safeTimer = 0f;

        if (_dogAnimator != null)
        {
            _dogAnimator.SetBool(PARAM_IS_RUNNING, true);
            //Debug.Log("[DogChase] ✓ Dog animation: RUNNING (permanent chase)");
        }
        // else
        // {
        //     Debug.LogError("[DogChase] ❌ Dog Animator is NULL!");
        // }

        // Debug.Log($"[DogChase] ✓ Dog taken from intro - PERMANENT chase started");
        // Debug.Log($"[DogChase]   Distance: {_currentDogDistance:F1}m");
        // Debug.Log($"[DogChase]   Animation: {(_dogAnimator != null ? "✓ Ready" : "✗ Missing")}");
    }
    
    #endregion

    #region Public API

    public void PositionDogAtHome(Vector3 position, Quaternion rotation)
    {
        if (_dogInstance == null)
        {
            if (dogPrefab == null)
            {
                //Debug.LogError("[DogChase] Cannot position dog - no prefab!");
                return;
            }
            
            _dogInstance = Instantiate(dogPrefab, position, rotation);
            _dogInstance.name = "ChasingDog_Victory";
            _dogAnimator = _dogInstance.GetComponent<Animator>();
            
            // if (showDebugLogs)
            // {
            //     Debug.Log("[DogChase] 🐕 Dog spawned for victory");
            // }
        }
        else
        {
            _dogInstance.transform.position = position;
            _dogInstance.transform.rotation = rotation;
            
            // if (showDebugLogs)
            // {
            //     Debug.Log("[DogChase] 🐕 Dog repositioned for victory");
            // }
        }
        
        _isChasing = false;
        _isActive = false;
        _isTemporaryChase = false;
        
        if (_dogAnimator != null)
        {
            _dogAnimator.SetBool(PARAM_IS_RUNNING, false);
            _dogAnimator.SetTrigger(TRIGGER_IDLE);
        }
        
        // if (showDebugLogs)
        // {
        //     Debug.Log($"[DogChase] ✓ Dog at home: {position}");
        // }
    }
    
    public void StopChaseOnDeath()
    {
        // if (showDebugLogs)
        // {
        //     Debug.Log("[DogChase] 💀 Player died - stopping permanent chase");
        // }
        
        _isActive = false;
        _isChasing = false;
        
        if (_dogAnimator != null)
        {
            _dogAnimator.SetBool(PARAM_IS_RUNNING, false);
        }
        
        // if (showDebugLogs)
        // {
        //     Debug.Log("[DogChase] ✓ Chase stopped - dog frozen in place");
        // }
    }
    
    public void SetDogIdle()
    {
        _isChasing = false;
        _isActive = false;
        _isTemporaryChase = false;

        if (_dogAnimator != null)
        {
            _dogAnimator.SetBool(PARAM_IS_RUNNING, false);
            _dogAnimator.SetTrigger(TRIGGER_IDLE);
        }

        // if (showDebugLogs)
        // {
        //     Debug.Log("[DogChase] 🐕 Dog set to idle");
        // }
    }

    public Vector3 GetDogPosition()
    {
        return _dogInstance != null ? _dogInstance.transform.position : Vector3.zero;
    }
    
    #endregion

    #region Event Handlers
    
    private void OnGameStarted()
    {
        // if (showDebugLogs)
        // {
        //     Debug.Log("[DogChase] Game started - waiting for trigger");
        // }
    }

    private void OnGameOver()
    {
        SetDogIdle();
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

        RemoveDog();
    }
    
    #endregion

    #region Debug
    
    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        //if (!showDebugLogs || _playerTransform == null) return;

        if (_dogInstance != null)
        {
            Gizmos.color = _isTemporaryChase ? Color.yellow : Color.red;
            Gizmos.DrawWireSphere(_dogInstance.transform.position, 1f);
            
            Gizmos.DrawLine(_dogInstance.transform.position, _playerTransform.position);
            
            string modeText = _isTemporaryChase ? "TEMPORARY (3s)" : "PERMANENT";
            
            UnityEditor.Handles.Label(
                _dogInstance.transform.position + Vector3.up * 2f,
                $"🐕 DOG - {modeText}\nDist: {_currentDogDistance:F1}m\nSafe: {_safeTimer:F1}s"
            );
        }

        // ═══ NEW: Visualize death catch target position ═══
        if (_playerTransform != null && Application.isPlaying)
        {
            Vector3 targetPos = CalculateDeathCatchTargetPosition();
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetPos, 0.5f);
            Gizmos.DrawLine(_playerTransform.position, targetPos);
            
            UnityEditor.Handles.Label(
                targetPos + Vector3.up * 1.5f,
                $"🎯 DEATH CATCH\nTARGET\n({deathCatchStopDistance}m + offset)"
            );
        }

        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Vector3 maxPos = _playerTransform.position - _playerTransform.forward * dogMaxDistance;
        Gizmos.DrawWireSphere(maxPos, 2f);
    }
    #endif
    
    #endregion
}