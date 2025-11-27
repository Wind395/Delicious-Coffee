// ═══════════════════════════════════════════════════════════════
// NEW FILE: HomeModelManager.cs
// Replaces: ToiletModelManager.cs
// Purpose: Spawn home (finish line) instead of toilet
// ═══════════════════════════════════════════════════════════════

using UnityEngine;

/// <summary>
/// Home Model Manager - Spawns home at finish line
/// Replaces: ToiletModelManager
/// </summary>
public class HomeModelManager : MonoBehaviour
{
    [Header("Home Spawn Settings")]
    [SerializeField] private Vector3 homeSpawnPosition = new Vector3(0, 0, 1000f);
    
    [Header("Trigger Settings")]
    [SerializeField] private bool createTriggerZone = true;
    [SerializeField] private Vector3 triggerSize = new Vector3(5f, 5f, 5f);
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    
    private GameObject _currentHomeModel;
    private HomeTriggerZone _triggerZone;
    
    void Start()
    {
        // ═══ NEW: Check game mode ═══
        if (GameModeManager.Instance != null)
        {
            if (GameModeManager.Instance.CurrentMode == GameMode.Endless)
            {
                if (showDebug)
                {
                    Debug.Log("[HomeModel] 🔁 Endless mode detected - skipping home spawn");
                }
                return; // Don't spawn home in Endless mode
            }
        }
        
        // Level mode → Spawn home normally
        SpawnEquippedHome();
    }
    
    /// <summary>
    /// Spawn equipped home model
    /// </summary>
    private void SpawnEquippedHome()
    {
        ClearCurrentHome();
        
        // Get equipped home ID
        string equippedID = PlayerDataManager.Instance.EquippedHome;
        
        if (showDebug)
        {
            Debug.Log($"[HomeModel] Equipped ID: {equippedID}");
        }
        
        // Get ShopItemData
        ShopItemData equippedHome = ShopManager.Instance?.GetItemByID(equippedID);
        
        if (equippedHome == null)
        {
            Debug.LogError($"[HomeModel] ❌ No home found for ID: {equippedID}");
            return;
        }
        
        // Verify type
        if (equippedHome.itemType != ShopItemType.Home) // You'll need to add this to ShopItemType enum
        {
            Debug.LogError($"[HomeModel] ❌ Item is not a Home!");
            return;
        }
        
        if (equippedHome.prefab == null)
        {
            Debug.LogError($"[HomeModel] ❌ Home has no prefab!");
            return;
        }
        
        // Calculate spawn position
        Vector3 spawnPosition = homeSpawnPosition;
        
        if (DistanceTracker.Instance != null)
        {
            float targetDistance = DistanceTracker.Instance.TargetDistance;
            spawnPosition.z = targetDistance;
        }
        
        // Spawn prefab
        _currentHomeModel = Instantiate(equippedHome.prefab, transform);
        _currentHomeModel.name = equippedHome.itemName + "_Model";
        
        // Apply settings
        HomeSettings settings = _currentHomeModel.GetComponent<HomeSettings>();
        
        if (settings != null)
        {
            settings.ApplySettings(_currentHomeModel.transform, spawnPosition);
        }
        else
        {
            _currentHomeModel.transform.position = spawnPosition;
        }
        
        // Create trigger
        if (createTriggerZone)
        {
            Vector3 finalPosition = settings != null ? 
                settings.GetFinalPosition(spawnPosition) : spawnPosition;
            
            CreateTriggerZone(finalPosition);
        }
        
        if (showDebug)
        {
            Debug.Log($"[HomeModel] ✓ Spawned: {equippedHome.itemName}");
        }
    }

    private void CreateTriggerZone(Vector3 position)
    {
        GameObject triggerObj = new GameObject("HomeTriggerZone");
        triggerObj.transform.SetParent(transform);
        triggerObj.transform.position = position;
        
        _triggerZone = triggerObj.AddComponent<HomeTriggerZone>();
        
        VictorySequenceController victoryController = GetComponent<VictorySequenceController>();
        
        if (victoryController != null)
        {
            _triggerZone.victoryController = victoryController;
        }
    }

    private void ClearCurrentHome()
    {
        if (_currentHomeModel != null)
        {
            Destroy(_currentHomeModel);
            _currentHomeModel = null;
        }
        
        if (_triggerZone != null)
        {
            Destroy(_triggerZone.gameObject);
            _triggerZone = null;
        }
    }

    public Vector3 GetHomePosition()
    {
        return _currentHomeModel != null ? 
            _currentHomeModel.transform.position : homeSpawnPosition;
    }

    public Transform GetHomeTransform()
    {
        return _currentHomeModel != null ? 
            _currentHomeModel.transform : null;
    }
}