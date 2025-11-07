using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shop Item UI - FIXED: Proper equipped state check
/// </summary>
public class ShopItemUI : MonoBehaviour
{
    #region UI References
    
    [Header("UI Elements")]
    [SerializeField] private RawImage iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button actionButton;
    [SerializeField] private Sprite buyButtonSprite;
    [SerializeField] private Sprite equipButtonSprite;
    [SerializeField] private Sprite equippedButtonSprite;
    [SerializeField] private GameObject equippedIndicator;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    #endregion

    #region State
    
    private ShopItemData _itemData;
    private ShopUIManager _shopUIManager;
    
    #endregion

    #region Setup
    
    public void Setup(ShopItemData itemData, ShopUIManager shopUIManager)
    {
        _itemData = itemData;
        _shopUIManager = shopUIManager;
        
        // Set icon
        if (iconImage != null && itemData.icon != null)
        {
            iconImage.texture = itemData.icon;
        }
        
        // Set name
        if (nameText != null)
        {
            nameText.text = itemData.itemName;
        }

        // Set price
        if (priceText != null)
        {
            priceText.text = $"{itemData.price}";
        }
        
        // Set description
        if (descriptionText != null)
        {
            descriptionText.text = itemData.description;
        }
        
        // Setup button
        actionButton?.onClick.RemoveAllListeners();
        actionButton?.onClick.AddListener(OnActionButtonClicked);
        
        // Initial refresh
        Refresh();
    }
    
    #endregion

    #region Update - FIXED
    
    /// <summary>
    /// Refresh UI state - FIXED: Proper equipped check
    /// </summary>
    public void Refresh()
    {
        if (_itemData == null)
        {
            Debug.LogWarning("[ShopItemUI] ItemData is null!");
            return;
        }
        
        bool isPurchased = PlayerDataManager.Instance.IsPurchased(_itemData.itemID);
        bool isEquipped = PlayerDataManager.Instance.IsEquipped(_itemData.itemID);
        
        if (showDebugLogs)
        {
            Debug.Log($"[ShopItemUI] Refresh {_itemData.itemID}: " +
                     $"Purchased={isPurchased}, Equipped={isEquipped}");
        }
        
        // ═══ FIXED: Clear priority logic ═══
        if (isEquipped)
        {
            ShowEquippedState();
        }
        else if (isPurchased)
        {
            ShowEquipState();
        }
        else
        {
            ShowBuyState();
        }
    }

    private void ShowBuyState()
    {
        if (buyButtonSprite != null)
        {
            actionButton.image.sprite = buyButtonSprite;
        }

        if (priceText != null)
        {
            priceText.gameObject.SetActive(true);
        }
        
        if (descriptionText != null)
        {
            descriptionText.gameObject.SetActive(true);
        }
        
        if (equippedIndicator != null)
        {
            equippedIndicator.SetActive(false);
        }
        
        if (actionButton != null)
        {
            // Check if can afford
            bool canAfford = PlayerDataManager.Instance.Gold >= _itemData.price;
            actionButton.interactable = canAfford;
            
            // Visual feedback
            var colors = actionButton.colors;
            colors.normalColor = canAfford ? Color.white : Color.gray;
            actionButton.colors = colors;
        }
    }

    private void ShowEquipState()
    {
        if (equipButtonSprite != null)
        {
            actionButton.image.sprite = equipButtonSprite;
        }

        if (priceText != null)
        {
            priceText.gameObject.SetActive(false);
        }

        if (descriptionText != null)
        {
            descriptionText.gameObject.SetActive(false);
        }
        
        if (equippedIndicator != null)
        {
            equippedIndicator.SetActive(false);
        }
        
        if (actionButton != null)
        {
            actionButton.interactable = true;
            
            var colors = actionButton.colors;
            colors.normalColor = Color.white;
            actionButton.colors = colors;
        }
    }

    private void ShowEquippedState()
    {
        if (equippedButtonSprite != null)
        {
            actionButton.image.sprite = equippedButtonSprite;
        }

        if (priceText != null)
        {
            priceText.gameObject.SetActive(false);
        }
        
        if (descriptionText != null)
        {
            descriptionText.gameObject.SetActive(false);
        }
        
        if (equippedIndicator != null)
        {
            equippedIndicator.SetActive(true);
        }
        
        if (actionButton != null)
        {
            actionButton.interactable = false;
            
            var colors = actionButton.colors;
            colors.normalColor = Color.green;
            actionButton.colors = colors;
        }
    }
    
    #endregion

    #region Button Handler
    
    private void OnActionButtonClicked()
    {
        if (_itemData == null || _shopUIManager == null) return;
        
        bool isPurchased = PlayerDataManager.Instance.IsPurchased(_itemData.itemID);
        
        if (isPurchased)
        {
            // Equip
            if (showDebugLogs)
                Debug.Log($"[ShopItemUI] Equipping {_itemData.itemID}");
            
            _shopUIManager.EquipItem(_itemData);
        }
        else
        {
            // Buy
            if (showDebugLogs)
                Debug.Log($"[ShopItemUI] Buying {_itemData.itemID}");
            
            _shopUIManager.BuyItem(_itemData);
        }
    }
    
    #endregion
}