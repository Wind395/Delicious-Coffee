using UnityEngine;

/// <summary>
/// Obstacle Settings - SIMPLIFIED: Only prefab-based configuration
/// No JSON override - full control in Inspector
/// SOLID: Single Responsibility - Only holds configuration data
/// </summary>
public class ObstacleSettings : MonoBehaviour
{
    #region Inspector Settings
    
    [Header("═══ POSITION OFFSET ═══")]
    [Tooltip("Position offset from spawn point (applied after lane position)")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    
    [Header("═══ ROTATION ═══")]
    [Tooltip("Rotation (Euler angles) - Full control from prefab")]
    [SerializeField] private Vector3 rotation = Vector3.zero;
    
    [Header("═══ SCALE (Optional) ═══")]
    [Tooltip("Override scale (leave at 1,1,1 for default)")]
    [SerializeField] private Vector3 scaleOverride = Vector3.one;
    
    [Header("═══ PREVIEW ═══")]
    [Tooltip("Show gizmos in Scene view")]
    [SerializeField] private bool showGizmos = true;
    
    #endregion
    
    #region Properties
    
    public Vector3 PositionOffset => positionOffset;
    public Vector3 Rotation => rotation;
    public Vector3 ScaleOverride => scaleOverride;
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Apply settings to this obstacle
    /// Called by spawner after positioning
    /// </summary>
    public void ApplySettings(Transform obstacleTransform)
    {
        if (obstacleTransform == null) return;
        
        // Apply position offset
        obstacleTransform.localPosition += positionOffset;
        
        // Apply rotation (from prefab settings only)
        obstacleTransform.localRotation = Quaternion.Euler(rotation);
        
        // Apply scale
        obstacleTransform.localScale = scaleOverride;
    }
    
    /// <summary>
    /// Reset to defaults
    /// </summary>
    [ContextMenu("Reset to Defaults")]
    public void ResetToDefaults()
    {
        positionOffset = Vector3.zero;
        rotation = Vector3.zero;
        scaleOverride = Vector3.one;
        
        Debug.Log($"[ObstacleSettings] Reset to defaults on {gameObject.name}");
    }
    
    #endregion
    
    #region Gizmos
    
    #if UNITY_EDITOR
    
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // Draw position offset
        if (positionOffset != Vector3.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + positionOffset);
            Gizmos.DrawWireSphere(transform.position + positionOffset, 0.1f);
        }
        
        // Draw rotation indicator (forward direction)
        Gizmos.color = Color.yellow;
        Vector3 forward = Quaternion.Euler(rotation) * Vector3.forward;
        Gizmos.DrawRay(transform.position, forward * 1f);
        
        // Draw rotation indicator (up direction)
        Gizmos.color = Color.green;
        Vector3 up = Quaternion.Euler(rotation) * Vector3.up;
        Gizmos.DrawRay(transform.position, up * 0.5f);
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        
        // Draw detailed info when selected
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2f,
            $"Offset: {positionOffset}\n" +
            $"Rotation: {rotation}\n" +
            $"Scale: {scaleOverride}"
        );
        
        // Draw wire cube at final position
        Gizmos.color = Color.yellow;
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(
            transform.position + positionOffset,
            Quaternion.Euler(rotation),
            scaleOverride
        );
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = Matrix4x4.identity;
    }
    
    #endif
    
    #endregion
}