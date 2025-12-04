using UnityEngine;

/// <summary>
/// Obstacle - FIXED: Serialize database reference
/// </summary>
public class Obstacle : MonoBehaviour, IPoolable
{
    [Header("Obstacle Type")]
    [SerializeField] private ObstacleType obstacleType = ObstacleType.GenericBarrier;

    [Header("Behavior Override (Optional)")]
    [SerializeField] private bool overrideBehavior = false;
    [SerializeField] private ObstacleBehavior customBehavior = ObstacleBehavior.Deadly;

    [Header("Slow Settings (if Slow behavior)")]
    [SerializeField] private float slowMultiplier = 0.5f;
    [SerializeField] private float slowDuration = 2f;

    [Header("Visual")]
    public Color obstacleColor = Color.red;

    [Header("Auto Destroy")]
    public float autoDestroyDistance = 20f;

    // ═══ FIX: Serialize database reference instead of Resources.Load ═══
    [Header("Database - REQUIRED")]
    [Tooltip("Assign ObstacleTypeDatabase ScriptableObject here")]
    [SerializeField] private ObstacleTypeDatabase typeDatabase;

    private Transform playerTransform;
    private Renderer obstacleRenderer;
    private JSONSectionSpawner _spawner;
    private bool isDestroyed;
    private bool hasCollided = false;

    #region IPoolable Implementation

    public GameObject GameObject => gameObject;

    public void OnSpawn()
    {
        isDestroyed = false;
        hasCollided = false;

        FindPlayerReference();
        SetupRenderer();

        // ═══ FIX: Validate database on spawn ═══
        //ValidateDatabase();
    }

    public void OnDespawn()
    {
        isDestroyed = false;
        hasCollided = false;
    }

    #endregion

    #region Initialization

    public void Initialize(JSONSectionSpawner spawner)
    {
        _spawner = spawner;
        hasCollided = false;
        FindPlayerReference();

        // ═══ FIX: Validate database ═══
        //ValidateDatabase();
    }

    private void FindPlayerReference()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
    }

    private void SetupRenderer()
    {
        if (obstacleRenderer == null)
        {
            obstacleRenderer = GetComponent<Renderer>();
        }

        if (obstacleRenderer != null)
        {
            obstacleRenderer.enabled = true;
            if (obstacleRenderer.material != null)
            {
                obstacleRenderer.material.color = obstacleColor;
            }
        }
    }

    /// <summary>
    /// Validate database - CRITICAL for Android builds
    /// </summary>
    // private void ValidateDatabase()
    // {
    //     if (typeDatabase == null)
    //     {
    //         Debug.LogError($"[Obstacle] ❌ CRITICAL: ObstacleTypeDatabase is NULL on {gameObject.name}!");
    //         Debug.LogError("[Obstacle] → Assign database in Inspector or prefab!");
    //         Debug.LogError("[Obstacle] → All obstacles will default to DEADLY behavior!");
    //     }
    //     else
    //     {
    //         if (showDebugLogs)
    //         {
    //             Debug.Log($"[Obstacle] ✓ Database validated on {gameObject.name}");
    //         }
    //     }
    // }

    #endregion

    #region Update

    void Update()
    {
        if (isDestroyed) return;

        if (playerTransform == null)
        {
            FindPlayerReference();
            return;
        }

        float distance = transform.position.z - playerTransform.position.z;

        if (distance < -autoDestroyDistance)
        {
            ReturnToPool();
        }
    }

    #endregion

    #region Collision - UPDATED with better logging

    void OnTriggerEnter(Collider other)
    {
        // if (showDebugLogs)
        // {
        //     Debug.Log($"[Obstacle] OnTriggerEnter - {other.name}, Tag: {other.tag}, Type: {obstacleType}");
        // }

        if (hasCollided || isDestroyed)
        {
            return;
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        HandlePlayerCollision(other.gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        // if (showDebugLogs)
        // {
        //     Debug.Log($"[Obstacle] OnCollisionEnter - {collision.gameObject.name}, Tag: {collision.gameObject.tag}, Type: {obstacleType}");
        // }

        if (hasCollided || isDestroyed)
        {
            return;
        }

        if (!collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        HandlePlayerCollision(collision.gameObject);
    }

    /// <summary>
    /// Handle collision with player - UPDATED with extensive logging
    /// </summary>
    private void HandlePlayerCollision(GameObject playerObj)
    {
        // Debug.Log($"[Obstacle] ════════════════════════════════════════");
        // Debug.Log($"[Obstacle] COLLISION DETECTED");
        // Debug.Log($"[Obstacle] Name: {gameObject.name}");
        // Debug.Log($"[Obstacle] ObstacleType: {obstacleType}");
        // Debug.Log($"[Obstacle] Database: {(typeDatabase != null ? "✓ EXISTS" : "❌ NULL")}");
        // Debug.Log($"[Obstacle] Override: {overrideBehavior}");

        if (hasCollided)
        {
            //Debug.LogWarning($"[Obstacle] Already collided, returning");
            return;
        }

        hasCollided = true;

        PlayerController player = playerObj.GetComponent<PlayerController>();

        if (player == null)
        {
            //Debug.LogError("[Obstacle] PlayerController component not found!");
            hasCollided = false;
            return;
        }

        if (!player.IsAlive)
        {
            //Debug.Log($"[Obstacle] Player is dead, ignoring");
            return;
        }

        // 1. Check SHIELD (Medicine)
        if (player.HasShield)
        {
            //Debug.Log($"[Obstacle] 🛡️ Shield blocked!");
            player.OnShieldHitObstacle(gameObject);
            isDestroyed = true;
            return;
        }

        // 2. Check ICE TEA INVINCIBILITY
        bool hasIceTeaInvincibility = PowerUpManager.Instance != null &&
                                    PowerUpManager.Instance.IsPowerUpActive<IceTeaPowerUp>();

        if (hasIceTeaInvincibility)
        {
            //Debug.Log($"[Obstacle] 🧊 Ice Tea invincible!");
            hasCollided = false;
            return;
        }

        // 3. Get behavior - CRITICAL
        ObstacleBehavior behavior = GetObstacleBehavior();

        if (behavior == ObstacleBehavior.Deadly)
        {
            // ✅ CORRECT: This will determine specific death reason
            IObstacleBehaviorHandler handler = new DeadlyBehaviorHandler();
            handler.HandleCollision(player, this);
        }
        else
        {
            // Slow obstacle
            IObstacleBehaviorHandler handler = CreateBehaviorHandler(behavior);
            handler.HandleCollision(player, this);
        }

        //Debug.Log($"[Obstacle] ════════════════════════════════════════");
    }

    #endregion

    #region Strategy Pattern - Factory Method - UPDATED

    /// <summary>
    /// Create behavior handler - UPDATED with logging
    /// </summary>
    private IObstacleBehaviorHandler CreateBehaviorHandler(ObstacleBehavior behavior)
    {
        //Debug.Log($"[Obstacle] CreateBehaviorHandler - Behavior: {behavior}");

        switch (behavior)
        {
            case ObstacleBehavior.Deadly:
                //Debug.Log($"[Obstacle] → Creating DeadlyBehaviorHandler");
                return new DeadlyBehaviorHandler();

            case ObstacleBehavior.Slow:
                float multiplier = GetSlowMultiplier();
                float duration = GetSlowDuration();

                //Debug.Log($"[Obstacle] → Creating SlowBehaviorHandler");
                //Debug.Log($"[Obstacle]    Multiplier: {multiplier}, Duration: {duration}");

                return new SlowBehaviorHandler(multiplier, duration);

            default:
                //Debug.LogWarning($"[Obstacle] Unknown behavior, defaulting to Deadly");
                return new DeadlyBehaviorHandler();
        }
    }

    /// <summary>
    /// Get obstacle behavior - UPDATED: Extensive debugging
    /// </summary>
    private ObstacleBehavior GetObstacleBehavior()
    {
        // Debug.Log($"[Obstacle] ═══════════════════════════════════════");
        // Debug.Log($"[Obstacle] GET BEHAVIOR for: {gameObject.name}");
        // Debug.Log($"[Obstacle] ObstacleType: {obstacleType}");
        // Debug.Log($"[Obstacle] Override: {overrideBehavior}");
        // Debug.Log($"[Obstacle] Database: {(typeDatabase != null ? "EXISTS" : "NULL")}");

        // Priority 1: Override
        if (overrideBehavior)
        {
            //Debug.Log($"[Obstacle] Using override behavior: {customBehavior}");
            return customBehavior;
        }

        // Priority 2: Database
        if (typeDatabase != null)
        {
            ObstacleTypeData typeData = typeDatabase.GetTypeData(obstacleType);
            
            // Debug.Log($"[Obstacle] Database lookup result:");
            // Debug.Log($"[Obstacle]   Type: {typeData.type}");
            // Debug.Log($"[Obstacle]   Behavior: {typeData.behavior}");
            // Debug.Log($"[Obstacle]   Display: {typeData.displayName}");
            // Debug.Log($"[Obstacle]   SlowMult: {typeData.slowMultiplier}");
            // Debug.Log($"[Obstacle]   SlowDur: {typeData.slowDuration}");
            
            // Debug.Log($"[Obstacle] → USING DATABASE BEHAVIOR: {typeData.behavior}");
            // Debug.Log($"[Obstacle] ═══════════════════════════════════════");
            
            return typeData.behavior;
        }

        // Priority 3: Hardcoded fallback
        //Debug.LogError($"[Obstacle] ❌ DATABASE IS NULL! Using hardcoded fallback");
        ObstacleBehavior fallback = GetHardcodedBehavior();
        
        //Debug.Log($"[Obstacle] → FALLBACK BEHAVIOR: {fallback}");
        //Debug.Log($"[Obstacle] ═══════════════════════════════════════");
        
        return fallback;
    }

    /// <summary>
    /// Hardcoded fallback behavior - UPDATED: Added ShoppingCart & Barrier
    /// </summary>
    private ObstacleBehavior GetHardcodedBehavior()
    {
        // Hardcoded mapping as fallback
        switch (obstacleType)
        {
            // ═══ DEADLY ═══
            case ObstacleType.Car:
            case ObstacleType.Motorcycle:
            case ObstacleType.Fence:
            case ObstacleType.Barrier: // ← NEW
            case ObstacleType.GenericBarrier:
            case ObstacleType.GenericLow:
            case ObstacleType.GenericHigh:
                //Debug.Log($"[Obstacle] Hardcoded: {obstacleType} → Deadly");
                return ObstacleBehavior.Deadly;

            // ═══ SLOW ═══
            case ObstacleType.StreetVendor:
            case ObstacleType.ShoppingCart: // ← NEW
            case ObstacleType.TrashCan:
            case ObstacleType.Human:
                //Debug.Log($"[Obstacle] Hardcoded: {obstacleType} → Slow");
                return ObstacleBehavior.Slow;

            default:
                //Debug.LogWarning($"[Obstacle] Unknown type {obstacleType}, defaulting to Deadly");
                return ObstacleBehavior.Deadly;
        }
    }

    /// <summary>
    /// Get slow multiplier with fallback - UPDATED
    /// </summary>
    private float GetSlowMultiplier()
    {
        if (overrideBehavior)
        {
            return slowMultiplier;
        }

        if (typeDatabase != null)
        {
            return typeDatabase.GetTypeData(obstacleType).slowMultiplier;
        }

        // Hardcoded fallbacks
        switch (obstacleType)
        {
            case ObstacleType.StreetVendor: return 0.6f;
            case ObstacleType.ShoppingCart: return 0.6f; // ← NEW: Same as StreetVendor
            case ObstacleType.TrashCan: return 0.7f;
            case ObstacleType.Human: return 0.5f;
            default: return 0.5f;
        }
    }

    /// <summary>
    /// Get slow duration with fallback - UPDATED
    /// </summary>
    private float GetSlowDuration()
    {
        if (overrideBehavior)
        {
            return slowDuration;
        }

        if (typeDatabase != null)
        {
            return typeDatabase.GetTypeData(obstacleType).slowDuration;
        }

        // Hardcoded fallbacks
        switch (obstacleType)
        {
            case ObstacleType.StreetVendor: return 2f;
            case ObstacleType.ShoppingCart: return 2f; // ← NEW: Same as StreetVendor
            case ObstacleType.TrashCan: return 1.5f;
            case ObstacleType.Human: return 2.5f;
            default: return 2f;
        }
    }

    #endregion

    #region Pool Management

    private void ReturnToPool()
    {
        if (isDestroyed) return;

        hasCollided = false;
        gameObject.SetActive(false);
    }

    #endregion

    #region Public API

    public void SetObstacleType(ObstacleType type)
    {
        obstacleType = type;

        // if (showDebugLogs)
        // {
        //     Debug.Log($"[Obstacle] SetObstacleType: {type}");
        // }
    }

    public ObstacleType GetObstacleType()
    {
        return obstacleType;
    }

    /// <summary>
    /// Set database reference - For runtime assignment
    /// </summary>
    public void SetDatabase(ObstacleTypeDatabase database)
    {
        typeDatabase = database;

        // if (showDebugLogs)
        // {
        //     Debug.Log($"[Obstacle] Database assigned: {(database != null ? "✓" : "✗")}");
        // }
    }

    #endregion

//     #region Editor Validation

// #if UNITY_EDITOR

//     void OnValidate()
//     {
//         // Validate database assignment
//         if (typeDatabase == null)
//         {
//             Debug.LogWarning($"[Obstacle] {gameObject.name}: ObstacleTypeDatabase not assigned! Will use hardcoded fallback.", this);
//         }
//     }

// #endif

//     #endregion
}