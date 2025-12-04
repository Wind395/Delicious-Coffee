using UnityEngine;

/// <summary>
/// Cold Towel PowerUp - UPDATED: VFX type assigned
/// </summary>
public class ColdTowelPowerUp : PowerUpBase
{
    [Header("Speed Boost")]
    [SerializeField] private float speedMultiplier = 1.5f;

    void Awake()
    {
        // ═══ ASSIGN VFX TYPE ═══
        vfxType = PowerUpVFXController.PowerUpType.ColdTowel;
    }
    
    protected override void OnActivate()
    {
        if (_player == null)
        {
            //Debug.LogError("[ColdTowel] Player is null!");
            return;
        }

        _player.SetSpeedMultiplier(speedMultiplier);
        
        // ═══ REMOVED: Manual visual effect ═══
        // if (visualEffect != null)
        // {
        //     visualEffect.SetActive(true);
        // }
        
        AudioManager.Instance?.PlayColdTowelSound();
        
        //Debug.Log($"[ColdTowel] ✓ Speed boost activated! Multiplier: {speedMultiplier}x");
    }

    protected override void OnDeactivate()
    {
        if (_player == null)
        {
            //Debug.LogWarning("[ColdTowel] Player is null on deactivate");
            return;
        }

        _player.ResetSpeedMultiplier();
        
        // ═══ REMOVED: Manual visual effect ═══
        // if (visualEffect != null)
        // {
        //     visualEffect.SetActive(false);
        // }
        
        //Debug.Log("[ColdTowel] ✓ Speed boost deactivated");
    }

    protected override void OnRefresh()
    {
        //Debug.Log("[ColdTowel] ⏱️ Speed boost refreshed!");
        AudioManager.Instance?.PlayColdTowelSound();
    }
}