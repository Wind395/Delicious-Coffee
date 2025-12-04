using UnityEngine;

/// <summary>
/// PowerUp Settings - FIXED: Proper local rotation animation
/// </summary>
public class PowerUpSettings : MonoBehaviour
{
    #region Inspector Settings
    
    [Header("═══ POSITION OFFSET ═══")]
    [Tooltip("Position offset from spawn point (applied after lane position)")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    
    [Header("═══ INITIAL ROTATION ═══")]
    [Tooltip("Initial rotation (Euler angles) - Applied once on spawn")]
    [SerializeField] private Vector3 initialRotation = Vector3.zero;
    
    [Header("═══ SCALE (Optional) ═══")]
    [Tooltip("Override scale (leave at 1,1,1 for default)")]
    [SerializeField] private Vector3 scaleOverride = Vector3.one;
    
    [Header("═══ FLOATING ANIMATION ═══")]
    [Tooltip("Enable floating animation")]
    [SerializeField] private bool enableFloating = true;
    
    [Tooltip("Floating height (bobbing amplitude)")]
    [SerializeField] private float floatingHeight = 0.3f;
    
    [Tooltip("Floating speed")]
    [SerializeField] private float floatingSpeed = 2f;
    
    [Header("═══ ROTATION ANIMATION ═══")]
    [Tooltip("Enable rotation animation")]
    [SerializeField] private bool enableRotation = true;
    
    [Tooltip("Rotation speed (degrees per second)")]
    [SerializeField] private float rotationSpeed = 100f;
    
    [Tooltip("Rotation axis (LOCAL space)")]
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.Up;
    
    [Header("═══ PREVIEW ═══")]
    [Tooltip("Show gizmos in Scene view")]
    [SerializeField] private bool showGizmos = true;
    
    [SerializeField] private Color gizmoColor = Color.yellow;
    
    #endregion
    
    #region Rotation Axis Enum
    
    public enum RotationAxis
    {
        Up,        // Y axis (default - spin upright)
        Right,     // X axis (tumble forward/back)
        Forward,   // Z axis (roll side to side)
        Custom     // Custom axis (edit in inspector)
    }
    
    [SerializeField] private Vector3 customRotationAxis = Vector3.up;
    
    #endregion
    
    #region Properties
    
    public Vector3 PositionOffset => positionOffset;
    public Vector3 InitialRotation => initialRotation;
    public Vector3 ScaleOverride => scaleOverride;
    public bool EnableFloating => enableFloating;
    public float FloatingHeight => floatingHeight;
    public float FloatingSpeed => floatingSpeed;
    public bool EnableRotation => enableRotation;
    public float RotationSpeed => rotationSpeed;
    
    #endregion
    
    #region Runtime State
    
    private Vector3 _basePosition;
    private float _floatingTimer;
    private Quaternion _initialRotationQuaternion;
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Apply settings to powerup transform
    /// FIXED: Store initial rotation properly
    /// </summary>
    public void ApplySettings(Transform powerUpTransform)
    {
        if (powerUpTransform == null) return;
        
        // Store base position (before offset)
        _basePosition = powerUpTransform.position;
        
        // Apply position offset
        powerUpTransform.position += positionOffset;
        
        // ← FIX: Apply and store initial rotation
        _initialRotationQuaternion = Quaternion.Euler(initialRotation);
        powerUpTransform.localRotation = _initialRotationQuaternion;
        
        // Apply scale
        powerUpTransform.localScale = scaleOverride;
        
        // Reset floating timer with random offset (so not all sync)
        _floatingTimer = Random.Range(0f, Mathf.PI * 2f);
        
        // Debug.Log($"[PowerUpSettings] Applied settings - " +
        //          $"Pos={powerUpTransform.position}, " +
        //          $"InitRot={initialRotation}, " +
        //          $"Scale={scaleOverride}");
    }
    
    /// <summary>
    /// Update animations - FIXED: Proper local rotation
    /// </summary>
    public void UpdateAnimations()
    {
        if (transform == null) return;
        
        // Floating animation
        if (enableFloating)
        {
            UpdateFloating();
        }
        
        // Rotation animation - FIXED
        if (enableRotation)
        {
            UpdateRotationAnimation();
        }
    }
    
    /// <summary>
    /// Reset to defaults
    /// </summary>
    [ContextMenu("Reset to Defaults")]
    public void ResetToDefaults()
    {
        positionOffset = Vector3.zero;
        initialRotation = Vector3.zero;
        scaleOverride = Vector3.one;
        enableFloating = true;
        floatingHeight = 0.3f;
        floatingSpeed = 2f;
        enableRotation = true;
        rotationSpeed = 100f;
        rotationAxis = RotationAxis.Up;
        
        //Debug.Log($"[PowerUpSettings] Reset to defaults on {gameObject.name}");
    }
    
    #endregion
    
    #region Animation Updates - FIXED
    
    /// <summary>
    /// Update floating animation (bobbing up/down)
    /// </summary>
    private void UpdateFloating()
    {
        _floatingTimer += Time.deltaTime * floatingSpeed;
        
        float yOffset = Mathf.Sin(_floatingTimer) * floatingHeight;
        
        Vector3 pos = _basePosition + positionOffset;
        pos.y += yOffset;
        
        transform.position = pos;
    }
    
    /// <summary>
    /// Update rotation animation - FIXED: Rotate in LOCAL space around own center
    /// </summary>
    private void UpdateRotationAnimation()
    {
        // Get rotation axis vector
        Vector3 axis = GetRotationAxisVector();
        
        // ← FIX: Rotate in LOCAL space (Space.Self) around object's own center
        transform.Rotate(axis, rotationSpeed * Time.deltaTime, Space.Self);
    }
    
    /// <summary>
    /// Get rotation axis vector based on enum
    /// </summary>
    private Vector3 GetRotationAxisVector()
    {
        switch (rotationAxis)
        {
            case RotationAxis.Up:
                return Vector3.up;      // Y axis
            
            case RotationAxis.Right:
                return Vector3.right;   // X axis
            
            case RotationAxis.Forward:
                return Vector3.forward; // Z axis
            
            case RotationAxis.Custom:
                return customRotationAxis.normalized;
            
            default:
                return Vector3.up;
        }
    }
    
    #endregion
    
    #region Quick Presets
    
    [ContextMenu("Preset: Ice Tea (Blue Glow)")]
    public void PresetIceTea()
    {
        positionOffset = new Vector3(0, 0.5f, 0);
        initialRotation = Vector3.zero;
        scaleOverride = new Vector3(1.2f, 1.2f, 1.2f);
        enableFloating = true;
        floatingHeight = 0.4f;
        floatingSpeed = 2.5f;
        enableRotation = true;
        rotationSpeed = 80f;
        rotationAxis = RotationAxis.Up; // ← Spin upright
        gizmoColor = Color.cyan;
        
        //Debug.Log("[PowerUpSettings] Applied Ice Tea preset");
    }
    
    [ContextMenu("Preset: Cold Towel (Fast Spin)")]
    public void PresetColdTowel()
    {
        positionOffset = new Vector3(0, 0.3f, 0);
        initialRotation = Vector3.zero;
        scaleOverride = Vector3.one;
        enableFloating = true;
        floatingHeight = 0.3f;
        floatingSpeed = 3f;
        enableRotation = true;
        rotationSpeed = 150f;
        rotationAxis = RotationAxis.Up; // ← FIX: Use simple Y axis rotation
        gizmoColor = Color.blue;
        
        //Debug.Log("[PowerUpSettings] Applied Cold Towel preset");
    }
    
    [ContextMenu("Preset: Medicine (Pulsing)")]
    public void PresetMedicine()
    {
        positionOffset = new Vector3(0, 0.4f, 0);
        initialRotation = Vector3.zero;
        scaleOverride = new Vector3(1.1f, 1.1f, 1.1f);
        enableFloating = true;
        floatingHeight = 0.5f;
        floatingSpeed = 1.5f;
        enableRotation = true;
        rotationSpeed = 60f;
        rotationAxis = RotationAxis.Up; // ← FIX: Use simple Y axis rotation
        gizmoColor = Color.green;
        
        //Debug.Log("[PowerUpSettings] Applied Medicine preset");
    }
    
    #endregion
    
    #region Gizmos
    
    #if UNITY_EDITOR
    
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Vector3 basePos = transform.position;
        Vector3 finalPos = basePos + positionOffset;
        
        // Draw offset line
        if (positionOffset != Vector3.zero)
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawLine(basePos, finalPos);
            Gizmos.DrawWireSphere(finalPos, 0.2f);
        }
        
        // Draw floating range
        if (enableFloating)
        {
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
            Gizmos.DrawWireSphere(finalPos + Vector3.up * floatingHeight, 0.15f);
            Gizmos.DrawWireSphere(finalPos + Vector3.down * floatingHeight, 0.15f);
            Gizmos.DrawLine(finalPos + Vector3.up * floatingHeight, 
                           finalPos + Vector3.down * floatingHeight);
        }
        
        // Draw rotation axis - FIXED: Show local axis
        if (enableRotation)
        {
            Vector3 axis = GetRotationAxisVector();
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(finalPos, axis * 0.8f);
            
            // Draw rotation circle
            UnityEditor.Handles.color = new Color(1, 1, 0, 0.3f);
            UnityEditor.Handles.DrawWireDisc(finalPos, axis, 0.5f);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        
        Vector3 finalPos = transform.position + positionOffset;
        
        // Draw detailed info when selected
        string axisName = rotationAxis == RotationAxis.Custom ? 
            $"Custom{customRotationAxis}" : rotationAxis.ToString();
        
        UnityEditor.Handles.Label(
            finalPos + Vector3.up * 1.5f,
            $"🎁 POWERUP SETTINGS\n" +
            $"Offset: {positionOffset}\n" +
            $"Init Rot: {initialRotation}\n" +
            $"Scale: {scaleOverride}\n" +
            $"Float: {(enableFloating ? $"✓ {floatingHeight}m" : "✗")}\n" +
            $"Spin: {(enableRotation ? $"✓ {rotationSpeed}°/s ({axisName})" : "✗")}"
        );
        
        // Draw wire sphere at final position
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(finalPos, 0.5f);
        
        // Draw local axes
        float axisLength = 0.7f;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(finalPos, transform.right * axisLength); // X (Right)
        
        Gizmos.color = Color.green;
        Gizmos.DrawRay(finalPos, transform.up * axisLength);    // Y (Up)
        
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(finalPos, transform.forward * axisLength); // Z (Forward)
    }
    
    #endif
    
    #endregion
}