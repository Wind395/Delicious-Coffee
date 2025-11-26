using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// JSON Section Loader - Load sections from JSON files
/// </summary>
public class JSONSectionLoader : MonoBehaviour
{
    [Header("JSON File")]
    [Tooltip("Đặt file JSON vào Resources/SectionData/")]
    public string jsonFileName = "sections";

    [Header("Loaded Data")]
    public SectionLibrary loadedLibrary;

    private Dictionary<int, List<SectionData>> sectionsByDifficulty;

    #region Initialization

    /// <summary>
    /// Load JSON file từ Resources
    /// </summary>
    public bool LoadSections()
    {
        try
        {
            // Load từ Resources folder
            TextAsset jsonFile = Resources.Load<TextAsset>($"SectionData/{jsonFileName}");

            if (jsonFile == null)
            {
                Debug.LogError($"[JSONLoader] Cannot find JSON file: Resources/SectionData/{jsonFileName}.json");
                return false;
            }

            // Parse JSON
            SectionLibraryWrapper wrapper = JsonUtility.FromJson<SectionLibraryWrapper>(jsonFile.text);

            if (wrapper == null || wrapper.sectionLibrary == null)
            {
                Debug.LogError("[JSONLoader] Failed to parse JSON!");
                return false;
            }

            loadedLibrary = wrapper.sectionLibrary;

            // Index by difficulty
            IndexSectionsByDifficulty();

            Debug.Log($"[JSONLoader] ✓ Loaded {loadedLibrary.sections.Count} sections from JSON");
            Debug.Log($"[JSONLoader] Version: {loadedLibrary.metadata.version}");

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[JSONLoader] Error loading JSON: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Load từ file path (cho editor tools)
    /// </summary>
    public bool LoadFromPath(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"[JSONLoader] File not found: {path}");
                return false;
            }

            string json = File.ReadAllText(path);
            SectionLibraryWrapper wrapper = JsonUtility.FromJson<SectionLibraryWrapper>(json);

            if (wrapper == null || wrapper.sectionLibrary == null)
            {
                Debug.LogError("[JSONLoader] Failed to parse JSON!");
                return false;
            }

            loadedLibrary = wrapper.sectionLibrary;
            IndexSectionsByDifficulty();

            Debug.Log($"[JSONLoader] ✓ Loaded from file: {path}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[JSONLoader] Error: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Index sections theo difficulty level
    /// </summary>
    private void IndexSectionsByDifficulty()
    {
        sectionsByDifficulty = new Dictionary<int, List<SectionData>>();

        foreach (var section in loadedLibrary.sections)
        {
            int diff = section.difficulty;

            if (!sectionsByDifficulty.ContainsKey(diff))
            {
                sectionsByDifficulty[diff] = new List<SectionData>();
            }

            sectionsByDifficulty[diff].Add(section);
        }

        Debug.Log($"[JSONLoader] Indexed sections:");
        foreach (var kvp in sectionsByDifficulty)
        {
            Debug.Log($"  Difficulty {kvp.Key}: {kvp.Value.Count} sections");
        }
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Get random section theo difficulty
    /// </summary>
    public SectionData GetRandomSection(int maxDifficulty)
    {
        if (loadedLibrary == null || loadedLibrary.sections.Count == 0)
        {
            Debug.LogError("[JSONLoader] No sections loaded!");
            return null;
        }

        // Get all sections with difficulty <= maxDifficulty
        List<SectionData> validSections = new List<SectionData>();

        foreach (var section in loadedLibrary.sections)
        {
            if (section.difficulty <= maxDifficulty)
            {
                validSections.Add(section);
            }
        }

        if (validSections.Count == 0)
        {
            Debug.LogWarning($"[JSONLoader] No sections found for difficulty <= {maxDifficulty}");
            return loadedLibrary.sections[Random.Range(0, loadedLibrary.sections.Count)];
        }

        return validSections[Random.Range(0, validSections.Count)];
    }

    /// <summary>
    /// Get section by ID
    /// </summary>
    public SectionData GetSectionByID(string id)
    {
        if (loadedLibrary == null) return null;

        return loadedLibrary.sections.Find(s => s.id == id);
    }

    /// <summary>
    /// Get all sections với difficulty cụ thể
    /// </summary>
    public List<SectionData> GetSectionsByDifficulty(int difficulty)
    {
        if (sectionsByDifficulty == null || !sectionsByDifficulty.ContainsKey(difficulty))
        {
            return new List<SectionData>();
        }

        return sectionsByDifficulty[difficulty];
    }

    /// <summary>
    /// Get total section count
    /// </summary>
    public int GetSectionCount()
    {
        return loadedLibrary?.sections.Count ?? 0;
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validate loaded data
    /// </summary>
    public bool ValidateSections()
    {
        if (loadedLibrary == null || loadedLibrary.sections == null)
        {
            Debug.LogError("[JSONLoader] No data to validate!");
            return false;
        }

        bool isValid = true;

        foreach (var section in loadedLibrary.sections)
        {
            // Check required fields
            if (string.IsNullOrEmpty(section.id))
            {
                Debug.LogError($"[JSONLoader] Section missing ID!");
                isValid = false;
            }

            if (section.length <= 0)
            {
                Debug.LogError($"[JSONLoader] Section {section.id} has invalid length: {section.length}");
                isValid = false;
            }

            // Check obstacles
            if (section.obstacles != null)
            {
                foreach (var obs in section.obstacles)
                {
                    if (obs.lane < 0 || obs.lane > 2)
                    {
                        Debug.LogWarning($"[JSONLoader] Section {section.id}: Invalid lane {obs.lane}");
                    }

                    if (obs.zPosition < 0 || obs.zPosition > section.length)
                    {
                        Debug.LogWarning($"[JSONLoader] Section {section.id}: Obstacle Z position out of bounds: {obs.zPosition}");
                    }
                }
            }

            // Check coins
            if (section.coins != null)
            {
                foreach (var coin in section.coins)
                {
                    if (coin.count <= 0)
                    {
                        Debug.LogWarning($"[JSONLoader] Section {section.id}: Invalid coin count: {coin.count}");
                    }
                }
            }
        }

        if (isValid)
        {
            Debug.Log("[JSONLoader] ✓ All sections validated successfully");
        }

        return isValid;
    }

    #endregion

    #region Debug

    [ContextMenu("Load JSON")]
    public void LoadJSON()
    {
        LoadSections();
    }

    [ContextMenu("Validate JSON")]
    public void ValidateJSON()
    {
        ValidateSections();
    }

    [ContextMenu("Print All Sections")]
    public void PrintAllSections()
    {
        if (loadedLibrary == null)
        {
            Debug.Log("[JSONLoader] No data loaded");
            return;
        }

        Debug.Log("===== ALL SECTIONS =====");
        foreach (var section in loadedLibrary.sections)
        {
            Debug.Log($"ID: {section.id}, Name: {section.name}, Difficulty: {section.difficulty}, " +
                     $"Obstacles: {section.obstacles?.Count ?? 0}, Coins: {section.coins?.Count ?? 0}");
        }
    }

    #endregion
}