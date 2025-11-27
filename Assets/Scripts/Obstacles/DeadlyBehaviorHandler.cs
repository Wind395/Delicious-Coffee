using UnityEngine;

/// <summary>
/// Deadly Behavior Handler - UPDATED: Dog catch player on death
/// </summary>
public class DeadlyBehaviorHandler : IObstacleBehaviorHandler
{
    public void HandleCollision(PlayerController player, Obstacle obstacle)
    {
        Debug.Log($"[DeadlyBehavior] ═══════════════════════════════");
        Debug.Log($"[DeadlyBehavior] 💥 DEADLY HIT: {obstacle.GetObstacleType()}");
        Debug.Log($"[DeadlyBehavior] ═══════════════════════════════");
        
        // ═══ STEP 1: DESTROY OBSTACLE ═══
        if (obstacle != null && obstacle.gameObject != null)
        {
            obstacle.gameObject.SetActive(false);
            Debug.Log($"[DeadlyBehavior] 💥 Destroyed obstacle: {obstacle.name}");
        }

        // ═══ STEP 2: PLAY DEATH EFFECTS ═══
        PlayDeadlyEffects(obstacle);

        // ═══ STEP 3: STOP PLAYER ═══
        player.StopPlayer();
        Debug.Log("[DeadlyBehavior] ⏹️ Player stopped");

        // ═══ STEP 4: STOP DOG CHASE (NEW) ═══
        if (DogChaseController.Instance != null)
        {
            DogChaseController.Instance.StopChaseOnDeath();
            Debug.Log("[DeadlyBehavior] 🐕 Dog chase stopped");
        }

        // ═══ STEP 5: TRIGGER DOG CATCH (PARALLEL - VISUAL ONLY) ═══
        if (DogChaseController.Instance != null)
        {
            DogChaseController.Instance.CatchPlayerParallel();
            Debug.Log("[DeadlyBehavior] 🐕 Dog catch started (parallel, visual only)");
        }

        // ═══ STEP 6: TRIGGER DEATH (INSTANT) ═══
        Debug.Log("[DeadlyBehavior] 💀 Triggering player death NOW");
        player.TriggerDeath();
        
        Debug.Log("[DeadlyBehavior] ✓ Deadly death sequence complete");
    }

    /// <summary>
    /// Play deadly collision effects
    /// </summary>
    private void PlayDeadlyEffects(Obstacle obstacle)
    {
        // Strong hit sound
        AudioManager.Instance?.PlayHitSound();
        
        // Strong camera shake
        var camera = UnityEngine.Object.FindObjectOfType<CameraFollowController>();
        if (camera != null)
        {
            camera.Shake(0.5f, 0.7f);
        }
        
        // Haptic feedback
        #if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        #endif
        
        Debug.Log("[DeadlyBehavior] ✓ Deadly effects played");
    }
}