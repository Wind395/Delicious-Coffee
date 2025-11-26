using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Endless Map Rotation Manager - Pre-load floor trước khi player tới (OPTIMIZED)
/// 
/// OPTIMIZATIONS:
/// - Cached manager references (no repeated Instance calls)
/// - Cached component references (no repeated FindObjectOfType)
/// - Conditional debug logging (no GC in production)
/// - Optimized list operations
/// - Coroutine cleanup tracking
/// - Early exit patterns
/// - OnValidate for inspector validation
/// </summary>
public class EndlessMapRotationManager : MonoBehaviour
{
    #region Singleton
    
    private static EndlessMapRotationManager _instance;
    public static EndlessMapRotationManager Instance => _instance;
    
    #endregion

    #region Serialized Fields
    
    [Header("Rotation Settings")]
    [Tooltip("Khoảng cách để đổi map (mặc định: 1000m)")]
    [SerializeField] private float mapChangeInterval = 1000f;
    
    [Tooltip("Khoảng cách trước threshold để bắt đầu pre-load (VD: 150m)")]
    [SerializeField] private float preloadDistance = 150f;
    
    [Tooltip("Hiển thị thông báo khi đổi map")]
    [SerializeField] private bool showMapChangeNotification = true;
    
    [Header("Map Pool")]
    [Tooltip("Danh sách maps để cycle (để trống = dùng tất cả maps)")]
    [SerializeField] private List<string> mapRotationPool = new List<string>();
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false; // ← Default OFF
    
    #endregion

    #region State (OPTIMIZED)
    
    private int _currentMapIndex = 0;
    private float _lastChangeDistance = 0f;
    private List<MapData> _availableMaps;
    private bool _isActive = false;
    
    // Pre-load state
    private bool _isPreloaded = false;
    private int _nextMapIndex = 0;
    
    // OPTIMIZATION: Cache manager references
    private GameModeManager _gameModeManager;
    private DistanceTracker _distanceTracker;
    private EventManager _eventManager;
    
    // OPTIMIZATION: Cache component references (no repeated FindObjectOfType)
    private TrackManager _trackManager;
    private JSONSectionSpawner _jsonSectionSpawner;
    
    // OPTIMIZATION: Track coroutines for cleanup
    private Coroutine _resumeSpawnerCoroutine;
    
    // OPTIMIZATION: Cache calculated values
    private float _nextChangeDistance;
    private float _preloadTriggerDistance;
    
    #endregion

    #region Properties (OPTIMIZED)
    
    public int CurrentMapIndex => _currentMapIndex;
    
    // OPTIMIZATION: Use cached values (no recalculation)
    public float NextChangeDistance => _nextChangeDistance;
    public float PreloadTriggerDistance => _preloadTriggerDistance;
    
    public MapData CurrentMap => _availableMaps != null && _availableMaps.Count > 0 ? 
        _availableMaps[_currentMapIndex] : null;
    
    #endregion

    #region Unity Lifecycle (OPTIMIZED)
    
    void Awake()
    {
        // Singleton setup
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    void Start()
    {
        Initialize();
    }

    /// <summary>
    /// OPTIMIZATION: Early exit if not active
    /// </summary>
    void Update()
    {
        // OPTIMIZATION: Early exit
        if (!_isActive) return;
        
        CheckMapRotation();
    }

    /// <summary>
    /// OPTIMIZATION: Validate inspector values
    /// </summary>
    void OnValidate()
    {
        // Clamp values
        mapChangeInterval = Mathf.Max(100f, mapChangeInterval);
        preloadDistance = Mathf.Clamp(preloadDistance, 50f, mapChangeInterval * 0.5f);
        
        // Update cached values if playing
        if (Application.isPlaying)
        {
            UpdateCachedDistances();
        }
    }

    void OnDestroy()
    {
        Cleanup();
    }
    
    #endregion

    #region Initialization (OPTIMIZED)
    
    /// <summary>
    /// OPTIMIZATION: Initialize and cache all references
    /// </summary>
    private void Initialize()
    {
        // Cache manager references
        _gameModeManager = GameModeManager.Instance;
        if (_gameModeManager == null)
        {
            Debug.LogError("[EndlessMapRotation] GameModeManager not found!");
            _isActive = false;
            return;
        }
        
        _distanceTracker = DistanceTracker.Instance;
        if (_distanceTracker == null)
        {
            Debug.LogWarning("[EndlessMapRotation] DistanceTracker not found!");
        }
        
        _eventManager = EventManager.Instance;
        if (_eventManager == null)
        {
            Debug.LogWarning("[EndlessMapRotation] EventManager not found!");
        }
        
        // OPTIMIZATION: Cache component references (no repeated FindObjectOfType)
        CacheComponentReferences();
        
        // Only active in Endless mode
        if (_gameModeManager.CurrentMode != GameMode.Endless)
        {
            LogDebug("[EndlessMapRotation] Not Endless mode - disabled");
            _isActive = false;
            return;
        }
        
        // Load available maps
        LoadAvailableMaps();
        
        // Set first map
        if (_availableMaps != null && _availableMaps.Count > 0)
        {
            ApplyMap(_availableMaps[0]);
            _isActive = true;
            
            // Calculate next map index
            _nextMapIndex = (_currentMapIndex + 1) % _availableMaps.Count;
            
            // Update cached distances
            UpdateCachedDistances();
            
            LogDebug($"[EndlessMapRotation] ✓ Initialized with {_availableMaps.Count} maps");
            LogDebug($"[EndlessMapRotation] Starting map: {_availableMaps[0].mapName}");
            LogDebug($"[EndlessMapRotation] Next change at: {_nextChangeDistance}m");
            LogDebug($"[EndlessMapRotation] Pre-load trigger at: {_preloadTriggerDistance}m");
        }
        else
        {
            Debug.LogError("[EndlessMapRotation] ❌ No maps available!");
            _isActive = false;
        }
    }

    /// <summary>
    /// OPTIMIZATION: Cache component references once (no repeated FindObjectOfType)
    /// </summary>
    private void CacheComponentReferences()
    {
        _trackManager = FindObjectOfType<TrackManager>();
        if (_trackManager == null)
        {
            Debug.LogWarning("[EndlessMapRotation] TrackManager not found!");
        }
        
        _jsonSectionSpawner = FindObjectOfType<JSONSectionSpawner>();
        if (_jsonSectionSpawner == null)
        {
            Debug.LogWarning("[EndlessMapRotation] JSONSectionSpawner not found!");
        }
    }

    /// <summary>
    /// OPTIMIZATION: Update cached distance calculations
    /// </summary>
    private void UpdateCachedDistances()
    {
        _nextChangeDistance = _lastChangeDistance + mapChangeInterval;
        _preloadTriggerDistance = _nextChangeDistance - preloadDistance;
    }
    
    /// <summary>
    /// OPTIMIZATION: Optimized map loading with Dictionary for fast lookup
    /// </summary>
    private void LoadAvailableMaps()
    {
        if (_gameModeManager.Database == null)
        {
            Debug.LogError("[EndlessMapRotation] LevelDatabase not found!");
            return;
        }
        
        List<MapData> allMaps = _gameModeManager.Database.GetAllMaps();
        
        if (mapRotationPool.Count > 0)
        {
            // OPTIMIZATION: Use dictionary for O(1) lookup instead of List.Find O(n)
            Dictionary<string, MapData> mapDict = new Dictionary<string, MapData>();
            foreach (MapData map in allMaps)
            {
                if (!mapDict.ContainsKey(map.mapID))
                {
                    mapDict.Add(map.mapID, map);
                }
            }
            
            _availableMaps = new List<MapData>(mapRotationPool.Count);
            
            foreach (string mapID in mapRotationPool)
            {
                if (mapDict.TryGetValue(mapID, out MapData map))
                {
                    _availableMaps.Add(map);
                }
                else
                {
                    Debug.LogWarning($"[EndlessMapRotation] Map '{mapID}' not found in database!");
                }
            }
            
            LogDebug($"[EndlessMapRotation] Using custom pool: {_availableMaps.Count} maps");
        }
        else
        {
            // Use all maps
            _availableMaps = allMaps;
            
            LogDebug($"[EndlessMapRotation] Using all maps: {_availableMaps.Count}");
        }
    }
    
    #endregion

    #region Map Rotation (OPTIMIZED)
    
    /// <summary>
    /// OPTIMIZATION: Use cached DistanceTracker and cached distances
    /// LOGIC: Giữ nguyên 100%
    /// </summary>
    private void CheckMapRotation()
    {
        // OPTIMIZATION: Use cached DistanceTracker (no Instance call)
        if (_distanceTracker == null) return;
        
        float currentDistance = _distanceTracker.CurrentDistance;
        
        // ═══ PHASE 1: PRE-LOAD MAP (LOGIC GỐC) ═══
        if (!_isPreloaded && currentDistance >= _preloadTriggerDistance)
        {
            PreloadNextMap();
        }
        
        // ═══ PHASE 2: ACTIVATE MAP (LOGIC GỐC) ═══
        if (currentDistance >= _nextChangeDistance)
        {
            ActivatePreloadedMap();
        }
    }
    
    /// <summary>
    /// LOGIC: Giữ nguyên 100%
    /// </summary>
    private void PreloadNextMap()
    {
        if (_availableMaps == null || _availableMaps.Count == 0) return;
        
        MapData nextMap = _availableMaps[_nextMapIndex];
        
        // Pre-build obstacle pools (trong background)
        PreloadObstaclePools(nextMap);
        
        _isPreloaded = true;
    }
    
    /// <summary>
    /// LOGIC: Giữ nguyên 100%
    /// </summary>
    private void PreloadObstaclePools(MapData mapData)
    {
        // Do nothing - pools already warmed up at game start!
        LogDebug("[EndlessMapRotation] ✓ Using pre-warmed pools (no creation needed)");
    }
    
    /// <summary>
    /// OPTIMIZATION: Use cached references
    /// LOGIC: Giữ nguyên 100%
    /// </summary>
    private void ActivatePreloadedMap()
    {
        if (!_isPreloaded)
        {
            Debug.LogWarning("[EndlessMapRotation] Map not pre-loaded! Forcing immediate load...");
            PreloadNextMap();
        }

        MapData nextMap = _availableMaps[_nextMapIndex];

        // ═══ STEP 1: Change floor (SMOOTH) ═══
        ChangeFloorSmooth(nextMap);

        // ═══ STEP 2: Activate obstacle set ═══
        ActivateObstacleSet(nextMap);

        // ═══ STEP 3: Update state ═══
        _lastChangeDistance = _distanceTracker.CurrentDistance;
        _currentMapIndex = _nextMapIndex;
        _nextMapIndex = (_currentMapIndex + 1) % _availableMaps.Count;
        _isPreloaded = false;
        
        // OPTIMIZATION: Update cached distances
        UpdateCachedDistances();

        // ═══ STEP 4: Show notification ═══
        if (showMapChangeNotification)
        {
            ShowMapChangeNotification(nextMap);
        }
    }

    /// <summary>
    /// OPTIMIZATION: Use cached TrackManager (no FindObjectOfType)
    /// LOGIC: Giữ nguyên 100%
    /// </summary>
    private void ChangeFloorSmooth(MapData mapData)
    {
        if (mapData.floorPrefab == null)
        {
            Debug.LogWarning($"[EndlessMapRotation] Map '{mapData.mapName}' has no floor prefab!");
            return;
        }

        // OPTIMIZATION: Use cached TrackManager
        if (_trackManager != null)
        {
            LogDebug("═══════════════════════════════");
            LogDebug("🏠 CHANGING FLOOR (SMOOTH)");
            LogDebug($"Map: {mapData.mapName}");
            LogDebug($"Floor: {mapData.floorPrefab.name}");
            LogDebug("═══════════════════════════════");

            _trackManager.SetFloorPrefabImmediate(mapData.floorPrefab);
        }
        else
        {
            Debug.LogError("[EndlessMapRotation] ❌ TrackManager not found!");
        }
    }

    /// <summary>
    /// OPTIMIZATION: Track coroutine for cleanup
    /// LOGIC: Giữ nguyên 100%
    /// </summary>
    private IEnumerator ResumeSpawnerAfterDelay(JSONSectionSpawner spawner)
    {
        yield return new WaitForSeconds(1f);

        if (spawner != null)
        {
            spawner.NotifyFloorChanging(false);
            LogDebug("[EndlessMapRotation] ✓ Spawner recycling resumed");
        }
        
        _resumeSpawnerCoroutine = null; // Clear reference
    }
    
    /// <summary>
    /// OPTIMIZATION: Use cached TrackManager
    /// LOGIC: Giữ nguyên 100%
    /// </summary>
    private void ChangeFloor(MapData mapData)
    {
        if (mapData.floorPrefab == null)
        {
            Debug.LogWarning($"[EndlessMapRotation] Map '{mapData.mapName}' has no floor prefab!");
            return;
        }
        
        // OPTIMIZATION: Use cached TrackManager
        if (_trackManager != null)
        {
            LogDebug("═══════════════════════════════");
            LogDebug("🏠 CHANGING FLOOR");
            LogDebug($"Map: {mapData.mapName}");
            LogDebug($"Floor Prefab: {mapData.floorPrefab.name}");
            LogDebug("═══════════════════════════════");
            
            _trackManager.SetFloorPrefab(mapData.floorPrefab);
        }
        else
        {
            Debug.LogError("[EndlessMapRotation] ❌ TrackManager not found!");
        }
    }
    
    /// <summary>
    /// OPTIMIZATION: Use cached JSONSectionSpawner
    /// LOGIC: Giữ nguyên 100%
    /// </summary>
    private void ActivateObstacleSet(MapData mapData)
    {
        if (mapData.obstacleVariantSet == null)
        {
            Debug.LogWarning($"[EndlessMapRotation] Map '{mapData.mapName}' has no obstacle set!");
            return;
        }
        
        // OPTIMIZATION: Use cached spawner
        if (_jsonSectionSpawner != null)
        {
            _jsonSectionSpawner.SetObstacleSetReference(mapData.obstacleVariantSet);
        }
        else
        {
            Debug.LogError("[EndlessMapRotation] ❌ JSONSectionSpawner not found!");
        }
    }
    
    /// <summary>
    /// OPTIMIZATION: Use cached references
    /// LOGIC: Giữ nguyên 100%
    /// </summary>
    private void ApplyMap(MapData mapData)
    {
        if (mapData == null)
        {
            Debug.LogError("[EndlessMapRotation] MapData is null!");
            return;
        }
        
        // Change floor
        if (mapData.floorPrefab != null && _trackManager != null)
        {
            _trackManager.SetFloorPrefab(mapData.floorPrefab);
        }
        
        // Change obstacles
        if (mapData.obstacleVariantSet != null && _jsonSectionSpawner != null)
        {
            _jsonSectionSpawner.SetObstacleSet(mapData.obstacleVariantSet);
        }
    }
    
    #endregion

    #region UI Notification (OPTIMIZED)
    
    /// <summary>
    /// OPTIMIZATION: Use cached EventManager
    /// LOGIC: Giữ nguyên 100%
    /// </summary>
    private void ShowMapChangeNotification(MapData newMap)
    {
        LogDebug($"[EndlessMapRotation] 📢 NOW ENTERING: {newMap.mapName}");
        
        // OPTIMIZATION: Use cached EventManager
        _eventManager?.TriggerEvent("OnMapChanged");
    }
    
    #endregion

    #region Public API (OPTIMIZED)
    
    /// <summary>
    /// OPTIMIZATION: Use cached values
    /// </summary>
    public string GetCurrentMapInfo()
    {
        if (CurrentMap == null)
        {
            return "No map loaded";
        }
        
        float distanceUntilNext = _nextChangeDistance - _distanceTracker.CurrentDistance;
        float distanceUntilPreload = _preloadTriggerDistance - _distanceTracker.CurrentDistance;
        
        string status = _isPreloaded ? "✓ Pre-loaded" : "Waiting";
        
        return $"Current: {CurrentMap.mapName}\n" +
               $"Next in: {distanceUntilNext:F0}m ({status})\n" +
               $"Pre-load in: {distanceUntilPreload:F0}m\n" +
               $"Map {_currentMapIndex + 1}/{_availableMaps.Count}";
    }

    /// <summary>
    /// NEW: Force refresh component references (e.g., after scene reload)
    /// </summary>
    public void RefreshComponentReferences()
    {
        CacheComponentReferences();
    }
    
    #endregion

    #region Cleanup (NEW)
    
    /// <summary>
    /// OPTIMIZATION: Proper cleanup with coroutine tracking
    /// </summary>
    private void Cleanup()
    {
        // Stop coroutines
        if (_resumeSpawnerCoroutine != null)
        {
            StopCoroutine(_resumeSpawnerCoroutine);
            _resumeSpawnerCoroutine = null;
        }
        
        StopAllCoroutines();
        
        // Clear cached references
        _gameModeManager = null;
        _distanceTracker = null;
        _eventManager = null;
        _trackManager = null;
        _jsonSectionSpawner = null;
    }
    
    #endregion

    #region Debug Helpers (CONDITIONAL)
    
    /// <summary>
    /// OPTIMIZATION: Conditional debug logging (no GC in production)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log(message);
        }
    }
    
    #endregion

    #region Debug Context Menu
    
    #if UNITY_EDITOR
    
    [ContextMenu("Debug: Print Current Map")]
    void DebugPrintCurrentMap()
    {
        Debug.Log(GetCurrentMapInfo());
    }
    
    [ContextMenu("Debug: Force Pre-load")]
    void DebugForcePreload()
    {
        if (!_isPreloaded && _availableMaps != null && _availableMaps.Count > 0)
        {
            PreloadNextMap();
        }
    }
    
    [ContextMenu("Debug: Force Activate")]
    void DebugForceActivate()
    {
        if (_availableMaps != null && _availableMaps.Count > 0)
        {
            ActivatePreloadedMap();
        }
    }

    [ContextMenu("Debug: Refresh Component References")]
    void DebugRefreshReferences()
    {
        RefreshComponentReferences();
        Debug.Log("[EndlessMapRotation] Component references refreshed");
    }
    
    #endif
    
    #endregion
}