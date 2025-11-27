using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Level Item UI - FIXED: Allow replay completed levels
/// </summary>
public class LevelItemUI : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI levelNumberText;
    [SerializeField] private TextMeshProUGUI levelNameText;
    [SerializeField] private TextMeshProUGUI targetDistanceText;
    [SerializeField] private Image thumbnail;
    [SerializeField] private Button playButton;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private GameObject completedBadge; // ← Keep badge, but allow replay
    [SerializeField] private Image difficultyBar;

    [Header("Difficulty Colors")]
    [SerializeField] private Color[] difficultyColors = new Color[]
    {
        Color.green,   // Difficulty 1
        Color.yellow,  // Difficulty 2
        Color.yellow,  // Difficulty 3
        Color.red,     // Difficulty 4
        Color.red      // Difficulty 5
    };

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    #endregion

    #region State

    private LevelData _levelData;
    private LevelSelectionUI _levelSelectionUI;

    #endregion

    #region Setup

    public void Setup(LevelData levelData, LevelSelectionUI levelSelectionUI)
    {
        _levelData = levelData;
        _levelSelectionUI = levelSelectionUI;
        
        // Set level number
        if (levelNumberText != null)
        {
            levelNumberText.text = $"{levelData.levelNumber}";
        }
        
        // Set level name
        if (levelNameText != null)
        {
            levelNameText.text = levelData.levelName;
        }
        
        // Set target distance
        if (targetDistanceText != null)
        {
            targetDistanceText.text = $"Goal: {levelData.targetDistance}m";
        }
        
        // Set thumbnail
        if (thumbnail != null && levelData.thumbnail != null)
        {
            thumbnail.sprite = levelData.thumbnail;
        }
        
        // Set difficulty color
        if (difficultyBar != null)
        {
            int diffIndex = Mathf.Clamp(levelData.difficulty - 1, 0, difficultyColors.Length - 1);
            difficultyBar.color = difficultyColors[diffIndex];
        }
        
        // Setup button
        playButton?.onClick.RemoveAllListeners();
        playButton?.onClick.AddListener(OnPlayClicked);
        
        // Update states
        UpdateLockState();
        UpdateCompletedState();
    }

    #endregion

    #region Update UI - FIXED

    /// <summary>
    /// Update lock state - FIXED: Only check unlock, not completed
    /// </summary>
    private void UpdateLockState()
    {
        bool isUnlocked = IsLevelUnlocked();
        
        // Show/hide lock overlay
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!isUnlocked);
        }
        
        // ═══ FIXED: Always enable button if unlocked (even if completed) ═══
        if (playButton != null)
        {
            playButton.interactable = isUnlocked; // ← Allow replay
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[LevelItemUI] Level {_levelData.levelNumber}: Unlocked={isUnlocked}");
        }
    }

    /// <summary>
    /// Update completed badge - Show badge but still allow play
    /// </summary>
    private void UpdateCompletedState()
    {
        bool isCompleted = PlayerDataManager.Instance.IsLevelCompleted(_levelData.levelID);
        
        // Show completed badge (visual feedback only)
        if (completedBadge != null)
        {
            completedBadge.SetActive(isCompleted);
        }
        
        // ═══ OPTIONAL: Change button text if completed ═══
        // TextMeshProUGUI buttonText = playButton?.GetComponentInChildren<TextMeshProUGUI>();
        // if (buttonText != null)
        // {
        //     buttonText.text = isCompleted ? "REPLAY" : "PLAY";
        // }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Check if level is unlocked - UNCHANGED
    /// </summary>
    private bool IsLevelUnlocked()
    {
        if (_levelData.isUnlockedByDefault)
        {
            return true;
        }
        
        // Level 1 always unlocked
        if (_levelData.levelNumber == 1)
        {
            return true;
        }
        
        // Check if required levels are completed
        if (_levelData.requiredLevels != null && _levelData.requiredLevels.Length > 0)
        {
            foreach (string requiredLevelID in _levelData.requiredLevels)
            {
                if (!PlayerDataManager.Instance.IsLevelCompleted(requiredLevelID))
                {
                    return false; // Required level not completed
                }
            }
        }
        
        return true;
    }

    #endregion

    #region Button Handler

    private void OnPlayClicked()
    {
        if (_levelSelectionUI != null && _levelData != null)
        {
            if (showDebugLogs)
            {
                bool isCompleted = PlayerDataManager.Instance.IsLevelCompleted(_levelData.levelID);
                Debug.Log($"[LevelItemUI] Playing level {_levelData.levelNumber} (Replay: {isCompleted})");
            }
            
            _levelSelectionUI.OnLevelSelected(_levelData);
        }
    }

    #endregion
}