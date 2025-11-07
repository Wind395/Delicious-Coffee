using UnityEngine;
using System.Collections;

/// <summary>
/// Player Visual Effects - Transparency Flash
/// SOLID: Single Responsibility - Visual effects only
/// KISS: Simple flash logic
/// </summary>
public class PlayerVisualEffects : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Flash Settings")]
    [SerializeField] private float flashSpeed = 5f;
    [SerializeField] private float minAlpha = 0.3f;
    [SerializeField] private float maxAlpha = 1f;
    
    [Header("Material Setup")]
    [Tooltip("Enable if using Standard Shader (need to set Rendering Mode to Transparent)")]
    [SerializeField] private bool useStandardShader = true;
    
    [Header("References")]
    [SerializeField] private Renderer[] characterRenderers;
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    
    #endregion

    #region State
    
    private bool _isFlashing = false;
    private Coroutine _flashCoroutine;
    private Material[] _originalMaterials;
    private Material[] _transparentMaterials;
    
    #endregion

    #region Unity Lifecycle
    
    void Awake()
    {
        InitializeRenderers();
        CreateTransparentMaterials();
    }

    void OnDestroy()
    {
        // Cleanup materials
        if (_transparentMaterials != null)
        {
            foreach (var mat in _transparentMaterials)
            {
                if (mat != null)
                    Destroy(mat);
            }
        }
    }
    
    #endregion

    #region Initialization
    
    /// <summary>
    /// Initialize renderers
    /// </summary>
    private void InitializeRenderers()
    {
        // Auto-find renderers if not assigned
        if (characterRenderers == null || characterRenderers.Length == 0)
        {
            characterRenderers = GetComponentsInChildren<Renderer>();
        }

        if (characterRenderers == null || characterRenderers.Length == 0)
        {
            Debug.LogError("[VisualFX] No renderers found!");
            return;
        }

        if (showDebug)
            Debug.Log($"[VisualFX] Found {characterRenderers.Length} renderers");
    }

    /// <summary>
    /// Create transparent material instances
    /// </summary>
    private void CreateTransparentMaterials()
    {
        if (characterRenderers == null || characterRenderers.Length == 0)
            return;

        _originalMaterials = new Material[characterRenderers.Length];
        _transparentMaterials = new Material[characterRenderers.Length];

        for (int i = 0; i < characterRenderers.Length; i++)
        {
            if (characterRenderers[i] == null)
                continue;

            // Store original material
            _originalMaterials[i] = characterRenderers[i].material;

            // Create transparent copy
            _transparentMaterials[i] = new Material(characterRenderers[i].material);
            
            if (useStandardShader)
            {
                // Set to Transparent mode for Standard Shader
                SetMaterialTransparent(_transparentMaterials[i]);
            }
        }

        if (showDebug)
            Debug.Log("[VisualFX] ✓ Transparent materials created");
    }

    /// <summary>
    /// Set material to transparent mode (for Standard Shader)
    /// </summary>
    private void SetMaterialTransparent(Material mat)
    {
        if (mat == null) return;

        // Standard Shader transparency setup
        mat.SetFloat("_Mode", 3); // Transparent mode
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }
    
    #endregion

    #region Flash Control
    
    /// <summary>
    /// Start flashing - KISS: Simple coroutine
    /// </summary>
    public void StartFlashing()
    {
        if (_isFlashing)
            return;
        
        _isFlashing = true;
        
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
        }
        
        _flashCoroutine = StartCoroutine(FlashCoroutine());
        
        if (showDebug)
            Debug.Log("[VisualFX] ✓ Flash started");
    }

    /// <summary>
    /// Stop flashing - Restore normal
    /// </summary>
    public void StopFlashing()
    {
        if (!_isFlashing)
            return;
        
        _isFlashing = false;
        
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
            _flashCoroutine = null;
        }
        
        // Restore full alpha
        SetAlpha(maxAlpha);
        
        if (showDebug)
            Debug.Log("[VisualFX] ✓ Flash stopped");
    }

    /// <summary>
    /// Flash coroutine - Ping-pong alpha
    /// </summary>
    private IEnumerator FlashCoroutine()
    {
        while (_isFlashing)
        {
            // Fade to min
            float t = 0f;
            while (t < 1f && _isFlashing)
            {
                t += Time.deltaTime * flashSpeed;
                float alpha = Mathf.Lerp(maxAlpha, minAlpha, t);
                SetAlpha(alpha);
                yield return null;
            }
            
            // Fade to max
            t = 0f;
            while (t < 1f && _isFlashing)
            {
                t += Time.deltaTime * flashSpeed;
                float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
                SetAlpha(alpha);
                yield return null;
            }
        }
        
        // Ensure full alpha when stopped
        SetAlpha(maxAlpha);
    }

    /// <summary>
    /// Set alpha for all renderers - DRY
    /// </summary>
    private void SetAlpha(float alpha)
    {
        if (characterRenderers == null || _transparentMaterials == null)
            return;
        
        for (int i = 0; i < characterRenderers.Length; i++)
        {
            if (characterRenderers[i] == null || _transparentMaterials[i] == null)
                continue;
            
            // Get current color
            Color color = _transparentMaterials[i].color;
            
            // Set new alpha
            color.a = alpha;
            _transparentMaterials[i].color = color;
            
            // Apply material
            characterRenderers[i].material = _transparentMaterials[i];
        }
    }
    
    #endregion

    #region Public API
    
    /// <summary>
    /// Reset to normal appearance
    /// </summary>
    public void Reset()
    {
        StopFlashing();
        
        // Restore original materials
        if (characterRenderers != null && _originalMaterials != null)
        {
            for (int i = 0; i < characterRenderers.Length; i++)
            {
                if (characterRenderers[i] != null && _originalMaterials[i] != null)
                {
                    characterRenderers[i].material = _originalMaterials[i];
                }
            }
        }
        
        if (showDebug)
            Debug.Log("[VisualFX] ✓ Reset to normal");
    }
    
    #endregion
}