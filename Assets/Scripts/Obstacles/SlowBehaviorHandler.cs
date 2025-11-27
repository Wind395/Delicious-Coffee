using UnityEngine;
using System.Collections;

/// <summary>
/// Slow Behavior Handler - REFACTORED: Use PlayerController.ApplySlowEffect()
/// SOLID: Single Responsibility - Delegates to PlayerController
/// Design Pattern: Strategy
/// </summary>
public class SlowBehaviorHandler : IObstacleBehaviorHandler
{
    private readonly float _slowMultiplier;
    private readonly float _slowDuration;

    public SlowBehaviorHandler(float multiplier, float duration)
    {
        _slowMultiplier = multiplier;
        _slowDuration = duration;
    }

    public void HandleCollision(PlayerController player, Obstacle obstacle)
    {
        Debug.Log($"[SlowBehavior] ═══ SLOW OBSTACLE HIT ═══");
        Debug.Log($"[SlowBehavior] Obstacle: {obstacle.GetObstacleType()}");
        Debug.Log($"[SlowBehavior] Slow Multiplier: {_slowMultiplier * 100:F0}%");
        Debug.Log($"[SlowBehavior] Duration: {_slowDuration}s");
        
        if (player == null || obstacle == null)
        {
            Debug.LogError("[SlowBehavior] ❌ Player or Obstacle is null!");
            return;
        }

        // ═══ GET ANIMATION CONTROLLER ═══
        PlayerAnimationController animController = player.GetComponent<PlayerAnimationController>();
        
        if (animController == null)
        {
            Debug.LogError("[SlowBehavior] ❌ PlayerAnimationController not found!");
            return;
        }

        // ═══ CHECK: ALREADY INJURED? → INSTANT DEATH! ═══
        if (animController.IsInjured)
        {
            Debug.Log("[SlowBehavior] 💀 Hit slow obstacle while INJURED → INSTANT DEATH!");
            
            TriggerInstantDeath(player, obstacle, animController);
            return;
        }

        // ═══ FIRST HIT: APPLY SLOW EFFECT ═══
        Debug.Log("[SlowBehavior] 🤕 First hit - applying slow effect");
        
        // ← CHANGED: Use PlayerController's method (handles everything)
        player.ApplySlowEffect(_slowMultiplier, _slowDuration, obstacle.gameObject);
        
        Debug.Log("[SlowBehavior] ✓ Slow effect applied successfully");
    }

    /// <summary>
    /// Trigger instant death on second hit - UNCHANGED
    /// </summary>
    private void TriggerInstantDeath(PlayerController player, Obstacle obstacle, PlayerAnimationController animController)
    {
        Debug.Log("[SlowBehavior] ═══════════════════════════════");
        Debug.Log("[SlowBehavior] ⚡ INSTANT DEATH SEQUENCE");
        Debug.Log("[SlowBehavior] ═══════════════════════════════");

        // ═══ STEP 1: DESTROY OBSTACLE ═══
        if (obstacle != null && obstacle.gameObject != null)
        {
            obstacle.gameObject.SetActive(false);
            Debug.Log($"[SlowBehavior] 💥 Destroyed obstacle: {obstacle.name}");
        }

        // ═══ STEP 2: PLAY DEATH EFFECTS ═══
        PlayDeathEffects();

        // ═══ STEP 3: STOP PLAYER ═══
        player.StopPlayer();
        Debug.Log("[SlowBehavior] ⏹️ Player stopped");

        // ═══ STEP 4: STOP DOG CHASE ═══
        if (DogChaseController.Instance != null)
        {
            DogChaseController.Instance.StopChaseOnDeath();
            Debug.Log("[SlowBehavior] 🐕 Dog chase stopped");
        }

        // ═══ STEP 5: TRIGGER DEATH ANIMATION ═══
        Debug.Log("[SlowBehavior] 💀 Triggering death animation NOW");
        player.TriggerDeath();

        // ═══ STEP 6: START DOG CATCH (PARALLEL) ═══
        if (DogChaseController.Instance != null)
        {
            DogChaseController.Instance.CatchPlayerParallel();
            Debug.Log("[SlowBehavior] 🐕 Dog catch (visual only)");
        }

        Debug.Log("[SlowBehavior] ✓ Instant death triggered");
    }

    /// <summary>
    /// Play death effects - UNCHANGED
    /// </summary>
    private void PlayDeathEffects()
    {
        AudioManager.Instance?.PlayHitSound();
        
        var camera = Object.FindObjectOfType<CameraFollowController>();
        if (camera != null)
        {
            camera.Shake(0.5f, 0.7f);
        }
        
        #if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        #endif
        
        Debug.Log("[SlowBehavior] ✓ Death effects played");
    }
}