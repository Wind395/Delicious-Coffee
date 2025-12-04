using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// JSON Section Spawner - FIXED: Full variant support with proper pooling
/// CÁCH 1: Chỉ dùng Variants arrays (Single prefabs = fallback)
/// </summary>
public class JSONSectionSpawner : MonoBehaviour
{
    #region Serialized Fields

    [Header("JSON Loader")]
    [SerializeField] private JSONSectionLoader jsonLoader;

    [Header("Single Prefabs (Fallback - Optional)")]
    [Tooltip("Chỉ dùng nếu Variants array trống")]
    [SerializeField] private GameObject obstacleBarrierPrefab;
    [SerializeField] private GameObject obstacleLowPrefab;
    [SerializeField] private GameObject obstacleHighPrefab;
    [SerializeField] private GameObject coinPrefab;

    [Header("Obstacle Variants - PRIMARY")]
    [Tooltip("Tất cả barrier variants - Sẽ chọn random")]
    [SerializeField] private GameObject[] barrierVariants;

    [Tooltip("Tất cả low variants - Sẽ chọn random")]
    [SerializeField] private GameObject[] lowVariants;

    [Tooltip("Tất cả high variants - Sẽ chọn random")]
    [SerializeField] private GameObject[] highVariants;

    [Header("Map-Based Obstacles")]
    [Tooltip("Obstacle variant set hiện tại (load từ MapData)")]
    [SerializeField] private ObstacleVariantSet currentObstacleSet;

    [Header("Support Item Prefabs")]
    [SerializeField] private GameObject iceTeaPowerUpPrefab;
    [SerializeField] private GameObject coldTowelPowerUpPrefab;
    [SerializeField] private GameObject medicinePowerUpPrefab;

    [Header("Support Item Spawn Rates")]
    [Range(0f, 1f)]
    [SerializeField] private float iceTeaRate = 0.4f;
    
    [Range(0f, 1f)]
    [SerializeField] private float coldTowelRate = 0.4f;
    
    [Range(0f, 1f)]
    [SerializeField] private float medicineRate = 0.2f;

    [Header("Settings")]
    [SerializeField] private float laneDistance = 3f;
    [SerializeField] private int activeSectionsCount = 3;
    [SerializeField] private float spawnDistanceAhead = 50f;
    [SerializeField] private int poolSizePerPrefab = 10;
    private bool _isActive = true;

    [Header("Difficulty")]
    [SerializeField] private int startDifficulty = 1;
    [SerializeField] private int maxDifficulty = 5;
    [SerializeField] private int sectionsPerDifficultyIncrease = 5;

    [Header("Debug")]
    // [SerializeField] private bool showDebugLogs = false;

    [Header("Obstacle Type Database")]
    [Tooltip("Assign ObstacleTypeDatabase - will be passed to all obstacles")]

    [SerializeField] private ObstacleTypeDatabase obstacleTypeDatabase;

    [Header("Safe Zone Settings")]
    [Tooltip("Distance before home where no obstacles spawn")]
    [SerializeField] private float homeSafeZoneDistance = 100f;

    [Tooltip("Force first section to be safe (no obstacles)")]
    [SerializeField] private bool forceFirstSectionSafe = true;

    // ═══ NEW: Granular control ═══
    [Header("Safe Zone Content Control")]

    [Tooltip("Remove coins in safe zone")]
    [SerializeField] private bool clearCoinsInSafeZone = true;

    [Tooltip("Remove power-ups in safe zone")]
    [SerializeField] private bool clearPowerUpsInSafeZone = true;

    private float _homePosition = 0f; // CHANGED from _toiletPosition
    private bool _isInHomeSafeZone = false; // CHANGED from _isInToiletSafeZone


    #endregion

    #region Internal State

    private Transform _playerTransform;
    private Queue<ActiveSection> _activeSections = new Queue<ActiveSection>();
    
    // Pool per prefab instance ID
    private Dictionary<int, Queue<GameObject>> _obstaclePools = new Dictionary<int, Queue<GameObject>>();
    
    private Queue<GameObject> _coinPool;
    private Queue<GameObject> _supportItemPool;

    private bool _isFloorChanging = false;

    private float _nextSpawnZ = 0f;
    private int _sectionsSpawned = 0;
    private int _currentDifficulty = 1;

    private class ActiveSection
    {
        public SectionData data;
        public float startZ;
        public float endZ;
        public List<GameObject> spawnedObjects = new List<GameObject>();
    }

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        Initialize();
        GetHomePosition();

        if (EventManager.Instance != null)
        {
            EventManager.Instance.StartListening("OnHomeReached", OnVictory);
        }
    }

    private void GetHomePosition()
    {
        // ═══ CHECK: Only enable safe zone in LEVEL mode ═══
        if (GameModeManager.Instance != null && 
            GameModeManager.Instance.CurrentMode == GameMode.Endless)
        {
            _homePosition = 0f; // No home in Endless mode
            
            // if (showSafeZoneLogs)
            // {
            //     Debug.Log("[JSONSpawner] ═══════════════════════════════");
            //     Debug.Log("[JSONSpawner] ENDLESS MODE DETECTED");
            //     Debug.Log("[JSONSpawner] Home safe zone DISABLED");
            //     Debug.Log("[JSONSpawner] Obstacles will spawn infinitely");
            //     Debug.Log("[JSONSpawner] ═══════════════════════════════");
            // }
            
            return;
        }
        
        // ═══ LEVEL MODE: Enable safe zone ═══
        if (DistanceTracker.Instance != null)
        {
            _homePosition = DistanceTracker.Instance.TargetDistance;
            
            // if (showSafeZoneLogs)
            // {
            //     Debug.Log($"[JSONSpawner] ═══════════════════════════════");
            //     Debug.Log($"[JSONSpawner] LEVEL MODE DETECTED");
            //     Debug.Log($"[JSONSpawner] 🏠 Home position: {_homePosition}m");
            //     Debug.Log($"[JSONSpawner] Safe zone starts at: {_homePosition - homeSafeZoneDistance}m");
            //     Debug.Log($"[JSONSpawner] ═══════════════════════════════");
            // }
        }
        else
        {
            //Debug.LogWarning("[JSONSpawner] DistanceTracker not found! Home safe zone disabled.");
            _homePosition = 0f;
        }
    }

    private void OnVictory()
    {
        _isActive = false;
        //Debug.Log("[JSONSpawner] 🏠 Victory - stopped spawning"); // CHANGED emoji/text
    }

    void Update()
    {
        // ═══ UPDATED: Check if active ═══
        if (_playerTransform != null && _isActive)
        {
            UpdateSections();
        }
    }

    #endregion

    #region Initialization

    private void Initialize()
    {
        //Debug.Log("[JSONSpawner] ===== INITIALIZING =====");

        // ═══ STEP 1: Load obstacle set từ Map ═══
        LoadObstacleSetFromMap();

        // Find player
        if (GameManager.Instance != null)
        {
            _playerTransform = GameManager.Instance.GetPlayer().transform;
        }

        if (_playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                _playerTransform = player.GetComponent<PlayerController>().transform;
        }

        // Load JSON loader
        if (jsonLoader == null)
        {
            jsonLoader = gameObject.AddComponent<JSONSectionLoader>();
        }

        if (!jsonLoader.LoadSections())
        {
            //Debug.LogError("[JSONSpawner] Failed to load sections from JSON!");
            return;
        }

        //jsonLoader.ValidateSections();
        
        // Initialize all pools
        InitializePools();

        _currentDifficulty = startDifficulty;

        // Spawn initial sections
        for (int i = 0; i < activeSectionsCount; i++)
        {
            SpawnNextSection();
        }

        //Debug.Log($"[JSONSpawner] ✓ Initialized with {_activeSections.Count} sections");
    }

    /// <summary>
    /// Initialize all object pools - UPDATED: Use currentObstacleSet
    /// </summary>
    void InitializePools()
    {
        //Debug.Log("[JSONSpawner] ═══ CREATING OBJECT POOLS ═══");
        
        // ═══ CHECK: Use ObstacleSet or Fallback ═══
        bool useObstacleSet = (currentObstacleSet != null);
        
        if (useObstacleSet)
        {
            //Debug.Log($"[JSONSpawner] Using ObstacleSet: {currentObstacleSet.setName}");
            
            // Create pools from ObstacleVariantSet
            CreatePoolsFromObstacleSet();
        }
        else
        {
            //Debug.LogWarning("[JSONSpawner] ⚠️ Using fallback variants (deprecated)");
            
            // Create pools from legacy arrays
            CreatePoolsFromLegacyVariants();
        }
        
        // ═══ COIN POOL (unchanged) ═══
        _coinPool = new Queue<GameObject>();
        
        if (coinPrefab != null)
        {
            for (int i = 0; i < poolSizePerPrefab * 3; i++)
            {
                GameObject coin = Instantiate(coinPrefab, transform);
                coin.name = $"Coin_Pooled_{i}";
                coin.SetActive(false);
                _coinPool.Enqueue(coin);
            }
            //Debug.Log($"[JSONSpawner] ✓ Coin pool created: {poolSizePerPrefab * 3} objects");
        }

        // ═══ SUPPORT ITEM POOL (unchanged) ═══
        _supportItemPool = new Queue<GameObject>();
        
        for (int i = 0; i < 15; i++)
        {
            GameObject prefab = GetRandomSupportItemPrefab();
            if (prefab != null)
            {
                GameObject item = Instantiate(prefab, transform);
                item.name = $"{prefab.name}_Pooled_{i}";
                item.SetActive(false);
                _supportItemPool.Enqueue(item);
            }
        }
        
        //Debug.Log($"[JSONSpawner] ✓ Support item pool created: {_supportItemPool.Count} objects");
        //Debug.Log($"[JSONSpawner] ✓ All pools created - Total obstacle pools: {_obstaclePools.Count}");
    }

    /// <summary>
    /// Create pools from legacy variant arrays - DEPRECATED
    /// </summary>
    private void CreatePoolsFromLegacyVariants()
    {
        // Barrier variants
        if (barrierVariants != null)
        {
            foreach (GameObject prefab in barrierVariants)
            {
                if (prefab != null)
                {
                    CreatePoolForPrefab(prefab, "Barrier");
                }
            }
        }
        
        // Low variants
        if (lowVariants != null)
        {
            foreach (GameObject prefab in lowVariants)
            {
                if (prefab != null)
                {
                    CreatePoolForPrefab(prefab, "Low");
                }
            }
        }
        
        // High variants
        if (highVariants != null)
        {
            foreach (GameObject prefab in highVariants)
            {
                if (prefab != null)
                {
                    CreatePoolForPrefab(prefab, "High");
                }
            }
        }
    }

    /// <summary>
    /// Create pool for specific prefab
    /// </summary>
    void CreatePoolForPrefab(GameObject prefab, string category)
    {
        if (prefab == null)
        {
            //Debug.LogError($"[JSONSpawner] Cannot create pool - prefab is null for {category}!");
            return;
        }

        int prefabID = prefab.GetInstanceID();
        
        // Check if pool already exists
        if (_obstaclePools.ContainsKey(prefabID))
        {
            //Debug.LogWarning($"[JSONSpawner] Pool already exists for {prefab.name} (ID: {prefabID})");
            return;
        }

        Queue<GameObject> pool = new Queue<GameObject>();

        for (int i = 0; i < poolSizePerPrefab; i++)
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.name = $"{prefab.name}_Pooled_{i}";
            obj.SetActive(false);
            pool.Enqueue(obj);
        }

        _obstaclePools[prefabID] = pool;
        
        //Debug.Log($"[JSONSpawner] ✓ Pool created: {prefab.name} [{category}] (ID: {prefabID}, Size: {poolSizePerPrefab})");
    }

    /// <summary>
    /// Create pools from ObstacleVariantSet - NEW
    /// </summary>
    private void CreatePoolsFromObstacleSet()
    {
        if (currentObstacleSet.carVariants != null)
        {
            foreach (GameObject prefab in currentObstacleSet.carVariants)
            {
                if (prefab != null)
                {
                    CreatePoolForPrefab(prefab, "Car");
                }
            }
        }
        
        // Low
        if (currentObstacleSet.motorcycleVariants != null)
        {
            foreach (GameObject prefab in currentObstacleSet.motorcycleVariants)
            {
                if (prefab != null)
                {
                    CreatePoolForPrefab(prefab, "Motorcycle");
                }
            }
        }

        if (currentObstacleSet.streetVendorVariants != null)
        {
            foreach (GameObject prefab in currentObstacleSet.streetVendorVariants)
            {
                if (prefab != null)
                {
                    CreatePoolForPrefab(prefab, "StreetVendor");
                }
            }
        }

        if (currentObstacleSet.fenceVariants != null)
        {
            foreach (GameObject prefab in currentObstacleSet.fenceVariants)
            {
                if (prefab != null)
                {
                    CreatePoolForPrefab(prefab, "Fence");
                }
            }
        }

        if (currentObstacleSet.trashCanVariants != null)
        {
            foreach (GameObject prefab in currentObstacleSet.trashCanVariants)
            {
                if (prefab != null)
                {
                    CreatePoolForPrefab(prefab, "TrashCan");
                }
            }
        }

        if (currentObstacleSet.humanVariants != null)
        {
            foreach (GameObject prefab in currentObstacleSet.humanVariants)
            {
                if (prefab != null)
                {
                    CreatePoolForPrefab(prefab, "Human");
                }
            }
        }
    }

    #endregion

    #region Floor Change Notification - NEW

    /// <summary>
    /// Notify spawner that floor is changing - Pause section recycling
    /// </summary>
    public void NotifyFloorChanging(bool isChanging)
    {
        _isFloorChanging = isChanging;

        // if (showDebugLogs)
        // {
        //     Debug.Log($"[JSONSpawner] Floor changing: {isChanging} - Recycle {(isChanging ? "PAUSED" : "RESUMED")}");
        // }
    }

    #endregion

    #region Load Obstacle Set

    /// <summary>
    /// Load obstacle variant set từ current map
    /// </summary>
    private void LoadObstacleSetFromMap()
    {
        // ═══ METHOD 1: Get từ GameModeManager ═══
        if (GameModeManager.Instance != null)
        {
            MapData currentMap = GameModeManager.Instance.SelectedMap;
            
            if (currentMap != null && currentMap.obstacleVariantSet != null)
            {
                currentObstacleSet = currentMap.obstacleVariantSet;
                
                // if (showDebugLogs)
                // {
                //     Debug.Log($"[JSONSpawner] ✓ Loaded obstacle set: {currentObstacleSet.setName}");
                //     Debug.Log($"[JSONSpawner]   Map: {currentMap.mapName}");
                // }
                
                return;
            }
            // else if (currentMap != null)
            // {
            //     Debug.LogWarning($"[JSONSpawner] ⚠️ Map '{currentMap.mapName}' has no obstacle set!");
            // }
        }

        // ═══ METHOD 2: Fallback to assigned variants ═══
        // if (barrierVariants != null && barrierVariants.Length > 0 ||
        //     lowVariants != null && lowVariants.Length > 0 ||
        //     highVariants != null && highVariants.Length > 0)
        // {
        //     if (showDebugLogs)
        //     {
        //         Debug.LogWarning("[JSONSpawner] ⚠️ Using fallback obstacle variants (deprecated)");
        //     }
        // }
        // else
        // {
        //     Debug.LogError("[JSONSpawner] ❌ No obstacle variants available!");
        // }
    }

    #endregion

    #region Section Management

    void UpdateSections()
    {
        // Spawn new section if needed
        if (_activeSections.Count > 0)
        {
            ActiveSection lastSection = GetLastSection();
            float distanceToEnd = lastSection.endZ - _playerTransform.position.z;

            if (distanceToEnd < spawnDistanceAhead)
            {
                SpawnNextSection();
            }
        }

        // Recycle old section
        if (_activeSections.Count > 0)
        {
            ActiveSection firstSection = _activeSections.Peek();

            if (_playerTransform.position.z > firstSection.endZ + 20f)
            {
                RecycleSection(firstSection);
            }
        }
    }
    
    private bool IsInHomeSafeZone(float zPosition)
    {
        // ═══ SAFETY: DISABLED in Endless mode ═══
        if (GameModeManager.Instance != null && 
            GameModeManager.Instance.CurrentMode == GameMode.Endless)
        {
            return false; // Always return false in Endless
        }
        
        // ═══ Check home position ═══
        if (_homePosition <= 0f)
        {
            return false;
        }

        float distanceToHome = _homePosition - zPosition;
        bool isInSafeZone = distanceToHome <= homeSafeZoneDistance && distanceToHome > 0f;
        
        // Debug log when entering safe zone
        // if (isInSafeZone && showSafeZoneLogs)
        // {
        //     Debug.Log($"[JSONSpawner] Position {zPosition:F0}m is in safe zone (distance to home: {distanceToHome:F0}m)");
        // }
        
        return isInSafeZone;
    }


    /// <summary>
    /// Spawn next section - UPDATED: Safe zone skips obstacles, coins, and powerups
    /// </summary>
    void SpawnNextSection()
    {
        // Check if entering home safe zone
        bool wasInSafeZone = _isInHomeSafeZone;
        _isInHomeSafeZone = IsInHomeSafeZone(_nextSpawnZ);

        // if (!wasInSafeZone && _isInHomeSafeZone && showSafeZoneLogs)
        // {
        //     Debug.Log($"[JSONSpawner] 🏠 ENTERING HOME SAFE ZONE at Z={_nextSpawnZ}");
        // }

        SectionData sectionData = jsonLoader.GetRandomSection(_currentDifficulty);

        if (sectionData == null)
        {
            //Debug.LogError("[JSONSpawner] No section data available!");
            return;
        }

        bool isSafeSection = ShouldBeSafeSection(sectionData);

        // if (isSafeSection && showSafeZoneLogs)
        // {
        //     Debug.Log($"[JSONSpawner] 🛡️ SAFE SECTION: {sectionData.name} (Reason: {GetSafeReason(sectionData)})");
        // }

        

        // Create section
        ActiveSection section = new ActiveSection
        {
            data = sectionData,
            startZ = _nextSpawnZ,
            endZ = _nextSpawnZ + sectionData.length
        };

        // ═══ SPAWN OBSTACLES (skip if safe) ═══
        if (sectionData.obstacles != null && !isSafeSection)
        {
            foreach (var obsData in sectionData.obstacles)
            {
                GameObject obstacle = SpawnObstacle(obsData, section.startZ);
                if (obstacle != null)
                {
                    section.spawnedObjects.Add(obstacle);
                }
            }
        }
        // else if (isSafeSection && showSafeZoneLogs)
        // {
        //     Debug.Log($"[JSONSpawner] ✓ Skipped {sectionData.obstacles?.Count ?? 0} obstacles (safe zone)");
        // }

        // ═══ SPAWN COINS (conditional) ═══
    if (sectionData.coins != null && !(isSafeSection && clearCoinsInSafeZone))
    {
        foreach (var coinGroup in sectionData.coins)
        {
            List<GameObject> coins = SpawnVerticalLine(coinGroup, section.startZ);
            section.spawnedObjects.AddRange(coins);
        }
    }

    // ═══ SPAWN SUPPORT ITEMS (conditional) ═══
    if (sectionData.supportItems != null && !(isSafeSection && clearPowerUpsInSafeZone))
    {
        foreach (var itemData in sectionData.supportItems)
        {
            GameObject item = SpawnSupportItem(itemData, section.startZ);
            if (item != null)
            {
                section.spawnedObjects.Add(item);
            }
        }
    }

        _activeSections.Enqueue(section);
        _nextSpawnZ += sectionData.length;
        _sectionsSpawned++;

        // Difficulty progression
        if (_sectionsSpawned % sectionsPerDifficultyIncrease == 0)
        {
            _currentDifficulty = Mathf.Min(_currentDifficulty + 1, maxDifficulty);

            // if (showDebugLogs)
            // {
            //     Debug.Log($"[JSONSpawner] Difficulty increased to {_currentDifficulty}");
            // }
        }

        // if (showDebugLogs)
        // {
        //     Debug.Log($"[JSONSpawner] Spawned section: {sectionData.name} at Z={section.startZ} (Safe: {isSafeSection})");
        // }
    }

    private bool ShouldBeSafeSection(SectionData sectionData)
    {
        // Check JSON flag
        if (sectionData.isSafeZone)
        {
            // if (showSafeZoneLogs)
            // {
            //     Debug.Log($"[JSONSpawner] Section '{sectionData.name}' is safe (JSON flag)");
            // }
            return true;
        }

        // Check first section
        if (forceFirstSectionSafe && _sectionsSpawned == 0)
        {
            // if (showSafeZoneLogs)
            // {
            //     Debug.Log($"[JSONSpawner] First section is safe (tutorial)");
            // }
            return true;
        }

        // Check home safe zone
        if (_isInHomeSafeZone)
        {
            // if (showSafeZoneLogs)
            // {
            //     Debug.Log($"[JSONSpawner] Section at Z={_nextSpawnZ:F0}m is in HOME SAFE ZONE");
            //     Debug.Log($"[JSONSpawner] Game Mode: {GameModeManager.Instance?.CurrentMode}");
            //     Debug.Log($"[JSONSpawner] Home Position: {_homePosition:F0}m");
            // }
            return true;
        }

        return false;
    }

    /// <summary>
    /// Check if position is in toilet safe zone
    /// </summary>
    // private bool IsInToiletSafeZone(float zPosition)
    // {
    //     if (_toiletPosition <= 0f)
    //     {
    //         return false;
    //     }

    //     float distanceToToilet = _toiletPosition - zPosition;
    //     return distanceToToilet <= toiletSafeZoneDistance && distanceToToilet > 0f;
    // }
    
    /// <summary>
    /// Get reason why section is safe - UPDATED
    /// </summary>
    private string GetSafeReason(SectionData sectionData)
    {
        if (sectionData.isSafeZone)
            return "JSON Safe Zone Flag";
        
        if (forceFirstSectionSafe && _sectionsSpawned == 0)
            return "First Section (Tutorial)";
        
        if (_isInHomeSafeZone)
            return $"Home Safe Zone (Distance: {_homePosition - _nextSpawnZ:F0}m to home)"; // CHANGED text
        
        return "Unknown";
    }

    /// <summary>
    /// Recycle section - UPDATED: Extra safety for floor change
    /// </summary>
    void RecycleSection(ActiveSection section)
    {
        // ═══ SKIP RECYCLE during floor change ═══
        if (_isFloorChanging)
        {
            // if (showDebugLogs)
            // {
            //     Debug.Log($"[JSONSpawner] ⏸️ Skip recycle (floor changing): {section.data.name}");
            // }
            return;
        }

        // ═══ SAFETY: Don't recycle if player hasn't passed section end yet ═══
        if (_playerTransform != null)
        {
            float playerZ = _playerTransform.position.z;
            
            // Only recycle if player is AT LEAST 30m past section end
            if (playerZ < section.endZ + 30f)
            {
                // if (showDebugLogs)
                // {
                //     Debug.Log($"[JSONSpawner] ⏸️ Skip recycle (player too close): {section.data.name}");
                // }
                return;
            }
        }

        _activeSections.Dequeue();

        foreach (var obj in section.spawnedObjects)
        {
            if (obj != null)
            {
                ReturnToPool(obj);
            }
        }

        section.spawnedObjects.Clear();

        // if (showDebugLogs)
        // {
        //     Debug.Log($"[JSONSpawner] Recycled section: {section.data.name}");
        // }
    }

    ActiveSection GetLastSection()
    {
        if (_activeSections.Count == 0) return null;
        ActiveSection[] array = _activeSections.ToArray();
        return array[array.Length - 1];
    }

    #endregion

    #region Obstacle Spawning

    /// <summary>
    /// Spawn obstacle - UPDATED: Lane restriction validation
    /// </summary>
    GameObject SpawnObstacle(ObstacleData data, float sectionStartZ)
    {
        GameObject prefab = GetRandomObstaclePrefab(data.type);

        if (prefab == null)
        {
            //Debug.LogError($"[JSONSpawner] No prefab found for type: {data.type}");
            return null;
        }

        // ═══ NEW: Get obstacle type from prefab ═══
        ObstacleType obstacleType = GetObstacleTypeFromPrefabName(prefab.name);
        
        // ═══ NEW: Validate lane restriction ═══
        int spawnLane = data.lane;
        
        if (!IsObstacleAllowedInLane(obstacleType, spawnLane))
        {
            // Option 1: Skip obstacle (don't spawn)
            // if (showDebugLogs)
            // {
            //     Debug.LogWarning($"[JSONSpawner] ⚠ Skipped {obstacleType} at lane {spawnLane} (restricted)");
            // }
            
            // Option 2: Relocate to valid lane
            int alternativeLane = GetAlternativeLane(spawnLane);
            
            // if (showDebugLogs)
            // {
            //     Debug.Log($"[JSONSpawner] → Relocated {obstacleType} from lane {spawnLane} to lane {alternativeLane}");
            // }
            
            spawnLane = alternativeLane; // ← Use alternative lane
            
            // ═══ UNCOMMENT to skip instead of relocate: ═══
            // return null; // Skip spawning
        }

        GameObject obstacle = GetFromPoolByPrefab(prefab);

        if (obstacle == null)
        {
            //Debug.LogError($"[JSONSpawner] Failed to get obstacle from pool for {prefab.name}");
            return null;
        }

        // ← FIX: Use validated lane
        float x = (spawnLane - 1) * laneDistance;
        float z = sectionStartZ + data.zPosition;
        obstacle.transform.position = new Vector3(x, data.yPosition, z);

        // Apply ObstacleSettings
        ObstacleSettings settings = obstacle.GetComponent<ObstacleSettings>();

        if (settings != null)
        {
            settings.ApplySettings(obstacle.transform);
        }
        else
        {
            obstacle.transform.rotation = Quaternion.identity;
        }

        // Activate
        obstacle.SetActive(true);

        // Initialize obstacle script
        Obstacle obsScript = obstacle.GetComponent<Obstacle>();
        if (obsScript != null)
        {
            obsScript.Initialize(this);

            // Set ObstacleType
            ObstacleType correctType = GetObstacleTypeFromPrefab(prefab);
            obsScript.SetObstacleType(correctType);
            
            // Assign database
            if (obstacleTypeDatabase != null)
            {
                obsScript.SetDatabase(obstacleTypeDatabase);
            }

            // if (showDebugLogs)
            // {
            //     Debug.Log($"[JSONSpawner] ✓ Spawned {prefab.name} as {correctType} at lane {spawnLane}, Z={z:F1}");
            // }
        }

        return obstacle;
    }
    
    /// <summary>
    /// Get correct ObstacleType from prefab name - FIXED: Correct detection order
    /// Maps prefab names to their correct ObstacleType
    /// </summary>
    private ObstacleType GetObstacleTypeFromPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            //Debug.LogError("[JSONSpawner] Prefab is null!");
            return ObstacleType.GenericBarrier;
        }

        string prefabName = prefab.name.ToLower().Replace("_pooled", "").Replace("(clone)", "").Trim();
        
        // if (showDebugLogs)
        // {
        //     Debug.Log($"[JSONSpawner] ═══ DETECTING TYPE ═══");
        //     Debug.Log($"[JSONSpawner] Prefab: '{prefab.name}'");
        //     Debug.Log($"[JSONSpawner] Normalized: '{prefabName}'");
        // }

        // ═══════════════════════════════════════════════════════
        // PRIORITY ORDER (MOST SPECIFIC → LEAST SPECIFIC)
        // ═══════════════════════════════════════════════════════
        
        // ═══ 1. ShoppingCart (CHECK BEFORE "cart" AND "car") ═══
        if (prefabName.Contains("shopping") || prefabName.Contains("shoppingcart"))
        {
            // if (showDebugLogs)
            //     Debug.Log("[JSONSpawner] → ShoppingCart (shopping keyword)");
            return ObstacleType.ShoppingCart;
        }
        
        // ═══ 2. Cart (WITHOUT "shopping") ═══
        if (prefabName.Contains("cart"))
        {
            // if (showDebugLogs)
            //     Debug.Log("[JSONSpawner] → ShoppingCart (cart keyword)");
            return ObstacleType.ShoppingCart;
        }
        
        // ═══ 3. Car (AFTER cart check to avoid false positive) ═══
        if (prefabName.Contains("car"))
        {
            // if (showDebugLogs)
            //     Debug.Log("[JSONSpawner] → Car");
            return ObstacleType.Car;
        }
        
        // ═══ 4. Motorcycle ═══
        if (prefabName.Contains("motorcycle") || prefabName.Contains("bike") || prefabName.Contains("motorbike"))
        {
            // if (showDebugLogs)
            //     Debug.Log("[JSONSpawner] → Motorcycle");
            return ObstacleType.Motorcycle;
        }
        
        // ═══ 5. StreetVendor ═══
        if (prefabName.Contains("vendor") || prefabName.Contains("street"))
        {
            // if (showDebugLogs)
            //     Debug.Log("[JSONSpawner] → StreetVendor");
            return ObstacleType.StreetVendor;
        }
        
        // ═══ 6. Barrier (SEPARATE from Fence) ═══
        if (prefabName.Contains("barrier") || prefabName.Contains("roadbarrier"))
        {
            // ← FIX: Don't check "fence" here anymore
            // if (showDebugLogs)
            //     Debug.Log("[JSONSpawner] → Barrier");
            return ObstacleType.Barrier;
        }
        
        // ═══ 7. Fence (SEPARATE from Barrier) ═══
        if (prefabName.Contains("fence"))
        {
            // if (showDebugLogs)
            //     Debug.Log("[JSONSpawner] → Fence");
            return ObstacleType.Fence;
        }
        
        // ═══ 8. TrashCan ═══
        if (prefabName.Contains("trash") || prefabName.Contains("can") || prefabName.Contains("trashcan"))
        {
            // if (showDebugLogs)
            //     Debug.Log("[JSONSpawner] → TrashCan");
            return ObstacleType.TrashCan;
        }
        
        // ═══ 9. Human ═══
        if (prefabName.Contains("human") || prefabName.Contains("person") || prefabName.Contains("pedestrian"))
        {
            // if (showDebugLogs)
            //     Debug.Log("[JSONSpawner] → Human");
            return ObstacleType.Human;
        }
        
        // ═══ GENERIC FALLBACK ═══
        //Debug.LogWarning($"[JSONSpawner] ⚠ Unknown prefab '{prefabName}' → Using GenericBarrier");
        return ObstacleType.GenericBarrier;
    }

    /// <summary>
    /// Get random obstacle prefab by SPECIFIC TYPE NAME (car, motorcycle, etc.)
    /// UPDATED: No more "barrier/low/high", use actual obstacle names
    /// </summary>
    private GameObject GetRandomObstaclePrefab(string type)
    {
        GameObject prefab = null;
        
        // ═══ METHOD 1: Get from currentObstacleSet (PRIORITY) ═══
        if (currentObstacleSet != null)
        {
            prefab = currentObstacleSet.GetRandomVariant(type);
            
            if (prefab != null)
            {
                // if (showDebugLogs)
                // {
                //     Debug.Log($"[JSONSpawner] ✓ Selected from set: {prefab.name} (type: {type})");
                // }
                
                return prefab;
            }
            // else
            // {
            //     Debug.LogWarning($"[JSONSpawner] ⚠️ Current set '{currentObstacleSet.setName}' has no variants for type: {type}");
            // }
        }
        // else
        // {
        //     Debug.LogWarning($"[JSONSpawner] ⚠️ No current obstacle set!");
        // }
        
        // ═══ METHOD 2: Legacy fallback (DEPRECATED) ═══
        //Debug.LogWarning($"[JSONSpawner] ⚠️ Falling back to legacy prefabs for type: {type}");
        
        // Try map old generic types to specific prefabs
        string normalizedType = type.ToLower().Trim();
        
        switch (normalizedType)
        {
            case "car":
            case "high":
                return obstacleHighPrefab; // Fallback
                
            case "motorcycle":
            case "bike":
                return obstacleHighPrefab; // Fallback

            case "streetvendor":
            case "vendor":
                return obstacleLowPrefab; // Fallback
                
            case "shoppingcart":
            case "cart":
                return obstacleLowPrefab; // Fallback

            case "human":
            case "pedestrian":
                return obstacleLowPrefab; // Fallback

            case "fence":
                return obstacleBarrierPrefab; // Fallback
                
            case "barrier":
                return obstacleBarrierPrefab; // Fallback
                
            case "trashcan":
            case "trash":
                return obstacleLowPrefab; // Fallback
                
            default:
                //Debug.LogError($"[JSONSpawner] ❌ No prefab available for type: {type}!");
                return obstacleBarrierPrefab; // Last resort
        }
    }


    /// <summary>
    /// Get object from pool - CRITICAL FIX: Validate prefab matches current set
    /// </summary>
    private GameObject GetFromPoolByPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            //Debug.LogError("[JSONSpawner] ❌ Cannot get from pool - prefab is null!");
            return null;
        }

        int prefabID = prefab.GetInstanceID();
        
        // ═══ CRITICAL: Check if pool exists for THIS EXACT prefab ═══
        if (!_obstaclePools.ContainsKey(prefabID))
        {
            //Debug.LogWarning($"[JSONSpawner] ⚠️ No pool for {prefab.name} (ID: {prefabID})");
            //Debug.LogWarning($"[JSONSpawner] Creating pool on-demand...");
            
            // Create pool on-the-fly
            CreatePoolForPrefab(prefab, "OnDemand");
        }

        Queue<GameObject> pool = _obstaclePools[prefabID];
        GameObject obj = null;

        // ═══ Try get from pool ═══
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
            
            // ═══ VALIDATE: Check if object matches expected prefab ═══
            if (obj != null && obj.name.Contains(prefab.name))
            {
                // if (showDebugLogs)
                // {
                //     Debug.Log($"[JSONSpawner] ✓ Got from pool: {obj.name}");
                // }
                
                return obj;
            }
            else
            {
                //Debug.LogWarning($"[JSONSpawner] ⚠️ Pool returned wrong object! Expected: {prefab.name}, Got: {obj?.name}");
                
                // Return wrong object back to pool
                if (obj != null)
                {
                    pool.Enqueue(obj);
                }
            }
        }

        // ═══ Pool empty or wrong object → Create new ═══
        // if (showDebugLogs)
        // {
        //     Debug.Log($"[JSONSpawner] Creating new instance of: {prefab.name}");
        // }
        
        obj = Instantiate(prefab, transform);
        obj.name = $"{prefab.name}_Runtime_{Time.frameCount}";
        
        return obj;
    }


    /// <summary>
    /// Return object to pool
    /// </summary>
    void ReturnToPool(GameObject obj)
    {
        if (obj == null)
        {
            return;
        }

        // Call OnDespawn
        IPoolable poolable = obj.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.OnDespawn();
        }

        // Disable
        obj.SetActive(false);

        // Route to correct pool
        if (obj.GetComponent<Coin>() != null)
        {
            _coinPool.Enqueue(obj);
        }
        else if (obj.GetComponent<PowerUpCollectible>() != null)
        {
            _supportItemPool.Enqueue(obj);
        }
        else if (obj.GetComponent<Obstacle>() != null)
        {
            // Return to any obstacle pool (will be reused)
            foreach (var pool in _obstaclePools.Values)
            {
                pool.Enqueue(obj);
                break;
            }
        }
    }

    #endregion


    #region Set Sections File

    /// <summary>
    /// Set sections file name - Called by GameManager
    /// </summary>
    public void SetSectionsFileName(string fileName)
    {
        if (jsonLoader != null)
        {
            jsonLoader.jsonFileName = fileName;
            
            // Reload sections
            bool success = jsonLoader.LoadSections();
            
            // if (success)
            // {
            //     Debug.Log($"[JSONSpawner] ✓ Loaded sections from: {fileName}.json");
            // }
            // else
            // {
            //     Debug.LogError($"[JSONSpawner] ❌ Failed to load: {fileName}.json");
            // }
        }
    }

    #endregion


    #region Pool Warmup - NEW

    /// <summary>
    /// Create pool object for warmup (called by PoolWarmupManager)
    /// </summary>
    public void WarmupCreatePoolObject(GameObject prefab, int index)
    {
        if (prefab == null)
        {
            //Debug.LogWarning("[JSONSpawner] Cannot warmup null prefab!");
            return;
        }
        
        int prefabID = prefab.GetInstanceID();
        
        // Create pool queue if doesn't exist
        if (!_obstaclePools.ContainsKey(prefabID))
        {
            _obstaclePools[prefabID] = new Queue<GameObject>();
        }
        
        // Instantiate object
        GameObject obj = Instantiate(prefab, transform);
        obj.name = $"{prefab.name}_Pooled_{index}";
        obj.SetActive(false); // Disabled by default
        
        // Add to pool
        _obstaclePools[prefabID].Enqueue(obj);
    }

    /// <summary>
    /// Get pool statistics for debugging
    /// </summary>
    public string GetPoolStats()
    {
        int totalPools = _obstaclePools.Count;
        int totalObjects = 0;
        
        foreach (var pool in _obstaclePools.Values)
        {
            totalObjects += pool.Count;
        }
        
        return $"Pools: {totalPools}, Objects: {totalObjects}";
    }

    #endregion


    #region Lane Restriction Rules

    /// <summary>
    /// Check if obstacle type is allowed in lane - UPDATED: Added Barrier restriction
    /// </summary>
    private bool IsObstacleAllowedInLane(ObstacleType obstacleType, int lane)
    {
        // ═══ RESTRICTED OBSTACLES: Only lane 0 & 2 (CANNOT spawn in center) ═══
        bool isRestricted = obstacleType == ObstacleType.Car ||
                            obstacleType == ObstacleType.Motorcycle ||
                            obstacleType == ObstacleType.StreetVendor ||
                            obstacleType == ObstacleType.Barrier; // ← NEW: Added Barrier
        
        if (isRestricted && lane == 1)
        {
            // if (showDebugLogs)
            // {
            //     Debug.LogWarning($"[JSONSpawner] ❌ {obstacleType} NOT allowed in lane {lane} (center)");
            // }
            return false;
        }
        
        // All other obstacles (Fence, TrashCan, Human, Generic) can spawn in any lane
        return true;
    }

    /// <summary>
    /// Get alternative lane for restricted obstacle
    /// </summary>
    private int GetAlternativeLane(int invalidLane)
    {
        // If center lane (1) is invalid, randomly pick 0 or 2
        if (invalidLane == 1)
        {
            return Random.Range(0, 2) == 0 ? 0 : 2;
        }
        
        // If lane 0 or 2, no change needed
        return invalidLane;
    }


    #endregion


    #region Obstacle Type Detection - HELPER

    /// <summary>
    /// Get obstacle type from prefab name - FIXED: Better detection order
    /// </summary>
    private ObstacleType GetObstacleTypeFromPrefabName(string prefabName)
    {
        string name = prefabName.ToLower()
            .Replace("_pooled", "")
            .Replace("(clone)", "")
            .Replace("_", "")
            .Replace(" ", "")
            .Trim();
        
        // Debug.Log($"[JSONSpawner] ════════════════════════════════");
        // Debug.Log($"[JSONSpawner] DETECTING TYPE");
        // Debug.Log($"[JSONSpawner] Original: '{prefabName}'");
        // Debug.Log($"[JSONSpawner] Normalized: '{name}'");
        
        ObstacleType detectedType;
        
        // ═══ PRIORITY 1: ShoppingCart (check BEFORE "cart" substring) ═══
        if (name.Contains("shopping") || name.Contains("shoppingcart"))
        {
            detectedType = ObstacleType.ShoppingCart;
            // Debug.Log($"[JSONSpawner] → Detected: ShoppingCart (shopping)");
            // Debug.Log($"[JSONSpawner] ════════════════════════════════");
            return detectedType;
        }
        
        // ═══ PRIORITY 2: Cart (without "shopping") ═══
        if (name.Contains("cart"))
        {
            detectedType = ObstacleType.ShoppingCart;
            // Debug.Log($"[JSONSpawner] → Detected: ShoppingCart (cart)");
            // Debug.Log($"[JSONSpawner] ════════════════════════════════");
            return detectedType;
        }
        
        // ═══ PRIORITY 3: Car (after cart check) ═══
        if (name.Contains("car"))
        {
            detectedType = ObstacleType.Car;
            // Debug.Log($"[JSONSpawner] → Detected: Car");
            // Debug.Log($"[JSONSpawner] ════════════════════════════════");
            return detectedType;
        }
        
        // ═══ PRIORITY 4: Motorcycle ═══
        if (name.Contains("motorcycle") || name.Contains("cub") || name.Contains("bike") || name.Contains("motorbike"))
        {
            detectedType = ObstacleType.Motorcycle;
            // Debug.Log($"[JSONSpawner] → Detected: Motorcycle");
            // Debug.Log($"[JSONSpawner] ════════════════════════════════");
            return detectedType;
        }
        
        // ═══ PRIORITY 5: StreetVendor ═══
        if (name.Contains("vendor") || name.Contains("street") || name.Contains("streetvendor"))
        {
            detectedType = ObstacleType.StreetVendor;
            // Debug.Log($"[JSONSpawner] → Detected: StreetVendor");
            // Debug.Log($"[JSONSpawner] ════════════════════════════════");
            return detectedType;
        }
        
        // ═══ PRIORITY 6: Barrier (specific, before generic) ═══
        if ((name.Contains("barrier") || name.Contains("roadbarrier")) && !name.Contains("generic"))
        {
            detectedType = ObstacleType.Barrier;
            // Debug.Log($"[JSONSpawner] → Detected: Barrier");
            // Debug.Log($"[JSONSpawner] ════════════════════════════════");
            return detectedType;
        }
        
        // ═══ PRIORITY 7: Fence ═══
        if (name.Contains("fence"))
        {
            detectedType = ObstacleType.Fence;
            // Debug.Log($"[JSONSpawner] → Detected: Fence");
            // Debug.Log($"[JSONSpawner] ════════════════════════════════");
            return detectedType;
        }
        
        // ═══ PRIORITY 8: TrashCan ═══
        if (name.Contains("trash") || name.Contains("can") || name.Contains("trashcan"))
        {
            detectedType = ObstacleType.TrashCan;
            // Debug.Log($"[JSONSpawner] → Detected: TrashCan");
            // Debug.Log($"[JSONSpawner] ════════════════════════════════");
            return detectedType;
        }
        
        // ═══ PRIORITY 9: Human ═══
        if (name.Contains("human") || name.Contains("person") || name.Contains("pedestrian"))
        {
            detectedType = ObstacleType.Human;
            // Debug.Log($"[JSONSpawner] → Detected: Human");
            // Debug.Log($"[JSONSpawner] ════════════════════════════════");
            return detectedType;
        }
        
        // ═══ DEFAULT: Generic ═══
        detectedType = ObstacleType.GenericBarrier;
        // Debug.LogWarning($"[JSONSpawner] ⚠ Unknown prefab '{name}' → Using GenericBarrier");
        // Debug.Log($"[JSONSpawner] ════════════════════════════════");
        
        return detectedType;
    }

    #endregion


    #region Coin Spawning

    /// <summary>
    /// Spawn vertical line of coins
    /// </summary>
    List<GameObject> SpawnVerticalLine(CoinGroupData data, float sectionStartZ)
    {
        List<GameObject> coins = new List<GameObject>();
        float x = (data.lane - 1) * laneDistance;

        for (int i = 0; i < data.count; i++)
        {
            GameObject coinObj = GetFromPool(_coinPool, coinPrefab);
            
            if (coinObj == null)
            {
                //Debug.LogError("[JSONSpawner] Failed to get coin from pool!");
                continue;
            }

            float z = sectionStartZ + data.zStart + (i * data.spacing);
            coinObj.transform.position = new Vector3(x, 0, z);
            
            coinObj.SetActive(true);
            
            IPoolable poolable = coinObj.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnSpawn();
            }

            coins.Add(coinObj);
        }

        return coins;
    }

    GameObject GetFromPool(Queue<GameObject> pool, GameObject prefab)
    {
        GameObject obj = null;

        if (pool.Count > 0)
        {
            obj = pool.Dequeue();

            if (obj == null)
            {
                obj = Instantiate(prefab, transform);
            }
        }
        else
        {
            obj = Instantiate(prefab, transform);
        }

        return obj;
    }

    #endregion


    #region Obstacle Set Change

    /// <summary>
    /// Đổi obstacle variant set runtime (cho Endless map rotation)
    /// </summary>
    public void SetObstacleSet(ObstacleVariantSet newSet)
    {
        if (newSet == null)
        {
            //Debug.LogWarning("[JSONSpawner] Cannot set null obstacle set!");
            return;
        }
        
        currentObstacleSet = newSet;
        
        // if (showDebugLogs)
        // {
        //     Debug.Log($"[JSONSpawner] ✓ Obstacle set changed to: {newSet.setName}");
        // }
        
        // Rebuild obstacle pools với set mới
        RebuildObstaclePools();
    }

    /// <summary>
    /// Rebuild obstacle pools với set hiện tại
    /// </summary>
    private void RebuildObstaclePools()
    {
        if (currentObstacleSet == null)
        {
            //Debug.LogWarning("[JSONSpawner] No obstacle set to rebuild!");
            return;
        }
        
        // Clear old pools
        ClearObstaclePools();
        
        // Create new pools from current set
        CreatePoolsFromObstacleSet();
        
        // if (showDebugLogs)
        // {
        //     Debug.Log($"[JSONSpawner] ✓ Obstacle pools rebuilt ({_obstaclePools.Count} pools)");
        // }
    }

    #endregion


    #region Pre-load Obstacle Set - NEW

    private ObstacleVariantSet _preloadedObstacleSet = null; // ← NEW: Pre-loaded set

    /// <summary>
    /// Pre-load obstacle set (build pools trong background)
    /// </summary>
    public void PreloadObstacleSet(ObstacleVariantSet newSet)
    {
        if (newSet == null)
        {
            //Debug.LogWarning("[JSONSpawner] Cannot pre-load null obstacle set!");
            return;
        }
        
        // if (showDebugLogs)
        // {
        //     Debug.Log($"[JSONSpawner] 📦 Pre-loading obstacle set: {newSet.setName}");
        // }
        
        // ═══ STORE for later activation ═══
        _preloadedObstacleSet = newSet;
        
        // ═══ PRE-BUILD POOLS (trong background, không lag gameplay) ═══
        StartCoroutine(PreBuildObstaclePools(newSet));
    }

    /// <summary>
    /// Pre-build obstacle pools trong background (coroutine để tránh lag)
    /// </summary>
    private System.Collections.IEnumerator PreBuildObstaclePools(ObstacleVariantSet obstacleSet)
    {
        if (obstacleSet == null)
        {
            yield break;
        }
        
        // if (showDebugLogs)
        // {
        //     Debug.Log($"[JSONSpawner] Building pools for: {obstacleSet.setName}...");
        // }
        
        // ═══ BUILD POOLS TỪNG TÍ (spread across frames) ═══
        
        if (obstacleSet.carVariants != null)
        {
            foreach (GameObject prefab in obstacleSet.carVariants)
            {
                if (prefab != null)
                {
                    CreatePoolForPrefab(prefab, "Car");
                    yield return null; // Wait 1 frame
                }
            }
        }
        
        // Low
        if (obstacleSet.motorcycleVariants != null)
        {
            foreach (GameObject prefab in obstacleSet.motorcycleVariants)
            {
                if (prefab != null)
                {
                    CreatePoolForPrefab(prefab, "Motorcycle");
                    yield return null;
                }
            }
        }

        if (obstacleSet.streetVendorVariants != null)
        {
            foreach (GameObject prefab in obstacleSet.streetVendorVariants)
            {
                if (prefab != null)
                {
                    CreatePoolForPrefab(prefab, "StreetVendor");
                    yield return null;
                }
            }
        }

        if (obstacleSet.fenceVariants != null)
        {
            foreach (GameObject prefab in obstacleSet.fenceVariants)
            {
                if (prefab != null)
                {
                    CreatePoolForPrefab(prefab, "Fence");
                    yield return null;
                }
            }
        }

        if (obstacleSet.trashCanVariants != null)
        {
            foreach (GameObject prefab in obstacleSet.trashCanVariants)
            {
                if (prefab != null)
                {
                    CreatePoolForPrefab(prefab, "TrashCan");
                    yield return null;
                }
            }
        }

        if (obstacleSet.humanVariants != null)
        {
            foreach (GameObject prefab in obstacleSet.humanVariants)
            {
                if (prefab != null)
                {
                    CreatePoolForPrefab(prefab, "Human");
                    yield return null;
                }
            }
        }
        
        // if (showDebugLogs)
        // {
        //     Debug.Log($"[JSONSpawner] ✓ Pre-build complete: {obstacleSet.setName}");
        // }
    }

    /// <summary>
    /// Activate pre-loaded obstacle set (instant, no lag)
    /// </summary>
    public void ActivatePreloadedObstacleSet()
    {
        if (_preloadedObstacleSet == null)
        {
            Debug.LogWarning("[JSONSpawner] No pre-loaded obstacle set to activate!");
            return;
        }
        
        // if (showDebugLogs)
        // {
        //     Debug.Log($"[JSONSpawner] ✅ Activating pre-loaded set: {_preloadedObstacleSet.setName}");
        // }
        
        // ═══ SWAP to pre-loaded set (instant) ═══
        currentObstacleSet = _preloadedObstacleSet;
        UpdateVFXDictionary(); // Update dictionary reference
        
        // ═══ CLEAR pre-load state ═══
        _preloadedObstacleSet = null;
        
        // if (showDebugLogs)
        // {
        //     Debug.Log($"[JSONSpawner] ✓ Now using: {currentObstacleSet.setName}");
        // }
    }

    #region Obstacle Set Swap - OPTIMIZED

    /// <summary>
    /// Set obstacle set reference (NO pool creation - instant)
    /// </summary>
    public void SetObstacleSetReference(ObstacleVariantSet newSet)
    {
        if (newSet == null)
        {
            //Debug.LogWarning("[JSONSpawner] Cannot set null obstacle set!");
            return;
        }
        
        // ═══ JUST SWAP REFERENCE (INSTANT) ═══
        currentObstacleSet = newSet;
        
        // if (showDebugLogs)
        // {
        //     Debug.Log($"[JSONSpawner] ✓ Obstacle set reference changed to: {newSet.setName}");
        // }
        
        // NO pool creation - pools already exist from warmup!
    }

    #endregion

    /// <summary>
    /// Update VFX dictionary after set change
    /// </summary>
    private void UpdateVFXDictionary()
    {
        // Update internal dictionary mapping obstacle types to prefabs
        // This is called after currentObstacleSet changes
        UpdateVFXDictionary();
    }

    #endregion


    #region Support Item Spawning

    GameObject SpawnSupportItem(SupportItemData data, float sectionStartZ)
    {
        GameObject prefab = GetRandomSupportItemPrefab();
        
        if (prefab == null)
        {
            //Debug.LogWarning("[JSONSpawner] No support item prefab!");
            return null;
        }

        GameObject itemObj = GetSupportItemFromPool(prefab);
        
        if (itemObj == null)
        {
            //Debug.LogError("[JSONSpawner] Failed to get support item!");
            return null;
        }

        float x = (data.lane - 1) * laneDistance;
        float z = sectionStartZ + data.zPosition;
        itemObj.transform.position = new Vector3(x, 1.5f, z);
        
        PowerUpSettings settings = itemObj.GetComponent<PowerUpSettings>();
        
        if (settings != null)
        {
            settings.ApplySettings(itemObj.transform);
        }
        
        itemObj.SetActive(true);
        
        IPoolable poolable = itemObj.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.OnSpawn();
        }

        return itemObj;
    }

    GameObject GetRandomSupportItemPrefab()
    {
        float total = iceTeaRate + coldTowelRate + medicineRate;

        if (Mathf.Abs(total - 1.0f) > 0.01f)
        {
            iceTeaRate /= total;
            coldTowelRate /= total;
            medicineRate /= total;
        }

        float random = Random.value;
        float cumulative = 0f;

        cumulative += iceTeaRate;
        if (random <= cumulative && iceTeaPowerUpPrefab != null)
        {
            return iceTeaPowerUpPrefab;
        }

        cumulative += coldTowelRate;
        if (random <= cumulative && coldTowelPowerUpPrefab != null)
        {
            return coldTowelPowerUpPrefab;
        }

        if (medicinePowerUpPrefab != null)
        {
            return medicinePowerUpPrefab;
        }

        return iceTeaPowerUpPrefab;
    }

    GameObject GetSupportItemFromPool(GameObject prefab)
    {
        Queue<GameObject> tempQueue = new Queue<GameObject>();
        GameObject result = null;

        while (_supportItemPool.Count > 0)
        {
            GameObject pooledItem = _supportItemPool.Dequeue();

            if (pooledItem == null)
            {
                continue;
            }

            string pooledName = pooledItem.name.Split('(')[0].Trim();
            string prefabName = prefab.name;

            if (pooledName == prefabName || pooledName.StartsWith(prefabName))
            {
                result = pooledItem;
                break;
            }
            else
            {
                tempQueue.Enqueue(pooledItem);
            }
        }

        while (tempQueue.Count > 0)
        {
            _supportItemPool.Enqueue(tempQueue.Dequeue());
        }

        if (result == null)
        {
            result = Instantiate(prefab, transform);
            result.name = prefab.name + "_Pooled";
        }

        return result;
    }

    #endregion
    

    #region Cleanup Safe Zone - UPDATED

    /// <summary>
    /// Clear obstacles, coins, and powerups near home - Extra safety measure
    /// UPDATED: Now clears all collectibles, not just obstacles
    /// </summary>
    public void ClearObstaclesNearHome()
    {
        if (_homePosition <= 0f)
        {
            //Debug.LogWarning("[JSONSpawner] Home position not set - cannot clear safe zone");
            return;
        }

        int obstaclesCleared = 0;
        int coinsCleared = 0;
        int powerUpsCleared = 0;

        foreach (var section in _activeSections)
        {
            // Only clear sections within safe zone distance
            if (section.startZ >= _homePosition - homeSafeZoneDistance)
            {
                foreach (var obj in section.spawnedObjects)
                {
                    if (obj == null) continue;

                    // Check object type and clear accordingly
                    if (obj.GetComponent<Obstacle>() != null)
                    {
                        obj.SetActive(false);
                        obstaclesCleared++;
                    }
                    else if (obj.GetComponent<Coin>() != null)
                    {
                        obj.SetActive(false);
                        coinsCleared++;
                    }
                    else if (obj.GetComponent<PowerUpCollectible>() != null)
                    {
                        obj.SetActive(false);
                        powerUpsCleared++;
                    }
                }
            }
        }

        // if (showSafeZoneLogs && (obstaclesCleared > 0 || coinsCleared > 0 || powerUpsCleared > 0))
        // {
        //     Debug.Log($"[JSONSpawner] 🧹 SAFE ZONE CLEARED:");
        //     Debug.Log($"[JSONSpawner]   - Obstacles: {obstaclesCleared}");
        //     Debug.Log($"[JSONSpawner]   - Coins: {coinsCleared}");
        //     Debug.Log($"[JSONSpawner]   - PowerUps: {powerUpsCleared}");
        //     Debug.Log($"[JSONSpawner]   - Total: {obstaclesCleared + coinsCleared + powerUpsCleared}");
        // }
    }

    /// <summary>
    /// Clear specific object type in safe zone - Helper method
    /// </summary>
    private void ClearObjectTypeInSafeZone<T>() where T : Component
    {
        if (_homePosition <= 0f) return;

        int clearedCount = 0;

        foreach (var section in _activeSections)
        {
            if (section.startZ >= _homePosition - homeSafeZoneDistance)
            {
                foreach (var obj in section.spawnedObjects)
                {
                    if (obj != null && obj.GetComponent<T>() != null)
                    {
                        obj.SetActive(false);
                        clearedCount++;
                    }
                }
            }
        }

        // if (showSafeZoneLogs && clearedCount > 0)
        // {
        //     Debug.Log($"[JSONSpawner] 🧹 Cleared {clearedCount} {typeof(T).Name} objects in safe zone");
        // }
    }

    /// <summary>
    /// Alternative API: Clear each type separately
    /// </summary>
    public void ClearSafeZoneObstacles()
    {
        ClearObjectTypeInSafeZone<Obstacle>();
    }

    public void ClearSafeZoneCoins()
    {
        ClearObjectTypeInSafeZone<Coin>();
    }

    public void ClearSafeZonePowerUps()
    {
        ClearObjectTypeInSafeZone<PowerUpCollectible>();
    }

    /// <summary>
    /// Clear all objects in safe zone (obstacles + coins + powerups)
    /// </summary>
    public void ClearAllInSafeZone()
    {
        if (_homePosition <= 0f)
        {
            //Debug.LogWarning("[JSONSpawner] Home position not set!");
            return;
        }

        // if (showSafeZoneLogs)
        // {
        //     Debug.Log($"[JSONSpawner] 🧹 Clearing ENTIRE safe zone (distance: {homeSafeZoneDistance}m)");
        // }

        ClearSafeZoneObstacles();
        ClearSafeZoneCoins();
        ClearSafeZonePowerUps();
    }

    #endregion



    #region Public API

    public void ReturnCoinToPool(GameObject coin)
    {
        ReturnToPool(coin);
    }

    public void ReturnObstacleToPool(GameObject obstacle, GameObject prefab)
    {
        ReturnToPool(obstacle);
    }

    /// <summary>
    /// Reload obstacle set từ current map
    /// </summary>
    public void ReloadObstacleSetFromMap()
    {
        LoadObstacleSetFromMap();
        
        // Rebuild pools
        if (currentObstacleSet != null)
        {
            // Clear old pools
            ClearObstaclePools();
            
            // Rebuild
            CreatePoolsFromObstacleSet();
            
            //Debug.Log($"[JSONSpawner] ✓ Obstacle pools rebuilt for: {currentObstacleSet.setName}");
        }
    }

    /// <summary>
    /// Clear obstacle pools - Helper
    /// </summary>
    private void ClearObstaclePools()
    {
        foreach (var pool in _obstaclePools.Values)
        {
            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }
        
        _obstaclePools.Clear();
    }

    #endregion

//     #region Debug GUI

//     // private Texture2D MakeBackgroundTexture(Color color)
//     // {
//     //     Texture2D texture = new Texture2D(1, 1);
//     //     texture.SetPixel(0, 0, color);
//     //     texture.Apply();
//     //     return texture;
//     // }

//     #endregion

//     #region Editor Helper

//     #if UNITY_EDITOR
    
//     [ContextMenu("Show Pool Status")]
//     void ShowPoolStatus()
//     {
//         Debug.Log("═══ POOL STATUS ═══");
//         Debug.Log($"Coins: {_coinPool?.Count ?? 0}");
//         Debug.Log($"Support Items: {_supportItemPool?.Count ?? 0}");
//         Debug.Log($"Obstacle Pools: {_obstaclePools?.Count ?? 0}");
        
//         if (_obstaclePools != null)
//         {
//             foreach (var kvp in _obstaclePools)
//             {
//                 Debug.Log($"  Pool ID {kvp.Key}: {kvp.Value.Count} objects");
//             }
//         }
//     }

//     [ContextMenu("List All Variant Prefabs")]
//     void ListVariantPrefabs()
//     {
//         Debug.Log("═══ VARIANT PREFABS ASSIGNED ═══");
        
//         Debug.Log($"\n▼ BARRIER VARIANTS: {barrierVariants?.Length ?? 0}");
//         if (barrierVariants != null)
//         {
//             for (int i = 0; i < barrierVariants.Length; i++)
//             {
//                 if (barrierVariants[i] != null)
//                 {
//                     Debug.Log($"  ✓ [{i}] {barrierVariants[i].name} (ID: {barrierVariants[i].GetInstanceID()})");
//                 }
//                 else
//                 {
//                     Debug.LogError($"  ✗ [{i}] NULL PREFAB!");
//                 }
//             }
//         }
//         else
//         {
//             Debug.LogWarning("  Array is NULL!");
//         }
        
//         Debug.Log($"\n▼ LOW VARIANTS: {lowVariants?.Length ?? 0}");
//         if (lowVariants != null)
//         {
//             for (int i = 0; i < lowVariants.Length; i++)
//             {
//                 if (lowVariants[i] != null)
//                 {
//                     Debug.Log($"  ✓ [{i}] {lowVariants[i].name} (ID: {lowVariants[i].GetInstanceID()})");
//                 }
//                 else
//                 {
//                     Debug.LogError($"  ✗ [{i}] NULL PREFAB!");
//                 }
//             }
//         }
//         else
//         {
//             Debug.LogWarning("  Array is NULL!");
//         }
        
//         Debug.Log($"\n▼ HIGH VARIANTS: {highVariants?.Length ?? 0}");
//         if (highVariants != null)
//         {
//             for (int i = 0; i < highVariants.Length; i++)
//             {
//                 if (highVariants[i] != null)
//                 {
//                     Debug.Log($"  ✓ [{i}] {highVariants[i].name} (ID: {highVariants[i].GetInstanceID()})");
//                 }
//                 else
//                 {
//                     Debug.LogError($"  ✗ [{i}] NULL PREFAB!");
//                 }
//             }
//         }
//         else
//         {
//             Debug.LogWarning("  Array is NULL!");
//         }
        
//         Debug.Log("\n═══════════════════════════════════");
//     }

//     [ContextMenu("Validate Spawn Rates")]
//     void ValidateSpawnRates()
//     {
//         float total = iceTeaRate + coldTowelRate + medicineRate;

//         if (Mathf.Abs(total - 1.0f) < 0.01f)
//         {
//             Debug.Log($"✓ Spawn rates valid: Ice Tea {iceTeaRate * 100:F0}%, " +
//                      $"Cold Towel {coldTowelRate * 100:F0}%, Medicine {medicineRate * 100:F0}%");
//         }
//         else
//         {
//             Debug.LogWarning($"⚠ Spawn rates sum to {total:F2}, should be 1.0. Will auto-normalize.");
//         }
//     }

// #endif

//     #endregion

    void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.StopListening("OnHomeReached", OnVictory); // CHANGED event name
        }
    }
    
    #region Editor Debug

    #if UNITY_EDITOR

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || _homePosition <= 0f) // CHANGED variable name
            return;

        // Draw home safe zone
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Vector3 safeZoneStart = new Vector3(0, 0, _homePosition - homeSafeZoneDistance);
        Vector3 safeZoneEnd = new Vector3(0, 0, _homePosition);
        Vector3 safeZoneCenter = (safeZoneStart + safeZoneEnd) / 2f;
        Vector3 safeZoneSize = new Vector3(20, 5, homeSafeZoneDistance);
        
        Gizmos.DrawCube(safeZoneCenter, safeZoneSize);
        
        // Draw home position
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(new Vector3(0, 2, _homePosition), 3f);
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(
            new Vector3(0, 5, _homePosition - homeSafeZoneDistance),
            $"SAFE ZONE START\n{_homePosition - homeSafeZoneDistance:F0}m"
        );
        
        UnityEditor.Handles.Label(
            new Vector3(0, 5, _homePosition),
            $"🏠 HOME\n{_homePosition:F0}m" // CHANGED from toilet to home
        );
        #endif
    }
    #endif

    #endregion
}