using UnityEngine;

/// <summary>
/// Toilet Trigger Zone - UPDATED: Simple trigger only
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class ToiletTriggerZone : MonoBehaviour
{
    #region Serialized Fields

    [Header("Trigger Settings")]
    [SerializeField] private Vector3 triggerSize = new Vector3(5f, 5f, 5f);
    [SerializeField] private Vector3 triggerOffset = new Vector3(0, 0, 2f);
    [SerializeField] private Vector3 toiletRotation = new Vector3(0, 180f, 0);
 
    [Header("Victory Controller")]
    public VictorySequenceController victoryController;
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    
    #endregion

    #region Components
    
    private BoxCollider _triggerCollider;
    private bool _hasTriggered = false;
    
    #endregion

    #region Unity Lifecycle
    
    void Awake()
    {
        SetupTrigger();
    }

    void Start()
    {
        // Auto-find VictorySequenceController if not assigned
        if (victoryController == null)
        {
            victoryController = FindObjectOfType<VictorySequenceController>();

            if (victoryController == null)
            {
                Debug.LogError("[ToiletTrigger] ❌ VictorySequenceController not found!");
            }
            else
            {
                if (showDebug)
                    Debug.Log("[ToiletTrigger] ✓ Auto-found VictorySequenceController");
            }
        }
        
        transform.rotation = Quaternion.Euler(toiletRotation);
    }
    
    #endregion

    #region Setup
    
    private void SetupTrigger()
    {
        _triggerCollider = GetComponent<BoxCollider>();
        
        if (_triggerCollider == null)
        {
            _triggerCollider = gameObject.AddComponent<BoxCollider>();
        }
        
        _triggerCollider.isTrigger = true;
        _triggerCollider.size = triggerSize;
        _triggerCollider.center = triggerOffset;
        transform.rotation = Quaternion.Euler(toiletRotation);
        
        if (showDebug)
            Debug.Log($"[ToiletTrigger] Trigger zone setup at {transform.position}");
    }
    
    #endregion

    #region Trigger Detection
    
    void OnTriggerEnter(Collider other)
    {
        if (showDebug)
            Debug.Log($"[ToiletTrigger] Trigger hit by: {other.name}, Tag: {other.tag}");
        
        // Only trigger once
        if (_hasTriggered)
        {
            if (showDebug)
                Debug.Log("[ToiletTrigger] Already triggered - ignoring");
            return;
        }
        
        // Check if player
        if (!other.CompareTag("Player"))
        {
            if (showDebug)
                Debug.LogWarning($"[ToiletTrigger] Not player! Tag: {other.tag}");
            return;
        }
        
        // Get player controller
        PlayerController player = other.GetComponent<PlayerController>();
        
        if (player == null)
        {
            Debug.LogError("[ToiletTrigger] ❌ Player has no PlayerController component!");
            return;
        }
        
        // Mark as triggered
        _hasTriggered = true;
        
        Debug.Log("[ToiletTrigger] 🚽 Player reached toilet!");
        
        // Trigger victory sequence
        TriggerVictory(player);
    }

    /// <summary>
    /// Trigger victory sequence
    /// </summary>
    private void TriggerVictory(PlayerController player)
    {
        if (victoryController == null)
        {
            Debug.LogError("[ToiletTrigger] ❌ VictorySequenceController is null!");
            
            // Fallback: Call GameManager directly
            Debug.LogWarning("[ToiletTrigger] ⚠️ Using fallback - calling GameManager.Victory() directly");
            
            if (player != null)
            {
                player.StopPlayer();
            }
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Victory();
            }
            
            return;
        }
        
        // Call victory sequence
        Debug.Log("[ToiletTrigger] → Calling VictorySequenceController.TriggerVictory()");
        victoryController.TriggerVictory(player);
    }
    
    #endregion

    #region Public API
    
    public void ResetTrigger()
    {
        _hasTriggered = false;
    }
    
    #endregion

    #region Gizmos
    
    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showDebug) return;
        
        Gizmos.color = _hasTriggered ? Color.green : Color.cyan;
        
        Vector3 center = transform.position + triggerOffset;
        Gizmos.DrawWireCube(center, triggerSize);
        
        UnityEditor.Handles.Label(center + Vector3.up * 3f, "🚽 TOILET TRIGGER");
    }
    #endif
    
    #endregion
}