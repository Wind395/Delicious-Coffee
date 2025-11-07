using UnityEngine;

/// <summary>
/// Slow Behavior - Reduce speed + invincibility + destroy obstacle
/// FIXED: Better debug logging
/// </summary>
public class SlowBehaviorHandler : IObstacleBehaviorHandler
{
    private readonly float _slowMultiplier;
    private readonly float _slowDuration;

    public SlowBehaviorHandler(float multiplier, float duration)
    {
        _slowMultiplier = multiplier;
        _slowDuration = duration;
        
        Debug.Log($"[SlowBehavior] Created handler - Multiplier: {multiplier}, Duration: {duration}");
    }

    public void HandleCollision(PlayerController player, Obstacle obstacle)
    {
        Debug.Log($"[SlowBehavior] ═══ SLOW HIT ═══");
        Debug.Log($"[SlowBehavior] Obstacle: {obstacle.GetObstacleType()}");
        Debug.Log($"[SlowBehavior] Speed: {_slowMultiplier * 100:F0}% for {_slowDuration}s");
        
        // Apply slow effect to player
        // This also:
        // - Destroys the obstacle
        // - Activates invincibility for 1s
        // - Starts visual flash
        player.ApplySlowEffect(_slowMultiplier, _slowDuration, obstacle.gameObject);

        Debug.Log("[SlowBehavior] ✓ ApplySlowEffect called");
        
        // Play sound
        AudioManager.Instance?.PlayObstacleDestroySound();
        // Play effects
        PlaySlowEffects(obstacle);
    }

    private void PlaySlowEffects(Obstacle obstacle)
    {
        // Light camera shake (less severe than deadly)
        var camera = Object.FindObjectOfType<CameraFollowController>();
        if (camera != null)
        {
            camera.Shake(0.2f, 0.3f);
        }
        
        Debug.Log("[SlowBehavior] ✓ Effects played");
    }
}