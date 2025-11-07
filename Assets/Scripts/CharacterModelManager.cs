using UnityEngine;

/// <summary>
/// Character Model Manager - Apply equipped character in gameplay
/// SOLID: Single Responsibility - Character model only
/// </summary>
public class CharacterModelManager : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Character Container")]
    [SerializeField] private Transform characterContainer; // Empty child of Player

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    
    [Header("Character Parent")]
    [SerializeField] private Transform characterParent;

    #endregion

    #region State

    private GameObject _currentCharacterModel;

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        SpawnEquippedCharacter();
    }

    void Start()
    {
        
    }
    
    #endregion

    #region Character Spawning
    
    /// <summary>
    /// Spawn equipped character - FIXED
    /// </summary>
    private void SpawnEquippedCharacter()
    {
        // Clear existing
        ClearCurrentCharacter();
        
        // ═══ STEP 1: Get equipped character ID ═══
        string equippedID = PlayerDataManager.Instance.EquippedCharacter;
        
        if (showDebug)
        {
            Debug.Log($"[CharacterModel] Equipped ID: {equippedID}");
        }
        
        // ═══ STEP 2: Get ShopItemData by ID ═══
        ShopItemData equippedCharacter = ShopManager.Instance?.GetItemByID(equippedID);
        
        if (equippedCharacter == null)
        {
            Debug.LogError($"[CharacterModel] ❌ No character found for ID: {equippedID}");
            return;
        }
        
        // ═══ STEP 3: Verify it's a character ═══
        if (equippedCharacter.itemType != ShopItemType.Character)
        {
            Debug.LogError($"[CharacterModel] ❌ Item {equippedID} is not a Character! Type: {equippedCharacter.itemType}");
            return;
        }
        
        if (equippedCharacter.prefab == null)
        {
            Debug.LogError($"[CharacterModel] ❌ Character {equippedCharacter.itemName} has no prefab!");
            return;
        }
        
        // ═══ STEP 4: Spawn prefab ═══
        Transform parent = characterParent != null ? characterParent : transform;
        
        _currentCharacterModel = Instantiate(equippedCharacter.prefab, parent);
        _currentCharacterModel.name = equippedCharacter.itemName + "_Model";
        
        // Reset transform
        _currentCharacterModel.transform.localPosition = Vector3.zero;
        _currentCharacterModel.transform.localRotation = Quaternion.identity;
        
        if (showDebug)
        {
            Debug.Log($"[CharacterModel] ✓ Spawned: {equippedCharacter.itemName} (ID: {equippedID})");
        }
    }

    private void ClearCurrentCharacter()
    {
        if (_currentCharacterModel != null)
        {
            Destroy(_currentCharacterModel);
            _currentCharacterModel = null;
        }
    }
    
    #endregion

    #region Public API
    
    /// <summary>
    /// Change character at runtime (if needed)
    /// </summary>
    // public void ChangeCharacter(ShopItemData newCharacter)
    // {
    //     if (newCharacter == null || newCharacter.itemType != ShopItemType.Character)
    //     {
    //         Debug.LogError("[CharacterModel] Invalid character data!");
    //         return;
    //     }
        
    //     ClearCurrentModel();
        
    //     _currentCharacterModel = Instantiate(newCharacter.prefab, characterContainer);
    //     _currentCharacterModel.name = newCharacter.itemName + "_Model";
        
    //     Debug.Log($"[CharacterModel] Changed to: {newCharacter.itemName}");
    // }
    
    #endregion
}