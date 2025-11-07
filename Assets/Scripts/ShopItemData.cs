using UnityEngine;

/// <summary>
/// Shop Item Data - ScriptableObject
/// SOLID: Single Responsibility - Data only
/// </summary>
[CreateAssetMenu(fileName = "ShopItem_", menuName = "Shop/Shop Item", order = 1)]
public class ShopItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemID; // Unique ID (e.g., "char_ninja", "toilet_gold")
    public string itemName;
    public Texture icon;
    public int price;

    [Header("Type")]
    public ShopItemType itemType;

    [Header("Prefab")]
    public GameObject prefab; // Character model or Toilet model

    [Header("Description")]
    [TextArea(3, 5)]
    public string description;
}

/// <summary>
/// Shop Item Types
/// </summary>
public enum ShopItemType
{
    Character,
    Toilet
}