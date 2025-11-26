using UnityEngine;

/// <summary>
/// Game Mode Manager - Quản lý mode hiện tại
/// </summary>
public class GameModeManager : MonoBehaviour
{
    #region Singleton
    
    private static GameModeManager _instance;
    public static GameModeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("GameModeManager");
                _instance = go.AddComponent<GameModeManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    
    #endregion

    #region Serialized Fields
    
    [Header("Database")]
    [SerializeField] private LevelDatabase levelDatabase;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    #endregion

    #region State
    
    private GameMode _currentMode;
    private MapData _selectedMap;
    private LevelData _selectedLevel;
    
    #endregion

    #region Properties
    
    public GameMode CurrentMode => _currentMode;
    public MapData SelectedMap => _selectedMap;
    public LevelData SelectedLevel => _selectedLevel;
    public LevelDatabase Database => levelDatabase;
    
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
        
        // Load database from Resources if not assigned
        if (levelDatabase == null)
        {
            levelDatabase = Resources.Load<LevelDatabase>("LevelDatabase");
            
            if (levelDatabase == null)
            {
                Debug.LogError("[GameModeManager] ❌ LevelDatabase not found in Resources!");
            }
        }
    }
    
    #endregion

    #region Mode Selection
    
    /// <summary>
    /// Set mode to Level
    /// </summary>
    public void SetLevelMode()
    {
        _currentMode = GameMode.Level;
        
        if (showDebugLogs)
        {
            Debug.Log("[GameModeManager] 🎮 Mode: LEVEL");
        }
    }
    
    /// <summary>
    /// Set mode to Endless
    /// </summary>
    public void SetEndlessMode()
    {
        _currentMode = GameMode.Endless;
        
        if (showDebugLogs)
        {
            Debug.Log("[GameModeManager] 🎮 Mode: ENDLESS");
        }
    }
    
    #endregion

    #region Map/Level Selection
    
    /// <summary>
    /// Select map (for Level mode)
    /// </summary>
    public void SelectMap(string mapID)
    {
        _selectedMap = levelDatabase?.GetMap(mapID);
        
        if (_selectedMap == null)
        {
            Debug.LogError($"[GameModeManager] ❌ Map not found: {mapID}");
            return;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[GameModeManager] 🗺️ Map selected: {_selectedMap.mapName}");
        }
    }
    
    /// <summary>
    /// Select level (for Level mode)
    /// </summary>
    public void SelectLevel(string levelID)
    {
        _selectedLevel = levelDatabase?.GetLevel(levelID);
        
        if (_selectedLevel == null)
        {
            Debug.LogError($"[GameModeManager] ❌ Level not found: {levelID}");
            return;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[GameModeManager] 📍 Level selected: {_selectedLevel.levelName}");
            Debug.Log($"[GameModeManager]   Distance: {_selectedLevel.targetDistance}m");
            Debug.Log($"[GameModeManager]   JSON: {_selectedLevel.sectionsFileName}.json");
        }
    }
    
    /// <summary>
    /// Select level by map + level number
    /// </summary>
    public void SelectLevel(string mapID, int levelNumber)
    {
        MapData map = levelDatabase?.GetMap(mapID);
        
        if (map == null)
        {
            Debug.LogError($"[GameModeManager] ❌ Map not found: {mapID}");
            return;
        }
        
        LevelData level = map.GetLevel(levelNumber);
        
        if (level == null)
        {
            Debug.LogError($"[GameModeManager] ❌ Level {levelNumber} not found in map {mapID}");
            return;
        }
        
        _selectedMap = map;
        _selectedLevel = level;
        
        if (showDebugLogs)
        {
            Debug.Log($"[GameModeManager] 📍 Selected: {map.mapName} - Level {levelNumber}");
        }
    }
    
    #endregion

    #region Next Level Logic - NEW

    /// <summary>
    /// Check if has next level
    /// </summary>
    public bool HasNextLevel()
    {
        if (_currentMode != GameMode.Level)
        {
            return false;
        }
        
        if (_selectedLevel == null)
        {
            return false;
        }
        
        // Get next level in same map
        MapData currentMap = GetMapForLevel(_selectedLevel.levelID);
        if (currentMap == null)
        {
            return false;
        }
        
        int nextLevelNumber = _selectedLevel.levelNumber + 1;
        LevelData nextLevel = currentMap.GetLevel(nextLevelNumber);
        
        if (nextLevel == null)
        {
            // No more levels in this map
            return false;
        }
        
        // Check if unlocked
        bool isUnlocked = IsLevelUnlocked(nextLevel);
        
        return isUnlocked;
    }

    /// <summary>
    /// Get next level data
    /// </summary>
    public LevelData GetNextLevel()
    {
        if (!HasNextLevel())
        {
            return null;
        }
        
        MapData currentMap = GetMapForLevel(_selectedLevel.levelID);
        if (currentMap == null)
        {
            return null;
        }
        
        int nextLevelNumber = _selectedLevel.levelNumber + 1;
        return currentMap.GetLevel(nextLevelNumber);
    }

    /// <summary>
    /// Load next level
    /// </summary>
    public bool LoadNextLevel()
    {
        LevelData nextLevel = GetNextLevel();
        
        if (nextLevel == null)
        {
            Debug.LogWarning("[GameModeManager] No next level available!");
            return false;
        }
        
        // Select next level
        SelectLevel(nextLevel.levelID);
        
        Debug.Log($"[GameModeManager] ✓ Next level selected: {nextLevel.levelName}");
        
        return true;
    }

    /// <summary>
    /// Get map that contains a specific level
    /// </summary>
    private MapData GetMapForLevel(string levelID)
    {
        if (levelDatabase == null)
        {
            return null;
        }
        
        foreach (MapData map in levelDatabase.maps)
        {
            if (map.levels.Exists(l => l.levelID == levelID))
            {
                return map;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Check if level is unlocked - NEW helper
    /// </summary>
    private bool IsLevelUnlocked(LevelData level)
    {
        if (level.isUnlockedByDefault)
        {
            return true;
        }
        
        if (level.levelNumber == 1)
        {
            return true;
        }
        
        if (level.requiredLevels != null && level.requiredLevels.Length > 0)
        {
            foreach (string requiredLevelID in level.requiredLevels)
            {
                if (!PlayerDataManager.Instance.IsLevelCompleted(requiredLevelID))
                {
                    return false;
                }
            }
        }
        
        return true;
    }

    #endregion

    #region Gameplay Data
    
    /// <summary>
    /// Get target distance for current mode
    /// </summary>
    public float GetTargetDistance()
    {
        if (_currentMode == GameMode.Level && _selectedLevel != null)
        {
            return _selectedLevel.targetDistance;
        }
        else if (_currentMode == GameMode.Endless)
        {
            return levelDatabase.endlessStartDistance;
        }
        
        return 1000f; // Default fallback
    }
    
    /// <summary>
    /// Get sections JSON file name
    /// </summary>
    public string GetSectionsFileName()
    {
        if (_currentMode == GameMode.Level && _selectedLevel != null)
        {
            return _selectedLevel.sectionsFileName;
        }
        else if (_currentMode == GameMode.Endless)
        {
            return levelDatabase.endlessSectionsFileName;
        }
        
        return "sections"; // Default fallback
    }
    
    #endregion

    #region Level Progress
    
    /// <summary>
    /// Complete current level
    /// </summary>
    public void CompleteCurrentLevel()
    {
        if (_currentMode != GameMode.Level || _selectedLevel == null)
        {
            return;
        }
        
        // Unlock next level
        UnlockNextLevel();
        
        if (showDebugLogs)
        {
            Debug.Log($"[GameModeManager] ✓ Level completed: {_selectedLevel.levelName}");
        }
    }
    
    /// <summary>
    /// Unlock next level in current map
    /// </summary>
    private void UnlockNextLevel()
    {
        if (_selectedMap == null || _selectedLevel == null)
        {
            return;
        }
        
        int nextLevelNumber = _selectedLevel.levelNumber + 1;
        LevelData nextLevel = _selectedMap.GetLevel(nextLevelNumber);
        
        if (nextLevel != null)
        {
            PlayerDataManager.Instance.UnlockLevel(nextLevel.levelID);
            
            if (showDebugLogs)
            {
                Debug.Log($"[GameModeManager] 🔓 Unlocked: Level {nextLevelNumber}");
            }
        }
        else
        {
            // Map completed - unlock next map
            if (showDebugLogs)
            {
                Debug.Log($"[GameModeManager] 🎉 Map {_selectedMap.mapName} COMPLETED!");
            }
        }
    }
    
    #endregion

    #region Reset
    
    /// <summary>
    /// Reset selection
    /// </summary>
    public void ResetSelection()
    {
        _selectedMap = null;
        _selectedLevel = null;
        
        if (showDebugLogs)
        {
            Debug.Log("[GameModeManager] Selection reset");
        }
    }
    
    #endregion
}