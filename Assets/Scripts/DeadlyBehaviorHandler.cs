using UnityEngine;

/// <summary>
/// Deadly Behavior - Instant Game Over
/// FIXED: Actually trigger death
/// </summary>
public class DeadlyBehaviorHandler : IObstacleBehaviorHandler
{
    public void HandleCollision(PlayerController player, Obstacle obstacle)
    {
        Debug.Log($"[DeadlyBehavior] 💥 DEADLY HIT: {obstacle.GetObstacleType()} - Triggering death!");
        
        // Play effects FIRST
        PlayDeadlyEffects(obstacle);
        
        // ═══ FIX: Trigger death ═══
        player.TriggerDeath();
        
        Debug.Log("[DeadlyBehavior] ✓ Death triggered");
    }

    private void PlayDeadlyEffects(Obstacle obstacle)
    {
        // Sound
        AudioManager.Instance?.PlayHitSound();
        
        // Camera shake (strong)
        var camera = Object.FindObjectOfType<CameraFollowController>();
        if (camera != null)
        {
            camera.Shake(0.5f, 0.7f);
        }
        
        // Haptic feedback
        #if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        #endif
    }
}