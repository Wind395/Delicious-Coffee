using UnityEngine;

/// <summary>
/// Toilet Settings - UPDATED: Player Rotation Offset
/// </summary>
public class ToiletSettings : MonoBehaviour
{
    #region Inspector Settings
    
    [Header("═══ SPAWN SETTINGS ═══")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    [SerializeField] private Vector3 scaleOverride = Vector3.one;
    
    // ═══ SITTING POSITION/ROTATION ═══
    [Header("═══ SITTING SETTINGS ═══")]
    [Tooltip("Child transform marking EXACT sitting position")]
    [SerializeField] private Transform sittingPoint;
    
    [Tooltip("Player rotation offset từ SittingPoint rotation (Euler angles)")]
    [SerializeField] private Vector3 playerRotationOffset = Vector3.zero;
    
    [Header("═══ FALLBACK (if no SittingPoint) ═══")]
    [SerializeField] private Vector3 fallbackPositionOffset = new Vector3(0, 0.5f, 0.5f);
    [SerializeField] private Vector3 fallbackRotationOffset = new Vector3(0, 180, 0);
    
    [Header("═══ PREVIEW ═══")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color toiletGizmoColor = Color.cyan;
    [SerializeField] private Color sittingGizmoColor = Color.green;
    [SerializeField] private Color playerGizmoColor = Color.yellow;
    
    #endregion
    
    #region Properties
    
    public Vector3 PositionOffset => positionOffset;
    public Vector3 ScaleOverride => scaleOverride;
    public Transform SittingPoint => sittingPoint;
    public Vector3 PlayerRotationOffset => playerRotationOffset;
    
    #endregion
    
    #region Unity Lifecycle
    
    void Awake()
    {
        // Auto-find sitting point if not assigned
        if (sittingPoint == null)
        {
            Transform child = transform.Find("SittingPoint");
            if (child != null)
            {
                sittingPoint = child;
                Debug.Log("[ToiletSettings] ✓ Auto-found SittingPoint");
            }
            else
            {
                Debug.LogWarning($"[ToiletSettings] No SittingPoint found in {gameObject.name}! Will use fallback offset.");
            }
        }
    }
    
    void OnValidate()
    {
        // Validate in Editor
        if (sittingPoint == null)
        {
            Debug.LogWarning($"[ToiletSettings] {gameObject.name}: No SittingPoint assigned! Add a child GameObject named 'SittingPoint'");
        }
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Apply spawn settings (toilet position/scale)
    /// </summary>
    public void ApplySettings(Transform toiletTransform, Vector3 basePosition)
    {
        if (toiletTransform == null) return;
        
        toiletTransform.position = basePosition + positionOffset;
        toiletTransform.localScale = scaleOverride;
        
        Debug.Log($"[ToiletSettings] Applied settings: Pos={toiletTransform.position}, Scale={scaleOverride}");
    }
    
    /// <summary>
    /// Get final toilet position with offset
    /// </summary>
    public Vector3 GetFinalPosition(Vector3 basePosition)
    {
        return basePosition + positionOffset;
    }
    
    // ═══════════════════════════════════════════════════════
    // PLAYER SITTING POSITION/ROTATION
    // ═══════════════════════════════════════════════════════
    
    /// <summary>
    /// Get EXACT player sitting position
    /// Priority: SittingPoint.position > Fallback offset
    /// </summary>
    public Vector3 GetPlayerSittingPosition()
    {
        // Priority 1: Use SittingPoint if exists
        if (sittingPoint != null)
        {
            return sittingPoint.position;
        }
        
        // Priority 2: Use fallback offset from toilet
        Vector3 worldOffset = transform.TransformDirection(fallbackPositionOffset);
        return transform.position + worldOffset;
    }
    
    /// <summary>
    /// Get EXACT player sitting rotation
    /// Formula: SittingPoint.rotation * PlayerRotationOffset
    /// </summary>
    public Quaternion GetPlayerSittingRotation()
    {
        Quaternion baseRotation;
        
        // Priority 1: Use SittingPoint rotation if exists
        if (sittingPoint != null)
        {
            baseRotation = sittingPoint.rotation;
        }
        else
        {
            // Priority 2: Use fallback
            baseRotation = transform.rotation * Quaternion.Euler(fallbackRotationOffset);
        }
        
        // Apply player rotation offset
        Quaternion offsetRotation = Quaternion.Euler(playerRotationOffset);
        Quaternion finalRotation = baseRotation * offsetRotation;
        
        return finalRotation;
    }
    
    /// <summary>
    /// Get sitting transform data (position + rotation combined)
    /// </summary>
    public (Vector3 position, Quaternion rotation) GetPlayerSittingTransform()
    {
        Vector3 position = GetPlayerSittingPosition();
        Quaternion rotation = GetPlayerSittingRotation();
        
        return (position, rotation);
    }
    
    #endregion
    
    #region Quick Presets
    
    [ContextMenu("Reset to Defaults")]
    public void ResetToDefaults()
    {
        positionOffset = Vector3.zero;
        scaleOverride = Vector3.one;
        playerRotationOffset = Vector3.zero;
        fallbackPositionOffset = new Vector3(0, 0.5f, 0.5f);
        fallbackRotationOffset = new Vector3(0, 180, 0);
        
        Debug.Log($"[ToiletSettings] Reset to defaults on {gameObject.name}");
    }
    
    [ContextMenu("Preset: No Player Offset")]
    public void PresetNoOffset()
    {
        playerRotationOffset = Vector3.zero;
        Debug.Log("[ToiletSettings] Player rotation = SittingPoint rotation (no offset)");
    }
    
    [ContextMenu("Preset: Slight Turn Left")]
    public void PresetTurnLeft()
    {
        playerRotationOffset = new Vector3(0, -15, 0);
        Debug.Log("[ToiletSettings] Player turns 15° left from SittingPoint");
    }
    
    [ContextMenu("Preset: Slight Turn Right")]
    public void PresetTurnRight()
    {
        playerRotationOffset = new Vector3(0, 15, 0);
        Debug.Log("[ToiletSettings] Player turns 15° right from SittingPoint");
    }
    
    [ContextMenu("Preset: Look Down")]
    public void PresetLookDown()
    {
        playerRotationOffset = new Vector3(10, 0, 0);
        Debug.Log("[ToiletSettings] Player looks down 10° from SittingPoint");
    }
    
    #endregion
    
    #region Debug Methods
    
    [ContextMenu("Debug: Print Sitting Info")]
    public void DebugPrintSittingInfo()
    {
        Debug.Log("═══════════════════════════════════");
        Debug.Log("TOILET SITTING INFO");
        Debug.Log("═══════════════════════════════════");
        
        if (sittingPoint != null)
        {
            Debug.Log($"✓ SittingPoint: {sittingPoint.name}");
            Debug.Log($"  Position: {sittingPoint.position}");
            Debug.Log($"  Rotation: {sittingPoint.rotation.eulerAngles}");
        }
        else
        {
            Debug.LogWarning("✗ No SittingPoint (using fallback)");
        }
        
        Debug.Log($"\nPlayer Rotation Offset: {playerRotationOffset}");
        
        Vector3 playerPos = GetPlayerSittingPosition();
        Quaternion playerRot = GetPlayerSittingRotation();
        
        Debug.Log($"\n📍 FINAL PLAYER POSITION: {playerPos}");
        Debug.Log($"🔄 FINAL PLAYER ROTATION: {playerRot.eulerAngles}");
        
        if (sittingPoint != null)
        {
            Vector3 diff = playerPos - sittingPoint.position;
            Debug.Log($"\nPosition difference: {diff} (should be zero)");
            
            float angleDiff = Quaternion.Angle(sittingPoint.rotation, playerRot);
            Debug.Log($"Rotation difference: {angleDiff:F2}° (= offset magnitude)");
        }
        
        Debug.Log("═══════════════════════════════════");
    }
    
    #endregion
    
    #region Gizmos - UPDATED
    
    #if UNITY_EDITOR
    
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // Draw toilet base
        Gizmos.color = new Color(toiletGizmoColor.r, toiletGizmoColor.g, toiletGizmoColor.b, 0.5f);
        Gizmos.DrawWireCube(transform.position, new Vector3(2, 3, 2));
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        
        Vector3 toiletPos = transform.position + positionOffset;
        
        // ═══════════════════════════════════════════════════════
        // 1. DRAW TOILET
        // ═══════════════════════════════════════════════════════
        Gizmos.color = toiletGizmoColor;
        Gizmos.DrawWireCube(toiletPos, new Vector3(2, 3, 2));
        
        // ═══════════════════════════════════════════════════════
        // 2. DRAW SITTING POINT (if exists)
        // ═══════════════════════════════════════════════════════
        if (sittingPoint != null)
        {
            Vector3 sittingPos = sittingPoint.position;
            Quaternion sittingRot = sittingPoint.rotation;
            
            // Sitting point sphere
            Gizmos.color = sittingGizmoColor;
            Gizmos.DrawWireSphere(sittingPos, 0.3f);
            
            // Sitting point forward direction
            Vector3 sittingForward = sittingRot * Vector3.forward;
            UnityEditor.Handles.color = sittingGizmoColor;
            UnityEditor.Handles.ArrowHandleCap(
                0, 
                sittingPos + Vector3.up * 0.5f, 
                Quaternion.LookRotation(sittingForward), 
                1.2f, 
                EventType.Repaint
            );
            
            // Label
            UnityEditor.Handles.Label(
                sittingPos + Vector3.up * 1.5f,
                $"🪑 SITTING POINT\nRot: {sittingRot.eulerAngles}"
            );
        }
        
        // ═══════════════════════════════════════════════════════
        // 3. DRAW PLAYER PREVIEW (with offset)
        // ═══════════════════════════════════════════════════════
        Vector3 playerPos = GetPlayerSittingPosition();
        Quaternion playerRot = GetPlayerSittingRotation();
        
        // Player position sphere
        Gizmos.color = playerGizmoColor;
        Gizmos.DrawSphere(playerPos, 0.2f);
        
        // Player capsule body
        UnityEditor.Handles.color = new Color(playerGizmoColor.r, playerGizmoColor.g, playerGizmoColor.b, 0.3f);
        
        Vector3 bodyBottom = playerPos;
        Vector3 bodyTop = playerPos + Vector3.up * 1.8f;
        
        // Draw capsule wireframe
        UnityEditor.Handles.DrawWireDisc(bodyBottom, Vector3.up, 0.3f);
        UnityEditor.Handles.DrawWireDisc(bodyTop, Vector3.up, 0.3f);
        
        // Draw vertical lines
        UnityEditor.Handles.DrawLine(bodyBottom + Vector3.forward * 0.3f, bodyTop + Vector3.forward * 0.3f);
        UnityEditor.Handles.DrawLine(bodyBottom - Vector3.forward * 0.3f, bodyTop - Vector3.forward * 0.3f);
        UnityEditor.Handles.DrawLine(bodyBottom + Vector3.right * 0.3f, bodyTop + Vector3.right * 0.3f);
        UnityEditor.Handles.DrawLine(bodyBottom - Vector3.right * 0.3f, bodyTop - Vector3.right * 0.3f);
        
        // Player forward direction (HEAD)
        Vector3 playerForward = playerRot * Vector3.forward;
        Vector3 headPos = bodyTop + Vector3.up * 0.3f;
        
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.ArrowHandleCap(
            0, 
            headPos, 
            Quaternion.LookRotation(playerForward), 
            1.5f, 
            EventType.Repaint
        );
        
        // Player label
        UnityEditor.Handles.Label(
            playerPos + Vector3.up * 2.8f,
            $"👤 PLAYER FINAL\n" +
            $"Pos: ({playerPos.x:F1}, {playerPos.y:F1}, {playerPos.z:F1})\n" +
            $"Rot: {playerRot.eulerAngles}\n" +
            $"Offset: {playerRotationOffset}"
        );
        
        // ═══════════════════════════════════════════════════════
        // 4. DRAW CONNECTION LINE
        // ═══════════════════════════════════════════════════════
        if (sittingPoint != null)
        {
            // Line from sitting point to player
            UnityEditor.Handles.color = new Color(1, 1, 0, 0.5f);
            UnityEditor.Handles.DrawDottedLine(sittingPoint.position, playerPos, 2f);
            
            // Show offset angle
            if (playerRotationOffset != Vector3.zero)
            {
                UnityEditor.Handles.color = Color.yellow;
                UnityEditor.Handles.Label(
                    (sittingPoint.position + playerPos) / 2f + Vector3.up * 0.5f,
                    $"↻ Offset: {playerRotationOffset}"
                );
            }
        }
        
        // ═══════════════════════════════════════════════════════
        // 5. DRAW GROUND REFERENCE
        // ═══════════════════════════════════════════════════════
        UnityEditor.Handles.color = new Color(0, 1, 1, 0.1f);
        UnityEditor.Handles.DrawSolidDisc(toiletPos, Vector3.up, 3f);
    }
    
    #endif
    
    #endregion
}