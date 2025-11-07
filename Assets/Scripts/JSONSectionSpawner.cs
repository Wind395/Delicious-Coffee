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
    [SerializeField] private bool showDebugLogs = false;
    [SerializeField] private bool showSafeZoneLogs = true; // ← NEW: Debug safe zone

    [Header("Obstacle Type Database")]
    [Tooltip("Assign ObstacleTypeDatabase - will be passed to all obstacles")]
    [SerializeField] private ObstacleTypeDatabase obstacleTypeDatabase;


    [Header("Safe Zone Settings")]
    [Tooltip("Distance before toilet where no obstacles spawn")]
    [SerializeField] private float toiletSafeZoneDistance = 100f; // 100m trước toilet
    
    [Tooltip("Force first section to be safe (no obstacles)")]
    [SerializeField] private bool forceFirstSectionSafe = true;
    

    private float _toiletPosition = 0f;
    private bool _isInToiletSafeZone = false;


    #endregion

    #region Internal State

    private Transform _playerTransform;
    private Queue<ActiveSection> _activeSections = new Queue<ActiveSection>();
    
    // Pool per prefab instance ID
    private Dictionary<int, Queue<GameObject>> _obstaclePools = new Dictionary<int, Queue<GameObject>>();
    
    private Queue<GameObject> _coinPool;
    private Queue<GameObject> _supportItemPool;

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

        // Get toilet position
        GetToiletPosition();

        // Subscribe to victory event
        if (EventManager.Instance != null)
        {
            EventManager.Instance.StartListening("OnToiletReached", OnVictory);
        }
    }
    
    /// <summary>
    /// Get toilet position from DistanceTracker
    /// </summary>
    private void GetToiletPosition()
    {
        if (DistanceTracker.Instance != null)
        {
            _toiletPosition = DistanceTracker.Instance.TargetDistance;
            
            if (showSafeZoneLogs)
            {
                Debug.Log($"[JSONSpawner] 🚽 Toilet position: {_toiletPosition}m");
                Debug.Log($"[JSONSpawner] Safe zone starts at: {_toiletPosition - toiletSafeZoneDistance}m");
            }
        }
        else
        {
            Debug.LogWarning("[JSONSpawner] DistanceTracker not found! Toilet safe zone disabled.");
        }
    }

    /// <summary>
    /// Stop spawning on victory
    /// </summary>
    private void OnVictory()
    {
        _isActive = false;
        Debug.Log("[JSONSpawner] 🚽 Victory - stopped spawning");
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

    void Initialize()
    {
        Debug.Log("[JSONSpawner] ===== INITIALIZING =====");

        // Find player
        if (GameManager.Instance != null)
        {
            _playerTransform = GameManager.Instance.GetPlayer()?.transform;
        }

        if (_playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                _playerTransform = player.transform;
        }

        // Load JSON loader
        if (jsonLoader == null)
        {
            jsonLoader = gameObject.AddComponent<JSONSectionLoader>();
        }

        if (!jsonLoader.LoadSections())
        {
            Debug.LogError("[JSONSpawner] Failed to load sections from JSON!");
            return;
        }

        jsonLoader.ValidateSections();
        
        // Initialize all pools
        InitializePools();

        _currentDifficulty = startDifficulty;

        // Spawn initial sections
        for (int i = 0; i < activeSectionsCount; i++)
        {
            SpawnNextSection();
        }

        Debug.Log($"[JSONSpawner] ✓ Initialized with {activeSectionsCount} sections");
    }

    /// <summary>
    /// Initialize all object pools - FIXED: Create pools for ALL variants
    /// </summary>
    void InitializePools()
    {
        Debug.Log("[JSONSpawner] ═══ CREATING OBJECT POOLS ═══");
        
        // ═══════════════════════════════════════════════════════
        // BARRIER OBSTACLE POOLS
        // ═══════════════════════════════════════════════════════
        if (barrierVariants != null && barrierVariants.Length > 0)
        {
            Debug.Log($"[JSONSpawner] Creating pools for {barrierVariants.Length} barrier variants");
            
            foreach (GameObject prefab in barrierVariants)
            {
                if (prefab != null)
                {
                    CreatePoolForPrefab(prefab, "Barrier");
                }
                else
                {
                    Debug.LogWarning("[JSONSpawner] Barrier variant array contains NULL prefab!");
                }
            }
        }
        else if (obstacleBarrierPrefab != null)
        {
            Debug.LogWarning("[JSONSpawner] No barrier variants assigned, using single fallback prefab");
            CreatePoolForPrefab(obstacleBarrierPrefab, "Barrier");
        }
        else
        {
            Debug.LogError("[JSONSpawner] No barrier prefabs assigned at all!");
        }

        // ═══════════════════════════════════════════════════════
        // LOW OBSTACLE POOLS
        // ═══════════════════════════════════════════════════════
        if (lowVariants != null && lowVariants.Length > 0)
        {
            Debug.Log($"[JSONSpawner] Creating pools for {lowVariants.Length} low variants");
            
            foreach (GameObject prefab in lowVariants)
            {
                if (prefab != null)
                {
                    CreatePoolForPrefab(prefab, "Low");
                }
                else
                {
                    Debug.LogWarning("[JSONSpawner] Low variant array contains NULL prefab!");
                }
            }
        }
        else if (obstacleLowPrefab != null)
        {
            Debug.LogWarning("[JSONSpawner] No low variants assigned, using single fallback prefab");
            CreatePoolForPrefab(obstacleLowPrefab, "Low");
        }
        else
        {
            Debug.LogError("[JSONSpawner] No low prefabs assigned at all!");
        }

        // ═══════════════════════════════════════════════════════
        // HIGH OBSTACLE POOLS
        // ═══════════════════════════════════════════════════════
        if (highVariants != null && highVariants.Length > 0)
        {
            Debug.Log($"[JSONSpawner] Creating pools for {highVariants.Length} high variants");
            
            foreach (GameObject prefab in highVariants)
            {
                if (prefab != null)
                {
                    CreatePoolForPrefab(prefab, "High");
                }
                else
                {
                    Debug.LogWarning("[JSONSpawner] High variant array contains NULL prefab!");
                }
            }
        }
        else if (obstacleHighPrefab != null)
        {
            Debug.LogWarning("[JSONSpawner] No high variants assigned, using single fallback prefab");
            CreatePoolForPrefab(obstacleHighPrefab, "High");
        }
        else
        {
            Debug.LogError("[JSONSpawner] No high prefabs assigned at all!");
        }

        // ═══════════════════════════════════════════════════════
        // COIN POOL
        // ═══════════════════════════════════════════════════════
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
            Debug.Log($"[JSONSpawner] ✓ Coin pool created: {poolSizePerPrefab * 3} objects");
        }
        else
        {
            Debug.LogError("[JSONSpawner] Coin prefab not assigned!");
        }

        // ═══════════════════════════════════════════════════════
        // SUPPORT ITEM POOL
        // ═══════════════════════════════════════════════════════
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
        
        Debug.Log($"[JSONSpawner] ✓ Support item pool created: {_supportItemPool.Count} objects");

        Debug.Log($"[JSONSpawner] ✓ All pools created - Total obstacle pools: {_obstaclePools.Count}");
    }

    /// <summary>
    /// Create pool for specific prefab
    /// </summary>
    void CreatePoolForPrefab(GameObject prefab, string category)
    {
        if (prefab == null)
        {
            Debug.LogError($"[JSONSpawner] Cannot create pool - prefab is null for {category}!");
            return;
        }

        int prefabID = prefab.GetInstanceID();
        
        // Check if pool already exists
        if (_obstaclePools.ContainsKey(prefabID))
        {
            Debug.LogWarning($"[JSONSpawner] Pool already exists for {prefab.name} (ID: {prefabID})");
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
        
        Debug.Log($"[JSONSpawner] ✓ Pool created: {prefab.name} [{category}] (ID: {prefabID}, Size: {poolSizePerPrefab})");
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

    /// <summary>
    /// Spawn next section - UPDATED: Safe zone check
    /// </summary>
    void SpawnNextSection()
    {
        // ═══ STEP 1: Check if entering toilet safe zone ═══
        bool wasInSafeZone = _isInToiletSafeZone;
        _isInToiletSafeZone = IsInToiletSafeZone(_nextSpawnZ);

        if (!wasInSafeZone && _isInToiletSafeZone && showSafeZoneLogs)
        {
            Debug.Log($"[JSONSpawner] 🚽 ENTERING TOILET SAFE ZONE at Z={_nextSpawnZ}");
        }

        // ═══ STEP 2: Get section data ═══
        SectionData sectionData = jsonLoader.GetRandomSection(_currentDifficulty);

        if (sectionData == null)
        {
            Debug.LogError("[JSONSpawner] No section data available!");
            return;
        }

        // ═══ STEP 3: Check if should be safe ═══
        bool isSafeSection = ShouldBeSafeSection(sectionData);

        if (isSafeSection && showSafeZoneLogs)
        {
            Debug.Log($"[JSONSpawner] 🛡️ SAFE SECTION: {sectionData.name} (Reason: {GetSafeReason(sectionData)})");
        }

        // ═══ STEP 4: Create section ═══
        ActiveSection section = new ActiveSection
        {
            data = sectionData,
            startZ = _nextSpawnZ,
            endZ = _nextSpawnZ + sectionData.length
        };

        // ═══ STEP 5: Spawn obstacles (if not safe) ═══
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
        else if (isSafeSection && showSafeZoneLogs)
        {
            Debug.Log($"[JSONSpawner] ✓ Skipped {sectionData.obstacles?.Count ?? 0} obstacles (safe zone)");
        }

        // ═══ STEP 6: Spawn coins (always) ═══
        if (sectionData.coins != null)
        {
            foreach (var coinGroup in sectionData.coins)
            {
                List<GameObject> coins = SpawnVerticalLine(coinGroup, section.startZ);
                section.spawnedObjects.AddRange(coins);
            }
        }

        // ═══ STEP 7: Spawn support items (always) ═══
        if (sectionData.supportItems != null)
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

            if (showDebugLogs)
            {
                Debug.Log($"[JSONSpawner] Difficulty increased to {_currentDifficulty}");
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"[JSONSpawner] Spawned section: {sectionData.name} at Z={section.startZ} (Safe: {isSafeSection})");
        }
    }

    /// <summary>
    /// Check if section should be safe - UPDATED
    /// </summary>
    private bool ShouldBeSafeSection(SectionData sectionData)
    {
        // Priority 1: JSON flag
        if (sectionData.isSafeZone)
        {
            return true;
        }

        // Priority 2: First section (tutorial)
        if (forceFirstSectionSafe && _sectionsSpawned == 0)
        {
            return true;
        }

        // Priority 3: Toilet safe zone (distance-based)
        if (_isInToiletSafeZone)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Check if position is in toilet safe zone
    /// </summary>
    private bool IsInToiletSafeZone(float zPosition)
    {
        if (_toiletPosition <= 0f)
        {
            return false;
        }

        float distanceToToilet = _toiletPosition - zPosition;
        return distanceToToilet <= toiletSafeZoneDistance && distanceToToilet > 0f;
    }
    
    /// <summary>
    /// Get reason why section is safe (for debugging)
    /// </summary>
    private string GetSafeReason(SectionData sectionData)
    {
        if (sectionData.isSafeZone)
            return "JSON Safe Zone Flag";
        
        if (forceFirstSectionSafe && _sectionsSpawned == 0)
            return "First Section (Tutorial)";
        
        if (_isInToiletSafeZone)
            return $"Toilet Safe Zone (Distance: {_toiletPosition - _nextSpawnZ:F0}m to toilet)";
        
        return "Unknown";
    }

    void RecycleSection(ActiveSection section)
    {
        _activeSections.Dequeue();

        foreach (var obj in section.spawnedObjects)
        {
            if (obj != null)
            {
                ReturnToPool(obj);
            }
        }

        section.spawnedObjects.Clear();
        
        if (showDebugLogs)
        {
            Debug.Log($"[JSONSpawner] Recycled section: {section.data.name}");
        }
    }

    ActiveSection GetLastSection()
    {
        if (_activeSections.Count == 0) return null;
        ActiveSection[] array = _activeSections.ToArray();
        return array[array.Length - 1];
    }

    #endregion

    #region Obstacle Spawning

    // <summary>
    /// Spawn obstacle - UPDATED: Assign database
    /// </summary>
    GameObject SpawnObstacle(ObstacleData data, float sectionStartZ)
    {
        GameObject prefab = GetRandomObstaclePrefab(data.type);

        if (prefab == null)
        {
            Debug.LogError($"[JSONSpawner] No prefab found for type: {data.type}");
            return null;
        }

        GameObject obstacle = GetFromPoolByPrefab(prefab);

        if (obstacle == null)
        {
            Debug.LogError($"[JSONSpawner] Failed to get obstacle from pool for {prefab.name}");
            return null;
        }

        // Position
        float x = (data.lane - 1) * laneDistance;
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
            
            // ═══ FIX: Assign database to obstacle ═══
            if (obstacleTypeDatabase != null)
            {
                obsScript.SetDatabase(obstacleTypeDatabase);
                Debug.Log($"[JSONSpawner] ✓ Database assigned to {prefab.name}");
            }
            else
            {
                Debug.LogWarning($"[JSONSpawner] ⚠ No database to assign to {prefab.name}");
            }

            if (showDebugLogs)
            {
                Debug.Log($"[JSONSpawner] ✓ Spawned {prefab.name} as {correctType} at lane {data.lane}, Z={z:F1}");
            }
        }

        return obstacle;
    }
    
    /// <summary>
    /// Get correct ObstacleType from prefab name - NEW
    /// Maps prefab names to their correct ObstacleType
    /// </summary>
    private ObstacleType GetObstacleTypeFromPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("[JSONSpawner] Prefab is null!");
            return ObstacleType.GenericBarrier;
        }

        string prefabName = prefab.name.ToLower().Replace("_pooled", "").Replace("(clone)", "").Trim();
        
        if (showDebugLogs)
        {
            Debug.Log($"[JSONSpawner] Mapping prefab name: '{prefabName}'");
        }

        // ═══════════════════════════════════════════════════════
        // MAP PREFAB NAMES → OBSTACLE TYPES
        // ═══════════════════════════════════════════════════════
        
        // DEADLY OBSTACLES
        if (prefabName.Contains("car"))
        {
            return ObstacleType.Car;
        }
        if (prefabName.Contains("motorcycle") || prefabName.Contains("bike"))
        {
            return ObstacleType.Motorcycle;
        }
        if (prefabName.Contains("fence") || prefabName.Contains("barrier"))
        {
            return ObstacleType.Fence;
        }
        
        // SLOW OBSTACLES
        if (prefabName.Contains("vendor") || prefabName.Contains("street"))
        {
            return ObstacleType.StreetVendor;
        }
        if (prefabName.Contains("trash") || prefabName.Contains("can"))
        {
            return ObstacleType.TrashCan;
        }
        if (prefabName.Contains("dog"))
        {
            return ObstacleType.Dog;
        }
        
        // GENERIC FALLBACK
        Debug.LogWarning($"[JSONSpawner] Unknown prefab name: {prefabName}, using GenericBarrier");
        return ObstacleType.GenericBarrier;
    }

    /// <summary>
    /// Get random obstacle prefab for category
    /// </summary>
    GameObject GetRandomObstaclePrefab(string type)
    {
        GameObject[] variants = null;
        GameObject fallback = null;
        
        switch (type.ToLower())
        {
            case "barrier":
                variants = barrierVariants;
                fallback = obstacleBarrierPrefab;
                break;
                
            case "low":
                variants = lowVariants;
                fallback = obstacleLowPrefab;
                break;
                
            case "high":
                variants = highVariants;
                fallback = obstacleHighPrefab;
                break;
                
            default:
                Debug.LogWarning($"[JSONSpawner] Unknown obstacle type: {type}");
                return obstacleBarrierPrefab;
        }
        
        // Try get from variants first
        if (variants != null && variants.Length > 0)
        {
            // Filter out null entries
            List<GameObject> validVariants = new List<GameObject>();
            
            foreach (GameObject variant in variants)
            {
                if (variant != null)
                {
                    validVariants.Add(variant);
                }
            }
            
            if (validVariants.Count > 0)
            {
                // Pick random valid variant
                int randomIndex = Random.Range(0, validVariants.Count);
                GameObject selected = validVariants[randomIndex];
                
                if (showDebugLogs)
                {
                    Debug.Log($"[JSONSpawner] Selected variant [{randomIndex}/{validVariants.Count}] {selected.name} for '{type}'");
                }
                
                return selected;
            }
            else
            {
                Debug.LogWarning($"[JSONSpawner] Variants array for '{type}' contains only NULL entries!");
            }
        }
        
        // Fallback to single prefab
        if (fallback != null)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[JSONSpawner] Using fallback prefab for '{type}': {fallback.name}");
            }
            return fallback;
        }
        
        Debug.LogError($"[JSONSpawner] No prefab available for type: {type}!");
        return null;
    }

    /// <summary>
    /// Get object from pool by prefab instance ID
    /// </summary>
    GameObject GetFromPoolByPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("[JSONSpawner] Cannot get from pool - prefab is null!");
            return null;
        }

        int prefabID = prefab.GetInstanceID();
        
        // Check if pool exists for this prefab
        if (!_obstaclePools.ContainsKey(prefabID))
        {
            Debug.LogWarning($"[JSONSpawner] No pool found for {prefab.name} (ID: {prefabID})");
            Debug.LogWarning($"[JSONSpawner] Creating emergency pool...");
            
            // Create pool on-the-fly
            CreatePoolForPrefab(prefab, "Emergency");
        }

        Queue<GameObject> pool = _obstaclePools[prefabID];
        GameObject obj = null;

        // Try get from pool
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
            
            // Validate
            if (obj == null)
            {
                Debug.LogWarning($"[JSONSpawner] Pool contained null object, creating new");
                obj = Instantiate(prefab, transform);
            }
        }
        else
        {
            // Pool empty, create new
            obj = Instantiate(prefab, transform);
            
            if (showDebugLogs)
            {
                Debug.Log($"[JSONSpawner] Pool empty for {prefab.name}, created new instance");
            }
        }

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
                Debug.LogError("[JSONSpawner] Failed to get coin from pool!");
                continue;
            }

            float z = sectionStartZ + data.zStart + (i * data.spacing);
            coinObj.transform.position = new Vector3(x, 1f, z);
            
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

    #region Support Item Spawning

    GameObject SpawnSupportItem(SupportItemData data, float sectionStartZ)
    {
        GameObject prefab = GetRandomSupportItemPrefab();
        
        if (prefab == null)
        {
            Debug.LogWarning("[JSONSpawner] No support item prefab!");
            return null;
        }

        GameObject itemObj = GetSupportItemFromPool(prefab);
        
        if (itemObj == null)
        {
            Debug.LogError("[JSONSpawner] Failed to get support item!");
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
    

    #region Cleanup Obstacles in Toilet Zone (Extra Safety)

    /// <summary>
    /// Clear obstacles in toilet zone - Extra safety measure
    /// Call this when player gets close to toilet
    /// </summary>
    public void ClearObstaclesNearToilet()
    {
        if (_toiletPosition <= 0f)
        {
            return;
        }

        int clearedCount = 0;
        
        foreach (var section in _activeSections)
        {
            if (section.startZ >= _toiletPosition - toiletSafeZoneDistance)
            {
                // Clear obstacles in this section
                foreach (var obj in section.spawnedObjects)
                {
                    if (obj != null && obj.GetComponent<Obstacle>() != null)
                    {
                        obj.SetActive(false);
                        clearedCount++;
                    }
                }
            }
        }

        if (showSafeZoneLogs && clearedCount > 0)
        {
            Debug.Log($"[JSONSpawner] 🧹 Cleared {clearedCount} obstacles near toilet");
        }
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

    #endregion

    #region Debug GUI

    // private Texture2D MakeBackgroundTexture(Color color)
    // {
    //     Texture2D texture = new Texture2D(1, 1);
    //     texture.SetPixel(0, 0, color);
    //     texture.Apply();
    //     return texture;
    // }

    #endregion

    #region Editor Helper

    #if UNITY_EDITOR
    
    [ContextMenu("Show Pool Status")]
    void ShowPoolStatus()
    {
        Debug.Log("═══ POOL STATUS ═══");
        Debug.Log($"Coins: {_coinPool?.Count ?? 0}");
        Debug.Log($"Support Items: {_supportItemPool?.Count ?? 0}");
        Debug.Log($"Obstacle Pools: {_obstaclePools?.Count ?? 0}");
        
        if (_obstaclePools != null)
        {
            foreach (var kvp in _obstaclePools)
            {
                Debug.Log($"  Pool ID {kvp.Key}: {kvp.Value.Count} objects");
            }
        }
    }

    [ContextMenu("List All Variant Prefabs")]
    void ListVariantPrefabs()
    {
        Debug.Log("═══ VARIANT PREFABS ASSIGNED ═══");
        
        Debug.Log($"\n▼ BARRIER VARIANTS: {barrierVariants?.Length ?? 0}");
        if (barrierVariants != null)
        {
            for (int i = 0; i < barrierVariants.Length; i++)
            {
                if (barrierVariants[i] != null)
                {
                    Debug.Log($"  ✓ [{i}] {barrierVariants[i].name} (ID: {barrierVariants[i].GetInstanceID()})");
                }
                else
                {
                    Debug.LogError($"  ✗ [{i}] NULL PREFAB!");
                }
            }
        }
        else
        {
            Debug.LogWarning("  Array is NULL!");
        }
        
        Debug.Log($"\n▼ LOW VARIANTS: {lowVariants?.Length ?? 0}");
        if (lowVariants != null)
        {
            for (int i = 0; i < lowVariants.Length; i++)
            {
                if (lowVariants[i] != null)
                {
                    Debug.Log($"  ✓ [{i}] {lowVariants[i].name} (ID: {lowVariants[i].GetInstanceID()})");
                }
                else
                {
                    Debug.LogError($"  ✗ [{i}] NULL PREFAB!");
                }
            }
        }
        else
        {
            Debug.LogWarning("  Array is NULL!");
        }
        
        Debug.Log($"\n▼ HIGH VARIANTS: {highVariants?.Length ?? 0}");
        if (highVariants != null)
        {
            for (int i = 0; i < highVariants.Length; i++)
            {
                if (highVariants[i] != null)
                {
                    Debug.Log($"  ✓ [{i}] {highVariants[i].name} (ID: {highVariants[i].GetInstanceID()})");
                }
                else
                {
                    Debug.LogError($"  ✗ [{i}] NULL PREFAB!");
                }
            }
        }
        else
        {
            Debug.LogWarning("  Array is NULL!");
        }
        
        Debug.Log("\n═══════════════════════════════════");
    }

    [ContextMenu("Validate Spawn Rates")]
    void ValidateSpawnRates()
    {
        float total = iceTeaRate + coldTowelRate + medicineRate;

        if (Mathf.Abs(total - 1.0f) < 0.01f)
        {
            Debug.Log($"✓ Spawn rates valid: Ice Tea {iceTeaRate * 100:F0}%, " +
                     $"Cold Towel {coldTowelRate * 100:F0}%, Medicine {medicineRate * 100:F0}%");
        }
        else
        {
            Debug.LogWarning($"⚠ Spawn rates sum to {total:F2}, should be 1.0. Will auto-normalize.");
        }
    }

#endif

    #endregion

    void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.StopListening("OnToiletReached", OnVictory);
        }
    }
    
    #region Editor Debug

    #if UNITY_EDITOR

    [ContextMenu("Debug: Show Safe Zone Info")]
    void DebugSafeZoneInfo()
    {
        Debug.Log("═══ SAFE ZONE INFO ═══");
        Debug.Log($"Toilet Position: {_toiletPosition}m");
        Debug.Log($"Safe Zone Distance: {toiletSafeZoneDistance}m");
        Debug.Log($"Safe Zone Starts At: {_toiletPosition - toiletSafeZoneDistance}m");
        Debug.Log($"Currently In Safe Zone: {_isInToiletSafeZone}");
        Debug.Log($"Next Spawn Z: {_nextSpawnZ}m");
        Debug.Log($"Sections Spawned: {_sectionsSpawned}");
        Debug.Log($"Force First Section Safe: {forceFirstSectionSafe}");
        Debug.Log("═════════════════════");
    }

    [ContextMenu("Debug: Clear Toilet Zone Now")]
    void DebugClearToiletZone()
    {
        ClearObstaclesNearToilet();
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || _toiletPosition <= 0f)
            return;

        // Draw toilet safe zone
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Vector3 safeZoneStart = new Vector3(0, 0, _toiletPosition - toiletSafeZoneDistance);
        Vector3 safeZoneEnd = new Vector3(0, 0, _toiletPosition);
        Vector3 safeZoneCenter = (safeZoneStart + safeZoneEnd) / 2f;
        Vector3 safeZoneSize = new Vector3(20, 5, toiletSafeZoneDistance);
        
        Gizmos.DrawCube(safeZoneCenter, safeZoneSize);
        
        // Draw toilet position
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(new Vector3(0, 2, _toiletPosition), 3f);
        
        // Draw labels
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(
            new Vector3(0, 5, _toiletPosition - toiletSafeZoneDistance),
            $"SAFE ZONE START\n{_toiletPosition - toiletSafeZoneDistance:F0}m"
        );
        
        UnityEditor.Handles.Label(
            new Vector3(0, 5, _toiletPosition),
            $"🚽 TOILET\n{_toiletPosition:F0}m"
        );
        #endif
    }

    #endif

    #endregion
}