using UnityEngine;

/// <summary>
/// Exclamation VFX Controller - Manages "!" particle effect
/// </summary>
public class ExclamationVFXController : MonoBehaviour
{
    #region Singleton
    
    private static ExclamationVFXController _instance;
    public static ExclamationVFXController Instance => _instance;
    
    #endregion

    #region Serialized Fields
    
    [Header("VFX Prefab")]
    [Tooltip("Particle effect or sprite for '!'")]
    [SerializeField] private GameObject exclamationPrefab;
    
    [Header("Spawn Settings")]
    [Tooltip("Height above player head")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, 2.5f, 0);
    
    [Tooltip("Effect duration (auto destroy)")]
    [SerializeField] private float effectDuration = 1f;
    
    [Header("Animation")]
    [SerializeField] private bool useScaleAnimation = true;
    [SerializeField] private float scaleAnimDuration = 0.3f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    #endregion

    #region Unity Lifecycle
    
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }
    
    #endregion

    #region Public API
    
    /// <summary>
    /// Play exclamation effect above target
    /// </summary>
    public void PlayExclamation(Transform target)
    {
        if (exclamationPrefab == null)
        {
            Debug.LogWarning("[ExclamationVFX] No prefab assigned!");
            return;
        }
        
        if (target == null)
        {
            Debug.LogWarning("[ExclamationVFX] Target is null!");
            return;
        }
        
        // Calculate spawn position
        Vector3 spawnPos = target.position + spawnOffset;
        
        // Spawn effect
        GameObject vfx = Instantiate(exclamationPrefab, spawnPos, Quaternion.identity);
        
        // Make it face camera
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            vfx.transform.LookAt(mainCam.transform);
            vfx.transform.Rotate(0, 180, 0); // Face camera
        }
        
        // Animate scale
        if (useScaleAnimation)
        {
            StartCoroutine(AnimateScale(vfx.transform));
        }
        
        // Auto destroy
        Destroy(vfx, effectDuration);
        
        if (showDebugLogs)
        {
            Debug.Log($"[ExclamationVFX] ❗ Played at {spawnPos}");
        }
    }
    
    #endregion

    #region Animation
    
    /// <summary>
    /// Animate scale (pop-in effect)
    /// </summary>
    private System.Collections.IEnumerator AnimateScale(Transform vfxTransform)
    {
        if (vfxTransform == null) yield break;
        
        Vector3 targetScale = vfxTransform.localScale;
        vfxTransform.localScale = Vector3.zero;
        
        float elapsed = 0f;
        
        while (elapsed < scaleAnimDuration)
        {
            if (vfxTransform == null) yield break;
            
            elapsed += Time.deltaTime;
            float t = elapsed / scaleAnimDuration;
            float curveValue = scaleCurve.Evaluate(t);
            
            vfxTransform.localScale = targetScale * curveValue;
            
            yield return null;
        }
        
        if (vfxTransform != null)
        {
            vfxTransform.localScale = targetScale;
        }
    }
    
    #endregion
}