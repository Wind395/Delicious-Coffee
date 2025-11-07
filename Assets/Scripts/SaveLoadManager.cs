using UnityEngine;
using System.IO;

/// <summary>
/// Save/Load Manager - Handles JSON file operations
/// SOLID: Single Responsibility - File I/O only
/// Design Pattern: Repository
/// </summary>
public class SaveLoadManager : MonoBehaviour
{
    #region Singleton
    
    private static SaveLoadManager _instance;
    public static SaveLoadManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("SaveLoadManager");
                _instance = go.AddComponent<SaveLoadManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    
    #endregion

    #region Constants
    
    private const string SAVE_FOLDER = "SaveData";
    private const string SAVE_FILE_NAME = "playerdata.json";
    private const string BACKUP_FILE_NAME = "playerdata_backup.json";
    
    #endregion

    #region Properties
    
    private string SaveFolderPath => Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
    private string SaveFilePath => Path.Combine(SaveFolderPath, SAVE_FILE_NAME);
    private string BackupFilePath => Path.Combine(SaveFolderPath, BACKUP_FILE_NAME);
    
    #endregion

    #region Unity Lifecycle
    
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        Initialize();
    }
    
    #endregion

    #region Initialization
    
    /// <summary>
    /// Initialize save system
    /// </summary>
    private void Initialize()
    {
        // Create save folder if doesn't exist
        if (!Directory.Exists(SaveFolderPath))
        {
            Directory.CreateDirectory(SaveFolderPath);
            Debug.Log($"[SaveLoad] Created save folder: {SaveFolderPath}");
        }
        
        Debug.Log($"[SaveLoad] Save file path: {SaveFilePath}");
    }
    
    #endregion

    #region Save Methods
    
    /// <summary>
    /// Save player data to JSON file
    /// SOLID: Single Responsibility - Just save
    /// </summary>
    public bool SaveData(PlayerData data)
    {
        if (data == null)
        {
            Debug.LogError("[SaveLoad] Cannot save null data!");
            return false;
        }

        try
        {
            // Update timestamp
            data.UpdateLastPlayed();
            
            // Create backup of existing save (if exists)
            CreateBackup();
            
            // Serialize to JSON
            string json = JsonUtility.ToJson(data, true); // Pretty print
            
            // Write to file
            File.WriteAllText(SaveFilePath, json);
            
            Debug.Log($"[SaveLoad] ✓ Data saved successfully to: {SaveFilePath}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveLoad] ❌ Save failed: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Create backup of current save file
    /// </summary>
    private void CreateBackup()
    {
        if (File.Exists(SaveFilePath))
        {
            try
            {
                File.Copy(SaveFilePath, BackupFilePath, true);
                Debug.Log("[SaveLoad] Backup created");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SaveLoad] Backup failed: {e.Message}");
            }
        }
    }
    
    #endregion

    #region Load Methods
    
    /// <summary>
    /// Load player data from JSON file
    /// </summary>
    public PlayerData LoadData()
    {
        // Check if save file exists
        if (!File.Exists(SaveFilePath))
        {
            Debug.LogWarning("[SaveLoad] No save file found. Creating new data...");
            return CreateNewPlayerData();
        }

        try
        {
            // Read JSON from file
            string json = File.ReadAllText(SaveFilePath);
            
            // Deserialize
            PlayerData data = JsonUtility.FromJson<PlayerData>(json);
            
            // Validate
            if (data == null)
            {
                Debug.LogError("[SaveLoad] Failed to deserialize data!");
                return TryLoadBackup();
            }
            
            Debug.Log($"[SaveLoad] ✓ Data loaded successfully");
            Debug.Log($"[SaveLoad] Gold: {data.gold}, Characters: {data.purchasedCharacters.Count}, Toilets: {data.purchasedToilets.Count}");
            
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveLoad] ❌ Load failed: {e.Message}");
            return TryLoadBackup();
        }
    }

    /// <summary>
    /// Try to load from backup file
    /// </summary>
    private PlayerData TryLoadBackup()
    {
        Debug.LogWarning("[SaveLoad] Attempting to load backup...");
        
        if (!File.Exists(BackupFilePath))
        {
            Debug.LogWarning("[SaveLoad] No backup found. Creating new data...");
            return CreateNewPlayerData();
        }

        try
        {
            string json = File.ReadAllText(BackupFilePath);
            PlayerData data = JsonUtility.FromJson<PlayerData>(json);
            
            if (data != null)
            {
                Debug.Log("[SaveLoad] ✓ Loaded from backup successfully");
                
                // Restore backup as main save
                File.Copy(BackupFilePath, SaveFilePath, true);
                
                return data;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveLoad] Backup load failed: {e.Message}");
        }
        
        return CreateNewPlayerData();
    }

    /// <summary>
    /// Create new player data with defaults
    /// </summary>
    private PlayerData CreateNewPlayerData()
    {
        Debug.Log("[SaveLoad] Creating new player data...");
        
        PlayerData newData = new PlayerData();
        
        // Save immediately
        SaveData(newData);
        
        return newData;
    }
    
    #endregion

    #region Utility Methods
    
    /// <summary>
    /// Check if save file exists
    /// </summary>
    public bool SaveFileExists()
    {
        return File.Exists(SaveFilePath);
    }

    /// <summary>
    /// Delete all save data (for testing/reset)
    /// </summary>
    public void DeleteAllSaveData()
    {
        try
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
                Debug.Log("[SaveLoad] Save file deleted");
            }
            
            if (File.Exists(BackupFilePath))
            {
                File.Delete(BackupFilePath);
                Debug.Log("[SaveLoad] Backup file deleted");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveLoad] Delete failed: {e.Message}");
        }
    }

    /// <summary>
    /// Get save file info (for debug)
    /// </summary>
    public string GetSaveFileInfo()
    {
        if (!File.Exists(SaveFilePath))
        {
            return "No save file found";
        }

        FileInfo fileInfo = new FileInfo(SaveFilePath);
        return $"Size: {fileInfo.Length} bytes\n" +
               $"Created: {fileInfo.CreationTime}\n" +
               $"Modified: {fileInfo.LastWriteTime}";
    }
    
    #endregion

    #region Debug
    
    #if UNITY_EDITOR
    
    [ContextMenu("Open Save Folder")]
    private void OpenSaveFolder()
    {
        if (Directory.Exists(SaveFolderPath))
        {
            System.Diagnostics.Process.Start(SaveFolderPath);
        }
        else
        {
            Debug.LogWarning("[SaveLoad] Save folder doesn't exist yet");
        }
    }

    [ContextMenu("Print Save File Path")]
    private void PrintSavePath()
    {
        Debug.Log($"[SaveLoad] Save Path: {SaveFilePath}");
    }

    [ContextMenu("Delete All Saves")]
    private void DeleteSaves()
    {
        DeleteAllSaveData();
    }
    
    #endif
    
    #endregion
}