using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Obstacle Variant Set - UPDATED: Specific obstacle types by name
/// </summary>
[CreateAssetMenu(fileName = "ObstacleSet", menuName = "Game/Obstacle Variant Set", order = 2)]
public class ObstacleVariantSet : ScriptableObject
{
    [Header("Set Info")]
    public string setID;
    public string setName;

    [Header("Specific Obstacle Types")]
    [Tooltip("Car variants")]
    public GameObject[] carVariants;

    [Tooltip("Motorcycle variants")]
    public GameObject[] motorcycleVariants;

    [Tooltip("Street Vendor variants")]
    public GameObject[] streetVendorVariants;

    [Tooltip("Fence variants")]
    public GameObject[] fenceVariants;

    [Tooltip("Trash Can variants")]
    public GameObject[] trashCanVariants;

    [Tooltip("Human/Pedestrian variants")]
    public GameObject[] humanVariants;

    #region Get Variant Methods

    /// <summary>
    /// Get random variant by specific type name
    /// </summary>
    public GameObject GetRandomVariant(string typeName)
    {
        GameObject[] variants = GetVariantsByType(typeName);

        if (variants != null && variants.Length > 0)
        {
            // Filter out null references
            GameObject[] validVariants = System.Array.FindAll(variants, v => v != null);

            if (validVariants.Length > 0)
            {
                int randomIndex = Random.Range(0, validVariants.Length);
                return validVariants[randomIndex];
            }
        }

        Debug.LogWarning($"[ObstacleSet] No variants found for type: {typeName} in set '{setName}'");
        return null;
    }

    /// <summary>
    /// Get variants array by type name
    /// </summary>
    public GameObject[] GetVariantsByType(string typeName)
    {
        string normalizedType = typeName.ToLower().Trim();

        switch (normalizedType)
        {
            // ═══ SPECIFIC TYPES (NEW) ═══
            case "car":
                return carVariants;

            case "motorcycle":
            case "bike":
            case "motorbike":
                return motorcycleVariants;

            case "vendor":
            case "streetvendor":
                return streetVendorVariants;

            case "fence":
                return fenceVariants;

            case "trashcan":
            case "trash":
                return trashCanVariants;

            case "human":
            case "pedestrian":
            case "person":
                return humanVariants;

            default:
                return null;
        }
    }

    /// <summary>
    /// Check if type is available in this set
    /// </summary>
    public bool HasType(string typeName)
    {
        GameObject[] variants = GetVariantsByType(typeName);
        return variants != null && variants.Length > 0;
    }

    /// <summary>
    /// Get all available type names in this set
    /// </summary>
    public List<string> GetAvailableTypes()
    {
        List<string> types = new List<string>();

        if (carVariants != null && carVariants.Length > 0) types.Add("car");
        if (motorcycleVariants != null && motorcycleVariants.Length > 0) types.Add("motorcycle");
        if (streetVendorVariants != null && streetVendorVariants.Length > 0) types.Add("vendor");
        if (fenceVariants != null && fenceVariants.Length > 0) types.Add("fence");
        if (trashCanVariants != null && trashCanVariants.Length > 0) types.Add("trashcan");
        if (humanVariants != null && humanVariants.Length > 0) types.Add("human");

        return types;
    }

    #endregion

    #region Validation

    void OnValidate()
    {

        // Check for empty set
        List<string> availableTypes = GetAvailableTypes();
        if (availableTypes.Count == 0)
        {
            Debug.LogWarning($"[ObstacleSet] '{name}' has NO obstacle variants assigned!");
        }
        else
        {
            Debug.Log($"[ObstacleSet] '{name}' has {availableTypes.Count} obstacle types: {string.Join(", ", availableTypes)}");
        }
    }

    #endregion
}