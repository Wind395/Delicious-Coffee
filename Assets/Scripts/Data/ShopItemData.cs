// ═══════════════════════════════════════════════════════════════
// CHANGES FOR DOG CHASE THEME:
// - Changed: Toilet → Home in enum
// ═══════════════════════════════════════════════════════════════

using UnityEngine;

[CreateAssetMenu(fileName = "ShopItem_", menuName = "Shop/Shop Item", order = 1)]
public class ShopItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemID;
    public string itemName;
    public Texture icon;
    public int price;

    [Header("Type")]
    public ShopItemType itemType;

    [Header("Prefab")]
    public GameObject prefab; // Character model or Home model

    [Header("Description")]
    [TextArea(3, 5)]
    public string description;
}

/// <summary>
/// Shop Item Types - MODIFIED
/// </summary>
public enum ShopItemType
{
    Character,
    Home // CHANGED from Toilet
}