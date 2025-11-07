using UnityEngine;

/// <summary>
/// Coin - REFACTORED: Clean, robust collection
/// SOLID: Single Responsibility - Coin behavior only
/// </summary>
public class Coin : CollectibleBase
{
    #region Serialized Fields
    
    [Header("Coin Settings")]
    [SerializeField] private int coinValue = 1;
    
    #endregion

    #region Overrides
    
    /// <summary>
    /// Called when coin is collected
    /// KISS: Simple, clear logic
    /// </summary>
    protected override void OnCollected(PlayerController player)
    {
        //Debug.Log($"[Coin] Collected at {transform.position}");
        
        // Add to score
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCoin();
        }
        else
        {
            Debug.LogError("[Coin] GameManager not found!");
        }
        
        // Play sound
        AudioManager.Instance?.PlayCoinSound();
        
        // Optional: Spawn VFX
        SpawnCollectionVFX();
    }

    /// <summary>
    /// Custom spawn behavior
    /// </summary>
    protected override void OnSpawnCustom()
    {
        // Coin-specific initialization
        // e.g., random rotation offset
    }

    #endregion


    


    #region VFX

    /// <summary>
    /// Spawn collection visual effect
    /// </summary>
    private void SpawnCollectionVFX()
    {
        // TODO: Instantiate particle effect
        // Optional: Scale animation before disappear
    }
    
    #endregion

    #region Public API
    
    /// <summary>
    /// Set coin value (for different coin types)
    /// </summary>
    public void SetValue(int value)
    {
        coinValue = Mathf.Max(1, value);
    }
    
    #endregion
}