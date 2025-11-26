    using UnityEngine;

    /// <summary>
    /// Ice Tea - FIXED: Ensure layer is properly restored
    /// </summary>
    public class IceTeaPowerUp : PowerUpBase
    {
        [Header("Ice Tea Settings")]
        [SerializeField] private GameObject invincibilityEffect;
        
        private int _originalLayer;
        private const string POWERUP_LAYER = "PowerUp";
        private const string PLAYER_LAYER = "Player";

        void Awake()
    {
        // ═══ ASSIGN VFX TYPE ═══
        vfxType = PowerUpVFXController.PowerUpType.IceTea;
    }

    protected override void OnActivate()
    {
        if (_player == null)
        {
            Debug.LogError("[IceTea] Player is null!");
            return;
        }

        _originalLayer = _player.gameObject.layer;
        int powerUpLayer = LayerMask.NameToLayer(POWERUP_LAYER);
        
        if (powerUpLayer == -1)
        {
            Debug.LogError("[IceTea] PowerUp layer not found!");
            return;
        }
        
        _player.gameObject.layer = powerUpLayer;
        
        // ═══ REMOVED: Manual visual effect (now handled by VFX Controller) ═══
        // if (invincibilityEffect != null)
        // {
        //     invincibilityEffect.SetActive(true);
        // }
        
        //DiarrheaMeter.Instance?.ApplyIceTea();
        AudioManager.Instance?.PlayIceTeaSound();
        
        Debug.Log($"[IceTea] ✓ Activated | Layer: {LayerMask.LayerToName(_originalLayer)} → {POWERUP_LAYER}");
    }

    protected override void OnDeactivate()
    {
        if (_player == null)
        {
            Debug.LogWarning("[IceTea] Player is null on deactivate");
            return;
        }

        int playerLayer = LayerMask.NameToLayer(PLAYER_LAYER);
        
        if (playerLayer == -1)
        {
            Debug.LogError("[IceTea] Player layer not found!");
            _player.gameObject.layer = _originalLayer;
        }
        else
        {
            _player.gameObject.layer = playerLayer;
        }
        
        // ═══ REMOVED: Manual visual effect ═══
        // if (invincibilityEffect != null)
        // {
        //     invincibilityEffect.SetActive(false);
        // }
        
        string currentLayerName = LayerMask.LayerToName(_player.gameObject.layer);
        Debug.Log($"[IceTea] ✓ Deactivated | Layer restored to: {currentLayerName}");
    }

    protected override void OnRefresh()
    {
        Debug.Log("[IceTea] ⏱️ Timer refreshed!");
        
        //DiarrheaMeter.Instance?.ApplyIceTea();
        AudioManager.Instance?.PlayIceTeaSound();
        
        // ═══ OPTIONAL: Pulse VFX via controller ═══
        // Can implement if you want refresh effect
    }

        // private System.Collections.IEnumerator PulseEffect()
        // {
        //     if (invincibilityEffect == null) yield break;

        //     Vector3 originalScale = invincibilityEffect.transform.localScale;
        //     Vector3 targetScale = originalScale * 1.3f;

        //     float time = 0f;
        //             while (time < 0.2f)
        //     {
        //         invincibilityEffect.transform.localScale = Vector3.Lerp(originalScale, targetScale, time / 0.2f);
        //         time += Time.deltaTime;
        //         yield return null;
        //     }

        //     time = 0f;
        //     while (time < 0.2f)
        //     {
        //         invincibilityEffect.transform.localScale = Vector3.Lerp(targetScale, originalScale, time / 0.2f);
        //         time += Time.deltaTime;
        //         yield return null;
        //     }

        //     invincibilityEffect.transform.localScale = originalScale;
        // }
}