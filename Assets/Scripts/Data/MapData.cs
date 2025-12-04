using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Map Data - UPDATED: Thêm Floor Prefab + Obstacle Variants
/// </summary>
[CreateAssetMenu(fileName = "Map_", menuName = "Game/Map Data")]
public class MapData : ScriptableObject
{
    [Header("Map Info")]
    public string mapID;
    public string mapName;
    public string description;
    
    [Header("Levels")]
    public List<LevelData> levels = new List<LevelData>(5);
    
    // ═══════════════════════════════════════════════════════
    // NEW: FLOOR PREFAB
    // ═══════════════════════════════════════════════════════
    
    [Header("Floor Prefab - NEW")]
    [Tooltip("Track/Floor prefab cho map này")]
    public GameObject floorPrefab;
    
    // ═══════════════════════════════════════════════════════
    // NEW: OBSTACLE VARIANTS
    // ═══════════════════════════════════════════════════════
    
    [Header("Obstacle Variants - NEW")]
    [Tooltip("Obstacle variant set cho map này")]
    public ObstacleVariantSet obstacleVariantSet;
    
    [Header("UI")]
    public Sprite mapIcon;
    public Sprite mapBanner;
    public Color themeColor = Color.white;
    
    [Header("Unlock")]
    public bool isUnlockedByDefault = true;
    public string requiredMapID;
    
    #region Query Methods
    
    /// <summary>
    /// Get level by number
    /// </summary>
    public LevelData GetLevel(int levelNumber)
    {
        return levels.Find(l => l.levelNumber == levelNumber);
    }
    
    /// <summary>
    /// Get total levels
    /// </summary>
    public int GetLevelCount()
    {
        return levels.Count;
    }
    
    #endregion
}