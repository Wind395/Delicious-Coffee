using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages ranking data - Simple version
/// </summary>
public class RankingManager : MonoBehaviour
{
    #region Singleton
    
    private static RankingManager _instance;
    public static RankingManager Instance => _instance;
    
    #endregion

    #region Settings
    
    [Header("Default NPC Rankings")]
    [Tooltip("NPC names and their distances")]
    [SerializeField] private List<NPCRankingData> defaultNPCs = new List<NPCRankingData>();
    
    [Header("Settings")]
    [SerializeField] private int maxRankings = 10;
    
    #endregion

    #region State
    
    private List<RankingEntry> _rankings = new List<RankingEntry>();
    private const string SAVE_KEY = "RankingData";
    private const string PLAYER_BEST_KEY = "PlayerBestDistance";
    
    #endregion

    #region Events
    
    public event System.Action OnRankingsUpdated;
    
    #endregion

    #region Unity Lifecycle
    
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    void Start()
    {
        InitializeRankings();
    }
    
    #endregion

    #region Initialization
    
    /// <summary>
    /// Initialize rankings - Load or create default
    /// </summary>
    private void InitializeRankings()
    {
        // Try load from save
        if (LoadRankings())
        {
            Debug.Log("[RankingManager] ✓ Loaded saved rankings");
        }
        else
        {
            // First time - Create default with NPCs + You
            CreateDefaultRankings();
            Debug.Log("[RankingManager] ✓ Created default rankings");
        }
        
        // Sort
        SortRankings();
        
        // Save
        SaveRankings();
    }
    
    /// <summary>
    /// Create default rankings with NPCs + You at bottom
    /// </summary>
    private void CreateDefaultRankings()
    {
        _rankings.Clear();
        
        // Add NPCs
        foreach (var npc in defaultNPCs)
        {
            _rankings.Add(new RankingEntry(npc.name, npc.distance, false));
        }
        
        // Add You with 0 distance
        float playerBest = PlayerPrefs.GetFloat(PLAYER_BEST_KEY, 0f);
        _rankings.Add(new RankingEntry("You", playerBest, true));
        
        Debug.Log($"[RankingManager] Created {_rankings.Count} default entries");
    }
    
    #endregion

    #region Public API
    
    /// <summary>
    /// Submit player's distance
    /// </summary>
    public void SubmitPlayerDistance(float distance)
    {
        // Get current best
        float currentBest = PlayerPrefs.GetFloat(PLAYER_BEST_KEY, 0f);
        
        // Only update if new distance is better
        if (distance > currentBest)
        {
            Debug.Log($"[RankingManager] 🏆 NEW RECORD: {distance:F0}m (Previous: {currentBest:F0}m)");
            
            // Save new best
            PlayerPrefs.SetFloat(PLAYER_BEST_KEY, distance);
            PlayerPrefs.Save();
            
            // Update ranking
            UpdatePlayerRanking(distance);
        }
        else
        {
            Debug.Log($"[RankingManager] Distance {distance:F0}m (Best: {currentBest:F0}m)");
        }
    }
    
    /// <summary>
    /// Get current rankings
    /// </summary>
    public List<RankingEntry> GetRankings()
    {
        return new List<RankingEntry>(_rankings); // Return copy
    }
    
    /// <summary>
    /// Get player's rank (1-based)
    /// </summary>
    public int GetPlayerRank()
    {
        for (int i = 0; i < _rankings.Count; i++)
        {
            if (_rankings[i].isPlayer)
            {
                return i + 1;
            }
        }
        return -1;
    }
    
    /// <summary>
    /// Get player's best distance
    /// </summary>
    public float GetPlayerBestDistance()
    {
        return PlayerPrefs.GetFloat(PLAYER_BEST_KEY, 0f);
    }
    
    #endregion

    #region Internal Methods
    
    /// <summary>
    /// Update player entry in rankings
    /// </summary>
    private void UpdatePlayerRanking(float newDistance)
    {
        // Find player entry
        RankingEntry playerEntry = _rankings.Find(e => e.isPlayer);
        
        if (playerEntry != null)
        {
            // Update distance
            playerEntry.distance = newDistance;
        }
        else
        {
            // Add player if not exists
            _rankings.Add(new RankingEntry("You", newDistance, true));
        }
        
        // Sort
        SortRankings();
        
        // Trim to max
        if (_rankings.Count > maxRankings)
        {
            _rankings = _rankings.GetRange(0, maxRankings);
        }
        
        // Save
        SaveRankings();
        
        // Notify
        OnRankingsUpdated?.Invoke();
    }
    
    /// <summary>
    /// Sort rankings by distance (descending)
    /// </summary>
    private void SortRankings()
    {
        _rankings.Sort();
    }
    
    #endregion

    #region Save/Load
    
    /// <summary>
    /// Save rankings to PlayerPrefs
    /// </summary>
    private void SaveRankings()
    {
        RankingData data = new RankingData { entries = _rankings };
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
        
        Debug.Log($"[RankingManager] ✓ Saved {_rankings.Count} entries");
    }
    
    /// <summary>
    /// Load rankings from PlayerPrefs
    /// </summary>
    private bool LoadRankings()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY))
        {
            return false;
        }
        
        string json = PlayerPrefs.GetString(SAVE_KEY);
        RankingData data = JsonUtility.FromJson<RankingData>(json);
        
        if (data != null && data.entries != null && data.entries.Count > 0)
        {
            _rankings = data.entries;
            return true;
        }
        
        return false;
    }
    
    #endregion

    #region Reset
    
    /// <summary>
    /// Reset rankings to default
    /// </summary>
    public void ResetToDefault()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.DeleteKey(PLAYER_BEST_KEY);
        PlayerPrefs.Save();
        
        CreateDefaultRankings();
        SortRankings();
        SaveRankings();
        
        OnRankingsUpdated?.Invoke();
        
        Debug.Log("[RankingManager] ✓ Reset to default");
    }
    
    #endregion

    #region Debug
    
    #if UNITY_EDITOR
    
    [ContextMenu("Print Rankings")]
    private void PrintRankings()
    {
        Debug.Log("═══════════════════════════════════");
        Debug.Log("CURRENT RANKINGS");
        Debug.Log("═══════════════════════════════════");
        
        for (int i = 0; i < _rankings.Count; i++)
        {
            RankingEntry entry = _rankings[i];
            string marker = entry.isPlayer ? " ← YOU" : "";
            Debug.Log($"#{i + 1}: {entry.playerName} - {entry.GetFormattedDistance()}{marker}");
        }
        
        Debug.Log("═══════════════════════════════════");
    }
    
    [ContextMenu("Reset Rankings")]
    private void DebugReset()
    {
        ResetToDefault();
    }
    
    [ContextMenu("Submit Test Distance (500m)")]
    private void DebugSubmit500()
    {
        SubmitPlayerDistance(500f);
        PrintRankings();
    }
    
    [ContextMenu("Submit Test Distance (800m)")]
    private void DebugSubmit800()
    {
        SubmitPlayerDistance(800f);
        PrintRankings();
    }
    
    #endif
    
    #endregion

    #region Helper Classes
    
    [System.Serializable]
    private class RankingData
    {
        public List<RankingEntry> entries;
    }
    
    [System.Serializable]
    public class NPCRankingData
    {
        public string name;
        public float distance;
    }
    
    #endregion
}