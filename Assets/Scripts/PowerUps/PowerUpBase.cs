using UnityEngine;

/// <summary>
/// PowerUp Base Class - UPDATED: Auto VFX management
/// </summary>
public abstract class PowerUpBase : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Base Settings")]
    [SerializeField] protected float duration;
    [SerializeField] protected GameObject visualEffect; // ← DEPRECATED (use VFX Controller instead)
    
    [Header("VFX System")]
    [Tooltip("Auto show/hide VFX via PowerUpVFXController")]
    [SerializeField] protected bool useVFXController = true;
    
    [SerializeField] protected PowerUpVFXController.PowerUpType vfxType;
    
    #endregion

    #region State
    
    protected bool _isActive;
    protected float _timer;
    protected PlayerController _player;
    
    #endregion

    #region Properties
    
    public bool IsActive => _isActive;
    public float TimeRemaining => _timer;
    public float Duration => duration;
    
    #endregion

    #region Template Method Pattern
    
    /// <summary>
    /// Activate powerup - UPDATED: Auto show VFX
    /// </summary>
    public void Activate(PlayerController player)
    {
        _player = player;
        bool wasActive = _isActive;
        
        _isActive = true;
        _timer = duration;
        
        if (!wasActive)
        {
            OnActivate();
            
            // ═══ AUTO SHOW VFX ═══
            if (useVFXController)
            {
                ShowVFX();
            }
            
            Debug.Log($"[PowerUp] {GetType().Name} activated for {duration}s");
        }
        else
        {
            OnRefresh();
            Debug.Log($"[PowerUp] {GetType().Name} refreshed! Timer reset to {duration}s");
        }
    }

    /// <summary>
    /// Deactivate powerup - UPDATED: Auto hide VFX
    /// </summary>
    public void Deactivate()
    {
        if (!_isActive)
        {
            Debug.LogWarning($"[PowerUp] {GetType().Name} already inactive");
            return;
        }
        
        _isActive = false;
        _timer = 0f;
        
        OnDeactivate();
        
        // ═══ AUTO HIDE VFX ═══
        if (useVFXController)
        {
            HideVFX();
        }
        
        Debug.Log($"[PowerUp] {GetType().Name} deactivated");
        
        // Notify manager to remove
        if (PowerUpManager.Instance != null)
        {
            Destroy(gameObject, 0.1f);
        }
    }

    protected virtual void Update()
    {
        if (_isActive)
        {
            _timer -= Time.deltaTime;
            
            OnUpdate();
            
            if (_timer <= 0)
            {
                Deactivate();
            }
        }
    }
    
    #endregion

    #region VFX Control - NEW
    
    /// <summary>
    /// Show VFX via controller
    /// </summary>
    protected void ShowVFX()
    {
        if (PowerUpVFXController.Instance != null)
        {
            PowerUpVFXController.Instance.ShowVFX(vfxType);
            Debug.Log($"[PowerUp] ✨ VFX shown: {vfxType}");
        }
        else
        {
            Debug.LogWarning($"[PowerUp] PowerUpVFXController not found!");
            
            // Fallback to old visual effect
            if (visualEffect != null)
            {
                visualEffect.SetActive(true);
            }
        }
    }
    
    /// <summary>
    /// Hide VFX via controller
    /// </summary>
    protected void HideVFX()
    {
        if (PowerUpVFXController.Instance != null)
        {
            PowerUpVFXController.Instance.HideVFX(vfxType);
            Debug.Log($"[PowerUp] ⚫ VFX hidden: {vfxType}");
        }
        else
        {
            // Fallback to old visual effect
            if (visualEffect != null)
            {
                visualEffect.SetActive(false);
            }
        }
    }
    
    #endregion

    #region Abstract Methods
    
    protected abstract void OnActivate();
    protected abstract void OnDeactivate();
    protected virtual void OnRefresh() { }
    protected virtual void OnUpdate() { }
    
    #endregion

    #region Cleanup
    
    void OnDestroy()
    {
        if (_isActive)
        {
            OnDeactivate();
            
            // Ensure VFX is hidden
            if (useVFXController)
            {
                HideVFX();
            }
        }
    }
    
    #endregion
}