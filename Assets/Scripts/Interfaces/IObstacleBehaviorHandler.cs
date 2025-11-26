using UnityEngine;

/// <summary>
/// Obstacle Behavior Handler Interface - Strategy Pattern
/// SOLID: Interface Segregation - Single method contract
/// Design Pattern: Strategy
/// </summary>
public interface IObstacleBehaviorHandler
{
    /// <summary>
    /// Handle collision behavior
    /// </summary>
    void HandleCollision(PlayerController player, Obstacle obstacle);
}