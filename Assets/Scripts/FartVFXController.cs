/// <summary>
/// Fart VFX Controller - Manages fart particle effects
/// SOLID: Single Responsibility - VFX only
/// </summary>
using UnityEngine;

public class FartVFXController : MonoBehaviour
{
    #region Singleton (Optional - hoặc attach vào Player)
    
    private static FartVFXController _instance;
    public static FartVFXController Instance => _instance;

    #endregion

    #region Serialized Fields

    [Header("VFX Prefabs")]
    [Tooltip("Fart particle effect prefab")]
    [SerializeField] private GameObject fartVFXPrefab;
    
    [Tooltip("Question VFX prefab")]
    [SerializeField] private GameObject questionVFXPrefab;

    [Header("VFX Settings")]
    [Tooltip("Spawn position offset from player (local space)")]
    [SerializeField] private Vector3 fartSpawnOffset = new Vector3(0, 0.5f, -0.3f);

    [Tooltip("VFX rotation offset from player (local space)")]
    [SerializeField] private Vector3 fartRotationOffset = Vector3.zero;

    [Tooltip("Spawn position offset from player (local space)")]
    [SerializeField] private Vector3 questionSpawnOffset = new Vector3(0, 1.5f, 0f);
    [Tooltip("VFX rotation offset from player (local space)")]
    [SerializeField] private Vector3 questionRotationOffset = Vector3.zero;
    
    [Tooltip("VFX lifetime (auto destroy)")]
    [SerializeField] private float vfxLifetime = 2f;
    
    [Header("VFX Variations")]
    [SerializeField] private float minScale = 0.8f;
    [SerializeField] private float maxScale = 1.2f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    #endregion

    #region State
    
    private Transform _playerTransform;
    
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
        FindPlayer();
    }
    
    #endregion

    #region Initialization
    
    private void FindPlayer()
    {
        // Find player transform
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("[FartVFX] Player not found!");
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Play fart VFX at player position
    /// </summary>
    public void PlayFartVFX()
    {
        if (fartVFXPrefab == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("[FartVFX] No VFX prefab assigned!");
            }
            return;
        }

        if (_playerTransform == null)
        {
            FindPlayer();

            if (_playerTransform == null)
            {
                Debug.LogError("[FartVFX] Cannot play VFX - player not found!");
                return;
            }
        }

        // Calculate spawn position (behind player)
        Vector3 spawnPosition = _playerTransform.position +
                               _playerTransform.TransformDirection(fartSpawnOffset);

        // Calculate rotation (point away from player)
        Quaternion spawnRotation = Quaternion.Euler(
            _playerTransform.eulerAngles + fartRotationOffset);

        // Spawn VFX
        GameObject vfx = Instantiate(fartVFXPrefab, spawnPosition, spawnRotation);

        // Random scale variation
        float randomScale = Random.Range(minScale, maxScale);
        vfx.transform.localScale = Vector3.one * randomScale;

        // Auto destroy
        Destroy(vfx, vfxLifetime);

        if (showDebugLogs)
        {
            Debug.Log($"[FartVFX] 💨 Played at {spawnPosition}");
        }
    }
    
    public void PlayQuestionVFX()
    {
        if (questionVFXPrefab == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("[FartVFX] No VFX prefab assigned!");
            }
            return;
        }

        if (_playerTransform == null)
        {
            FindPlayer();

            if (_playerTransform == null)
            {
                Debug.LogError("[FartVFX] Cannot play VFX - player not found!");
                return;
            }
        }

        // Calculate spawn position (behind player)
        Vector3 spawnPosition = _playerTransform.position +
                               _playerTransform.TransformDirection(questionSpawnOffset);

        // Calculate rotation (point away from player)
        Quaternion spawnRotation = Quaternion.Euler(
            _playerTransform.eulerAngles + questionRotationOffset);

        // Spawn VFX
        GameObject vfx = Instantiate(questionVFXPrefab, spawnPosition, spawnRotation);

        // Random scale variation
        float randomScale = Random.Range(minScale, maxScale);
        vfx.transform.localScale = Vector3.one * randomScale;

        // Auto destroy
        Destroy(vfx, vfxLifetime);

        if (showDebugLogs)
        {
            Debug.Log($"[FartVFX] 💨 Played at {spawnPosition}");
        }
    }

    /// <summary>
    /// Play VFX at specific position
    /// </summary>
    // public void PlayFartVFXAt(Vector3 position, Quaternion rotation)
    // {
    //     if (fartVFXPrefab == null) return;

    //     GameObject vfx = Instantiate(fartVFXPrefab, position, rotation);

    //     float randomScale = Random.Range(minScale, maxScale);
    //     vfx.transform.localScale = Vector3.one * randomScale;

    //     Destroy(vfx, vfxLifetime);
    // }



    #endregion

    #region Debug

#if UNITY_EDITOR

    [ContextMenu("Test: Play Fart VFX")]
    void TestPlayVFX()
    {
        PlayFartVFX();
    }
    
    [ContextMenu("Test: Play Question VFX")]
    void TestPlayQuestionVFX()
    {
        PlayQuestionVFX();
    }
    
    void OnDrawGizmosSelected()
    {
        if (_playerTransform != null)
        {
            // Draw spawn position
            Vector3 spawnPos = _playerTransform.position + 
                              _playerTransform.TransformDirection(fartSpawnOffset);
            
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            Gizmos.DrawSphere(spawnPos, 0.2f);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(_playerTransform.position, spawnPos);
            
            UnityEditor.Handles.Label(spawnPos + Vector3.up * 0.5f, "💨 FART VFX SPAWN");
        }
    }
    
    #endif
    
    #endregion
}