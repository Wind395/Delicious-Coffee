using UnityEngine;

/// <summary>
/// Human Obstacle - Dynamic movement based on lane and player proximity
/// BEHAVIOR:
/// - Lane 0 (Left): Move right
/// - Lane 1 (Center): Move toward player
/// - Lane 2 (Right): Move left
/// - Only moves when player is within detection range
/// </summary>
[RequireComponent(typeof(Obstacle))]
public class HumanObstacle : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Detection")]
    [Tooltip("Distance to detect player and start moving")]
    [SerializeField] private float detectionRange = 10f;
    
    [Tooltip("Stop moving when player is this close")]
    [SerializeField] private float stopDistance = 1f;
    
    [Header("Movement")]
    [Tooltip("Movement speed")]
    [SerializeField] private float moveSpeed = 3f;
    
    [Tooltip("Rotation speed when turning")]
    [SerializeField] private float rotationSpeed = 5f;
    
    [Header("Lane Settings")]
    [Tooltip("Lane distance (should match game settings)")]
    [SerializeField] private float laneDistance = 3f;
    
    [Tooltip("Max distance to move laterally")]
    [SerializeField] private float maxLateralDistance = 6f;
    
    [Header("Animation")]
    [Tooltip("Animator component (optional)")]
    [SerializeField] private Animator animator;
    
    [Tooltip("Animation parameter names")]
    [SerializeField] private string walkAnimParam = "IsWalking";
    [SerializeField] private string speedAnimParam = "Speed";
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    [SerializeField] private bool showGizmos = true;
    
    #endregion

    #region State
    
    private Transform _playerTransform;
    private Obstacle _obstacleComponent;
    
    private int _spawnLane = 1; // 0=Left, 1=Center, 2=Right
    private Vector3 _spawnPosition;
    private Vector3 _initialPosition;
    
    private bool _isMoving = false;
    private bool _hasDetectedPlayer = false;
    private bool _isInitialized = false;
    
    // Movement direction based on lane
    private Vector3 _moveDirection;
    
    // Animation hashes
    private int _walkParamHash;
    private int _speedParamHash;
    
    #endregion

    #region Unity Lifecycle
    
    void Awake()
    {
        _obstacleComponent = GetComponent<Obstacle>();
        
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        
        // Cache animation parameter hashes
        if (animator != null)
        {
            _walkParamHash = Animator.StringToHash(walkAnimParam);
            _speedParamHash = Animator.StringToHash(speedAnimParam);
        }
    }

    void Start()
    {
        Initialize();
    }

    void Update()
    {
        if (!_isInitialized || _playerTransform == null)
        {
            return;
        }

        UpdatePlayerDetection();
        UpdateMovement();
    }
    
    #endregion

    #region Initialization
    
    /// <summary>
    /// Initialize human obstacle
    /// </summary>
    private void Initialize()
    {
        // Find player
        FindPlayer();
        
        // Store spawn data
        _spawnPosition = transform.position;
        _initialPosition = transform.position;
        
        // Determine spawn lane
        DetermineSpawnLane();
        
        // Calculate movement direction
        CalculateMovementDirection();
        
        _isInitialized = true;
        
        if (showDebugLogs)
        {
            Debug.Log($"[HumanObstacle] Initialized at lane {_spawnLane}");
            Debug.Log($"[HumanObstacle] Position: {transform.position}");
            Debug.Log($"[HumanObstacle] Move direction: {_moveDirection}");
        }
    }

    /// <summary>
    /// Find player transform
    /// </summary>
    private void FindPlayer()
    {
        if (_playerTransform != null)
        {
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        if (player != null)
        {
            _playerTransform = player.transform;
            
            if (showDebugLogs)
            {
                Debug.Log("[HumanObstacle] ✓ Player found");
            }
        }
        else
        {
            Debug.LogWarning("[HumanObstacle] ⚠ Player not found!");
        }
    }

    /// <summary>
    /// Determine which lane this human spawned in
    /// </summary>
    private void DetermineSpawnLane()
    {
        float xPos = transform.position.x;
        
        // Lane 0 (Left): x ≈ -3
        // Lane 1 (Center): x ≈ 0
        // Lane 2 (Right): x ≈ +3
        
        if (xPos < -laneDistance)
        {
            _spawnLane = 0; // Left
        }
        else if (xPos > laneDistance)
        {
            _spawnLane = 2; // Right
        }
        else
        {
            _spawnLane = 1; // Center
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[HumanObstacle] Spawn lane: {_spawnLane} (x={xPos:F2})");
        }
    }

    /// <summary>
    /// Calculate movement direction based on spawn lane
    /// </summary>
    private void CalculateMovementDirection()
    {
        switch (_spawnLane)
        {
            case 0: // Left lane → Move RIGHT
                _moveDirection = Vector3.right;
                
                // Face right
                transform.rotation = Quaternion.Euler(0, 90, 0);
                
                if (showDebugLogs)
                {
                    Debug.Log("[HumanObstacle] Lane 0 (Left) → Moving RIGHT");
                }
                break;

            case 1: // Center lane → Move TOWARD PLAYER
                _moveDirection = Vector3.forward; // Will update dynamically
                
                // Face forward initially
                transform.rotation = Quaternion.Euler(0, 0, 0);
                
                if (showDebugLogs)
                {
                    Debug.Log("[HumanObstacle] Lane 1 (Center) → Moving TOWARD PLAYER");
                }
                break;

            case 2: // Right lane → Move LEFT
                _moveDirection = Vector3.left;
                
                // Face left
                transform.rotation = Quaternion.Euler(0, -90, 0);
                
                if (showDebugLogs)
                {
                    Debug.Log("[HumanObstacle] Lane 2 (Right) → Moving LEFT");
                }
                break;
        }
    }
    
    #endregion

    #region Detection
    
    /// <summary>
    /// Update player detection and trigger movement
    /// </summary>
    private void UpdatePlayerDetection()
    {
        if (_playerTransform == null)
        {
            FindPlayer();
            return;
        }

        // Calculate distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
        
        // Check if player is within detection range
        if (!_hasDetectedPlayer && distanceToPlayer <= detectionRange)
        {
            OnPlayerDetected();
        }
        
        // Check if player is too close → stop moving
        if (_isMoving && distanceToPlayer <= stopDistance)
        {
            StopMoving();
        }
    }

    /// <summary>
    /// Called when player enters detection range
    /// </summary>
    private void OnPlayerDetected()
    {
        _hasDetectedPlayer = true;
        
        if (showDebugLogs)
        {
            Debug.Log($"[HumanObstacle] 👁️ Player detected! Distance: {Vector3.Distance(transform.position, _playerTransform.position):F1}m");
        }
        
        StartMoving();
    }
    
    #endregion

    #region Movement
    
    /// <summary>
    /// Start moving
    /// </summary>
    private void StartMoving()
    {
        if (_isMoving)
        {
            return;
        }

        _isMoving = true;
        
        // Set animation
        if (animator != null)
        {
            animator.SetBool(_walkParamHash, true);
            animator.SetFloat(_speedParamHash, 1f);
            
            if (showDebugLogs)
            {
                Debug.Log("[HumanObstacle] ✓ Animation: Walking");
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log("[HumanObstacle] 🚶 Started moving");
        }
    }

    /// <summary>
    /// Stop moving
    /// </summary>
    private void StopMoving()
    {
        if (!_isMoving)
        {
            return;
        }

        _isMoving = false;
        
        // Set animation
        if (animator != null)
        {
            animator.SetBool(_walkParamHash, false);
            animator.SetFloat(_speedParamHash, 0f);
            
            if (showDebugLogs)
            {
                Debug.Log("[HumanObstacle] ✓ Animation: Idle");
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log("[HumanObstacle] 🛑 Stopped moving");
        }
    }

    /// <summary>
    /// Update movement logic
    /// </summary>
    private void UpdateMovement()
    {
        if (!_isMoving)
        {
            return;
        }

        // Calculate move direction based on lane
        Vector3 moveDir = CalculateCurrentMoveDirection();
        
        // Move
        transform.position += moveDir * moveSpeed * Time.deltaTime;
        
        // Rotate to face movement direction
        if (moveDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
        
        // Check if moved too far (safety)
        CheckMovementBounds();
    }

    /// <summary>
    /// Calculate current movement direction
    /// </summary>
    private Vector3 CalculateCurrentMoveDirection()
    {
        switch (_spawnLane)
        {
            case 0: // Left → Right
                return Vector3.right;

            case 1: // Center → Toward player
                // if (_playerTransform != null)
                // {
                //     Vector3 dirToPlayer = (_playerTransform.position - transform.position).normalized;
                //     dirToPlayer.y = 0; // Keep horizontal
                //     return dirToPlayer;
                // }
                return Vector3.back;

            case 2: // Right → Left
                return Vector3.left;

            default:
                return Vector3.zero;
        }
    }

    /// <summary>
    /// Check if human moved too far from spawn position
    /// </summary>
    private void CheckMovementBounds()
    {
        float distanceFromSpawn = Vector3.Distance(transform.position, _initialPosition);
        
        if (distanceFromSpawn > maxLateralDistance)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[HumanObstacle] ⚠ Moved too far ({distanceFromSpawn:F1}m) - stopping");
            }
            
            StopMoving();
        }
    }
    
    #endregion

    #region Public API
    
    /// <summary>
    /// Reset human obstacle (for pooling)
    /// </summary>
    public void ResetHuman()
    {
        _isMoving = false;
        _hasDetectedPlayer = false;
        _isInitialized = false;
        
        if (animator != null)
        {
            animator.SetBool(_walkParamHash, false);
            animator.SetFloat(_speedParamHash, 0f);
        }
        
        if (showDebugLogs)
        {
            Debug.Log("[HumanObstacle] ✓ Reset");
        }
    }

    /// <summary>
    /// Force start moving (for testing)
    /// </summary>
    [ContextMenu("Force Start Moving")]
    public void ForceStartMoving()
    {
        if (!_isInitialized)
        {
            Initialize();
        }
        
        OnPlayerDetected();
    }
    
    #endregion

    #region Gizmos
    
    #if UNITY_EDITOR
    
    void OnDrawGizmos()
    {
        if (!showGizmos)
        {
            return;
        }

        // Draw detection range
        Gizmos.color = _hasDetectedPlayer ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Draw stop distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
        
        // Draw movement direction
        if (_isMoving || Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Vector3 moveDir = CalculateCurrentMoveDirection();
            Gizmos.DrawRay(transform.position, moveDir * 2f);
        }
        
        // Draw max movement bounds
        if (Application.isPlaying)
        {
            Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
            Gizmos.DrawWireSphere(_initialPosition, maxLateralDistance);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos)
        {
            return;
        }

        // Draw labels
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2f,
            $"🚶 HUMAN OBSTACLE\n" +
            $"Lane: {_spawnLane}\n" +
            $"Moving: {(_isMoving ? "✓" : "✗")}\n" +
            $"Detected: {(_hasDetectedPlayer ? "✓" : "✗")}"
        );
        
        // Draw spawn position
        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(_initialPosition, Vector3.one * 0.5f);
            
            UnityEditor.Handles.Label(
                _initialPosition + Vector3.up * 1f,
                "Spawn Point"
            );
        }
    }
    
    #endif
    
    #endregion
}