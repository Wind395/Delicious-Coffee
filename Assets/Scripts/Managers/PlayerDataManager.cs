using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Player Data Manager - REFACTORED: Uses JSON save/load
/// SOLID: Single Responsibility - Data management only
/// Design Pattern: Facade
/// </summary>
public class PlayerDataManager : MonoBehaviour
{
    #region Singleton
    
    private static PlayerDataManager _instance;
    public static PlayerDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("PlayerDataManager");
                _instance = go.AddComponent<PlayerDataManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    
    #endregion

    #region Player Data
    
    private PlayerData _playerData;
    
    #endregion

    #region Properties - MODIFIED
    
    public int Gold => _playerData?.gold ?? 0;
    public string EquippedCharacter => _playerData?.equippedCharacter ?? "char_default";
    public string EquippedHome => _playerData?.equippedHome ?? "home_default"; // CHANGED from EquippedToilet
    public int HighScore => _playerData?.highScore ?? 0;
    public int TotalGamesPlayed => _playerData?.totalGamesPlayed ?? 0;
    public int TotalWins => _playerData?.totalWins ?? 0;
    
    #endregion

    #region Events
    
    public event System.Action<int> OnGoldChanged;
    public event System.Action<string> OnItemPurchased;
    public event System.Action<string, ShopItemType> OnItemEquipped;
    public event System.Action OnDataLoaded;
    public event System.Action OnDataSaved;
    
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
        
        LoadData();
    }

    #endregion

    #region Save/Load - JSON

    /// <summary>
    /// Load data from JSON file
    /// </summary>
    private void LoadData()
    {
        _playerData = SaveLoadManager.Instance.LoadData();

        if (_playerData == null)
        {
            Debug.LogError("[PlayerData] Failed to load data!");
            _playerData = new PlayerData();
        }

        EnsureDefaultItems();

        // Trigger events
        OnGoldChanged?.Invoke(_playerData.gold);
        OnDataLoaded?.Invoke();

        Debug.Log($"[PlayerData] ✓ Loaded - Gold: {_playerData.gold}, Games: {_playerData.totalGamesPlayed}");
    }
    
    /// <summary>
    /// Ensure default items are in purchased list - MODIFIED
    /// </summary>
    private void EnsureDefaultItems()
    {
        bool changed = false;
        
        // Ensure default character
        if (!_playerData.purchasedCharacters.Contains("char_default"))
        {
            _playerData.purchasedCharacters.Add("char_default");
            changed = true;
        }
        
        // ═══ CHANGED: Ensure default home ═══
        if (!_playerData.purchasedHomes.Contains("home_default"))
        {
            _playerData.purchasedHomes.Add("home_default");
            changed = true;
            Debug.Log("[PlayerDataManager] Added default home");
        }
        
        // Ensure equipped IDs are valid
        if (string.IsNullOrEmpty(_playerData.equippedCharacter))
        {
            _playerData.equippedCharacter = "char_default";
            changed = true;
        }
        
        // ═══ CHANGED: Validate home ═══
        if (string.IsNullOrEmpty(_playerData.equippedHome))
        {
            _playerData.equippedHome = "home_default";
            changed = true;
        }
        
        if (changed)
        {
            SaveData();
        }
    }

    /// <summary>
    /// Save data to JSON file
    /// </summary>
    public void SaveData()
    {
        if (_playerData == null)
        {
            Debug.LogError("[PlayerData] No data to save!");
            return;
        }
        
        bool success = SaveLoadManager.Instance.SaveData(_playerData);
        
        if (success)
        {
            OnDataSaved?.Invoke();
            Debug.Log("[PlayerData] ✓ Data saved");
        }
        else
        {
            Debug.LogError("[PlayerData] ❌ Save failed!");
        }
    }
    
    #endregion

    #region Gold Management
    
    /// <summary>
    /// Add gold
    /// </summary>
    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        
        _playerData.gold += amount;
        _playerData.totalCoinsCollected += amount; // Track stats
        
        OnGoldChanged?.Invoke(_playerData.gold);
        
        // Auto-save
        SaveData();
        
        //Debug.Log($"[PlayerData] +{amount} gold. Total: {_data.gold}");
    }

    /// <summary>
    /// Spend gold
    /// </summary>
    public bool SpendGold(int amount)
    {
        if (amount <= 0 || _playerData.gold < amount)
        {
            Debug.LogWarning($"[PlayerData] Not enough gold! Need {amount}, have {_playerData.gold}");
            return false;
        }
        
        _playerData.gold -= amount;
        OnGoldChanged?.Invoke(_playerData.gold);
        
        // Auto-save
        SaveData();
        
        Debug.Log($"[PlayerData] -{amount} gold. Remaining: {_playerData.gold}");
        return true;
    }
    
    #endregion

    #region Shop Management
    
    /// <summary>
    /// Check if item is purchased - MODIFIED
    /// </summary>
    public bool IsPurchased(string itemID)
    {
        if (_playerData == null) return false;
        
        if (_playerData.purchasedCharacters.Contains(itemID))
            return true;
        
        // ═══ CHANGED: Check homes instead of toilets ═══
        if (_playerData.purchasedHomes.Contains(itemID))
            return true;
        
        return false;
    }

    /// <summary>
    /// Purchase item - UPDATED: Better debugging
    /// </summary>
    public bool PurchaseItem(ShopItemData item)
    {
        if (_playerData == null || item == null)
        {
            Debug.LogError("[PlayerDataManager] ❌ Cannot purchase - data or item is null!");
            return false;
        }
        
        Debug.Log($"[PlayerDataManager] ═══ PURCHASE ATTEMPT ═══");
        Debug.Log($"[PlayerDataManager] Item: {item.itemName} (ID: {item.itemID})");
        Debug.Log($"[PlayerDataManager] Type: {item.itemType}");
        Debug.Log($"[PlayerDataManager] Price: {item.price}");
        Debug.Log($"[PlayerDataManager] Your Gold: {_playerData.gold}");
        
        // Check already purchased
        if (IsPurchased(item.itemID))
        {
            Debug.LogWarning($"[PlayerDataManager] ⚠️ Item already purchased: {item.itemID}");
            return false;
        }
        
        // Check gold
        if (!SpendGold(item.price))
        {
            Debug.LogWarning($"[PlayerDataManager] ❌ Not enough gold! Need {item.price}, have {_playerData.gold}");
            return false;
        }
        
        // Add to purchased list
        if (item.itemType == ShopItemType.Character)
        {
            _playerData.purchasedCharacters.Add(item.itemID);
            Debug.Log($"[PlayerDataManager] ✓ Added to purchasedCharacters");
        }
        else if (item.itemType == ShopItemType.Home)
        {
            _playerData.purchasedHomes.Add(item.itemID);
            Debug.Log($"[PlayerDataManager] ✓ Added to purchasedHomes");
        }
        else
        {
            Debug.LogError($"[PlayerDataManager] ❌ Unknown item type: {item.itemType}");
            // Refund gold
            AddGold(item.price);
            return false;
        }
        
        SaveData();
        OnItemPurchased?.Invoke(item.itemID);
        
        Debug.Log($"[PlayerDataManager] ═══ PURCHASE SUCCESS ═══");
        Debug.Log($"[PlayerDataManager] Item: {item.itemName} purchased!");
        Debug.Log($"[PlayerDataManager] Remaining Gold: {_playerData.gold}");
        
        return true;
    }

    /// <summary>
    /// Equip item - UPDATED: Better debugging
    /// </summary>
    public void EquipItem(ShopItemData item)
    {
        if (_playerData == null || item == null)
        {
            Debug.LogError("[PlayerDataManager] ❌ Cannot equip - data or item is null!");
            return;
        }
        
        Debug.Log($"[PlayerDataManager] ═══ EQUIP ATTEMPT ═══");
        Debug.Log($"[PlayerDataManager] Item: {item.itemName} (ID: {item.itemID})");
        Debug.Log($"[PlayerDataManager] Type: {item.itemType}");
        
        // Check purchased
        if (!IsPurchased(item.itemID))
        {
            Debug.LogWarning($"[PlayerDataManager] ❌ Cannot equip unpurchased item: {item.itemID}");
            return;
        }
        
        // Equip based on type
        if (item.itemType == ShopItemType.Character)
        {
            _playerData.equippedCharacter = item.itemID;
            Debug.Log($"[PlayerDataManager] ✓ Equipped character: {item.itemID}");
        }
        else if (item.itemType == ShopItemType.Home)
        {
            _playerData.equippedHome = item.itemID;
            Debug.Log($"[PlayerDataManager] ✓ Equipped home: {item.itemID}");
        }
        else
        {
            Debug.LogError($"[PlayerDataManager] ❌ Unknown item type: {item.itemType}");
            return;
        }
        
        SaveData();
        OnItemEquipped?.Invoke(item.itemID, item.itemType);
        
        Debug.Log($"[PlayerDataManager] ═══ EQUIP SUCCESS ═══");
    }

    /// <summary>
    /// Check if item is equipped - MODIFIED
    /// </summary>
    public bool IsEquipped(string itemID)
    {
        if (_playerData == null) return false;
        
        if (_playerData.equippedCharacter == itemID)
        {
            return true;
        }
        
        // ═══ CHANGED: Check home instead of toilet ═══
        if (_playerData.equippedHome == itemID)
        {
            return true;
        }
        
        return false;
    }
    
    #endregion

    #region Level Progress - NEW

    /// <summary>
    /// Check if level is completed
    /// </summary>
    public bool IsLevelCompleted(string levelID)
    {
        return _playerData?.IsLevelCompleted(levelID) ?? false;
    }

    /// <summary>
    /// Check if level is unlocked
    /// </summary>
    public bool IsLevelUnlocked(string levelID)
    {
        return _playerData?.IsLevelUnlocked(levelID) ?? false;
    }

    /// <summary>
    /// Check if map is completed
    /// </summary>
    public bool IsMapCompleted(string mapID)
    {
        return _playerData?.IsMapCompleted(mapID) ?? false;
    }

    /// <summary>
    /// Mark level as completed
    /// </summary>
    public void MarkLevelCompleted(string levelID)
    {
        if (_playerData == null)
        {
            Debug.LogError("[PlayerDataManager] PlayerData is null!");
            return;
        }
        
        _playerData.MarkLevelCompleted(levelID);
        SaveData();
        
        Debug.Log($"[PlayerDataManager] ✓ Level completed: {levelID}");
    }

    /// <summary>
    /// Unlock level
    /// </summary>
    public void UnlockLevel(string levelID)
    {
        if (_playerData == null)
        {
            Debug.LogError("[PlayerDataManager] PlayerData is null!");
            return;
        }
        
        _playerData.UnlockLevel(levelID);
        SaveData();
        
        Debug.Log($"[PlayerDataManager] 🔓 Level unlocked: {levelID}");
    }

    /// <summary>
    /// Mark map as completed
    /// </summary>
    public void MarkMapCompleted(string mapID)
    {
        if (_playerData == null)
        {
            Debug.LogError("[PlayerDataManager] PlayerData is null!");
            return;
        }
        
        _playerData.MarkMapCompleted(mapID);
        SaveData();
        
        Debug.Log($"[PlayerDataManager] ✓ Map completed: {mapID}");
    }

    /// <summary>
    /// Update level record
    /// </summary>
    public void UpdateLevelRecord(string levelID, float time, int score)
    {
        if (_playerData == null) return;
        
        _playerData.UpdateLevelRecord(levelID, time, score);
        SaveData();
        
        Debug.Log($"[PlayerDataManager] ✓ Level record updated: {levelID} - Time: {time:F1}s, Score: {score}");
    }

    /// <summary>
    /// Get level record
    /// </summary>
    public LevelRecord GetLevelRecord(string levelID)
    {
        return _playerData?.GetLevelRecord(levelID);
    }

    #endregion

    #region Endless Mode - NEW

    /// <summary>
    /// Update endless mode record
    /// </summary>
    public void UpdateEndlessRecord(float distance)
    {
        if (_playerData == null) return;
        
        float oldBest = _playerData.bestEndlessDistance;
        
        _playerData.UpdateEndlessRecord(distance);
        SaveData();
        
        if (distance > oldBest)
        {
            Debug.Log($"[PlayerDataManager] 🏆 NEW ENDLESS RECORD: {distance:F0}m (Old: {oldBest:F0}m)");
        }
        else
        {
            Debug.Log($"[PlayerDataManager] Endless run: {distance:F0}m (Best: {_playerData.bestEndlessDistance:F0}m)");
        }
    }

    /// <summary>
    /// Get best endless distance
    /// </summary>
    public float GetBestEndlessDistance()
    {
        return _playerData?.bestEndlessDistance ?? 0f;
    }

    #endregion

    #region Statistics
    
    /// <summary>
    /// Record game played
    /// </summary>
    public void RecordGamePlayed()
    {
        _playerData.totalGamesPlayed++;
        Debug.Log($"[PlayerData] Games played: {_playerData.totalGamesPlayed}");
    }

    /// <summary>
    /// Record victory
    /// </summary>
    public void RecordVictory(int score, float distance)
    {
        _playerData.totalWins++;
        _playerData.totalDistanceTraveled += distance;
        
        // Update high score
        if (score > _playerData.highScore)
        {
            _playerData.highScore = score;
            Debug.Log($"[PlayerData] 🏆 New High Score: {score}!");
        }
        
        // Auto-save
        SaveData();
        
        Debug.Log($"[PlayerData] Victory recorded! Total wins: {_playerData.totalWins}");
    }

    /// <summary>
    /// Record loss
    /// </summary>
    public void RecordLoss(float distance)
    {
        _playerData.totalLosses++;
        _playerData.totalDistanceTraveled += distance;
        
        // Auto-save
        SaveData();
        
        Debug.Log($"[PlayerData] Loss recorded. Total losses: {_playerData.totalLosses}");
    }

    /// <summary>
    /// Record power-up collected
    /// </summary>
    public void RecordPowerUpCollected()
    {
        _playerData.totalPowerUpsCollected++;
    }
    
    #endregion

    #region Public API
    
    /// <summary>
    /// Get full player data (for display)
    /// </summary>
    public PlayerData GetPlayerData()
    {
        return _playerData;
    }

    /// <summary>
    /// Force save (manual trigger)
    /// </summary>
    public void ForceSave()
    {
        SaveData();
    }

    /// <summary>
    /// Reset all data (for testing)
    /// </summary>
    [ContextMenu("Reset All Data")]
    public void ResetAllData()
    {
        _playerData = new PlayerData();
        SaveData();
        
        OnGoldChanged?.Invoke(_playerData.gold);
        
        Debug.Log("[PlayerData] ✓ All data reset!");
    }
    
    #endregion

    #region Debug
    
    #if UNITY_EDITOR
    
    [ContextMenu("Add 1000 Gold (Test)")]
    public void AddTestGold()
    {
        AddGold(1000);
    }

    // [ContextMenu("Print All Data")]
    // public void PrintData()
    // {
    //     if (_playerData == null)
    //     {
    //         Debug.Log("[PlayerData] No data loaded");
    //         return;
    //     }
        
    //     Debug.Log("=== PLAYER DATA ===");
    //     Debug.Log($"Gold: {_playerData.gold}");
    //     Debug.Log($"High Score: {_playerData.highScore}");
    //     Debug.Log($"Games Played: {_playerData.totalGamesPlayed}");
    //     Debug.Log($"Wins: {_playerData.totalWins} | Losses: {_playerData.totalLosses}");
    //     Debug.Log($"Equipped Character: {_playerData.equippedCharacter}");
    //     Debug.Log($"Equipped Toilet: {_playerData.equippedToilet}");
    //     Debug.Log($"Purchased Characters: {string.Join(", ", _playerData.purchasedCharacters)}");
    //     Debug.Log($"Purchased Toilets: {string.Join(", ", _playerData.purchasedToilets)}");
    //     Debug.Log($"Created: {_playerData.createdDate}");
    //     Debug.Log($"Last Played: {_playerData.lastPlayedDate}");
    // }
    
    #endif
    
    #endregion
}