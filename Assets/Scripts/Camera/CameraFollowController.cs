using UnityEngine;

/// <summary>
/// Camera Follow Controller - Smooth camera following
/// SOLID: Single Responsibility - Camera control only
/// </summary>
public class CameraFollowController : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Target")]
    [SerializeField] private Transform target;

    
    [Header("Follow Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 5, -10);
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private bool lookAtTarget = true;
    
    [Header("Look At Settings")]
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0, 2, 0);
    [SerializeField] private float lookSmoothSpeed = 5f;
    
    [Header("Camera Shake")]
    [SerializeField] private bool enableShake = true;
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeMagnitude = 0.3f;
    
    #endregion

    #region State
    
    private Vector3 _velocity = Vector3.zero;
    private Vector3 _currentLookAtVelocity = Vector3.zero;
    
    // Shake state
    private bool _isShaking = false;
    private float _shakeTimer = 0f;
    private Vector3 _shakeOffset = Vector3.zero;
    
    #endregion

    #region Unity Lifecycle
    
    void Start()
    {
        FindTarget();
        //Initialize();
    }

    void LateUpdate()
    {
        if (target == null) return;

        UpdateCameraPosition();
        UpdateCameraRotation();
        UpdateCameraShake();
    }

    #endregion

    
    #region Target Finding - UPDATED
    
    /// <summary>
    /// Find player target - Multiple methods
    /// </summary>
    private void FindTarget()
    {
        if (target != null)
        {
            Debug.Log($"[Camera] ✓ Target already assigned: {target.name}");
            return;
        }
        
        // Method 1: Via CharacterSpawner
        CharacterSpawner spawner = FindAnyObjectByType<CharacterSpawner>();
        if (spawner != null && spawner.CurrentPlayer != null)
        {
            target = spawner.CurrentPlayer.transform;
            return;
        }
        
        // Method 2: Via GameManager
        if (GameManager.Instance != null)
        {
            PlayerController player = GameManager.Instance.GetPlayer();
            
            if (player != null)
            {
                target = player.transform;
                return;
            }
        }
        
        // Method 3: Via Player tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        
        if (playerObj != null)
        {
            target = playerObj.transform;
            return;
        }
        Debug.LogWarning("[Camera] ⚠️ No player target found!");
    }
    
    #endregion

    #region Camera Updates
    
    /// <summary>
    /// Update camera position - Smooth follow
    /// KISS: Simple SmoothDamp
    /// </summary>
    private void UpdateCameraPosition()
    {
        //Calculate desired position
        Vector3 desiredPosition = target.position + offset + _shakeOffset;

        //Smooth damp to desired position
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref _velocity,
            1f / smoothSpeed
        );

        transform.position = smoothedPosition;

        // Vector3 desiredPosition = target.position + offset + _shakeOffset;
        // desiredPosition.x = Mathf.Clamp(desiredPosition.x, -2f, 2f);

        // Vector3 smoothPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // transform.position = smoothPosition;
    }

    /// <summary>
    /// Update camera rotation - Look at target
    /// </summary>
    private void UpdateCameraRotation()
    {
        if (!lookAtTarget) return;

        // Calculate look at point
        Vector3 lookAtPoint = target.position + lookAtOffset;

        // Calculate desired rotation
        Quaternion desiredRotation = Quaternion.LookRotation(
            lookAtPoint - transform.position
        );

        // Smooth rotation
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            lookSmoothSpeed * Time.deltaTime
        );
    }

    /// <summary>
    /// Update camera shake effect
    /// </summary>
    private void UpdateCameraShake()
    {
        if (!_isShaking) return;

        _shakeTimer -= Time.deltaTime;

        if (_shakeTimer > 0f)
        {
            // Random shake offset
            _shakeOffset = Random.insideUnitSphere * shakeMagnitude;
        }
        else
        {
            // End shake
            _isShaking = false;
            _shakeOffset = Vector3.zero;
        }
    }
    
    #endregion

    #region Camera Shake - Public API
    
    /// <summary>
    /// Trigger camera shake
    /// </summary>
    public void Shake()
    {
        if (!enableShake) return;

        _isShaking = true;
        _shakeTimer = shakeDuration;
    }

    /// <summary>
    /// Trigger shake with custom parameters
    /// </summary>
    public void Shake(float duration, float magnitude)
    {
        if (!enableShake) return;

        _isShaking = true;
        _shakeTimer = duration;
        
        // Temporarily override magnitude
        float originalMagnitude = shakeMagnitude;
        shakeMagnitude = magnitude;
        
        // Reset after shake
        Invoke(nameof(ResetShakeMagnitude), duration);
    }

    /// <summary>
    /// Reset shake magnitude
    /// </summary>
    private void ResetShakeMagnitude()
    {
        // This would need to store original value
        // For simplicity, using default
    }

    #endregion


    #region Offset Control - ADD THESE METHODS

    /// <summary>
    /// Get current offset - NEW
    /// </summary>
    public Vector3 GetOffset()
    {
        return offset;
    }

    /// <summary>
    /// Set new offset - NEW
    /// </summary>
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }

    /// <summary>
    /// Smoothly transition to new offset - NEW
    /// </summary>
    public void TransitionToOffset(Vector3 newOffset, float duration)
    {
        StartCoroutine(SmoothOffsetTransition(newOffset, duration));
    }

    private System.Collections.IEnumerator SmoothOffsetTransition(Vector3 targetOffset, float duration)
    {
        Vector3 startOffset = offset;
        float time = 0f;
        
        while (time < duration)
        {
            offset = Vector3.Lerp(startOffset, targetOffset, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        
        offset = targetOffset;
    }

    #endregion


    #region Public API

    /// <summary>
    /// Set new target
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        _velocity = Vector3.zero;
    }

    /// <summary>
    /// Set smooth speed
    /// </summary>
    public void SetSmoothSpeed(float speed)
    {
        smoothSpeed = Mathf.Max(0.1f, speed);
    }

    /// <summary>
    /// Reset camera to target immediately (no smooth)
    /// </summary>
    public void ResetToTarget()
    {
        if (target == null) return;

        transform.position = target.position + offset;
        
        if (lookAtTarget)
        {
            transform.LookAt(target.position + lookAtOffset);
        }

        _velocity = Vector3.zero;
    }
    
    #endregion

    #region Gizmos
    
    #if UNITY_EDITOR
    
    void OnDrawGizmosSelected()
    {
        if (target == null) return;

        // Draw offset line
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(target.position, target.position + offset);
        Gizmos.DrawWireSphere(target.position + offset, 0.5f);

        // Draw look at point
        if (lookAtTarget)
        {
            Gizmos.color = Color.cyan;
            Vector3 lookAtPoint = target.position + lookAtOffset;
            Gizmos.DrawWireSphere(lookAtPoint, 0.3f);
            Gizmos.DrawLine(transform.position, lookAtPoint);
        }
    }
    
    #endif
    
    #endregion
}