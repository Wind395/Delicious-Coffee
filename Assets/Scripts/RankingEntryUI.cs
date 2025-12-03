using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI for single ranking entry
/// </summary>
public class RankingEntryUI : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private Image background;

    [SerializeField] private Color playerEntryColor = Color.yellow;
    
    #endregion

    #region Public Methods
    
    /// <summary>
    /// Setup entry UI
    /// </summary>
    public void Setup(int rank, RankingEntry entry)
    {
        // Rank
        if (rankText != null)
        {
            rankText.text = GetRankString(rank);

            if (entry.isPlayer)
            {
                rankText.color = playerEntryColor;
            }
        }
        
        // Name
        if (nameText != null)
        {
            nameText.text = entry.playerName;

            if (entry.isPlayer)
            {
                nameText.color = playerEntryColor;
            }
        }
        
        // Distance
        if (distanceText != null)
        {
            distanceText.text = entry.GetFormattedDistance();
            if (entry.isPlayer)
            {
                distanceText.color = playerEntryColor;
            }
        }
    }
    
    #endregion

    #region Helper Methods
    
    private string GetRankString(int rank)
    {
        switch (rank)
        {
            case 1: return "#1";
            case 2: return "#2";
            case 3: return "#3";
            default: return $"#{rank}";
        }
    }
    
    #endregion
}