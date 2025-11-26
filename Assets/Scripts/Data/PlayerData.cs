using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerData
{
    #region Basic Data
    
    public int gold;
    public int totalCoinsCollected;
    public int totalGamesPlayed;
    public int totalWins;
    public int totalLosses;
    public int highScore;
    
    #endregion

    #region Shop - Purchased & Equipped Items
    
    public List<string> purchasedCharacters = new List<string>();
    public List<string> purchasedHomes = new List<string>();
    
    public string equippedCharacter = "char_default";
    public string equippedHome = "home_default";
    
    #endregion

    #region Statistics
    
    public float totalDistanceTraveled;
    public int totalObstaclesAvoided;
    public int totalPowerUpsCollected;
    
    public int totalDogEscapes;
    public int totalDogCaught;
    public float longestDogChaseDistance;
    
    #endregion

    // ═══════════════════════════════════════════════════════
    // NEW: LEVEL MODE DATA
    // ═══════════════════════════════════════════════════════
    
    #region Level Mode Data - NEW
    
    [Header("Level Progress")]
    public List<string> completedLevels = new List<string>(); // Level IDs
    public List<string> unlockedLevels = new List<string>();  // Level IDs
    public List<string> completedMaps = new List<string>();   // Map IDs
    
    [Header("Best Records")]
    public List<LevelRecord> levelRecords = new List<LevelRecord>(); // Best time/score per level
    
    #endregion

    // ═══════════════════════════════════════════════════════
    // NEW: ENDLESS MODE DATA
    // ═══════════════════════════════════════════════════════
    
    #region Endless Mode Data - NEW
    
    [Header("Endless Mode")]
    public float bestEndlessDistance = 0f; // Longest distance in endless mode
    public int endlessGamesPlayed = 0;
    public float totalEndlessDistance = 0f;
    
    #endregion

    #region Timestamps
    
    public string lastPlayedDate;
    public string createdDate;
    
    #endregion

    #region Constructor
    
    public PlayerData()
    {
        gold = 0;
        totalCoinsCollected = 0;
        totalGamesPlayed = 0;
        totalWins = 0;
        totalLosses = 0;
        highScore = 0;
        
        equippedCharacter = "char_default";
        equippedHome = "home_default";
        
        purchasedCharacters = new List<string> { "char_default" };
        purchasedHomes = new List<string> { "home_default" };
        
        totalDistanceTraveled = 0f;
        totalObstaclesAvoided = 0;
        totalPowerUpsCollected = 0;
        
        totalDogEscapes = 0;
        totalDogCaught = 0;
        longestDogChaseDistance = 0f;
        
        // NEW: Initialize level data
        completedLevels = new List<string>();
        unlockedLevels = new List<string>();
        completedMaps = new List<string>();
        levelRecords = new List<LevelRecord>();
        
        // NEW: Initialize endless data
        bestEndlessDistance = 0f;
        endlessGamesPlayed = 0;
        totalEndlessDistance = 0f;
        
        createdDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        lastPlayedDate = createdDate;
    }
    
    #endregion

    #region Helper Methods
    
    public void UpdateLastPlayed()
    {
        lastPlayedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public bool HasCharacter(string characterID)
    {
        return purchasedCharacters.Contains(characterID);
    }

    public bool HasHome(string homeID)
    {
        return purchasedHomes.Contains(homeID);
    }

    public void AddCharacter(string characterID)
    {
        if (!purchasedCharacters.Contains(characterID))
        {
            purchasedCharacters.Add(characterID);
        }
    }

    public void AddHome(string homeID)
    {
        if (!purchasedHomes.Contains(homeID))
        {
            purchasedHomes.Add(homeID);
        }
    }
    
    #endregion
    
    // ═══════════════════════════════════════════════════════
    // NEW: LEVEL PROGRESS METHODS
    // ═══════════════════════════════════════════════════════
    
    #region Level Progress Methods - NEW
    
    /// <summary>
    /// Check if level is completed
    /// </summary>
    public bool IsLevelCompleted(string levelID)
    {
        return completedLevels.Contains(levelID);
    }
    
    /// <summary>
    /// Check if level is unlocked
    /// </summary>
    public bool IsLevelUnlocked(string levelID)
    {
        return unlockedLevels.Contains(levelID);
    }
    
    /// <summary>
    /// Check if map is completed (all levels)
    /// </summary>
    public bool IsMapCompleted(string mapID)
    {
        return completedMaps.Contains(mapID);
    }
    
    /// <summary>
    /// Mark level as completed
    /// </summary>
    public void MarkLevelCompleted(string levelID)
    {
        if (!completedLevels.Contains(levelID))
        {
            completedLevels.Add(levelID);
        }
    }
    
    /// <summary>
    /// Unlock level
    /// </summary>
    public void UnlockLevel(string levelID)
    {
        if (!unlockedLevels.Contains(levelID))
        {
            unlockedLevels.Add(levelID);
        }
    }
    
    /// <summary>
    /// Mark map as completed
    /// </summary>
    public void MarkMapCompleted(string mapID)
    {
        if (!completedMaps.Contains(mapID))
        {
            completedMaps.Add(mapID);
        }
    }
    
    /// <summary>
    /// Get level record
    /// </summary>
    public LevelRecord GetLevelRecord(string levelID)
    {
        return levelRecords.Find(r => r.levelID == levelID);
    }
    
    /// <summary>
    /// Update level record (best time/score)
    /// </summary>
    public void UpdateLevelRecord(string levelID, float time, int score)
    {
        LevelRecord existing = GetLevelRecord(levelID);
        
        if (existing != null)
        {
            // Update if better
            if (time < existing.bestTime || existing.bestTime == 0)
            {
                existing.bestTime = time;
            }
            
            if (score > existing.bestScore)
            {
                existing.bestScore = score;
            }
        }
        else
        {
            // Create new record
            LevelRecord newRecord = new LevelRecord
            {
                levelID = levelID,
                bestTime = time,
                bestScore = score
            };
            
            levelRecords.Add(newRecord);
        }
    }
    
    #endregion
    
    // ═══════════════════════════════════════════════════════
    // NEW: ENDLESS MODE METHODS
    // ═══════════════════════════════════════════════════════
    
    #region Endless Mode Methods - NEW
    
    /// <summary>
    /// Update endless mode record
    /// </summary>
    public void UpdateEndlessRecord(float distance)
    {
        endlessGamesPlayed++;
        totalEndlessDistance += distance;
        
        if (distance > bestEndlessDistance)
        {
            bestEndlessDistance = distance;
        }
    }
    
    #endregion
}

// ═══════════════════════════════════════════════════════
// NEW: LEVEL RECORD CLASS
// ═══════════════════════════════════════════════════════

/// <summary>
/// Level Record - Best time/score for a level
/// </summary>
[Serializable]
public class LevelRecord
{
    public string levelID;
    public float bestTime;  // Seconds to complete
    public int bestScore;   // Best score achieved
}