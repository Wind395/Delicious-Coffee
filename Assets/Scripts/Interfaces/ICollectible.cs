using UnityEngine;

/// <summary>
/// Interface for collectible objects
/// SOLID: Interface Segregation - Specific contract for collectibles
/// </summary>
public interface ICollectible
{
    /// <summary>
    /// Called when player collects this item
    /// </summary>
    void Collect(PlayerController player);
    
    /// <summary>
    /// Check if can be collected
    /// </summary>
    bool CanCollect();
    
    /// <summary>
    /// Get current state
    /// </summary>
    CollectibleState GetState();
}

/// <summary>
/// State Pattern - Clear state management
/// KISS: Simple enum, easy to understand
/// </summary>
public enum CollectibleState
{
    Inactive,   // In pool, disabled
    Active,     // Spawned, can collect
    Collected   // Already collected, waiting for pool return
}