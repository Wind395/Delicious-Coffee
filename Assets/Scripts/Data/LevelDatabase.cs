using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Level Database - ScriptableObject chứa tất cả maps
/// </summary>
[CreateAssetMenu(fileName = "LevelDatabase", menuName = "Game/Level Database")]
public class LevelDatabase : ScriptableObject
{
    [Header("Maps")]
    public List<MapData> maps = new List<MapData>();
    
    [Header("Endless Mode")]
    public string endlessSectionsFileName = "endless_sections"; // JSON cho endless
    public float endlessStartDistance = 1000f; // Distance ban đầu
    
    #region Validation
    
    void OnValidate()
    {
        // Auto-validate maps
        if (maps == null || maps.Count == 0)
        {
            Debug.LogWarning("[LevelDatabase] No maps assigned!");
            return;
        }
        
        // Check duplicates
        var duplicateMapIDs = maps.GroupBy(m => m.mapID)
                                   .Where(g => g.Count() > 1)
                                   .Select(g => g.Key);
        
        foreach (var id in duplicateMapIDs)
        {
            Debug.LogError($"[LevelDatabase] Duplicate map ID: {id}");
        }
    }
    
    #endregion
    
    #region Query Methods
    
    /// <summary>
    /// Get map by ID
    /// </summary>
    public MapData GetMap(string mapID)
    {
        return maps.Find(m => m.mapID == mapID);
    }
    
    /// <summary>
    /// Get all maps
    /// </summary>
    public List<MapData> GetAllMaps()
    {
        return maps;
    }
    
    /// <summary>
    /// Get level by ID
    /// </summary>
    public LevelData GetLevel(string levelID)
    {
        foreach (var map in maps)
        {
            var level = map.levels.Find(l => l.levelID == levelID);
            if (level != null)
            {
                return level;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Get total level count
    /// </summary>
    public int GetTotalLevelCount()
    {
        int total = 0;
        foreach (var map in maps)
        {
            total += map.GetLevelCount();
        }
        return total;
    }
    
    #endregion
    
    #region Debug
    
    #if UNITY_EDITOR
    
    [ContextMenu("Print Database Info")]
    void PrintInfo()
    {
        Debug.Log("═══════════════════════════════════");
        Debug.Log("LEVEL DATABASE");
        Debug.Log("═══════════════════════════════════");
        Debug.Log($"Total Maps: {maps.Count}");
        Debug.Log($"Total Levels: {GetTotalLevelCount()}");
        
        foreach (var map in maps)
        {
            Debug.Log($"\n▼ {map.mapName} (ID: {map.mapID})");
            Debug.Log($"  Levels: {map.GetLevelCount()}");
            
            foreach (var level in map.levels)
            {
                Debug.Log($"    - Level {level.levelNumber}: {level.levelName}");
                Debug.Log($"      Distance: {level.targetDistance}m");
                Debug.Log($"      JSON: {level.sectionsFileName}.json");
            }
        }
        
        Debug.Log("\n═══════════════════════════════════");
    }
    
    #endif
    
    #endregion
}