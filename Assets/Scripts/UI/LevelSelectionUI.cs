using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Level Selection UI - Chọn Level 1-5 trong map
/// </summary>
public class LevelSelectionUI : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Panels")]
    [SerializeField] private GameObject levelSelectionPanel;
    
    [Header("Map Info")]
    [SerializeField] private TextMeshProUGUI mapNameText;
    [SerializeField] private RawImage mapBanner;
    
    [Header("Level Item Prefab")]
    [SerializeField] private GameObject levelItemPrefab;
    
    [Header("Content")]
    [SerializeField] private Transform levelContainer;
    
    [Header("Buttons")]
    [SerializeField] private Button backButton;

    [Header("References")]
    [SerializeField] private MapSelectionUI mapUI;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    #endregion

    #region State
    
    private MapData _currentMap;
    private List<LevelItemUI> _spawnedLevelItems = new List<LevelItemUI>();
    
    #endregion

    #region Unity Lifecycle
    
    void Start()
    {
        SetupButtons();
    }
    
    #endregion

    #region Setup
    
    private void SetupButtons()
    {
        backButton?.onClick.AddListener(OnBackClicked);
    }
    
    #endregion

    #region Level Selection
    
    /// <summary>
    /// Open level selection for specific map
    /// </summary>
    public void OpenLevelSelection(MapData mapData)
    {
        if (mapData == null)
        {
            Debug.LogError("[LevelSelectionUI] Map data is null!");
            return;
        }
        
        _currentMap = mapData;
        
        levelSelectionPanel?.SetActive(true);
        
        // Update map info
        UpdateMapInfo();
        
        // Populate levels
        PopulateLevels();
    }
    
    /// <summary>
    /// Update map info display
    /// </summary>
    private void UpdateMapInfo()
    {
        if (_currentMap == null)
        {
            return;
        }
        
        if (mapNameText != null)
        {
            mapNameText.text = _currentMap.mapName;
        }
        
        if (mapBanner != null && _currentMap.mapBanner != null)
        {
            mapBanner.texture = _currentMap.mapBanner.texture;
        }
    }
    
    /// <summary>
    /// Populate level items
    /// </summary>
    private void PopulateLevels()
    {
        // Clear old items
        ClearLevelItems();
        
        if (_currentMap == null || _currentMap.levels == null)
        {
            return;
        }
        
        // Spawn level items
        foreach (LevelData level in _currentMap.levels)
        {
            SpawnLevelItem(level);
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[LevelSelectionUI] Populated {_currentMap.levels.Count} levels");
        }
    }
    
    /// <summary>
    /// Spawn level item UI
    /// </summary>
    private void SpawnLevelItem(LevelData levelData)
    {
        if (levelItemPrefab == null)
        {
            Debug.LogError("[LevelSelectionUI] Level item prefab not assigned!");
            return;
        }
        
        GameObject itemObj = Instantiate(levelItemPrefab, levelContainer);
        LevelItemUI itemUI = itemObj.GetComponent<LevelItemUI>();
        
        if (itemUI != null)
        {
            itemUI.Setup(levelData, this);
            _spawnedLevelItems.Add(itemUI);
        }
    }
    
    /// <summary>
    /// Clear level items
    /// </summary>
    private void ClearLevelItems()
    {
        foreach (LevelItemUI item in _spawnedLevelItems)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
        
        _spawnedLevelItems.Clear();
    }
    
    #endregion

    #region Level Item Callback
    
    /// <summary>
    /// Called when level is selected
    /// </summary>
    public void OnLevelSelected(LevelData levelData)
    {
        if (levelData == null)
        {
            Debug.LogError("[LevelSelectionUI] Level data is null!");
            return;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[LevelSelectionUI] Level selected: {levelData.levelName}");
        }
        
        AudioManager.Instance?.PlayButtonClickSound();
        
        // Set selected level
        GameModeManager.Instance.SelectLevel(levelData.levelID);
        
        // Start game
        SceneController.Instance.LoadGameplayScene();
    }
    
    #endregion

    #region Button Handlers
    
    private void OnBackClicked()
    {
        AudioManager.Instance?.PlayButtonClickSound();
        
        levelSelectionPanel?.SetActive(false);
        
        // Back to map selection
        if (mapUI != null)
        {
            Debug.Log("[LevelSelectionUI] Back to Map Selection");
            mapUI.OpenMapSelection();
        }
        else
        {
            Debug.LogError("[LevelSelectionUI] MapSelectionUI not assigned!");
        }
    }
    
    #endregion

    #region Public API
    
    public void CloseLevelSelection()
    {
        levelSelectionPanel?.SetActive(false);
    }
    
    #endregion
}