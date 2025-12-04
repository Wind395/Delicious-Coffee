using UnityEngine;

/// <summary>
/// Medicine PowerUp - UPDATED: VFX type assigned
/// </summary>
public class MedicinePowerUp : PowerUpBase
{
    [Header("Shield Settings")]
    [SerializeField] private GameObject shieldVisual; // ← Can remove if using VFX Controller

    void Awake()
    {
        // ═══ ASSIGN VFX TYPE ═══
        vfxType = PowerUpVFXController.PowerUpType.Medicine;
    }

    protected override void OnActivate()
    {
        if (_player == null)
        {
            //Debug.LogError("[Medicine] Player is null!");
            return;
        }
        _player.EnableShield(this);
        
        // ═══ REMOVED: Manual visual effect ═══
        // if (shieldVisual != null)
        // {
        //     shieldVisual.SetActive(true);
        // }
        
        //DiarrheaMeter.Instance?.ApplyMedicine();
        AudioManager.Instance?.PlayMedicineSound();
        
        //Debug.Log("[Medicine] ✓ Shield activated!");
    }

    protected override void OnDeactivate()
    {
        if (_player == null)
        {
            //Debug.LogWarning("[Medicine] Player is null on deactivate");
            return;
        }

        _player.DisableShield();
        
        // ═══ REMOVED: Manual visual effect ═══
        // if (shieldVisual != null)
        // {
        //     shieldVisual.SetActive(false);
        // }
        
        //Debug.Log("[Medicine] Shield deactivated");
    }

    protected override void OnRefresh()
    {
        //Debug.Log("[Medicine] ⏱️ Shield refreshed!");
        //DiarrheaMeter.Instance?.ApplyMedicine();
        AudioManager.Instance?.PlayMedicineSound();
    }

    public void OnObstacleDestroyed(GameObject obstacle)
    {
        if (obstacle == null) return;
        
        //Debug.Log($"[Medicine] Shield destroyed obstacle: {obstacle.name}");
        
        // Play effects
        AudioManager.Instance?.PlayShieldBreakSound();
        
        // Shield breaks
        Deactivate();
    }
}