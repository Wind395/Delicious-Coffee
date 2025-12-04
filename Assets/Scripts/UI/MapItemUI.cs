using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Map Item UI - Hiển thị 1 map trong list
/// </summary>
public class MapItemUI : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("UI Elements")]
    [SerializeField] private Image mapIcon;
    [SerializeField] private TextMeshProUGUI mapNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Button selectButton;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private Image backgroundImage;
    
    
    #endregion

    #region State
    
    private MapData _mapData;
    private MapSelectionUI _mapSelectionUI;
    
    #endregion

    #region Setup
    
    public void Setup(MapData mapData, MapSelectionUI mapSelectionUI)
    {
        _mapData = mapData;
        _mapSelectionUI = mapSelectionUI;
        
        // Set icon
        if (mapIcon != null && mapData.mapIcon != null)
        {
            mapIcon.sprite = mapData.mapIcon;
        }
        
        // Set name
        if (mapNameText != null)
        {
            mapNameText.text = mapData.mapName;
        }
        
        // Set description
        if (descriptionText != null)
        {
            descriptionText.text = mapData.description;
        }
        
        // Set theme color
        if (backgroundImage != null)
        {
            backgroundImage.color = mapData.themeColor;
        }
        
        // Setup button
        selectButton?.onClick.RemoveAllListeners();
        selectButton?.onClick.AddListener(OnSelectClicked);
        
        // Update lock state
        UpdateLockState();
        
        // Update progress
        UpdateProgress();
    }
    
    #endregion

    #region Update UI
    
    /// <summary>
    /// Update lock state
    /// </summary>
    private void UpdateLockState()
    {
        bool isUnlocked = IsMapUnlocked();
        
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!isUnlocked);
        }
        
        if (selectButton != null)
        {
            selectButton.interactable = isUnlocked;
        }
    }
    
    /// <summary>
    /// Update progress (X/5 levels completed)
    /// </summary>
    private void UpdateProgress()
    {
        if (progressText == null || _mapData == null)
        {
            return;
        }
        
        int totalLevels = _mapData.GetLevelCount();
        int completedLevels = GetCompletedLevelCount();
        
        progressText.text = $"{completedLevels}/{totalLevels} Levels";
    }
    
    #endregion

    #region Helper Methods
    
    /// <summary>
    /// Check if map is unlocked
    /// </summary>
    private bool IsMapUnlocked()
    {
        if (_mapData.isUnlockedByDefault)
        {
            return true;
        }
        
        // Check if required map is completed
        if (!string.IsNullOrEmpty(_mapData.requiredMapID))
        {
            return PlayerDataManager.Instance.IsMapCompleted(_mapData.requiredMapID);
        }
        
        return true;
    }
    
    /// <summary>
    /// Get completed level count in this map
    /// </summary>
    private int GetCompletedLevelCount()
    {
        if (_mapData == null)
        {
            return 0;
        }
        
        int count = 0;
        
        foreach (LevelData level in _mapData.levels)
        {
            if (PlayerDataManager.Instance.IsLevelCompleted(level.levelID))
            {
                count++;
            }
        }
        
        return count;
    }
    
    #endregion

    #region Button Handler
    
    private void OnSelectClicked()
    {
        if (_mapSelectionUI != null && _mapData != null)
        {
            _mapSelectionUI.OnMapSelected(_mapData);
        }
    }
    
    #endregion
}