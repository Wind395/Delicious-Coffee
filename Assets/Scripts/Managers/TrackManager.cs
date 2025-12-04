using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Track Manager - FIXED: Smooth floor transition without destroying visible tracks
/// </summary>
public class TrackManager : MonoBehaviour
{
    #region Serialized Fields

    [Header("Track Settings")]
    [SerializeField] private GameObject trackPrefab;
    [SerializeField] private int numberOfActiveTracks = 5;
    [SerializeField] private float trackLength = 50f;

    [Header("Safety Settings")]

    [Tooltip("Distance ahead to start spawning (meters)")]
    [SerializeField] private float spawnAheadDistance = 150f;

    [Tooltip("Distance behind to recycle tracks (meters)")]
    [SerializeField] private float recycleBehindDistance = 100f;

    [Header("Map-Based Floor")]
    [Tooltip("Floor prefab sẽ load từ MapData")]
    [SerializeField] private GameObject currentFloorPrefab;

    [Header("Optimization")]
    [SerializeField] private bool usePooling = true;
    [SerializeField] private int poolSize = 10;



    #endregion

    #region Internal State

    private List<GameObject> _activeTracks = new List<GameObject>();
    private Queue<GameObject> _trackPool = new Queue<GameObject>();
    private float _nextSpawnZ = 0f;
    private Transform _playerTransform;

    // ═══ NEW: Track floor type mixing ═══
    private Dictionary<GameObject, GameObject> _trackFloorTypes = new Dictionary<GameObject, GameObject>();

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        _nextSpawnZ = 0f;

        // if (showDebugLogs)
        // {
        //     Debug.Log("[TrackManager] Awake - Reset spawn position to 0");
        // }
    }

    void Start()
    {
        Initialize();
    }

    void Update()
    {
        if (_playerTransform == null)
        {
            FindPlayer();
            return;
        }

        if (_playerTransform != null)
        {
            UpdateTracksOptimized();
        }
    }

    private void UpdateTracksOptimized()
    {
        // Spawn tracks
        int spawned = 0;
        while (ShouldSpawnTrack() && spawned < 5)
        {
            SpawnTrack();
            spawned++;
        }

        // Recycle old tracks
        RecycleOldTracks();
    }

    private bool ShouldSpawnTrack()
    {
        if (_activeTracks.Count == 0)
        {
            return true;
        }

        GameObject lastTrack = _activeTracks[_activeTracks.Count - 1];
        if (lastTrack == null)
        {
            return true;
        }

        float lastTrackEndZ = lastTrack.transform.position.z + trackLength;
        float playerZ = _playerTransform.position.z;

        float distanceToEnd = lastTrackEndZ - playerZ;

        return distanceToEnd < spawnAheadDistance;
    }

    private void RecycleOldTracks()
    {
        if (_playerTransform == null) return;

        float playerZ = _playerTransform.position.z;
        int recycled = 0;

        for (int i = _activeTracks.Count - 1; i >= 0; i--)
        {
            GameObject track = _activeTracks[i];

            if (track == null)
            {
                _activeTracks.RemoveAt(i);
                continue;
            }

            float trackEndZ = track.transform.position.z + trackLength;

            if (trackEndZ < playerZ - recycleBehindDistance)
            {
                // ═══ REMOVE from floor type tracking ═══
                _trackFloorTypes.Remove(track);

                if (usePooling)
                {
                    track.SetActive(false);
                    _trackPool.Enqueue(track);
                }
                else
                {
                    Destroy(track);
                }

                _activeTracks.RemoveAt(i);
                recycled++;
            }
        }

        // if (showDebugLogs && recycled > 0)
        // {
        //     Debug.Log($"[TrackManager] Recycled {recycled} old tracks");
        // }
    }

    #endregion

    #region Initialization

    private void Initialize()
    {
        //.Log("[TrackManager] ===== INITIALIZING =====");

        LoadFloorPrefabFromMap();

        if (currentFloorPrefab == null)
        {
            //Debug.LogError("[TrackManager] ❌ No floor prefab available!");
            return;
        }

        _nextSpawnZ = 0f;

        ClearAllTracks();

        if (usePooling)
        {
            InitializePool();
        }

        FindPlayer();

        SpawnInitialTracks();

        // if (showDebugLogs)
        // {
        //     Debug.Log($"[TrackManager] ✓ Initialized with {_activeTracks.Count} tracks");
        //     Debug.Log($"[TrackManager] ✓ Floor: {currentFloorPrefab.name}");
        // }
    }

    private void FindPlayer()
    {
        CharacterSpawner spawner = FindAnyObjectByType<CharacterSpawner>();
        if (spawner != null && spawner.CurrentPlayer != null)
        {
            _playerTransform = spawner.CurrentPlayer.transform;

            // if (showDebugLogs)
            //     Debug.Log("[TrackManager] ✓ Found player via CharacterSpawner");
            return;
        }

        if (GameManager.Instance != null)
        {
            PlayerController player = GameManager.Instance.GetPlayer();
            if (player != null)
            {
                _playerTransform = player.transform;

                // if (showDebugLogs)
                //     Debug.Log("[TrackManager] ✓ Found player via GameManager");
                return;
            }
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;

            // if (showDebugLogs)
            //     Debug.Log("[TrackManager] ✓ Found player via tag");
            return;
        }

        // if (showDebugLogs)
        // {
        //     Debug.LogWarning("[TrackManager] ⚠ Player not found (will retry in Update)");
        // }
    }

    private void ClearAllTracks()
    {
        foreach (GameObject track in _activeTracks)
        {
            if (track != null)
            {
                Destroy(track);
            }
        }
        _activeTracks.Clear();

        while (_trackPool.Count > 0)
        {
            GameObject track = _trackPool.Dequeue();
            if (track != null)
            {
                Destroy(track);
            }
        }

        // ═══ NEW: Clear floor type tracking ═══
        _trackFloorTypes.Clear();

        // if (showDebugLogs)
        // {
        //     Debug.Log("[TrackManager] Cleared all existing tracks");
        // }
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject track = CreateTrack();
            track.SetActive(false);
            _trackPool.Enqueue(track);
        }

        // if (showDebugLogs)
        // {
        //     Debug.Log($"[TrackManager] ✓ Pool initialized with {poolSize} tracks");
        // }
    }

    private void SpawnInitialTracks()
    {
        for (int i = 0; i < numberOfActiveTracks; i++)
        {
            SpawnTrack();
        }

        // if (showDebugLogs)
        // {
        //     Debug.Log($"[TrackManager] ✓ Spawned {numberOfActiveTracks} initial tracks");
        // }
    }

    #endregion

    #region Load Floor Prefab

    private void LoadFloorPrefabFromMap()
    {
        if (GameModeManager.Instance != null)
        {
            MapData currentMap = GameModeManager.Instance.SelectedMap;
            
            if (currentMap != null && currentMap.floorPrefab != null)
            {
                currentFloorPrefab = currentMap.floorPrefab;
                
                // if (showDebugLogs)
                // {
                //     Debug.Log($"[TrackManager] ✓ Loaded floor from MapData: {currentFloorPrefab.name}");
                //     Debug.Log($"[TrackManager]   Map: {currentMap.mapName}");
                // }
                
                return;
            }
            // else if (currentMap != null)
            // {
            //     Debug.LogWarning($"[TrackManager] ⚠️ Map '{currentMap.mapName}' has no floor prefab!");
            // }
        }

        if (trackPrefab != null)
        {
            currentFloorPrefab = trackPrefab;
            
            // if (showDebugLogs)
            // {
            //     Debug.LogWarning("[TrackManager] ⚠️ Using fallback floor prefab");
            // }
        }
        // else
        // {
        //     Debug.LogError("[TrackManager] ❌ No floor prefab assigned!");
        // }
    }

    #endregion

    #region Track Management

    private void SpawnTrack()
    {
        GameObject track = GetTrack();

        Vector3 position = new Vector3(0, 0, _nextSpawnZ);
        track.transform.position = position;
        track.SetActive(true);

        _activeTracks.Add(track);

        // ═══ NEW: Track which floor type this track uses ═══
        _trackFloorTypes[track] = currentFloorPrefab;

        _nextSpawnZ += trackLength;

        // if (showDebugLogs)
        // {
        //     Debug.Log($"[TrackManager] Spawned track at Z={position.z}, next: {_nextSpawnZ}");
        // }
    }

    #endregion

    #region Floor Change - FIXED: Smooth Transition

    /// <summary>
    /// Set floor prefab IMMEDIATE - FIXED: NO destroying visible tracks
    /// </summary>
    public void SetFloorPrefabImmediate(GameObject newFloorPrefab)
    {
        if (newFloorPrefab == null)
        {
            //Debug.LogError("[TrackManager] Cannot set null floor prefab!");
            return;
        }

        if (currentFloorPrefab == newFloorPrefab)
        {
            // if (showDebugLogs)
            // {
            //     Debug.Log("[TrackManager] Same floor prefab - skipping");
            // }
            return;
        }

        // Debug.Log($"[TrackManager] ═══════════════════════════════");
        // Debug.Log($"[TrackManager] 🔄 SMOOTH FLOOR CHANGE");
        // Debug.Log($"[TrackManager] Old: {currentFloorPrefab?.name}");
        // Debug.Log($"[TrackManager] New: {newFloorPrefab.name}");
        // Debug.Log($"[TrackManager] ═══════════════════════════════");

        // ═══ JUST SWAP REFERENCE (NO REBUILD) ═══
        currentFloorPrefab = newFloorPrefab;

        // ═══ OPTIONAL: Force spawn new tracks ahead ═══
        EnsureTracksAhead();

        // Debug.Log($"[TrackManager] ✓ Floor changed - old tracks will recycle naturally");
        // Debug.Log($"[TrackManager] ✓ New tracks will use: {newFloorPrefab.name}");
    }

    /// <summary>
    /// Ensure enough tracks ahead of player - SAFE
    /// </summary>
    private void EnsureTracksAhead()
    {
        if (_playerTransform == null)
        {
            FindPlayer();
        }

        if (_playerTransform == null)
        {
            //Debug.LogWarning("[TrackManager] No player - cannot ensure tracks");
            return;
        }

        // Spawn until we have enough tracks ahead
        int spawned = 0;
        while (ShouldSpawnTrack() && spawned < 10)
        {
            SpawnTrack();
            spawned++;
        }

        // if (showDebugLogs)
        // {
        //     Debug.Log($"[TrackManager] ✓ Ensured tracks ahead: spawned {spawned}");
        // }
    }

    /// <summary>
    /// Set floor prefab (backward compatibility)
    /// </summary>
    public void SetFloorPrefab(GameObject newFloorPrefab)
    {
        SetFloorPrefabImmediate(newFloorPrefab);
    }

    #endregion

    #region Track Creation

    private GameObject GetTrack()
    {
        if (usePooling && _trackPool.Count > 0)
        {
            return _trackPool.Dequeue();
        }
        else
        {
            return CreateTrack();
        }
    }

    private GameObject CreateTrack()
    {
        if (currentFloorPrefab == null)
        {
            //Debug.LogError("[TrackManager] Cannot create track - no floor prefab!");
            return null;
        }

        GameObject track = Instantiate(currentFloorPrefab, transform);
        track.name = $"Track_{_activeTracks.Count + _trackPool.Count}_{currentFloorPrefab.name}";
        
        return track;
    }

    #endregion

    #region Public API

    public void ResetTracks()
    {
        // if (showDebugLogs)
        // {
        //     Debug.Log("[TrackManager] === RESETTING TRACKS ===");
        // }

        _nextSpawnZ = 0f;

        ClearAllTracks();

        SpawnInitialTracks();

        // if (showDebugLogs)
        // {
        //     Debug.Log("[TrackManager] ✓ Reset complete");
        // }
    }

    public void SetTrackColor(Color color)
    {
        foreach (GameObject track in _activeTracks)
        {
            Renderer renderer = track.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }
    }

    public void ReloadFloorFromMap()
    {
        LoadFloorPrefabFromMap();
        
        if (currentFloorPrefab != null)
        {
            ResetTracks();
        }
    }

    #endregion

    #region Cleanup

    void OnDestroy()
    {
        ClearAllTracks();
    }

    #endregion

    #region Debug

#if UNITY_EDITOR

    [ContextMenu("Debug: Print Track Positions")]
    void DebugPrintTracks()
    {
        Debug.Log("=== ACTIVE TRACKS ===");
        for (int i = 0; i < _activeTracks.Count; i++)
        {
            if (_activeTracks[i] != null)
            {
                GameObject floorType = _trackFloorTypes.ContainsKey(_activeTracks[i]) ? 
                    _trackFloorTypes[_activeTracks[i]] : null;
                
                Debug.Log($"Track {i}: Z={_activeTracks[i].transform.position.z}, Floor={floorType?.name}");
            }
        }
        Debug.Log($"Next Spawn Z: {_nextSpawnZ}");
        Debug.Log($"Pool Count: {_trackPool.Count}");
    }

    [ContextMenu("Debug: Force Reset")]
    void DebugReset()
    {
        ResetTracks();
    }

#endif

    #endregion
}