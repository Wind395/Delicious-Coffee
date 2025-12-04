using UnityEngine;

/// <summary>
/// Home Trigger Zone - UPDATED: Stop dog chase immediately
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class HomeTriggerZone : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Trigger Settings")]
    [SerializeField] private Vector3 triggerSize = new Vector3(5f, 5f, 5f);
    [SerializeField] private Vector3 triggerOffset = new Vector3(0, 0, 2f);
    [SerializeField] private Vector3 toiletRotation = new Vector3(0, 180f, 0);

    [Header("Victory Controller")]
    public VictorySequenceController victoryController;

    // [Header("Debug")]
    // [SerializeField] private bool showDebug = true;

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
            victoryController = FindAnyObjectByType<VictorySequenceController>();

            // if (victoryController == null)
            // {
            //     Debug.LogError("[HomeTrigger] ❌ VictorySequenceController not found!");
            // }
            // else
            // {
            //     if (showDebug)
            //         Debug.Log("[HomeTrigger] ✓ Auto-found VictorySequenceController");
            // }
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
        
        // if (showDebug)
        //     Debug.Log($"[HomeTrigger] Trigger zone setup at {transform.position}");
    }

    #endregion

    #region Trigger Detection

    void OnTriggerEnter(Collider other)
    {
        // if (showDebug)
        //     Debug.Log($"[HomeTrigger] Trigger hit by: {other.name}, Tag: {other.tag}");
        
        // Only trigger once
        if (_hasTriggered)
        {
            // if (showDebug)
            //     Debug.Log("[HomeTrigger] Already triggered - ignoring");
            return;
        }
        
        // Check if player
        if (!other.CompareTag("Player"))
        {
            // if (showDebug)
            //     Debug.LogWarning($"[HomeTrigger] Not player! Tag: {other.tag}");
            return;
        }
        
        // Get player controller
        PlayerController player = other.GetComponent<PlayerController>();
        
        if (player == null)
        {
            //Debug.LogError("[HomeTrigger] ❌ Player has no PlayerController component!");
            return;
        }
        
        // Mark as triggered
        _hasTriggered = true;
        
        //Debug.Log("[HomeTrigger] 🏠 Player reached Home!");
        
        // ═══ NEW: STOP DOG CHASE IMMEDIATELY ═══
        StopDogChaseImmediately();
        
        // Trigger victory sequence
        TriggerVictory(player);
    }

    #endregion

    #region Dog Chase Control - NEW

    /// <summary>
    /// Stop dog chase and disable dog GameObject - NEW
    /// </summary>
    private void StopDogChaseImmediately()
    {
        if (DogChaseController.Instance == null)
        {
            // if (showDebug)
            //     Debug.Log("[HomeTrigger] No DogChaseController found");
            return;
        }

        //Debug.Log("[HomeTrigger] 🐕 Stopping dog chase...");

        // ═══ STEP 1: Stop chase logic ═══
        DogChaseController.Instance.StopChaseOnVictory();

        // ═══ STEP 2: Disable dog GameObject ═══
        GameObject dogInstance = DogChaseController.Instance.DogInstance;
        
        if (dogInstance != null)
        {
            // Stop animator
            Animator dogAnimator = dogInstance.GetComponent<Animator>();
            if (dogAnimator != null)
            {
                dogAnimator.enabled = false;
                //Debug.Log("[HomeTrigger] 🐕 Dog animator stopped");
            }

            // Disable GameObject
            dogInstance.SetActive(false);
            //Debug.Log("[HomeTrigger] 🐕 Dog GameObject disabled");
        }
        // else
        // {
        //     if (showDebug)
        //         Debug.Log("[HomeTrigger] Dog instance is null (not spawned)");
        // }
    }

    #endregion

    #region Victory Trigger

    /// <summary>
    /// Trigger victory sequence
    /// </summary>
    private void TriggerVictory(PlayerController player)
    {
        if (victoryController == null)
        {
            //Debug.LogError("[HomeTrigger] ❌ VictorySequenceController is null!");
            
            // Fallback: Call GameManager directly
            //Debug.LogWarning("[HomeTrigger] ⚠️ Using fallback - calling GameManager.Victory() directly");
            
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
        //Debug.Log("[HomeTrigger] → Calling VictorySequenceController.TriggerVictory()");
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
        //if (!showDebug) return;
        
        Gizmos.color = _hasTriggered ? Color.green : Color.cyan;
        
        Vector3 center = transform.position + triggerOffset;
        Gizmos.DrawWireCube(center, triggerSize);
        
        UnityEditor.Handles.Label(center + Vector3.up * 3f, "🏠 HOME TRIGGER");
    }
    #endif

    #endregion
}