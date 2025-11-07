using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Track Manager - FIXED: Proper reset on scene reload
/// </summary>
public class TrackManager : MonoBehaviour
{
    #region Serialized Fields

    [Header("Track Settings")]
    [SerializeField] private GameObject trackPrefab;
    [SerializeField] private int numberOfActiveTracks = 5;
    [SerializeField] private float trackLength = 50f;

    [Header("Optimization")]
    [SerializeField] private bool usePooling = true;
    [SerializeField] private int poolSize = 10;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    #endregion

    #region Internal State

    private List<GameObject> _activeTracks = new List<GameObject>();
    private Queue<GameObject> _trackPool = new Queue<GameObject>();
    private float _nextSpawnZ = 0f;
    private Transform _playerTransform;

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        // ← FIX 1: Reset spawn position IMMEDIATELY
        _nextSpawnZ = 0f;

        if (showDebugLogs)
        {
            Debug.Log("[TrackManager] Awake - Reset spawn position to 0");
        }
    }

    void Start()
    {
        Initialize();
    }

    void Update()
    {
        // ← FIX 2: Find player on-demand if null
        if (_playerTransform == null)
        {
            FindPlayer();
        }

        if (_playerTransform != null)
        {
            UpdateTracks();
        }
    }

    #endregion

    #region Initialization - FIXED

    /// <summary>
    /// Initialize track system - FIXED
    /// </summary>
    private void Initialize()
    {
        // ← FIX 3: CRITICAL - Reset spawn position
        _nextSpawnZ = 0f;

        // Clear old tracks (safety)
        ClearAllTracks();

        // Initialize pool if enabled
        if (usePooling)
        {
            InitializePool();
        }

        // ← FIX 4: Try to find player (but don't fail if not found)
        FindPlayer();

        // ← FIX 5: ALWAYS spawn initial tracks (don't depend on player)
        SpawnInitialTracks();

        if (showDebugLogs)
        {
            Debug.Log($"[TrackManager] ✓ Initialized with {_activeTracks.Count} tracks at Z=0");
        }
    }

    /// <summary>
    /// Find player - FIXED: Multiple fallback methods
    /// </summary>
    private void FindPlayer()
    {
        // Method 1: Via CharacterSpawner
        CharacterSpawner spawner = FindObjectOfType<CharacterSpawner>();
        if (spawner != null && spawner.CurrentPlayer != null)
        {
            _playerTransform = spawner.CurrentPlayer.transform;

            if (showDebugLogs)
                Debug.Log("[TrackManager] ✓ Found player via CharacterSpawner");
            return;
        }

        // Method 2: Via GameManager
        if (GameManager.Instance != null)
        {
            PlayerController player = GameManager.Instance.GetPlayer();
            if (player != null)
            {
                _playerTransform = player.transform;

                if (showDebugLogs)
                    Debug.Log("[TrackManager] ✓ Found player via GameManager");
                return;
            }
        }

        // Method 3: Via tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;

            if (showDebugLogs)
                Debug.Log("[TrackManager] ✓ Found player via tag");
            return;
        }

        if (showDebugLogs)
        {
            Debug.LogWarning("[TrackManager] ⚠ Player not found (will retry in Update)");
        }
    }

    /// <summary>
    /// Clear all existing tracks - FIXED
    /// </summary>
    private void ClearAllTracks()
    {
        // Destroy active tracks
        foreach (GameObject track in _activeTracks)
        {
            if (track != null)
            {
                Destroy(track);
            }
        }
        _activeTracks.Clear();

        // Clear pool
        while (_trackPool.Count > 0)
        {
            GameObject track = _trackPool.Dequeue();
            if (track != null)
            {
                Destroy(track);
            }
        }

        if (showDebugLogs)
        {
            Debug.Log("[TrackManager] Cleared all existing tracks");
        }
    }

    /// <summary>
    /// Initialize object pool
    /// </summary>
    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject track = CreateTrack();
            track.SetActive(false);
            _trackPool.Enqueue(track);
        }

        if (showDebugLogs)
        {
            Debug.Log($"[TrackManager] ✓ Pool initialized with {poolSize} tracks");
        }
    }

    /// <summary>
    /// Spawn initial tracks - FIXED: Always start from Z=0
    /// </summary>
    private void SpawnInitialTracks()
    {
        for (int i = 0; i < numberOfActiveTracks; i++)
        {
            SpawnTrack();
        }

        if (showDebugLogs)
        {
            Debug.Log($"[TrackManager] ✓ Spawned {numberOfActiveTracks} initial tracks");
        }
    }

    #endregion

    #region Track Management

    /// <summary>
    /// Update track positions - Check if need to spawn/recycle
    /// </summary>
    private void UpdateTracks()
    {
        // Check if player has passed first track
        if (_activeTracks.Count > 0)
        {
            GameObject firstTrack = _activeTracks[0];
            float trackEndZ = firstTrack.transform.position.z + trackLength;

            // If player passed the track
            if (_playerTransform.position.z > trackEndZ)
            {
                RecycleTrack();
                SpawnTrack();
            }
        }
    }

    /// <summary>
    /// Spawn new track at end
    /// </summary>
    private void SpawnTrack()
    {
        GameObject track = GetTrack();

        // ← FIX: Position at next spawn point
        Vector3 position = new Vector3(0, 0, _nextSpawnZ);
        track.transform.position = position;
        track.SetActive(true);

        // Add to active list
        _activeTracks.Add(track);

        // Update next spawn position
        _nextSpawnZ += trackLength;

        if (showDebugLogs)
        {
            Debug.Log($"[TrackManager] Spawned track at Z={position.z}, next: {_nextSpawnZ}");
        }
    }

    /// <summary>
    /// Recycle first track (move to end or return to pool)
    /// </summary>
    private void RecycleTrack()
    {
        if (_activeTracks.Count == 0) return;

        GameObject track = _activeTracks[0];
        _activeTracks.RemoveAt(0);

        if (usePooling)
        {
            track.SetActive(false);
            _trackPool.Enqueue(track);
        }
        else
        {
            track.transform.position = new Vector3(0, 0, _nextSpawnZ);
            _activeTracks.Add(track);
            _nextSpawnZ += trackLength;
        }
    }

    #endregion

    #region Track Creation

    /// <summary>
    /// Get track from pool or create new
    /// </summary>
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

    /// <summary>
    /// Create new track instance
    /// </summary>
    private GameObject CreateTrack()
    {
        GameObject track = Instantiate(trackPrefab, transform);
        track.name = $"Track_{_activeTracks.Count + _trackPool.Count}";
        return track;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Reset track system - FIXED
    /// </summary>
    public void ResetTracks()
    {
        if (showDebugLogs)
        {
            Debug.Log("[TrackManager] === RESETTING TRACKS ===");
        }

        // ← FIX: Reset spawn position
        _nextSpawnZ = 0f;

        // Clear all tracks
        ClearAllTracks();

        // Respawn initial tracks
        SpawnInitialTracks();

        if (showDebugLogs)
        {
            Debug.Log("[TrackManager] ✓ Reset complete");
        }
    }

    /// <summary>
    /// Change track material/color (for level themes)
    /// </summary>
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
                Debug.Log($"Track {i}: Z={_activeTracks[i].transform.position.z}");
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