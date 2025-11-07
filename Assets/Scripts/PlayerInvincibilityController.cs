using UnityEngine;

/// <summary>
/// Player Invincibility Controller - FIXED: Proper layer restoration
/// SOLID: Single Responsibility - Invincibility only
/// </summary>
public class PlayerInvincibilityController : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Invincibility Settings")]
    [SerializeField] private float invincibilityDuration = 1f;
    
    [Header("Layer Settings")]
    [SerializeField] private string normalLayer = "Player";
    [SerializeField] private string invincibleLayer = "PowerUp";
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    
    #endregion

    #region State
    
    private bool _isInvincible = false;
    private float _invincibilityTimer = 0f;
    private int _normalLayerID;
    private int _invincibleLayerID;
    
    #endregion

    #region Events
    
    public event System.Action OnInvincibilityStart;
    public event System.Action OnInvincibilityEnd;
    
    #endregion

    #region Properties
    
    public bool IsInvincible => _isInvincible;
    public float TimeRemaining => _invincibilityTimer;
    
    #endregion

    #region Unity Lifecycle
    
    void Awake()
    {
        // Get layer IDs
        _normalLayerID = LayerMask.NameToLayer(normalLayer);
        _invincibleLayerID = LayerMask.NameToLayer(invincibleLayer);
        
        // Verify layers exist
        if (_normalLayerID == -1)
        {
            Debug.LogError($"[Invincibility] ❌ Layer '{normalLayer}' not found!");
        }
        
        if (_invincibleLayerID == -1)
        {
            Debug.LogError($"[Invincibility] ❌ Layer '{invincibleLayer}' not found!");
        }
        
        // Ensure we start in normal layer
        gameObject.layer = _normalLayerID;
        
        if (showDebug)
        {
            Debug.Log($"[Invincibility] Initialized - Normal Layer: {_normalLayerID}, Invincible Layer: {_invincibleLayerID}");
        }
    }

    void Update()
    {
        UpdateInvincibilityTimer();
    }
    
    #endregion

    #region Invincibility Control
    
    /// <summary>
    /// Activate invincibility - FIXED
    /// </summary>
    public void ActivateInvincibility()
    {
        ActivateInvincibility(invincibilityDuration);
    }

    /// <summary>
    /// Activate invincibility with custom duration - FIXED
    /// </summary>
    public void ActivateInvincibility(float duration)
    {
        if (_invincibleLayerID == -1)
        {
            Debug.LogError("[Invincibility] ❌ Invincible layer not set up!");
            return;
        }
        
        if (_isInvincible)
        {
            // Already invincible - extend duration
            _invincibilityTimer = Mathf.Max(_invincibilityTimer, duration);
            
            if (showDebug)
                Debug.Log($"[Invincibility] ⏱️ Extended to {_invincibilityTimer:F2}s");
            
            return;
        }
        
        // Start invincibility
        _isInvincible = true;
        _invincibilityTimer = duration;
        
        // Change to invincible layer
        ChangeLayer(_invincibleLayerID);
        
        // Trigger event
        OnInvincibilityStart?.Invoke();
        
        if (showDebug)
        {
            Debug.Log($"[Invincibility] ✅ ACTIVATED for {duration:F2}s");
            Debug.Log($"[Invincibility] Layer: {LayerMask.LayerToName(gameObject.layer)} (ID: {gameObject.layer})");
        }
    }

    /// <summary>
    /// Deactivate invincibility - FIXED: Force layer restore
    /// </summary>
    public void DeactivateInvincibility()
    {
        if (!_isInvincible)
        {
            if (showDebug)
                Debug.Log("[Invincibility] Already inactive");
            return;
        }
        
        _isInvincible = false;
        _invincibilityTimer = 0f;
        
        // CRITICAL: Restore normal layer
        ChangeLayer(_normalLayerID);
        
        // Trigger event
        OnInvincibilityEnd?.Invoke();
        
        if (showDebug)
        {
            Debug.Log("[Invincibility] ✅ DEACTIVATED");
            Debug.Log($"[Invincibility] Layer restored to: {LayerMask.LayerToName(gameObject.layer)} (ID: {gameObject.layer})");
        }
    }

    /// <summary>
    /// Update timer - FIXED: Better logging
    /// </summary>
    private void UpdateInvincibilityTimer()
    {
        if (!_isInvincible)
            return;
        
        _invincibilityTimer -= Time.deltaTime;
        
        // Debug log every 0.5s
        if (showDebug && Mathf.FloorToInt(_invincibilityTimer * 2f) % 1 == 0)
        {
            Debug.Log($"[Invincibility] ⏱️ Remaining: {_invincibilityTimer:F2}s, Layer: {gameObject.layer}");
        }
        
        if (_invincibilityTimer <= 0f)
        {
            DeactivateInvincibility();
        }
    }

    /// <summary>
    /// Change player layer - FIXED: Validation
    /// </summary>
    private void ChangeLayer(int layerID)
    {
        if (layerID == -1)
        {
            Debug.LogError($"[Invincibility] ❌ Cannot change to invalid layer!");
            return;
        }
        
        int previousLayer = gameObject.layer;
        gameObject.layer = layerID;
        
        // Verify layer was changed
        if (gameObject.layer != layerID)
        {
            Debug.LogError($"[Invincibility] ❌ Layer change FAILED! Expected {layerID}, got {gameObject.layer}");
        }
        else if (showDebug)
        {
            string layerName = LayerMask.LayerToName(layerID);
            Debug.Log($"[Invincibility] Layer changed: {previousLayer} → {layerID} ({layerName})");
        }
    }
    
    #endregion

    #region Public API
    
    /// <summary>
    /// Force stop invincibility
    /// </summary>
    public void ForceStop()
    {
        if (showDebug)
            Debug.Log("[Invincibility] Force stop");
        
        DeactivateInvincibility();
    }

    /// <summary>
    /// Reset to normal state - FIXED: Ensure layer is reset
    /// </summary>
    public void Reset()
    {
        _isInvincible = false;
        _invincibilityTimer = 0f;
        
        // CRITICAL: Always restore normal layer
        if (_normalLayerID != -1)
        {
            gameObject.layer = _normalLayerID;
        }
        
        if (showDebug)
        {
            Debug.Log($"[Invincibility] ✅ Reset - Layer: {LayerMask.LayerToName(gameObject.layer)}");
        }
    }
    
    /// <summary>
    /// Get current state info for debugging
    /// </summary>
    public string GetStateInfo()
    {
        return $"Invincible: {_isInvincible}, Timer: {_invincibilityTimer:F2}s, Layer: {gameObject.layer} ({LayerMask.LayerToName(gameObject.layer)})";
    }
    
    #endregion

    #region Debug GUI
    
    #if UNITY_EDITOR
    // void OnGUI()
    // {
    //     if (!showDebug || !Application.isPlaying) return;

    //     GUIStyle style = new GUIStyle();
    //     style.fontSize = 16;
    //     style.normal.textColor = _isInvincible ? Color.yellow : Color.gray;
    //     style.normal.background = MakeBackgroundTexture(_isInvincible ? new Color(1, 1, 0, 0.3f) : new Color(0, 0, 0, 0.3f));

    //     int y = 350;
    //     int lineHeight = 22;
        
    //     GUI.Label(new Rect(10, y, 400, 25), "═══ INVINCIBILITY ═══", style);
    //     y += 30;

    //     if (_isInvincible)
    //     {
    //         style.normal.textColor = Color.yellow;
    //         GUI.Label(new Rect(10, y, 400, lineHeight), $"⚡ INVINCIBLE: {_invincibilityTimer:F2}s", style);
    //         y += lineHeight;
            
    //         GUI.Label(new Rect(10, y, 400, lineHeight), $"Layer: {LayerMask.LayerToName(gameObject.layer)} ({gameObject.layer})", style);
    //     }
    //     else
    //     {
    //         style.normal.textColor = Color.green;
    //         GUI.Label(new Rect(10, y, 400, lineHeight), "✓ Normal (Vulnerable)", style);
    //         y += lineHeight;
            
    //         GUI.Label(new Rect(10, y, 400, lineHeight), $"Layer: {LayerMask.LayerToName(gameObject.layer)} ({gameObject.layer})", style);
    //     }
    // }

    private Texture2D MakeBackgroundTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
    #endif
    
    #endregion

    #region Context Menu - Debug Tools
    
    #if UNITY_EDITOR
    
    [ContextMenu("Test Activate Invincibility")]
    void TestActivate()
    {
        ActivateInvincibility(2f);
    }
    
    [ContextMenu("Test Deactivate Invincibility")]
    void TestDeactivate()
    {
        DeactivateInvincibility();
    }
    
    [ContextMenu("Print State Info")]
    void PrintState()
    {
        Debug.Log($"[Invincibility] {GetStateInfo()}");
    }
    
    [ContextMenu("Force Reset Layer")]
    void ForceResetLayer()
    {
        gameObject.layer = _normalLayerID;
        Debug.Log($"[Invincibility] Force reset layer to {_normalLayerID} ({normalLayer})");
    }
    
    #endif
    
    #endregion
}