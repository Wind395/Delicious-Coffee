using UnityEngine;

/// <summary>
/// Coin Settings - Floating & Rotation animations
/// Attach to coin prefab
/// </summary>
public class CoinSettings : MonoBehaviour
{
    #region Settings
    
    [Header("═══ FLOATING ═══")]
    [SerializeField] private bool enableFloating = true;
    [SerializeField] private float floatingHeight = 0.3f;
    [SerializeField] private float floatingSpeed = 2f;
    
    [Header("═══ ROTATION ═══")]
    [SerializeField] private bool enableRotation = true;
    [SerializeField] private float rotationSpeed = 180f;
    
    #endregion
    
    #region State
    
    private Vector3 _basePosition;
    private float _floatingTimer;
    
    #endregion
    
    #region Unity Lifecycle
    
    void Update()
    {
        // Floating
        if (enableFloating)
        {
            UpdateFloating();
        }
        
        // Rotation
        if (enableRotation)
        {
            UpdateRotation();
        }
    }
    
    #endregion
    
    #region Animations
    
    /// <summary>
    /// Float up/down
    /// </summary>
    private void UpdateFloating()
    {
        _floatingTimer += Time.deltaTime * floatingSpeed;
        
        float yOffset = Mathf.Sin(_floatingTimer) * floatingHeight;
        
        Vector3 pos = _basePosition;
        pos.y += yOffset;
        
        transform.position = pos;
    }
    
    /// <summary>
    /// Rotate around Y axis
    /// </summary>
    private void UpdateRotation()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Set base position when spawned
    /// </summary>
    public void SetBasePosition(Vector3 position)
    {
        _basePosition = position;
        transform.position = position;
        
        // Random start time so coins don't sync
        _floatingTimer = Random.Range(0f, Mathf.PI * 2f);
    }
    
    /// <summary>
    /// Reset animation
    /// </summary>
    public void ResetAnimation()
    {
        _floatingTimer = Random.Range(0f, Mathf.PI * 2f);
        transform.position = _basePosition;
    }
    
    #endregion
}