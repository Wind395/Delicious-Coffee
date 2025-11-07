using UnityEngine;

/// <summary>
/// Toilet Model Manager - UPDATED: Apply ToiletSettings from prefab
/// </summary>
public class ToiletModelManager : MonoBehaviour
{
    [Header("Toilet Spawn Settings")]
    [SerializeField] private Vector3 toiletSpawnPosition = new Vector3(0, 0, 1000f);
    //[SerializeField] private Vector3 toiletRotation = new Vector3(0, 180f, 0); // ← Default (used if no settings)
    
    [Header("Trigger Settings")]
    [SerializeField] private bool createTriggerZone = true;
    [SerializeField] private Vector3 triggerSize = new Vector3(5f, 5f, 5f);
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    
    private GameObject _currentToiletModel;
    private ToiletTriggerZone _triggerZone;
    
    // ════════════════════════════════════════
    // UNITY LIFECYCLE
    // ════════════════════════════════════════
    
    void Start()
    {
        SpawnEquippedToilet();
    }
    
    // ════════════════════════════════════════
    // SPAWN TOILET - UPDATED
    // ════════════════════════════════════════
    
    /// <summary>
    /// Spawn equipped toilet - FIXED
    /// </summary>
    private void SpawnEquippedToilet()
    {
        // Clear existing
        ClearCurrentToilet();
        
        // ═══ STEP 1: Get equipped toilet ID ═══
        string equippedID = PlayerDataManager.Instance.EquippedToilet;
        
        if (showDebug)
        {
            Debug.Log($"[ToiletModel] Equipped ID: {equippedID}");
        }
        
        // ═══ STEP 2: Get ShopItemData by ID ═══
        ShopItemData equippedToilet = ShopManager.Instance?.GetItemByID(equippedID);
        
        if (equippedToilet == null)
        {
            Debug.LogError($"[ToiletModel] ❌ No toilet found for ID: {equippedID}");
            return;
        }
        
        // ═══ STEP 3: Verify it's a toilet ═══
        if (equippedToilet.itemType != ShopItemType.Toilet)
        {
            Debug.LogError($"[ToiletModel] ❌ Item {equippedID} is not a Toilet! Type: {equippedToilet.itemType}");
            return;
        }
        
        if (equippedToilet.prefab == null)
        {
            Debug.LogError($"[ToiletModel] ❌ Toilet {equippedToilet.itemName} has no prefab!");
            return;
        }
        
        // ═══ STEP 4: Calculate spawn position ═══
        Vector3 spawnPosition = toiletSpawnPosition;
        
        if (DistanceTracker.Instance != null)
        {
            float targetDistance = DistanceTracker.Instance.TargetDistance;
            spawnPosition.z = targetDistance;
            
            if (showDebug)
            {
                Debug.Log($"[ToiletModel] Spawn position from distance tracker: Z={targetDistance}m");
            }
        }
        
        // ═══ STEP 5: Spawn prefab ═══
        _currentToiletModel = Instantiate(equippedToilet.prefab, transform);
        _currentToiletModel.name = equippedToilet.itemName + "_Model";
        
        // ═══ STEP 6: Apply settings ═══
        ToiletSettings settings = _currentToiletModel.GetComponent<ToiletSettings>();
        
        if (settings != null)
        {
            settings.ApplySettings(_currentToiletModel.transform, spawnPosition);
            
            if (showDebug)
            {
                Debug.Log($"[ToiletModel] ✓ Applied ToiletSettings");
            }
        }
        else
        {
            // Fallback
            _currentToiletModel.transform.position = spawnPosition;
            //_currentToiletModel.transform.rotation = Quaternion.Euler(toiletRotation);
            
            if (showDebug)
            {
                Debug.LogWarning($"[ToiletModel] No ToiletSettings - using defaults");
            }
        }
        
        // ═══ STEP 7: Create trigger zone ═══
        if (createTriggerZone)
        {
            Vector3 finalPosition = settings != null ? 
                settings.GetFinalPosition(spawnPosition) : spawnPosition;
            
            CreateTriggerZone(finalPosition);
        }
        
        if (showDebug)
        {
            Debug.Log($"[ToiletModel] ✓ Spawned: {equippedToilet.itemName} (ID: {equippedID})");
        }
    }

    private void CreateTriggerZone(Vector3 position)
    {
        GameObject triggerObj = new GameObject("ToiletTriggerZone");
        triggerObj.transform.SetParent(transform);
        triggerObj.transform.position = position;
        
        _triggerZone = triggerObj.AddComponent<ToiletTriggerZone>();
        
        VictorySequenceController victoryController = GetComponent<VictorySequenceController>();
        
        if (victoryController != null)
        {
            _triggerZone.victoryController = victoryController;
        }
        
        if (showDebug)
        {
            Debug.Log($"[ToiletModel] ✓ Trigger zone created at {position}");
        }
    }

    private void ClearCurrentToilet()
    {
        if (_currentToiletModel != null)
        {
            Destroy(_currentToiletModel);
            _currentToiletModel = null;
        }
        
        if (_triggerZone != null)
        {
            Destroy(_triggerZone.gameObject);
            _triggerZone = null;
        }
    }

    public Vector3 GetToiletPosition()
    {
        if (_currentToiletModel != null)
        {
            return _currentToiletModel.transform.position;
        }

        return toiletSpawnPosition;
    }


    public Transform GetToiletTransform()
    {
        if (_currentToiletModel != null)
        {
            return _currentToiletModel.transform;
        }

        Debug.LogWarning("[ToiletModel] ❌ No current toilet model to get transform from.");
        return null;
        //return toiletSpawnPosition;
    }
    
    #if UNITY_EDITOR
    // void OnDrawGizmos()
    // {
    //     // Draw base spawn position
    //     Gizmos.color = Color.gray;
    //     Gizmos.DrawWireCube(toiletSpawnPosition, new Vector3(2, 3, 2));
        
    //     // Draw trigger zone preview
    //     if (createTriggerZone)
    //     {
    //         Gizmos.color = new Color(0, 1, 1, 0.3f);
    //         Gizmos.DrawWireCube(toiletSpawnPosition, triggerSize);
    //     }
        
    //     UnityEditor.Handles.Label(toiletSpawnPosition + Vector3.up * 3.5f, "🚽 BASE TOILET POSITION");
        
    //     // Draw actual toilet position if exists
    //     if (_currentToiletModel != null)
    //     {
    //         Gizmos.color = Color.cyan;
    //         Gizmos.DrawWireCube(_currentToiletModel.transform.position, new Vector3(2.5f, 3.5f, 2.5f));
            
    //         UnityEditor.Handles.color = Color.yellow;
    //         UnityEditor.Handles.Label(
    //             _currentToiletModel.transform.position + Vector3.up * 4f,
    //             "🚽 ACTUAL TOILET"
    //         );
    //     }
    // }
    #endif
}