using System.Collections;
using UnityEngine;

/// <summary>
/// Victory Sequence Controller - UPDATED: Exact sitting position + rotation offset
/// </summary>
public class VictorySequenceController : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("References")]
    [SerializeField] private ToiletModelManager toiletManager;
    [SerializeField] private CameraFollowController cameraController;
    
    [Header("Teleport Settings")]
    [SerializeField] private bool teleportToToilet = true;
    [SerializeField] private bool useToiletSettings = true;
    
    [Header("Audio")]
    [SerializeField] private AudioClip victorySound;
    
    [Header("Effects")]
    [SerializeField] private GameObject confettiEffect;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    

    [Header("Fart System")]
    [SerializeField] private bool enableToiletFart = true;
    [SerializeField] private float fartDelayAfterSitting = 0.5f;
    
    #endregion

    #region State
    
    private bool _isPlayingSequence = false;
    private PlayerController _player;
    private PlayerAnimationController _playerAnimation;
    private Vector3 _toiletPosition;
    private Transform _toiletTransform;
    private ToiletSettings _toiletSettings;
    
    #endregion

    #region Unity Lifecycle
    
    void Start()
    {
        Initialize();
    }
    
    #endregion

    #region Initialization
    
    private void Initialize()
    {
        if (toiletManager == null)
        {
            toiletManager = FindObjectOfType<ToiletModelManager>();
        }
        
        if (cameraController == null)
        {
            cameraController = FindObjectOfType<CameraFollowController>();
        }

        if (debugMode)
        {
            Debug.Log("[VictorySequence] ✓ Initialized");
        }
    }
    
    #endregion

    #region Victory Sequence - UPDATED
    
    public void TriggerVictory(PlayerController player)
    {
        if (_isPlayingSequence)
        {
            Debug.LogWarning("[VictorySequence] Already playing sequence!");
            return;
        }
        
        if (player == null)
        {
            Debug.LogError("[VictorySequence] ❌ Player is null!");
            return;
        }
        
        _player = player;
        _playerAnimation = player.GetComponent<PlayerAnimationController>();
        
        if (_playerAnimation == null)
        {
            Debug.LogError("[VictorySequence] ❌ PlayerAnimationController not found!");
        }
        
        // Get toilet references
        if (toiletManager != null)
        {
            _toiletTransform = toiletManager.GetToiletTransform();
            _toiletPosition = toiletManager.GetToiletPosition();
            
            if (_toiletTransform != null)
            {
                _toiletSettings = _toiletTransform.GetComponent<ToiletSettings>();
                
                if (_toiletSettings != null)
                {
                    if (debugMode)
                    {
                        Debug.Log("[VictorySequence] ✓ ToiletSettings found");
                        _toiletSettings.DebugPrintSittingInfo();
                    }
                }
                else
                {
                    Debug.LogWarning("[VictorySequence] ⚠ ToiletSettings not found on toilet!");
                }
            }
        }
        else
        {
            _toiletPosition = transform.position;
            Debug.LogWarning("[VictorySequence] Using fallback toilet position");
        }
        
        Debug.Log($"[VictorySequence] ▶️ Starting victory sequence");
        
        StartCoroutine(PlayVictorySequence());
    }

    private IEnumerator PlayVictorySequence()
    {
        _isPlayingSequence = true;

        // Deactivate PowerUps

        // ═══ DEACTIVATE ALL POWERUPS ═══
        // SOLID: Single Responsibility - Delegate to PowerUpManager
        // Pattern: Observer - Trigger event for other systems to react
        DeactivateAllPowerUps();


        // Step 1: Stop player
        Debug.Log("[VictorySequence] Step 1: Stop player");
        StopPlayer();
        yield return new WaitForSeconds(0.1f);

        // Step 2: Position player
        Debug.Log("[VictorySequence] Step 2: Move player to sitting position");
        TeleportPlayerToSittingPosition();
        yield return new WaitForSeconds(0.1f);

        // Step 3: Play sitting animation
        Debug.Log("[VictorySequence] Step 3: Play sitting animation");
        yield return StartCoroutine(PlaySittingAnimation());

        // ═══ NEW: TOILET FART ═══
        if (enableToiletFart)
        {
            yield return new WaitForSeconds(fartDelayAfterSitting);
            PlayToiletFart();
        }

        // Step 4: Play effects
        Debug.Log("[VictorySequence] Step 4: Play effects");
        yield return StartCoroutine(PlayVictoryEffects());

        // Step 5: Show Victory UI
        Debug.Log("[VictorySequence] Step 5: Show Victory UI");
        ShowVictoryUI();

        _isPlayingSequence = false;
        Debug.Log("[VictorySequence] ✅ Sequence complete!");
    }
    
    /// <summary>
    /// Deactivate all active power-ups when reaching toilet
    /// SOLID: Interface Segregation - Use specific method
    /// </summary>
    private void DeactivateAllPowerUps()
    {
        Debug.Log("[VictorySequence] 🔌 Deactivating all power-ups...");
        
        // Method 1: Direct call (Simple)
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.ClearAllPowerUps();
            Debug.Log("[VictorySequence] ✓ All power-ups deactivated");
        }
        else
        {
            Debug.LogWarning("[VictorySequence] ⚠ PowerUpManager not found!");
        }
        
        // Method 2: Event System (Decoupled)
        EventManager.Instance?.TriggerEvent("OnReachedToilet");
    }


    /// <summary>
    /// Play toilet fart sound - NEW
    /// </summary>
    private void PlayToiletFart()
    {
        Debug.Log("[VictorySequence] 💨 TOILET FART!");
        
        // Play sound
        AudioManager.Instance?.PlayFartToilet();
        
        // Optional: Play VFX if you want
        // FartVFXController.Instance?.PlayFartVFX();
        
        // Alternative: Call via player animation controller
        if (_playerAnimation != null)
        {
            _playerAnimation.PlayToiletFart();
        }
    }

    private void StopPlayer()
    {
        if (_player != null)
        {
            _player.StopForVictory();
            Debug.Log("[VictorySequence] ✓ Player stopped");
        }
    }

    /// <summary>
    /// Teleport player to EXACT sitting position + rotation - UPDATED
    /// </summary>
    private void TeleportPlayerToSittingPosition()
    {
        if (_player == null)
        {
            Debug.LogError("[VictorySequence] Player is null!");
            return;
        }

        if (!teleportToToilet)
        {
            Debug.Log("[VictorySequence] Skipping teleport (disabled)");
            return;
        }

        Vector3 targetPosition;
        Quaternion targetRotation;

        // ═══ USE TOILET SETTINGS ═══
        if (useToiletSettings && _toiletSettings != null)
        {
            // Get EXACT position and rotation from ToiletSettings
            (targetPosition, targetRotation) = _toiletSettings.GetPlayerSittingTransform();
            
            if (debugMode)
            {
                Debug.Log("═══════════════════════════════════");
                Debug.Log("[VictorySequence] Using ToiletSettings");
                Debug.Log($"  Target Position: {targetPosition}");
                Debug.Log($"  Target Rotation: {targetRotation.eulerAngles}");
                
                if (_toiletSettings.SittingPoint != null)
                {
                    Debug.Log($"  SittingPoint Pos: {_toiletSettings.SittingPoint.position}");
                    Debug.Log($"  SittingPoint Rot: {_toiletSettings.SittingPoint.rotation.eulerAngles}");
                    Debug.Log($"  Player Rot Offset: {_toiletSettings.PlayerRotationOffset}");
                }
                
                Debug.Log("═══════════════════════════════════");
            }
        }
        else
        {
            // Fallback: Simple positioning
            targetPosition = _toiletPosition - new Vector3(0, 0, 1.5f);
            targetPosition.y = _player.transform.position.y;
            
            Vector3 direction = (_toiletPosition - targetPosition).normalized;
            direction.y = 0;
            targetRotation = direction != Vector3.zero ? 
                Quaternion.LookRotation(direction) : 
                Quaternion.identity;
            
            Debug.LogWarning("[VictorySequence] Using fallback positioning (no ToiletSettings)");
        }

        // ═══ TELEPORT PLAYER ═══
        CharacterController controller = _player.GetComponent<CharacterController>();
        
        if (controller != null)
        {
            // Disable CharacterController để set position
            controller.enabled = false;
            
            // Set position
            _player.transform.position = targetPosition;
            
            // Set rotation
            _player.transform.rotation = targetRotation;
            
            // Re-enable
            controller.enabled = true;
            
            if (debugMode)
            {
                Debug.Log($"[VictorySequence] ✓ Player teleported (CharacterController disabled/enabled)");
            }
        }
        else
        {
            // No CharacterController, direct set
            _player.transform.position = targetPosition;
            _player.transform.rotation = targetRotation;
            
            if (debugMode)
            {
                Debug.Log($"[VictorySequence] ✓ Player teleported (direct transform)");
            }
        }

        if (debugMode)
        {
            Debug.Log($"[VictorySequence] ✓ Player positioned at: {_player.transform.position}");
            Debug.Log($"[VictorySequence] ✓ Player rotated to: {_player.transform.rotation.eulerAngles}");
        }
    }

    private IEnumerator PlaySittingAnimation()
    {
        if (_playerAnimation == null)
        {
            Debug.LogWarning("[VictorySequence] No PlayerAnimationController - skipping sitting animation");
            yield return new WaitForSeconds(2f);
            yield break;
        }
        
        Debug.Log("[VictorySequence] 🚽 Playing sitting animation...");
        
        bool sittingComplete = false;
        System.Action onComplete = () => { sittingComplete = true; };
        _playerAnimation.OnSittingComplete += onComplete;
        
        _playerAnimation.OnUseToilet();
        
        float timeout = _playerAnimation.SittingDuration + 1f;
        float elapsed = 0f;
        
        while (!sittingComplete && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        _playerAnimation.OnSittingComplete -= onComplete;
        
        if (sittingComplete)
        {
            Debug.Log("[VictorySequence] ✓ Sitting animation complete");
        }
        else
        {
            Debug.LogWarning("[VictorySequence] ⚠ Sitting animation timeout");
        }
    }

    private IEnumerator PlayVictoryEffects()
    {
        Debug.Log("[VictorySequence] 🎉 Playing victory effects");
        
        if (victorySound != null)
        {
            AudioManager.Instance?.PlaySFX(victorySound);
        }
        else
        {
            AudioManager.Instance?.PlayVictorySound();
        }
        
        if (confettiEffect != null)
        {
            GameObject confetti = Instantiate(confettiEffect, _toiletPosition + Vector3.up * 2f, Quaternion.identity);
            Destroy(confetti, 5f);
        }
        
        if (cameraController != null)
        {
            cameraController.Shake(0.3f, 0.2f);
        }
        
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("[VictorySequence] ✓ Effects complete");
    }

    private void ShowVictoryUI()
    {
        Debug.Log("[VictorySequence] 🏆 Calling GameManager.Victory()");

        EventManager.Instance?.TriggerEvent("OnToiletReached");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.Victory();
            Debug.Log("[VictorySequence] ✓ Victory UI shown");
        }
        else
        {
            Debug.LogError("[VictorySequence] ❌ GameManager not found!");
        }
    }
    
    #endregion
    
    #region Debug Methods
    
    [ContextMenu("Debug: Test Teleport (Without Victory)")]
    void DebugTestTeleport()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        
        if (player != null)
        {
            _player = player;
            _toiletTransform = toiletManager?.GetToiletTransform();
            _toiletSettings = _toiletTransform?.GetComponent<ToiletSettings>();
            
            Debug.Log("[VictorySequence] Testing teleport...");
            TeleportPlayerToSittingPosition();
        }
        else
        {
            Debug.LogError("[VictorySequence] No player found in scene!");
        }
    }
    
    #endregion
}