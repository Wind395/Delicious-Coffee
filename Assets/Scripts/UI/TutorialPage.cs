using UnityEngine;

/// <summary>
/// Data container for a single tutorial page
/// Follows Data-Driven Design pattern
/// </summary>
[CreateAssetMenu(fileName = "TutorialPage", menuName = "Game/Tutorial Page", order = 1)]
public class TutorialPage : ScriptableObject
{
    [Header("Visual")]
    [SerializeField] private Sprite image;

    [Header("Content")]
    [SerializeField][TextArea(3, 6)] private string description;

    [Header("Optional")]
    [SerializeField] private string title; // Optional: Tiêu đề trang

    #region Properties

    public Sprite Image => image;
    public string Description => description;
    public string Title => title;

    #endregion

    #region Validation

    private void OnValidate()
    {
        if (image == null)
        {
            Debug.LogWarning($"[TutorialPage] {name}: Image is not assigned!");
        }

        if (string.IsNullOrEmpty(description))
        {
            Debug.LogWarning($"[TutorialPage] {name}: Description is empty!");
        }
    }

    #endregion
}