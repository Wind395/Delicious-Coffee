using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// PowerUp Manager - FIXED: Proper instantiation and lifecycle
/// </summary>
public class PowerUpManager : MonoBehaviour
{
    #region Singleton
    
    private static PowerUpManager _instance;
    public static PowerUpManager Instance => _instance;
    
    #endregion

    #region Serialized Fields
    
    [Header("PowerUp Prefabs")]
    [SerializeField] private GameObject iceTeaPrefab;
    [SerializeField] private GameObject coldTowelPrefab;
    [SerializeField] private GameObject medicinePrefab;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    
    #endregion

    #region State
    
    private Dictionary<System.Type, PowerUpBase> _activePowerUps = new Dictionary<System.Type, PowerUpBase>();
    private PlayerController _player;
    
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
        CheckExpiredPowerUps();
    }
    
    #endregion

    #region Initialization
    
    private void Initialize()
    {
        FindPlayer();
        
        // Validate prefabs
        ValidatePrefabs();
    }

    private void FindPlayer()
    {
        if (GameManager.Instance != null)
        {
            _player = GameManager.Instance.GetPlayer();
        }

        if (_player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                _player = playerObj.GetComponent<PlayerController>();
            }
        }

        if (_player == null)
        {
            Debug.LogError("[PowerUpManager] ❌ Player not found!");
        }
        else
        {
            Debug.Log("[PowerUpManager] ✓ Player found");
        }
    }

    private void ValidatePrefabs()
    {
        if (iceTeaPrefab == null)
            Debug.LogWarning("[PowerUpManager] Ice Tea prefab not assigned!");
        
        if (coldTowelPrefab == null)
            Debug.LogWarning("[PowerUpManager] Cold Towel prefab not assigned!");
        
        if (medicinePrefab == null)
            Debug.LogWarning("[PowerUpManager] Medicine prefab not assigned!");
    }
    
    #endregion

    #region PowerUp Activation - FIXED
    
    /// <summary>
    /// Activate powerup - FIXED: Proper instantiation
    /// </summary>
    public void ActivatePowerUp<T>() where T : PowerUpBase
    {
        if (_player == null)
        {
            Debug.LogError("[PowerUpManager] ❌ Cannot activate - player is null!");
            FindPlayer(); // Try to find again
            
            if (_player == null)
            {
                return;
            }
        }

        System.Type type = typeof(T);
        
        // Check if already active
        if (_activePowerUps.ContainsKey(type))
        {
            PowerUpBase existing = _activePowerUps[type];
            
            if (existing != null && existing.IsActive)
            {
                // Refresh existing
                existing.Activate(_player);
                
                if (debugMode)
                    Debug.Log($"[PowerUpManager] ⏱️ Refreshed {type.Name}");
                
                return;
            }
            else
            {
                // Remove dead reference
                _activePowerUps.Remove(type);
            }
        }
        
        // Create new powerup
        PowerUpBase powerUp = CreatePowerUp<T>();
        
        if (powerUp != null)
        {
            _activePowerUps[type] = powerUp;
            powerUp.Activate(_player);
            
            if (debugMode)
                Debug.Log($"[PowerUpManager] ✓ Activated {type.Name}");
        }
        else
        {
            Debug.LogError($"[PowerUpManager] ❌ Failed to create {type.Name}!");
        }
    }

    public void DeactivatePowerUp<T>() where T : PowerUpBase
    {
        System.Type type = typeof(T);
        
        if (_activePowerUps.ContainsKey(type))
        {
            PowerUpBase powerUp = _activePowerUps[type];
            
            if (powerUp != null)
            {
                powerUp.Deactivate();
                
                if (powerUp.gameObject != null)
                {
                    Destroy(powerUp.gameObject);
                }
            }
            
            _activePowerUps.Remove(type);
            
            if (debugMode)
                Debug.Log($"[PowerUpManager] ✓ Deactivated {type.Name}");
        }
    }

    public bool IsPowerUpActive<T>() where T : PowerUpBase
    {
        System.Type type = typeof(T);
        
        if (_activePowerUps.ContainsKey(type))
        {
            PowerUpBase powerUp = _activePowerUps[type];
            return powerUp != null && powerUp.IsActive;
        }
        
        return false;
    }
    
    #endregion

    #region Monitoring
    
    private void CheckExpiredPowerUps()
    {
        List<System.Type> expiredTypes = new List<System.Type>();
        
        foreach (var kvp in _activePowerUps)
        {
            PowerUpBase powerUp = kvp.Value;
            
            if (powerUp == null || !powerUp.IsActive)
            {
                expiredTypes.Add(kvp.Key);
            }
        }
        
        foreach (var type in expiredTypes)
        {
            if (_activePowerUps.ContainsKey(type))
            {
                PowerUpBase powerUp = _activePowerUps[type];
                
                if (powerUp != null && powerUp.gameObject != null)
                {
                    Destroy(powerUp.gameObject);
                }
                
                _activePowerUps.Remove(type);
                
                if (debugMode)
                    Debug.Log($"[PowerUpManager] 🗑️ Cleaned up {type.Name}");
            }
        }
    }
    
    #endregion

    #region Factory - FIXED
    
    /// <summary>
    /// Create powerup instance - FIXED: Proper instantiation
    /// </summary>
    private PowerUpBase CreatePowerUp<T>() where T : PowerUpBase
    {
        GameObject prefab = GetPowerUpPrefab<T>();
        
        if (prefab == null)
        {
            Debug.LogError($"[PowerUpManager] ❌ No prefab for {typeof(T).Name}!");
            return null;
        }

        // FIXED: Instantiate as child of player
        GameObject instance = Instantiate(prefab, _player.transform);
        instance.name = $"{typeof(T).Name}_Active";
        
        // Get component
        T component = instance.GetComponent<T>();
        
        if (component == null)
        {
            Debug.LogError($"[PowerUpManager] ❌ Prefab missing {typeof(T).Name} component!");
            Destroy(instance);
            return null;
        }
        
        if (debugMode)
            Debug.Log($"[PowerUpManager] ✓ Created {typeof(T).Name} instance");
        
        return component;
    }

    private GameObject GetPowerUpPrefab<T>() where T : PowerUpBase
    {
        if (typeof(T) == typeof(IceTeaPowerUp))
            return iceTeaPrefab;
        
        if (typeof(T) == typeof(ColdTowelPowerUp))
            return coldTowelPrefab;
        
        if (typeof(T) == typeof(MedicinePowerUp))
            return medicinePrefab;
        
        return null;
    }
    
    #endregion

    #region Public API
    
    public T GetActivePowerUp<T>() where T : PowerUpBase
    {
        System.Type type = typeof(T);
        
        if (_activePowerUps.ContainsKey(type))
        {
            PowerUpBase powerUp = _activePowerUps[type];
            
            if (powerUp != null && powerUp.IsActive)
            {
                return powerUp as T;
            }
        }
        
        return null;
    }

    public void ClearAllPowerUps()
    {
        foreach (var powerUp in _activePowerUps.Values)
        {
            if (powerUp != null)
            {
                powerUp.Deactivate();
                
                if (powerUp.gameObject != null)
                {
                    Destroy(powerUp.gameObject);
                }
            }
        }
        
        _activePowerUps.Clear();
        
        if (debugMode)
            Debug.Log("[PowerUpManager] All powerups cleared");
    }
    
    #endregion
}