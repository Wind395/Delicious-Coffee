using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Shop Manager - UPDATED: Better validation and debugging
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
                _instance = FindAnyObjectByType<ShopManager>();
            }
            return _instance;
        }
    }
    
    #endregion

    #region Serialized Fields
    
    [Header("Shop Items")]
    [SerializeField] private List<ShopItemData> allShopItems = new List<ShopItemData>();
    
    // [Header("Debug")]
    // [SerializeField] private bool showDebugLogs = true;
    
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
    
    /// <summary>
    /// Validate items - UPDATED: Better debugging
    /// </summary>
    private void ValidateItems()
    {
        if (allShopItems == null || allShopItems.Count == 0)
        {
            Debug.LogError("[ShopManager] ❌ NO SHOP ITEMS ASSIGNED!");
            return;
        }
        
        // Count by type
        int characterCount = 0;
        int homeCount = 0;
        
        foreach (var item in allShopItems)
        {
            if (item == null)
            {
                //Debug.LogWarning("[ShopManager] ⚠️ NULL item in list!");
                continue;
            }
            
            if (item.itemType == ShopItemType.Character)
            {
                characterCount++;
            }
            else if (item.itemType == ShopItemType.Home)
            {
                homeCount++;
            }
            
            // if (showDebugLogs)
            // {
            //     Debug.Log($"[ShopManager] Item: {item.itemName} (ID: {item.itemID}, Type: {item.itemType})");
            // }
        }
        
        // Debug.Log($"[ShopManager] ═══ SHOP ITEMS LOADED ═══");
        // Debug.Log($"[ShopManager] Total: {allShopItems.Count}");
        // Debug.Log($"[ShopManager] Characters: {characterCount}");
        // Debug.Log($"[ShopManager] Homes: {homeCount}");
        // Debug.Log($"[ShopManager] ════════════════════════════");
        
        // Validate default items exist
        ValidateDefaultItems();
    }
    
    /// <summary>
    /// Validate default items exist - NEW
    /// </summary>
    private void ValidateDefaultItems()
    {
        bool hasDefaultChar = GetItemByID("char_default") != null;
        bool hasDefaultHome = GetItemByID("home_default") != null;
        
        // if (!hasDefaultChar)
        // {
        //     Debug.LogError("[ShopManager] ❌ Missing 'char_default' item!");
        // }
        
        // if (!hasDefaultHome)
        // {
        //     Debug.LogError("[ShopManager] ❌ Missing 'home_default' item!");
        //     Debug.LogError("[ShopManager] → Create a ShopItemData with:");
        //     Debug.LogError("[ShopManager]    - itemID = 'home_default'");
        //     Debug.LogError("[ShopManager]    - itemType = Home");
        //     Debug.LogError("[ShopManager]    - price = 0");
        // }
    }
    
    #endregion

    #region Public API
    
    public List<ShopItemData> GetAllItems()
    {
        return allShopItems;
    }

    /// <summary>
    /// Get items by type - UPDATED: Better debugging
    /// </summary>
    public List<ShopItemData> GetItemsByType(ShopItemType type)
    {
        var items = allShopItems.Where(item => item != null && item.itemType == type).ToList();
        
        // if (showDebugLogs)
        // {
        //     Debug.Log($"[ShopManager] GetItemsByType({type}): Found {items.Count} items");
        // }
        
        return items;
    }

    /// <summary>
    /// Get item by ID - UPDATED: Better debugging
    /// </summary>
    public ShopItemData GetItemByID(string itemID)
    {
        var item = allShopItems.Find(i => i != null && i.itemID == itemID);
        
        // if (showDebugLogs)
        // {
        //     if (item != null)
        //     {
        //         Debug.Log($"[ShopManager] GetItemByID('{itemID}'): ✓ Found '{item.itemName}'");
        //     }
        //     else
        //     {
        //         Debug.LogWarning($"[ShopManager] GetItemByID('{itemID}'): ✗ NOT FOUND");
        //     }
        // }
        
        return item;
    }

    public ShopItemData GetEquippedCharacter()
    {
        string equippedID = PlayerDataManager.Instance.EquippedCharacter;
        return GetItemByID(equippedID);
    }

    public ShopItemData GetEquippedHome()
    {
        string equippedID = PlayerDataManager.Instance.EquippedHome;
        return GetItemByID(equippedID);
    }
    
    #endregion
    
    #region Debug
    
    #if UNITY_EDITOR
    
    [ContextMenu("Debug: Print All Items")]
    private void DebugPrintAllItems()
    {
        // Debug.Log("═══════════════════════════════════");
        // Debug.Log("SHOP ITEMS LIST");
        // Debug.Log("═══════════════════════════════════");
        
        if (allShopItems == null || allShopItems.Count == 0)
        {
            //Debug.LogWarning("NO ITEMS!");
            return;
        }
        
        for (int i = 0; i < allShopItems.Count; i++)
        {
            var item = allShopItems[i];
            
            if (item == null)
            {
                Debug.LogWarning($"[{i}] NULL ITEM");
                continue;
            }
            
            // Debug.Log($"[{i}] {item.itemName}");
            // Debug.Log($"     ID: {item.itemID}");
            // Debug.Log($"     Type: {item.itemType}");
            // Debug.Log($"     Price: {item.price}");
            // Debug.Log($"     Prefab: {(item.prefab != null ? item.prefab.name : "NULL")}");
        }
        
        //Debug.Log("═══════════════════════════════════");
    }
    
    [ContextMenu("Debug: Check Default Items")]
    private void DebugCheckDefaults()
    {
        //Debug.Log("═══ CHECKING DEFAULT ITEMS ═══");
        
        var defaultChar = GetItemByID("char_default");
        var defaultHome = GetItemByID("home_default");
        
        //Debug.Log($"char_default: {(defaultChar != null ? "✓ EXISTS" : "✗ MISSING")}");
        //Debug.Log($"home_default: {(defaultHome != null ? "✓ EXISTS" : "✗ MISSING")}");
        
        // if (defaultHome != null)
        // {
        //     Debug.Log($"  Name: {defaultHome.itemName}");
        //     Debug.Log($"  Type: {defaultHome.itemType}");
        //     Debug.Log($"  Price: {defaultHome.price}");
        // }
    }
    
    #endif
    
    #endregion
}