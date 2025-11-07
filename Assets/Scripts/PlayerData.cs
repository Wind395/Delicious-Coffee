using System;
using System.Collections.Generic;

/// <summary>
/// Player Data - Serializable data structure
/// SOLID: Single Responsibility - Data structure only
/// </summary>
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

    #region Shop - Purchased & Equipped Items - FIXED
    
    // Purchased items (IDs)
    public List<string> purchasedCharacters = new List<string>();
    public List<string> purchasedToilets = new List<string>();
    
    // ═══ FIXED: Single equipped items ═══
    public string equippedCharacter = "char_default"; // ← DEFAULT ID
    public string equippedToilet = "toilet_default";   // ← DEFAULT ID
    
    #endregion

    #region Statistics
    
    public float totalDistanceTraveled;
    public int totalObstaclesAvoided;
    public int totalPowerUpsCollected;
    
    #endregion

    #region Timestamps
    
    public string lastPlayedDate;
    public string createdDate;
    
    #endregion

    #region Constructor
    
    /// <summary>
    /// Create new player data with defaults
    /// </summary>
    public PlayerData()
    {
        gold = 0;
        totalCoinsCollected = 0;
        totalGamesPlayed = 0;
        totalWins = 0;
        totalLosses = 0;
        highScore = 0;
        
        // ═══ CRITICAL: Set default equipped items ═══
        equippedCharacter = "char_default";
        equippedToilet = "toilet_default";
        
        // ═══ CRITICAL: Add defaults to purchased list ═══
        purchasedCharacters = new List<string> { "char_default" };
        purchasedToilets = new List<string> { "toilet_default" };
        
        totalDistanceTraveled = 0f;
        totalObstaclesAvoided = 0;
        totalPowerUpsCollected = 0;
        
        createdDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        lastPlayedDate = createdDate;
    }
    
    #endregion

    #region Helper Methods
    
    /// <summary>
    /// Update last played timestamp
    /// </summary>
    public void UpdateLastPlayed()
    {
        lastPlayedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// Check if character is purchased
    /// </summary>
    public bool HasCharacter(string characterID)
    {
        return purchasedCharacters.Contains(characterID);
    }

    /// <summary>
    /// Check if toilet is purchased
    /// </summary>
    public bool HasToilet(string toiletID)
    {
        return purchasedToilets.Contains(toiletID);
    }

    /// <summary>
    /// Add purchased character
    /// </summary>
    public void AddCharacter(string characterID)
    {
        if (!purchasedCharacters.Contains(characterID))
        {
            purchasedCharacters.Add(characterID);
        }
    }

    /// <summary>
    /// Add purchased toilet
    /// </summary>
    public void AddToilet(string toiletID)
    {
        if (!purchasedToilets.Contains(toiletID))
        {
            purchasedToilets.Add(toiletID);
        }
    }
    
    #endregion
}