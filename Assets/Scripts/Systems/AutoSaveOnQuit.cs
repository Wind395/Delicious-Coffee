using UnityEngine;

/// <summary>
/// Auto Save On Quit - Ensures data is saved when app closes
/// Attach to SaveLoadManager or PlayerDataManager
/// </summary>
public class AutoSaveOnQuit : MonoBehaviour
{
    void OnApplicationQuit()
    {
        Debug.Log("[AutoSave] Application quitting - saving data...");
        
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.ForceSave();
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        // Save when app goes to background (mobile)
        if (pauseStatus)
        {
            Debug.Log("[AutoSave] Application paused - saving data...");
            
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.ForceSave();
            }
        }
    }
}