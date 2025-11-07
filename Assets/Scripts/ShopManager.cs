using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Shop Manager - Manages shop items
/// SOLID: Single Responsibility - Shop logic only
/// </summary>
public class ShopManager : MonoBehaviour
{
    #region Singleton
    
    private static ShopManager _instance;
    public static ShopManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ShopManager>();
            }
            return _instance;
        }
    }
    
    #endregion

    #region Serialized Fields
    
    [Header("Shop Items")]
    [SerializeField] private List<ShopItemData> allShopItems = new List<ShopItemData>();
    
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
        
        ValidateItems();
    }
    
    #endregion

    #region Initialization
    
    private void ValidateItems()
    {
        if (allShopItems == null || allShopItems.Count == 0)
        {
            Debug.LogWarning("[ShopManager] No shop items assigned!");
        }
        else
        {
            Debug.Log($"[ShopManager] Loaded {allShopItems.Count} shop items");
        }
    }
    
    #endregion

    #region Public API
    
    /// <summary>
    /// Get all shop items
    /// </summary>
    public List<ShopItemData> GetAllItems()
    {
        return allShopItems;
    }

    /// <summary>
    /// Get items by type
    /// </summary>
    public List<ShopItemData> GetItemsByType(ShopItemType type)
    {
        return allShopItems.Where(item => item.itemType == type).ToList();
    }

    /// <summary>
    /// Get item by ID
    /// </summary>
    public ShopItemData GetItemByID(string itemID)
    {
        return allShopItems.Find(item => item.itemID == itemID);
    }

    /// <summary>
    /// Get equipped character data
    /// </summary>
    public ShopItemData GetEquippedCharacter()
    {
        string equippedID = PlayerDataManager.Instance.EquippedCharacter;
        return GetItemByID(equippedID);
    }

    /// <summary>
    /// Get equipped toilet data
    /// </summary>
    public ShopItemData GetEquippedToilet()
    {
        string equippedID = PlayerDataManager.Instance.EquippedToilet;
        return GetItemByID(equippedID);
    }
    
    #endregion
}