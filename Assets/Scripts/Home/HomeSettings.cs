using UnityEngine;

/// <summary>
/// Home Settings - UPDATED: Added Door reference
/// </summary>
public class HomeSettings : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    [SerializeField] private Vector3 scaleOverride = Vector3.one;
    
    [Header("═══ DOOR REFERENCE ═══")]
    [Tooltip("Reference to door controller (auto-find if null)")]
    [SerializeField] private DoorController doorController;
    
    [Header("═══ VICTORY SEQUENCE POSITIONS ═══")]
    [Tooltip("Player stops here before entering home")]
    [SerializeField] private Transform victoryPosition;
    
    [Tooltip("Player walks to here (inside home) then disappears")]
    [SerializeField] private Transform homeEntrancePosition;
    
    [Tooltip("Dog waits here (outside, visible in camera)")]
    [SerializeField] private Transform dogWaitingPosition;
    
    [Header("Victory Animation")]
    [Tooltip("Player rotation when facing home entrance")]
    [SerializeField] private Vector3 playerRotationOffset = Vector3.zero;
    
    [Tooltip("Player walk speed into home")]
    [SerializeField] private float walkSpeed = 2f;
    
    [Header("Fallback Positions (if Transforms not assigned)")]
    [SerializeField] private Vector3 fallbackVictoryOffset = new Vector3(0, 0, 3f);
    [SerializeField] private Vector3 fallbackEntranceOffset = new Vector3(0, 0, 0f);
    [SerializeField] private Vector3 fallbackDogOffset = new Vector3(-3f, 0, 5f);
    [SerializeField] private Vector3 fallbackRotationOffset = new Vector3(0, 0, 0);
    
    #region Properties
    
    public float WalkSpeed => walkSpeed;
    public DoorController Door => doorController; // NEW
    
    #endregion
    
    #region Initialization - NEW
    
    void Awake()
    {
        // Auto-find door if not assigned
        if (doorController == null)
        {
            doorController = GetComponentInChildren<DoorController>();
        }
    }
    
    #endregion
    
    #region Setup
    
    public void ApplySettings(Transform homeTransform, Vector3 basePosition)
    {
        if (homeTransform == null) return;
        
        homeTransform.position = basePosition + positionOffset;
        homeTransform.localScale = scaleOverride;
    }
    
    public Vector3 GetFinalPosition(Vector3 basePosition)
    {
        return basePosition + positionOffset;
    }
    
    #endregion
    
    #region Victory Sequence
    
    /// <summary>
    /// Get player victory position (before home entrance)
    /// </summary>
    public (Vector3 position, Quaternion rotation) GetPlayerVictoryTransform()
    {
        Vector3 position;
        Quaternion rotation;
        
        if (victoryPosition != null)
        {
            position = victoryPosition.position;
            rotation = victoryPosition.rotation * Quaternion.Euler(playerRotationOffset);
        }
        else
        {
            // Fallback
            position = transform.position + transform.TransformDirection(fallbackVictoryOffset);
            rotation = transform.rotation * Quaternion.Euler(fallbackRotationOffset);
        }
        
        return (position, rotation);
    }
    
    /// <summary>
    /// Get home entrance position (player walks here then disappears)
    /// </summary>
    public Vector3 GetHomeEntrancePosition()
    {
        if (homeEntrancePosition != null)
        {
            return homeEntrancePosition.position;
        }
        else
        {
            return transform.position + transform.TransformDirection(fallbackEntranceOffset);
        }
    }
    
    /// <summary>
    /// Get dog waiting position (outside home, visible)
    /// </summary>
    public (Vector3 position, Quaternion rotation) GetDogWaitingTransform()
    {
        Vector3 position;
        Quaternion rotation;
        
        if (dogWaitingPosition != null)
        {
            position = dogWaitingPosition.position;
            rotation = dogWaitingPosition.rotation;
        }
        else
        {
            // Fallback: To the left/right of home entrance
            position = transform.position + transform.TransformDirection(fallbackDogOffset);
            
            // Face home entrance
            Vector3 entrancePos = GetHomeEntrancePosition();
            Vector3 direction = (entrancePos - position).normalized;
            direction.y = 0;
            rotation = direction != Vector3.zero ? Quaternion.LookRotation(direction) : transform.rotation;
        }
        
        return (position, rotation);
    }
    
    #endregion
    
    #region Gizmos
    
    #if UNITY_EDITOR
    
    void OnDrawGizmos()
    {
        // Victory Position
        if (victoryPosition != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(victoryPosition.position, 0.5f);
            
            UnityEditor.Handles.Label(
                victoryPosition.position + Vector3.up * 2f,
                "🎯 VICTORY\nPOSITION"
            );
            
            // Draw forward direction
            Gizmos.DrawRay(victoryPosition.position, victoryPosition.forward * 1.5f);
        }
        
        // Home Entrance
        if (homeEntrancePosition != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(homeEntrancePosition.position, 0.3f);
            
            UnityEditor.Handles.Label(
                homeEntrancePosition.position + Vector3.up * 1.5f,
                "🚪 ENTRANCE"
            );
        }
        
        // Dog Waiting Position
        if (dogWaitingPosition != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(dogWaitingPosition.position, 0.5f);
            
            UnityEditor.Handles.Label(
                dogWaitingPosition.position + Vector3.up * 2f,
                "🐕 DOG\nWAITING"
            );
            
            // Draw look direction
            Gizmos.DrawRay(dogWaitingPosition.position, dogWaitingPosition.forward * 1f);
        }
        
        // Draw path: Victory → Entrance
        if (victoryPosition != null && homeEntrancePosition != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(victoryPosition.position, homeEntrancePosition.position);
        }
    }
    
    #endif
    
    #endregion
}