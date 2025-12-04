using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Shop UI Manager - Manages shop UI
/// SOLID: Single Responsibility - Shop UI only
/// </summary>
public class ShopUIManager : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Panels")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject characterPanel;
    [SerializeField] private GameObject homePanel;
    
    [Header("Gold Display")]
    [SerializeField] private TextMeshProUGUI goldText;
    
    [Header("Shop Item Prefab")]
    [SerializeField] private GameObject shopItemPrefab;
    
    [Header("Content Containers")]
    [SerializeField] private Transform characterContent;
    [SerializeField] private Transform homeContent;
    
    [Header("Tab Buttons")]
    [SerializeField] private Button charactersTabButton;
    [SerializeField] private Button homesTabButton;
    [SerializeField] private Button backButton;
    
    [Header("Colors")]
    [SerializeField] private Color selectedTabColor = Color.cyan;
    [SerializeField] private Color normalTabColor = Color.white;
    
    #endregion

    #region State
    
    private ShopItemType _currentTab = ShopItemType.Character;
    private List<ShopItemUI> _spawnedItemUIs = new List<ShopItemUI>();
    
    #endregion

    #region Unity Lifecycle
    
    void Start()
    {
        Initialize();
    }
    
    #endregion

    #region Initialization
    
    private void Initialize()
    {
        // Subscribe to events
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnGoldChanged += UpdateGoldDisplay;
            PlayerDataManager.Instance.OnItemPurchased += OnItemPurchased;
            PlayerDataManager.Instance.OnItemEquipped += OnItemEquipped;
        }
        
        // Setup buttons
        charactersTabButton?.onClick.AddListener(() => SwitchTab(ShopItemType.Character));
        homesTabButton?.onClick.AddListener(() => SwitchTab(ShopItemType.Home));
        backButton?.onClick.AddListener(CloseShop);
        
        // Initial setup
        UpdateGoldDisplay(PlayerDataManager.Instance.Gold);
        
        // Populate shop (don't show by default)
        shopPanel?.SetActive(false);
    }
    
    #endregion

    #region Shop Control
    
    /// <summary>
    /// Open shop
    /// </summary>
    public void OpenShop()
    {
        shopPanel?.SetActive(true);
        SwitchTab(ShopItemType.Character);
        
        AudioManager.Instance?.PlayButtonClickSound();
    }

    /// <summary>
    /// Close shop
    /// </summary>
    public void CloseShop()
    {
        shopPanel?.SetActive(false);
        
        AudioManager.Instance?.PlayButtonClickSound();
    }

    /// <summary>
    /// Switch tab (Characters/Toilets)
    /// </summary>
    private void SwitchTab(ShopItemType tabType)
    {
        _currentTab = tabType;
        
        // Update tab visuals
        UpdateTabButtons();
        
        // Show/hide panels
        characterPanel?.SetActive(tabType == ShopItemType.Character);
        homePanel?.SetActive(tabType == ShopItemType.Home);
        
        // Populate items
        PopulateShopItems(tabType);
        
        AudioManager.Instance?.PlayButtonClickSound();
    }

    private void UpdateTabButtons()
    {
        if (charactersTabButton != null)
        {
            Image img = charactersTabButton.GetComponent<Image>();
            if (img != null)
            {
                img.color = _currentTab == ShopItemType.Character ? selectedTabColor : normalTabColor;
            }
        }
        
        if (homesTabButton != null)
        {
            Image img = homesTabButton.GetComponent<Image>();
            if (img != null)
            {
                img.color = _currentTab == ShopItemType.Home ? selectedTabColor : normalTabColor;
            }
        }
    }
    
    #endregion

    #region Shop Items Population
    
    /// <summary>
    /// Populate shop items by type
    /// </summary>
    private void PopulateShopItems(ShopItemType type)
    {
        // Clear existing items
        ClearShopItems();
        
        // Get items
        List<ShopItemData> items = ShopManager.Instance.GetItemsByType(type);
        
        // Get correct container
        Transform container = type == ShopItemType.Character ? characterContent : homeContent;
        
        if (container == null)
        {
            //Debug.LogError("[ShopUI] Container is null!");
            return;
        }
        
        // Spawn item UIs
        foreach (ShopItemData item in items)
        {
            SpawnShopItemUI(item, container);
        }
        
        //Debug.Log($"[ShopUI] Populated {items.Count} {type} items");
    }

    /// <summary>
    /// Spawn shop item UI
    /// </summary>
    private void SpawnShopItemUI(ShopItemData itemData, Transform container)
    {
        if (shopItemPrefab == null)
        {
            //Debug.LogError("[ShopUI] Shop item prefab not assigned!");
            return;
        }
        
        GameObject itemObj = Instantiate(shopItemPrefab, container);
        ShopItemUI itemUI = itemObj.GetComponent<ShopItemUI>();
        
        if (itemUI != null)
        {
            itemUI.Setup(itemData, this);
            _spawnedItemUIs.Add(itemUI);
        }
    }

    /// <summary>
    /// Clear all shop item UIs
    /// </summary>
    private void ClearShopItems()
    {
        foreach (ShopItemUI itemUI in _spawnedItemUIs)
        {
            if (itemUI != null)
            {
                Destroy(itemUI.gameObject);
            }
        }
        
        _spawnedItemUIs.Clear();
    }
    
    #endregion

    #region Shop Actions - Called by ShopItemUI
    
    /// <summary>
    /// Buy item
    /// </summary>
    public void BuyItem(ShopItemData item)
    {
        bool success = PlayerDataManager.Instance.PurchaseItem(item);
        
        if (success)
        {
            //Debug.Log($"[ShopUI] Purchased: {item.itemName}");
            AudioManager.Instance?.PlayCoinSound();
        }
        // else
        // {
        //     Debug.LogWarning($"[ShopUI] Failed to purchase: {item.itemName}");
        //     // TODO: Show "Not enough gold" popup
        // }
    }

    /// <summary>
    /// Equip item
    /// </summary>
    public void EquipItem(ShopItemData item)
    {
        PlayerDataManager.Instance.EquipItem(item);
        
        //Debug.Log($"[ShopUI] Equipped: {item.itemName}");
        AudioManager.Instance?.PlayButtonClickSound();
    }
    
    #endregion

    #region UI Updates
    
    /// <summary>
    /// Update gold display
    /// </summary>
    private void UpdateGoldDisplay(int gold)
    {
        if (goldText != null)
        {
            goldText.text = $"{gold}";
        }
    }

    /// <summary>
    /// Refresh item UIs when purchase happens
    /// </summary>
    private void OnItemPurchased(string itemID)
    {
        RefreshAllItemUIs();
    }

    /// <summary>
    /// Refresh item UIs when equip happens
    /// </summary>
    private void OnItemEquipped(string itemID, ShopItemType type)
    {
        RefreshAllItemUIs();
    }

    /// <summary>
    /// Refresh all item UIs (update button states)
    /// </summary>
    private void RefreshAllItemUIs()
    {
        foreach (ShopItemUI itemUI in _spawnedItemUIs)
        {
            if (itemUI != null)
            {
                itemUI.Refresh();
            }
        }
    }
    
    #endregion

    #region Cleanup
    
    void OnDestroy()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnGoldChanged -= UpdateGoldDisplay;
            PlayerDataManager.Instance.OnItemPurchased -= OnItemPurchased;
            PlayerDataManager.Instance.OnItemEquipped -= OnItemEquipped;
        }
    }
    
    #endregion
}