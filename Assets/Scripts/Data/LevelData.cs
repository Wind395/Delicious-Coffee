using UnityEngine;

/// <summary>
/// Level Data - ScriptableObject cho 1 level
/// </summary>
[CreateAssetMenu(fileName = "Level_", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public string levelID;
    public int levelNumber; // 1-5
    public string mapID;
    public string levelName;
    
    [Header("Gameplay")]
    public float targetDistance = 500f;
    public string sectionsFileName = "sections"; // Tên file JSON (không có .json)
    
    [Header("Difficulty")]
    [Range(1, 5)]
    public int difficulty = 1;
    
    [Header("UI")]
    public Sprite thumbnail;
    
    [Header("Unlock")]
    public bool isUnlockedByDefault = false; // Level 1 mở sẵn
    public string[] requiredLevels; // Level cần hoàn thành để unlock
    
    [Header("Rewards")]
    public int goldReward = 100;
    public int coinReward = 50;
}