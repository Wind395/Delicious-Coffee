using UnityEngine;

/// <summary>
/// Visual effect when obstacle is destroyed
/// </summary>
public class ObstacleDestructionEffect : MonoBehaviour
{
    [Header("Particle Effect")]
    [SerializeField] private GameObject destroyParticlePrefab;
    
    [Header("Settings")]
    [SerializeField] private float effectDuration = 1f;
    
    private static ObstacleDestructionEffect _instance;
    public static ObstacleDestructionEffect Instance => _instance;
    
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
    }
    
    /// <summary>
    /// Spawn destroy effect at position
    /// </summary>
    public void SpawnEffect(Vector3 position, ObstacleType obstacleType)
    {
        if (destroyParticlePrefab == null)
        {
            Debug.LogWarning("[ObstacleDestroyEffect] No particle prefab assigned!");
            return;
        }
        
        // Instantiate effect
        GameObject effect = Instantiate(destroyParticlePrefab, position, Quaternion.identity);
        
        // Auto destroy after duration
        Destroy(effect, effectDuration);
        
        Debug.Log($"[ObstacleDestroyEffect] ✓ Effect spawned at {position}");
    }
    
    /// <summary>
    /// Spawn effect with custom color
    /// </summary>
    public void SpawnEffect(Vector3 position, Color color)
    {
        if (destroyParticlePrefab == null) return;
        
        GameObject effect = Instantiate(destroyParticlePrefab, position, Quaternion.identity);
        
        // Try to set color
        ParticleSystem ps = effect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startColor = color;
        }
        
        Destroy(effect, effectDuration);
    }
}