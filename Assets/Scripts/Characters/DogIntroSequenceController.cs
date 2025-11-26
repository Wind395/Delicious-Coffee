using UnityEngine;
using System.Collections;


public class DogIntroSequenceController : MonoBehaviour
{
    #region Singleton
    
    private static DogIntroSequenceController _instance;
    public static DogIntroSequenceController Instance => _instance;
    
    #endregion

    #region Serialized Fields
    
    [Header("References")]
    [SerializeField] private GameObject dogPrefab;
    [SerializeField] private ExclamationVFXController exclamationVFX;
    
    [Header("Dog Movement Settings")]
    [Tooltip("Dog spawn distance BEHIND player (in screen view)")]
    [SerializeField] private float dogStartDistance = 15f;
    
    [Tooltip("Dog run speed toward player")]
    [SerializeField] private float dogRunSpeed = 6f;
    
    [Tooltip("Dog stops this distance from player")]
    [SerializeField] private float dogStopDistance = 4f;
    
    [Header("Sequence Timing")]
    [Tooltip("Delay before dog barks")]
    [SerializeField] private float barkDelay = 0.3f;
    
    [Tooltip("Delay before player looks behind")]
    [SerializeField] private float lookBehindDelay = 0.2f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip dogBarkSound;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool showGizmos = true;

    [Tooltip("Delay AFTER look behind, BEFORE running")]
    [SerializeField] private float delayBeforeRunning = 1f;

    [Header("Player Rotation Settings")]
    [Tooltip("Reset player rotation before running")]
    [SerializeField] private bool resetRotationBeforeRunning = true;

    [Tooltip("Smooth rotation transition (0 = instant)")]
    [SerializeField] private float rotationTransitionDuration = 0.3f;

    [Tooltip("Target rotation when reset (usually 0, 0, 0)")]
    [SerializeField] private Vector3 targetRotation = Vector3.zero;
    
    #endregion

    #region State
    
    private GameObject _dogInstance;
    private Animator _dogAnimator;
    private Transform _playerTransform;
    private PlayerController _playerController;
    private PlayerAnimationController _playerAnimController;
    
    private bool _sequenceStarted = false;
    private bool _sequenceComplete = false;
    
    // Animation hashes
    private static readonly int ANIM_IS_RUNNING = Animator.StringToHash("IsRunning");
    private static readonly int ANIM_BARK = Animator.StringToHash("Bark");
    
    #endregion

    #region Events
    
    public event System.Action OnSequenceComplete;
    
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
    }

    void Start()
    {
        Initialize();
    }
    
    #endregion

    #region Initialization
    
    private void Initialize()
    {
        if (exclamationVFX == null)
        {
            exclamationVFX = FindObjectOfType<ExclamationVFXController>();
        }
        
        if (EventManager.Instance != null)
        {
            EventManager.Instance.StartListening(GameEvents.GAME_STARTED, OnGameStarted);
        }
        
        Debug.Log("[DogIntro] ✓ Initialized");
    }
    
    #endregion

    #region Sequence Control
    
    /// <summary>
    /// Start intro sequence
    /// </summary>
    public void StartIntroSequence(PlayerController player)
    {
        if (_sequenceStarted)
        {
            Debug.LogWarning("[DogIntro] Sequence already started!");
            return;
        }
        
        _playerController = player;
        _playerTransform = player.transform;
        _playerAnimController = player.GetComponent<PlayerAnimationController>();
        
        Debug.Log("[DogIntro] ═══════════════════════════════════");
        Debug.Log("[DogIntro] 🎬 STARTING NEW INTRO SEQUENCE");
        Debug.Log("[DogIntro] ═══════════════════════════════════");
        
        _sequenceStarted = true;
        
        StartCoroutine(IntroSequenceCoroutine());
    }

    /// <summary>
    /// Main sequence coroutine - NEW FLOW
    /// </summary>
    private IEnumerator IntroSequenceCoroutine()
    {
        // ═══ STEP 1: Player Idle (No Control) ═══
        yield return StartCoroutine(Step1_PlayerIdle());
        
        // ═══ STEP 2: Spawn Dog Far Behind ═══
        yield return StartCoroutine(Step2_SpawnDog());
        
        // ═══ STEP 3: Dog Runs Toward Player ═══
        yield return StartCoroutine(Step3_DogRunToPlayer());
        
        // ═══ STEP 4: Dog Stops + Bark ═══
        yield return StartCoroutine(Step4_DogBark());
        
        // ═══ STEP 5: Player Look Behind + Shock Particle ═══
        yield return StartCoroutine(Step5_PlayerLookBehindWithShock());
        
        // ═══ STEP 6: Player Start Running + Dog Chase ═══
        yield return StartCoroutine(Step6_StartChase());
        
        // ═══ COMPLETE ═══
        _sequenceComplete = true;
        
        Debug.Log("[DogIntro] ═══════════════════════════════════");
        Debug.Log("[DogIntro] ✅ INTRO SEQUENCE COMPLETE!");
        Debug.Log("[DogIntro] ═══════════════════════════════════");
        
        OnSequenceComplete?.Invoke();
    }
    
    #endregion

    #region Sequence Steps
    
    /// <summary>
    /// STEP 1: Player stands idle (no control)
    /// </summary>
    private IEnumerator Step1_PlayerIdle()
    {
        Debug.Log("[DogIntro] ▶ Step 1: Player Idle...");
        
        if (_playerController != null)
        {
            _playerController.StopPlayer(); // Ensure no movement
        }
        
        if (_playerAnimController != null)
        {
            _playerAnimController.SetIdleState(); // Set to idle animation
        }
        
        Debug.Log("[DogIntro] ✓ Player is idle and waiting");
        
        yield return new WaitForSeconds(0.5f);
    }

    /// <summary>
    /// STEP 2: Spawn dog far behind player
    /// </summary>
    private IEnumerator Step2_SpawnDog()
    {
        Debug.Log("[DogIntro] ▶ Step 2: Spawning Dog behind player...");
        
        if (dogPrefab == null)
        {
            Debug.LogError("[DogIntro] ❌ Dog prefab not assigned!");
            yield break;
        }
        
        // Calculate spawn position BEHIND player
        Vector3 spawnPos = _playerTransform.position - _playerTransform.forward * dogStartDistance;
        spawnPos.y = _playerTransform.position.y;
        
        // Spawn dog
        _dogInstance = Instantiate(dogPrefab, spawnPos, Quaternion.identity);
        _dogInstance.name = "DogIntro";
        
        // Face player
        Vector3 directionToPlayer = (_playerTransform.position - _dogInstance.transform.position).normalized;
        directionToPlayer.y = 0;
        if (directionToPlayer != Vector3.zero)
        {
            _dogInstance.transform.rotation = Quaternion.LookRotation(directionToPlayer);
        }
        
        // Get animator
        _dogAnimator = _dogInstance.GetComponent<Animator>();
        
        Debug.Log($"[DogIntro] ✓ Dog spawned at {spawnPos} (distance: {dogStartDistance}m)");
        
        yield return new WaitForSeconds(0.3f);
    }

    /// <summary>
    /// STEP 3: Dog runs toward player
    /// </summary>
    private IEnumerator Step3_DogRunToPlayer()
    {
        Debug.Log("[DogIntro] ▶ Step 3: Dog running to player...");
        
        if (_dogAnimator != null)
        {
            _dogAnimator.SetBool(ANIM_IS_RUNNING, true);
        }
        
        // Run until close enough
        float currentDistance = Vector3.Distance(_dogInstance.transform.position, _playerTransform.position);
        
        while (currentDistance > dogStopDistance)
        {
            // Move toward player
            Vector3 direction = (_playerTransform.position - _dogInstance.transform.position).normalized;
            direction.y = 0;
            
            _dogInstance.transform.position += direction * dogRunSpeed * Time.deltaTime;
            
            // Face player
            if (direction != Vector3.zero)
            {
                _dogInstance.transform.rotation = Quaternion.LookRotation(direction);
            }
            
            currentDistance = Vector3.Distance(_dogInstance.transform.position, _playerTransform.position);
            
            yield return null;
        }
        
        // Dog stops running
        if (_dogAnimator != null)
        {
            _dogAnimator.SetBool(ANIM_IS_RUNNING, false);
        }
        
        Debug.Log($"[DogIntro] ✓ Dog stopped at {currentDistance:F1}m from player");
        
        yield return new WaitForSeconds(0.2f);
    }

    /// <summary>
    /// STEP 4: Dog barks
    /// </summary>
    private IEnumerator Step4_DogBark()
    {
        Debug.Log("[DogIntro] ▶ Step 4: Dog barking...");
        
        yield return new WaitForSeconds(barkDelay);
        
        // Bark animation
        if (_dogAnimator != null)
        {
            _dogAnimator.SetTrigger(ANIM_BARK);
            Debug.Log("[DogIntro] ✓ Dog: Bark animation started");
        }
        
        // Bark sound
        if (dogBarkSound != null)
        {
            AudioManager.Instance?.PlaySFX(dogBarkSound);
        }
        else
        {
            AudioManager.Instance?.PlayDogBarkSound();
        }
        
        Debug.Log("[DogIntro] 🐕 BARK!");
        
        // Wait for bark animation to finish
        yield return new WaitForSeconds(1f);
    }

    /// <summary>
    /// STEP 5: Player look behind + shock particle
    /// UPDATED: Reset rotation before running
    /// </summary>
    private IEnumerator Step5_PlayerLookBehindWithShock()
    {
        Debug.Log("[DogIntro] ▶ Step 5: Player look behind + shock...");
        
        yield return new WaitForSeconds(lookBehindDelay);
        
        // ═══ PLAYER: Look Behind ═══
        if (_playerAnimController != null)
        {
            _playerAnimController.OnDogCollision();
            Debug.Log("[DogIntro] ✓ Player: LookBehind animation started");
        }
        
        // ═══ SPAWN "!" SHOCK PARTICLE ═══
        if (exclamationVFX != null)
        {
            exclamationVFX.PlayExclamation(_playerTransform);
            Debug.Log("[DogIntro] ✓ '!' shock particle spawned");
        }
        else
        {
            Debug.LogWarning("[DogIntro] ⚠ ExclamationVFX not found!");
        }
        
        // ═══ WAIT FOR LOOKBEHIND ANIMATION DURATION ═══
        float lookBehindDuration = _playerAnimController != null ? 
            _playerAnimController.LookBehindDuration : 1f;
        
        yield return new WaitForSeconds(lookBehindDuration);
        
        Debug.Log("[DogIntro] ✓ LookBehind animation complete");
        
        // ═══ RESET PLAYER ROTATION ═══
        if (resetRotationBeforeRunning)
        {
            yield return StartCoroutine(ResetPlayerRotation());
        }
        
        // ═══ DELAY BEFORE RUNNING ═══
        if (delayBeforeRunning > 0f)
        {
            Debug.Log($"[DogIntro] ⏱️ Waiting {delayBeforeRunning}s before running...");
            yield return new WaitForSeconds(delayBeforeRunning);
            Debug.Log("[DogIntro] ✓ Delay complete - ready to run");
        }
    }

    /// <summary>
    /// STEP 6: Player start running + dog chase
    /// </summary>
    private IEnumerator Step6_StartChase()
    {
        Debug.Log("[DogIntro] ▶ Step 6: Starting chase...");

        // ═══ PLAYER: Start Running ═══
        if (_playerAnimController != null)
        {
            // OnDogCollision() already triggers running after LookBehind
            Debug.Log("[DogIntro] ✓ Player: Running");
        }

        // Enable player control
        if (_playerController != null)
        {
            _playerController.EnableMovement();
            Debug.Log("[DogIntro] ✓ Player control ENABLED");
        }

        // ═══ DOG: Start Chase ═══
        if (DogChaseController.Instance != null)
        {
            // Transfer dog to chase controller
            DogChaseController.Instance.TakeDogFromIntro(_dogInstance);
            _dogInstance = null; // Transfer ownership

            Debug.Log("[DogIntro] ✓ Dog transferred to DogChaseController");
        }
        else
        {
            Debug.LogError("[DogIntro] ❌ DogChaseController not found!");
        }

        yield return new WaitForSeconds(0.2f);
    }
    
    #endregion
    
    #region Player Rotation Reset - NEW

    /// <summary>
    /// Reset player rotation to target (usually 0, 0, 0)
    /// </summary>
    private IEnumerator ResetPlayerRotation()
    {
        if (_playerTransform == null)
        {
            Debug.LogWarning("[DogIntro] Player transform is null!");
            yield break;
        }
        
        Quaternion targetRot = Quaternion.Euler(targetRotation);
        Quaternion startRot = _playerTransform.rotation;
        
        if (showDebugLogs)
        {
            Debug.Log($"[DogIntro] 🔄 Resetting rotation from {startRot.eulerAngles} to {targetRotation}");
        }
        
        // ═══ INSTANT RESET ═══
        if (rotationTransitionDuration <= 0f)
        {
            _playerTransform.rotation = targetRot;
            
            if (showDebugLogs)
            {
                Debug.Log("[DogIntro] ✓ Rotation reset (instant)");
            }
            
            yield break;
        }
        
        // ═══ SMOOTH TRANSITION ═══
        float elapsed = 0f;
        
        while (elapsed < rotationTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rotationTransitionDuration;
            
            // Smooth interpolation
            _playerTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            
            yield return null;
        }
        
        // Ensure final rotation
        _playerTransform.rotation = targetRot;
        
        if (showDebugLogs)
        {
            Debug.Log($"[DogIntro] ✓ Rotation reset complete (smooth {rotationTransitionDuration}s)");
        }
    }

    #endregion

    #region Event Handlers
    
    private void OnGameStarted()
    {
        // Sequence is started by GameManager explicitly
    }
    
    #endregion

    #region Cleanup
    
    void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.StopListening(GameEvents.GAME_STARTED, OnGameStarted);
        }
        
        if (_dogInstance != null)
        {
            Destroy(_dogInstance);
        }
    }
    
    #endregion

    #region Debug - UPDATED
    
    #if UNITY_EDITOR
    
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // Find player
        if (_playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
            }
        }
        
        if (_playerTransform == null) return;
        
        // ═══ DRAW DOG SPAWN POSITION (BEHIND PLAYER) ═══
        Vector3 dogSpawnPos = _playerTransform.position - _playerTransform.forward * dogStartDistance;
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(dogSpawnPos, 0.5f);
        
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.Label(dogSpawnPos + Vector3.up * 2f, "🐕 DOG SPAWN\n(Behind Player)");
        
        // ═══ DRAW DOG STOP POSITION ═══
        Vector3 dogStopPos = _playerTransform.position - _playerTransform.forward * dogStopDistance;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(dogStopPos, 0.5f);
        
        UnityEditor.Handles.color = Color.yellow;
        UnityEditor.Handles.Label(dogStopPos + Vector3.up * 1f, "⏹️ DOG STOPS HERE");
        
        // ═══ DRAW DOG RUN PATH ═══
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(dogSpawnPos, dogStopPos);
        
        UnityEditor.Handles.Label(
            (dogSpawnPos + dogStopPos) / 2f + Vector3.up * 1.5f,
            $"← Dog runs {dogStartDistance - dogStopDistance}m →"
        );
        
        // ═══ DRAW PLAYER POSITION ═══
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_playerTransform.position, 0.3f);
        
        UnityEditor.Handles.color = Color.cyan;
        UnityEditor.Handles.Label(_playerTransform.position + Vector3.up * 2.5f, "🧍 PLAYER\n(Idle)");
        
        // ═══ SHOW SEQUENCE STATE ═══
        if (Application.isPlaying)
        {
            string stateInfo = "";
            
            if (!_sequenceStarted)
            {
                stateInfo = "⏸️ NOT STARTED";
            }
            else if (_sequenceComplete)
            {
                stateInfo = "✅ COMPLETE";
            }
            else
            {
                stateInfo = "🎬 IN PROGRESS";
            }
            
            UnityEditor.Handles.Label(
                _playerTransform.position + Vector3.up * 3.5f,
                $"INTRO SEQUENCE:\n{stateInfo}"
            );
        }
    }
    
    [ContextMenu("Print Sequence Info")]
    public void PrintSequenceInfo()
    {
        Debug.Log("═══════════════════════════════════");
        Debug.Log("DOG INTRO SEQUENCE INFO (NEW FLOW)");
        Debug.Log("═══════════════════════════════════");
        Debug.Log($"Dog Prefab: {(dogPrefab != null ? dogPrefab.name : "NULL")}");
        Debug.Log($"Dog Start Distance (behind): {dogStartDistance}m");
        Debug.Log($"Dog Run Speed: {dogRunSpeed} m/s");
        Debug.Log($"Dog Stop Distance: {dogStopDistance}m");
        Debug.Log($"Bark Delay: {barkDelay}s");
        Debug.Log($"LookBehind Delay: {lookBehindDelay}s");
        Debug.Log($"Exclamation VFX: {(exclamationVFX != null ? "✓" : "✗")}");
        Debug.Log($"Sequence Started: {_sequenceStarted}");
        Debug.Log($"Sequence Complete: {_sequenceComplete}");
        Debug.Log("═══════════════════════════════════");
    }
    
    #endif
    
    #endregion
}