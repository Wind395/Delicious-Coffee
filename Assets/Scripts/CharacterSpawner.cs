using UnityEngine;

/// <summary>
/// Character Spawner - Spawns complete character prefab at game start
/// </summary>
public class CharacterSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private Vector3 spawnPosition = new Vector3(0, 0, 0);
    [SerializeField] private Quaternion spawnRotation = Quaternion.identity;
    
    [Header("References")]
    [SerializeField] private Transform spawnParent;
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    
    private GameObject _currentCharacter;
    private PlayerController _currentPlayerController;
    
    #region Properties
    
    public PlayerController CurrentPlayer => _currentPlayerController;
    public GameObject CurrentCharacter => _currentCharacter;
    
    #endregion
    
    #region Unity Lifecycle
    
    void Awake()
    {
        // Spawn equipped character immediately
        SpawnEquippedCharacter();
    }
    
    #endregion
    
    #region Character Spawning
    
    /// <summary>
    /// Spawn equipped character from shop
    /// </summary>
    public void SpawnEquippedCharacter()
    {
        if (showDebug)
        {
            Debug.Log("[CharacterSpawner] === SPAWNING CHARACTER ===");
        }
        
        // Clear existing
        ClearCurrentCharacter();
        
        // Get equipped character ID
        string equippedID = PlayerDataManager.Instance.EquippedCharacter;
        
        if (showDebug)
        {
            Debug.Log($"[CharacterSpawner] Equipped ID: {equippedID}");
        }
        
        // Get shop item data
        ShopItemData characterData = ShopManager.Instance?.GetItemByID(equippedID);
        
        if (characterData == null)
        {
            Debug.LogError($"[CharacterSpawner] ❌ No character found for ID: {equippedID}");
            return;
        }
        
        // Validate type
        if (characterData.itemType != ShopItemType.Character)
        {
            Debug.LogError($"[CharacterSpawner] ❌ Item is not a Character! Type: {characterData.itemType}");
            return;
        }
        
        // Validate prefab
        if (characterData.prefab == null)
        {
            Debug.LogError($"[CharacterSpawner] ❌ Character has no prefab assigned!");
            return;
        }
        
        // Spawn character
        Transform parent = spawnParent != null ? spawnParent : null;
        
        _currentCharacter = Instantiate(characterData.prefab, spawnPosition, spawnRotation, parent);
        _currentCharacter.name = characterData.itemName;
        
        // Ensure tag
        if (!_currentCharacter.CompareTag("Player"))
        {
            _currentCharacter.tag = "Player";
            
            if (showDebug)
            {
                Debug.Log("[CharacterSpawner] ✓ Set Player tag");
            }
        }
        
        // Get PlayerController
        _currentPlayerController = _currentCharacter.GetComponent<PlayerController>();
        
        if (_currentPlayerController == null)
        {
            Debug.LogError("[CharacterSpawner] ❌ Spawned character has no PlayerController!");
            Destroy(_currentCharacter);
            _currentCharacter = null;
            return;
        }
        
        // Validate components
        ValidateCharacterComponents();
        
        if (showDebug)
        {
            Debug.Log($"[CharacterSpawner] ✓ Spawned: {characterData.itemName}");
            Debug.Log($"[CharacterSpawner] ✓ Position: {_currentCharacter.transform.position}");
            Debug.Log($"[CharacterSpawner] ✓ PlayerController: Found");
        }
        
        // Notify GameManager
        NotifyGameManager();
    }
    
    /// <summary>
    /// Validate character has required components
    /// </summary>
    private void ValidateCharacterComponents()
    {
        if (_currentCharacter == null) return;
        
        bool hasCharacterController = _currentCharacter.GetComponent<CharacterController>() != null;
        bool hasPlayerController = _currentCharacter.GetComponent<PlayerController>() != null;
        bool hasAnimationController = _currentCharacter.GetComponent<PlayerAnimationController>() != null;
        bool hasAnimator = _currentCharacter.GetComponentInChildren<Animator>() != null;
        
        if (showDebug)
        {
            Debug.Log("[CharacterSpawner] === COMPONENT VALIDATION ===");
            Debug.Log($"CharacterController: {(hasCharacterController ? "✓" : "❌")}");
            Debug.Log($"PlayerController: {(hasPlayerController ? "✓" : "❌")}");
            Debug.Log($"PlayerAnimationController: {(hasAnimationController ? "✓" : "❌")}");
            Debug.Log($"Animator: {(hasAnimator ? "✓" : "❌")}");
        }
        
        if (!hasCharacterController)
        {
            Debug.LogError("[CharacterSpawner] ❌ Character missing CharacterController!");
        }
        
        if (!hasAnimator)
        {
            Debug.LogWarning("[CharacterSpawner] ⚠️ Character missing Animator!");
        }
    }
    
    /// <summary>
    /// Clear current character
    /// </summary>
    private void ClearCurrentCharacter()
    {
        if (_currentCharacter != null)
        {
            Destroy(_currentCharacter);
            _currentCharacter = null;
            _currentPlayerController = null;
        }
    }
    
    /// <summary>
    /// Notify GameManager that character is ready
    /// </summary>
    private void NotifyGameManager()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCharacterSpawned(_currentPlayerController);
        }
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Respawn character (for game restart)
    /// </summary>
    public void RespawnCharacter()
    {
        SpawnEquippedCharacter();
    }
    
    /// <summary>
    /// Get current player controller
    /// </summary>
    public PlayerController GetPlayerController()
    {
        return _currentPlayerController;
    }
    
    /// <summary>
    /// Check if character is ready
    /// </summary>
    public bool IsReady()
    {
        return _currentCharacter != null && _currentPlayerController != null;
    }
    
    #endregion
}