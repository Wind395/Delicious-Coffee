/// <summary>
/// PowerUp VFX Controller - Manages visual effects for powerups
/// SOLID: Single Responsibility - VFX management only
/// Design Pattern: Singleton
/// </summary>
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PowerUpVFXController : MonoBehaviour
{
    #region Singleton
    
    private static PowerUpVFXController _instance;
    public static PowerUpVFXController Instance => _instance;
    
    #endregion

    #region Serialized Fields
    
    [Header("=== VFX SEARCH SETTINGS ===")]
    [Tooltip("Tên GameObject VFX trong Player hierarchy")]
    [SerializeField] private string iceTeaVFXName = "IceTeaVFX";
    [SerializeField] private string coldTowelVFXName = "ColdTowelVFX";
    [SerializeField] private string medicineVFXName = "MedicineVFX";
    
    [Header("=== AUTO FIND SETTINGS ===")]
    [Tooltip("Tự động tìm Player khi spawn")]
    [SerializeField] private bool autoFindPlayer = true;
    
    [Tooltip("Tag của Player (để tìm)")]
    [SerializeField] private string playerTag = "Player";
    
    [Tooltip("Thời gian chờ tối đa để tìm Player (giây)")]
    [SerializeField] private float maxWaitTime = 5f;
    
    [Tooltip("Interval giữa các lần retry tìm Player (giây)")]
    [SerializeField] private float retryInterval = 0.5f;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugLogs = false;
    
    #endregion

    #region State
    
    // VFX References
    private GameObject iceTeaVFX;
    private GameObject coldTowelVFX;
    private GameObject medicineVFX;
    
    // Parent transform
    private Transform vfxParent;
    
    // Track active VFX
    private Dictionary<PowerUpType, GameObject> _vfxDictionary;
    private Dictionary<PowerUpType, bool> _activeStates;
    
    // Initialization state
    private bool _isInitialized = false;
    
    #endregion

    #region PowerUp Type Enum
    
    public enum PowerUpType
    {
        IceTea,
        ColdTowel,
        Medicine
    }
    
    #endregion

    #region Unity Lifecycle
    
    void Awake()
    {
        // Singleton setup
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        
        // Initialize dictionaries
        InitializeDictionaries();
    }

    void Start()
    {
        if (autoFindPlayer)
        {
            StartCoroutine(AutoFindAndInitialize());
        }
    }
    
    #endregion

    #region Initialization
    
    /// <summary>
    /// Initialize VFX dictionaries
    /// </summary>
    private void InitializeDictionaries()
    {
        _activeStates = new Dictionary<PowerUpType, bool>
        {
            { PowerUpType.IceTea, false },
            { PowerUpType.ColdTowel, false },
            { PowerUpType.Medicine, false }
        };
        
        if (showDebugLogs)
        {
            Debug.Log("[PowerUpVFX] Dictionaries initialized");
        }
    }
    
    /// <summary>
    /// Auto find player và initialize VFX (Coroutine)
    /// </summary>
    private IEnumerator AutoFindAndInitialize()
    {
        float elapsedTime = 0f;
        
        if (showDebugLogs)
        {
            Debug.Log("[PowerUpVFX] 🔍 Đang tìm Player...");
        }
        
        while (elapsedTime < maxWaitTime)
        {
            // Tìm Player
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            
            if (player != null)
            {
                vfxParent = player.transform;
                
                if (showDebugLogs)
                {
                    Debug.Log($"[PowerUpVFX] ✓ Tìm thấy Player: {player.name}");
                }
                
                // Tìm và initialize VFX
                if (FindAndInitializeVFX())
                {
                    yield break; // Thành công -> dừng coroutine
                }
            }
            
            // Chờ trước khi retry
            yield return new WaitForSeconds(retryInterval);
            elapsedTime += retryInterval;
        }
        
        // Timeout
        Debug.LogError($"[PowerUpVFX] ❌ Không tìm thấy Player sau {maxWaitTime}s!");
    }
    
    /// <summary>
    /// Tìm các VFX GameObject trong Player và initialize
    /// </summary>
    private bool FindAndInitializeVFX()
    {
        if (vfxParent == null)
        {
            Debug.LogError("[PowerUpVFX] ❌ VFX Parent is null!");
            return false;
        }
        
        // Tìm VFX objects
        iceTeaVFX = FindVFXChild(iceTeaVFXName);
        coldTowelVFX = FindVFXChild(coldTowelVFXName);
        medicineVFX = FindVFXChild(medicineVFXName);
        
        // Validate
        bool allFound = ValidateVFXReferences();
        
        if (!allFound)
        {
            Debug.LogWarning("[PowerUpVFX] ⚠️ Một số VFX chưa được tìm thấy!");
            return false;
        }
        
        // Update dictionary
        UpdateVFXDictionary();
        
        // Hide all VFX initially
        HideAllVFX();
        
        _isInitialized = true;
        
        if (showDebugLogs)
        {
            Debug.Log("[PowerUpVFX] ✓ VFX initialized successfully!");
            DebugPrintVFXInfo();
        }
        
        return true;
    }
    
    /// <summary>
    /// Tìm VFX child object theo tên (search trong children)
    /// </summary>
    private GameObject FindVFXChild(string vfxName)
    {
        if (string.IsNullOrEmpty(vfxName))
        {
            return null;
        }
        
        // Tìm trong direct children
        Transform child = vfxParent.Find(vfxName);
        
        if (child != null)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[PowerUpVFX] ✓ Found VFX: {vfxName}");
            }
            return child.gameObject;
        }
        
        // Tìm trong tất cả children (recursive)
        child = FindChildRecursive(vfxParent, vfxName);
        
        if (child != null)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[PowerUpVFX] ✓ Found VFX (recursive): {vfxName}");
            }
            return child.gameObject;
        }
        
        Debug.LogWarning($"[PowerUpVFX] ⚠️ VFX not found: {vfxName}");
        return null;
    }
    
    /// <summary>
    /// Tìm child object đệ quy
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }
            
            Transform result = FindChildRecursive(child, childName);
            if (result != null)
            {
                return result;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Update VFX dictionary sau khi tìm được objects
    /// </summary>
    private void UpdateVFXDictionary()
    {
        _vfxDictionary = new Dictionary<PowerUpType, GameObject>
        {
            { PowerUpType.IceTea, iceTeaVFX },
            { PowerUpType.ColdTowel, coldTowelVFX },
            { PowerUpType.Medicine, medicineVFX }
        };
    }
    
    /// <summary>
    /// Validate all VFX are assigned
    /// </summary>
    private bool ValidateVFXReferences()
    {
        bool allValid = true;
        
        if (iceTeaVFX == null)
        {
            Debug.LogWarning($"[PowerUpVFX] ⚠️ Ice Tea VFX not found! (Looking for: {iceTeaVFXName})");
            allValid = false;
        }
        
        if (coldTowelVFX == null)
        {
            Debug.LogWarning($"[PowerUpVFX] ⚠️ Cold Towel VFX not found! (Looking for: {coldTowelVFXName})");
            allValid = false;
        }
        
        if (medicineVFX == null)
        {
            Debug.LogWarning($"[PowerUpVFX] ⚠️ Medicine VFX not found! (Looking for: {medicineVFXName})");
            allValid = false;
        }
        
        if (vfxParent == null)
        {
            Debug.LogError("[PowerUpVFX] ❌ VFX Parent not assigned!");
            allValid = false;
        }
        
        return allValid;
    }
    
    #endregion

    #region Public API - Manual Initialization
    
    /// <summary>
    /// Manual initialize - gọi sau khi Player đã spawn
    /// </summary>
    public void ManualInitialize(Transform playerTransform)
    {
        if (_isInitialized)
        {
            if (showDebugLogs)
            {
                Debug.Log("[PowerUpVFX] Already initialized - skipping");
            }
            return;
        }
        
        vfxParent = playerTransform;
        FindAndInitializeVFX();
    }
    
    /// <summary>
    /// Force re-initialize (nếu cần)
    /// </summary>
    public void ForceReinitialize()
    {
        _isInitialized = false;
        
        if (autoFindPlayer)
        {
            StartCoroutine(AutoFindAndInitialize());
        }
        else
        {
            Debug.LogWarning("[PowerUpVFX] Auto find disabled - call ManualInitialize()");
        }
    }
    
    /// <summary>
    /// Check if initialized
    /// </summary>
    public bool IsInitialized => _isInitialized;
    
    #endregion

    #region Public API - Show/Hide
    
    /// <summary>
    /// Show VFX for specific powerup type
    /// </summary>
    public void ShowVFX(PowerUpType type)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[PowerUpVFX] ⚠️ Not initialized yet - cannot show VFX");
            return;
        }
        
        if (!_vfxDictionary.ContainsKey(type))
        {
            Debug.LogError($"[PowerUpVFX] Unknown powerup type: {type}");
            return;
        }
        
        GameObject vfx = _vfxDictionary[type];
        
        if (vfx == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[PowerUpVFX] {type} VFX is null - cannot show");
            }
            return;
        }
        
        // Show VFX
        vfx.SetActive(true);
        _activeStates[type] = true;
        
        // Reset particle systems
        ResetParticleSystems(vfx);
        
        if (showDebugLogs)
        {
            Debug.Log($"[PowerUpVFX] ✨ {type} VFX SHOWN");
        }
    }
    
    /// <summary>
    /// Hide VFX for specific powerup type
    /// </summary>
    public void HideVFX(PowerUpType type)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[PowerUpVFX] ⚠️ Not initialized yet - cannot hide VFX");
            return;
        }
        
        if (!_vfxDictionary.ContainsKey(type))
        {
            Debug.LogError($"[PowerUpVFX] Unknown powerup type: {type}");
            return;
        }
        
        GameObject vfx = _vfxDictionary[type];
        
        if (vfx == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[PowerUpVFX] {type} VFX is null - cannot hide");
            }
            return;
        }
        
        // Hide VFX
        vfx.SetActive(false);
        _activeStates[type] = false;
        
        if (showDebugLogs)
        {
            Debug.Log($"[PowerUpVFX] ⚫ {type} VFX HIDDEN");
        }
    }
    
    /// <summary>
    /// Hide all VFX
    /// </summary>
    public void HideAllVFX()
    {
        if (!_isInitialized)
        {
            return;
        }
        
        foreach (PowerUpType type in System.Enum.GetValues(typeof(PowerUpType)))
        {
            HideVFX(type);
        }
        
        if (showDebugLogs)
        {
            Debug.Log("[PowerUpVFX] All VFX hidden");
        }
    }
    
    /// <summary>
    /// Check if VFX is active
    /// </summary>
    public bool IsVFXActive(PowerUpType type)
    {
        if (_activeStates.ContainsKey(type))
        {
            return _activeStates[type];
        }
        return false;
    }
    
    #endregion

    #region Helper Methods
    
    /// <summary>
    /// Reset all particle systems in VFX
    /// </summary>
    private void ResetParticleSystems(GameObject vfx)
    {
        ParticleSystem[] particles = vfx.GetComponentsInChildren<ParticleSystem>();
        
        foreach (ParticleSystem ps in particles)
        {
            ps.Clear();
            ps.Play();
        }
        
        if (showDebugLogs && particles.Length > 0)
        {
            Debug.Log($"[PowerUpVFX] Reset {particles.Length} particle systems");
        }
    }
    
    /// <summary>
    /// Set VFX parent (for runtime changes)
    /// </summary>
    public void SetVFXParent(Transform parent)
    {
        vfxParent = parent;
        
        // Re-parent all VFX
        if (iceTeaVFX != null)
        {
            iceTeaVFX.transform.SetParent(vfxParent);
        }
        
        if (coldTowelVFX != null)
        {
            coldTowelVFX.transform.SetParent(vfxParent);
        }
        
        if (medicineVFX != null)
        {
            medicineVFX.transform.SetParent(vfxParent);
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[PowerUpVFX] VFX parent set to: {parent.name}");
        }
    }
    
    #endregion

    #region Debug Methods
    
    /// <summary>
    /// Print VFX info for debugging
    /// </summary>
    private void DebugPrintVFXInfo()
    {
        Debug.Log("═══════════════════════════════");
        Debug.Log("  POWERUP VFX CONTROLLER INFO  ");
        Debug.Log("═══════════════════════════════");
        Debug.Log($"Parent: {(vfxParent != null ? vfxParent.name : "NULL")}");
        Debug.Log($"Ice Tea VFX: {(iceTeaVFX != null ? "✓" : "✗")}");
        Debug.Log($"Cold Towel VFX: {(coldTowelVFX != null ? "✓" : "✗")}");
        Debug.Log($"Medicine VFX: {(medicineVFX != null ? "✓" : "✗")}");
        Debug.Log($"Initialized: {_isInitialized}");
        Debug.Log("═══════════════════════════════");
    }
    
    #if UNITY_EDITOR
    
    [ContextMenu("Manual: Find And Initialize")]
    void EditorManualInitialize()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            ManualInitialize(player.transform);
        }
        else
        {
            Debug.LogError("[PowerUpVFX] Player not found!");
        }
    }
    
    [ContextMenu("Test: Show Ice Tea VFX")]
    void TestShowIceTea()
    {
        ShowVFX(PowerUpType.IceTea);
    }
    
    [ContextMenu("Test: Show Cold Towel VFX")]
    void TestShowColdTowel()
    {
        ShowVFX(PowerUpType.ColdTowel);
    }
    
    [ContextMenu("Test: Show Medicine VFX")]
    void TestShowMedicine()
    {
        ShowVFX(PowerUpType.Medicine);
    }
    
    [ContextMenu("Test: Hide All VFX")]
    void TestHideAll()
    {
        HideAllVFX();
    }
    
    [ContextMenu("Debug: Print VFX States")]
    void DebugPrintStates()
    {
        Debug.Log("═══ POWERUP VFX STATES ═══");
        Debug.Log($"Initialized: {_isInitialized}");
        
        if (_activeStates != null)
        {
            foreach (var kvp in _activeStates)
            {
                string status = kvp.Value ? "✓ ACTIVE" : "⚫ HIDDEN";
                Debug.Log($"{kvp.Key}: {status}");
            }
        }
    }
    
    [ContextMenu("Debug: Print VFX Info")]
    void EditorDebugPrintInfo()
    {
        DebugPrintVFXInfo();
    }
    
    void OnDrawGizmosSelected()
    {
        if (vfxParent == null) return;
        
        // Draw VFX parent position
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(vfxParent.position, 0.5f);
        
        UnityEditor.Handles.Label(
            vfxParent.position + Vector3.up * 2f,
            $"✨ VFX PARENT\n{(_isInitialized ? "✓ Initialized" : "⚫ Not Init")}"
        );
    }
    
    #endif
    
    #endregion
}