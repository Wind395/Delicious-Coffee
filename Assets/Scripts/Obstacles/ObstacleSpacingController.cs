using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Auto-spacing controller - Uses EXISTING collider
/// No new colliders created - works with CapsuleCollider/BoxCollider/SphereCollider
/// </summary>
public class ObstacleSpacingController : MonoBehaviour
{
    #region Serialized Fields

    [Header("Spacing Settings")]
    [Tooltip("Minimum distance to maintain from other objects")]
    [SerializeField] private float minSpacing = 3f;

    [Tooltip("How far to push when overlap detected")]
    [SerializeField] private float pushDistance = 3f;

    [Tooltip("Push direction (forward/backward)")]
    [SerializeField] private PushDirection pushDirection = PushDirection.Auto;

    [Header("Object Type")]
    [Tooltip("Type of this object (for spacing rules)")]
    [SerializeField] private SpacingObjectType objectType = SpacingObjectType.Obstacle;

    [Header("Detection Settings")]
    [Tooltip("Detection method")]
    [SerializeField] private DetectionMethod detectionMethod = DetectionMethod.PhysicsOverlap;

    [Tooltip("Detection radius multiplier (based on collider size)")]
    [SerializeField] private float detectionRadiusMultiplier = 1.5f;

    [Header("Timing")]
    [Tooltip("Delay before checking spacing (wait for spawn to complete)")]
    [SerializeField] private float checkDelay = 0.1f;

    [Tooltip("Only check once on spawn")]
    [SerializeField] private bool checkOnce = true;

    [Header("Layer Settings")]
    [Tooltip("Layers to check for spacing (leave default for all)")]
    [SerializeField] private LayerMask spacingLayers = -1;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    [SerializeField] private bool drawDebugLines = false;

    #endregion

    #region Enums

    public enum PushDirection
    {
        Auto,       // Push away from overlapping object
        Forward,    // Always push forward (positive Z)
        Backward    // Always push backward (negative Z)
    }

    public enum SpacingObjectType
    {
        Obstacle,
        Coin,
        PowerUp
    }

    public enum DetectionMethod
    {
        PhysicsOverlap,  // Use Physics.OverlapSphere
        Trigger          // Use OnTriggerStay (requires existing trigger)
    }

    #endregion

    #region State

    private Collider _mainCollider;
    private bool _hasChecked = false;
    private bool _isAdjusting = false;
    private Vector3 _originalPosition;
    private float _detectionRadius;

    // For trigger method
    private HashSet<GameObject> _overlappingObjects = new HashSet<GameObject>();

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        SetupCollider();
    }

    void OnEnable()
    {
        _hasChecked = false;
        _originalPosition = transform.position;
        _overlappingObjects.Clear();

        StartCoroutine(CheckSpacingAfterDelay());
    }

    void OnDisable()
    {
        _hasChecked = false;
        _overlappingObjects.Clear();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Setup existing collider (NO NEW COLLIDER CREATED)
    /// </summary>
    private void SetupCollider()
    {
        // ═══ GET EXISTING COLLIDER ═══
        _mainCollider = GetComponent<Collider>();

        if (_mainCollider == null)
        {
            Debug.LogError($"[SpacingController] {gameObject.name} has NO COLLIDER! Please add one manually.");
            enabled = false;
            return;
        }

        // ═══ CALCULATE DETECTION RADIUS FROM EXISTING COLLIDER ═══
        _detectionRadius = CalculateColliderRadius(_mainCollider) * detectionRadiusMultiplier;

        // ═══ CONFIGURE EXISTING COLLIDER ═══
        if (detectionMethod == DetectionMethod.Trigger)
        {
            // Use as trigger for detection
            if (!_mainCollider.isTrigger)
            {
                Debug.LogWarning($"[SpacingController] {gameObject.name} collider is NOT a trigger. " +
                    "Set to trigger or use PhysicsOverlap detection method.");
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"[SpacingController] Using existing {_mainCollider.GetType().Name} on {gameObject.name}");
            Debug.Log($"[SpacingController] Detection radius: {_detectionRadius:F2}m");
        }
    }

    /// <summary>
    /// Calculate effective radius from any collider type
    /// </summary>
    private float CalculateColliderRadius(Collider col)
    {
        if (col is SphereCollider sphere)
        {
            return sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        }
        else if (col is CapsuleCollider capsule)
        {
            // Use capsule radius + half height
            float scaleXZ = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
            return Mathf.Max(capsule.radius * scaleXZ, capsule.height * 0.5f * transform.lossyScale.y);
        }
        else if (col is BoxCollider box)
        {
            // Use max extent
            Vector3 size = box.size;
            Vector3 scaledSize = new Vector3(
                size.x * transform.lossyScale.x,
                size.y * transform.lossyScale.y,
                size.z * transform.lossyScale.z
            );
            return Mathf.Max(scaledSize.x, scaledSize.y, scaledSize.z) * 0.5f;
        }
        else
        {
            // Generic bounds
            return col.bounds.extents.magnitude;
        }
    }

    #endregion

    #region Spacing Check

    /// <summary>
    /// Check spacing after delay
    /// </summary>
    private IEnumerator CheckSpacingAfterDelay()
    {
        yield return new WaitForSeconds(checkDelay);

        if (!_hasChecked || !checkOnce)
        {
            CheckAndAdjustSpacing();
            _hasChecked = true;
        }
    }

    private void CheckAndAdjustSpacing()
    {
        if (_isAdjusting || _mainCollider == null) return;

        int maxAttempts = 5;
        int attempt = 0;
        bool hasOverlap = true;

        while (hasOverlap && attempt < maxAttempts)
        {
            List<GameObject> overlappingObjects = new List<GameObject>();

            // ═══ DETECTION METHOD ═══
            switch (detectionMethod)
            {
                case DetectionMethod.PhysicsOverlap:
                    overlappingObjects = FindOverlappingObjectsPhysics();
                    break;
                case DetectionMethod.Trigger:
                    overlappingObjects = new List<GameObject>(_overlappingObjects);
                    break;
            }

            // ═══ FILTER BY DISTANCE ═══
            overlappingObjects.RemoveAll(obj =>
            {
                if (obj == null) return true;
                float distance = Vector3.Distance(transform.position, obj.transform.position);
                return distance >= minSpacing;
            });

            hasOverlap = overlappingObjects.Count > 0;

            if (hasOverlap)
            {
                AdjustPosition(overlappingObjects);
            }

            attempt++;
        }

        // Sau khi spacing xong, nếu là coin, kiểm tra lại với obstacle
        if (objectType == SpacingObjectType.Coin)
        {
            List<GameObject> obstacles = FindNearbyObstacles();
            if (!IsCoinPositionSafe())
            {
                TryAdjustAwayFromObstacles(obstacles);
            }
        }

        if (showDebugLogs && hasOverlap)
        {
            Debug.LogWarning($"[SpacingController] {gameObject.name} vẫn còn overlap sau {maxAttempts} lần thử!");
        }
    }


    /// <summary>
    /// Find nearby obstacles
    /// </summary>
    private List<GameObject> FindNearbyObstacles()
    {
        List<GameObject> obstacles = new List<GameObject>();

        Collider[] nearbyColliders = Physics.OverlapSphere(
            transform.position,
            _detectionRadius * 2f,
            spacingLayers
        );

        foreach (var col in nearbyColliders)
        {
            if (col.gameObject == gameObject) continue;

            if (col.GetComponent<Obstacle>() != null)
            {
                obstacles.Add(col.gameObject);
            }
        }

        return obstacles;
    }

    /// <summary>
    /// Try to adjust coin position away from obstacles
    /// </summary>
    private bool TryAdjustAwayFromObstacles(List<GameObject> obstacles)
    {
        // Try multiple positions
        float[] offsets = { 3f, 5f, 7f, 10f, -3f, -5f, -7f, -10f };

        foreach (float offset in offsets)
        {
            Vector3 testPosition = transform.position + Vector3.forward * offset;
            testPosition.y = transform.position.y;

            // Check if this position is safe
            bool isSafe = true;

            foreach (var obstacle in obstacles)
            {
                float distance = Vector3.Distance(testPosition, obstacle.transform.position);

                if (distance < minSpacing + 2f)
                {
                    isSafe = false;
                    break;
                }
            }

            if (isSafe)
            {
                // Found safe position
                transform.position = testPosition;

                if (showDebugLogs)
                {
                    Debug.Log($"[SpacingController] ✓ COIN adjusted away from obstacles by {offset}m");
                }

                if (drawDebugLines)
                {
                    Debug.DrawLine(_originalPosition, testPosition, Color.cyan, 5f);
                }

                return true;
            }
        }

        return false; // No safe position found
    }


    /// <summary>
    /// Find overlapping objects using Physics.OverlapSphere
    /// </summary>
    private List<GameObject> FindOverlappingObjectsPhysics()
    {
        List<GameObject> result = new List<GameObject>();

        Collider[] overlaps = Physics.OverlapSphere(
            transform.position,
            _detectionRadius,
            spacingLayers
        );

        foreach (var col in overlaps)
        {
            // Skip self
            if (col.gameObject == gameObject) continue;

            // Check if spacing object
            if (IsSpacingObject(col.gameObject))
            {
                result.Add(col.gameObject);
            }
        }

        return result;
    }

    /// <summary>
    /// Check if object should be considered for spacing
    /// </summary>
    private bool IsSpacingObject(GameObject obj)
    {
        return obj.GetComponent<Obstacle>() != null ||
               obj.GetComponent<Coin>() != null ||
               obj.GetComponent<PowerUpCollectible>() != null ||
               obj.GetComponent<ObstacleSpacingController>() != null;
    }

    #endregion

    #region Position Adjustment

    /// <summary>
    /// Adjust position to avoid overlapping objects
    /// </summary>
    private void AdjustPosition(List<GameObject> overlappingObjects)
    {
        _isAdjusting = true;

        Vector3 pushVector = CalculatePushVector(overlappingObjects);

        // Apply push
        Vector3 newPosition = transform.position + pushVector;

        // Clamp Y position (don't change height)
        newPosition.y = transform.position.y;

        // Validate new position
        if (IsPositionValid(newPosition))
        {
            transform.position = newPosition;

            if (showDebugLogs)
            {
                Debug.Log($"[SpacingController] ✓ {gameObject.name} adjusted by {pushVector.magnitude:F2}m (Z offset: {pushVector.z:F2}m)");
            }

            if (drawDebugLines)
            {
                Debug.DrawLine(_originalPosition, newPosition, Color.green, 5f);
            }
        }
        else
        {
            // Try opposite direction
            newPosition = transform.position - pushVector;
            newPosition.y = transform.position.y;

            if (IsPositionValid(newPosition))
            {
                transform.position = newPosition;

                if (showDebugLogs)
                {
                    Debug.Log($"[SpacingController] ✓ {gameObject.name} adjusted (opposite) by {pushVector.magnitude:F2}m");
                }

                if (drawDebugLines)
                {
                    Debug.DrawLine(_originalPosition, newPosition, Color.yellow, 5f);
                }
            }
            else
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning($"[SpacingController] ❌ {gameObject.name} cannot find valid position!");
                }

                if (drawDebugLines)
                {
                    Debug.DrawLine(_originalPosition, transform.position, Color.red, 5f);
                }
            }
        }

        _isAdjusting = false;
    }

    /// <summary>
    /// Calculate push vector based on overlapping objects
    /// </summary>
    private Vector3 CalculatePushVector(List<GameObject> overlappingObjects)
    {
        Vector3 pushVector = Vector3.zero;

        switch (pushDirection)
        {
            case PushDirection.Auto:
                // Calculate direction away from nearest object
                GameObject nearest = GetNearestObject(overlappingObjects);

                if (nearest != null)
                {
                    Vector3 directionAway = (transform.position - nearest.transform.position).normalized;
                    pushVector = directionAway * pushDistance;
                }
                else
                {
                    // Fallback: average direction
                    foreach (var obj in overlappingObjects)
                    {
                        Vector3 directionAway = (transform.position - obj.transform.position).normalized;
                        pushVector += directionAway;
                    }

                    pushVector = pushVector.normalized * pushDistance;
                }

                // Only push along Z axis (same lane)
                pushVector.x = 0;
                pushVector.y = 0;
                break;

            case PushDirection.Forward:
                pushVector = Vector3.forward * pushDistance;
                break;

            case PushDirection.Backward:
                pushVector = Vector3.back * pushDistance;
                break;
        }

        return pushVector;
    }

    /// <summary>
    /// Get nearest overlapping object
    /// </summary>
    private GameObject GetNearestObject(List<GameObject> objects)
    {
        if (objects.Count == 0) return null;

        GameObject nearest = objects[0];
        float minDistance = Vector3.Distance(transform.position, nearest.transform.position);

        foreach (var obj in objects)
        {
            float distance = Vector3.Distance(transform.position, obj.transform.position);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = obj;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Check if new position is valid (no new overlaps)
    /// </summary>
    private bool IsPositionValid(Vector3 newPosition)
    {
        // Check if new position creates new overlaps
        Collider[] overlaps = Physics.OverlapSphere(
            newPosition,
            _detectionRadius * 0.7f, // Smaller radius for validation
            spacingLayers
        );

        foreach (var col in overlaps)
        {
            if (col.gameObject == gameObject) continue;

            if (IsSpacingObject(col.gameObject))
            {
                float distance = Vector3.Distance(newPosition, col.transform.position);

                if (distance < minSpacing * 0.8f) // Allow slightly closer
                {
                    if (showDebugLogs)
                    {
                        Debug.LogWarning($"[SpacingController] New position still overlaps with {col.gameObject.name}");
                    }
                    return false;
                }
            }
        }

        return true;
    }

    #endregion

    #region Enhanced Coin Detection

    /// <summary>
    /// Special check for coins - must avoid ALL obstacles in detection area
    /// </summary>
    private bool IsCoinPositionSafe()
    {
        if (objectType != SpacingObjectType.Coin)
        {
            return true; // Not a coin, skip
        }

        // Check larger area for coins
        float coinSafetyRadius = _detectionRadius * 1.5f;

        Collider[] nearbyColliders = Physics.OverlapSphere(
            transform.position,
            coinSafetyRadius,
            spacingLayers
        );

        foreach (var col in nearbyColliders)
        {
            if (col.gameObject == gameObject) continue;

            // Check if obstacle
            Obstacle obstacle = col.GetComponent<Obstacle>();

            if (obstacle != null)
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);

                // Coins must be further from obstacles
                float requiredDistance = minSpacing + 2f; // Extra 2m safety buffer

                if (distance < requiredDistance)
                {
                    if (showDebugLogs)
                    {
                        Debug.LogError($"[SpacingController] ❌ COIN too close to OBSTACLE: {distance:F1}m < {requiredDistance:F1}m");
                    }
                    return false;
                }
            }
        }

        return true;
    }

    #endregion

    #region Trigger Events (Optional - for Trigger detection method)

    /// <summary>
    /// Trigger stay - track overlapping objects
    /// </summary>
    void OnTriggerStay(Collider other)
    {
        if (detectionMethod != DetectionMethod.Trigger) return;

        if (IsSpacingObject(other.gameObject))
        {
            _overlappingObjects.Add(other.gameObject);

            if (!_hasChecked || !checkOnce)
            {
                CheckAndAdjustSpacing();
            }
        }
    }

    /// <summary>
    /// Trigger exit - remove from tracking
    /// </summary>
    void OnTriggerExit(Collider other)
    {
        if (detectionMethod != DetectionMethod.Trigger) return;

        _overlappingObjects.Remove(other.gameObject);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Force spacing check
    /// </summary>
    public void ForceCheck()
    {
        _hasChecked = false;
        CheckAndAdjustSpacing();
    }

    /// <summary>
    /// Set minimum spacing
    /// </summary>
    public void SetMinSpacing(float spacing)
    {
        minSpacing = spacing;
    }

    /// <summary>
    /// Enable/disable spacing controller
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
    }

    /// <summary>
    /// Get detection radius
    /// </summary>
    public float GetDetectionRadius()
    {
        return _detectionRadius;
    }

    #endregion

    #region Debug Visualization

    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (_mainCollider == null) return;

        // Draw detection radius
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);

        // Draw min spacing
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, minSpacing);

        // Draw existing collider bounds
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawWireCube(_mainCollider.bounds.center, _mainCollider.bounds.size);
    }
    #endif

    #endregion
}