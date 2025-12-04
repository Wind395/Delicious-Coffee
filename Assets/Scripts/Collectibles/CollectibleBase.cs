using UnityEngine;

/// <summary>
/// Base class for all collectible objects (Coin, PowerUp)
/// SOLID: Single Responsibility - Handle collection logic only
/// Design Pattern: Template Method
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class CollectibleBase : MonoBehaviour, ICollectible, IPoolable
{
    #region Serialized Fields
    
    [Header("Visual")]
    [SerializeField] protected float rotationSpeed = 100f;
    [SerializeField] protected float bobSpeed = 2f;
    [SerializeField] protected float bobHeight = 0.3f;
    
    [Header("Auto Despawn")]
    [SerializeField] protected float autoDespawnDistance = 25f;
    
    #endregion

    #region State - KISS: Minimal state
    
    protected CollectibleState _state = CollectibleState.Inactive;
    protected Transform _playerTransform;
    protected Collider _collider;
    protected float _spawnY;
    protected float _bobTimer;
    
    #endregion

    #region Properties
    
    public GameObject GameObject => gameObject;
    public CollectibleState GetState() => _state;
    
    #endregion

    #region Unity Lifecycle
    
    protected virtual void Awake()
    {
        _collider = GetComponent<Collider>();
        
        // if (_collider == null)
        // {
        //     Debug.LogError($"[Collectible] {name} missing Collider!");
        // }
    }

    protected virtual void Update()
    {
        if (_state != CollectibleState.Active) return;
        
        UpdateVisuals();
        CheckAutoDespawn();
    }
    
    #endregion

    #region IPoolable Implementation
    
    /// <summary>
    /// Called when spawned from pool
    /// SOLID: Open/Closed - Override for custom behavior
    /// </summary>
    public virtual void OnSpawn()
    {
        // Reset state
        _state = CollectibleState.Active;
        _bobTimer = Random.Range(0f, Mathf.PI * 2f);
        _spawnY = transform.position.y;
        
        // Enable collider
        if (_collider != null)
        {
            _collider.enabled = true;
        }
        
        // Find player if needed
        if (_playerTransform == null)
        {
            FindPlayer();
        }
        
        OnSpawnCustom();
    }

    /// <summary>
    /// Called when returned to pool
    /// </summary>
    public virtual void OnDespawn()
    {
        _state = CollectibleState.Inactive;
        
        // Disable collider immediately
        if (_collider != null)
        {
            _collider.enabled = false;
        }
        
        OnDespawnCustom();
    }
    
    #endregion

    #region ICollectible Implementation
    
    /// <summary>
    /// Check if can be collected
    /// KISS: Simple validation
    /// </summary>
    public virtual bool CanCollect()
    {
        return _state == CollectibleState.Active && 
               _collider != null && 
               _collider.enabled;
    }

    /// <summary>
    /// Collect this item
    /// Template Method Pattern - Subclasses implement OnCollected()
    /// </summary>
    public void Collect(PlayerController player)
    {
        // CRITICAL: Validate first
        if (!CanCollect())
        {
            //Debug.LogWarning($"[Collectible] {name} cannot be collected! State: {_state}");
            return;
        }
        
        // CRITICAL: Set state IMMEDIATELY to prevent double collection
        _state = CollectibleState.Collected;
        
        // Disable collider immediately
        if (_collider != null)
        {
            _collider.enabled = false;
        }
        
        // Call subclass implementation
        OnCollected(player);
        
        // Return to pool after a short delay (safety)
        Invoke(nameof(ReturnToPoolSafe), 0.1f);
    }
    
    #endregion

    #region Visuals - KISS: Simple animation
    
    /// <summary>
    /// Update visual effects (rotation, bobbing)
    /// </summary>
    protected virtual void UpdateVisuals()
    {
        // Rotation
        transform.Rotate(transform.eulerAngles, rotationSpeed * Time.deltaTime, Space.Self);
        
        // Bobbing
        // _bobTimer += Time.deltaTime * bobSpeed;
        // Vector3 pos = transform.position;
        // pos.y = _spawnY + Mathf.Sin(_bobTimer) * bobHeight;
        // transform.position = pos;
    }
    
    #endregion

    #region Auto Despawn - FIXED
    
    /// <summary>
    /// Check if should auto despawn
    /// FIX: Only despawn if FAR BEHIND player, not ahead
    /// </summary>
    protected virtual void CheckAutoDespawn()
    {
        if (_playerTransform == null)
        {
            FindPlayer();
            return;
        }
        
        // CRITICAL: Only despawn if BEHIND player
        float distanceBehindPlayer = _playerTransform.position.z - transform.position.z;
        
        if (distanceBehindPlayer > autoDespawnDistance)
        {
            //Debug.Log($"[Collectible] {name} auto despawned (behind player by {distanceBehindPlayer:F1}m)");
            ReturnToPoolSafe();
        }
    }
    
    #endregion

    #region Pool Management - SAFE
    
    /// <summary>
    /// Safe return to pool - Validates state first
    /// </summary>
    protected void ReturnToPoolSafe()
    {
        // Don't return if already inactive
        if (_state == CollectibleState.Inactive) return;
        
        // Set state
        _state = CollectibleState.Inactive;
        
        // Disable immediately
        if (_collider != null)
        {
            _collider.enabled = false;
        }
        
        gameObject.SetActive(false);
    }
    
    #endregion

    #region Collision Detection - ROBUST
    
    /// <summary>
    /// Trigger collision - FIXED: Proper validation
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        // Validate state first
        if (!CanCollect())
        {
            return;
        }
        
        // Check player tag
        if (!other.CompareTag("Player"))
        {
            return;
        }
        
        // Get player component
        PlayerController player = other.GetComponent<PlayerController>();
        
        if (player == null)
        {
            //Debug.LogError($"[Collectible] Player collider missing PlayerController!");
            return;
        }
        
        // Collect
        Collect(player);
    }
    
    #endregion

    #region Helper Methods
    
    /// <summary>
    /// Find player in scene
    /// </summary>
    protected void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
        }
        // else
        // {
        //     Debug.LogWarning("[Collectible] Player not found!");
        // }
    }
    
    #endregion

    #region Abstract/Virtual Methods - SOLID: Open/Closed
    
    /// <summary>
    /// Called when collected - Implement in subclass
    /// </summary>
    protected abstract void OnCollected(PlayerController player);
    
    /// <summary>
    /// Custom spawn logic - Override if needed
    /// </summary>
    protected virtual void OnSpawnCustom() { }
    
    /// <summary>
    /// Custom despawn logic - Override if needed
    /// </summary>
    protected virtual void OnDespawnCustom() { }
    
    #endregion

    #region Debug
    
    #if UNITY_EDITOR
    protected virtual void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // Show state with color
        Gizmos.color = _state switch
        {
            CollectibleState.Active => Color.green,
            CollectibleState.Collected => Color.yellow,
            CollectibleState.Inactive => Color.red,
            _ => Color.white
        };
        
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
    #endif
    
    #endregion
}