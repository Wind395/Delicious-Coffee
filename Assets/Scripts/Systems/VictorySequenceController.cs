using System.Collections;
using UnityEngine;

/// <summary>
/// Victory Sequence Controller - UPDATED: Integrated Door Animation
/// </summary>
public class VictorySequenceController : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("References")]
    [SerializeField] private HomeModelManager homeManager;
    [SerializeField] private CameraFollowController cameraController;
    
    [Header("Victory Timeline")]
    [Tooltip("Duration of each victory step")]
    [SerializeField] private float walkToHomeDelay = 0.5f;
    [SerializeField] private float waitForDoorOpenDelay = 0.3f; // NEW: Wait after door opens
    [SerializeField] private float enterHomeDuration = 2f;
    [SerializeField] private float beforeUIDelay = 0.5f; // NEW: Reduced, door close handles delay
    
    [Header("Player Fade")]
    [SerializeField] private bool enablePlayerFade = true;
    [SerializeField] private float fadeDuration = 1f;

    [Header("Camera")]
    [SerializeField] private Vector3 beforeVictoryCameraOffset = new Vector3(0, 3f, -8f);
    [SerializeField] private Vector3 victoryCameraOffset = new Vector3(0, 3f, -8f);
    
    [Header("Audio")]
    [SerializeField] private AudioClip victorySound;

    [Header("Effects")]
    [SerializeField] private GameObject confettiEffect;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    
    #endregion

    #region State
    
    private bool _isPlayingSequence = false;
    private PlayerController _player;
    private PlayerAnimationController _playerAnimation;
    private CharacterController _characterController;
    private Vector3 _homePosition;
    private Transform _homeTransform;
    private HomeSettings _homeSettings;
    private DoorController _doorController; // NEW
    
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
        if (homeManager == null)
        {
            homeManager = FindObjectOfType<HomeModelManager>();
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

    #region Victory Sequence - UPDATED WITH DOOR
    
    public void TriggerVictory(PlayerController player)
    {
        // Safety check - No victory in Endless mode
        if (GameModeManager.Instance != null && 
            GameModeManager.Instance.CurrentMode == GameMode.Endless)
        {
            Debug.LogWarning("[VictorySequence] ⚠️ Triggered in Endless mode - ignored!");
            return;
        }

        if (_isPlayingSequence)
        {
            return;
        }
        
        if (player == null)
        {
            Debug.LogError("[VictorySequence] ❌ Player is null!");
            return;
        }
        
        _player = player;
        _playerAnimation = player.GetComponent<PlayerAnimationController>();
        _characterController = player.GetComponent<CharacterController>();
        
        // Get home references
        if (homeManager != null)
        {
            _homeTransform = homeManager.GetHomeTransform();
            _homePosition = homeManager.GetHomePosition();
            
            if (_homeTransform != null)
            {
                _homeSettings = _homeTransform.GetComponent<HomeSettings>();
                
                // NEW: Get door controller
                if (_homeSettings != null)
                {
                    _doorController = _homeSettings.Door;
                    
                    if (_doorController != null)
                    {
                        // Subscribe to door events
                        _doorController.OnDoorOpenComplete += OnDoorOpened;
                        _doorController.OnDoorCloseComplete += OnDoorClosed;
                    }
                    else
                    {
                        Debug.LogWarning("[VictorySequence] ⚠️ No DoorController found!");
                    }
                }
            }
        }
        else
        {
            _homePosition = transform.position;
        }
        
        if (debugMode)
        {
            Debug.Log($"[VictorySequence] ▶️ Starting victory sequence");
        }
        
        StartCoroutine(PlayCompleteVictorySequence());
    }

    /// <summary>
    /// Complete victory sequence - UPDATED: With door animations
    /// </summary>
    private IEnumerator PlayCompleteVictorySequence()
    {
        _isPlayingSequence = true;

        Debug.Log("[VictorySequence] ═══════════════════════════════");
        Debug.Log("[VictorySequence] 🎉 VICTORY SEQUENCE START");
        Debug.Log("[VictorySequence] ═══════════════════════════════");

        // ═══ STEP 1: Stop systems ═══
        yield return StartCoroutine(StopGameSystems());

        // ═══ STEP 2: Walk to victory point + OPEN DOOR ═══
        yield return StartCoroutine(WalkPlayerToVictoryPointAndOpenDoor());

        // ═══ STEP 3: Walk into home ═══
        yield return StartCoroutine(WalkPlayerIntoHome());

        // ═══ STEP 4: CLOSE DOOR (player inside) ═══
        yield return StartCoroutine(CloseDoorAfterPlayerEnters());

        // ═══ STEP 5: Hide player ═══
        yield return StartCoroutine(HidePlayer());

        // ═══ STEP 6: Wait for door close animation to complete ═══
        yield return StartCoroutine(WaitForDoorCloseComplete());

        // ═══ STEP 7: Play effects ═══
        yield return StartCoroutine(PlayVictoryEffects());

        // ═══ STEP 8: Show Victory UI (only after door closed) ═══
        ShowVictoryUI();

        _isPlayingSequence = false;
        
        Debug.Log("[VictorySequence] ═══════════════════════════════");
        Debug.Log("[VictorySequence] ✅ VICTORY SEQUENCE COMPLETE");
        Debug.Log("[VictorySequence] ═══════════════════════════════");
    }

    #endregion

    #region Step 1: Stop Systems
    
    private IEnumerator StopGameSystems()
    {
        Debug.Log("[VictorySequence] Step 1: Stop systems");
        
        if (DogChaseController.Instance != null)
        {
            DogChaseController.Instance.StopChaseOnDeath();
            Debug.Log("[VictorySequence] 🐕 Dog chase stopped");
        }
        
        if (_player != null)
        {
            _player.StopForVictory();
            Debug.Log("[VictorySequence] ✓ Player stopped");
        }
        
        yield return new WaitForSeconds(0.2f);
    }
    
    #endregion

    #region Step 2: Walk to Victory Point + Open Door - NEW

    /// <summary>
    /// Walk to victory position AND trigger door open
    /// </summary>
    private IEnumerator WalkPlayerToVictoryPointAndOpenDoor()
    {
        Debug.Log("[VictorySequence] Step 2: Walk to victory + OPEN DOOR");
        
        if (_player == null)
        {
            Debug.LogError("[VictorySequence] Player is null!");
            yield break;
        }

        // Get target position
        Vector3 targetPosition;
        Quaternion targetRotation;

        if (_homeSettings != null)
        {
            (targetPosition, targetRotation) = _homeSettings.GetPlayerVictoryTransform();
        }
        else
        {
            targetPosition = _homePosition - new Vector3(0, 0, 3f);
            targetPosition.y = _player.transform.position.y;

            Vector3 direction = (_homePosition - targetPosition).normalized;
            direction.y = 0;
            targetRotation = direction != Vector3.zero ?
                Quaternion.LookRotation(direction) :
                Quaternion.identity;
        }

        // Rotate player
        Vector3 moveDirection = (targetPosition - _player.transform.position).normalized;
        moveDirection.y = 0;
        
        if (moveDirection != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
            _player.transform.rotation = Quaternion.Slerp(
                _player.transform.rotation,
                lookRotation,
                1f
            );
        }

        // Move player
        Vector3 startPos = _player.transform.position;
        float distance = Vector3.Distance(startPos, targetPosition);
        float moveSpeed = _player.CurrentSpeed > 0 ? _player.CurrentSpeed : 8f;
        float moveDuration = distance / moveSpeed;
        
        float elapsed = 0f;
        bool doorOpened = false;
        
        while (elapsed < moveDuration)
        {
            if (_player == null) yield break;
            
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            
            // ═══ NEW: Open door when player is halfway ═══
            if (!doorOpened && t >= 0.5f)
            {
                OpenDoor();
                doorOpened = true;
            }
            
            // Move player
            Vector3 newPos = Vector3.Lerp(startPos, targetPosition, t);
            
            if (_characterController != null)
            {
                Vector3 movement = newPos - _player.transform.position;
                _characterController.Move(movement);
            }
            else
            {
                _player.transform.position = newPos;
            }
            
            // Keep facing target
            Vector3 currentDirection = (targetPosition - _player.transform.position).normalized;
            currentDirection.y = 0;
            if (currentDirection != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(currentDirection);
                _player.transform.rotation = Quaternion.Slerp(
                    _player.transform.rotation,
                    lookRot,
                    10f * Time.deltaTime
                );
            }
            
            yield return null;
        }

        // Final position
        if (_characterController != null)
        {
            _characterController.enabled = false;
            _player.transform.position = targetPosition;
            _characterController.enabled = true;
        }
        else
        {
            _player.transform.position = targetPosition;
        }

        // ═══ NEW: If door not opened yet (distance too short), open now ═══
        if (!doorOpened)
        {
            OpenDoor();
        }

        if (debugMode)
        {
            Debug.Log($"[VictorySequence] ✓ Player at victory position");
        }
        
        AdjustCameraForVictory("BeforeVictory");
        
        // Wait for door to finish opening
        yield return new WaitForSeconds(waitForDoorOpenDelay);
    }

    #endregion

    #region Step 3: Walk Into Home
    
    private IEnumerator WalkPlayerIntoHome()
    {
        Debug.Log("[VictorySequence] Step 3: Walk into home");
        
        if (_player == null)
        {
            yield break;
        }

        Vector3 entrancePosition = _homeSettings != null ?
            _homeSettings.GetHomeEntrancePosition() :
            _homePosition;

        float startTime = Time.time;
        Vector3 startPos = _player.transform.position;
        
        while (Time.time - startTime < enterHomeDuration)
        {
            if (_player == null) yield break;
            
            float t = (Time.time - startTime) / enterHomeDuration;
            
            Vector3 newPos = Vector3.Lerp(startPos, entrancePosition, t);
            
            if (_characterController != null)
            {
                _characterController.enabled = false;
                _player.transform.position = newPos;
                _characterController.enabled = true;
            }
            else
            {
                _player.transform.position = newPos;
            }
            
            yield return null;
        }

        if (debugMode)
        {
            Debug.Log("[VictorySequence] ✓ Player reached entrance");
        }
    }
    
    #endregion

    #region Step 4: Close Door - NEW
    
    /// <summary>
    /// Close door after player enters
    /// </summary>
    private IEnumerator CloseDoorAfterPlayerEnters()
    {
        Debug.Log("[VictorySequence] Step 4: CLOSE DOOR");
        
        CloseDoor();
        
        yield return new WaitForSeconds(0.2f);
    }
    
    #endregion

    #region Step 5: Hide Player
    
    private IEnumerator HidePlayer()
    {
        Debug.Log("[VictorySequence] Step 5: Hide player");
        
        if (_player == null)
        {
            yield break;
        }

        if (enablePlayerFade)
        {
            yield return StartCoroutine(FadeOutPlayer());
        }
        else
        {
            _player.gameObject.SetActive(false);
        }

        if (debugMode)
        {
            Debug.Log("[VictorySequence] ✓ Player hidden");
        }
    }
    
    private IEnumerator FadeOutPlayer()
    {
        Renderer[] renderers = _player.GetComponentsInChildren<Renderer>();
        
        float elapsed = 0f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / fadeDuration);
            
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    foreach (Material mat in renderer.materials)
                    {
                        if (mat.HasProperty("_Color"))
                        {
                            Color color = mat.color;
                            color.a = alpha;
                            mat.color = color;
                        }
                    }
                }
            }
            
            yield return null;
        }
        
        _player.gameObject.SetActive(false);
        AdjustCameraForVictory("Victory");
    }
    
    #endregion

    #region Step 6: Wait for Door Close - NEW
    
    private bool _isDoorClosedComplete = false;
    
    /// <summary>
    /// Wait for door close animation to complete
    /// </summary>
    private IEnumerator WaitForDoorCloseComplete()
    {
        Debug.Log("[VictorySequence] Step 6: Wait for door close complete");
        
        if (_doorController == null)
        {
            Debug.LogWarning("[VictorySequence] No door controller, skipping wait");
            yield break;
        }
        
        // Wait until door close event fires
        float timeout = 5f; // Safety timeout
        float elapsed = 0f;
        
        while (!_isDoorClosedComplete && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (_isDoorClosedComplete)
        {
            Debug.Log("[VictorySequence] ✅ Door close complete!");
        }
        else
        {
            Debug.LogWarning("[VictorySequence] ⚠️ Door close timeout!");
        }
    }
    
    #endregion

    #region Step 7: Effects
    
    private IEnumerator PlayVictoryEffects()
    {
        Debug.Log("[VictorySequence] Step 7: Play effects");
        
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
            GameObject confetti = Instantiate(confettiEffect, _homePosition + Vector3.up * 3f, Quaternion.identity);
            Destroy(confetti, 5f);
        }
        
        yield return new WaitForSeconds(beforeUIDelay);
    }
    
    #endregion

    #region Step 8: Show UI

    private void ShowVictoryUI()
    {
        Debug.Log("[VictorySequence] Step 8: Show Victory UI");

        EventManager.Instance?.TriggerEvent("OnHomeReached");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.Victory();
        }

        if (debugMode)
        {
            Debug.Log("[VictorySequence] ✓ Victory UI shown");
        }
    }
    
    #endregion
    
    #region Door Control - NEW
    
    /// <summary>
    /// Open the door
    /// </summary>
    private void OpenDoor()
    {
        if (_doorController == null)
        {
            Debug.LogWarning("[VictorySequence] No door to open!");
            return;
        }
        
        Debug.Log("[VictorySequence] 🚪 Opening door...");
        _doorController.Open();
    }
    
    /// <summary>
    /// Close the door
    /// </summary>
    private void CloseDoor()
    {
        if (_doorController == null)
        {
            Debug.LogWarning("[VictorySequence] No door to close!");
            return;
        }
        
        Debug.Log("[VictorySequence] 🚪 Closing door...");
        _doorController.Close();
    }
    
    #endregion

    #region Door Event Handlers - NEW
    
    /// <summary>
    /// Called when door open animation completes
    /// </summary>
    private void OnDoorOpened()
    {
        Debug.Log("[VictorySequence] 🚪✅ Door OPENED");
        // Door is now open, player can enter
    }
    
    /// <summary>
    /// Called when door close animation completes
    /// </summary>
    private void OnDoorClosed()
    {
        Debug.Log("[VictorySequence] 🚪✅ Door CLOSED");
        _isDoorClosedComplete = true;
    }
    
    #endregion

    #region Camera
    
    private void AdjustCameraForVictory(string state)
    {
        if (cameraController == null) return;
        
        switch (state)
        {
            case "BeforeVictory":
                cameraController.SetOffset(beforeVictoryCameraOffset);
                break;
            case "Victory":
                cameraController.SetOffset(victoryCameraOffset);
                break;
        }
        
        if (debugMode)
        {
            Debug.Log($"[VictorySequence] 📷 Camera adjusted");
        }
    }
    
    #endregion

    #region Cleanup - NEW
    
    void OnDestroy()
    {
        // Unsubscribe from door events
        if (_doorController != null)
        {
            _doorController.OnDoorOpenComplete -= OnDoorOpened;
            _doorController.OnDoorCloseComplete -= OnDoorClosed;
        }
    }
    
    #endregion
}