using UnityEngine;
using TMPro;

/// <summary>
/// Simple endless distance display - Text only
/// </summary>
public class EndlessDistanceDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI distanceText;

    [Header("Display Format")]
    [SerializeField] private bool showBestDistance = true;
    [SerializeField] private string format = "{0:F0}m"; // Example: "1234m"

    private float _bestDistance = 0f;

    void Start()
    {
        // Only active in Endless mode
        if (GameModeManager.Instance == null || 
            GameModeManager.Instance.CurrentMode != GameMode.Endless)
        {
            distanceText.gameObject.SetActive(false);
            return;
        }
    }

    void Update()
    {
        if (GameManager.Instance == null || 
            GameManager.Instance.CurrentState != GameState.Playing)
            return;

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (DistanceTracker.Instance == null || distanceText == null) return;

        float currentDistance = DistanceTracker.Instance.CurrentDistance;

        // Simple format
        if (showBestDistance)
        {
            distanceText.text = $"{currentDistance:F0}m";
        }
        else
        {
            distanceText.text = string.Format(format, currentDistance);
        }
    }
}