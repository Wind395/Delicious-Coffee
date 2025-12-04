using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Ranking Panel UI - Displays in menu
/// </summary>
public class RankingPanelUI : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform entryContainer;
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private Button closeButton;
    
    [Header("Header")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI infoText;
    
    [Header("Animation")]
    [SerializeField] private bool useAnimation = true;
    [SerializeField] private float entrySpawnDelay = 0.05f;
    
    #endregion

    #region State
    
    private List<GameObject> _spawnedEntries = new List<GameObject>();
    
    #endregion

    #region Unity Lifecycle

    void Start()
    {
        //Initialize();
    }
    
    #endregion

    #region Initialization
    
    public void Initialize()
    {
        // Setup close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }
        
        // Subscribe to updates
        if (RankingManager.Instance != null)
        {
            RankingManager.Instance.OnRankingsUpdated += OnRankingsUpdated;
        }
        
        // Hide by default
        if (panel != null)
        {
            panel.SetActive(false);
        }
        
        //Debug.Log("[RankingPanelUI] ✓ Initialized");
    }
    
    #endregion

    #region Show/Hide
    
    /// <summary>
    /// Show ranking panel
    /// </summary>
    public void Show()
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }
        
        // Play sound
        AudioManager.Instance?.PlayButtonClickSound();
        
        // Refresh rankings
        RefreshRankings();
        
        // Update header
        UpdateHeader();
    }
    
    /// <summary>
    /// Hide ranking panel
    /// </summary>
    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
        
        ClearEntries();
    }
    
    #endregion

    #region Display
    
    /// <summary>
    /// Refresh rankings display
    /// </summary>
    public void RefreshRankings()
    {
        if (RankingManager.Instance == null)
        {
            //Debug.LogError("[RankingPanelUI] RankingManager not found!");
            return;
        }
        
        List<RankingEntry> rankings = RankingManager.Instance.GetRankings();
        
        if (useAnimation)
        {
            StartCoroutine(DisplayRankingsAnimated(rankings));
        }
        else
        {
            DisplayRankings(rankings);
        }
    }
    
    /// <summary>
    /// Display rankings immediately
    /// </summary>
    private void DisplayRankings(List<RankingEntry> rankings)
    {
        ClearEntries();
        
        for (int i = 0; i < rankings.Count; i++)
        {
            CreateEntry(i + 1, rankings[i]);
        }
    }
    
    /// <summary>
    /// Display rankings with animation
    /// </summary>
    private IEnumerator DisplayRankingsAnimated(List<RankingEntry> rankings)
    {
        ClearEntries();
        
        for (int i = 0; i < rankings.Count; i++)
        {
            CreateEntry(i + 1, rankings[i]);
            yield return new WaitForSecondsRealtime(entrySpawnDelay);
        }
    }
    
    /// <summary>
    /// Create single entry
    /// </summary>
    private void CreateEntry(int rank, RankingEntry entry)
    {
        if (entryPrefab == null || entryContainer == null)
        {
            //Debug.LogError("[RankingPanelUI] Entry prefab or container not assigned!");
            return;
        }
        
        GameObject obj = Instantiate(entryPrefab, entryContainer);
        
        RankingEntryUI ui = obj.GetComponent<RankingEntryUI>();
        if (ui != null)
        {
            ui.Setup(rank, entry);
        }
        
        _spawnedEntries.Add(obj);
    }
    
    /// <summary>
    /// Clear all entries
    /// </summary>
    private void ClearEntries()
    {
        foreach (GameObject obj in _spawnedEntries)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        _spawnedEntries.Clear();
    }
    
    /// <summary>
    /// Update header info
    /// </summary>
    private void UpdateHeader()
    {
        if (titleText != null)
        {
            titleText.text = "🏆 RANKING 🏆";
        }
        
        if (infoText != null && RankingManager.Instance != null)
        {
            float playerBest = RankingManager.Instance.GetPlayerBestDistance();
            int playerRank = RankingManager.Instance.GetPlayerRank();
            
            if (playerBest > 0)
            {
                infoText.text = $"Your Best: {playerBest:F0}m (Rank #{playerRank})";
            }
            else
            {
                infoText.text = "Play Endless Mode to set a record!";
            }
        }
    }
    
    #endregion

    #region Event Handlers
    
    private void OnRankingsUpdated()
    {
        // Auto refresh if panel is open
        if (panel != null && panel.activeSelf)
        {
            RefreshRankings();
        }
    }
    
    #endregion

    #region Cleanup
    
    void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
        }
        
        if (RankingManager.Instance != null)
        {
            RankingManager.Instance.OnRankingsUpdated -= OnRankingsUpdated;
        }
    }
    
    #endregion
}