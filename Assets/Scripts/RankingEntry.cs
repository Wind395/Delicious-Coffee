using System;
using UnityEngine;

/// <summary>
/// Simple ranking entry data
/// </summary>
[System.Serializable]
public class RankingEntry : IComparable<RankingEntry>
{
    public string playerName;
    public float distance;
    public bool isPlayer; // true = You, false = NPC
    
    public RankingEntry(string name, float dist, bool player = false)
    {
        playerName = name;
        distance = dist;
        isPlayer = player;
    }
    
    /// <summary>
    /// Sort by distance descending (higher is better)
    /// </summary>
    public int CompareTo(RankingEntry other)
    {
        if (other == null) return 1;
        return other.distance.CompareTo(this.distance); // Descending
    }
    
    public string GetFormattedDistance()
    {
        return $"{distance:F0}m";
    }
}