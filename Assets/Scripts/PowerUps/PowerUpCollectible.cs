using UnityEngine;

/// <summary>
/// PowerUp Collectible - UPDATED: Use PowerUpSettings
/// </summary>
public class PowerUpCollectible : CollectibleBase
{
    #region Serialized Fields
    
    [Header("PowerUp Type")]
    [SerializeField] private PowerUpType powerUpType;
    
    #endregion

    #region PowerUp Types
    
    public enum PowerUpType
    {
        IceTea,
        ColdTowel,
        Medicine
    }
    
    #endregion

    #region Components - NEW
    
    private PowerUpSettings _settings;
    
    #endregion

    #region Unity Lifecycle - NEW
    
    protected override void Awake()
    {
        base.Awake();
        
        // Get PowerUpSettings component
        _settings = GetComponent<PowerUpSettings>();
    }

    protected override void Update()
    {
        base.Update();
        
        // Update animations from settings
        if (_settings != null && _state == CollectibleState.Active)
        {
            _settings.UpdateAnimations();
        }
    }
    
    #endregion

    #region Overrides
    
    /// <summary>
    /// Called when powerup is collected
    /// </summary>
    protected override void OnCollected(PlayerController player)
    {
        Debug.Log($"[PowerUp] Collected {powerUpType} at {transform.position}");
        
        // Activate powerup
        bool activated = ActivatePowerUp();
        
        if (activated)
        {
            // Play sound
            AudioManager.Instance?.PlayPowerUpCollectSound();
            
            // Spawn VFX
            SpawnCollectionVFX();
        }
        else
        {
            Debug.LogError($"[PowerUp] Failed to activate {powerUpType}!");
        }
    }

    /// <summary>
    /// Custom spawn behavior - UPDATED: Apply settings
    /// </summary>
    protected override void OnSpawnCustom()
    {
        // Apply settings from component
        if (_settings != null)
        {
            _settings.ApplySettings(transform);
            
            Debug.Log($"[PowerUp] ✓ Applied settings: " +
                     $"Offset={_settings.PositionOffset}, " +
                     $"Float={_settings.EnableFloating}, " +
                     $"Rotate={_settings.EnableRotation}");
        }
        
        // Reset bobbing timer with random offset
        _bobTimer = Random.Range(0f, Mathf.PI * 2f);
    }

    /// <summary>
    /// Override base visuals - Let settings handle it
    /// </summary>
    protected override void UpdateVisuals()
    {
        // If settings exist, let it handle animations
        if (_settings != null)
        {
            // Settings.UpdateAnimations() is called in Update()
            return;
        }
        
        // Fallback to base behavior if no settings
        base.UpdateVisuals();
    }

    #endregion

    #region PowerUp Activation

    /// <summary>
    /// Activate powerup based on type
    /// </summary>
    private bool ActivatePowerUp()
    {
        if (PowerUpManager.Instance == null)
        {
            Debug.LogError("[PowerUp] PowerUpManager not found!");
            return false;
        }

        switch (powerUpType)
        {
            case PowerUpType.IceTea:
                PowerUpManager.Instance.ActivatePowerUp<IceTeaPowerUp>();
                return true;

            case PowerUpType.ColdTowel:
                PowerUpManager.Instance.ActivatePowerUp<ColdTowelPowerUp>();
                return true;

            case PowerUpType.Medicine:
                PowerUpManager.Instance.ActivatePowerUp<MedicinePowerUp>();
                return true;

            default:
                Debug.LogWarning($"[PowerUp] Unknown type: {powerUpType}");
                return false;
        }
    }
    
    #endregion
    

    #region VFX
    
    /// <summary>
    /// Spawn collection effect
    /// </summary>
    private void SpawnCollectionVFX()
    {
        // TODO: Different VFX per powerup type
    }
    
    #endregion

    #region Public API
    
    /// <summary>
    /// Set powerup type (for pool reuse)
    /// </summary>
    public void SetPowerUpType(PowerUpType type)
    {
        powerUpType = type;
    }
    
    /// <summary>
    /// Get current powerup type
    /// </summary>
    public PowerUpType GetPowerUpType()
    {
        return powerUpType;
    }
    
    #endregion
}