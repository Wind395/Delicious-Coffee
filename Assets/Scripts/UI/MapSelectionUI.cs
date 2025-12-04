using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Map Selection UI - Chọn Map 1 hoặc Map 2
/// </summary>
public class MapSelectionUI : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Panels")]
    [SerializeField] private GameObject mapSelectionPanel;
    
    [Header("Map Item Prefab")]
    [SerializeField] private GameObject mapItemPrefab;
    
    [Header("Content")]
    [SerializeField] private Transform mapContainer;
    
    [Header("Buttons")]
    [SerializeField] private Button backButton;
    
    [Header("References")]
    [SerializeField] private LevelSelectionUI levelSelectionUI;
    [SerializeField] private ModeSelectionUI modeUI;
    
    // [Header("Debug")]
    // [SerializeField] private bool showDebugLogs = false;
    
    #endregion

    #region State
    
    private List<MapItemUI> _spawnedMapItems = new List<MapItemUI>();
    
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

    #region Map Selection
    
    /// <summary>
    /// Open map selection - Populate maps
    /// </summary>
    public void OpenMapSelection()
    {
        mapSelectionPanel?.SetActive(true);
        
        // Populate maps
        PopulateMaps();
    }
    
    /// <summary>
    /// Populate map items
    /// </summary>
    private void PopulateMaps()
    {
        // Clear old items
        ClearMapItems();
        
        // Get maps from database
        if (GameModeManager.Instance.Database == null)
        {
            //Debug.LogError("[MapSelectionUI] LevelDatabase not found!");
            return;
        }
        
        List<MapData> maps = GameModeManager.Instance.Database.GetAllMaps();
        
        if (maps == null || maps.Count == 0)
        {
            //Debug.LogWarning("[MapSelectionUI] No maps found!");
            return;
        }
        
        // Spawn map items
        foreach (MapData map in maps)
        {
            SpawnMapItem(map);
        }
        
        // if (showDebugLogs)
        // {
        //     Debug.Log($"[MapSelectionUI] Populated {maps.Count} maps");
        // }
    }
    
    /// <summary>
    /// Spawn map item UI
    /// </summary>
    private void SpawnMapItem(MapData mapData)
    {
        if (mapItemPrefab == null)
        {
            //Debug.LogError("[MapSelectionUI] Map item prefab not assigned!");
            return;
        }
        
        GameObject itemObj = Instantiate(mapItemPrefab, mapContainer);
        MapItemUI itemUI = itemObj.GetComponent<MapItemUI>();
        
        if (itemUI != null)
        {
            itemUI.Setup(mapData, this);
            _spawnedMapItems.Add(itemUI);
        }
    }
    
    /// <summary>
    /// Clear map items
    /// </summary>
    private void ClearMapItems()
    {
        foreach (MapItemUI item in _spawnedMapItems)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
        
        _spawnedMapItems.Clear();
    }
    
    #endregion

    #region Map Item Callback
    
    /// <summary>
    /// Called when map is selected
    /// </summary>
    public void OnMapSelected(MapData mapData)
    {
        if (mapData == null)
        {
            //Debug.LogError("[MapSelectionUI] Map data is null!");
            return;
        }
        
        // if (showDebugLogs)
        // {
        //     Debug.Log($"[MapSelectionUI] Map selected: {mapData.mapName}");
        // }
        
        AudioManager.Instance?.PlayButtonClickSound();
        
        // Set selected map
        GameModeManager.Instance.SelectMap(mapData.mapID);
        
        // Open level selection
        if (levelSelectionUI != null)
        {
            mapSelectionPanel?.SetActive(false);
            levelSelectionUI.OpenLevelSelection(mapData);
        }
        // else
        // {
        //     Debug.LogError("[MapSelectionUI] LevelSelectionUI not assigned!");
        // }
    }
    
    #endregion

    #region Button Handlers
    
    private void OnBackClicked()
    {
        AudioManager.Instance?.PlayButtonClickSound();
        
        mapSelectionPanel?.SetActive(false);
        
        // Back to mode selection
        if (modeUI != null)
        {
            //Debug.Log("[MapSelectionUI] Back to Mode Selection");
            modeUI.OpenModeSelection();
        }
        // else
        // {
        //     Debug.LogError("[MapSelectionUI] ModeSelectionUI not found!");
        // }
    }
    
    #endregion

    #region Public API
    
    public void CloseMapSelection()
    {
        mapSelectionPanel?.SetActive(false);
    }
    
    #endregion
}