using UnityEngine;

/// <summary>
/// Level Data - UPDATED for JSON System
/// </summary>
[CreateAssetMenu(fileName = "Level_", menuName = "Endless Runner/Level Data", order = 1)]
public class LevelData : ScriptableObject
{
    #region Basic Info
    
    [Header("Basic Info")]
    public int levelNumber = 1;
    public string levelName = "Level 1";
    
    [Range(30f, 120f)]
    public float duration = 60f;
    
    #endregion

    #region Player Settings
    
    [Header("Player Settings")]
    [Range(5f, 25f)]
    public float playerSpeed = 10f;
    
    [Range(5f, 20f)]
    public float laneChangeSpeed = 10f;
    
    #endregion

    #region Spawner Settings - SIMPLIFIED
    
    [Header("Spawner Settings")]
    [Tooltip("Force spawner difficulty (optional)")]
    public bool forceSpawnerDifficulty = false;
    
    [Tooltip("Difficulty to force (1-5)")]
    [Range(1, 5)]
    public int difficulty = 1;
    
    // ← REMOVED: Không còn cần các settings cho HybridSpawner
    // JSON spawner tự quản lý dựa trên internal difficulty progression
    
    #endregion

    #region Scoring
    
    [Header("Scoring")]
    [Range(1, 5)]
    public int scoreMultiplier = 1;
    
    public int completionBonus = 500;
    
    #endregion

    #region Visual Theme
    
    [Header("Visual Theme")]
    public Color trackColor = Color.gray;
    public Material skyboxMaterial;
    public Color ambientLightColor = Color.white;
    
    #endregion

    #region Validation
    
    void OnValidate()
    {
        levelNumber = Mathf.Clamp(levelNumber, 1, 99);
        duration = Mathf.Clamp(duration, 10f, 300f);
    }
    
    #endregion

    #region Helper Methods
    
    public int GetDifficultyRating()
    {
        float speedFactor = playerSpeed / 25f;
        float difficultyValue = speedFactor * 5f;
        return Mathf.Clamp(Mathf.RoundToInt(difficultyValue), 1, 10);
    }
    
    #endregion
}